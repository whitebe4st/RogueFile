using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Catch The Page" AR Minigame — Timing / Reflex.
/// 5 papers fly across the screen one at a time. Player must close their fist
/// when each paper passes through the catch zone. Speed increases each catch.
/// Papers: Memo → Report → Diary → Receipt → Restraining Order (always last).
/// </summary>
public class CatchThePageManager : MonoBehaviour
{
    // ─── Paper Types ─────────────────────────────────────────────────

    [System.Serializable]
    public class PaperData
    {
        public string paperName = "Document";
        public Sprite sprite;
        [TextArea(1, 2)]
        public string grimmLine = "";
    }

    [Header("References")]
    [SerializeField] private HandDetection handDetection;
    [SerializeField] private RectTransform paperRect;
    [SerializeField] private Image paperImage;              // To swap sprites
    [SerializeField] private RectTransform catchZoneRect;
    [SerializeField] private Image catchZoneImage;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI catchCountText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI grimmText;
    [SerializeField] private TextMeshProUGUI paperNameText;  // Shows current paper name

    [Header("Papers (order matters — last = Restraining Order)")]
    [SerializeField] private PaperData[] papers = new PaperData[]
    {
        new PaperData { paperName = "Memo",              grimmLine = "\"A memo... might be useful.\"" },
        new PaperData { paperName = "Report",            grimmLine = "\"An incident report... interesting.\"" },
        new PaperData { paperName = "Diary Page",        grimmLine = "\"Someone's diary... let's see.\"" },
        new PaperData { paperName = "Receipt",           grimmLine = "\"A receipt? Could be a lead.\"" },
        new PaperData { paperName = "Restraining Order", grimmLine = "\"This is it! The restraining order!\"" },
    };

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 20f;
    [SerializeField] private float baseSpeed = 300f;
    [SerializeField] private float speedMultiplier = 1.25f;

    [Header("Paper Movement")]
    [SerializeField] private float arcAmplitude = 200f;
    [SerializeField] private float arcFrequency = 1.5f;

    // ─── State ───────────────────────────────────────────────────────

    enum GameState { Intro, Playing, Win, TimeUp }
    private GameState state = GameState.Intro;

    private float timeRemaining;
    private int catchCount;
    private int currentPaperIndex;
    private float currentSpeed;

    // Paper movement
    private float paperT;
    private float paperDirection = 1f;
    private Vector2 canvasSize;
    private float arcYOffset;

    // Fist tracking
    private bool isFistClosed;
    private bool wasFistClosed;
    private float catchCooldown;
    private Vector2 handTargetPos;    // Raw target from landmarks
    private Vector2 handScreenPos;    // Smoothed position applied to UI
    private bool hasHandPos;

    [Header("Hand Tracking Feel")]
    [SerializeField, Range(5f, 30f)] private float handSmoothing = 20f;
    [SerializeField] private bool handFlipX = false;
    [SerializeField] private bool handFlipY = false;

    // ─── Properties ──────────────────────────────────────────────────

    int TotalPapers => papers != null ? papers.Length : 5;
    PaperData CurrentPaper => papers != null && currentPaperIndex < papers.Length ? papers[currentPaperIndex] : null;
    bool IsLastPaper => currentPaperIndex >= TotalPapers - 1;

    // ─── Lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarks;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasSize = canvas.GetComponent<RectTransform>().rect.size;
        else
            canvasSize = new Vector2(1920, 1080);

