using System;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    public static class CombatPhotographicAssets
    {
        const string Characters = "Assets/SimulatedShooting/Art/Characters/Renderpeople";
        const string Facade = "Assets/SimulatedShooting/Art/Combat/TaiwanStreet/PhotographicFacade/";

        [MenuItem("Tools/Simulated Shooting/Scene 3/Apply Photographic People And Facade")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, EditorSceneManager.sceneCount).Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Exit Play mode and save scene edits before applying assets.");
            ConfigureTexture(Facade + "diff.jpg", false);
            ConfigureTexture(Facade + "normal.jpg", true);
            foreach (var name in new[] { "Eric", "Claudia" })
            {
                var folder = Characters + name + "/";
                ConfigureTexture(folder + "dif.jpg", false);
                ConfigureTexture(folder + "norm.jpg", true);
                var importer = (ModelImporter)AssetImporter.GetAtPath(folder + name + ".fbx");
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            var scene = EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath, OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var people = new[] { CreateDisplayPerson("Eric"), CreateDisplayPerson("Claudia") };
            int personIndex = 0;
            try
            {
                foreach (Transform house in bindings.GeometryRoot.Find("Town_PerimeterBuildings"))
                {
                    var foundation = house.Find("Foundation");
                    // The old plinth and the new room floor previously shared y=0, causing visible z-fighting while walking.
                    foundation.localPosition = new Vector3(foundation.localPosition.x, -foundation.localScale.y * .5f - .04f, foundation.localPosition.z);
                    var dummy = house.Find("Interior/TrainingDummy");
                    foreach (Transform child in dummy.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                    var visual = Object.Instantiate(people[personIndex++ % people.Length], dummy);
                    visual.name = "PhotographicPerson";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                }
            }
            finally { foreach (var person in people) Object.DestroyImmediate(person); }
            ApplyFacade();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            CombatTaiwanStreetArt.Capture();
        }

        static GameObject CreateDisplayPerson(string name)
        {
            var folder = Characters + name + "/";
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".fbx"));
            try
            {
                var animator = source.GetComponent<Animator>();
                if (animator == null || !animator.isHuman) throw new InvalidOperationException(name + " needs a valid Humanoid avatar before posing.");
                using (var handler = new HumanPoseHandler(animator.avatar, source.transform))
                {
                    var pose = new HumanPose();
                    handler.GetHumanPose(ref pose);
                    for (int i = 0; i < HumanTrait.MuscleCount; i++)
                        if (HumanTrait.MuscleName[i].EndsWith("Arm Down-Up")) pose.muscles[i] = -.85f;
                    handler.SetHumanPose(ref pose);
                }
                var material = Material(folder + "SkinAndClothes.mat");
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "dif.jpg"));
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "norm.jpg"));
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_Smoothness", .2f);
                material.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(material);
                var root = new GameObject("PhotographicPerson");
                int index = 0;
                foreach (var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = new Mesh();
                    renderer.BakeMesh(mesh);
                    var path = folder + "DisplayMesh" + index++ + ".asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
                    else { EditorUtility.CopySerialized(mesh, saved); EditorUtility.SetDirty(saved); Object.DestroyImmediate(mesh); }
                    var part = new GameObject(renderer.name);
                    part.transform.SetParent(root.transform, false);
                    part.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    part.transform.localScale = renderer.transform.lossyScale;
                    part.AddComponent<MeshFilter>().sharedMesh = saved;
                    part.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, saved.subMeshCount).ToArray();
                }
                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0) { Object.DestroyImmediate(root); throw new InvalidOperationException("No scanned meshes were baked."); }
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 1.78f / bounds.size.y;
                foreach (Transform part in root.transform)
                {
                    part.localPosition = (part.localPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)) * scale;
                    part.localScale *= scale;
                }
                return root;
            }
            finally { Object.DestroyImmediate(source); }
        }

        static void ApplyFacade()
        {
            var roughImporter = (TextureImporter)AssetImporter.GetAtPath(Facade + "rough.jpg");
            roughImporter.isReadable = true; roughImporter.sRGBTexture = false; roughImporter.SaveAndReimport();
            var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(Facade + "rough.jpg");
            var packed = new Texture2D(rough.width, rough.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels(rough.GetPixels().Select(c => new Color(0, 0, 0, 1 - c.r)).ToArray());
            packed.Apply();
            File.WriteAllBytes(Facade + "smoothness.png", packed.EncodeToPNG()); Object.DestroyImmediate(packed);
            roughImporter.isReadable = false; roughImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(Facade + "smoothness.png");
            var packedImporter = (TextureImporter)AssetImporter.GetAtPath(Facade + "smoothness.png");
            packedImporter.sRGBTexture = false; packedImporter.SaveAndReimport();
            foreach (var name in new[] { "IvoryTiles", "MintTiles" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/TaiwanStreet/" + name + ".mat");
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Facade + "diff.jpg"));
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Facade + "normal.jpg"));
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Facade + "smoothness.png"));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 1);
                material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
                EditorUtility.SetDirty(material);
            }
        }

        static void ConfigureTexture(string path, bool normal)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = 2048;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.SaveAndReimport();
        }

        static Material Material(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
