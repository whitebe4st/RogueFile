using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// "STEADY POUR" — Clue 5 Minigame
/// Player tilts their wrist to pour liquid. 
/// Must stay in the "Sweet Spot" angle (e.g. 45 to 75 degrees) to fill the progress bar.
/// 3 spills = reset fill bar. 30 second timer.
/// </summary>
public class SteadyPourManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandDetection handDetection;
    [SerializeField] private RectTransform vialRect;       // The tilting bottle
    [SerializeField] private Slider fillSlider;            // The collection bag progress
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI grimmText;
    [SerializeField] private Image strike1, strike2, strike3; // Splash/Strike indicators

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 30f;
    [SerializeField] private float fillSpeed = 10f;        // Units per second while pouring
    [SerializeField] private float maxFill = 100f;
    [SerializeField] private float minPourAngle = 45f;     // Start pouring here (tilt right)
    [SerializeField] private float maxPourAngle = 75f;     // Spill if tilted past this
    [SerializeField] private int maxSplashes = 3;

    // ─── State ───────────────────────────────────────────────────────

    enum GameState { Idle, Intro, Playing, End }
    private GameState state = GameState.Idle;

    private float timeRemaining;
    private float currentFill;
    private int splashCount;
    private bool isInSplashCooldown;

    // Hand tracking
    private Vector3[] currentLandmarks;
    private float currentWristRoll;
    private bool isHandVisible;
    private float smoothAngle;

    // ─── Lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        if (fillSlider != null)
        {
            fillSlider.minValue = 0;
            fillSlider.maxValue = maxFill;
            fillSlider.value = 0;
        }

        UpdateStrikeUI();

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
            currentWristRoll = SentisGestureDetector.GetWristRoll(lm);
        }
    }

    // ─── Update Loop ─────────────────────────────────────────────────

    void Update()
    {
        if (state != GameState.Playing) return;

        // 1. Timer
        timeRemaining -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Max(0, Mathf.CeilToInt(timeRemaining)).ToString() + "s";

        if (timeRemaining <= 0)
        {
            EndGame();
            return;
        }

        // 2. Wrist Rotation Smoothing
        float targetAngle = isHandVisible ? currentWristRoll : 0f;
        // Clamp it a bit for UI sanity
        targetAngle = Mathf.Clamp(targetAngle, -20f, 135f); 
        smoothAngle = Mathf.Lerp(smoothAngle, targetAngle, Time.deltaTime * 5f);

        if (vialRect != null)
        {
            // Rotate the vial UI (visual only)
            vialRect.localRotation = Quaternion.Euler(0, 0, -smoothAngle);
        }

        // 3. Pouring Logic
        if (isHandVisible && !isInSplashCooldown)
        {
            if (smoothAngle > minPourAngle && smoothAngle < maxPourAngle)
            {
                // Sweet Spot! 
                SetStatus("POURING...");
                currentFill += fillSpeed * Time.deltaTime;
                if (currentFill > maxFill) currentFill = maxFill;
            }
            else if (smoothAngle >= maxPourAngle)
            {
                // Too far! Splash!
                RegisterSplash();
            }
            else
            {
                // Not tilted enough
                SetStatus("TILT TO POUR");
            }
        }
        else if (!isHandVisible)
        {
            SetStatus("HAND LOST");
        }

        if (fillSlider != null) fillSlider.value = currentFill;
    }

    // ─── Game Loop ───────────────────────────────────────────────────

    IEnumerator IntroSequence()
    {
        state = GameState.Intro;
        SetStatus("READY VIAL...");
        yield return new WaitForSeconds(1.5f);
        
        if (grimmText != null)
            grimmText.text = "\"...Whatever this is, it dissolved him from the inside.\"";
        
        SetStatus("STEADY POUR!");
        yield return new WaitForSeconds(1f);
        
        timeRemaining = gameDuration;
        currentFill = 0;
        splashCount = 0;
        UpdateStrikeUI();
        state = GameState.Playing;
    }

    void RegisterSplash()
    {
        splashCount++;
        UpdateStrikeUI();
        StartCoroutine(SplashCooldown());

        if (splashCount >= maxSplashes)
        {
            SetStatus("FATAL SPILL! RESETTING...");
            currentFill = 0; // Empty the bag entirely
            splashCount = 0; // Reset strikes, keep timer going
            // Don't update strikes immediately if we want a dramatic flash, 
            // but for simplicity we'll reset them.
            Invoke(nameof(UpdateStrikeUI), 1f); 
        }
    }

    IEnumerator SplashCooldown()
    {
        isInSplashCooldown = true;
        SetStatus("SPLASH! TOO FAST!");
        // Small penalty, player must level out to recover
        yield return new WaitForSeconds(1.5f);
        isInSplashCooldown = false;
    }

    void UpdateStrikeUI()
    {
        if (strike1 != null) strike1.color = splashCount >= 1 ? Color.red : Color.black;
        if (strike2 != null) strike2.color = splashCount >= 2 ? Color.red : Color.black;
        if (strike3 != null) strike3.color = splashCount >= 3 ? Color.red : Color.black;
    }

    void EndGame()
    {
        state = GameState.End;
        float percent = (currentFill / maxFill) * 100f;
        SetStatus(percent >= 50f ? $"SUCCESS: {percent:F0}%" : $"FAILED: {percent:F0}%");
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
