using UnityEngine;

namespace DialogueSystem
{
    public class DialogueTrigger : MonoBehaviour
    {
        // Simple static tracking of the trigger the player is currently in
        public static DialogueTrigger Current;
        
        [Tooltip("The dialogue graph to start when triggered (preferred for xNode).")]
        public DialogueGraph StartingGraph;

        [Tooltip("The specific dialogue node to start (if you don't want to start from the beginning of the graph).")]
        public DialogueNode StartingNode;

        [Tooltip("If true, triggers automatically when the player enters the trigger zone.")]
        public bool TriggerOnEnter = false;

        [Tooltip("Tag of the object that can trigger the dialogue (e.g. 'Player').")]
        public string TriggerTag = "Player";

        public virtual DialogueGraph GetStartingGraph()
        {
            return StartingGraph;
        }

        public virtual DialogueNode GetStartingNode()
        {
            return StartingNode;
        }

        /// <summary>
        /// Call this method via UnityEvent, button click, or another script to start the dialogue.
        /// </summary>
        public void TriggerDialogue()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueGraph graphToPlay = GetStartingGraph();
                DialogueNode nodeToPlay = GetStartingNode();

                if (graphToPlay != null)
                {
                    DialogueNode startNode = graphToPlay.GetStartNode();
                    if (startNode != null)
                    {
                        DialogueManager.Instance.StartDialogue(startNode);
                    }
                    else
                    {
                        Debug.LogWarning("DialogueTrigger: Cannot start dialogue. The DialogueGraph has no valid start node.");
                    }
                }
                else if (nodeToPlay != null)
                {
                    DialogueManager.Instance.StartDialogue(nodeToPlay);
                }
                else
                {
                    Debug.LogWarning("DialogueTrigger: Cannot start dialogue. Both StartingGraph and StartingNode are null.");
                }
            }
            else
            {
                Debug.LogWarning("DialogueTrigger: Cannot start dialogue. Manager is null.");
            }
        }

        [Header("Interaction")]
        [Tooltip("The UI object (e.g., World Space Canvas or Sprite) that says 'Press E'.")]
        public GameObject InteractPrompt;
        
        [Tooltip("Key to press to interact.")]
        public KeyCode InteractKey = KeyCode.E;

        public bool IsPlayerInRange { get; private set; } = false;

        private void UpdatePromptState(bool show)
        {
            if (InteractPrompt == null) return;

            UniversalUIAnimator animator = InteractPrompt.GetComponent<UniversalUIAnimator>();
            if (animator != null)
            {
                if (show) animator.Show();
                else animator.Hide();
            }
            else
            {
                InteractPrompt.SetActive(show);
            }
        }

        protected virtual void Start()
        {
            if (InteractPrompt != null)
                InteractPrompt.SetActive(false);
        }

        protected virtual void Update()
        {
            // Suppress interaction if any dialogue is already active
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                if (InteractPrompt != null && InteractPrompt.activeSelf)
                    UpdatePromptState(false);
                return;
            }

            if (IsPlayerInRange)
            {
                // Ensure prompt is visible if we are in range and dialogue is NOT active
                if (InteractPrompt != null && !InteractPrompt.activeSelf)
                    UpdatePromptState(true);

                if (Input.GetKeyDown(InteractKey))
                {
                    TriggerDialogue();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(TriggerTag))
            {
                if (TriggerOnEnter)
                {
                    TriggerDialogue();
                }
                else
                {
                    IsPlayerInRange = true;
                    UpdatePromptState(true);
                        
                    // Register as active trigger (Simple static reference for now, or just FindObjectOfType)
                    DialogueTrigger.Current = this; 
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(TriggerTag))
            {
                IsPlayerInRange = false;
                if (Current == this) Current = null;
                
                UpdatePromptState(false);
                
                // Optional: Close dialogue if player walks away?
                // DialogueManager.Instance.EndDialogue(); 
            }
        }
    }
}
