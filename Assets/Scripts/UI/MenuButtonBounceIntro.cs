using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to your "panel" GameObject (parent of the buttons).
/// Call PlayIntro() whenever the panel becomes visible to animate
/// each button flying in from below with a bounce landing.
///
/// HOW TO SET UP IN INSPECTOR:
///   buttons   — drag in [resume, options, save, quit] in order (bottom → top or top → bottom)
///   staggerDelay   — seconds between each button starting
///   slideDist — how far below the button starts (pixels in UI space)
///   bounceCurve    — an AnimationCurve shaped like a bounce (see tip below)
///
/// QUICK CURVE TIP:
///   Window → Animation → create a curve that goes 0 → 1.15 → 0.95 → 1.0
///   (overshoot then settle) for a nice spring feel.
///   Or leave it as default — the script provides a built-in bounce curve.
/// </summary>
public class MenuButtonBounceIntro : MonoBehaviour
{
    [Header("Buttons (top → bottom order in the hierarchy)")]
    public List<RectTransform> buttons = new List<RectTransform>();

    [Header("Timing")]
    [Tooltip("Delay between each button's start, in seconds.")]
    public float staggerDelay = 0.07f;

    [Tooltip("Total duration of one button's animation.")]
    public float duration = 0.45f;

    [Header("Motion")]
    [Tooltip("How many pixels below the final position the button starts from.")]
    public float slideDist = 120f;

    [Tooltip("Custom bounce curve. Leave empty to use the built-in spring curve.")]
    public AnimationCurve bounceCurve;

    // ── Internal ─────────────────────────────────────────────────────────────

    private Coroutine _introRoutine;
    private readonly List<Vector2> _originalPositions = new List<Vector2>();

    private void Awake()
    {
        // Cache the rest positions set in the editor
        _originalPositions.Clear();
        foreach (var btn in buttons)
            _originalPositions.Add(btn != null ? btn.anchoredPosition : Vector2.zero);

        // Build a default bounce curve if none assigned
        if (bounceCurve == null || bounceCurve.length == 0)
            bounceCurve = BuildDefaultBounceCurve();
    }

    private void OnEnable()
    {
        // Auto-play when the panel is shown
        PlayIntro();
    }

    /// <summary>
    /// Kick off the staggered entrance. Safe to call multiple times — cancels the previous run.
    /// Animates bottom-to-top (last button in list comes in first so the top button lands last).
    /// </summary>
    public void PlayIntro()
    {
        if (_introRoutine != null) StopCoroutine(_introRoutine);
        _introRoutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        // Snap all buttons to their "hidden" start position first
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].anchoredPosition = _originalPositions[i] + Vector2.down * slideDist;
        }

        // Spawn from bottom button upward → reverse iteration order
        // (last index = bottom-most button in a vertical layout)
        for (int i = buttons.Count - 1; i >= 0; i--)
        {
            if (buttons[i] == null) continue;

            // Fire each button's animation as a parallel coroutine so stagger works
            StartCoroutine(AnimateButton(buttons[i], _originalPositions[i]));

            yield return new WaitForSecondsRealtime(staggerDelay);
        }
    }

    private IEnumerator AnimateButton(RectTransform btn, Vector2 targetPos)
    {
        Vector2 startPos = btn.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;   // unscaled so it works when Time.timeScale = 0
            float t = Mathf.Clamp01(elapsed / duration);
            float curve = bounceCurve.Evaluate(t);
            btn.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curve);
            yield return null;
        }

        btn.anchoredPosition = targetPos; // Snap to exact final position
    }

    // ── Built-in spring/bounce curve ──────────────────────────────────────────

    private static AnimationCurve BuildDefaultBounceCurve()
    {
        // Keys: t=0 (y=0), t=0.6 (y=1.18 overshoot), t=0.78 (y=0.93 undershoot), t=1 (y=1.0 settle)
        Keyframe[] keys =
        {
            new Keyframe(0f,    0f,    0f,    3.5f),
            new Keyframe(0.6f,  1.18f, 0f,    0f),
            new Keyframe(0.78f, 0.93f, 0f,    0f),
            new Keyframe(1f,    1f,    0f,    0f),
        };
        var curve = new AnimationCurve(keys);
        // Smooth all tangents
        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0.2f);
        return curve;
    }
}
