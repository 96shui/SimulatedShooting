using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    // One-time scene migration. The saved scene is authoritative after creation.
    public static class BunkersTrenchSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BunkersTrenchCombatScene.unity";
        const string OriginalScene = "Assets/Scenes/CombatScene.unity";
        const string Art = "Assets/SimulatedShooting/Art/Combat/BunkersTrenchesPack";
        const string NavigationPath = Art + "/BunkersTrenchNavigation.asset";
        const string MapPath = Art + "/BunkersTrenchMap.png";
        const string PreviewPath = Art + "/BunkersTrenchPreview.png";
        static readonly Vector2 MapMin = new Vector2(-32, -12);
        static readonly Vector2 MapMax = new Vector2(48, 68);

        [MenuItem("Tools/Simulated Shooting/Scene 3/Create Bunkers Trench Scene")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Bunkers trench scene already exists. Preserve saved scene edits.");
            File.Copy(OriginalScene, ScenePath);
            AssetDatabase.ImportAsset(ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var binding = Object.FindObjectOfType<CombatSceneBindings>();
            if (binding == null || binding.GeometryRoot == null) throw new InvalidOperationException("Copied scene has no combat bindings.");
            binding.enabled = false; // Removes the copied scene's old navigation data instance.
            binding.gameObject.name = "BunkersTrenchCombatScene";

            // The original scene also stores its visual polish outside the bound geometry root.
            foreach (var root in scene.GetRootGameObjects())
                if (root != binding.gameObject && root.name != "Sun") Object.DestroyImmediate(root);

            foreach (Transform child in binding.GeometryRoot.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var backdrop = binding.transform.Find("EnvironmentBackdrop");
            if (backdrop != null) Object.DestroyImmediate(backdrop.gameObject);
            foreach (var point in binding.Points) if (point != null) Object.DestroyImmediate(point.gameObject);
            foreach (var door in binding.Doors) if (door != null) Object.DestroyImmediate(door.gameObject);
            binding.Points = Array.Empty<CombatScenePoint>();
            binding.Doors = Array.Empty<CombatDoorView>();

            var geometry = binding.GeometryRoot;
            var trench = Material("TrenchEarth", "Trench01", new Color(.86f, .81f, .73f));
            var observation = Material("ObservationPost", "ObservationPost", Color.white);
            var shelter = Material("TrenchShelter", "TrenchShelter", Color.white);
            var bunker = Material("Casemate", "Casemate01", Color.white);

            AddModel(geometry, "Pack_Entry", "Trench01_Out", new Vector2(-16, 0), 0, trench);
            AddModel(geometry, "Pack_Straight", "Trench01", new Vector2(0, 0), 0, trench);
            AddModel(geometry, "Pack_Crossroads", "Trench01_Cross", new Vector2(16, 0), 0, trench);
            AddModel(geometry, "Pack_North01", "Trench01", new Vector2(16, 16), -90, trench);
            AddModel(geometry, "Pack_North02", "Trench01", new Vector2(16, 32), -90, trench);
            AddModel(geometry, "Pack_NorthEnd", "Trench01_Out", new Vector2(16, 48), -90, trench);
            AddModel(geometry, "Pack_EastBranch", "Trench02", new Vector2(32, 0), 0, trench);
            AddModel(geometry, "Pack_Observation", "ObservationPost", new Vector2(24, 28), 0, observation);
            AddModel(geometry, "Pack_Shelter", "TrenchShelter", new Vector2(26, 43), 0, shelter);
            AddModel(geometry, "Pack_Bunker", "Casemate01", new Vector2(25, 57), 0, bunker);

            // The photogrammetry contains uneven terrain and solid triangles inside its
            // visible trench. Gameplay uses one continuous corridor instead of those meshes.
            var floorColliders = new[]
            {
                WalkableFloor(geometry, "WestTrench", new Vector3(-3.1f, .25f, 0), new Vector3(23.8f, .20f, 1.5f)),
                WalkableSegment(geometry, "TurnOut", new Vector2(7.8f, 0), new Vector2(12, -1.2f), 1.6f),
                WalkableSegment(geometry, "TurnIn", new Vector2(12, -1.2f), new Vector2(16.5f, 0), 1.6f),
                WalkableFloor(geometry, "NorthTrench", new Vector3(16.5f, .25f, 18.5f), new Vector3(1.6f, .20f, 37f))
            };
            CollisionWall(geometry, "WestLeft", new Vector3(-4, 1.1f, -1.45f), new Vector3(22.8f, 1.8f, .5f));
            CollisionWall(geometry, "WestRight", new Vector3(-4, 1.1f, 1.45f), new Vector3(22.8f, 1.8f, .5f));
            CollisionWallSegment(geometry, "TurnOut", new Vector2(7.8f, 0), new Vector2(12, -1.2f));
            CollisionWallSegment(geometry, "TurnIn", new Vector2(12, -1.2f), new Vector2(16.5f, 0));
            CollisionWall(geometry, "NorthLeft", new Vector3(15.05f, 1.1f, 20), new Vector3(.5f, 1.8f, 34));
            CollisionWall(geometry, "NorthRight", new Vector3(17.95f, 1.1f, 20), new Vector3(.5f, 1.8f, 34));
            foreach (var seam in new[] { new Vector2(-8, 0), new Vector2(8, 0), new Vector2(16.5f, 8), new Vector2(16.5f, 24) })
                Bridge(geometry, seam, trench);
            var ground = Material("TerrainEarth", "Trench01", new Color(.65f, .60f, .50f));
            ground.SetTextureScale("_BaseMap", new Vector2(15, 15));
            Backdrop(geometry, ground);

            var forward = Quaternion.Euler(0, 90, 0);
            SetAnchor(binding.PlayerSpawn, new Vector3(-11, .35f, 0), forward);
            SetAnchor(binding.TrenchEntry, new Vector3(-11, .35f, 0), forward);
            SetAnchor(binding.UrbanEntry, new Vector3(-11, .35f, 0), forward);
            SetAnchor(binding.TeammateSpawns[0], new Vector3(-12.5f, .35f, 0), forward);
            SetAnchor(binding.TeammateSpawns[1], new Vector3(-14, .35f, 0), forward);
            SetAnchor(binding.WeaponAnchor, new Vector3(-10.5f, 1.35f, 3.2f), forward);
            SetAnchor(binding.BriefingAnchor, new Vector3(-10, 1.85f, 0), forward);
            SetAnchor(binding.HudAnchor, new Vector3(-10, 1.95f, 0), forward);
            SetAnchor(binding.ResultsAnchor, new Vector3(-10, 1.85f, 0), forward);
            SetAnchor(binding.ProjectionAnchor, new Vector3(-10, 1.65f, -1), forward);
            SetAnchor(binding.Drone, new Vector3(-11, 2.55f, 1), forward);
            var inspectionPlayer = binding.transform.Find("Player_Inspection");
            if (inspectionPlayer != null) SetAnchor(inspectionPlayer, binding.PlayerSpawn.position, forward);
            var xrOrigin = binding.transform.Find("XR Origin (VR)");
            if (xrOrigin != null) SetAnchor(xrOrigin, binding.PlayerSpawn.position, forward);

            var points = new List<CombatScenePoint>();
            var nodes = new[]
            {
                new Vector3(-11, .35f, 0), new Vector3(-4, .35f, 0), new Vector3(4, .35f, 0),
                new Vector3(12, .35f, -1.2f), new Vector3(16.5f, .35f, 10),
                new Vector3(16.5f, .35f, 20), new Vector3(16.5f, .35f, 28), new Vector3(16.5f, .35f, 36)
            };
            for (int i = 0; i < nodes.Length; i++) points.Add(Point(binding.transform, "trench-node-" + (i + 1), CombatPointKind.SearchNode, nodes[i]));
            var spawns = new[]
            {
                new Vector3(0, .35f, 0), new Vector3(12, .35f, -1.2f),
                new Vector3(16.5f, .35f, 13), new Vector3(16.5f, .35f, 29), new Vector3(16.5f, .35f, 36)
            };
            for (int i = 0; i < spawns.Length; i++)
            {
                var spawn = Point(binding.transform, "trench-spawn-" + (i + 1), CombatPointKind.EnemySpawn, spawns[i]);
                var estimatePosition = spawns[i] + (i < 2 ? Vector3.right : Vector3.forward) * 1.25f;
                var estimate = new GameObject("EstimateAnchor").transform;
                estimate.SetParent(spawn.transform, false);
                estimate.position = estimatePosition;
                spawn.EstimateAnchor = estimate;
                points.Add(spawn);
                points.Add(Point(binding.transform, "trench-estimate-" + (i + 1), CombatPointKind.EstimateArea, estimatePosition));
            }
            points.Add(Point(binding.transform, "trench-corner-1", CombatPointKind.Corner, new Vector3(14, .35f, -1)));
            points.Add(Point(binding.transform, "trench-corner-2", CombatPointKind.Corner, new Vector3(16.5f, .35f, 8)));
            binding.Points = points.ToArray();

            var navSources = floorColliders.Select(c => new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = c.transform.localToWorldMatrix * Matrix4x4.Translate(c.center),
                size = c.size,
                area = 0
            }).ToList();
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = .3f;
            settings.agentHeight = 1.8f;
            settings.agentClimb = .35f;
            settings.agentSlope = 45;
            settings.overrideVoxelSize = true;
            settings.voxelSize = .08f;
            var nav = NavMeshBuilder.BuildNavMeshData(settings, navSources,
                new Bounds(new Vector3(4, 2, 26), new Vector3(92, 12, 92)), Vector3.zero, Quaternion.identity);
            if (nav == null) throw new InvalidOperationException("Bunkers trench navigation bake failed.");
            AssetDatabase.CreateAsset(nav, NavigationPath);
            binding.NavigationData = nav;
            binding.Maps = new[] { new CombatMapBinding { Id = "trench-a", Min = MapMin, Max = MapMax, Plan = CapturePlan() } };
            binding.enabled = true;
            CapturePreview();

            var errors = binding.ValidateBindings();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Bunkers trench Mode 3 scene saved: " + ScenePath);
        }

        static Material Material(string materialName, string textureName, Color color)
        {
            var folder = Art + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Art, "Materials");
            var path = folder + "/" + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/" + textureName + ".jpg"));
            material.SetFloat("_Smoothness", .08f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void AddModel(Transform root, string label, string assetName, Vector2 center, float yaw, Material material)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Models/" + assetName + ".FBX");
            if (asset == null) throw new FileNotFoundException(assetName + ".FBX");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
            instance.name = label;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            foreach (var group in instance.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(group);
            var active = new List<Renderer>();
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var name = renderer.name;
                renderer.enabled = !name.Contains("LOD") || name.EndsWith("LOD0", StringComparison.OrdinalIgnoreCase);
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                if (!renderer.enabled) continue;
                renderer.gameObject.layer = 31;
                active.Add(renderer);
                // Collision is authored separately from this uneven visual-only mesh.
            }
            if (active.Count == 0) throw new InvalidOperationException("No visible mesh in " + assetName);
            var bounds = active[0].bounds;
            foreach (var renderer in active.Skip(1)) bounds.Encapsulate(renderer.bounds);
            instance.transform.position += new Vector3(center.x - bounds.center.x, 0, center.y - bounds.center.z);
        }

        static BoxCollider WalkableFloor(Transform root, string label, Vector3 position, Vector3 size)
        {
            var go = new GameObject("WalkableFloor_" + label);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        static BoxCollider WalkableSegment(Transform root, string label, Vector2 start, Vector2 end, float width)
        {
            var from = new Vector3(start.x, .25f, start.y);
            var to = new Vector3(end.x, .25f, end.y);
            var go = new GameObject("WalkableFloor_" + label);
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation((from + to) * .5f, Quaternion.LookRotation(to - from));
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, .20f, Vector3.Distance(from, to) + .8f);
            return collider;
        }

        static void CollisionWall(Transform root, string label, Vector3 position, Vector3 size)
        {
            var go = new GameObject("GameplayWall_" + label);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        static void CollisionWallSegment(Transform root, string label, Vector2 start, Vector2 end)
        {
            var from = new Vector3(start.x, 1.1f, start.y);
            var to = new Vector3(end.x, 1.1f, end.y);
            var rotation = Quaternion.LookRotation(to - from);
            var right = rotation * Vector3.right;
            for (int side = -1; side <= 1; side += 2)
            {
                var go = new GameObject("GameplayWall_" + label + (side < 0 ? "Left" : "Right"));
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation((from + to) * .5f + right * (side * 1.25f), rotation);
                go.AddComponent<BoxCollider>().size = new Vector3(.35f, 1.8f, Vector3.Distance(from, to) - .4f);
            }
        }

        static void Bridge(Transform root, Vector2 center, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Pack_CorridorBridge";
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(center.x, .06f, center.y);
            go.transform.localScale = new Vector3(center.x == -8 || center.x == 8 ? 1.6f : 1.25f, .08f,
                center.x == -8 || center.x == 8 ? 1.25f : 1.6f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.layer = 31;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static void Backdrop(Transform root, Material material)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Pack_BackdropGround";
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(4, -.8f, 28);
            ground.transform.localScale = new Vector3(120, .2f, 120);
            ground.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(ground.GetComponent<Collider>());
        }

        static CombatScenePoint Point(Transform parent, string id, CombatPointKind kind, Vector3 position)
        {
            var go = new GameObject("Point_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var point = go.AddComponent<CombatScenePoint>();
            point.Id = id;
            point.RegionId = "trench-a";
            point.Kind = kind;
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = Vector3.up;
            collider.size = new Vector3(1.8f, 2, 1.8f);
            point.Volume = collider;
            return point;
        }

        static void SetAnchor(Transform anchor, Vector3 position, Quaternion rotation)
        {
            if (anchor == null) throw new InvalidOperationException("Missing scene anchor");
            anchor.SetPositionAndRotation(position, rotation);
        }

        static Texture2D CapturePlan()
        {
            Render(new Vector3(8, 85, 28), Quaternion.Euler(90, 0, 0), true, 40, MapPath, 1024, 1 << 31);
            AssetDatabase.ImportAsset(MapPath);
            var importer = AssetImporter.GetAtPath(MapPath) as TextureImporter;
            if (importer != null)
            {
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
        }

        static void CapturePreview()
        {
            var from = new Vector3(55, 48, -38);
            var target = new Vector3(8, 0, 26);
            Render(from, Quaternion.LookRotation(target - from), false, 55, PreviewPath, 1200, -1);
            AssetDatabase.ImportAsset(PreviewPath);
        }

        static void Render(Vector3 position, Quaternion rotation, bool orthographic, float viewSize, string path, int size, int mask)
        {
            var cameraObject = new GameObject("BunkersMapCapture");
            var camera = cameraObject.AddComponent<Camera>();
            var previous = RenderTexture.active;
            RenderTexture target = null;
            Texture2D pixels = null;
            try
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.orthographic = orthographic;
                if (orthographic) camera.orthographicSize = viewSize;
                else camera.fieldOfView = viewSize;
                camera.cullingMask = mask;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.13f, .16f, .15f);
                camera.farClipPlane = 200;
                target = new RenderTexture(size, size, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(size, size, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) Object.DestroyImmediate(pixels);
                if (target != null) Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
