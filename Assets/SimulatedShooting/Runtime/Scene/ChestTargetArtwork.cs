using UnityEngine;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Scene
{
    /// <summary>One calibrated reference texture for the physical target and both results views.</summary>
    public static class ChestTargetArtwork
    {
        static Material material;
        static Material highlightMaterial;
        public static void ApplyHighlight(Transform center)
        {
            if (center.Find("ScoredTargetHighlight") != null) return;
            var root = new GameObject("ScoredTargetHighlight").transform;
            root.SetParent(center, false);
            if (highlightMaterial == null)
            {
                highlightMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                highlightMaterial.color = new Color(1f, .85f, .05f);
            }
            AddBar(root, "Top", new Vector3(0, .72f, -.04f), new Vector3(1.6f, .1f, .02f));
            AddBar(root, "Bottom", new Vector3(0, -.72f, -.04f), new Vector3(1.6f, .1f, .02f));
            AddBar(root, "Left", new Vector3(-.75f, 0, -.04f), new Vector3(.1f, 1.44f, .02f));
            AddBar(root, "Right", new Vector3(.75f, 0, -.04f), new Vector3(.1f, 1.44f, .02f));
        }
        static void AddBar(Transform root, string name, Vector3 position, Vector3 scale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(root, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            var collider = bar.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            bar.GetComponent<Renderer>().sharedMaterial = highlightMaterial;
        }
        public static void Apply(TargetImpactSurface surface)
        {
            if(surface.TargetCenter==null)return;
            var texture=Resources.Load<Texture2D>("UI/Tactical/ChestTargetReference");
            if(texture==null)return;
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
    }
}
