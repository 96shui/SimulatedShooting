using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRShooting.Unity.UI
{
    /// <summary>Orthographic face of the scene's 50 cm bust target, using the same dimensions and palette.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TacticalTargetPlot : MaskableGraphic
    {
        readonly List<Vector2> impacts = new List<Vector2>();
        readonly List<RectTransform> markers = new List<RectTransform>();
        RectTransform averageMarker, previewMarker;
        Vector2? averagePoint, previewPoint;
        Texture2D sceneFace;
        public override Texture mainTexture => SceneFace!=null?SceneFace:Texture2D.whiteTexture;
        Texture2D SceneFace => sceneFace!=null?sceneFace:(sceneFace=Resources.Load<Texture2D>("UI/Tactical/ChestTargetReference"));
        // Shared photograph calibration: white 10-ring centre and diameter in original pixels.
        public const float ArtworkWidthMetres = .10f * 1068f / 202f;
        public const float ArtworkHeightMetres = .10f * 1051f / 202f;
        public static readonly Vector2 ArtworkCentreUv = new Vector2(538f/1068f,1f-638f/1051f);
        public int ImpactCount => impacts.Count;
        public const float TargetSideCm = 50f;
        public const float TenRingDiameterCm = 10f;
        public static bool IsOnTarget(Vector2 point)
            => !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
                Mathf.Abs(point.x)<=TargetSideCm*.5f && Mathf.Abs(point.y)<=TargetSideCm*.5f;

        public static Rect HitRegion(Rect outer)
        {
            var art=ArtworkRect(outer);
            var centre=art.min+Vector2.Scale(art.size,ArtworkCentreUv);
            var side=art.width*.5f/ArtworkWidthMetres;
            return new Rect(centre-Vector2.one*side*.5f,Vector2.one*side);
        }

        public static Rect ArtworkRect(Rect outer)
        {
            var scale=Mathf.Min(outer.width/ArtworkWidthMetres,outer.height/ArtworkHeightMetres);
            var size=new Vector2(ArtworkWidthMetres,ArtworkHeightMetres)*scale;
            return new Rect(outer.center-size*.5f,size);
        }

        public static Vector2 MapImpactPointCm(Rect outer, Vector2 point)
        {
            var region = HitRegion(outer);
            return region.center + point * (region.width / TargetSideCm);
        }
        public void SetImpacts(IEnumerable<Vector2> points)
        {
            impacts.Clear();
            if (points != null) impacts.AddRange(points);
            while (markers.Count < impacts.Count)
            {
                var marker = new GameObject("ImpactMarker_" + (markers.Count + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                marker.transform.SetParent(transform, false);
                var image = marker.GetComponent<Image>();
                image.color = new Color(1f, .15f, .08f, 1f);
                image.raycastTarget = false;
                markers.Add(marker.GetComponent<RectTransform>());
            }
            UpdateMarkers();
            SetVerticesDirty();
        }
        public void SetReferencePoints(Vector2? average, Vector2? preview)
        {
            averagePoint = average;
            previewPoint = preview;
            if (averageMarker == null) averageMarker = CreateReference("AverageMarker", new Color(1f, .82f, .1f));
            if (previewMarker == null) previewMarker = CreateReference("PreviewMarker", new Color(.1f, .8f, 1f));
            UpdateMarkers();
        }
        RectTransform CreateReference(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }
        void UpdateReference(RectTransform marker, Vector2? point, float size)
        {
            if (marker == null) return;
            var active = point.HasValue && IsOnTarget(point.Value);
            marker.gameObject.SetActive(active);
            if (!active) return;
            marker.anchorMin = marker.anchorMax = rectTransform.pivot;
            marker.sizeDelta = Vector2.one * size;
            marker.anchoredPosition = MapImpactPointCm(rectTransform.rect, point.Value);
        }
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateMarkers();
        }
        void UpdateMarkers()
        {
            for (var i = 0; i < markers.Count; i++)
            {
                var active = i < impacts.Count && IsOnTarget(impacts[i]);
                markers[i].gameObject.SetActive(active);
                if (!active) continue;
                markers[i].anchorMin = markers[i].anchorMax = rectTransform.pivot;
                markers[i].sizeDelta = new Vector2(7, 7);
                markers[i].anchoredPosition = MapImpactPointCm(rectTransform.rect, impacts[i]);
            }
            UpdateReference(averageMarker, averagePoint, 5f);
            UpdateReference(previewMarker, previewPoint, 3f);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var outer = GetPixelAdjustedRect();
            var rect = HitRegion(outer);
            var side = rect.width;
            Quad(vh,ArtworkRect(GetPixelAdjustedRect()),Color.white);
        }
        static void Quad(VertexHelper vh, Rect rect, Color tint)
        {
            var n = vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin, rect.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector2(rect.xMin, rect.yMax), tint, Vector2.up);
            vh.AddVert(new Vector2(rect.xMax, rect.yMax), tint, Vector2.one);
            vh.AddVert(new Vector2(rect.xMax, rect.yMin), tint, Vector2.right);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        static void Circle(VertexHelper vh, Vector2 center, float radius, float width, Color tint)
        {
            for (var i = 0; i < 64; i++)
            {
                var a = i * Mathf.PI / 32; var b = (i + 1) * Mathf.PI / 32;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                var n = vh.currentVertCount;
                vh.AddVert(center + u * radius, tint, new Vector2(.08f, .9f));
                vh.AddVert(center + v * radius, tint, new Vector2(.08f, .9f));
                vh.AddVert(center + v * Mathf.Max(0, radius - width), tint, new Vector2(.08f, .9f));
                vh.AddVert(center + u * Mathf.Max(0, radius - width), tint, new Vector2(.08f, .9f));
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
