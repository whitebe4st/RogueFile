using UnityEngine;
using DialogueSystem;
using Devdog.QuestSystemPro;

public class QuestDialogueEventBridge : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("If true, this script will automatically listen to all Dialogue Events. If false, you must manually call 'HandleDialogueEvent' from an inspector UnityEvent.")]
    public bool AutoListenToDialogueEvents = true;

    private void OnEnable()
    {
        if (AutoListenToDialogueEvents && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEvent.AddListener(HandleDialogueEvent);
        }
    }

    private void OnDisable()
    {
        if (AutoListenToDialogueEvents && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEvent.RemoveListener(HandleDialogueEvent);
        }
    }

    /// <summary>
    /// Processes a string event from the Dialogue System.
    /// Expects the event key to be in the format: COMPLETE_TASK_TaskKey
    /// e.g. COMPLETE_TASK_beat_rps
    /// </summary>
    public void HandleDialogueEvent(string eventKey)
    {
        // Check if the event starts with our magic prefix
        string prefix = "COMPLETE_TASK_";
        if (!string.IsNullOrEmpty(eventKey) && eventKey.StartsWith(prefix))
        {
            // Extract the actual Task Key from the event string
            string taskKey = eventKey.Substring(prefix.Length);
            
            Debug.Log($"[QuestBridge] Heard Dialogue Event. Attempting to complete quest task with key: '{taskKey}'");
            
            CompleteQuestTask(taskKey);
        }
    }

    private void CompleteQuestTask(string taskKey)
    {
        if (QuestManager.instance == null)
        {
            Debug.LogWarning("[QuestBridge] Cannot complete task. QuestManager is not found in the scene.");
            return;
        }

        // Search through all currently Active quests
        var activeQuests = QuestManager.instance.GetQuestStates().activeQuests;
        bool taskFound = false;

        foreach (var quest in activeQuests)
        {
            var task = quest.GetTask(taskKey);
            if (task != null)
            {
                taskFound = true;
                
                // Usually task.progressCap is exactly what we need to reach to complete it length
                float targetProgress = task.progressCap > 0 ? task.progressCap : 1f;

                // Set the progress to the max
                bool success = quest.SetTaskProgress(taskKey, targetProgress);
                
                if (success)
                {
                    Debug.Log($"[QuestBridge] Successfully completed task '{taskKey}' in quest '{quest.name.message}'!");
                }
                else
                {
                    Debug.LogWarning($"[QuestBridge] Found task '{taskKey}' in quest '{quest.name.message}', but failed to set progress. " +
                                     $"Is the task already completed, or is there a sequence requirement?");
                }
            }
        }

        if (!taskFound)
        {
             Debug.Log($"[QuestBridge] No active quest has a task with the key '{taskKey}'. Player might not be on the quest yet.");
        }
    }
}
