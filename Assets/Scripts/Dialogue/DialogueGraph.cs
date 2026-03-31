using UnityEngine;
using XNode;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "New Dialogue Graph", menuName = "Dialogue System/Dialogue Graph")]
    public class DialogueGraph : NodeGraph 
    {
        // Finds the first node in the graph, useful for starting a dialogue directly from the graph asset
        public DialogueNode GetStartNode()
        {
            // Find a node that has no incoming connections on its "Previous" port
            foreach (Node n in nodes)
            {
                DialogueNode dNode = n as DialogueNode;
                if (dNode != null)
                {
                    NodePort previousPort = dNode.GetInputPort("Previous");
                    if (previousPort != null && !previousPort.IsConnected)
                    {
                        return dNode;
                    }
                }
            }

            // Fallback: return the first node added
            if (nodes.Count > 0)
            {
                return nodes[0] as DialogueNode;
            }

            return null;
        }
    }
}
