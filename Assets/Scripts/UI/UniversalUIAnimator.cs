using UnityEngine;
using System.Collections;

// [RequireComponent(typeof(CanvasGroup))] // Removed to support Sprites/Graphics without CanvasGroup
public class UniversalUIAnimator : MonoBehaviour
{
    public enum AnimationType { Fade, Zoom, Slide, None }
    public enum IdleEffect { None, Wave, Wiggle, Shake, Bounce, Scribble }
    
    [Header("Transition Settings")]
    public AnimationType animationType = AnimationType.Fade;
    public float duration = 0.4f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool useFadeDuringTransition = true; // New toggle
    
    [Header("Slide Settings")]
    public Vector2 startOffset = new Vector2(0, -100);

    [Header("Idle Effects Settings")]
    public IdleEffect currentIdleEffect = IdleEffect.None;
    public float idleMagnitude = 5f;
    public float idleFrequency = 2f;

    [Tooltip("Idle effect to play while hovered. None = scale only.")]
    public IdleEffect hoverIdleEffect = IdleEffect.None;

    private CanvasGroup canvasGroup;
    private UnityEngine.UI.Graphic uiGraphic;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private float hoverMultiplier = 1f;
    private Coroutine idleCoroutine;
    private Coroutine hoverCoroutine;
    private IdleEffect _savedIdleEffect; // restored on hover exit

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        uiGraphic = GetComponent<UnityEngine.UI.Graphic>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();
        
