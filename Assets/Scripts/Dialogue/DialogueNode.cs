using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using XNode;

namespace DialogueSystem
{
    [System.Serializable]
    public struct DialogueChoice
    {
        [Tooltip("Text to display on the choice button")]
        public string ChoiceText;
        
        [Tooltip("Leave empty if you are connecting this choice via the node graph ports.")]
        public DialogueNode NextNode;
    }

    [NodeWidth(300)]
    [CreateNodeMenu("Dialogue/Dialogue Node")]
    public class DialogueNode : Node
    {
        [Input(ShowBackingValue.Never, ConnectionType.Multiple)] 
        public DialogueNode Previous;

        [Header("Content")]
        public string SpeakerName;
        
        [TextArea(3, 10)]
        public string DialogueText;
        
        [Header("Character Representation")]
        public CharacterProfile characterProfile;
        public string emotionKey = "Neutral";
        
        [Header("Choices")]
        public List<DialogueChoice> Choices;

        [Header("Events")]
        [Tooltip("Event triggered when this node is entered.")]
        public UnityEvent OnNodeEnter;

        [Tooltip("String key to trigger a specific event in the Scene via the DialogueManager.")]
        public string EventKey;
        
        [Header("Chaining")]
        [Output(ShowBackingValue.Never, ConnectionType.Override)] 
        public DialogueNode Next;
        
        // Helper property to check if it's a simple continuation (no choices)
        public bool IsContinuous => (Choices == null || Choices.Count == 0);

        // xNode requires this to get the value of an output port
        public override object GetValue(NodePort port) 
        {
            return this;
        }

        public DialogueNode GetNextNode(int choiceIndex = -1)
        {
            if (Application.isEditor)
            {
                foreach (var p in Ports)
                {
                    Debug.Log($"[DialogueNode] Available Port on {name}: '{p.fieldName}'");
                }
            }

            // If it's a choice node
            if (choiceIndex >= 0 && choiceIndex < Choices.Count)
            {
                // Iterate through all ports to find the one matching this choice index
                foreach (NodePort p in DynamicPorts)
                {
                    if (p.fieldName == $"Choices {choiceIndex}")
                    {
                        if (p.IsConnected) return p.Connection.node as DialogueNode;
                    }
                }
                
                // Fallback 1: xNode standard list naming
                NodePort port = GetOutputPort($"Choices {choiceIndex}");
                if (port != null && port.IsConnected)
                {
                    return port.Connection.node as DialogueNode;
                }
                
                // Fallback to the explicit reference if no port connection
                return Choices[choiceIndex].NextNode;
            }
            
            // If it's a continuous node (no choices)
            NodePort nextPort = GetOutputPort("Next");
            if (nextPort != null && nextPort.IsConnected)
            {
                return nextPort.Connection.node as DialogueNode;
            }

            // Fallback for old Inspector assigned nodes (not graph-connected)
            return Next;
        }
    }
}
