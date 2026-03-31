using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Resident Evil-style grid inventory with Clues/Items category tabs.
/// Fixed grid layout — empty slots remain visible as dark cell
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject inventoryPanel;
    public Transform slotContainer;
    public GameObject slotPrefab;

    [Header("Connect to Player")]
    public Inventory playerInventory;

    [Header("Category Tabs")]
    public Button cluesTabButton;
    public Button itemsTabButton;
    public Color activeTabColor = new Color(0.8f, 0.5f, 0.1f, 1f);
    public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Grid Settings")]
    [Tooltip("Total number of grid slots to display (e.g. 20 for a 4x5 grid)")]
    public int gridSlotCount = 20;

    [Header("Item Detail Panel")]
    public GameObject itemDetailPanel;
    public Image detailIcon;
    public TMP_Text detailName;
    public TMP_Text detailDesc;
    public TMP_Text detailCategory;

    [Header("Action Buttons")]
    public Button useButton;
    public TMP_Text useButtonText;
    public Button discardButton;

    [Header("Detail Close Button")]
    public Button detailCloseButton;

    [Header("Confirm Panel")]
    public GameObject confirmPanel;
    public TMP_Text confirmText;
    public Button yesButton;
    public Button noButton;

    // ─── State ───────────────────────────────────────────────────────

    private bool isInventoryOpen = false;
    private ItemData selectedItem;
    private ItemCategory currentTab = ItemCategory.Item;
    private List<InventorySlot> activeSlots = new List<InventorySlot>();
    private InventorySlot selectedSlot;

    // ─── Confirm Action Type ─────────────────────────────────────────

    private enum ConfirmAction { Use, Discard }
    private ConfirmAction pendingAction;

    public bool IsOpen => isInventoryOpen;

    // ─── Lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        if (inventoryPanel) inventoryPanel.SetActive(false);
        HideConfirm();
        HideDetails();

        // Tab buttons
        if (cluesTabButton != null)
        {
            cluesTabButton.onClick.RemoveAllListeners();
            cluesTabButton.onClick.AddListener(() => SwitchTab(ItemCategory.Clue));
        }
        if (itemsTabButton != null)
        {
            itemsTabButton.onClick.RemoveAllListeners();
            itemsTabButton.onClick.AddListener(() => SwitchTab(ItemCategory.Item));
        }

        // Detail close
        if (detailCloseButton != null)
        {
            detailCloseButton.onClick.RemoveAllListeners();
            detailCloseButton.onClick.AddListener(() =>
            {
                HideConfirm();
                HideDetails();
            });
        }

        // Discard button
        if (discardButton != null)
        {
            discardButton.onClick.RemoveAllListeners();
            discardButton.onClick.AddListener(OpenConfirmDiscard);
        }
    }

    // ─── Open / Close ────────────────────────────────────────────────

    public void ToggleInventory() => ToggleInventory(!isInventoryOpen);
    public void OpenInventory() => ToggleInventory(true);
    public void CloseInventory() => ToggleInventory(false);

    void ToggleInventory(bool open)
    {
        isInventoryOpen = open;
        if (inventoryPanel) inventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            SwitchTab(currentTab);
            HideDetails();
            HideConfirm();
        }
        else
        {
            HideDetails();
            HideConfirm();
        }
    }

    // ─── Tab Switching ───────────────────────────────────────────────

    void SwitchTab(ItemCategory tab)
    {
        currentTab = tab;
        UpdateTabVisuals();
        UpdateGrid();
        HideDetails();
        HideConfirm();
    }

    void UpdateTabVisuals()
    {
        if (cluesTabButton != null)
        {
            var colors = cluesTabButton.colors;
            colors.normalColor = currentTab == ItemCategory.Clue ? activeTabColor : inactiveTabColor;
            cluesTabButton.colors = colors;

            // Also try to update text/image directly for immediate feedback
            var img = cluesTabButton.GetComponent<Image>();
            if (img != null) img.color = currentTab == ItemCategory.Clue ? activeTabColor : inactiveTabColor;
        }

        if (itemsTabButton != null)
        {
            var img = itemsTabButton.GetComponent<Image>();
            if (img != null) img.color = currentTab == ItemCategory.Item ? activeTabColor : inactiveTabColor;
        }
    }

    // ─── Grid Population ─────────────────────────────────────────────

    void UpdateGrid()
    {
        // Clear old slots
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        activeSlots.Clear();
        selectedSlot = null;

        // Get items for current tab
        List<ItemData> tabItems = playerInventory.GetItemsByCategory(currentTab);

        // Create fixed number of slots
        for (int i = 0; i < gridSlotCount; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, slotContainer);
            var slot = newSlotObj.GetComponent<InventorySlot>();

            if (slot == null)
            {
                slot = newSlotObj.AddComponent<InventorySlot>();
            }

            if (i < tabItems.Count)
            {
                // Filled slot
                slot.SetItem(tabItems[i]);

                // Add click handler
                Button btn = newSlotObj.GetComponent<Button>();
                if (btn == null) btn = newSlotObj.AddComponent<Button>();

                btn.onClick.RemoveAllListeners();
                int capturedIndex = i;
                InventorySlot capturedSlot = slot;
                btn.onClick.AddListener(() => OnClickSlot(capturedSlot));
            }
            else
            {
                // Empty slot
                slot.ClearSlot();
            }

            activeSlots.Add(slot);
        }
    }

    // ─── Slot Click ──────────────────────────────────────────────────

    void OnClickSlot(InventorySlot slot)
    {
        if (slot == null || slot.currentItem == null) return;

        // Deselect previous
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        // Select new
        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        selectedItem = slot.currentItem;
        ShowDetails(selectedItem);
        HideConfirm();
    }

    // ─── Detail Panel ────────────────────────────────────────────────

    void ShowDetails(ItemData item)
    {
        if (itemDetailPanel) itemDetailPanel.SetActive(true);

        if (detailIcon)
        {
            detailIcon.sprite = item.icon;
            detailIcon.enabled = item.icon != null;
        }
        if (detailName) detailName.text = item.itemName;
        if (detailDesc) detailDesc.text = item.description;
        if (detailCategory) detailCategory.text = item.category == ItemCategory.Clue ? "CLUE" : "ITEM";

        // Use button — contextual
        if (useButton)
        {
            useButton.onClick.RemoveAllListeners();

            if (item.category == ItemCategory.Clue)
            {
                // Clues get "Examine" (non-destructive)
                if (useButtonText) useButtonText.text = "Examine";
                useButton.interactable = true;
                useButton.onClick.AddListener(() => ExamineClue(item));
            }
            else
            {
                // Items get "Use" if usable
                bool canUse = item.isUsable;
                useButton.interactable = canUse;
                if (useButtonText) useButtonText.text = canUse ? "Use" : "Cannot Use";
                if (canUse) useButton.onClick.AddListener(OpenConfirmUse);
            }
        }

        // Discard button — only for regular items
        if (discardButton)
        {
            discardButton.gameObject.SetActive(item.category == ItemCategory.Item);
        }
    }

    void HideDetails()
    {
        if (itemDetailPanel) itemDetailPanel.SetActive(false);

        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
        selectedItem = null;
    }

    // ─── Examine Clue ────────────────────────────────────────────────

    void ExamineClue(ItemData clue)
    {
        // For clues, "Examine" just shows the description more prominently
        // Could be expanded later with a full-screen examine view
        Debug.Log($"[Inventory] Examining clue: {clue.itemName}");
    }

    // ─── Confirm Use ─────────────────────────────────────────────────

    void OpenConfirmUse()
    {
        if (selectedItem == null) return;
        pendingAction = ConfirmAction.Use;
        ShowConfirm($"Use {selectedItem.itemName}?");
    }

    void OpenConfirmDiscard()
    {
        if (selectedItem == null) return;
        pendingAction = ConfirmAction.Discard;
        ShowConfirm($"Discard {selectedItem.itemName}?");
    }

    void ShowConfirm(string message)
    {
        if (confirmPanel) confirmPanel.SetActive(true);
        if (confirmText) confirmText.text = message;

        if (yesButton)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(ConfirmYes);
        }
        if (noButton)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(HideConfirm);
        }
    }

    void ConfirmYes()
    {
        if (selectedItem == null) return;

        switch (pendingAction)
        {
            case ConfirmAction.Use:
                bool used = playerInventory.UseItem(selectedItem);
                if (used)
                {
                    HideConfirm();
                    HideDetails();
                    UpdateGrid();
                }
                break;

            case ConfirmAction.Discard:
                playerInventory.RemoveItem(selectedItem);
                HideConfirm();
                HideDetails();
                UpdateGrid();
                break;
        }
    }

    void HideConfirm()
    {
        if (confirmPanel) confirmPanel.SetActive(false);
    }

    // ─── Layer Close (for ESC key handling) ──────────────────────────

    public bool CloseTopLayer()
    {
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            HideConfirm();
            return true;
        }

        if (itemDetailPanel != null && itemDetailPanel.activeSelf)
        {
            HideDetails();
            return true;
        }

        return false;
    }
}