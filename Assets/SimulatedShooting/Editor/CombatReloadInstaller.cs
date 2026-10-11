using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class CombatReloadInstaller
    {
        [MenuItem("Tools/Simulated Shooting/Scene 3/Install Magazine Reload")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Install magazine in Edit Mode");
            const string path = "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab";
            var model = Resources.Load<GameObject>("Combat/Weapons/QBZ191_Reload");
            if (model == null) throw new InvalidOperationException("Split QBZ191 reload model not imported");
            var meshes = model.GetComponentsInChildren<MeshFilter>(true);
            var body = meshes.SingleOrDefault(m => m.name == "RifleBody" || m.sharedMesh.name == "RifleBody")?.sharedMesh;
            var mag = meshes.SingleOrDefault(m => m.name == "Magazine" || m.sharedMesh.name == "Magazine")?.sharedMesh;
            if(body==null||mag==null)throw new InvalidOperationException("Split mesh names: " + string.Join(", ",meshes.Select(m=>m.name+":"+m.sharedMesh.name)));
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var binding = root.GetComponent<WeaponPrefabBinding>();
                var original = binding.VisualRoot.GetComponentsInChildren<MeshFilter>(true)
                    .First(m => m.name == "QBZ" || m.transform.parent?.name == "Model_QBZ191");
                original.sharedMesh = body;
                var renderer = original.GetComponent<MeshRenderer>();
                const string materials = "Assets/SimulatedShooting/Prefabs/Weapons/QBC-191/materials/";
                var bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(materials + "QBZ191_Body_URP.mat");
                var magazineMaterial = AssetDatabase.LoadAssetAtPath<Material>(materials + "QBZ191_Magazine_URP.mat");
                if (bodyMaterial == null || magazineMaterial == null) throw new InvalidOperationException("QBZ191 materials are missing");
                renderer.sharedMaterials = new[] { bodyMaterial };
                var existing = original.transform.parent.Find("Visual_training-rifle_Magazine");
                var magazine = existing != null ? existing.gameObject : new GameObject("Visual_training-rifle_Magazine");
                magazine.transform.SetParent(original.transform.parent, false);
                magazine.transform.localPosition = original.transform.localPosition;
                magazine.transform.localRotation = original.transform.localRotation;
                magazine.transform.localScale = original.transform.localScale;
                var magazineFilter = magazine.GetComponent<MeshFilter>();
                if (magazineFilter == null) magazineFilter = magazine.AddComponent<MeshFilter>();
                magazineFilter.sharedMesh = mag;
                var magazineRenderer = magazine.GetComponent<MeshRenderer>();
                if (magazineRenderer == null) magazineRenderer = magazine.AddComponent<MeshRenderer>();
                magazineRenderer.sharedMaterial = magazineMaterial;
                var serialized = new SerializedObject(binding);
                serialized.FindProperty("magazineVisual").objectReferenceValue = magazine.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Repair P2 Reference Regression")]
        public static void RepairP2Reference()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Repair in Edit Mode");
            var active = SceneManager.GetActiveScene();
            const string p1Path = "Assets/Scenes/ZeroingRangeScene.unity";
            const string p2Path = "Assets/Scenes/MovingTargetRangeScene.unity";
            var p1 = SceneManager.GetSceneByPath(p1Path); var p2 = SceneManager.GetSceneByPath(p2Path);
            var ownP1 = !p1.isLoaded; var ownP2 = !p2.isLoaded;
            if (ownP1) p1 = EditorSceneManager.OpenScene(p1Path, OpenSceneMode.Additive);
            if (ownP2) p2 = EditorSceneManager.OpenScene(p2Path, OpenSceneMode.Additive);
            try
            {
                var copy = p2.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "ZeroingStartAreaCopy");
                if (copy.GetComponentsInChildren<Transform>(true).Any(t => t.name == "WeaponReference_Blockout")) return;
                var source = p1.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Single(t => t.name == "WeaponReference_Blockout");
                var clone = UnityEngine.Object.Instantiate(source.gameObject); clone.name = source.name;
                SceneManager.MoveGameObjectToScene(clone, p2); clone.transform.SetParent(copy, true);
                foreach (var id in clone.GetComponentsInChildren<SceneTestId>(true))
                    id.Id = id.Id.Replace("ZeroingRange.", "MovingTargetRange.");
                foreach (var node in clone.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = copy.gameObject.layer;
                EditorSceneManager.MarkSceneDirty(p2); EditorSceneManager.SaveScene(p2);
            }
            finally
            {
                if (ownP2) EditorSceneManager.CloseScene(p2, true);
                if (ownP1) EditorSceneManager.CloseScene(p1, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }
    }
}
