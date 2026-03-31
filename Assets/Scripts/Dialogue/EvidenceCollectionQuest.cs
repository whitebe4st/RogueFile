using System.Collections.Generic;
using UnityEngine;
using DialogueSystem;

public enum EvidenceQuestState
{
    NotStarted,
    InProgress,
    Completed
}

public class EvidenceCollectionQuest : DialogueTrigger
{
    // Static state so it remembers completion across scene loads
    public static EvidenceQuestState globalQuestState = EvidenceQuestState.NotStarted;

    [Header("Quest Requirements")]
    [Tooltip("The evidence items the player needs to collect (e.g., กรรไกร, สมุด, ขวดน้ำ)")]
    public List<ItemData> requiredItems = new List<ItemData>();

    [Header("Dialogue Graphs")]
    [Tooltip("Dialogue to play when giving the quest for the first time.")]
    public DialogueGraph questStartGraph;
    
    [Tooltip("Dialogue to play when the quest is active but not all items are found.")]
    public DialogueGraph questInProgressGraph;
    
    [Tooltip("Dialogue to play when all items have been collected.")]
    public DialogueGraph questCompleteGraph;

    [Header("Current State (Managed Globally)")]
    // Used for inspector display only, actual state is static
    [SerializeField] private EvidenceQuestState inspectorStateDisplay;

    protected override void Start()
    {
        base.Start();
        inspectorStateDisplay = globalQuestState;

        // If we return to this scene later and the quest is already done,
        // we should destroy the evidence item pickups from the scene so they don't respawn.
        if (globalQuestState == EvidenceQuestState.Completed)
        {
            ItemPickup[] allPickups = FindObjectsOfType<ItemPickup>();
            foreach (var pickup in allPickups)
            {
                if (pickup != null && pickup.item != null && requiredItems != null)
                {
                    foreach (var reqItem in requiredItems)
                    {
                        if (reqItem != null && (pickup.item.itemName == reqItem.itemName || pickup.item.name == reqItem.name))
                        {
                            Destroy(pickup.gameObject);
                        }
                    }
                }
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        // constantly sync display
        if (inspectorStateDisplay != globalQuestState) inspectorStateDisplay = globalQuestState;
    }

    public override DialogueGraph GetStartingGraph()
    {
        // 1. If the quest is already completed, just return complete graph
        // (Uses static flag to remember after scene change)
        if (globalQuestState == EvidenceQuestState.Completed)
        {
            return questCompleteGraph;
        }

        // 2. We need to check the player's inventory
        Inventory playerInventory = FindObjectOfType<Inventory>();
        if (playerInventory == null)
        {
            Debug.LogError("EvidenceQuest: Cannot find an Inventory script in the scene!");
            return base.GetStartingGraph();
        }

        if (requiredItems == null || requiredItems.Count == 0)
        {
            Debug.LogError("EvidenceQuest: requiredItems list is empty! Please assign the ItemData (.asset) items to the NPC script in the Unity Inspector.");
            return questStartGraph;
        }

        Debug.Log($"EvidenceQuest: Checking inventory ({playerInventory.items.Count} items) against {requiredItems.Count} required items.");

        // 3. Count how many required items the player actually has based on item name
        int itemsFound = 0;
        foreach (var reqItem in requiredItems)
        {
            bool foundThisItem = false;
            foreach (var inventoryItem in playerInventory.items)
            {
                if (inventoryItem != null && reqItem != null)
                {
                    string invName = string.IsNullOrEmpty(inventoryItem.itemName) ? "" : inventoryItem.itemName.Trim().ToLower();
                    string reqName = string.IsNullOrEmpty(reqItem.itemName) ? "" : reqItem.itemName.Trim().ToLower();
                    
                    string invAssetName = string.IsNullOrEmpty(inventoryItem.name) ? "" : inventoryItem.name.Trim().ToLower();
                    string reqAssetName = string.IsNullOrEmpty(reqItem.name) ? "" : reqItem.name.Trim().ToLower();

                    bool nameMatches = (!string.IsNullOrEmpty(invName) && invName == reqName) ||
                                       (!string.IsNullOrEmpty(invAssetName) && invAssetName == reqAssetName);

                    if (nameMatches)
                    {
                        foundThisItem = true;
                        break;
                    }
                }
            }

            if (foundThisItem)
            {
                itemsFound++;
                Debug.Log($"EvidenceQuest: Player has required item '{reqItem.itemName}'.");
            }
            else
            {
                Debug.Log($"EvidenceQuest: Player is missing item '{reqItem.itemName}'.");
            }
        }

        Debug.Log($"EvidenceQuest: Found {itemsFound} / {requiredItems.Count} items.");

        // 4. Check if we have all of them
        if (itemsFound >= requiredItems.Count && requiredItems.Count > 0)
        {
            // Quest is ready to complete!
            CompleteQuest(playerInventory);
            return questCompleteGraph;
        }

        // 5. If not complete: return Start vs InProgress based on state
        if (globalQuestState == EvidenceQuestState.NotStarted)
        {
            // First time talking, give quest, switch state to InProgress
            globalQuestState = EvidenceQuestState.InProgress;
            
            // Show any hidden evidence items in the CURRENT scene by enabling their objects
            // Use Resources.FindObjectsOfTypeAll as a safe fallback for all Unity versions to find inactive objects
            UniqueEvidenceItem[] hiddenItems = Resources.FindObjectsOfTypeAll<UniqueEvidenceItem>();
            foreach (var item in hiddenItems)
            {
                if (item != null && item.gameObject.scene.isLoaded) // ensure it's in the scene, not a prefab
                {
                    item.SyncVisibility();
                }
            }

            return questStartGraph;
        }
        else
        {
            // They came back but don't have all items yet
            return questInProgressGraph;
        }
    }

    private void CompleteQuest(Inventory playerInventory)
    {
        globalQuestState = EvidenceQuestState.Completed;
        Debug.Log("EvidenceQuest: Completed! Removing required items from inventory...");

        // Remove the evidence items from the player's inventory as requested
        foreach (var reqItem in requiredItems)
        {
            ItemData itemToRemove = null;
            foreach (var invItem in playerInventory.items)
            {
                if (invItem != null && reqItem != null)
                {
                    string invName = string.IsNullOrEmpty(invItem.itemName) ? "" : invItem.itemName.Trim().ToLower();
                    string reqName = string.IsNullOrEmpty(reqItem.itemName) ? "" : reqItem.itemName.Trim().ToLower();
                    
                    string invAssetName = string.IsNullOrEmpty(invItem.name) ? "" : invItem.name.Trim().ToLower();
                    string reqAssetName = string.IsNullOrEmpty(reqItem.name) ? "" : reqItem.name.Trim().ToLower();

                    bool nameMatches = (!string.IsNullOrEmpty(invName) && invName == reqName) ||
                                       (!string.IsNullOrEmpty(invAssetName) && invAssetName == reqAssetName);

                    if (nameMatches)
                    {
                        itemToRemove = invItem;
                        break;
                    }
                }
            }

            if (itemToRemove != null)
            {
                playerInventory.RemoveItem(itemToRemove);
                Debug.Log($"EvidenceQuest: Removed '{itemToRemove.itemName}' from inventory.");
            }
        }
    }
}
