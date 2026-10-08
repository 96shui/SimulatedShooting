using NUnit.Framework;
using UnityEngine;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.EditMode.UI
{
    // BDD06/07: scene bust proportions and centimetre coordinates remain identical at every UI size.
    public sealed class BustTargetMappingTests
    {
        [Test]
        public void PlotUsesSharedReferenceTexture()
        {
            var root=new GameObject("SceneChestTargetTexture",typeof(RectTransform));
            try
            {
                var plot=root.AddComponent<TacticalTargetPlot>();
                var sceneTexture=Resources.Load<Texture2D>("UI/Tactical/ChestTargetReference");
                Assert.That(sceneTexture,Is.Not.Null);
                Assert.That(plot.mainTexture,Is.SameAs(sceneTexture));
            }
            finally {Object.DestroyImmediate(root);}
        }
        // BDD07: marker geometry must use the same calibration once and an opaque texel.
        [Test]
        public void FinalThumbnailMarkerUsesCalibratedPositionAndOpaqueBackgroundUv()
        {
            var root = new GameObject("Thumbnail", typeof(RectTransform));
            var mesh = new Mesh();
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(96, 96);
                var plot = root.AddComponent<TacticalTargetPlot>();
                var point = new Vector2(-8, 12);
                plot.SetImpacts(new[] { point });
                using (var helper = new UnityEngine.UI.VertexHelper())
                {
                    typeof(TacticalTargetPlot).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(plot, new object[] { helper });
                    helper.FillMesh(mesh);
                }
                var expected = TacticalTargetPlot.MapImpactPointCm(rect.rect, point);
                var markerCentre = Vector2.zero;
                for (var index = 4; index < mesh.vertexCount; index++)
                {
                    markerCentre += (Vector2)mesh.vertices[index];
                    Assert.That(mesh.uv[index], Is.EqualTo(new Vector2(.08f, .9f)));
                }
                markerCentre /= mesh.vertexCount - 4;
                Assert.That(Vector2.Distance(markerCentre, expected), Is.LessThan(.001f));
            }
            finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(root); }
        }
        [TestCase(100f)]
        [TestCase(255f)]
        [TestCase(500f)]
        public void CentreAxesAndTenRingUseSamePhysicalScale(float side)
        {
            var rect = new Rect(10f, 20f, side, side);
            var region=TacticalTargetPlot.HitRegion(rect);
            var art=TacticalTargetPlot.ArtworkRect(rect);
            Assert.That(TacticalTargetPlot.MapImpactPointCm(rect, Vector2.zero),
                Is.EqualTo(art.min+Vector2.Scale(art.size,TacticalTargetPlot.ArtworkCentreUv)));
            Assert.That(TacticalTargetPlot.MapImpactPointCm(rect, new Vector2(25f, 0f)).x, Is.EqualTo(region.xMax).Within(.001f));
            Assert.That(TacticalTargetPlot.MapImpactPointCm(rect, new Vector2(0f, -25f)).y, Is.EqualTo(region.yMin).Within(.001f));
            Assert.That(TacticalTargetPlot.MapImpactPointCm(rect, new Vector2(5f, 0f)).x - region.center.x,
                Is.EqualTo(art.width*202f/1068f*.5f).Within(.001f));
            Assert.That(TacticalTargetPlot.TenRingDiameterCm, Is.EqualTo(10f));
        }

        [Test]
        public void NonSquareDisplayKeepsSquareRegionAndDoesNotClampRawCoordinates()
        {
            var rect = new Rect(0f, 0f, 200f, 100f);
            var region=TacticalTargetPlot.HitRegion(rect);
            Assert.That(region.width,Is.EqualTo(region.height));
            Assert.That(TacticalTargetPlot.MapImpactPointCm(rect,new Vector2(30f,0)).x,Is.GreaterThan(region.xMax));
        }
        [Test]
        public void PhysicalBoundsIncludeAllFourEdgesButNeverClampMissesOntoTheBoard()
        {
            foreach(var edge in new[]{new Vector2(25,25),new Vector2(-25,-25),new Vector2(25,-25),new Vector2(-25,25)})
                Assert.That(TacticalTargetPlot.IsOnTarget(edge),Is.True);
            Assert.That(TacticalTargetPlot.IsOnTarget(new Vector2(25.01f,0)),Is.False);
            Assert.That(TacticalTargetPlot.IsOnTarget(new Vector2(0,float.NaN)),Is.False);
            Assert.That(TacticalTargetPlot.IsOnTarget(new Vector2(float.PositiveInfinity,0)),Is.False);
        }
    }
}
