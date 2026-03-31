using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace EvidenceSystem
{
    public class HypothesisController : MonoBehaviour
    {
        [System.Serializable]
        public class Hypothesis
        {
            public string hypothesisName;
            public List<string> requiredEvidenceIDs;
            [TextArea] public string gameStateUpdate; // Text sent to AI
            public bool isFormed = false;
        }

        [Header("Deduction Rules")]
        public List<Hypothesis> hypothesisLibrary = new List<Hypothesis>();

        [Header("Events")]
        [Tooltip("Triggered when a specific deduction is formed. Passes the Hypothesis Name.")]
        public UnityEvent<string> OnHypothesisBreakthrough;

        private void Start()
        {
            // Optional: Subscribe to some event if you want auto-checking
        }

        /// <summary>
        /// Checks all hypotheses to see if the latest connections satisfied any requirements.
        /// </summary>
        public void CheckAllHypotheses()
        {
            if (EvidenceManager.Instance == null) return;
            
            DifyManager dify = FindObjectOfType<DifyManager>();

            foreach (var h in hypothesisLibrary)
            {
                if (h.isFormed) continue;

                // For a hypothesis to be formed, we check if ALL its required evidence IDs 
                // have been logicalyl connected in the EvidenceManager.
                bool allMet = true;
                foreach (string id in h.requiredEvidenceIDs)
                {
                    // If any node in the list hasn't met ITS own internal requirements 
                    // (which are checked by EvidenceManager), we might wait.
                    // Or, more simply, we check if they are all linked together.
                    if (!EvidenceManager.Instance.IsHypothesisFormed(id))
                    {
                        allMet = false;
                        break;
                    }
                }

                if (allMet)
                {
                    h.isFormed = true;
                    OnHypothesisBreakthrough?.Invoke(h.hypothesisName);
                    UpdateGameState(dify, h.gameStateUpdate);

                    // Sync to Global State (Internal Logic)
                    if (GlobalStateManager.Instance != null) GlobalStateManager.Instance.NotifyStateChange();
                }
            }
        }

        private void UpdateGameState(DifyManager dify, string update)
        {
            if (string.IsNullOrEmpty(update)) return;
            
            // Append the new conclusion to the global state
            dify.globalGameState += "\n- [PLAYER DISCOVERY]: " + update;
            Debug.Log($"[Hypothesis] New State Found and Synced: {update}");
            
            // Optional: Trigger a UI popup or sound "Breakthrough!"
        }
    }
}
