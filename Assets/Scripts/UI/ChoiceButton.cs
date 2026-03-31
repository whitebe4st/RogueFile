using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.05f;
    public float animationDuration = 0.15f;
    public Color hoverColor = Color.white;
    private Color originalColor;
    private Image buttonImage;

    [Header("Click Settings")]
    public float clickScale = 0.95f;
    public float clickDuration = 0.1f;

    private Vector3 originalScale;
    private Coroutine animationCoroutine;
    private UniversalUIAnimator uiAnimator;

    void Awake()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();
        if (buttonImage != null) originalColor = buttonImage.color;
        uiAnimator = GetComponent<UniversalUIAnimator>();
    }

    void OnDisable()
    {
        transform.localScale = originalScale;
        if (buttonImage != null) buttonImage.color = originalColor;
        StopAllCoroutines();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetHover(true, hoverScale);
            AnimateColor(hoverColor); // Still handle color locally
        }
        else
        {
            AnimateTo(originalScale * hoverScale, hoverColor);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetHover(false);
            AnimateColor(originalColor);
        }
        else
        {
            AnimateTo(originalScale, originalColor);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Safety check: Don't start a coroutine if the button was deactivated by its own click event
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ClickRoutine());
        }
    }

    private void AnimateColor(Color targetColor)
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(ColorRoutine(targetColor));
    }

    private IEnumerator ColorRoutine(Color targetColor)
    {
        Color startColor = buttonImage != null ? buttonImage.color : Color.white;
        float elapsed = 0;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            if (buttonImage != null) buttonImage.color = Color.Lerp(startColor, targetColor, elapsed / animationDuration);
            yield return null;
        }
        if (buttonImage != null) buttonImage.color = targetColor;
    }

    private void AnimateTo(Vector3 targetScale, Color targetColor)
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateRoutine(targetScale, targetColor));
    }

    private IEnumerator AnimateRoutine(Vector3 targetScale, Color targetColor)
    {
        Vector3 startScale = transform.localScale;
        Color startColor = buttonImage != null ? buttonImage.color : Color.white;
        float elapsed = 0;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, percent);
            if (buttonImage != null) buttonImage.color = Color.Lerp(startColor, targetColor, percent);
            yield return null;
        }

        transform.localScale = targetScale;
        if (buttonImage != null) buttonImage.color = targetColor;
    }

    private IEnumerator ClickRoutine()
    {
        transform.localScale = originalScale * clickScale;
        yield return new WaitForSecondsRealtime(clickDuration);
        transform.localScale = originalScale * hoverScale;
    }
}
