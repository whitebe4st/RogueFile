using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// "SORT THE HATE" — Clue 3 Minigame
/// Falling printouts of 3 types: Julian fan attacks, Aria fan attacks, Noise.
/// Player swipes Left (Julian), Right (Aria), or flicks Down (discard Noise).
/// 30-second timer, speed ramps every card. Combo multiplier on streak
/// </summary>
public class SortTheHateManager : MonoBehaviour
{
    // ─── Printout Data ───────────────────────────────────────────────

    public enum PrintoutCategory { JulianFan, AriaFan, Noise }

    [System.Serializable]
    public class PrintoutData
    {
        public string label = "Tweet";
        public Sprite sprite;
        public PrintoutCategory category;
        [TextArea(1, 3)]
        public string previewText = "";  // optional text on card
    }

    // ─── Inspector Fields ────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private HandDetection handDetection;

    [Header("Printout Cards")]
    [SerializeField] private PrintoutData[] printouts;

    [Header("Falling Card UI")]
    [SerializeField] private RectTransform cardRect;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardText;

    [Header("Score & Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI grimmText;

    [Header("Side Zone Indicators")]
    [SerializeField] private Image leftZoneFlash;   // Julian side
    [SerializeField] private Image rightZoneFlash;  // Aria side
    [SerializeField] private Image bottomZoneFlash; // Discard side

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 30f;
    [SerializeField] private float baseFallDuration = 2.0f;   // time to fall across screen
    [SerializeField] private float speedMultiplier = 0.92f;   // each card falls faster (multiplied)

    [Header("Swipe Sensitivity")]
    [SerializeField] private float swipeXThreshold = 0.5f;
    [SerializeField] private float swipeYThreshold = 0.5f;

    // ─── State ───────────────────────────────────────────────────────

    enum GameState { Idle, Intro, Playing, End }
    private GameState state = GameState.Idle;

    private float timeRemaining;
    private int score;
    private int combo;
    private float currentFallDuration;

    // Current card
    private PrintoutData currentCard;
    private float cardFallT;             // 0 = top, 1 = bottom
    private bool cardActive;
    private float canvasHeight;
    private float canvasWidth;

    // Hand tracking
    private Vector3[] previousLandmarks;
    private Vector3[] currentLandmarks;
    private float landmarkDeltaTime;
    private float lastLandmarkTime;

    // Swipe cooldown — prevent double-firing on same gesture
    private float swipeCooldown;
    private const float SWIPE_COOLDOWN_DURATION = 0.6f;

    // Zone flash state
    private float leftFlashTimer;
    private float rightFlashTimer;
    private float bottomFlashTimer;
    private const float FLASH_DURATION = 0.3f;

    // ─── Lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        // Get canvas size
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasWidth  = canvasRect.rect.width;
            canvasHeight = canvasRect.rect.height;
        }

        // Subscribe to hand tracking
        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarks;

        // Start game immediately
        StartCoroutine(IntroSequence());
    }

    void OnDestroy()
    {
        if (handDetection != null)
            handDetection.OnLandmarksUpdated -= OnLandmarks;
    }

    // ─── Hand Input ──────────────────────────────────────────────────

    private void OnLandmarks(Vector3[] lm)
    {
        float now = Time.time;
        landmarkDeltaTime = now - lastLandmarkTime;
        lastLandmarkTime  = now;

        previousLandmarks = currentLandmarks;
        currentLandmarks  = lm;
    }

    // ─── Update ──────────────────────────────────────────────────────

    void Update()
    {
        if (state != GameState.Playing) return;

        // Timer
        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();
        if (timeRemaining <= 0f)
        {
            EndGame();
            return;
        }

        // Swipe detection (continuous)
        if (swipeCooldown > 0f) swipeCooldown -= Time.deltaTime;
        else DetectAndHandleSwipe();

        // Card falling
        if (cardActive) UpdateCardFall();

        // Zone flash timers
        UpdateZoneFlashes();
    }

    // ─── Intro ───────────────────────────────────────────────────────

    IEnumerator IntroSequence()
    {
        state = GameState.Intro;
        SetStatus("Get ready...");
        yield return new WaitForSeconds(1f);
        SetStatus("SORT THE HATE!");
        yield return new WaitForSeconds(1f);
        SetStatus("");

        // Show Grimm line
        if (grimmText != null)
            grimmText.text = "\"These people tore each other apart over a TV appearance.\"";

        state = GameState.Playing;
        timeRemaining = gameDuration;
        currentFallDuration = baseFallDuration;
        score = 0;
        combo = 0;
        SpawnCard();
    }

    // ─── Card Management ─────────────────────────────────────────────

