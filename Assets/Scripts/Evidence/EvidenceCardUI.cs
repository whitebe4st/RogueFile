using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EvidenceSystem
{
    public class EvidenceCardUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public Image iconImage;
        public RawImage iconRawImage; // Added support for RawImage
        public Image backgroundImage; // The card's background sprite

        /// <summary>
        /// Populates the UI elements with data from the EvidenceNode.
        /// </summary>
        public void Setup(EvidenceNode node)
        {
            if (node == null) return;

            if (titleText != null) 
                titleText.text = node.title;

            // Apply Category Colors (matching user diagram)
            if (backgroundImage != null)
            {
                switch (node.category)
                {
                    case EvidenceCategory.When: backgroundImage.color = new Color(0.6f, 0.8f, 1.0f); break; // Light Blue
                    case EvidenceCategory.Why:  backgroundImage.color = new Color(0.7f, 1.0f, 0.9f); break; // Cyan/Mint
                    case EvidenceCategory.What: backgroundImage.color = new Color(1.0f, 0.7f, 0.7f); break; // Pink/Red
                    case EvidenceCategory.Who:  backgroundImage.color = new Color(0.7f, 1.0f, 0.7f); break; // Light Green
                    default: backgroundImage.color = Color.white; break;
                }
            }

            // Handle standard UI Image
            if (iconImage != null)
            {
                iconImage.sprite = node.icon;
                iconImage.enabled = node.icon != null;
            }

            // Handle RawImage (converts sprite to texture)
            if (iconRawImage != null)
            {
                if (node.icon != null)
                {
                    iconRawImage.texture = node.icon.texture;
                    iconRawImage.enabled = true;
                }
                else
                {
                    iconRawImage.enabled = false;
                }
            }
        }
    }
}