        StartIntro();
    }

    void OnDestroy()
    {
        if (handDetection != null)
            handDetection.OnLandmarksUpdated -= OnLandmarks;
    }

    void Update()
    {
        switch (state)
        {
            case GameState.Intro: UpdateIntro(); break;
            case GameState.Playing: UpdatePlaying(); break;
        }
    }

    // ─── Hand Input ──────────────────────────────────────────────────

    private void OnLandmarks(Vector3[] lm)
    {
        isFistClosed = SentisGestureDetector.IsFistClosed(lm);

        // Track palm center (average of wrist + middle finger MCP) for catch zone positioning
        if (lm != null && lm.Length >= 21)
        {
            // Use landmark 9 (middle finger MCP) as palm center
            Vector2 palmNorm = new Vector2(lm[9].x, lm[9].y);
            // Convert normalized (0-1) to canvas coords
            float x = (palmNorm.x - 0.5f) * canvasSize.x;
            float y = (palmNorm.y - 0.5f) * canvasSize.y;

            if (handFlipX) x = -x;
            if (handFlipY) y = -y;

            handTargetPos = new Vector2(x, y);
            hasHandPos = true;
        }
    }

    // ─── Intro ───────────────────────────────────────────────────────

    private float introTimer;

    void StartIntro()
    {
        state = GameState.Intro;
        introTimer = 2.5f;

        if (grimmText != null) grimmText.text = "\"Papers are flying everywhere... grab them!\"";
        if (statusText != null) statusText.text = "CATCH THE PAGE";
        if (timerText != null) timerText.text = "";
        if (catchCountText != null) catchCountText.text = "";
        if (paperNameText != null) paperNameText.text = "";
        if (paperRect != null) paperRect.gameObject.SetActive(false);
    }

    void UpdateIntro()
    {
        introTimer -= Time.deltaTime;
        if (introTimer <= 0f) StartGame();
    }

    // ─── Playing ─────────────────────────────────────────────────────

    void StartGame()
    {
        state = GameState.Playing;
        timeRemaining = gameDuration;
        catchCount = 0;
        currentPaperIndex = 0;
        currentSpeed = baseSpeed;

        if (grimmText != null) grimmText.text = "";
        if (statusText != null) statusText.text = "";
        if (paperRect != null) paperRect.gameObject.SetActive(true);

        SetupCurrentPaper();
        ResetPaperArc();
        UpdateUI();
    }

    void SetupCurrentPaper()
    {
        var paper = CurrentPaper;
        if (paper == null) return;

        // Swap sprite if assigned
        if (paperImage != null && paper.sprite != null)
            paperImage.sprite = paper.sprite;

        // Show paper name
        if (paperNameText != null)
            paperNameText.text = paper.paperName;
    }

    void UpdatePlaying()
    {
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame(false);
            return;
        }

        MovePaper();

        if (catchCooldown > 0f) catchCooldown -= Time.deltaTime;

        bool fistJustClosed = isFistClosed && !wasFistClosed;
        wasFistClosed = isFistClosed;

        if (fistJustClosed && catchCooldown <= 0f)
            TryToCatch();

        // Smoothly move catch zone to follow hand
        if (hasHandPos && catchZoneRect != null)
        {
            handScreenPos = Vector2.Lerp(handScreenPos, handTargetPos, handSmoothing * Time.deltaTime);
            catchZoneRect.anchoredPosition = handScreenPos;
        }

        UpdateCatchZoneGlow();
        UpdateUI();
    }

    // ─── Paper Movement ──────────────────────────────────────────────

    void ResetPaperArc()
    {
        paperT = 0f;
        paperDirection = Random.value > 0.5f ? 1f : -1f;
        arcYOffset = Random.Range(-canvasSize.y * 0.15f, canvasSize.y * 0.15f);
        arcAmplitude = Random.Range(100f, 250f);
        arcFrequency = Random.Range(1.2f, 2.5f);
    }

    void MovePaper()
    {
        float speed01 = currentSpeed / canvasSize.x;
        paperT += speed01 * Time.deltaTime;

        if (paperT >= 1f)
        {
            ResetPaperArc();
            return;
        }

        float xNorm = paperDirection > 0 ? paperT : (1f - paperT);
        float x = Mathf.Lerp(-canvasSize.x * 0.45f, canvasSize.x * 0.45f, xNorm);
        float y = arcYOffset + arcAmplitude * Mathf.Sin(paperT * Mathf.PI * arcFrequency * 2f);
        float rot = 15f * Mathf.Sin(paperT * Mathf.PI * 3f);

        if (paperRect != null)
        {
            paperRect.anchoredPosition = new Vector2(x, y);
            paperRect.localRotation = Quaternion.Euler(0, 0, rot);
        }
    }

    // ─── Catch Logic ─────────────────────────────────────────────────

    void TryToCatch()
    {
        if (paperRect == null || catchZoneRect == null) return;

        Rect zoneRect = GetWorldRect(catchZoneRect);
        Vector2 paperWorld = paperRect.position;

        if (zoneRect.Contains(paperWorld))
        {
            catchCount++;
            catchCooldown = 0.3f;
            currentSpeed *= speedMultiplier;

            // Show Grimm's reaction line for this paper
            var paper = CurrentPaper;
            if (paper != null && grimmText != null)
                grimmText.text = paper.grimmLine;

            // Flash green
            if (catchZoneImage != null) catchZoneImage.color = Color.green;

            // Status text
            if (statusText != null)
            {
                string catchMsg = IsLastPaper ? "EVIDENCE SECURED!" : $"Caught: {paper?.paperName ?? "???"}!";
                statusText.text = catchMsg;
                CancelInvoke(nameof(ClearStatus));
                Invoke(nameof(ClearStatus), 0.5f);
            }

            // Check win
            if (catchCount >= TotalPapers)
            {
                EndGame(true);
                return;
            }

            // Advance to next paper
            currentPaperIndex++;
            SetupCurrentPaper();
            ResetPaperArc();
        }
        else
        {
            // Missed
            if (catchZoneImage != null) catchZoneImage.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        }
    }

    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
    }

    // ─── End Game ────────────────────────────────────────────────────

    void EndGame(bool won)
    {
        state = won ? GameState.Win : GameState.TimeUp;
        if (paperRect != null) paperRect.gameObject.SetActive(false);

        if (won)
        {
            if (statusText != null) { statusText.text = "ALL EVIDENCE COLLECTED!"; statusText.color = Color.green; }
            if (grimmText != null) grimmText.text = "\"That's everything. Let's get out of here.\"";
            if (paperNameText != null) paperNameText.text = "✓ Complete";
        }
        else
        {
            if (statusText != null) { statusText.text = "TIME'S UP!"; statusText.color = Color.red; }
            if (grimmText != null) grimmText.text = "\"Damn... the wind took them.\"";
            if (paperNameText != null) paperNameText.text = "";
        }
    }

    // ─── UI Helpers ──────────────────────────────────────────────────

    void UpdateUI()
    {
        if (timerText != null) timerText.text = $"{timeRemaining:F1}s";
        if (catchCountText != null) catchCountText.text = $"{catchCount} / {TotalPapers}";
    }

    void UpdateCatchZoneGlow()
    {
        if (catchZoneImage == null) return;
        float pulse = 0.3f + 0.2f * Mathf.Sin(Time.time * 4f);
        catchZoneImage.color = isFistClosed
            ? new Color(1f, 0.9f, 0.2f, pulse + 0.2f)
            : new Color(1f, 1f, 1f, pulse);
    }

    void ClearStatus() { if (statusText != null) statusText.text = ""; }
}
