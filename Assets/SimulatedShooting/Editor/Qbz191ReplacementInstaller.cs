using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class Qbz191ReplacementInstaller
    {
        const string Folder = "Assets/SimulatedShooting/Prefabs/Weapons/QBZ191_New/";
        const string Prefab = TrainingRiflePrefabBuilder.PrefabPath;

        [MenuItem("Tools/Simulated Shooting/Install New QBZ191")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode first.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "QBZ191.fbx");
            if (model == null) throw new InvalidOperationException("New QBZ191 FBX is missing.");
            var bodyMaterial = Material("01", "QBZ191_New_Body");
            var magazineMaterial = Material("02", "QBZ191_Magazine_New");
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var binding = root.GetComponent<WeaponPrefabBinding>();
                var previous = binding.RecoilRoot.Find("Model_QBZ191");
                if (previous == null) throw new InvalidOperationException("Original weapon visual is missing.");
                previous.gameObject.SetActive(false);
                var existing = binding.RecoilRoot.Find("Model_QBZ191_New");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var fresh = (GameObject)PrefabUtility.InstantiatePrefab(model, binding.RecoilRoot);
                fresh.name = "Model_QBZ191_New";
                fresh.transform.localPosition = new Vector3(-.006f, -.108f, -.160f);
                fresh.transform.localRotation = Quaternion.Euler(0, -90, 0);
                fresh.transform.localScale = Vector3.one;
                Transform magazine = null;
                foreach (var renderer in fresh.GetComponentsInChildren<Renderer>(true))
                {
                    var isMagazine = renderer.name == "QBZ191_0001";
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => isMagazine ? magazineMaterial : bodyMaterial).ToArray();
                    if (isMagazine) magazine = renderer.transform;
                }
                if (magazine == null) throw new InvalidOperationException("New model magazine is missing.");
                var serialized = new SerializedObject(binding);
                serialized.FindProperty("magazineVisual").objectReferenceValue = magazine;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                binding.MagazinePoint.localPosition = new Vector3(-.005f, -.120f, .025f);
                binding.MuzzlePoint.localPosition = new Vector3(-.0045f, -.041f, .455f);
                binding.AimLinePoint.localPosition = new Vector3(-.0048f, .052f, -.140f);
                binding.AimLinePoint.localRotation = Quaternion.identity;
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("New QBZ191 installed, preserving the original hand anchors.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Material Material(string suffix, string name)
        {
            var basePath = Folder + "QBZ191_" + suffix;
            var normalPath = basePath + "_Normal.png";
            var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            var matPath = Folder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, matPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_Base_Color.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", .25f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
