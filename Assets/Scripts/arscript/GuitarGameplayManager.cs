using UnityEngine;
using TMPro;
using DetectiveGame.HandTracking;

/// <summary>
/// Guitar AR minigame manager — Sentis version (single hand).
/// Reads hand landmarks from HandDetection (Sentis) and uses
/// SentisGestureDetector for chord recognition and strum detection.
/// </summary>
public class GuitarGameplayManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandDetection handDetection;
    [SerializeField] private GuitarAudioSynthesizer audioSynthesizer;
    [SerializeField] private TextMeshProUGUI chordStatusText;
    [SerializeField] private TextMeshProUGUI strumStatusText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Game Settings")]
    [SerializeField, Range(0.1f, 2.0f)] private float strumVelocityThreshold = 0.5f;
    [SerializeField, Range(0.01f, 1.0f)] private float smoothingAlpha = 0.2f;
    [SerializeField] private float strumCooldown = 0.15f;

    // State
    private SentisGestureDetector.GuitarChord currentChord = SentisGestureDetector.GuitarChord.None;
    private float previousWristY = -1f;
    private float smoothedWristY = 0f;
    private float lastStrumTime = 0f;
    private int score = 0;

    private void Start()
    {
        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (audioSynthesizer == null)
        {
            audioSynthesizer = FindAnyObjectByType<GuitarAudioSynthesizer>();
            if (audioSynthesizer == null)
                audioSynthesizer = gameObject.AddComponent<GuitarAudioSynthesizer>();
        }

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarksUpdated;
        else
            Debug.LogError("[GuitarGameplayManager] No HandDetection found!");
    }

    private void OnDestroy()
    {
        if (handDetection != null)
            handDetection.OnLandmarksUpdated -= OnLandmarksUpdated;
    }

    private void OnLandmarksUpdated(Vector3[] normalizedLandmarks)
    {
        if (normalizedLandmarks == null || normalizedLandmarks.Length < 21) return;

        // Chord Detection
        currentChord = SentisGestureDetector.DetectChord(normalizedLandmarks);
        UpdateChordUI();
        UpdateAudioChord();

        // Strum Detection (wrist Y velocity)
        float wristY = normalizedLandmarks[SentisGestureDetector.WRIST].y;

        if (previousWristY < 0f)
        {
            smoothedWristY = wristY;
            previousWristY = wristY;
            return;
        }

        smoothedWristY = Mathf.Lerp(smoothedWristY, wristY, smoothingAlpha);

        if (Time.time - lastStrumTime > strumCooldown)
        {
            if (SentisGestureDetector.DetectStrum(smoothedWristY, previousWristY, Time.deltaTime, strumVelocityThreshold))
            {
                OnStrum();
                lastStrumTime = Time.time;
            }
        }

        previousWristY = smoothedWristY;
    }

    private void OnStrum()
    {
        if (audioSynthesizer != null) audioSynthesizer.Strum();

        if (strumStatusText != null)
        {
            strumStatusText.text = "♪ STRUM! ♪";
            CancelInvoke(nameof(ResetStrumText));
            Invoke(nameof(ResetStrumText), 0.3f);
        }

        if (currentChord != SentisGestureDetector.GuitarChord.None)
        {
            score += 10;
            if (scoreText != null) scoreText.text = $"Score: {score}";
        }
    }

    private void ResetStrumText()
    {
        if (strumStatusText != null) strumStatusText.text = "";
    }

    private void UpdateChordUI()
    {
        if (chordStatusText == null) return;

        if (currentChord != SentisGestureDetector.GuitarChord.None)
        {
            chordStatusText.text = $"Chord: {currentChord}";
            chordStatusText.color = Color.green;
        }
        else
        {
            chordStatusText.text = "Chord: None";
            chordStatusText.color = Color.yellow;
        }
    }

    private void UpdateAudioChord()
    {
        if (audioSynthesizer == null) return;

        var oldChord = currentChord switch
        {
            SentisGestureDetector.GuitarChord.A_Major => GuitarChord.A_Major,
            SentisGestureDetector.GuitarChord.B_Minor => GuitarChord.B_Minor,
            SentisGestureDetector.GuitarChord.C_Major => GuitarChord.C_Major,
            SentisGestureDetector.GuitarChord.D_Major => GuitarChord.D_Major,
            SentisGestureDetector.GuitarChord.E_Major => GuitarChord.E_Major,
            SentisGestureDetector.GuitarChord.F_Major => GuitarChord.F_Major,
            _ => GuitarChord.None
        };

        audioSynthesizer.PlayChord(oldChord);
    }
}
