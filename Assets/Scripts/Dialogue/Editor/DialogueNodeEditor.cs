using UnityEngine;
using UnityEditor;
using XNodeEditor;
using System.Linq;
using DialogueSystem;

namespace DialogueSystem.Editors
{
    [CustomNodeEditor(typeof(DialogueNode))]
    public class DialogueNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            serializedObject.Update();

            DialogueNode node = target as DialogueNode;

            // Define fields we handle manually or skip
            string[] excludes = { "m_Script", "graph", "position", "ports", "emotionKey" };

            // Iterate through serialized properties and draw them
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (excludes.Contains(iterator.name)) continue;
                NodeEditorGUILayout.PropertyField(iterator, true);
            }

            // Manually draw the emotionKey dropdown
            DrawEmotionKeyDropdown(node);

            // Iterate through dynamic ports and draw them
            foreach (XNode.NodePort dynamicPort in target.DynamicPorts)
            {
                if (NodeEditorGUILayout.IsDynamicPortListPort(dynamicPort)) continue;
                NodeEditorGUILayout.PortField(dynamicPort);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEmotionKeyDropdown(DialogueNode node)
        {
            SerializedProperty emotionProp = serializedObject.FindProperty("emotionKey");

            if (node.characterProfile != null && node.characterProfile.expressions != null && node.characterProfile.expressions.Count > 0)
            {
                // Get all keys from the profile
                string[] options = node.characterProfile.expressions.Select(e => e.key).ToArray();
                
                // Find current index
                int currentIndex = System.Array.IndexOf(options, emotionProp.stringValue);
                
                // If not found (e.g. key typed in manually before), default to index 0 or keep as is if we want to support legacy
                if (currentIndex == -1)
                {
                    // If it's empty, default to Neutral if it exists, otherwise 0
                    if (string.IsNullOrEmpty(emotionProp.stringValue))
                    {
                        currentIndex = Mathf.Max(0, System.Array.IndexOf(options, "Neutral"));
                    }
                    else
                    {
                        // It's a custom key not in the list. Let's add it to the options temporarily or show a warning.
                        // For simplicity, we'll just prepend it so the user knows it's "Wrong"
                        var optionsList = options.ToList();
                        optionsList.Insert(0, emotionProp.stringValue + " (Not in Profile)");
                        options = optionsList.ToArray();
                        currentIndex = 0;
                    }
                }

                int newIndex = EditorGUILayout.Popup("Emotion Key", currentIndex, options);
                
                // Only update if it changed and isn't our "Not in Profile" warning
                if (newIndex != currentIndex)
                {
                    emotionProp.stringValue = options[newIndex].Replace(" (Not in Profile)", "");
                }
            }
            else
            {
                // Fallback to text field if no profile is assigned
                NodeEditorGUILayout.PropertyField(emotionProp);
            }
        }
    }
}
