using UnityEngine;
using DialogueSystem;
using Devdog.QuestSystemPro;

public class QuestAwareDialogueTrigger : DialogueTrigger
{
    [System.Serializable]
    public struct QuestDialogueOverride
    {
        [Tooltip("The Quest to check the status of.")]
        public Quest targetQuest;
        
        [Tooltip("The required status for this override to play (e.g., Active, Completed).")]
        public QuestStatus requiredStatus;
        
        [Tooltip("The Dialogue Graph to play if the quest condition is met.")]
        public DialogueGraph graphToPlay;
    }

    [Header("Quest-Specific Dialogues")]
    [Tooltip("Overrides are checked from top to bottom. The FIRST one that matches the requirement will play. Leave the core 'Starting Graph' field above as the fallback/default dialogue.")]
    public QuestDialogueOverride[] questDialogues;

    public override DialogueGraph GetStartingGraph()
    {
        // Safety check to ensure the Quest Manager is actually active in the scene
        if (QuestManager.instance != null && questDialogues != null)
        {
            // Go through each override one by one
            foreach (var overrideData in questDialogues)
            {
                // If a quest is slotted in, check its status
                if (overrideData.targetQuest != null)
                {
                    if (overrideData.targetQuest.status == overrideData.requiredStatus)
                    {
                        // Found a match! Return this specific graph instead of the default
                        return overrideData.graphToPlay; 
                    }
                }
            }
        }
        
        // If no overrides matched (or QuestManager is missing), fall back to the default Graph in the parent class
        return base.GetStartingGraph();
    }
}
