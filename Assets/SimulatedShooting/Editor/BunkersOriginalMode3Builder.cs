using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using VRShooting.Common;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    // Imports the pack's saved demonstration layout into a separate production scene.
    // The saved scene, rather than this one-time migration, is authoritative afterward.
    public static class BunkersOriginalMode3Builder
    {
        public const string ScenePath = "Assets/Scenes/BunkersOriginalMode3CombatScene.unity";
        const string PreviewScene = "Assets/Scenes/BunkersTrenchesOriginalPreview.unity";
        const string PreviousCombatScene = "Assets/Scenes/BunkersTrenchCombatScene.unity";
        const string Art = "Assets/SimulatedShooting/Art/Combat/BunkersTrenchesPack";
        const string NavigationPath = Art + "/OriginalDemoNavigation.asset";
        const string MapPath = Art + "/OriginalDemoMap.png";
        const string PreviewPath = "Screenshots/OriginalDemoMode3Entry.png";
        static readonly Vector2 MapMin = new Vector2(255, 215);
        static readonly Vector2 MapMax = new Vector2(385, 345);

        // Updates a saved scene created with an earlier estimate radius.
        public static void RepairEstimatePositions()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var binding = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
            var terrain = binding.GeometryRoot.GetComponentInChildren<Terrain>(true);
            var spawns = binding.Points.Where(p => p.Kind == CombatPointKind.EnemySpawn).OrderBy(p => p.Id).ToArray();
            for (var i = 0; i < spawns.Length; i++)
            {
                var spawn = spawns[i];
                var offset = new Vector2(1, i % 2 == 0 ? .5f : -.5f);
                var position = At(terrain, spawn.transform.position.x + offset.x, spawn.transform.position.z + offset.y);
                spawn.EstimateAnchor.position = position;
                var area = binding.Points.Single(p => p.Id == "trench-estimate-" + (i + 1));
                area.transform.position = position;
            }
            var navigation = NavMesh.AddNavMeshData(binding.NavigationData);
            try
            {
                var definition = CombatSceneDefinitionBuilder.Build(binding, TrainingMode.Trench);
                var valid = definition.Validate();
                if (!valid.Success) throw new InvalidOperationException(valid.Message);
            }
            finally { if (navigation.valid) navigation.Remove(); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Original demo estimate positions repaired.");
        }

        [MenuItem("Tools/Simulated Shooting/Scene 3/Create Original Demo Mode 3 Scene")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Original demo combat scene already exists; preserve saved scene edits.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScene) == null)
                throw new FileNotFoundException(PreviewScene);

            var preview = EditorSceneManager.OpenScene(PreviewScene, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(preview, ScenePath, true))
                throw new InvalidOperationException("Could not copy original demo scene");
            AssetDatabase.ImportAsset(ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var previous = EditorSceneManager.OpenScene(PreviousCombatScene, OpenSceneMode.Additive);
            var binding = previous.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
            binding.enabled = false;
            SceneManager.MoveGameObjectToScene(binding.gameObject, scene);
            EditorSceneManager.CloseScene(previous, true);
            SceneManager.SetActiveScene(scene);
            binding.gameObject.name = "BunkersOriginalMode3CombatScene";

            var demoCamera = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Main Camera");
            if (demoCamera != null) Object.DestroyImmediate(demoCamera);
            foreach (Transform child in binding.GeometryRoot.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);
            var oldBackdrop = binding.transform.Find("EnvironmentBackdrop");
            if (oldBackdrop != null) Object.DestroyImmediate(oldBackdrop.gameObject);
            foreach (var point in binding.Points)
                if (point != null) Object.DestroyImmediate(point.gameObject);
            foreach (var door in binding.Doors)
                if (door != null) Object.DestroyImmediate(door.gameObject);
            binding.Points = Array.Empty<CombatScenePoint>();
            binding.Doors = Array.Empty<CombatDoorView>();
            binding.NavigationData = null;

            var terrainObject = scene.GetRootGameObjects().Single(r => r.name == "Terrain");
            var demoObject = scene.GetRootGameObjects().Single(r => r.name == "Scene");
            terrainObject.transform.SetParent(binding.GeometryRoot, true);
            demoObject.transform.SetParent(binding.GeometryRoot, true);
            var terrain = terrainObject.GetComponent<Terrain>();
            if (terrain == null || terrain.GetComponent<TerrainCollider>() == null)
                throw new InvalidOperationException("Demo terrain and collision are required");
            AddBunkerCollision(demoObject.transform.Find("Bunkers"));
            AddTrenchFloorCollision(demoObject.transform.Find("Trenches"));

            var forward = Quaternion.Euler(0, 90, 0);
            SetAnchor(binding.PlayerSpawn, At(terrain, 290, 283), forward);
            SetAnchor(binding.TrenchEntry, binding.PlayerSpawn.position, forward);
            SetAnchor(binding.UrbanEntry, binding.PlayerSpawn.position, forward);
            SetAnchor(binding.TeammateSpawns[0], At(terrain, 288.5f, 283), forward);
            SetAnchor(binding.TeammateSpawns[1], At(terrain, 287, 283), forward);
            SetAnchor(binding.WeaponAnchor, binding.PlayerSpawn.TransformPoint(new Vector3(.22f, 1.25f, .65f)), forward);
            SetAnchor(binding.BriefingAnchor, binding.PlayerSpawn.position + new Vector3(1.5f, 1.85f, 0), forward);
            SetAnchor(binding.HudAnchor, binding.PlayerSpawn.position + new Vector3(1.5f, 1.95f, 0), forward);
            SetAnchor(binding.ResultsAnchor, binding.PlayerSpawn.position + new Vector3(1.5f, 1.85f, 0), forward);
            SetAnchor(binding.ProjectionAnchor, binding.PlayerSpawn.position + new Vector3(1.6f, 1.65f, -.7f), forward);
            SetAnchor(binding.Drone, binding.PlayerSpawn.position + new Vector3(0, 3.2f, 1), forward);
            var inspectionPlayer = binding.transform.Find("Player_Inspection");
            if (inspectionPlayer != null) SetAnchor(inspectionPlayer, binding.PlayerSpawn.position, forward);
            var xrOrigin = binding.transform.Find("XR Origin (VR)");
            if (xrOrigin != null) SetAnchor(xrOrigin, binding.PlayerSpawn.position, forward);

            var points = new List<CombatScenePoint>();
            var nodes = new[]
            {
                new Vector2(299, 282), new Vector2(307, 282), new Vector2(311, 282),
                new Vector2(322, 282), new Vector2(333, 282), new Vector2(345, 281),
                new Vector2(333, 269), new Vector2(332, 253)
            };
            for (var i = 0; i < nodes.Length; i++)
                points.Add(Point(binding.transform, terrain, "trench-node-" + (i + 1), CombatPointKind.SearchNode, nodes[i]));
            var candidates = new[]
            {
                new Vector2(307, 282), new Vector2(311, 282), new Vector2(322, 282),
                new Vector2(337, 282), new Vector2(345, 281), new Vector2(333, 268),
                new Vector2(332, 253)
            };
            for (var i = 0; i < candidates.Length; i++)
            {
                var spawn = Point(binding.transform, terrain, "trench-spawn-" + (i + 1), CombatPointKind.EnemySpawn, candidates[i]);
                spawn.transform.rotation = Quaternion.LookRotation((binding.PlayerSpawn.position - spawn.transform.position).normalized);
                var estimated = candidates[i] + new Vector2(1, i % 2 == 0 ? .5f : -.5f);
                var estimate = new GameObject("EstimateAnchor").transform;
                estimate.SetParent(spawn.transform, false);
                estimate.position = At(terrain, estimated.x, estimated.y);
                spawn.EstimateAnchor = estimate;
                points.Add(spawn);
                points.Add(Point(binding.transform, terrain, "trench-estimate-" + (i + 1), CombatPointKind.EstimateArea, estimated));
            }
            points.Add(Point(binding.transform, terrain, "trench-corner-1", CombatPointKind.Corner, new Vector2(326, 282)));
            points.Add(Point(binding.transform, terrain, "trench-corner-2", CombatPointKind.Corner, new Vector2(333, 273)));
            binding.Points = points.ToArray();

            BuildNavigation(binding);
            binding.Maps = new[] { new CombatMapBinding { Id = "trench-a", Min = MapMin, Max = MapMax, Plan = CapturePlan() } };
            binding.enabled = true;
            var errors = binding.ValidateBindings();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
            CheckNavigation(binding);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save combat scene");
            CaptureEntry(binding);
            var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath)
                .Select(s => s.path == PreviousCombatScene ? new EditorBuildSettingsScene(s.path, false) : s).ToList();
            buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Original demo Mode 3 scene saved: " + ScenePath);
        }

        static Vector3 At(Terrain terrain, float x, float z)
        {
            var approximate = new Vector3(x, 0, z);
            return new Vector3(x, terrain.SampleHeight(approximate) + terrain.transform.position.y, z);
        }

        static void SetAnchor(Transform anchor, Vector3 position, Quaternion rotation)
        {
            if (anchor == null) throw new InvalidOperationException("Missing combat anchor");
            anchor.SetPositionAndRotation(position, rotation);
        }

        static CombatScenePoint Point(Transform parent, Terrain terrain, string id, CombatPointKind kind, Vector2 xz)
        {
            var objectName = new GameObject("Point_" + id);
            objectName.transform.SetParent(parent, false);
            objectName.transform.position = At(terrain, xz.x, xz.y);
            var point = objectName.AddComponent<CombatScenePoint>();
            point.Id = id;
            point.RegionId = "trench-a";
            point.Kind = kind;
            var volume = objectName.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.center = Vector3.up;
            volume.size = new Vector3(3.2f, 2.6f, 3.2f);
            point.Volume = volume;
            return point;
        }

        static void AddBunkerCollision(Transform bunkerRoot)
        {
            if (bunkerRoot == null) throw new InvalidOperationException("Demo bunker root missing");
            foreach (var filter in bunkerRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Renderer>() == null) continue;
                if (filter.name.Contains("LOD1") || filter.name.Contains("LOD2")) continue;
                if (filter.GetComponent<Collider>() != null) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }

        static void AddTrenchFloorCollision(Transform trenchRoot)
        {
            if (trenchRoot == null) throw new InvalidOperationException("Demo trench root missing");
            foreach (var filter in trenchRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Renderer>() == null) continue;
                if (!filter.name.StartsWith("trench01_", StringComparison.Ordinal)
                    && !filter.name.StartsWith("TrenchShelter", StringComparison.Ordinal)) continue;
                if (filter.name.Contains("LOD1") || filter.name.Contains("LOD2")) continue;
                if (filter.GetComponent<Collider>() != null) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }

        static void BuildNavigation(CombatSceneBindings binding)
        {
            var terrain = binding.GeometryRoot.GetComponentInChildren<Terrain>();
            var sources = new List<NavMeshBuildSource>();
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Terrain,
                sourceObject = terrain.terrainData,
                transform = terrain.transform.localToWorldMatrix,
                area = 0
            });
            var route = new[]
            {
                new Vector2(287, 283), new Vector2(290, 283), new Vector2(299, 282),
                new Vector2(307, 282), new Vector2(311, 282), new Vector2(321, 282),
                new Vector2(333, 282), new Vector2(345, 281)
            };
            for (var i = 0; i < route.Length - 1; i++)
                AddNavigationCorridor(sources, terrain, route[i], route[i + 1]);
            AddNavigationCorridor(sources, terrain, new Vector2(333, 282), new Vector2(333, 269));
            AddNavigationCorridor(sources, terrain, new Vector2(333, 269), new Vector2(332, 253));
            Debug.Log("Original demo navigation sources: " + sources.Count);
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = .28f;
            settings.agentHeight = 1.8f;
            settings.agentClimb = .45f;
            settings.agentSlope = 45;
            settings.overrideVoxelSize = true;
            settings.voxelSize = .08f;
            var bounds = new Bounds(new Vector3(320, 3, 280), new Vector3(140, 45, 140));
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Original demo navigation bake failed");
            AssetDatabase.CreateAsset(data, NavigationPath);
            binding.NavigationData = data;
        }

        static void AddNavigationCorridor(List<NavMeshBuildSource> sources, Terrain terrain, Vector2 from, Vector2 to)
        {
            var start = At(terrain, from.x, from.y) + Vector3.up * .08f;
            var end = At(terrain, to.x, to.y) + Vector3.up * .08f;
            var direction = end - start;
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS((start + end) * .5f, Quaternion.LookRotation(direction), Vector3.one),
                size = new Vector3(2.8f, .14f, direction.magnitude + .5f),
                area = 0
            });
        }

        static void CheckNavigation(CombatSceneBindings binding)
        {
            var instance = NavMesh.AddNavMeshData(binding.NavigationData);
            try
            {
                var triangles = NavMesh.CalculateTriangulation();
                Debug.Log("Original demo navigation vertices: " + triangles.vertices.Length);
                if (triangles.vertices.Length > 0)
                {
                    var min = triangles.vertices[0];
                    var max = min;
                    foreach (var vertex in triangles.vertices)
                    {
                        min = Vector3.Min(min, vertex);
                        max = Vector3.Max(max, vertex);
                    }
                    Debug.Log($"Original demo navigation bounds: {min} to {max}; entry {binding.TrenchEntry.position}");
                }
                if (NavMesh.SamplePosition(binding.TrenchEntry.position, out var nearest, 100, NavMesh.AllAreas))
                    Debug.Log($"Nearest original demo navigation: {nearest.position}, distance {Vector3.Distance(nearest.position, binding.TrenchEntry.position):F2}");
                if (!NavMesh.SamplePosition(binding.TrenchEntry.position, out var start, 1.2f, NavMesh.AllAreas))
                    throw new InvalidOperationException("Player entry is not on navigation");
                foreach (var transform in binding.TeammateSpawns.Concat(binding.Points.Where(p => p.RequiresNavigation).Select(p => p.transform)))
                {
                    if (!NavMesh.SamplePosition(transform.position, out var hit, 1.2f, NavMesh.AllAreas))
                        throw new InvalidOperationException("Off navigation: " + transform.name);
                    var path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException("Unreachable: " + transform.name);
                }
            }
            finally { if (instance.valid) instance.Remove(); }
        }

        static Texture2D CapturePlan()
        {
            Render(new Vector3(320, 120, 280), Quaternion.Euler(90, 0, 0), true, 65,
                MapPath, 1024, 1024);
            AssetDatabase.ImportAsset(MapPath);
            var importer = AssetImporter.GetAtPath(MapPath) as TextureImporter;
            if (importer != null)
            {
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
        }

        static void CaptureEntry(CombatSceneBindings binding)
        {
            Directory.CreateDirectory("Screenshots");
            var from = binding.TrenchEntry.position + Vector3.up * 1.65f;
            Render(from, binding.TrenchEntry.rotation, false, 70, PreviewPath, 1600, 900);
        }

        static void Render(Vector3 position, Quaternion rotation, bool orthographic, float viewSize,
            string path, int width, int height)
        {
            var cameraObject = new GameObject("TemporaryMode3Capture");
            var camera = cameraObject.AddComponent<Camera>();
            var oldFog = RenderSettings.fog;
            var oldActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D pixels = null;
            try
            {
                RenderSettings.fog = false;
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.orthographic = orthographic;
                if (orthographic) camera.orthographicSize = viewSize;
                else camera.fieldOfView = viewSize;
                camera.nearClipPlane = .05f;
                camera.farClipPlane = orthographic ? 250 : 500;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.22f, .23f, .24f);
                target = new RenderTexture(width, height, 24);
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderSettings.fog = oldFog;
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (pixels != null) Object.DestroyImmediate(pixels);
                if (target != null) Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
