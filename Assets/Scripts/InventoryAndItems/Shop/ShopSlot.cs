using UnityEngine;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconDisplay;     
    public Text priceText;       
    public Button buyButton;
    public Text description;

    private ItemData currentItem;
    private ShopManager shopManager;

    public void Setup(ItemData item, ShopManager manager)
    {
        currentItem = item;
        shopManager = manager;

        if (currentItem != null)
        {
            iconDisplay.sprite = currentItem.icon; 
            priceText.text = currentItem.price.ToString() + " B";
            description.text = currentItem.description.ToString();

       
            buyButton.onClick.RemoveAllListeners();
        
            buyButton.onClick.AddListener(OnBuyClick);
        }
    }

    void OnBuyClick()
    {
        if (shopManager != null && currentItem != null)
        {
            shopManager.BuyItem(currentItem);
        }
    }
}