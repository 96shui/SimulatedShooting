using UnityEngine;
using UnityEngine.UI;
using VRShooting.Common;

namespace VRShooting.Unity.UI
{
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public sealed class TacticalMapMarkerGraphic : MaskableGraphic
    {
        public MarkerType MarkerType;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var c = r.center;
            var s = Mathf.Min(r.width, r.height) * .45f;
            if (MarkerType == MarkerType.Player)
            {
                Triangle(vh, c + new Vector2(0, s), c + new Vector2(-s, -s), c + new Vector2(s, -s));
                return;
            }
            if (MarkerType == MarkerType.EnemyKilled)
            {
                Line(vh, c + new Vector2(-s, -s), c + new Vector2(s, s), 3);
                Line(vh, c + new Vector2(-s, s), c + new Vector2(s, -s), 3);
                return;
            }
            var segments = MarkerType == MarkerType.Teammate ? 20 : 4;
            for (var i = 0; i < segments; i++)
            {
                var a = Mathf.PI * 2 * i / segments;
                var b = Mathf.PI * 2 * (i + 1) / segments;
                Line(vh, c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * s, c + new Vector2(Mathf.Sin(b), Mathf.Cos(b)) * s, 2.5f);
            }
        }
        void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
        {
            var n = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
        }
        void Line(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            var d = (b - a).normalized; var normal = new Vector2(-d.y, d.x) * width * .5f;
            Triangle(vh, a - normal, a + normal, b + normal);
            Triangle(vh, a - normal, b + normal, b - normal);
        }
    }
}
