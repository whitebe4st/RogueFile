using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// Attach this script to any GameObject in a scene where you want an AI / Dialogue interaction to start automatically as soon as the level loads.
    /// </summary>
    public class DialogueAutoStarter : MonoBehaviour
    {
        [Tooltip("The dialogue graph to start automatically when the scene loads.")]
        public DialogueGraph StartingGraph;

        [Tooltip("The specific dialogue node to start (starts from the very beginning of the graph if left blank).")]
        public DialogueNode StartingNode;

        [Tooltip("Delay in seconds before the dialogue pops up. Useful to let the scene fade in first.")]
        public float startDelay = 0.5f;

        private void Start()
        {
            Invoke(nameof(ExecuteStart), startDelay);
        }

        private void ExecuteStart()
        {
            if (DialogueManager.Instance != null)
            {
                if (StartingNode != null)
                {
                    DialogueManager.Instance.StartDialogue(StartingNode);
                }
                else if (StartingGraph != null)
                {
                    DialogueNode startNode = StartingGraph.GetStartNode();
                    if (startNode != null)
                    {
                        DialogueManager.Instance.StartDialogue(startNode);
                    }
                    else
                    {
                        Debug.LogWarning("[DialogueAutoStarter] The assigned DialogueGraph has no valid start node.");
                    }
                }
                else
                {
                    Debug.LogWarning("[DialogueAutoStarter] Cannot auto-start dialogue. Both StartingGraph and StartingNode are missing in the Inspector.");
                }
            }
            else
            {
                Debug.LogWarning("[DialogueAutoStarter] DialogueManager not found in the scene! Cannot auto-start dialogue.");
            }
        }
    }
}
