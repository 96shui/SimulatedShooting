using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimulatedShooting.Editor
{
    public static class MovingTargetSilhouetteAuthoring
    {
        const string ScenePath = "Assets/Scenes/MovingTargetRangeScene.unity";
        const string Folder = "Assets/SimulatedShooting/Art/Environment/MovingTargetRange/";
        const string ModelPath = Folder + "SideProfileTarget.obj";
        const string MaterialPath = Folder + "SideProfileTargetGreen.mat";

        // Use the imported polygon mesh, while keeping hit detection on the existing collider.
        public static GameObject CreateVisual(Transform parent)
        {
            var mesh = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Mesh>().FirstOrDefault();
            if (mesh == null) throw new InvalidOperationException("Moving target silhouette mesh is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SideProfileTargetGreen" };
                material.SetColor("_BaseColor", new Color(.15f, .30f, .17f));
                material.SetFloat("_Smoothness", .05f);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            var target = new GameObject("Visual_MovingTarget_SideProfile");
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = target.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return target;
        }

        [MenuItem("Tools/Simulated Shooting/Install Moving Target Silhouette")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode before editing the target.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var wasLoaded = scene.isLoaded;
            if (wasLoaded && scene.isDirty) throw new InvalidOperationException("MovingTargetRangeScene has unsaved changes; save them before installing the target.");
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var binding = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MovingTargetRouteBinding>(true)).Single();
                var parent = binding.TargetRoot;
                var old = parent.Find("Visual_MovingTarget_SideProfile");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (Transform child in parent)
                    if (child.name.StartsWith("Runner_", StringComparison.Ordinal)) child.gameObject.SetActive(false);
                var visual = CreateVisual(parent);
                var serialized = new SerializedObject(binding);
                var renderers = serialized.FindProperty("targetRenderers");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = visual.GetComponent<Renderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save moving target scene.");
                AssetDatabase.SaveAssets();
            }
            finally { if (!wasLoaded && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
