using UnityEngine;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Scene
{
    /// <summary>One calibrated reference texture for the physical target and both results views.</summary>
    public static class ChestTargetArtwork
    {
        static Material material;
        static Material assistedRingMaterial;
        public static void Apply(TargetImpactSurface surface)
        {
            if(surface.TargetCenter==null)return;
            var texture=Resources.Load<Texture2D>("UI/Tactical/ChestTargetReference");
            if(texture==null)return;
            EnsureAssistedRing(surface);
            var root=surface.transform.parent;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                if(renderer.name=="TargetFace_50cm"||renderer.name.StartsWith("TargetSilhouette_")||renderer.name=="TenRing_10cm")
                    renderer.enabled=false;
            var existing=surface.TargetCenter.Find("ChestTarget_ReferenceFace");
            if(existing!=null)return;
            var face=GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name="ChestTarget_ReferenceFace";face.transform.SetParent(surface.TargetCenter,false);
            var offset=Vector2.Scale(Vector2.one*.5f-TacticalTargetPlot.ArtworkCentreUv,
                new Vector2(TacticalTargetPlot.ArtworkWidthMetres,TacticalTargetPlot.ArtworkHeightMetres));
            face.transform.localPosition=new Vector3(offset.x,offset.y,-.001f);
            face.transform.localScale=new Vector3(TacticalTargetPlot.ArtworkWidthMetres,TacticalTargetPlot.ArtworkHeightMetres,1);
            var collider=face.GetComponent<Collider>();collider.enabled=false;
            if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            if(material==null)
            {
                material=Resources.Load<Material>("UI/Tactical/ChestTargetReference");
                if(material==null)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Texture"));
                    material.name="ChestTarget_SharedReference";material.mainTexture=texture;
                }
            }
            face.GetComponent<Renderer>().sharedMaterial=material;
        }

        static void EnsureAssistedRing(TargetImpactSurface surface)
        {
            if (surface.TargetCenter.Find("AssistedTenRing_20cm") != null) return;
            var node = new GameObject("AssistedTenRing_20cm");
            node.transform.SetParent(surface.TargetCenter, false);
            var id = surface.GetComponent<SceneTestId>();
            if (id != null) node.AddComponent<SceneTestId>().Id = id.Id + ".AssistedTenRing";
            var circle = node.AddComponent<LineRenderer>();
            circle.useWorldSpace = false; circle.loop = true; circle.positionCount = 128;
            circle.widthMultiplier = .0025f;
            circle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            circle.receiveShadows = false;
            circle.startColor = circle.endColor = new Color32(247, 185, 85, 255);
            if (assistedRingMaterial == null)
            {
                assistedRingMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                assistedRingMaterial.name = "AssistedTenRing_SharedUnlit";
                assistedRingMaterial.hideFlags = HideFlags.HideAndDontSave;
                assistedRingMaterial.color = new Color32(247, 185, 85, 255);
            }
            circle.sharedMaterial = assistedRingMaterial;
            for (var i = 0; i < circle.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2 / circle.positionCount;
                circle.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (surface.TenRingRadiusCm * .01f) - Vector3.forward * .0025f);
            }
        }
    }
}
