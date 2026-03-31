using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace EvidenceSystem
{
    /// <summary>
    /// A "Pure" UI Line renderer that draws a procedural line between two RectTransforms.
    /// This avoids the "Sprite stretching" artifacts and allows for much better precision.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ProceduralUILine : MaskableGraphic
    {
        public RectTransform startTarget;
        public RectTransform endTarget;
        public float thickness = 4f;

        private void LateUpdate()
        {
            if (startTarget != null && endTarget != null)
            {
                // SetVerticesDirty tells the Canvas to call OnPopulateMesh
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (startTarget == null || endTarget == null) return;

            // Get local positions relative to this line's transform
            Vector2 p1 = rectTransform.InverseTransformPoint(startTarget.position);
            Vector2 p2 = rectTransform.InverseTransformPoint(endTarget.position);

            float angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float halfThickness = thickness / 2f;

            // Calculate the 4 corners of the line "quad"
            Vector2 v1 = new Vector2(p1.x + halfThickness * sin, p1.y - halfThickness * cos);
            Vector2 v2 = new Vector2(p1.x - halfThickness * sin, p1.y + halfThickness * cos);
            Vector2 v3 = new Vector2(p2.x - halfThickness * sin, p2.y + halfThickness * cos);
            Vector2 v4 = new Vector2(p2.x + halfThickness * sin, p2.y - halfThickness * cos);

            // Add vertices
            AddVert(vh, v1);
            AddVert(vh, v2);
            AddVert(vh, v3);
            AddVert(vh, v4);

            // Create triangles
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private void AddVert(VertexHelper vh, Vector2 pos)
        {
            UIVertex vert = UIVertex.simpleVert;
            vert.position = pos;
            vert.color = color;
            vh.AddVert(vert);
        }

        public void Connect(RectTransform start, RectTransform end)
        {
            startTarget = start;
            endTarget = end;
            SetVerticesDirty();
        }
    }
}
