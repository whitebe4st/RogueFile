using UnityEngine;

public enum ItemCategory
{
    Item,
    Clue
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public int price;

    [Header("Category")]
    public ItemCategory category = ItemCategory.Item;

    [Header("Use Settings")]
    public bool isUsable = false;     
    public bool consumeOnUse = true;  
}
