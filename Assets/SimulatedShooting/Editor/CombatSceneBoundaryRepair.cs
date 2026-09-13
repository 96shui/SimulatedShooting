using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    public static class CombatSceneBoundaryRepair
    {
        [MenuItem("Tools/Simulated Shooting/Scene 3/Repair Exterior Collision")]
        public static void Apply()
        {
            if (!Application.isBatchMode && Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scene edits first.");
            var scene = EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath, OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            // The visible landscape previously had no collision at all.
            var landscape = bindings.transform.Find("EnvironmentBackdrop/Landscape");
            if (landscape.GetComponent<BoxCollider>() == null) landscape.gameObject.AddComponent<BoxCollider>();
            if (bindings.GeometryRoot.Find("Town_ExteriorCollision") == null)
            {
                var root = new GameObject("Town_ExteriorCollision").transform;
                root.SetParent(bindings.GeometryRoot, false);
                var earth = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/Earth.mat");
                var wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/Concrete.mat");
                Box(root, "TownGround", new Vector3(20, -.3f, 71), new Vector3(90, .4f, 86), earth);
                Box(root, "WestWall", new Vector3(-25, 1.5f, 71), new Vector3(.6f, 3.2f, 86.6f), wall);
                Box(root, "EastWall", new Vector3(65, 1.5f, 71), new Vector3(.6f, 3.2f, 86.6f), wall);
                Box(root, "NorthWall", new Vector3(20, 1.5f, 114), new Vector3(90.6f, 3.2f, .6f), wall);
                // Leave the existing 4.4m trench connection at x=12 open.
                Box(root, "SouthWestWall", new Vector3(-7.6f, 1.5f, 28), new Vector3(34.8f, 3.2f, .6f), wall);
                Box(root, "SouthEastWall", new Vector3(39.6f, 1.5f, 28), new Vector3(50.8f, 3.2f, .6f), wall);
            }
            Physics.SyncTransforms();
            CombatSceneBuilder.Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.isStatic = true;
        }
    }
}
