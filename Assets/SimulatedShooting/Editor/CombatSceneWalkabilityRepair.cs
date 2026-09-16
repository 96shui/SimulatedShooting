using System;
using System.Collections.Generic;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    public static class CombatSceneWalkabilityRepair
    {
        [MenuItem("Tools/Simulated Shooting/Scene 3/Repair Walkability And Open Houses")]
        public static void Apply()
        {
            if (!Application.isBatchMode && Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scene edits first.");
            var scene = EditorSceneManager.OpenScene(CombatSceneBuilder.ScenePath, OpenSceneMode.Single);
            var b = Object.FindObjectOfType<CombatSceneBindings>();
            if (b.GeometryRoot.Find("Town_ExteriorCollision") == null)
            {
                CombatSceneBoundaryRepair.Apply();
                scene = EditorSceneManager.GetActiveScene();
                b = Object.FindObjectOfType<CombatSceneBindings>();
            }
            var geometry = b.GeometryRoot;
            foreach (var name in new[] { "WestBoundary", "EastBoundary" })
            {
                var old = geometry.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var landscape = b.transform.Find("EnvironmentBackdrop/Landscape");
            if (landscape.GetComponent<BoxCollider>() == null) landscape.gameObject.AddComponent<BoxCollider>();
            foreach (Transform house in geometry.Find("Town_PerimeterBuildings")) OpenHouse(house, b);
            var railRoot = geometry.Find("WalkwaySafety");
            if (railRoot == null)
            {
                railRoot = new GameObject("WalkwaySafety").transform;
                railRoot.SetParent(geometry, false);
                var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/Steel.mat");
                foreach (var ramp in geometry.GetComponentsInChildren<BoxCollider>().Where(c => c.name == "StairRamp").ToArray())
                    foreach (int side in new[] { -1, 1 })
                    {
                        var rail = Box(railRoot, "StairGuard", ramp.transform.position + new Vector3(side * 1.5f, .75f, 0),
                            new Vector3(.12f, 1.25f, ramp.transform.localScale.z), metal);
                        rail.rotation = ramp.transform.rotation;
                    }
                // Exposed platform edges only; stair mouths and building entrances stay open.
                Rail(railRoot, new Vector3(3,3.6f,82), new Vector3(3,3.6f,86), metal);
                Rail(railRoot, new Vector3(3,3.6f,86), new Vector3(16,3.6f,86), metal);
                Rail(railRoot, new Vector3(3,3.6f,82), new Vector3(5.5f,3.6f,82), metal);
                Rail(railRoot, new Vector3(8.5f,3.6f,82), new Vector3(11.5f,3.6f,82), metal);
                Rail(railRoot, new Vector3(14.5f,3.6f,82), new Vector3(16,3.6f,82), metal);
                Rail(railRoot, new Vector3(3,7.2f,62), new Vector3(17,7.2f,62), metal);
                Rail(railRoot, new Vector3(3,7.2f,62), new Vector3(3,7.2f,66), metal);
                Rail(railRoot, new Vector3(3,7.2f,66), new Vector3(5.5f,7.2f,66), metal);
                Rail(railRoot, new Vector3(8.5f,7.2f,66), new Vector3(11.5f,7.2f,66), metal);
                Rail(railRoot, new Vector3(11.5f,7.2f,66), new Vector3(11.5f,7.2f,82), metal);
                Rail(railRoot, new Vector3(14.5f,7.2f,66), new Vector3(14.5f,7.2f,82), metal);
                Rail(railRoot, new Vector3(10,7.2f,82), new Vector3(11.5f,7.2f,82), metal);
                Rail(railRoot, new Vector3(14.5f,7.2f,82), new Vector3(16,7.2f,82), metal);
                Rail(railRoot, new Vector3(10,7.2f,82), new Vector3(10,7.2f,86), metal);
                Rail(railRoot, new Vector3(10,7.2f,86), new Vector3(16,7.2f,86), metal);
                foreach (float y in new[] { 3.6f, 7.2f })
                {
                    Rail(railRoot, new Vector3(16,y,64), new Vector3(22,y,64), metal);
                    Rail(railRoot, new Vector3(16,y,64), new Vector3(16,y,66), metal);
                }
            }
            if (railRoot.Find("TopLandingSideGuard") == null)
            {
                var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/Steel.mat");
                Rail(railRoot, new Vector3(17,7.2f,62), new Vector3(17,7.2f,64), metal);
                railRoot.GetChild(railRoot.childCount-1).name = "TopLandingSideGuard";
                Rail(railRoot, new Vector3(14.5f,7.2f,66), new Vector3(16,7.2f,66), metal);
            }
            Physics.SyncTransforms();
            // Include the entire expanded district, which exceeded the original bake bounds.
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(b.PlayerSpawn.position, Vector3.one);
            foreach (var c in geometry.GetComponentsInChildren<BoxCollider>())
            {
                if (!c.enabled || c.isTrigger) continue;
                bounds.Encapsulate(c.bounds);
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = c.transform.localToWorldMatrix * Matrix4x4.Translate(c.center), size = c.size, area = 0 });
            }
            bounds.Expand(2);
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = .3f; settings.agentHeight = 1.8f; settings.agentClimb = .35f; settings.agentSlope = 45;
            settings.overrideVoxelSize = true; settings.voxelSize = .1f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Navigation bake failed.");
            EditorUtility.CopySerialized(data, b.NavigationData);
            Object.DestroyImmediate(data);
            EditorUtility.SetDirty(b.NavigationData);
            CombatSceneBuilder.Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            CombatSceneBuilder.Capture();
        }

        static void OpenHouse(Transform house, CombatSceneBindings b)
        {
            var shell = house.Find("ClosedBuildingShell");
            if (shell == null) return;
            var size = shell.localScale;
            var material = shell.GetComponent<Renderer>().sharedMaterial;
            var doorX = house.Find("SealedDoor").localPosition.x;
            Object.DestroyImmediate(shell.gameObject);
            Object.DestroyImmediate(house.Find("SealedDoor").gameObject);
            var interior = new GameObject("Interior").transform;
            interior.SetParent(house, false);
            float left = -size.x / 2, right = size.x / 2, front = -size.z / 2;
            Box(interior, "WestWall", new Vector3(left,size.y/2,0), new Vector3(.3f,size.y,size.z), material);
            Box(interior, "EastWall", new Vector3(right,size.y/2,0), new Vector3(.3f,size.y,size.z), material);
            Box(interior, "RearWall", new Vector3(0,size.y/2,-front), new Vector3(size.x,size.y,.3f), material);
            Box(interior, "DoorLeft", new Vector3((left+doorX-1)/2,size.y/2,front), new Vector3(doorX-1-left,size.y,.3f), material);
            Box(interior, "DoorRight", new Vector3((right+doorX+1)/2,size.y/2,front), new Vector3(right-doorX-1,size.y,.3f), material);
            Box(interior, "DoorHeader", new Vector3(doorX,(size.y+2.5f)/2,front), new Vector3(2,size.y-2.5f,.3f), material);
            var floorMaterial = house.Find("Foundation").GetComponent<Renderer>().sharedMaterial;
            // Keep the existing plinth silhouette but bring the entrance below step height.
            house.Find("Foundation").localPosition = new Vector3(0,-.4f,0);
            Box(interior, "Floor", new Vector3(0,-.15f,0), new Vector3(size.x,.3f,size.z), floorMaterial);
            Box(interior, "Ceiling", new Vector3(0,3,0), new Vector3(size.x,.2f,size.z), floorMaterial);
            // Facade dressing at the opening must not visually seal the doorway.
            foreach (Transform child in house.Cast<Transform>().ToArray())
                if (child != interior && child.localPosition.y > .2f && child.localPosition.y < 2.6f &&
                    child.localPosition.z < front && Mathf.Abs(child.localPosition.x-doorX) < 1.8f)
                    Object.DestroyImmediate(child.gameObject);
            var dummy = Object.Instantiate(b.EnemyPrefab.VisualRoot.gameObject, interior).transform;
            dummy.name = "TrainingDummy";
            dummy.localPosition = new Vector3(doorX,0,0);
            dummy.localRotation = Quaternion.Euler(0,180,0);
            dummy.gameObject.AddComponent<SceneTestId>().Id = "CombatScene.Dummy." + house.name;
            var light = new GameObject("InteriorLight").AddComponent<Light>();
            light.transform.SetParent(interior, false); light.transform.localPosition = new Vector3(0,2.6f,0);
            light.type = LightType.Point; light.range = Mathf.Max(size.x,size.z); light.intensity = 1.5f;
            light.shadows = LightShadows.None;
            var entry = new GameObject("WalkEntrance").transform;
            entry.SetParent(interior, false); entry.localPosition = new Vector3(doorX,0,front);
        }

        static void Rail(Transform root, Vector3 a, Vector3 b, Material material)
        {
            var rail = Box(root, "PlatformGuard", (a+b)/2 + Vector3.up*.65f,
                new Vector3(.12f,1.3f,Vector3.Distance(a,b)+.12f), material);
            rail.rotation = Quaternion.LookRotation(b-a);
        }

        static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true;
            return go.transform;
        }
    }
}
