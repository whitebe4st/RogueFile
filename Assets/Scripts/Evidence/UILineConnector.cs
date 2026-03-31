using UnityEngine;
using UnityEngine.UI;

namespace EvidenceSystem
{
    [RequireComponent(typeof(Image))]
    public class UILineConnector : MonoBehaviour
    {
        public float thickness = 5f;
        public Color stringColor = Color.red;

        private Image lineImage;
        private RectTransform rect;
        private RectTransform startTarget;
        private RectTransform endTarget;

        private void Awake()
        {
            lineImage = GetComponent<Image>();
            rect = GetComponent<RectTransform>();
            
            // Set base properties for the line image
            lineImage.color = stringColor;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0.5f);
        }

        private void LateUpdate()
        {
            // Use LateUpdate to ensure UI layout has settled for the frame
            if (startTarget != null && endTarget != null)
            {
                UpdateLinePosition();
            }
        }

        public void Connect(RectTransform start, RectTransform end)
        {
            startTarget = start;
            endTarget = end;
            Canvas.ForceUpdateCanvases(); // Ensure positions are current
            UpdateLinePosition();
        }

        private void UpdateLinePosition()
        {
            if (startTarget == null || endTarget == null || rect.parent == null) return;

            // Get the world center positions
            Vector3 worldStart = startTarget.position;
            Vector3 worldEnd = endTarget.position;

            // Convert world positions to the local space of the line's parent (lineContainer)
            Vector2 localStart = rect.parent.InverseTransformPoint(worldStart);
            Vector2 localEnd = rect.parent.InverseTransformPoint(worldEnd);

            Vector2 dir = (localEnd - localStart);
            float distance = dir.magnitude;
            
            // If the distance is zero (e.g. on first frame before layout), hide the line
            if (distance < 0.1f)
            {
                lineImage.enabled = false;
                return;
            }
            lineImage.enabled = true;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rect.localPosition = localStart;
            rect.sizeDelta = new Vector2(distance, thickness);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