        // Initial capture (may be 0,0 if layout group hasn't run)
        originalPosition = rectTransform != null ? rectTransform.anchoredPosition : (Vector2)transform.localPosition;
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        Show();
    }

    void OnDisable()
    {
        // Panel was hidden externally (e.g. Escape key) while a button was still hovered.
        // PointerExit never fired, so we must clean up hover state manually here.
        StopAllCoroutines();
        idleCoroutine  = null;
        hoverCoroutine = null;

        // Restore idle effect in case it was overridden by hover
        currentIdleEffect = _savedIdleEffect;

        // Reset scale multiplier and transform drift
        hoverMultiplier = 1f;
        transform.localScale    = originalScale;
        if (rectTransform != null) rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = Quaternion.identity;
    }

    public void Show()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        StartCoroutine(Animate(true));
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool appearing)
    {
        float elapsed = 0;

        // Force layout update if in a group to get the 'correct' destination (UI Only)
        if (appearing && rectTransform != null && transform.parent != null && transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
            originalPosition = rectTransform.anchoredPosition;
        }

        Vector2 startPos = appearing ? originalPosition + startOffset : originalPosition;
        Vector2 endPos = appearing ? originalPosition : originalPosition + startOffset;
        
        Vector3 startScale = appearing ? Vector3.zero : originalScale;
        Vector3 endScale = appearing ? originalScale : Vector3.zero;

        float startAlpha = appearing ? 0 : 1;
        float endAlpha = appearing ? 1 : 0;

        // If animation type is None, skip to end but still hide if disappearing
        if (animationType == AnimationType.None)
        {
            elapsed = duration;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = easeCurve.Evaluate(elapsed / duration);
            
            // Apply Fade only if type is Fade OR if the toggle is enabled
            bool shouldFade = animationType == AnimationType.Fade || useFadeDuringTransition;
            float currentAlpha = shouldFade ? Mathf.Lerp(startAlpha, endAlpha, percent) : 1f;

            // 1. Sync Alpha (Universal support)
            if (canvasGroup != null) canvasGroup.alpha = currentAlpha;
            else if (uiGraphic != null) uiGraphic.color = new Color(uiGraphic.color.r, uiGraphic.color.g, uiGraphic.color.b, currentAlpha);
            else if (spriteRenderer != null) spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, currentAlpha);
            
            // 2. Sync Scale
            if (animationType == AnimationType.Zoom)
                transform.localScale = Vector3.Lerp(startScale, endScale, percent);
            
            // 3. Sync Position
            if (animationType == AnimationType.Slide)
            {
                if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, percent);
                else transform.localPosition = Vector3.Lerp((Vector3)startPos, (Vector3)endPos, percent);
            }

            yield return null;
        }

        // Apply final states
        SetAlpha(endAlpha);
        if (animationType == AnimationType.Zoom) transform.localScale = endScale;
        if (animationType == AnimationType.Slide) 
        {
            if (rectTransform != null) rectTransform.anchoredPosition = endPos;
            else transform.localPosition = (Vector3)endPos;
        }

        if (appearing) StartIdle();
        else gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null) canvasGroup.alpha = alpha;
        else if (uiGraphic != null) uiGraphic.color = new Color(uiGraphic.color.r, uiGraphic.color.g, uiGraphic.color.b, alpha);
        else if (spriteRenderer != null) spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
    }

    [Header("Hover Scale")]
    public float hoverScale = 1.1f;

    /// <summary>Bind this to EventTrigger → PointerEnter</summary>
    public void OnHoverEnter()
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(HoverTween(hoverScale));

        if (hoverIdleEffect != IdleEffect.None)
        {
            if (idleCoroutine != null) StopCoroutine(idleCoroutine);
            originalPosition = rectTransform != null ? rectTransform.anchoredPosition : (Vector2)transform.localPosition;
            originalScale = transform.localScale;
            // Save base idle so we can restore it on exit
            _savedIdleEffect = currentIdleEffect;
            currentIdleEffect = hoverIdleEffect;
            idleCoroutine = StartCoroutine(IdleRoutine());
        }
    }

    /// <summary>Bind this to EventTrigger → PointerExit</summary>
    public void OnHoverExit()
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(HoverTween(1f));

        if (hoverIdleEffect != IdleEffect.None)
        {
            if (idleCoroutine != null) StopCoroutine(idleCoroutine);
            idleCoroutine = null;
            // Restore currentIdleEffect BEFORE calling StartIdle
            currentIdleEffect = _savedIdleEffect;
            // Reset any drift the idle caused
            if (rectTransform != null) rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = Quaternion.identity;
            transform.localScale = originalScale;
            // Resume base idle if there was one
            StartIdle();
        }
    }

    public void SetHover(bool isHovered, float scale = -1f)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        float target = isHovered ? (scale > 0 ? scale : hoverScale) : 1f;
        hoverCoroutine = StartCoroutine(HoverTween(target));
    }

    private IEnumerator HoverTween(float target)
    {
        float start = hoverMultiplier;
        float elapsed = 0;
        float hoverDuration = 0.15f;
        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            hoverMultiplier = Mathf.Lerp(start, target, elapsed / hoverDuration);
            yield return null;
        }
        hoverMultiplier = target;
    }

    private void StartIdle()
    {
        if (currentIdleEffect == IdleEffect.None) return;

        // Re-capture right before idle to ensure we are centered on our final layout position
        originalPosition = rectTransform != null ? rectTransform.anchoredPosition : (Vector2)transform.localPosition;
        originalScale = transform.localScale;

        idleCoroutine = StartCoroutine(IdleRoutine());
    }


    private IEnumerator IdleRoutine()
    {
        float time = 0;
        while (true)
        {
            time += Time.deltaTime * idleFrequency;
            float val = Mathf.Sin(time);

            switch (currentIdleEffect)
            {
                case IdleEffect.Wave:
                    rectTransform.anchoredPosition = originalPosition + new Vector2(0, val * idleMagnitude);
                    break;
                case IdleEffect.Wiggle:
                    rectTransform.localRotation = Quaternion.Euler(0, 0, val * idleMagnitude);
                    break;
                case IdleEffect.Shake:
                    rectTransform.anchoredPosition = originalPosition + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * idleMagnitude;
                    break;
                case IdleEffect.Bounce:
                    transform.localScale = originalScale * (1f + (val * 0.1f * idleMagnitude / 5f)) * hoverMultiplier;
                    break;
                case IdleEffect.Scribble:
                    // Jittery pencil effect: High frequency random offsets
                    rectTransform.anchoredPosition = originalPosition + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (idleMagnitude * 0.5f);
                    rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-1f, 1f) * (idleMagnitude * 0.2f));
                    transform.localScale = originalScale * (1f + Random.Range(-0.02f, 0.02f) * idleMagnitude) * hoverMultiplier;
                    break;
            }
            
            // If No idle effect but hovering, we still need to apply the multiplier
            if (currentIdleEffect == IdleEffect.None && hoverMultiplier != 1f)
            {
                transform.localScale = originalScale * hoverMultiplier;
            }
            yield return null;
        }
    }
}
