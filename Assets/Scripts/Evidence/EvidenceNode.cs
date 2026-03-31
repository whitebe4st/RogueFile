using UnityEngine;
using System.Collections.Generic;

namespace EvidenceSystem
{
    public enum EvidenceCategory { None, When, Why, What, Who, General }
    public enum NodeType { Clue, Question, Conclusion }

    [CreateAssetMenu(fileName = "NewEvidenceNode", menuName = "Investigative/Evidence Node")]
    public class EvidenceNode : ScriptableObject
    {
        [Header("Identification")]
        public string nodeID;
        public string title;
        public EvidenceCategory category;
        public NodeType nodeType;

        [Header("Details")]
        [TextArea(3, 10)]
        public string description;

        [Header("Visuals")]
        public Sprite icon;

        [Header("State")]
        public bool isDiscovered = false;

        [Header("Hypothesis Logic")]
        [Tooltip("List of other EvidenceNode IDs that must be connected to this one to form a hypothesis.")]
        public List<string> requiredConnections = new List<string>();
    }
}
