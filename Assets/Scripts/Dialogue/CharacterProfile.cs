using UnityEngine;
using System.Collections.Generic;

namespace DialogueSystem
{
    [System.Serializable]
    public struct EmotionalSprite
    {
        public string key;
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "Dialogue System/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        public string characterName;
        public string npcID; // Unique ID for Dify logic
        public float portraitScale = 1f;

        [Header("Expressions")]
        public List<EmotionalSprite> expressions = new List<EmotionalSprite>();

        public Sprite GetSprite(string emotionKey)
        {
            if (string.IsNullOrEmpty(emotionKey)) emotionKey = "Neutral";

            // Find matching key
            foreach (var expr in expressions)
            {
                if (expr.key.Equals(emotionKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    return expr.sprite;
                }
            }

            // Fallback: If "Neutral" is in the list, use it. Otherwise use the first available.
            if (expressions.Count > 0)
            {
                foreach (var expr in expressions)
                {
                    if (expr.key.Equals("Neutral", System.StringComparison.OrdinalIgnoreCase))
                        return expr.sprite;
                }
                return expressions[0].sprite;
            }

            return null;
        }
    }
}
