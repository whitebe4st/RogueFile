// ============================================================
// HandSkeletonInference.cs
// Requires: a YOLO11-pose model trained on hands with 21 keypoints.
// Output tensor shape: [1, 68, 8400]
//   where 68 = 4 (cx,cy,w,h) + 1 (confidence) + 21*3 (kx,ky,kconf each)
//
// Recommended model: "nicehuster/yolo11n-hand-pose" on HuggingFace
// Export it to ONNX, then import into Unity Sentis.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class HandSkeletonInference : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset modelAsset;
    public RawImage displayImage;

    [Header("Inference")]
    public int inputWidth  = 640;
    public int inputHeight = 640;
    [Range(0f, 1f)] public float confidenceThreshold = 0.15f;
    [Range(0f, 1f)] public float iouThreshold        = 0.45f;
    [Range(0f, 1f)] public float keypointThreshold   = 0.10f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothingFactor = 0.6f;  // 0 = no smoothing, 1 = frozen

    [Header("Visuals")]
    public Color skeletonColor  = Color.green;
    public Color keypointColor  = Color.cyan;
    public Color boxColor       = Color.yellow;
    public float lineWidth      = 2f;
    public float keypointRadius = 6f;

    // ─── 21 MediaPipe-style hand keypoints ──────────────────────────────
    // 0: Wrist
    // 1-4:  Thumb  (CMC → TIP)
    // 5-8:  Index  (MCP → TIP)
    // 9-12: Middle (MCP → TIP)
    // 13-16:Ring   (MCP → TIP)
    // 17-20:Pinky  (MCP → TIP)
    static readonly int[][] SKELETON_CONNECTIONS = new int[][]
    {
        // Thumb
        new int[]{0,1}, new int[]{1,2}, new int[]{2,3}, new int[]{3,4},
        // Index
        new int[]{0,5}, new int[]{5,6}, new int[]{6,7}, new int[]{7,8},
        // Middle
        new int[]{0,9}, new int[]{9,10}, new int[]{10,11}, new int[]{11,12},
        // Ring
        new int[]{0,13}, new int[]{13,14}, new int[]{14,15}, new int[]{15,16},
        // Pinky
        new int[]{0,17}, new int[]{17,18}, new int[]{18,19}, new int[]{19,20},
        // Palm knuckles
        new int[]{5,9}, new int[]{9,13}, new int[]{13,17},
    };

    // Colour per finger for a nicer look
    static readonly Color[] FINGER_COLORS =
    {
        new Color(1f, 0.2f, 0.2f),  // Thumb  – red
        new Color(1f, 0.8f, 0f),    // Index  – yellow
        new Color(0.2f, 1f, 0.2f),  // Middle – green
        new Color(0.2f, 0.6f, 1f),  // Ring   – blue
        new Color(0.8f, 0.2f, 1f),  // Pinky  – purple
    };

    // Connection → finger index lookup (same order as SKELETON_CONNECTIONS)
    static readonly int[] CONNECTION_FINGER = new int[]
    {
        0,0,0,0,  // Thumb
        1,1,1,1,  // Index
        2,2,2,2,  // Middle
        3,3,3,3,  // Ring
        4,4,4,4,  // Pinky
        1,2,3,    // Palm (colour by left finger)
    };

    struct HandDetection
    {
        public Rect   box;
        public float  confidence;
        public Vector2[] keypoints;   // 21 points, normalised 0-1
        public float[]   kpConf;      // 21 confidence values
    }

    private WebCamTexture            webCamTexture;
    private Model                    runtimeModel;
    private Worker                   worker;
    private Tensor<float>            inputTensor;
    private List<HandDetection>      detectedHands = new List<HandDetection>();
    private List<HandDetection>      smoothedHands = new List<HandDetection>();  // temporal filter

    private Vector2                  letterboxScale  = Vector2.one;
    private Vector2                  letterboxOffset = Vector2.zero;
    private RenderTexture            letterboxTexture;

    // ─── Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        // Webcam
        var devices = WebCamTexture.devices;
        if (devices.Length == 0) { Debug.LogError("[Hand] No webcam found!"); return; }
        webCamTexture = new WebCamTexture(devices[0].name, 640, 480, 30);
        if (displayImage) displayImage.texture = webCamTexture;
        webCamTexture.Play();
        Debug.Log("[Hand] Webcam started.");

        // Model — auto-detect input resolution from ONNX metadata
        runtimeModel = ModelLoader.Load(modelAsset);
        worker       = new Worker(runtimeModel, BackendType.GPUCompute);

        var inShape = runtimeModel.inputs[0].shape;
        // inShape is a DynamicTensorShape; dims are [N, C, H, W] for NCHW models.
        // Use inspector values as fallback if shape is dynamic (value == -1).
        if (inShape.rank == 4)
        {
            int h = inShape.Get(2);
            int w = inShape.Get(3);
            inputHeight = (h <= 0) ? inputHeight : h;
            inputWidth  = (w <= 0) ? inputWidth  : w;
        }
        Debug.Log($"[Hand] Model input: (1, 3, {inputHeight}, {inputWidth})");

        inputTensor = new Tensor<float>(new TensorShape(1, 3, inputHeight, inputWidth));
        Debug.Log("[Hand] Model loaded.");
    }

    void Update()
    {
        if (webCamTexture != null && webCamTexture.didUpdateThisFrame)
            RunInference();
    }

    // ─── Inference ───────────────────────────────────────────────────────

    void RunInference()
    {
        // 1. Create letterbox texture to avoid Aspect Ratio squashing (crucial for YOLO)
        if (letterboxTexture == null || letterboxTexture.width != inputWidth || letterboxTexture.height != inputHeight)
        {
            if (letterboxTexture != null) letterboxTexture.Release();
            letterboxTexture = new RenderTexture(inputWidth, inputHeight, 0, RenderTextureFormat.ARGB32);
            letterboxTexture.Create();
        }

        // 2. Clear canvas to YOLO padding colour (grey 114)
        RenderTexture.active = letterboxTexture;
        GL.Clear(false, true, new Color(114f/255f, 114f/255f, 114f/255f, 1f));

        // 3. Calculate aspect ratio fit to draw the webcam feed in the exact centre
        float aspectSrc = (float)webCamTexture.width / webCamTexture.height;
        float aspectDst = (float)inputWidth / inputHeight;
        
        float w = inputWidth;
        float h = inputHeight;

        if (aspectSrc > aspectDst) {
            h = inputWidth / aspectSrc;
        } else {
            w = inputHeight * aspectSrc;
        }

        float x = (inputWidth - w) / 2f;
        float y = (inputHeight - h) / 2f;

        letterboxScale = new Vector2(w / inputWidth, h / inputHeight);
        letterboxOffset = new Vector2(x / inputWidth, y / inputHeight);

        // Draw webcam texture in the centre with correct aspect ratio
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, inputWidth, inputHeight, 0);
        Graphics.DrawTexture(new Rect(x, y, w, h), webCamTexture);
        GL.PopMatrix();
        RenderTexture.active = null;

        // 4. Send padded square image to Sentis
        var transform = new TextureTransform().SetDimensions(inputWidth, inputHeight, 3);
        TextureConverter.ToTensor(letterboxTexture, inputTensor, transform);
        worker.Schedule(inputTensor);

        using Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        using var cpu = output.ReadbackAndClone();
        float[] data = cpu.DownloadToArray();

        Debug.Log($"[Hand] Output shape: {cpu.shape}");

        if (cpu.shape.rank == 2)
            DecodeLandmarkRegression(cpu.shape, data);   // (1, 63) style
        else if (cpu.shape.rank == 3)
            DecodeOutput(cpu.shape, data);               // YOLO-pose (1, 68, 8400) style
        else
            Debug.LogWarning($"[Hand] Unsupported output rank {cpu.shape.rank}. Shape: {cpu.shape}");
    }

    void DecodeOutput(TensorShape shape, float[] data)
    {
        // Expected: [1, 68, 8400]  (5 + 21*3 = 68 features per anchor)
        // Also handle transposed [1, 8400, 68] exports.
        int dim1 = shape[1], dim2 = shape[2];
        int numAnchors  = Mathf.Max(dim1, dim2);
        int numFeatures = Mathf.Min(dim1, dim2);
        bool transposed = dim1 == numAnchors;

        int GetIndex(int f, int a) => transposed ? a * numFeatures + f : f * numAnchors + a;

        int numKp = (numFeatures - 5) / 3;
        if (numKp != 21)
        {
            Debug.LogError($"[Hand] WRONG MODEL ASSIGNED! Expected a YOLO-pose model with 68 features (got {numFeatures}). " +
                           $"The model you assigned is NOT a 21-keypoint skeleton model. Please use 'best.onnx'.");
            return;
        }

        var candidates = new List<HandDetection>();

        for (int a = 0; a < numAnchors; a++)
        {
            float conf = data[GetIndex(4, a)];
            if (conf < confidenceThreshold) continue;

            float cx = data[GetIndex(0, a)];
            float cy = data[GetIndex(1, a)];
            float bw = data[GetIndex(2, a)];
            float bh = data[GetIndex(3, a)];

            // YOLO outputs pixel-space coords; normalise and reverse the letterboxing
            float nx = (cx / inputWidth  - letterboxOffset.x) / letterboxScale.x;
            float ny = (cy / inputHeight - letterboxOffset.y) / letterboxScale.y;
            float nw = (bw / inputWidth)  / letterboxScale.x;
            float nh = (bh / inputHeight) / letterboxScale.y;

            var kp     = new Vector2[numKp];
            var kpConf = new float[numKp];
            for (int k = 0; k < numKp; k++)
            {
                float kx = (data[GetIndex(5 + k * 3,     a)] / inputWidth  - letterboxOffset.x) / letterboxScale.x;
                float ky = (data[GetIndex(5 + k * 3 + 1, a)] / inputHeight - letterboxOffset.y) / letterboxScale.y;
                float kc = data[GetIndex(5 + k * 3 + 2, a)];
                kp[k]     = new Vector2(kx, ky);
                kpConf[k] = kc;
            }

            candidates.Add(new HandDetection
            {
                box        = new Rect(nx - nw / 2f, ny - nh / 2f, nw, nh),
                confidence = conf,
                keypoints  = kp,
                kpConf     = kpConf,
            });
        }

        detectedHands = NMS(candidates);
        SmoothHands();
    }

    // Handles regression landmark models that output (1, 63) = 21 keypoints × (x, y, z)
    // Coordinates are assumed to be in pixel space [0, inputWidth/inputHeight].
    void DecodeLandmarkRegression(TensorShape shape, float[] data)
    {
        int numKp = data.Length / 3;  // 63 / 3 = 21
        var kp     = new Vector2[numKp];
        var kpConf = new float[numKp];

        for (int i = 0; i < numKp; i++)
        {
            float x = (data[i * 3 + 0] / inputWidth  - letterboxOffset.x) / letterboxScale.x;
            float y = (data[i * 3 + 1] / inputHeight - letterboxOffset.y) / letterboxScale.y;
            kp[i]     = new Vector2(x, y);
            kpConf[i] = 1f;   // regression models don't produce per-keypoint confidence
        }

        // Compute bounding box from keypoint extents
        float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
        foreach (var p in kp)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        detectedHands = new List<HandDetection>
        {
            new HandDetection
            {
                box        = new Rect(minX, minY, maxX - minX, maxY - minY),
                confidence = 1f,
                keypoints  = kp,
                kpConf     = kpConf,
            }
        };
        SmoothHands();
    }

    // ─── Temporal Smoothing ─────────────────────────────────────────────
    void SmoothHands()
    {
        if (smoothingFactor <= 0f || smoothedHands.Count == 0)
        {
            // First frame or smoothing disabled: just copy
            smoothedHands = new List<HandDetection>(detectedHands);
            return;
        }

        // For each detected hand, find the closest smoothed hand and lerp
        var newSmoothed = new List<HandDetection>();
        var usedIndices = new HashSet<int>();

        foreach (var det in detectedHands)
        {
            int bestIdx = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < smoothedHands.Count; i++)
            {
                if (usedIndices.Contains(i)) continue;
                float dist = Vector2.Distance(det.box.center, smoothedHands[i].box.center);
                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
            }

            if (bestIdx >= 0 && bestDist < 0.3f) // matched to a previous hand
            {
                usedIndices.Add(bestIdx);
                var prev = smoothedHands[bestIdx];
                float t = smoothingFactor;

                // Lerp bounding box
                var box = new Rect(
                    Mathf.Lerp(det.box.x, prev.box.x, t),
                    Mathf.Lerp(det.box.y, prev.box.y, t),
                    Mathf.Lerp(det.box.width, prev.box.width, t),
                    Mathf.Lerp(det.box.height, prev.box.height, t)
                );

                // Lerp keypoints
                int count = Mathf.Min(det.keypoints.Length, prev.keypoints.Length);
                var kp = new Vector2[det.keypoints.Length];
                var kc = new float[det.kpConf.Length];
                for (int k = 0; k < count; k++)
                {
                    kp[k] = Vector2.Lerp(det.keypoints[k], prev.keypoints[k], t);
                    kc[k] = Mathf.Lerp(det.kpConf[k], prev.kpConf[k], t);
                }
                for (int k = count; k < det.keypoints.Length; k++)
                {
                    kp[k] = det.keypoints[k];
                    kc[k] = det.kpConf[k];
                }

                newSmoothed.Add(new HandDetection
                {
                    box = box,
                    confidence = det.confidence,
                    keypoints = kp,
                    kpConf = kc,
                });
            }
            else
            {
                // New hand, no previous match
                newSmoothed.Add(det);
            }
        }

        smoothedHands = newSmoothed;
    }

    List<HandDetection> NMS(List<HandDetection> boxes)
    {
        boxes.Sort((a, b) => b.confidence.CompareTo(a.confidence));
        var kept = new List<HandDetection>();
        foreach (var box in boxes)
        {
            bool suppress = false;
            foreach (var k in kept)
            {
                if (IOU(box.box, k.box) > iouThreshold) { suppress = true; break; }
            }
            if (!suppress) kept.Add(box);
        }
        return kept;
    }

    float IOU(Rect a, Rect b)
    {
        float ix = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float iy = Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        float inter = ix * iy;
        float areaA = a.width * a.height;
        float areaB = b.width * b.height;
        
        float iou = inter / (areaA + areaB - inter);
        float iom = inter / Mathf.Min(areaA, areaB); // Intersection over Minimum (fixes nested boxes)
        
        // Suppress if standard IOU is high, OR if one box is almost entirely inside the other
        return Mathf.Max(iou, iom);
    }

    // ─── Drawing ─────────────────────────────────────────────────────────

    void OnGUI()
    {
        // Draw smoothed hands (not raw detections) for stable visuals
        var handsToDraw = smoothedHands != null && smoothedHands.Count > 0 ? smoothedHands : detectedHands;
        if (handsToDraw == null || handsToDraw.Count == 0 || displayImage == null) return;

        Rect imgRect = GetDisplayRect();

        foreach (var hand in handsToDraw)
        {
            // ── Bounding box
            DrawRectGUI(
                imgRect.x + hand.box.x      * imgRect.width,
                imgRect.y + hand.box.y      * imgRect.height,
                hand.box.width  * imgRect.width,
                hand.box.height * imgRect.height,
                boxColor
            );

            // Map keypoints to screen
            var pts = new Vector2[hand.keypoints.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(
                    imgRect.x + hand.keypoints[i].x * imgRect.width,
                    imgRect.y + hand.keypoints[i].y * imgRect.height
                );
            }

            // ── Skeleton lines (always draw; dim occluded bones)
            for (int c = 0; c < SKELETON_CONNECTIONS.Length; c++)
            {
                int a = SKELETON_CONNECTIONS[c][0];
                int b = SKELETON_CONNECTIONS[c][1];

                // Safety check: ensure the model actually output enough keypoints to form a hand skeleton
                if (a >= pts.Length || b >= pts.Length) continue;

                Color col = c < CONNECTION_FINGER.Length
                    ? FINGER_COLORS[CONNECTION_FINGER[c]]
                    : skeletonColor;

                // Fade the bone if either endpoint is low-confidence (occluded/folded)
                bool aVisible = hand.kpConf[a] >= keypointThreshold;
                bool bVisible = hand.kpConf[b] >= keypointThreshold;
                col.a = (aVisible && bVisible) ? 1f : 0.35f;

                DrawLineGUI(pts[a], pts[b], col, lineWidth);
            }

            // ── Keypoint dots (always draw; dim occluded joints)
            for (int i = 0; i < pts.Length; i++)
            {
                float r = keypointRadius;
                Color dc = keypointColor;
                dc.a = hand.kpConf[i] >= keypointThreshold ? 1f : 0.25f;
                GUI.color = dc;
                GUI.DrawTexture(new Rect(pts[i].x - r, pts[i].y - r, r * 2, r * 2), Texture2D.whiteTexture);
            }
        }
    }

    void DrawRectGUI(float x, float y, float w, float h, Color color)
    {
        float t = lineWidth;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, w, t), Texture2D.whiteTexture); // Top
        GUI.DrawTexture(new Rect(x, y + h - t, w, t), Texture2D.whiteTexture); // Bottom
        GUI.DrawTexture(new Rect(x, y, t, h), Texture2D.whiteTexture); // Left
        GUI.DrawTexture(new Rect(x + w - t, y, t, h), Texture2D.whiteTexture); // Right
    }

    void DrawLineGUI(Vector2 from, Vector2 to, Color color, float thickness)
    {
        GUI.color = color;
        Vector2 dir = (to - from);
        float len = dir.magnitude;
        if (len < 0.001f) return;
        dir /= len;
        int steps = Mathf.CeilToInt(len);
        float step = len / steps;
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = from + dir * (i * step);
            GUI.DrawTexture(new Rect(p.x - thickness * 0.5f, p.y - thickness * 0.5f, thickness, thickness), Texture2D.whiteTexture);
        }
    }

    // Convert the RawImage RectTransform into a screen-space Rect for GUI drawing.
    Rect GetDisplayRect()
    {
        var corners = new Vector3[4];
        displayImage.rectTransform.GetWorldCorners(corners);

        Canvas canvas = displayImage.canvas;
        Camera cam    = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        ? null : Camera.main;

        Vector3 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector3 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        // GUI matrix: Y=0 at TOP. ScreenSpace Y=0 is bottom.
        return new Rect(bl.x, Screen.height - tr.y, tr.x - bl.x, tr.y - bl.y);
    }

    // ─── Cleanup ─────────────────────────────────────────────────────────

    void OnDisable()
    {
        webCamTexture?.Stop();
        worker?.Dispose();
        inputTensor?.Dispose();
        if (letterboxTexture != null) letterboxTexture.Release();
    }
}
