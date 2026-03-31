using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueItemRewarder : MonoBehaviour
{
    [Serializable]
    public class Reward
    {
        public string eventKey;     
        public ItemData item;       
        public int amount = 1;      
        public bool giveOnce = true;
    }

    public Inventory playerInventory;    
    public List<Reward> rewards = new List<Reward>();

    private HashSet<string> _givenKeys = new HashSet<string>();

    private void Awake()
    {
       
        if (playerInventory == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerInventory = player.GetComponent<Inventory>();
        }
    }

    private void OnEnable()
    {
        if (DialogueSystem.DialogueManager.Instance != null)
        {
            DialogueSystem.DialogueManager.Instance.OnDialogueEvent.AddListener(HandleDialogueEvent);
        }
    }

    private void Start()
    {
        if (DialogueSystem.DialogueManager.Instance != null)
        {
            DialogueSystem.DialogueManager.Instance.OnDialogueEvent.RemoveListener(HandleDialogueEvent);
            DialogueSystem.DialogueManager.Instance.OnDialogueEvent.AddListener(HandleDialogueEvent);
        }
    }

    private void OnDisable()
    {
        if (DialogueSystem.DialogueManager.Instance != null)
        {
            DialogueSystem.DialogueManager.Instance.OnDialogueEvent.RemoveListener(HandleDialogueEvent);
        }
    }

    // ãËé DialogueManager.OnDialogueEvent àÃÕÂ¡¿Ñ§¡ìªÑ¹¹Õé
    public void HandleDialogueEvent(string key)
    {
        if (playerInventory == null) { Debug.LogWarning("Player Inventory not found."); return; }

        // ËÒ reward ·Õè key µÃ§
        Reward r = null;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i].eventKey == key) { r = rewards[i]; break; }
        }
        if (r == null) return;

        if (r.giveOnce && _givenKeys.Contains(key)) return;
        if (r.item == null) { Debug.LogWarning($"Reward '{key}' has no item."); return; }

        int count = Mathf.Max(1, r.amount);
        for (int i = 0; i < count; i++)
            playerInventory.AddItem(r.item); // ãªé¢Í§à´ÔÁã¹ Inventory :contentReference[oaicite:3]{index=3}

        _givenKeys.Add(key);
    }
}
