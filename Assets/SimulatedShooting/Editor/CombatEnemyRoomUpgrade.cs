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
    static class CombatEnemyRoomUpgradeAutoRun
    {
        static readonly string Marker = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/apply-combat-enemy-room-upgrade"));

        static CombatEnemyRoomUpgradeAutoRun()
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
                Debug.LogError("Combat enemy/room upgrade paused because the open scene has unsaved changes.");
                return;
            }

            try
            {
                File.Delete(Marker);
                CombatEnemyRoomUpgrade.Apply();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    public static class CombatEnemyRoomUpgrade
    {
        const string ScenePath = "Assets/Scenes/CombatScene.unity";
        const string EnemyPrefabPath = "Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab";
        const string ModelPath = "Assets/SimulatedShooting/Art/Characters/CC0TacticalSWAT/Swat.fbx";
        const string MaterialFolder = "Assets/SimulatedShooting/Art/Characters/CC0TacticalSWAT/Materials";

        [MenuItem("Tools/Simulated Shooting/Scene 3/Apply Tactical Enemies And Room Passages")]
        public static void Apply()
        {
            ConfigureModelImport();
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            UpgradeEnemyPrefab();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bindings = UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            if (bindings == null || bindings.GeometryRoot == null)
                throw new InvalidOperationException("CombatScene bindings or geometry root is missing.");

            OpenAdjacentRoomPassages(bindings.GeometryRoot);
            ArrangeScooters(bindings.GeometryRoot);
            PolishSafetyRails(bindings.GeometryRoot);
            PolishAndOpenDoors(bindings);
            PositionAmbushPoints(bindings);
            EditorSceneManager.MarkSceneDirty(bindings.gameObject.scene);
            EditorSceneManager.SaveScene(bindings.gameObject.scene);
            AssetDatabase.SaveAssets();
            CombatSceneWalkabilityRepair.Apply();
            bindings = UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            EditorSceneManager.MarkSceneDirty(bindings.gameObject.scene);
            EditorSceneManager.SaveScene(bindings.gameObject.scene);
            AssetDatabase.SaveAssets();
            CombatSceneBuilder.Validate();
            Debug.Log("CombatScene tactical enemy and permanent room passages applied.");
        }

        static void ConfigureModelImport()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("SWAT FBX did not import.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        static void UpgradeEnemyPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                var view = root.GetComponent<CombatActorView>();
                if (view == null || view.VisualRoot == null) throw new InvalidOperationException("Enemy prefab visual root is missing.");

                foreach (var old in view.VisualRoot.GetComponentsInChildren<Transform>(true)
                             .Where(t => t.name == "CC0_SoldierModel" || t.name == "CC0_TacticalSWAT")
                             .ToArray())
                    UnityEngine.Object.DestroyImmediate(old.gameObject);

                foreach (var renderer in view.VisualRoot.GetComponentsInChildren<Renderer>(true))
                {
                    var keep = renderer.transform.IsChildOf(view.Muzzle)
                               || renderer.GetComponentsInParent<Transform>(true).Any(t => t.name == "Model_QBZ191_Enemy")
                               || renderer.GetComponentsInParent<Transform>(true).Any(t => t.name == "TrainingForceArmband");
                    renderer.enabled = keep;
                }

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, view.VisualRoot);
                model.name = "CC0_TacticalSWAT";
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ApplyUrpMaterials(model);

                var bounds = CombinedBounds(model);
                model.transform.localScale = Vector3.one * (1.78f / Mathf.Max(.01f, bounds.size.y));
                bounds = CombinedBounds(model);
                model.transform.position += Vector3.up * -bounds.min.y;
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ApplyUrpMaterials(GameObject model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(CreateUrpMaterial).ToArray();
        }

        static Material CreateUrpMaterial(Material source)
        {
            var sourceName = source != null ? source.name : "Tactical";
            var safeName = string.Concat(sourceName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var path = MaterialFolder + "/SWAT_" + safeName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            var color = source != null && source.HasProperty("_Color") ? source.color : new Color(.12f, .15f, .17f);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", .05f);
            material.SetFloat("_Smoothness", .22f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Bounds CombinedBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("SWAT model has no renderers.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static void OpenAdjacentRoomPassages(Transform geometry)
        {
            var existingRoots = geometry.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("PermanentRoomPassage_Floor_", StringComparison.Ordinal)).ToArray();
            var dividers = geometry.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name == "RoomDivider").ToArray();
            if (dividers.Length == 0 && existingRoots.Length == 2) return;
            if (dividers.Length != 2) throw new InvalidOperationException("Expected two room dividers before opening passages.");
            foreach (var oldRoot in existingRoots) UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            foreach (var divider in dividers)
            {
                var bounds = divider.bounds;
                var material = divider.GetComponent<Renderer>().sharedMaterial;
                var floor = Mathf.RoundToInt(bounds.min.y / 3.6f) + 1;
                var parent = divider.transform.parent;
                UnityEngine.Object.DestroyImmediate(divider.gameObject);

                var passage = new GameObject("PermanentRoomPassage_Floor_" + floor).transform;
                passage.SetParent(parent, false);
                const float doorwayWidth = 2.4f;
                const float doorwayHeight = 2.5f;
                var sideWidth = (bounds.size.x - doorwayWidth) * .5f;
                Box(passage, "InterRoomWall_Left", new Vector3(bounds.min.x + sideWidth * .5f, bounds.center.y, bounds.center.z),
                    new Vector3(sideWidth, bounds.size.y, bounds.size.z), material);
                Box(passage, "InterRoomWall_Right", new Vector3(bounds.max.x - sideWidth * .5f, bounds.center.y, bounds.center.z),
                    new Vector3(sideWidth, bounds.size.y, bounds.size.z), material);
                Box(passage, "InterRoomWall_Header", new Vector3(bounds.center.x, bounds.min.y + doorwayHeight + (bounds.size.y - doorwayHeight) * .5f, bounds.center.z),
                    new Vector3(doorwayWidth, bounds.size.y - doorwayHeight, bounds.size.z), material);
            }
        }

        static void ArrangeScooters(Transform geometry)
        {
            var scooters = geometry.GetComponentsInChildren<Transform>(true).Where(t => t.name == "ParkedScooter").ToArray();
            if (scooters.Length != 12) throw new InvalidOperationException("Expected twelve parked scooters.");
            for (var i = 0; i < scooters.Length; i++)
            {
                var streetRow = i < 8;
                var position = streetRow
                    ? new Vector3(-.35f + i * 1.22f, .02f, 60)
                    : new Vector3(43.2f, .02f, 67.4f + (i - 8) * 2.15f);
                var yaw = streetRow ? (i % 2 == 0 ? 167 : 173) : (i % 2 == 0 ? 8 : -8);
                scooters[i].SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
                var renderers = scooters[i].GetComponentsInChildren<Renderer>(true);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                scooters[i].position += new Vector3(position.x - bounds.center.x, position.y - bounds.min.y, position.z - bounds.center.z);
                EditorUtility.SetDirty(scooters[i]);
            }
        }

        static void PolishSafetyRails(Transform geometry)
        {
            var safety = geometry.Find("WalkwaySafety");
            if (safety == null) throw new InvalidOperationException("Walkway safety root is missing.");
            var oldVisuals = safety.Find("VisibleSafetyRails");
            if (oldVisuals != null) UnityEngine.Object.DestroyImmediate(oldVisuals.gameObject);
            var visuals = new GameObject("VisibleSafetyRails").transform;
            visuals.SetParent(safety, false);
            var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/TaiwanStreet/GalvanizedMetal.mat");

            foreach (var guard in safety.GetComponentsInChildren<BoxCollider>(true)
                         .Where(c => c.name == "StairGuard" || c.name == "PlatformGuard").ToArray())
            {
                var renderer = guard.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                var start = guard.transform.TransformPoint(new Vector3(0, 0, -.5f));
                var end = guard.transform.TransformPoint(new Vector3(0, 0, .5f));
                VisualBar(visuals, "TopRail", start + Vector3.up * .62f, end + Vector3.up * .62f, .065f, steel);
                var count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / 2.2f));
                for (var i = 0; i <= count; i++)
                {
                    var point = Vector3.Lerp(start, end, i / (float)count);
                    VisualBox(visuals, "RailPost", point, new Vector3(.07f, 1.25f, .07f), steel);
                }
            }
        }

        static void PolishAndOpenDoors(CombatSceneBindings bindings)
        {
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/WeatheredWood/WeatheredWood.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/TaiwanStreet/GalvanizedMetal.mat");
            foreach (var door in bindings.Doors)
            {
                door.InitiallyOpen = true;
                var leaf = door.Hinge.Find("DoorLeaf");
                if (leaf != null) leaf.GetComponent<Renderer>().sharedMaterial = wood;
                var old = door.transform.Find("DoorTrim");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var oldHandle = door.Hinge.Find("DoorHandle");
                if (oldHandle != null) UnityEngine.Object.DestroyImmediate(oldHandle.gameObject);
                var trim = new GameObject("DoorTrim").transform;
                trim.SetParent(door.transform, false);
                VisualBox(trim, "FrameLeft", door.transform.TransformPoint(new Vector3(0, 1.25f, -.05f)), new Vector3(.38f, 2.55f, .1f), steel);
                VisualBox(trim, "FrameRight", door.transform.TransformPoint(new Vector3(0, 1.25f, 2.05f)), new Vector3(.38f, 2.55f, .1f), steel);
                VisualBox(trim, "FrameHeader", door.transform.TransformPoint(new Vector3(0, 2.52f, 1)), new Vector3(.38f, .1f, 2.2f), steel);
                var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                handle.name = "DoorHandle";
                handle.transform.SetParent(door.Hinge, false);
                handle.transform.localPosition = new Vector3(-.11f, 1.08f, 1.58f);
                handle.transform.localScale = Vector3.one * .11f;
                handle.GetComponent<Renderer>().sharedMaterial = steel;
                UnityEngine.Object.DestroyImmediate(handle.GetComponent<Collider>());
                door.ResetView();
                EditorUtility.SetDirty(door);
            }
        }

        static void PositionAmbushPoints(CombatSceneBindings bindings)
        {
            SetPoint(bindings, "building-spawn-1", new Vector3(37, 0, 67.2f), 220, "room-11");
            SetPoint(bindings, "building-spawn-2", new Vector3(25, 0, 84.5f), 145, "room-12");
            SetPoint(bindings, "building-spawn-3", new Vector3(36.8f, 3.6f, 74.2f), 235, "room-21");
            SetPoint(bindings, "building-spawn-4", new Vector3(24.8f, 3.6f, 84.5f), 150, "room-22");
            SetPoint(bindings, "building-spawn-5", new Vector3(37, 7.2f, 68.5f), 225, "room-31");
            SetPoint(bindings, "building-spawn-6", new Vector3(34.5f, 7.2f, 84.5f), 205, "room-31");
            SetPoint(bindings, "street-spawn-1", new Vector3(42.6f, 0, 68.2f), 250, null);
            SetPoint(bindings, "street-spawn-2", new Vector3(12.7f, 0, 63.2f), 95, null);
        }

        static void SetPoint(CombatSceneBindings bindings, string id, Vector3 position, float yaw, string roomId)
        {
            var point = bindings.Points.Single(p => p.Id == id);
            point.RoomId = roomId;
            point.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            EditorUtility.SetDirty(point);
        }

        static void VisualBar(Transform parent, string name, Vector3 start, Vector3 end, float width, Material material)
        {
            var delta = end - start;
            var bar = VisualBox(parent, name, (start + end) * .5f, new Vector3(width, width, delta.magnitude), material);
            bar.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        static Transform VisualBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            box.name = name;
            box.SetParent(parent, false);
            box.position = position;
            box.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }
    }
}
