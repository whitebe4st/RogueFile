using UnityEngine;

public class UniqueEvidenceItem : MonoBehaviour
{
    private ItemPickup itemPickup;

    void Start()
    {
        itemPickup = GetComponent<ItemPickup>();

        // 1. If the entire quest is already complete, this item should definitely not exist anymore.
        if (EvidenceCollectionQuest.globalQuestState == EvidenceQuestState.Completed)
        {
            Destroy(gameObject);
            return;
        }

        // 2. If the quest is NOT complete, but the player currently has THIS item in their inventory,
        // it means they picked it up, went to another scene, and came back. We must destroy it so it doesn't duplicate.
        if (itemPickup != null && itemPickup.item != null)
        {
            Inventory playerInventory = FindObjectOfType<Inventory>();
            if (playerInventory != null)
            {
                // check if player has THIS item name
                foreach (var invItem in playerInventory.items)
                {
                    if (invItem != null && (invItem.itemName == itemPickup.item.itemName || invItem.name == itemPickup.item.name))
                    {
                        // Found in inventory! Destroy the scene object to prevent infinite collecting
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }

        // 3. Sync visibility at Start: hide if NotStarted, show if InProgress
        SyncVisibility();
    }

    public void SyncVisibility()
    {
        // Only show the item if the quest is actively in progress
        bool shouldBeVisible = (EvidenceCollectionQuest.globalQuestState == EvidenceQuestState.InProgress);
        gameObject.SetActive(shouldBeVisible);
    }
}
