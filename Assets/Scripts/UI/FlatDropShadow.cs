using UnityEngine;
using UnityEngine.UI;

public class FlatDropShadow : MonoBehaviour
{
    [Header("Shadow Settings")]
    public Color shadowColor = new Color(0, 0, 0, 0.5f);
    public Vector2 shadowOffset = new Vector2(5f, -5f);
    
    [Header("Behavior")]
    public bool useRelativeOffset = true;
    public int shadowSortingOrderOffset = -1;

    private GameObject shadowObj;
    private RectTransform rectTransform;
    private SpriteRenderer spriteRenderer;
    
    // Shadow components
    private Image mainImage, shadowImage;
    private RawImage mainRawImage, shadowRawImage;
    private SpriteRenderer shadowSpriteRenderer;
    private CanvasGroup mainCanvasGroup, shadowCanvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainImage = GetComponent<Image>();
        mainRawImage = GetComponent<RawImage>();
        mainCanvasGroup = GetComponent<CanvasGroup>();
        CreateShadowObject();
    }

    void CreateShadowObject()
    {
        if (shadowObj != null) return;

        // 1. Create Sibling Shadow (Must be sibling to avoid "Children-on-Top" UI rule)
        shadowObj = new GameObject(gameObject.name + "_ShadowInstance");
        shadowObj.transform.SetParent(transform.parent, false);
        
        // Load custom shader
        Shader silhouetteShader = Shader.Find("UI/FlatShadowSilhouette");
        Material shadowMaterial = (silhouetteShader != null) ? new Material(silhouetteShader) : null;

        if (rectTransform != null)
        {
            RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
            
            // CRITICAL: Ignore layout groups so we don't break the parent layout
            LayoutElement le = shadowObj.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // FADE SYNC: Add CanvasGroup if main has one, or just add it to handle group fades
            shadowCanvasGroup = shadowObj.AddComponent<CanvasGroup>();
            shadowCanvasGroup.interactable = false;
            shadowCanvasGroup.blocksRaycasts = false;

            if (mainImage != null)
            {
                shadowImage = shadowObj.AddComponent<Image>();
                shadowImage.raycastTarget = false;
                if (shadowMaterial != null) shadowImage.material = shadowMaterial;
            }
            else if (mainRawImage != null)
            {
                shadowRawImage = shadowObj.AddComponent<RawImage>();
                shadowRawImage.raycastTarget = false;
                if (shadowMaterial != null) shadowRawImage.material = shadowMaterial;
            }
        }
        else if (spriteRenderer != null)
        {
            shadowSpriteRenderer = shadowObj.AddComponent<SpriteRenderer>();
            if (shadowMaterial != null) shadowSpriteRenderer.material = shadowMaterial;
        }
    }

    void LateUpdate()
    {
        if (shadowObj == null) CreateShadowObject();

        // Ensure shadow visibility mirrors target
        bool isVisible = gameObject.activeInHierarchy;
        if (shadowObj.activeSelf != isVisible) shadowObj.SetActive(isVisible);
        if (!isVisible) return;

        // Ensure shadow stays at the same hierarchy level
        if (shadowObj.transform.parent != transform.parent)
            shadowObj.transform.SetParent(transform.parent, false);

        // FORCE BEHIND: Shadow must be BEFORE the target in sibling index to be behind it in UI
        int myIndex = transform.GetSiblingIndex();
        int shadowIndex = shadowObj.transform.GetSiblingIndex();
        if (shadowIndex >= myIndex)
        {
            shadowObj.transform.SetSiblingIndex(Mathf.Max(0, myIndex));
        }

        UpdateSync();
    }

    void UpdateSync()
    {
        if (rectTransform != null)
        {
            RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
            shadowRect.anchorMin = rectTransform.anchorMin;
            shadowRect.anchorMax = rectTransform.anchorMax;
            shadowRect.pivot = rectTransform.pivot;
            shadowRect.sizeDelta = rectTransform.sizeDelta;
            shadowRect.rotation = rectTransform.rotation;
            shadowRect.localScale = rectTransform.localScale;
            shadowRect.anchoredPosition = rectTransform.anchoredPosition + shadowOffset;

            // Sync Alpha/Fade (Handles UniversalUIAnimator Fade)
            if (mainCanvasGroup != null) shadowCanvasGroup.alpha = mainCanvasGroup.alpha;
            else if (mainImage != null) shadowCanvasGroup.alpha = mainImage.color.a;

            if (shadowImage != null && mainImage != null)
            {
                shadowImage.sprite = mainImage.sprite;
                shadowImage.color = shadowColor;
                shadowImage.type = mainImage.type;
                shadowImage.preserveAspect = mainImage.preserveAspect;
            }
            else if (shadowRawImage != null && mainRawImage != null)
            {
                shadowRawImage.texture = mainRawImage.texture;
                shadowRawImage.color = shadowColor;
            }
        }
        else if (spriteRenderer != null && shadowSpriteRenderer != null)
        {
            // World Sprites
            shadowObj.transform.position = transform.position + (Vector3)shadowOffset + new Vector3(0, 0, 0.1f);
            shadowObj.transform.rotation = transform.rotation;
            shadowObj.transform.localScale = transform.localScale;

            shadowSpriteRenderer.sprite = spriteRenderer.sprite;
            // Combined Alpha (Shadow Color + Sprite Renderer Alpha)
            Color finalColor = shadowColor;
            finalColor.a *= spriteRenderer.color.a;
            shadowSpriteRenderer.color = finalColor;

            shadowSpriteRenderer.flipX = spriteRenderer.flipX;
            shadowSpriteRenderer.flipY = spriteRenderer.flipY;
            shadowSpriteRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            shadowSpriteRenderer.sortingOrder = spriteRenderer.sortingOrder + shadowSortingOrderOffset;
        }
    }

    void OnDisable()
    {
        if (shadowObj != null) shadowObj.SetActive(false);
    }

    void OnEnable()
    {
        if (shadowObj != null) shadowObj.SetActive(true);
    }

    void OnDestroy()
    {
        if (shadowObj != null) Destroy(shadowObj);
    }
}
