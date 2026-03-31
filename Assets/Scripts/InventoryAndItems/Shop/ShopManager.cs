using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    public bool isShopOpen = false;

    [Header("UI References")]
    public GameObject shopPanel;
    public Transform shopContent;
    public GameObject shopSlotPrefeb;
    public Text goldDisplay;

    [Header("Shop Items")]
    public List<ItemData> itemsForSale;

    private Inventory playerInventory;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenShop(Inventory inventory)
    {
        isShopOpen = true;
        playerInventory = inventory;
        shopPanel.SetActive(true);
        UpdateShopUI();
    }

    public void CloseShop()
    {
        isShopOpen = false;
        shopPanel.SetActive(false);
    }

    void UpdateShopUI()
    {
        goldDisplay.text = "Gold: " + playerInventory.gold;

        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }

        foreach(ItemData item in itemsForSale)
        {
            GameObject slot = Instantiate(shopSlotPrefeb, shopContent);
            slot.GetComponent<ShopSlot>().Setup(item, this);
        }
    }

    public void BuyItem(ItemData item)
    {
        if (playerInventory.SpendGold(item.price))
        {
            playerInventory.AddItem(item);
            UpdateShopUI();
            Debug.Log("Bought: " + item.itemName + " for " + item.price);
        }else
        {
            Debug.Log("Not enough gold!");
        }
    }



}
