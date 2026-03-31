using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a single grid cell in the RE-style inventory.
/// Can be empty (dark cell) or filled with an item icon.
/// </summary>
public class InventorySlot : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconDisplay;
    public Image backgroundImage;
    public Image borderImage;

    [Header("Colors")]
    public Color emptyColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    public Color filledColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color selectedColor = new Color(0.8f, 0.5f, 0.1f, 1f);
    public Color normalBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    /// <summary>The item stored in this slot, or null if empty.</summary>
    [HideInInspector] public ItemData currentItem;

    public void SetItem(ItemData item)
    {
        currentItem = item;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = item.icon;
            iconDisplay.enabled = item.icon != null;
        }

        if (backgroundImage != null)
            backgroundImage.color = filledColor;
    }

    public void ClearSlot()
    {
        currentItem = null;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = null;
            iconDisplay.enabled = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = emptyColor;

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (borderImage != null)
            borderImage.color = selected ? selectedColor : normalBorderColor;
    }
}