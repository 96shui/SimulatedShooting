using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRShooting.Unity.UI
{
    /// <summary>50 cm target; shot positions are copied from service DTOs, never inferred from labels.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TacticalTargetPlot : MaskableGraphic
    {
        readonly List<Vector2> impacts = new List<Vector2>();
        public int ImpactCount => impacts.Count;
        public void SetImpacts(IEnumerable<Vector2> points)
        {
            impacts.Clear();
            if (points != null) impacts.AddRange(points);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .48f;
            for (var ring = 1; ring <= 5; ring++)
                Circle(vh, rect.center, radius * ring / 5, 1.2f, new Color32(118, 184, 209, 220));
            foreach (var point in impacts)
            {
                var offset = new Vector2(Mathf.Clamp(point.x / 25, -1, 1), Mathf.Clamp(point.y / 25, -1, 1)) * radius;
                Circle(vh, rect.center + offset, 3.5f, 3.5f, new Color32(255, 175, 69, 255));
            }
        }
        static void Circle(VertexHelper vh, Vector2 center, float radius, float width, Color tint)
        {
            for (var i = 0; i < 64; i++)
            {
                var a = i * Mathf.PI / 32; var b = (i + 1) * Mathf.PI / 32;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                var n = vh.currentVertCount;
                vh.AddVert(center + u * radius, tint, Vector2.zero);
                vh.AddVert(center + v * radius, tint, Vector2.zero);
                vh.AddVert(center + v * Mathf.Max(0, radius - width), tint, Vector2.zero);
                vh.AddVert(center + u * Mathf.Max(0, radius - width), tint, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
