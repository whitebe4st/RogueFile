using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;

public class RewardUIManager : MonoBehaviour
{
    public static RewardUIManager instance;

    [Header("UI References")]
    public GameObject rewardPanel;
    public Text lootText;

    [Header("Scene Transition Settings")]
    [Tooltip("Leave empty to stay in current scene. Otherwise, transition to this scene name after receiving reward.")]
    public string nextSceneAfterReward;

    private int pendingMoney = 0;
    private List<ItemData> pendingItems = new List<ItemData>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void AccumulateReward(int money, List<ItemData> droppedItems)
    {
        pendingMoney += money;
        if (droppedItems != null)
        {
            pendingItems.AddRange(droppedItems);
        }
    }

    public void ShowAccumulatedReward()
    {
        rewardPanel.SetActive(true); 

        string message = "Rewards:\n";
        if (pendingMoney > 0) message += "Gold: " + pendingMoney + " G\n";
        
        // Group items by name to format like 'ItemName x2'
        Dictionary<string, int> itemCounts = new Dictionary<string, int>();
        foreach (ItemData item in pendingItems)
        {
            if (item != null)
            {
                if (itemCounts.ContainsKey(item.itemName))
                    itemCounts[item.itemName]++;
                else
                    itemCounts[item.itemName] = 1;
            }
        }

        foreach (var kvp in itemCounts)
        {
            message += "- " + kvp.Key + (kvp.Value > 1 ? " x" + kvp.Value : "") + "\n";
        }

        lootText.text = message;

        Time.timeScale = 0f;
    }

    public void CollectReward()
    {
        rewardPanel.SetActive(false); 
        Time.timeScale = 1f; 

        Inventory playerInv = FindObjectOfType<Inventory>();
        if (playerInv != null)
        {
            if (pendingMoney > 0)
            {
                playerInv.AddGold(pendingMoney);
            }

            if (pendingItems != null && pendingItems.Count > 0)
            {
                foreach (ItemData item in pendingItems)
                {
                    playerInv.AddItem(item);
                }
            }
        }
        else
        {
            Debug.LogWarning("No Inventory found in the scene to give rewards to!");
        }

        // Reset
        pendingMoney = 0;
        pendingItems.Clear();

        Debug.Log("Reward Collected!");

        // Move to the next scene if a name is provided
        if (!string.IsNullOrEmpty(nextSceneAfterReward))
        {
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadScene(nextSceneAfterReward);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneAfterReward);
            }
        }
    }
}