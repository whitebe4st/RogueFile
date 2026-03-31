using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// "DON'T LET GO" — Clue 4 Minigame
/// Balancing a document folder on an open palm for 20 seconds.
/// Difficulty increases in waves (wind shaking).
/// Player must keep hand flat (low pitch/roll).
/// </summary>
public class DontLetGoManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandDetection handDetection;
    [SerializeField] private RectTransform folderRect;     // The UI object to balance
    [SerializeField] private Image folderImage;            // The Image component for sprite swapping
    [SerializeField] private Slider progressSlider;        // 0 to 20 seconds
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI grimmText;

    [Header("Folder Sprites")]
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite tiltLeftSprite;
    [SerializeField] private Sprite tiltRightSprite;
    [SerializeField] private Sprite droppedSprite;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 20f;
    [SerializeField] private float tiltThreshold = 30f;    // Drop if tilt > 30 degrees
    [SerializeField] private float tiltSpriteThreshold = 10f; // Swap to tilt sprite at 10 deg
    [SerializeField] private float baseShakeIntensity = 10f;
    [SerializeField] private float waveIntensityMultiplier = 1.5f;

    // ─── State ───────────────────────────────────────────────────────

    enum GameState { Idle, Intro, Playing, Fail, Win }
    private GameState state = GameState.Idle;

    private float survivalTime;
    private float waveTimer;
    private float currentShakeIntensity;
    
    // Hand tracking
    private Vector3[] currentLandmarks;
    private float currentPitch;
    private float currentRoll;
    private bool isHandVisible;

    // Folder physics (simulated)
    private float folderTiltX;
    private float folderTiltY;
    private Vector2 folderOffset;

    // ─── Lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = gameDuration;
            progressSlider.value = 0;
        }

        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarks;

        StartCoroutine(IntroSequence());
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

        if (isHandVisible)
        {
            SentisGestureDetector.GetPalmTilt(lm, out currentPitch, out currentRoll);
        }
    }

    // ─── Update Loop ─────────────────────────────────────────────────

    void Update()
    {
        if (state != GameState.Playing) return;

        // 1. Check Hand Presence
        if (!isHandVisible)
        {
            SetStatus("HAND LOST!");
            ApplyGravityEffect(); // Pull folder down if hand is missing
        }
        else
        {
            SetStatus("");
        }

        // 2. Wave Difficulty
        waveTimer += Time.deltaTime;
        int waveCount = Mathf.FloorToInt(waveTimer / 5f); // Wave every 5s
        currentShakeIntensity = baseShakeIntensity * (1f + waveCount * waveIntensityMultiplier);

        // 3. Balance Physics
        UpdateBalance();

        // 4. Survival Progress
        survivalTime += Time.deltaTime;
        if (progressSlider != null) progressSlider.value = survivalTime;
        if (timerText != null) timerText.text = $"{(gameDuration - survivalTime):F1}s";

        if (survivalTime >= gameDuration)
        {
            WinGame();
        }
    }

    // ─── Game Loop ───────────────────────────────────────────────────

    IEnumerator IntroSequence()
    {
        state = GameState.Intro;
        SetStatus("BALANCING ACT...");
        yield return new WaitForSeconds(1.5f);
        
        if (grimmText != null)
            grimmText.text = "\"Silas lost millions because of this show. That's motive enough.\"";
        
        SetStatus("HOLD STEADY!");
        yield return new WaitForSeconds(1f);
        SetStatus("");
        
        ResetGame();
        state = GameState.Playing;
    }

    void ResetGame()
    {
        survivalTime = 0;
        waveTimer = 0;
        folderTiltX = 0;
        folderTiltY = 0;
        folderOffset = Vector2.zero;
        if (folderRect != null)
        {
            folderRect.anchoredPosition = Vector2.zero;
            folderRect.localRotation = Quaternion.identity;
        }
    }

    void UpdateBalance()
    {
        // Add random wind shake
        float shakeX = Random.Range(-1f, 1f) * currentShakeIntensity;
        float shakeY = Random.Range(-1f, 1f) * currentShakeIntensity;

        // The folder wants to follow the hand tilt, plus wind
        // pitch -> tilting forward/backward
        // roll  -> tilting left/right
        
        folderTiltX = Mathf.Lerp(folderTiltX, currentPitch + shakeX, Time.deltaTime * 5f);
        folderTiltY = Mathf.Lerp(folderTiltY, currentRoll + shakeY, Time.deltaTime * 5f);

        // Visual tilt
        if (folderRect != null)
        {
            // Position shifts slightly based on tilt
            folderOffset.x = folderTiltY * 2f;
            folderOffset.y = -folderTiltX * 2f;
            folderRect.anchoredPosition = folderOffset;

            // Rotation follows tilt
            folderRect.localRotation = Quaternion.Euler(-folderTiltX, folderTiltY, 0);

            // ── Sprite Swapping ────────────────────────────
            if (folderImage != null)
            {
                if (folderTiltY < -tiltSpriteThreshold)
                    folderImage.sprite = tiltLeftSprite;
                else if (folderTiltY > tiltSpriteThreshold)
                    folderImage.sprite = tiltRightSprite;
                else
                    folderImage.sprite = neutralSprite;
            }
        }

        // Drop Check
        if (Mathf.Abs(currentPitch) > tiltThreshold || Mathf.Abs(currentRoll) > tiltThreshold)
        {
            FailGame();
        }
    }

    void ApplyGravityEffect()
    {
        // Hand is missing, folder starts "falling"
        folderTiltX += Time.deltaTime * 50f;
        if (Mathf.Abs(folderTiltX) > tiltThreshold)
        {
            FailGame();
        }
    }

    // ─── End Game ────────────────────────────────────────────────────

    void FailGame()
    {
        state = GameState.Fail;
        SetStatus("FOLDER DROPPED!");

        if (folderImage != null && droppedSprite != null)
            folderImage.sprite = droppedSprite;

        StartCoroutine(RestartSequence());
    }

    void WinGame()
    {
        state = GameState.Win;
        SetStatus("HELD STEADY!\nClue Found.");
        if (timerText != null) timerText.text = "0.0s";
    }

    IEnumerator RestartSequence()
    {
        yield return new WaitForSeconds(1.5f);
        SetStatus("TRY AGAIN...");
        yield return new WaitForSeconds(1f);
        ResetGame();
        state = GameState.Playing;
    }

    // ─── UI Helpers ──────────────────────────────────────────────────

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
