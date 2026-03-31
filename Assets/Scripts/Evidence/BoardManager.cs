using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace EvidenceSystem
{
    public class BoardManager : MonoBehaviour
    {
        [Header("UI References")]
        public Transform cardContainer; // The parent panel for evidence cards (can have Layout Group)
        public Transform lineContainer; // The parent panel for connection lines (must NOT have Layout Group)
        public GameObject evidencePrefab; // Prefab representing an evidence card
        public GameObject stringPrefab;   // Prefab for the red string (Image with specific setup)

        [Header("State")]
        private List<EvidenceNode> discoveredClues = new List<EvidenceNode>();
        private Dictionary<string, RectTransform> spawnedNodes = new Dictionary<string, RectTransform>();
        private HashSet<string> activeConnections = new HashSet<string>();

        private void Start()
        {
            // Subscribe to discovery events (Observer Pattern)
            if (EvidenceManager.Instance != null)
            {
                EvidenceManager.Instance.OnEvidenceDiscovered += OnEvidenceFound;
                
                // RESTORE STATE: Refresh board with anything already discovered (e.g. from previous editor session)
                RestoreDiscoveredState();
            }
        }

        private void RestoreDiscoveredState()
        {
            if (EvidenceManager.Instance == null) return;

            // Important: Use a temp list or copy if EvidenceManager has many nodes
            foreach (var node in EvidenceManager.Instance.allEvidence)
            {
                if (node.isDiscovered)
                {
                    OnEvidenceFound(node.nodeID);
                }
            }
        }

        private void OnDestroy()
        {
            if (EvidenceManager.Instance != null)
            {
                EvidenceManager.Instance.OnEvidenceDiscovered -= OnEvidenceFound;
            }
        }

        /// <summary>
        /// Called when evidence is discovered. Spawns the visual representation.
        /// </summary>
        public void OnEvidenceFound(string id)
        {
            EvidenceNode node = EvidenceManager.Instance.allEvidence.Find(e => e.nodeID == id);
            if (node == null) return;

            if (!discoveredClues.Contains(node))
            {
                discoveredClues.Add(node);
                SpawnEvidenceVisual(node);

                // --- AUTO CONNECT LOGIC ---
                // After spawning, check if this new clue has predefined connections to already revealed clues
                foreach (var other in discoveredClues)
                {
                    if (other.nodeID == id) continue;

                    // If node requires other, or other requires node
                    if (node.requiredConnections.Contains(other.nodeID) || other.requiredConnections.Contains(id))
                    {
                        Debug.Log($"[BoardManager] Auto-connecting {id} to {other.nodeID}");
                        DrawConnection(id, other.nodeID);
                    }
                }
            }
        }

        private void SpawnEvidenceVisual(EvidenceNode node)
        {
            if (cardContainer == null || evidencePrefab == null) return;

            GameObject card = Instantiate(evidencePrefab, cardContainer);
            RectTransform rect = card.GetComponent<RectTransform>();
            spawnedNodes[node.nodeID] = rect;

            // Setup card data
            EvidenceCardUI cardUI = card.GetComponent<EvidenceCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(node);
            }
            else
            {
                Debug.LogWarning($"[BoardManager] Prefab for {node.title} is missing EvidenceCardUI script!");
            }
            
            Debug.Log($"[BoardManager] Spawned visual for: {node.title}");
        }

        /// <summary>
        /// Visualizes a red string between two nodes.
        /// </summary>
        public void DrawConnection(string idA, string idB)
        {
            // Consistent key regardless of order
            string key = string.Compare(idA, idB) < 0 ? $"{idA}_{idB}" : $"{idB}_{idA}";
            if (activeConnections.Contains(key)) return;

            if (!spawnedNodes.ContainsKey(idA) || !spawnedNodes.ContainsKey(idB))
            {
                Debug.LogWarning($"[BoardManager] Cannot draw connection: one or both nodes are missing ({idA} or {idB}).");
                return;
            }
            
            activeConnections.Add(key);

            RectTransform rectA = spawnedNodes[idA];
            RectTransform rectB = spawnedNodes[idB];

            if (stringPrefab != null && lineContainer != null)
            {
                GameObject lineObj = Instantiate(stringPrefab, lineContainer);
                lineObj.name = $"Connection_{key}";
                lineObj.transform.localScale = Vector3.one; // Safety: Ensure UI scale is 1
                
                ProceduralUILine line = lineObj.GetComponent<ProceduralUILine>();
                if (line == null) line = lineObj.AddComponent<ProceduralUILine>(); // Fallback if missing component

                if (line != null)
                {
                    line.color = Color.red; // Pure red line
                    line.thickness = 6f;    // Thicker for visibility
                    line.Connect(rectA, rectB);
                }
            }

            // Sync with Logic (Internal manager tracks this for hypothesis checking)
            EvidenceManager.Instance.AddConnection(idA, idB);

            // AUTO-CHECK DEDUCTIONS
            HypothesisController hc = FindObjectOfType<HypothesisController>();
            if (hc != null) hc.CheckAllHypotheses();
        }
    }
}
