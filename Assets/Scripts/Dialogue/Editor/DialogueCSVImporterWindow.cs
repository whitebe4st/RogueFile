using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XNode;
using DialogueSystem;
using System.Text.RegularExpressions;

namespace DialogueSystem.Editors
{
    public class DialogueCSVImporterWindow : EditorWindow
    {
        private TextAsset csvFile;
        private DialogueGraph targetGraph;
        private float verticalSpacing = 200f;
        private float horizontalSpacing = 350f;

        [MenuItem("Tools/Dialogue Importer")]
        public static void ShowWindow()
        {
            GetWindow<DialogueCSVImporterWindow>("Dialogue Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Dialogue CSV Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Format: ID, Type, Speaker, Text, Emotion, TargetID, EventKey\nTypes: TALK, CHOICE, OPTION", MessageType.Info);
            
            EditorGUILayout.Space();
            csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
            targetGraph = (DialogueGraph)EditorGUILayout.ObjectField("Target Graph", targetGraph, typeof(DialogueGraph), false);
            
            EditorGUILayout.Space();
            verticalSpacing = EditorGUILayout.FloatField("Vertical Spacing", verticalSpacing);
            horizontalSpacing = EditorGUILayout.FloatField("Horizontal Spacing", horizontalSpacing);

            EditorGUILayout.Space();
            if (GUILayout.Button("Import Dialogue"))
            {
                if (csvFile == null || targetGraph == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign both a CSV file and a Target Graph.", "OK");
                    return;
                }

                if (EditorUtility.DisplayDialog("Confirm Import", "This will add new nodes to the target graph. Continue?", "Yes", "Cancel"))
                {
                    Import();
                }
            }
        }

