using UnityEngine;
using UnityEngine.UI;

namespace VRShooting.Unity.UI
{
    /// <summary>Resolution-independent target ring. Geometry remains crisp in a world-space canvas.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TacticalRingGraphic : MaskableGraphic
    {
        public float Thickness = 2f;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var bounds = GetPixelAdjustedRect();
            var radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            var inner = Mathf.Max(0, radius - Thickness);
            for (var i = 0; i < 96; i++)
            {
                float a = i * Mathf.PI * 2 / 96, b = (i + 1) * Mathf.PI * 2 / 96;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                var n = vh.currentVertCount;
                vh.AddVert(bounds.center + u * radius, color, Vector2.zero);
                vh.AddVert(bounds.center + v * radius, color, Vector2.zero);
                vh.AddVert(bounds.center + v * inner, color, Vector2.zero);
                vh.AddVert(bounds.center + u * inner, color, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
