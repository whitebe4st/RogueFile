using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameHUD — the single in-game HUD controller.
/// 
/// HOW TO USE IN SCENE:
///   1. Attach this to a persistent Canvas or HUD GameObject in your gameplay scene.
///   2. Wire up the inspector references below.
///   3. Hook your HUD buttons (Inventory, Pause) to the public methods via OnClick.
/// 
/// KEYBOARD SHORTCUTS (handled here, not in InventoryUI/PauseMenu):
///   I   — Toggle Inventory
///   Tab — Toggle Pause / Main HUD panel
///   Escape is handled by EscapePriorityManager (no conflict).
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Controllers (assign in Inspector)")]
    public InventoryUI inventoryUI;
    public PauseMenu   pauseMenu;

    [Header("Optional: HUD Panel GameObjects to show/hide")]
    [Tooltip("The root GameObject of your HUD bar (optional, for show/hide during cutscenes etc.)")]
    public GameObject hudRoot;

    // ── Keyboard shortcuts ───────────────────────────────────────────────────

    private void Update()
    {
        // I → inventory
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("[GameHUD] KeyCode.I pressed. Calling ToggleInventory()...");
            ToggleInventory();
        }

        // Tab → pause/resume
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePause();
        }
    }

    // ── Public button methods (wire these to your UI buttons via OnClick) ────

    /// <summary>Called by the Inventory HUD button.</summary>
    public void ToggleInventory()
    {
        Debug.Log("[GameHUD] ToggleInventory() invoked.");
        if (inventoryUI == null)
        {
            Debug.LogWarning("[GameHUD] ERROR: InventoryUI is not assigned in the GameHUD inspector!");
            return;
        }

        Debug.Log("[GameHUD] Calling inventoryUI.ToggleInventory()...");
        inventoryUI.ToggleInventory();
    }

    /// <summary>Called by the Menu / Pause HUD button.</summary>
    public void TogglePause()
    {
        if (pauseMenu == null)
        {
            Debug.LogWarning("[GameHUD] PauseMenu is not assigned.");
            return;
        }

        pauseMenu.Toggle();
    }

    // ── HUD visibility helpers (call these from cutscene / dialogue events) ──

    /// <summary>Hide the entire HUD bar (e.g. during cutscenes).</summary>
    public void HideHUD()
    {
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    /// <summary>Restore the HUD bar.</summary>
    public void ShowHUD()
    {
        if (hudRoot != null) hudRoot.SetActive(true);
    }
}