        private void Import()
        {
            string[] lines = csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            // Header mapping
            string header = lines[0];
            var columnMap = GetColumnMap(header);

            var rows = new List<CSVRow>();
            for (int i = 1; i < lines.Length; i++)
            {
                var rowData = ParseCSVLine(lines[i]);
                if (rowData == null || rowData.Length < 2) continue;
                rows.Add(new CSVRow(rowData, columnMap));
            }

            Undo.RecordObject(targetGraph, "Import Dialogue from CSV");

            Dictionary<int, DialogueNode> idToNode = new Dictionary<int, DialogueNode>();
            
            // 1. First Pass: Create all nodes (TALK and CHOICE)
            float xOffset = 0;
            float yOffset = 0;

            foreach (var row in rows)
            {
                if (row.Type == "OPTION") continue;

                DialogueNode node = targetGraph.AddNode<DialogueNode>();
                AssetDatabase.AddObjectToAsset(node, targetGraph);
                node.name = $"Node_{row.ID}_{row.Speaker}";
                
                // Set metadata
                node.SpeakerName = row.Speaker;
                node.DialogueText = row.Text;
                node.emotionKey = row.Emotion;
                node.EventKey = row.EventKey;

                // Auto-hook Character Profile
                if (!string.IsNullOrEmpty(row.Speaker))
                {
                    node.characterProfile = FindCharacterProfile(row.Speaker);
                }
                
                // Position
                node.position = new Vector2(xOffset, yOffset);
                yOffset += verticalSpacing;

                idToNode[row.ID] = node;
            }

            // 2. Second Pass: Handle OPTION rows and Connections
            DialogueNode currentChoiceNode = null;
            int currentChoiceIndex = 0;

            foreach (var row in rows)
            {
                if (row.Type == "CHOICE")
                {
                    currentChoiceNode = idToNode.ContainsKey(row.ID) ? idToNode[row.ID] : null;
                    currentChoiceIndex = 0;
                    if (currentChoiceNode != null)
                    {
                        currentChoiceNode.Choices = new List<DialogueChoice>();
                    }
                    continue;
                }

                if (row.Type == "OPTION" && currentChoiceNode != null)
                {
                    DialogueChoice choice = new DialogueChoice();
                    choice.ChoiceText = row.Text;
                    
                    DialogueNode targetNode = (row.TargetID != -1 && idToNode.ContainsKey(row.TargetID)) ? idToNode[row.TargetID] : null;
                    if (targetNode != null)
                    {
                        choice.NextNode = targetNode;
                    }

                    // Add to the internal list
                    currentChoiceNode.Choices.Add(choice);
                    
                    // Add a dynamic port for xNode visualize
                    string portName = $"Choices {currentChoiceIndex}";
                    NodePort choicePort = currentChoiceNode.AddDynamicOutput(typeof(DialogueNode), Node.ConnectionType.Override, Node.TypeConstraint.None, portName);
                    
                    if (choicePort != null && targetNode != null)
                    {
                        NodePort inputPort = targetNode.GetInputPort("Previous");
                        if (inputPort != null) choicePort.Connect(inputPort);
                    }
                    
                    currentChoiceIndex++;
                    continue;
                }

                if (row.Type == "TALK")
                {
                    if (idToNode.ContainsKey(row.ID))
                    {
                        DialogueNode node = idToNode[row.ID];
                        if (row.TargetID != -1 && idToNode.ContainsKey(row.TargetID))
                        {
                            DialogueNode targetNode = idToNode[row.TargetID];
                            NodePort outputPort = node.GetOutputPort("Next");
                            NodePort inputPort = targetNode.GetInputPort("Previous");
                            if (outputPort != null && inputPort != null)
                            {
                                outputPort.Connect(inputPort);
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(targetGraph));
            EditorUtility.DisplayDialog("Success", $"Imported {idToNode.Count} dialogue nodes.", "Awesome!");
        }

        private Dictionary<string, int> GetColumnMap(string header)
        {
            var map = new Dictionary<string, int>();
            var cols = ParseCSVLine(header);
            for (int i = 0; i < cols.Length; i++)
            {
                string c = cols[i].Trim().ToUpper();
                map[c] = i;
            }
            return map;
        }

        private CharacterProfile FindCharacterProfile(string speaker)
        {
            // First search by exact name "[Speaker]_Profile"
            string targetName = $"{speaker}_Profile";
            string[] guids = AssetDatabase.FindAssets($"{targetName} t:CharacterProfile");
            
            if (guids.Length == 0)
            {
                // Fallback to searching just the speaker name
                guids = AssetDatabase.FindAssets($"{speaker} t:CharacterProfile");
            }

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
            }

            return null;
        }

        private string[] ParseCSVLine(string line)
        {
            // Robust CSV parsing to handle commas within quotes
            List<string> result = new List<string>();
            bool inQuotes = false;
            string current = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current.Trim());
            return result.ToArray();
        }

        private class CSVRow
        {
            public int ID;
            public string Type;
            public string Speaker;
            public string Text;
            public string Emotion;
            public int TargetID;
            public string EventKey;

            public CSVRow(string[] cols, Dictionary<string, int> map)
            {
                ID = GetVal(cols, map, "ID", -1);
                
                // Check if "TYPE" exists, otherwise default to "TALK" (unless it's empty ID)
                if (map.ContainsKey("TYPE"))
                    Type = cols[map["TYPE"]].Trim().ToUpper();
                else
                    Type = "TALK";

                Speaker = GetVal(cols, map, "SPEAKER", "");
                
                // User's CSV uses "DIALOGUE" instead of "TEXT"
                Text = GetVal(cols, map, "DIALOGUE", GetVal(cols, map, "TEXT", ""));
                Text = Text.Trim('\"');

                Emotion = GetVal(cols, map, "EMOTION", "");
                
                // Support either "NEXTID" or "TARGETID"
                TargetID = GetVal(cols, map, "NEXTID", GetVal(cols, map, "TARGETID", -1));
                    
                EventKey = GetVal(cols, map, "EVENTKEY", "");
            }

            private string GetVal(string[] cols, Dictionary<string, int> map, string key, string fallback)
            {
                if (map.ContainsKey(key) && map[key] < cols.Length)
                    return cols[map[key]].Trim();
                return fallback;
            }

            private int GetVal(string[] cols, Dictionary<string, int> map, string key, int fallback)
            {
                if (map.ContainsKey(key) && map[key] < cols.Length)
                {
                    int v;
                    if (int.TryParse(cols[map[key]], out v)) return v;
                }
                return fallback;
            }
        }
    }
}
