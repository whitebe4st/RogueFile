using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class SignalMatchGame : MonoBehaviour
{
    [Header("UI Plotter (UILineRenderer)")]
    public UIWaveLinePlotter uiPlotter;

    [Header("Wave Settings")]
    [Range(32, 1024)] public int samples = 256;
    public float timeWindow = 1.2f;

    [Header("UI Controls")]
    public Slider freqSlider;
    public Slider ampSlider;
    public Slider phaseSlider;
    public TMP_Text similarityText;
    public Image lockProgressFill;

    [Header("UI Root (Panel)")]
    public GameObject signalPanel;

    [Header("Disable Player Control While Playing")]
    public MonoBehaviour[] disableWhilePlaying;

    [Header("Target Range")]
    public Vector2 freqRange = new Vector2(0.8f, 7.0f);
    public Vector2 ampRange = new Vector2(0.2f, 1.0f);

    [Header("Match Rules")]
    [Range(0.7f, 0.999f)] public float winThreshold = 0.95f;
    public float holdTimeToWin = 1.0f;

    [Header("End Behavior")]
    public bool autoCloseOnWin = true;
    public float closeDelay = 0.0f;

    [Header("MiniGame Events")]
    public UnityEvent OnWin;
    public UnityEvent OnCancel;

    private float targetFreq, targetAmp, targetPhaseRad;
    private float holdTimer;

    private float[] targetSamples;
    private float[] playerSamples;

    private bool isPlaying;
    private bool isCompleted;

    public bool IsPlaying => isPlaying;

    void Start()
    {
        targetSamples = new float[samples];
        playerSamples = new float[samples];

        if (signalPanel) signalPanel.SetActive(false);
        isPlaying = false;

        if (uiPlotter)
        {
            uiPlotter.ClearLines();
            uiPlotter.ShowLines(false);
        }
    }

    void Update()
    {
        if (!isPlaying || isCompleted) return;

        

        float playerFreq = freqSlider.value;
        float playerAmp = ampSlider.value;
        float playerPhaseRad = phaseSlider.value * Mathf.Deg2Rad;

        FillWave(targetSamples, targetFreq, targetAmp, targetPhaseRad);
        FillWave(playerSamples, playerFreq, playerAmp, playerPhaseRad);

        if (uiPlotter) uiPlotter.Draw(targetSamples, playerSamples);

        float sim = SimilarityNormalizedCorrelation(targetSamples, playerSamples);
        if (similarityText) similarityText.text = $"Similarity: {(sim * 100f):0.0}%";

        if (sim >= winThreshold) holdTimer += Time.deltaTime;
        else holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 1.5f);

        float progress = Mathf.Clamp01(holdTimer / holdTimeToWin);
        if (lockProgressFill) lockProgressFill.fillAmount = progress;

        if (holdTimer >= holdTimeToWin)
        {
            CompleteMinigame();
        }
    }

    public void StartMinigame()
    {
        isPlaying = true;
        isCompleted = false;

        holdTimer = 0f;
        if (lockProgressFill) lockProgressFill.fillAmount = 0f;

        if (signalPanel) signalPanel.SetActive(true);

        if (uiPlotter)
        {
            uiPlotter.ShowLines(true);
            uiPlotter.ClearLines();
        }

        foreach (var s in disableWhilePlaying)
            if (s) s.enabled = false;

        NewRound();
    }

    
    public void CancelByEsc()
    {
        CancelMinigame();
    }

    void CancelMinigame()
    {
        if (!isPlaying) return;

        isCompleted = true;
        OnCancel?.Invoke();
        StopMinigame();
    }

    public void StopMinigame()
    {
        isPlaying = false;

        if (signalPanel) signalPanel.SetActive(false);

        if (uiPlotter)
        {
            uiPlotter.ClearLines();
            uiPlotter.ShowLines(false);
        }

        foreach (var s in disableWhilePlaying)
            if (s) s.enabled = true;
    }

    void CompleteMinigame()
    {
        isCompleted = true;
        OnWin?.Invoke();

        if (autoCloseOnWin)
        {
            if (closeDelay <= 0f) StopMinigame();
            else Invoke(nameof(StopMinigame), closeDelay);
        }
    }

    void NewRound()
    {
        targetFreq = Random.Range(freqRange.x, freqRange.y);
        targetAmp = Random.Range(ampRange.x, ampRange.y);
        targetPhaseRad = Random.Range(0f, Mathf.PI * 2f);
        holdTimer = 0f;
        if (lockProgressFill) lockProgressFill.fillAmount = 0f;

        freqSlider.value = Mathf.Clamp(targetFreq + Random.Range(-1.5f, 1.5f), freqRange.x, freqRange.y);
        ampSlider.value = Mathf.Clamp(targetAmp + Random.Range(-0.2f, 0.2f), ampRange.x, ampRange.y);
        phaseSlider.value = Random.Range(0f, 360f);
    }

    void FillWave(float[] buffer, float freq, float amp, float phaseRad)
    {
        int n = buffer.Length;
        for (int i = 0; i < n; i++)
        {
            float t = (i / (float)(n - 1)) * timeWindow;
            buffer[i] = Mathf.Sin((2f * Mathf.PI * freq * t) + phaseRad) * amp;
        }
    }

    float SimilarityNormalizedCorrelation(float[] a, float[] b)
    {
        int n = a.Length;
        float meanA = 0f, meanB = 0f;

        for (int i = 0; i < n; i++) { meanA += a[i]; meanB += b[i]; }
        meanA /= n; meanB /= n;

        float num = 0f, denA = 0f, denB = 0f;
        for (int i = 0; i < n; i++)
        {
            float da = a[i] - meanA;
            float db = b[i] - meanB;
            num += da * db;
            denA += da * da;
            denB += db * db;
        }

        float den = Mathf.Sqrt(denA * denB);
        if (den < 1e-6f) return 0f;

        float corr = num / den;
        return Mathf.Clamp01((corr + 1f) * 0.5f);
    }
}
