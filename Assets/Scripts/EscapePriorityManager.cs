using UnityEngine;
using DialogueSystem;

public class EscapePriorityManager : MonoBehaviour
{
    [Header("Refs")]
    public InventoryUI inventoryUI;
    public SignalMatchGame signalMinigame;
    public PauseMenu pauseMenu;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 1) Inventory Confirm -> Detail -> Inventory
        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            if (inventoryUI.CloseTopLayer()) return;
            inventoryUI.CloseInventory();
            return;
        }

        // 2) MiniGame
        if (signalMinigame != null && signalMinigame.IsPlaying)
        {
            signalMinigame.CancelByEsc();
            return;
        }

        // 3) Dialogue
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            DialogueManager.Instance.EndDialogue();
            return;
        }

        // 4) Pause Menu
        if (pauseMenu != null)
        {
            pauseMenu.Toggle();
        }
    }
}
