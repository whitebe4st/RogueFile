using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EvidenceSystem
{
    public class EvidenceManager : MonoBehaviour
    {
        public static EvidenceManager Instance { get; private set; }

        [Header("Evidence Library")]
        [Tooltip("Assign all EvidenceNode assets here so the manager can track them by ID.")]
        public List<EvidenceNode> allEvidence = new List<EvidenceNode>();

        // Observer Pattern: Event triggered when evidence is discovered
        public delegate void EvidenceDiscoveredHandler(string nodeID);
        public event EvidenceDiscoveredHandler OnEvidenceDiscovered;

        // Tracks which evidence IDs are connected on the board. 
        // Key: Node ID, Value: List of connected Node IDs.
        private Dictionary<string, HashSet<string>> currentConnections = new Dictionary<string, HashSet<string>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Marks an evidence node as discovered by its unique ID.
        /// </summary>
        public void DiscoverEvidence(string nodeID)
        {
            EvidenceNode node = allEvidence.Find(e => e.nodeID == nodeID);
            if (node != null)
            {
                if (!node.isDiscovered)
                {
                    node.isDiscovered = true;
                    Debug.Log($"[EvidenceManager] Evidence Discovered: {node.title}");
                    OnEvidenceDiscovered?.Invoke(nodeID);

                    // Sync to Global State
                    if (GlobalStateManager.Instance != null) GlobalStateManager.Instance.NotifyStateChange();
                }
            }
            else
            {
                Debug.LogWarning($"[EvidenceManager] Could not find evidence with ID: {nodeID}");
            }
        }

        /// <summary>
        /// Adds a connection between two nodes on the investigation board.
        /// </summary>
        public void AddConnection(string idA, string idB)
        {
            AddDirectionalLink(idA, idB);
            AddDirectionalLink(idB, idA);
            Debug.Log($"[EvidenceManager] Connection added between {idA} and {idB}");

            // After any connection, check if a Conclusion has been earned
            CheckAllDeductions();
        }

        /// <summary>
        /// Scans all EvidenceNodes. If a 'Conclusion' type is hidden but its requirements 
        /// are met on the board, unlock it automatically.
        /// </summary>
        public void CheckAllDeductions()
        {
            bool anyNewDiscovery = false;

            foreach (var node in allEvidence)
            {
                // We only care about Conclusions that aren't discovered yet
                if (node.nodeType == NodeType.Conclusion && !node.isDiscovered)
                {
                    if (IsHypothesisFormed(node.nodeID))
                    {
                        Debug.Log($"[EvidenceManager] Auto-Deduction reached: {node.title}");
                        DiscoverEvidence(node.nodeID);
                        anyNewDiscovery = true;
                    }
                }
            }

            // If a conclusion was found, it might lead to ANOTHER conclusion
            if (anyNewDiscovery) CheckAllDeductions();
        }

        private void AddDirectionalLink(string from, string to)
        {
            if (!currentConnections.ContainsKey(from))
                currentConnections[from] = new HashSet<string>();
            currentConnections[from].Add(to);
        }

        /// <summary>
        /// Checks if a node has all its required connections met.
        /// </summary>
        public bool IsHypothesisFormed(string nodeID)
        {
            EvidenceNode node = allEvidence.Find(e => e.nodeID == nodeID);
            if (node == null || node.requiredConnections.Count == 0) return false;

            if (!currentConnections.ContainsKey(nodeID)) return false;

            HashSet<string> connectionsForNode = currentConnections[nodeID];
            
            // Check if every required connection ID is present in the current connections map
            foreach (string reqID in node.requiredConnections)
            {
                if (!connectionsForNode.Contains(reqID))
                    return false;
            }

            Debug.Log($"[EvidenceManager] Hypothesis Confirmed for: {node.title}!");
            return true;
        }

        /// <summary>
        /// Resets all discovery flags (for testing or game restart).
        /// </summary>
        public void ResetDiscovery()
        {
            foreach (var node in allEvidence)
                node.isDiscovered = false;
            currentConnections.Clear();
        }
    }
}
