using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Text;
using EvidenceSystem;

public class GlobalStateManager : MonoBehaviour
{
    public static GlobalStateManager Instance { get; private set; }

    [Header("Managers")]
    public DifyManager difyManager;
    public EvidenceManager evidenceManager;
    public HypothesisController hypothesisController;
    public Inventory playerInventory;

    [Header("Events")]
    [Tooltip("Triggered whenever any game state (Items, Clues, Hypotheses) changes.")]
    public UnityEvent OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-find dependencies if not assigned in inspector
        if (difyManager == null) difyManager = FindObjectOfType<DifyManager>();
        if (evidenceManager == null) evidenceManager = FindObjectOfType<EvidenceManager>();
        if (hypothesisController == null) hypothesisController = FindObjectOfType<HypothesisController>();
        if (playerInventory == null) playerInventory = FindObjectOfType<Inventory>();
        
        // Final sanity check: The game works even if many of these are null!
        NotifyStateChange();
    }

    /// <summary>
    /// Call this whenever a system updates its local data.
    /// </summary>
    public void NotifyStateChange()
    {
        OnStateChanged?.Invoke();
        SyncStateToAI();
    }

    /// <summary>
    /// Generates a comprehensive summary of the current game state to send to AI.
    /// </summary>
    public string GetGlobalStateSummary()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== CURRENT GAME STATE ===");

        // 1. Inventory State
        if (playerInventory != null)
        {
            sb.AppendLine("[Items Carried]:");
            if (playerInventory.items.Count > 0)
            {
                foreach (var item in playerInventory.items)
                {
                    sb.AppendLine($"- {item.itemName}");
                }
            }
            else
            {
                sb.AppendLine("- None");
            }
            sb.AppendLine($"[Gold]: {playerInventory.gold}");
        }

        // 2. Evidence Discovery
        if (evidenceManager != null)
        {
            sb.AppendLine("[Discovered Clues]:");
            var discovered = evidenceManager.allEvidence.FindAll(e => e.isDiscovered);
            if (discovered.Count > 0)
            {
                foreach (var clue in discovered)
                {
                    sb.AppendLine($"- {clue.title}");
                }
            }
            else
            {
                sb.AppendLine("- No clues found yet.");
            }
        }

        // 3. Hypotheses / Conclusions
        if (hypothesisController != null)
        {
            sb.AppendLine("[Conclusions Reached]:");
            var formed = hypothesisController.hypothesisLibrary.FindAll(h => h.isFormed);
            if (formed.Count > 0)
            {
                foreach (var h in formed)
                {
                    sb.AppendLine($"- {h.hypothesisName}");
                }
            }
            else
            {
                sb.AppendLine("- No major breakthroughs yet.");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Syncs the current consolidated state to the Dify AI's persistent memory.
    /// </summary>
    public void SyncStateToAI()
    {
        if (difyManager == null) return;

        string summary = GetGlobalStateSummary();
        difyManager.globalGameState = summary;
        Debug.Log("[GlobalState] Syncing unified state to AI memory.");
    }
}
