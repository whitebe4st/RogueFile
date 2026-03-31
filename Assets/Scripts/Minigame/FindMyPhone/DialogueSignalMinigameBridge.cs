using System.Collections;
using UnityEngine;
using DialogueSystem; // ใช้ namespace เดียวกับ DialogueManager/DialogueTrigger/DialogueNode

public class DialogueSignalMinigameBridge : MonoBehaviour
{
    [Header("Event Key from DialogueNode")]
    public string triggerKey = "START_SIGNAL_MINIGAME";

    [Header("Refs")]
    public DialogueTrigger ownerTrigger;   // NPC ตัวนี้ (DialogueTrigger)
    public SignalMatchGame minigame;       // ตัวมินิเกม
    public DialogueNode afterWinNode;      // Node ที่พูดต่อหลังชนะ (เช่น "ขอบคุณ")
    public DialogueNode afterCancelNode;   // Node หลังยกเลิก (optional)

    [Header("Options")]
    public bool requireCurrentOwner = false; // ถ้า true จะเช็ค DialogueTrigger.Current ต้องเป็น ownerTrigger

    private bool waiting;

    void Awake()
    {
        if (ownerTrigger == null) ownerTrigger = GetComponent<DialogueTrigger>();
    }

    // เอาไปผูกกับ DialogueManager.OnDialogueEvent (string)
    public void HandleDialogueEvent(string key)
    {
        Debug.Log($"[Bridge] got key='{key}', triggerKey='{triggerKey}', waiting={waiting}, minigame={(minigame != null)}");

        if (key != triggerKey) return;
        if (minigame == null) { Debug.LogWarning("[Bridge] minigame is null"); return; }
        if (waiting) return;

        if (requireCurrentOwner && ownerTrigger != null && DialogueTrigger.Current != ownerTrigger)
        {
            Debug.LogWarning("[Bridge] Current dialogue owner mismatch, ignoring.");
            return;
        }

        waiting = true;

        // ปิดหน้าต่างบทสนทนา
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.EndDialogue();

        // กันกดเริ่มคุยซ้อนระหว่างเล่น
        if (ownerTrigger) ownerTrigger.enabled = false;

        // subscribe one-shot
        minigame.OnWin.AddListener(OnMinigameWin);
        minigame.OnCancel.AddListener(OnMinigameCancel);

        StartCoroutine(StartMinigameNextFrame());
    }

    IEnumerator StartMinigameNextFrame()
    {
        // รอ 1 เฟรมให้ UI dialogue ปิดก่อน
        yield return null;

        Debug.Log("[Bridge] Starting minigame...");
        minigame.StartMinigame();
    }

    void Cleanup()
    {
        if (minigame != null)
        {
            minigame.OnWin.RemoveListener(OnMinigameWin);
            minigame.OnCancel.RemoveListener(OnMinigameCancel);
        }

        if (ownerTrigger) ownerTrigger.enabled = true;
        waiting = false;
    }

    void OnMinigameWin()
    {
        Debug.Log("[Bridge] Minigame WIN");
        Cleanup();

        if (afterWinNode != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(afterWinNode);
    }

    void OnMinigameCancel()
    {
        Debug.Log("[Bridge] Minigame CANCEL");
        Cleanup();

        if (afterCancelNode != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(afterCancelNode);
    }
}