    void SpawnCard()
    {
        if (printouts == null || printouts.Length == 0) return;

        // Pick a weighted random printout (roughly even distribution)
        currentCard = printouts[Random.Range(0, printouts.Length)];

        // Reset fall
        cardFallT = 0f;
        cardActive = true;

        // Random X position in upper area
        float spawnX = Random.Range(-canvasWidth * 0.35f, canvasWidth * 0.35f);

        if (cardRect != null)
        {
            cardRect.anchoredPosition = new Vector2(spawnX, canvasHeight * 0.5f);
            cardRect.gameObject.SetActive(true);
        }

        // Set visuals
        if (cardImage != null && currentCard.sprite != null)
            cardImage.sprite = currentCard.sprite;
        if (cardText != null)
            cardText.text = currentCard.previewText;

        SetScore();
    }

    void UpdateCardFall()
    {
        cardFallT += Time.deltaTime / currentFallDuration;

        float topY    =  canvasHeight * 0.5f;
        float bottomY = -canvasHeight * 0.5f;

        if (cardRect != null)
        {
            Vector2 pos = cardRect.anchoredPosition;
            pos.y = Mathf.Lerp(topY, bottomY, cardFallT);
            cardRect.anchoredPosition = pos;
        }

        // Missed — card hit the floor
        if (cardFallT >= 1f)
        {
            HandleMiss();
        }
    }

    // ─── Swipe Handling ──────────────────────────────────────────────

    void DetectAndHandleSwipe()
    {
        if (currentLandmarks == null || previousLandmarks == null) return;
        if (!cardActive) return;

        // Override thresholds with inspector values
        float origH = SentisGestureDetector.SWIPE_HORIZONTAL_THRESHOLD;
        float origD = SentisGestureDetector.SWIPE_DOWN_THRESHOLD;

        // Use built-in detector (thresholds are constants, so we just call it)
        var swipe = SentisGestureDetector.GetSwipeDirection(currentLandmarks, previousLandmarks, landmarkDeltaTime);

        if (swipe == SentisGestureDetector.SwipeDirection.None) return;

        swipeCooldown = SWIPE_COOLDOWN_DURATION;
        HandleSort(swipe);
    }

    void HandleSort(SentisGestureDetector.SwipeDirection swipe)
    {
        if (currentCard == null) return;

        bool correct = false;

        switch (swipe)
        {
            case SentisGestureDetector.SwipeDirection.Left:
                FlashZone(leftZoneFlash);
                correct = (currentCard.category == PrintoutCategory.JulianFan);
                break;

            case SentisGestureDetector.SwipeDirection.Right:
                FlashZone(rightZoneFlash);
                correct = (currentCard.category == PrintoutCategory.AriaFan);
                break;

            case SentisGestureDetector.SwipeDirection.Down:
                FlashZone(bottomZoneFlash);
                correct = (currentCard.category == PrintoutCategory.Noise);
                break;
        }

        if (correct)
        {
            combo++;
            int gained = 10 * combo;
            score += gained;
            SetStatus($"+{gained}  ×{combo} COMBO!");
        }
        else
        {
            combo = 1;
            SetStatus("WRONG!");
        }

        SetScore();
        DismissCard();
    }

    void HandleMiss()
    {
        combo = 1;
        SetStatus("TOO SLOW!");
        SetScore();
        DismissCard();
    }

    void DismissCard()
    {
        cardActive = false;
        if (cardRect != null) cardRect.gameObject.SetActive(false);

        // Speed up next card
        currentFallDuration *= speedMultiplier;
        currentFallDuration = Mathf.Max(currentFallDuration, 0.6f); // minimum fall speed cap

        Invoke(nameof(SpawnCard), 0.4f);
    }

    // ─── End Game ────────────────────────────────────────────────────

    void EndGame()
    {
        state = GameState.End;
        cardActive = false;
        if (cardRect != null) cardRect.gameObject.SetActive(false);

        SetStatus($"TIME'S UP!\nFinal Score: {score}");
        if (timerText != null) timerText.text = "0";
    }

    // ─── UI Helpers ──────────────────────────────────────────────────

    void SetScore()
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (comboText != null) comboText.text = combo > 1 ? $"×{combo} COMBO" : "";
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
    }

    // ─── Zone Flashes ────────────────────────────────────────────────

    void FlashZone(Image zone)
    {
        if (zone == null) return;
        zone.gameObject.SetActive(true);

        if (zone == leftZoneFlash)       leftFlashTimer   = FLASH_DURATION;
        else if (zone == rightZoneFlash) rightFlashTimer  = FLASH_DURATION;
        else if (zone == bottomZoneFlash) bottomFlashTimer = FLASH_DURATION;
    }

    void UpdateZoneFlashes()
    {
        TickFlash(ref leftFlashTimer,   leftZoneFlash);
        TickFlash(ref rightFlashTimer,  rightZoneFlash);
        TickFlash(ref bottomFlashTimer, bottomZoneFlash);
    }

    void TickFlash(ref float timer, Image zone)
    {
        if (zone == null || timer <= 0f) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) zone.gameObject.SetActive(false);
    }
}
