using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();
    public int gold = 100;
    public event Action OnChanged;

    // Static variables to persist data across scene loads automatically
    private static List<ItemData> persistentItems;
    private static int persistentGold = -1;

    private void Awake()
    {
        // On very first load, initialize static data from the Inspector's starting values
        if (persistentItems == null)
        {
            persistentItems = new List<ItemData>(items);
            persistentGold = gold;
        }

        // Connect this instance's local fields directly to the persistent data in memory
        items = persistentItems;
        gold = persistentGold;
    }

    // ─── Category Helpers ────────────────────────────────────────────

    /// <summary>Returns all items matching the given category.</summary>
    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        return items.Where(i => i != null && i.category == category).ToList();
    }

    /// <summary>Returns all clues.</summary>
    public List<ItemData> GetClues() => GetItemsByCategory(ItemCategory.Clue);

    /// <summary>Returns all regular items.</summary>
    public List<ItemData> GetItems() => GetItemsByCategory(ItemCategory.Item);

    // ─── Core Operations ─────────────────────────────────────────────

    public void AddItem(ItemData newItem)
    {
        items.Add(newItem);
        Debug.Log("Item received: " + newItem.itemName);

        // Sync to Global State
        if (GlobalStateManager.Instance != null)
        {
            GlobalStateManager.Instance.NotifyStateChange();
        }

        OnChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount > 0)
        {
            gold += amount;
            persistentGold = gold; // Sync to static
            Debug.Log($"Earned {amount} Gold! Total: {gold}");
            
            if (GlobalStateManager.Instance != null)
            {
                GlobalStateManager.Instance.NotifyStateChange();
            }
            OnChanged?.Invoke();
        }
    }

    // check got items for quest
    public bool HasItem(ItemData itemToCheck)
    {
        return items.Contains(itemToCheck);
    }

    public void RemoveItem(ItemData itemToRemove)
    {
        if (items.Contains(itemToRemove))
        {
            items.Remove(itemToRemove);
            Debug.Log("Item removed: " + itemToRemove.itemName);

            if (GlobalStateManager.Instance != null)
            {
                GlobalStateManager.Instance.NotifyStateChange();
            }

            OnChanged?.Invoke();
        }
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            persistentGold = gold; // Sync to static
            return true;
        }
        else 
        { 
            return false; 
        }
    }

    public bool UseItem(ItemData item)
    {
        if (item == null) return false;
        if (!items.Contains(item)) return false;
        if (!item.isUsable)
        {
            Debug.Log($"Item '{item.itemName}' cannot be used.");
            return false;
        }

        // Logic for item effects (heal/quest/buff)
        Debug.Log("Used item: " + item.itemName);

        string lowerName = item.itemName.ToLower();
        if (lowerName.Contains("medkit"))
        {
            CharacterStats playerStats = FindObjectOfType<PlayerCombat>()?.GetComponent<CharacterStats>();
            if (playerStats != null)
            {
                playerStats.Heal(50);
            }
        }
        else if (lowerName.Contains("proteinbar") || lowerName.Contains("protein bar"))
        {
            PlayerCombat playerCombat = FindObjectOfType<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.meleeDamage += 3;
                Debug.Log($"Increased ATK by 3. Current ATK: {playerCombat.meleeDamage}");
            }
        }

        if (item.consumeOnUse)
        {
            items.Remove(item);
            OnChanged?.Invoke();
        }

        return true;
    }
}
