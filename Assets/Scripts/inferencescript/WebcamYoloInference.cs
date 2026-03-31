using UnityEngine;
using Unity.InferenceEngine;
using UnityEngine.UI;

public class WebcamYoloInference : MonoBehaviour
{
    public ModelAsset modelAsset;     // Your YOLO11 .sentis or .onnx file
    public RawImage displayImage;     // UI element to show the webcam feed
    
    [Header("Model Input Settings")]
    [Tooltip("Width the model was exported with (e.g. 224 or 640). Check your ONNX model's input shape.")]
    public int imageWidth = 224;
    [Tooltip("Height the model was exported with (e.g. 224 or 640).")]
    public int imageHeight = 224;
    [Tooltip("True = NHWC layout (1,H,W,3) - common for TF/Keras exports.\nFalse = NCHW layout (1,3,H,W) - standard YOLO/PyTorch exports.")]
    public bool isNHWC = true;

    [Tooltip("Run inference only every N frames to reduce CPU/GPU load. 1 = every frame.")]
    public int inferenceFrameInterval = 2;

    private WebCamTexture webCamTexture;
    private Model runtimeModel;
    private Worker worker;
    private Tensor<float> inputTensor;
    private float[] _tensorData; // reusable CPU buffer for manual tensor fill

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("No webcam found!");
            return;
        }

        // Use the first available camera
        webCamTexture = new WebCamTexture(devices[0].name, 640, 480, 30);
        displayImage.texture = webCamTexture;
        webCamTexture.Play();

        runtimeModel = ModelLoader.Load(modelAsset);
        // CPU backend: no GPU sync stall; fast for small models like 224x224 hand landmark
        worker = new Worker(runtimeModel, BackendType.CPU);

        // Create tensor with the shape the model expects.
        // Set imageWidth, imageHeight, and isNHWC in the Inspector to match your ONNX model.
        if (isNHWC)
        {
            // NHWC: (1, H, W, 3) — TF/Keras-style
            inputTensor = new Tensor<float>(new TensorShape(1, imageHeight, imageWidth, 3));
            Debug.Log($"[Inference] Tensor shape: NHWC (1, {imageHeight}, {imageWidth}, 3)");
        }
        else
        {
            // NCHW: (1, 3, H, W) — PyTorch/YOLO-style
            inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
            Debug.Log($"[Inference] Tensor shape: NCHW (1, 3, {imageHeight}, {imageWidth})");
        }
    }

    private int _frameCount = 0;

    void Update()
    {
        if (webCamTexture != null && webCamTexture.didUpdateThisFrame)
        {
            _frameCount++;
            if (_frameCount % Mathf.Max(1, inferenceFrameInterval) == 0)
                RunInference();
        }
    }

    // Reusable Texture2D for reading pixels — allocated once and reused each frame.
    private Texture2D _readTex;

    void RunInference()
    {
        // TextureConverter.ToTensor only supports NCHW tensors (it reads dim[1] as channels).
        // For NHWC models we must fill the tensor manually from raw pixel data instead.

        // 1. Blit webcam → RenderTexture at model resolution
        var rt = RenderTexture.GetTemporary(imageWidth, imageHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(webCamTexture, rt);

        // 2. Read pixels from RenderTexture into a CPU Texture2D
        if (_readTex == null)
            _readTex = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);

        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;
        _readTex.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0, false);
        _readTex.Apply();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        // 3. Fill inputTensor manually using Color32 (cheaper than Color)
        Color32[] pixels = _readTex.GetPixels32();
        if (_tensorData == null) _tensorData = new float[inputTensor.shape.length];

        const float inv255 = 1f / 255f;
        if (isNHWC)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                int srcRow = (imageHeight - 1 - y);
                for (int x = 0; x < imageWidth; x++)
                {
                    Color32 c = pixels[srcRow * imageWidth + x];
                    int baseIdx = (y * imageWidth + x) * 3;
                    _tensorData[baseIdx + 0] = c.r * inv255;
                    _tensorData[baseIdx + 1] = c.g * inv255;
                    _tensorData[baseIdx + 2] = c.b * inv255;
                }
            }
        }
        else
        {
            int hw = imageHeight * imageWidth;
            for (int y = 0; y < imageHeight; y++)
            {
                int srcRow = (imageHeight - 1 - y);
                for (int x = 0; x < imageWidth; x++)
                {
                    Color32 c = pixels[srcRow * imageWidth + x];
                    int px = y * imageWidth + x;
                    _tensorData[0 * hw + px] = c.r * inv255;
                    _tensorData[1 * hw + px] = c.g * inv255;
                    _tensorData[2 * hw + px] = c.b * inv255;
                }
            }
        }

        // Upload to tensor and run inference (instant on CPU backend — no GPU sync)
        inputTensor.Upload(_tensorData);
        worker.Schedule(inputTensor);

        var outputTensor = worker.PeekOutput() as Tensor<float>;
        float[] results = outputTensor.DownloadToArray();

        DecodeHandLandmarks(results);
    }

    // ── Hand Landmark Data ────────────────────────────────────────────────────

    struct Keypoint { public float x, y, z; }
    private Keypoint[] _keypoints = new Keypoint[21];
    private bool _hasKeypoints = false;

    [Header("Hand Landmark Settings")]
    [Tooltip("False = pixel coords (0–imageWidth/Height). True = [0,1] normalized.\nMax output ~167 at 224px → use False.")]
    public bool outputIsNormalized = false;  // pixel coords confirmed by observed max ~167

    // MediaPipe hand skeleton connections (21 landmarks, 0-indexed)
    static readonly (int, int)[] BoneConnections = new (int, int)[]
    {
        // Wrist → Palm
        (0,1),(1,2),(2,3),(3,4),           // Thumb
        (0,5),(5,6),(6,7),(7,8),           // Index
        (0,9),(9,10),(10,11),(11,12),      // Middle
        (0,13),(13,14),(14,15),(15,16),    // Ring
        (0,17),(17,18),(18,19),(19,20),    // Pinky
        (5,9),(9,13),(13,17),              // Palm cross-connections
    };

    void DecodeHandLandmarks(float[] results)
    {
        // Output shape (1, 63) — 21 keypoints × 3 values each
        for (int i = 0; i < 21; i++)
        {
            _keypoints[i] = new Keypoint
            {
                x = results[i * 3 + 0],
                y = results[i * 3 + 1],
                z = results[i * 3 + 2],
            };
        }
        _hasKeypoints = true;
    }

    void OnGUI()
    {
        if (displayImage == null) return;

        if (!_hasKeypoints) return;

        Rect imageRect = GetScreenRect(displayImage.rectTransform);

        // Convert a keypoint's model coords to screen coords
        Vector2 ToScreen(Keypoint kp)
        {
            float nx = outputIsNormalized ? kp.x : kp.x / imageWidth;
            float ny = outputIsNormalized ? kp.y : kp.y / imageHeight;
            return new Vector2(
                imageRect.x + nx * imageRect.width,
                imageRect.y + ny * imageRect.height   // y grows downward, already matches GUI
            );
        }

        // Draw bones
        GUI.color = Color.green;
        foreach (var (a, b) in BoneConnections)
        {
            Vector2 pa = ToScreen(_keypoints[a]);
            Vector2 pb = ToScreen(_keypoints[b]);
            DrawLine(pa, pb, Color.green, 2f);
        }

        // Draw joint dots
        foreach (var kp in _keypoints)
        {
            Vector2 p = ToScreen(kp);
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(p.x - 4, p.y - 4, 8, 8), Texture2D.whiteTexture);
        }
    }

    // Simple GL-free line drawn as a series of tiny boxes
    void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
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

    Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // Use displayImage.canvas (not GetComponentInParent on this script's GameObject)
        Canvas canvas = displayImage.canvas;
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;
        
        Vector3 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector3 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        
        return new Rect(bl.x, Screen.height - tr.y, tr.x - bl.x, tr.y - bl.y);
    }

    private void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();

        worker?.Dispose();
        inputTensor?.Dispose();

        if (_readTex != null)
        {
            Destroy(_readTex);
            _readTex = null;
        }
    }
}
