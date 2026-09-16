using System;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimulatedShooting.Editor
{
    [InitializeOnLoad]
    static class CombatTaipeiBuildingBackdropAutoRun
    {
        static readonly string Marker = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/apply-combat-taipei-building-backdrop"));

        static CombatTaipeiBuildingBackdropAutoRun()
        {
            if (File.Exists(Marker)) EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                Debug.LogError("Taipei building backdrop paused because the open scene has unsaved changes.");
                return;
            }

            try
            {
                File.Delete(Marker);
                CombatTaipeiBuildingBackdrop.Apply();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    public static class CombatTaipeiBuildingBackdrop
    {
        const string ScenePath = "Assets/Scenes/CombatScene.unity";
        const string AssetRoot = "Assets/SimulatedShooting/Art/Combat/TaipeiGovBuildings";

        static readonly string[] NodeIds = { "321214", "509040", "854014" };

        static readonly Vector3[] Positions =
        {
            new Vector3(-38, 0, 34), new Vector3(-38, 0, 54), new Vector3(-38, 0, 76), new Vector3(-38, 0, 99),
            new Vector3(78, 0, 35), new Vector3(78, 0, 57), new Vector3(78, 0, 80), new Vector3(78, 0, 102),
            new Vector3(-12, 0, 128), new Vector3(8, 0, 128), new Vector3(31, 0, 128), new Vector3(55, 0, 128),
            new Vector3(-10, 0, 17), new Vector3(43, 0, 17)
        };

        [MenuItem("Tools/Simulated Shooting/Scene 3/Replace Earth Mounds With Taipei Buildings")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bindings = UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            if (bindings == null) throw new InvalidOperationException("CombatScene bindings are missing.");
            var environment = bindings.transform.Find("EnvironmentBackdrop");
            if (environment == null) throw new InvalidOperationException("EnvironmentBackdrop is missing.");

            foreach (var mound in environment.GetComponentsInChildren<Transform>(true)
                         .Where(t => t.name == "DistantEarth").ToArray())
                UnityEngine.Object.DestroyImmediate(mound.gameObject);

            var previous = environment.Find("TaipeiGovBuildingBackdrop");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root = new GameObject("TaipeiGovBuildingBackdrop").transform;
            root.SetParent(environment, false);

            var centre = new Vector3(20, 0, 71);
            for (var i = 0; i < Positions.Length; i++)
            {
                var nodeId = NodeIds[i % NodeIds.Length];
                var modelPath = $"{AssetRoot}/{nodeId}/building.obj";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (source == null) throw new InvalidOperationException($"Taipei building model did not import: {modelPath}");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root);
                instance.name = $"TaipeiBuilding_{i + 1:00}_Node{nodeId}";
                instance.transform.position = Positions[i];
                instance.transform.localScale = Vector3.one * BackdropScale(nodeId);
                var direction = centre - Positions[i];
                instance.transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                instance.isStatic = true;
                var material = AssetDatabase.LoadAssetAtPath<Material>($"{AssetRoot}/{nodeId}/TaipeiFacade.mat");
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            CombatTaiwanStreetArt.Capture();
            Debug.Log("Replaced DistantEarth mounds with 14 Taipei government building meshes.");
        }

        static float BackdropScale(string nodeId)
        {
            return nodeId == "854014" ? 2.2f : 2.5f;
        }

        static void ConfigureAssets()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            foreach (var nodeId in NodeIds)
            {
                var folder = $"{AssetRoot}/{nodeId}";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/albedo.jpg");
                if (texture == null) throw new InvalidOperationException($"Taipei texture did not import for node {nodeId}.");
                var materialPath = $"{folder}/TaipeiFacade.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = $"TaipeiFacade_{nodeId}" };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.shader = shader;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", .08f);
                EditorUtility.SetDirty(material);
            }
        }
    }
}
