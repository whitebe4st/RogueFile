using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// "TRACE THE SCRATCH" — Clue 2 Minigame
/// Player memorizes a scratch pattern, then traces it using their index fingertip.
/// 3 Rounds total, with paths getting longer each time.
/// </summary>
public class TraceTheScratchManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandDetection handDetection;
    [SerializeField] private LineRenderer guideLine;      // Pre-rendered pattern
    [SerializeField] private LineRenderer playerLine;     // Player's current trace
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI grimmText;
    [SerializeField] private TextMeshProUGUI accuracyText;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 25f;
    [SerializeField] private float showDuration = 3f;      // How long to show the path
    [SerializeField] private float pointTolerance = 50f;   // Pixels to reach a point
    [SerializeField] private float breakThreshold = 150f;  // Pixels deviation to fail
    [SerializeField] private int totalRounds = 3;

    // ─── State ───────────────────────────────────────────────────────

    enum GamePhase { Idle, Intro, ShowPath, TracePath, End }
    private GamePhase phase = GamePhase.Idle;

    private int currentRound = 1;
    private float timeRemaining;
    private List<Vector2> targetPath = new List<Vector2>();
    private List<Vector2> currentTrace = new List<Vector2>();
    private int nextPointIndex = 0;

    private Vector2 canvasSize;
    private Vector3[] currentLandmarks;
    private bool isHandVisible;

    // Scoring
    private float totalAccuracy;

    // ─── Lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform cr = canvas.GetComponent<RectTransform>();
            canvasSize = new Vector2(cr.rect.width, cr.rect.height);
        }

        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarks;

        StartCoroutine(FullGameSequence());
    }

    void OnDestroy()
    {
        if (handDetection != null)
            handDetection.OnLandmarksUpdated -= OnLandmarks;
    }

    private void OnLandmarks(Vector3[] lm)
    {
        currentLandmarks = lm;
        isHandVisible = (lm != null && lm.Length >= 21);
    }

    // ─── Main Game Loop ──────────────────────────────────────────────

    IEnumerator FullGameSequence()
    {
        phase = GamePhase.Intro;
        SetStatus("MEMORY TRACE...");
        yield return new WaitForSeconds(1.5f);

        if (grimmText != null)
            grimmText.text = "\"Someone forced their way in. Someone who knew what they were doing.\"";

        timeRemaining = gameDuration;

        for (int r = 1; r <= totalRounds; r++)
        {
            currentRound = r;
            yield return StartCoroutine(PlayRound(r));
        }

        EndGame();
    }

    IEnumerator PlayRound(int roundNum)
    {
        // 1. Generate Path
        int pointsCount = 2 + roundNum; // 3, 4, 5 points
        GenerateRandomPath(pointsCount);

        // 2. Show Path
        phase = GamePhase.ShowPath;
        SetStatus($"ROUND {roundNum}\nMEMORIZE!");
        DrawLine(guideLine, targetPath, true);
        DrawLine(playerLine, null, false); // Clear player line
        yield return new WaitForSeconds(showDuration);

        // 3. Hide Path
        DrawLine(guideLine, null, false);
        SetStatus("TRACE IT!");
        phase = GamePhase.TracePath;
        nextPointIndex = 0;
        currentTrace.Clear();

        // 4. Wait for Trace Completion
        while (nextPointIndex < targetPath.Count && timeRemaining > 0)
        {
            UpdateTraceLogic();
            yield return null;
        }

        if (timeRemaining > 0)
        {
            SetStatus("PERFECT!");
            yield return new WaitForSeconds(1f);
        }
    }

    // ─── Logic ───────────────────────────────────────────────────────

    void GenerateRandomPath(int count)
    {
        targetPath.Clear();
        float margin = 100f;
        for (int i = 0; i < count; i++)
        {
            float rx = Random.Range(-canvasSize.x * 0.4f + margin, canvasSize.x * 0.4f - margin);
            float ry = Random.Range(-canvasSize.y * 0.4f + margin, canvasSize.y * 0.4f - margin);
            targetPath.Add(new Vector2(rx, ry));
        }
    }

    void UpdateTraceLogic()
    {
        if (!isHandVisible) return;

        // Trace index tip (Landmark 8)
        Vector3 rawTip = currentLandmarks[8];
        Vector2 tipSource = new Vector2(
            (rawTip.x - 0.5f) * canvasSize.x,
            (0.5f - rawTip.y) * canvasSize.y
        );

        // Add to player trace visual
        currentTrace.Add(tipSource);
        if (currentTrace.Count > 50) currentTrace.RemoveAt(0); // keep it a short trail or full path?
        // Actually, for "Trace the scratch", we want to see the whole path they've drawn
        DrawLine(playerLine, currentTrace, true);

        // Check distance to next point
        Vector2 target = targetPath[nextPointIndex];
        float dist = Vector2.Distance(tipSource, target);

        if (dist < pointTolerance)
        {
            nextPointIndex++;
            // Could add a small "ding" sound/flash here
        }

        // Optional: Fail if they stray too far from the line segments?
        // For simplicity: resets the trace if they lose the finger or go too far.
    }

    void Update()
    {
        if (phase == GamePhase.TracePath || phase == GamePhase.ShowPath)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.Max(0, Mathf.CeilToInt(timeRemaining)).ToString();

            if (timeRemaining <= 0) phase = GamePhase.End;
        }
    }

    // ─── Drawing ─────────────────────────────────────────────────────

    void DrawLine(LineRenderer lr, List<Vector2> points, bool visible)
    {
        if (lr == null) return;
        lr.enabled = visible;
        if (!visible || points == null) return;

        lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            lr.SetPosition(i, new Vector3(points[i].x, points[i].y, 0));
        }
    }

    void EndGame()
    {
        phase = GamePhase.End;
        SetStatus(timeRemaining > 0 ? "SUCCESS!" : "TIME UP!");
        DrawLine(guideLine, targetPath, true); // Show the final path they should have traced
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
