using System;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace SimulatedShooting.Editor
{
    public static class ProneRangeSceneMigration
    {
        const string ZeroingScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        const string MovingScenePath = "Assets/Scenes/MovingTargetRangeScene.unity";
        const string EnvironmentLayerName = "TrainingEnvironment";
        const string TargetLayerName = "TrainingTarget";
        const string PreviewPath = "docs/codex-reports/evidence/task003-task006-moving-target-range.png";
        const string BaselinePath = "docs/codex-reports/evidence/task003-task006-performance-baseline.md";

        [MenuItem("Tools/Simulated Shooting/Migrate P1 P2 To Fixed Prone Range Contract")]
        public static void ApplyTask003And006()
        {
            PatchScene(ZeroingScenePath, PatchZeroingRangeScene);
            PatchScene(MovingScenePath, PatchMovingTargetRangeScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProneRangeSceneMigration] Applied task003/task006 without rebuilding range geometry.");
        }

        [MenuItem("Tools/Simulated Shooting/Capture Task003 Task006 Evidence")]
        public static void CaptureTask003And006Evidence()
        {
            var scene = EditorSceneManager.OpenScene(MovingScenePath, OpenSceneMode.Single);
            var camera = FindByTestId(scene, "MovingTargetRange.Camera.NoVR")?.GetComponent<Camera>();
            var driver = FindByTestId(scene, "MovingTargetRange.Target.Binding")?
                .GetComponent<MovingTargetVisualDriver>();
            if (camera == null || driver == null)
                throw new InvalidOperationException("Moving-target camera or visual driver is missing.");

            const int width = 1600;
            const int height = 900;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                throw new InvalidOperationException("Unity project root could not be resolved.");

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                var baselines = new[]
                {
                    CaptureBaseline(camera, driver, "移动目标", new MovingTargetVisualState(
                        0.25f, MovingTargetTravelDirection.RightToLeft, false, true, 3f)),
                    CaptureBaseline(camera, driver, "实射", new MovingTargetVisualState(
                        0.50f, MovingTargetTravelDirection.RightToLeft, false, true, 5f)),
                    CaptureBaseline(camera, driver, "结算空壳", new MovingTargetVisualState(
                        1f, MovingTargetTravelDirection.Stationary, true, false, 0f))
                };

                driver.Apply(new MovingTargetVisualState(
                    0.50f, MovingTargetTravelDirection.RightToLeft, false, true, 4f));
                camera.Render();
                frame.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                frame.Apply();

                var previewOutput = Path.Combine(projectRoot, PreviewPath);
                Directory.CreateDirectory(Path.GetDirectoryName(previewOutput) ?? projectRoot);
                File.WriteAllBytes(previewOutput, frame.EncodeToPNG());

                var baselineOutput = Path.Combine(projectRoot, BaselinePath);
                Directory.CreateDirectory(Path.GetDirectoryName(baselineOutput) ?? projectRoot);
                File.WriteAllText(baselineOutput,
                    "# task003/task006 Editor 渲染基线\n\n" +
                    "采集环境：Unity 2022.3.62f3c1，Editor batchmode，1600x900，固定白天。" +
                    "该数据用于后续回归比较，不替代 VR 实机 GPU/舒适度验收。\n\n" +
                    "| 状态 | Camera.Render 样本(ms) | 活跃 Renderers | 活跃 Lights | ParticleSystems | 已分配内存(MB) | 保留内存(MB) |\n" +
                    "|---|---:|---:|---:|---:|---:|---:|\n" +
                    string.Join("\n", baselines) + "\n");

                Debug.Log($"[ProneRangeSceneMigration] Captured evidence: {previewOutput}; {baselineOutput}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        static string CaptureBaseline(
            Camera camera,
            MovingTargetVisualDriver driver,
            string stateName,
            MovingTargetVisualState state)
        {
            driver.Apply(state);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            camera.Render();
            stopwatch.Stop();
            var activeRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true)
                .Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
            var activeLights = UnityEngine.Object.FindObjectsOfType<Light>(true)
                .Count(light => light.enabled && light.gameObject.activeInHierarchy);
            var particleSystems = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true).Length;
            var allocatedMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            var reservedMb = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
            return $"| {stateName} | {stopwatch.Elapsed.TotalMilliseconds:F2} | {activeRenderers} | " +
                   $"{activeLights} | {particleSystems} | {allocatedMb:F1} | {reservedMb:F1} |";
        }

        public static void PatchOpenZeroingRangeScene()
        {
            PatchZeroingRangeScene(SceneManager.GetActiveScene());
        }

        public static void PatchOpenMovingTargetRangeScene()
        {
            PatchMovingTargetRangeScene(SceneManager.GetActiveScene());
        }

        static void PatchScene(string path, Action<UnityEngine.SceneManagement.Scene> patch)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            patch(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException($"Could not save migrated scene: {path}");
        }

        static void PatchZeroingRangeScene(UnityEngine.SceneManagement.Scene scene)
        {
            var root = FindByTestId(scene, "ZeroingRange.Root") ?? scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "ZeroingRange");
            if (root == null)
                throw new InvalidOperationException("ZeroingRange root is missing.");

            var anchors = FindDescendant(root.transform, "TrainingAnchors") ??
                          CreateAnchor("TrainingAnchors", root.transform, Vector3.zero);
            var target = FindByTestId(scene, "ZeroingRange.Target.Primary")?.transform;
            var xrOrigin = FindByTestId(scene, "ZeroingRange.Origin.VR");
            var noVrCamera = FindByTestId(scene, "ZeroingRange.Camera.NoVR")?.GetComponent<Camera>();
            var weaponSpawn = FindByTestId(scene, "ZeroingRange.WeaponSpawn")?.transform;

            var station = EnsureAnchor(anchors, "FiringStation_Prone", Vector3.zero,
                "ZeroingRange.FiringStation.Root");
            var playerRoot = EnsureAnchor(station, "PlayerRootAnchor", Vector3.zero,
                "ZeroingRange.FiringStation.PlayerRoot");
            var proneHead = EnsureAnchor(station, "ProneHeadReference", new Vector3(0f, 0.72f, 0f),
                "ZeroingRange.FiringStation.ProneHeadReference");
            var aim = EnsureAnchor(station, "AimForwardAnchor", new Vector3(0f, 0.72f, 1f),
                "ZeroingRange.FiringStation.AimForward");
            var largeUi = EnsureAnchor(station, "LargeUiAnchor", new Vector3(-0.72f, 0.88f, 1.55f),
                "ZeroingRange.FiringStation.LargeUiAnchor");
            var minimalHud = EnsureAnchor(station, "MinimalHudAnchor", new Vector3(0.62f, 0.82f, 1.25f),
                "ZeroingRange.FiringStation.MinimalHudAnchor");
            var weaponRackPosition = weaponSpawn == null
                ? new Vector3(0.42f, 0.35f, 0.75f)
                : station.InverseTransformPoint(weaponSpawn.position);
            var weaponRack = EnsureAnchor(station, "WeaponRackAnchor", weaponRackPosition,
                "ZeroingRange.FiringStation.WeaponRackAnchor");
            var targetRootPosition = target == null
                ? new Vector3(0f, 0.72f, 100f)
                : station.InverseTransformPoint(target.position);
            var targetRoot = EnsureAnchor(station, "TargetRootAnchor", targetRootPosition,
                "ZeroingRange.FiringStation.TargetRoot");

            ConfigureStation(station, playerRoot, proneHead, aim, largeUi, minimalHud, weaponRack, targetRoot,
                xrOrigin);
            if (noVrCamera != null)
            {
                noVrCamera.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                noVrCamera.transform.localRotation = Quaternion.identity;
                noVrCamera.transform.localScale = Vector3.one;
            }

            foreach (var footsteps in root.GetComponentsInChildren<PlayerFootstepAudio>(true))
                UnityEngine.Object.DestroyImmediate(footsteps);
            var footstepObject = FindByTestId(scene, "ZeroingRange.Player.Footsteps");
            if (footstepObject != null)
            {
                var obsoleteId = footstepObject.GetComponent<SceneTestId>();
                if (obsoleteId != null)
                    UnityEngine.Object.DestroyImmediate(obsoleteId);
            }
        }

        static void PatchMovingTargetRangeScene(UnityEngine.SceneManagement.Scene scene)
        {
            var root = FindByTestId(scene, "MovingTargetRange.Root") ?? scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "MovingTargetRange");
            if (root == null)
                throw new InvalidOperationException("MovingTargetRange root is missing.");

            var anchors = FindDescendant(root.transform, "TrainingAnchors") ??
                          CreateAnchor("TrainingAnchors", root.transform, Vector3.zero);
            var route = FindByTestId(scene, "MovingTargetRange.Route.Root")?.transform;
            var movingTarget = FindByTestId(scene, "MovingTargetRange.Target")?.transform;
            var xrOrigin = FindByTestId(scene, "MovingTargetRange.Origin.VR");
            var noVrCamera = FindByTestId(scene, "MovingTargetRange.Camera.NoVR")?.GetComponent<Camera>();

            var station = EnsureAnchor(anchors, "FiringStation_Prone", Vector3.zero,
                "MovingTargetRange.FiringStation.Root");
            var playerRoot = EnsureAnchor(station, "PlayerRootAnchor", Vector3.zero,
                "MovingTargetRange.FiringStation.PlayerRoot");
            var proneHead = EnsureAnchor(station, "ProneHeadReference", new Vector3(0f, 0.72f, 0f),
                "MovingTargetRange.FiringStation.ProneHeadReference");
            var aim = EnsureAnchor(station, "AimForwardAnchor", new Vector3(0f, 0.72f, 1f),
                "MovingTargetRange.FiringStation.AimForward");
            var largeUi = EnsureAnchor(station, "LargeUiAnchor", new Vector3(-0.72f, 0.88f, 1.55f),
                "MovingTargetRange.FiringStation.LargeUiAnchor");
            var minimalHud = EnsureAnchor(station, "MinimalHudAnchor", new Vector3(0.62f, 0.82f, 1.25f),
                "MovingTargetRange.FiringStation.MinimalHudAnchor");
            var weaponRack = EnsureAnchor(station, "WeaponRackAnchor", new Vector3(0.42f, 0.35f, 0.75f),
                "MovingTargetRange.FiringStation.WeaponRackAnchor");
            var targetRootPosition = route == null
                ? new Vector3(0f, 0f, 100f)
                : station.InverseTransformPoint(route.position);
            var targetRoot = EnsureAnchor(station, "TargetRootAnchor", targetRootPosition,
                "MovingTargetRange.FiringStation.TargetRoot");

            ConfigureStation(station, playerRoot, proneHead, aim, largeUi, minimalHud, weaponRack, targetRoot,
                xrOrigin);
            CopyZeroingRangeStartArea(scene, root.transform, station, noVrCamera);
            ConfigureMovingTargetRoute(scene, route, movingTarget);
            RemoveOutOfScopeNightObjects(scene);
        }

        static void ConfigureStation(
            Transform station,
            Transform playerRoot,
            Transform proneHead,
            Transform aim,
            Transform largeUi,
            Transform minimalHud,
            Transform weaponRack,
            Transform targetRoot,
            GameObject xrOrigin)
        {
            var bindings = station.GetComponent<TrainingRangeSceneBindings>() ??
                           station.gameObject.AddComponent<TrainingRangeSceneBindings>();
            bindings.Configure(playerRoot, proneHead, aim, largeUi, minimalHud, weaponRack, targetRoot);

            if (xrOrigin != null)
            {
                xrOrigin.transform.SetPositionAndRotation(playerRoot.position, playerRoot.rotation);
                var locomotion = FindDescendant(xrOrigin.transform, "Locomotion")?.gameObject;
                var guard = station.GetComponent<FixedProneLocomotionGuard>() ??
                            station.gameObject.AddComponent<FixedProneLocomotionGuard>();
                guard.Configure(playerRoot, xrOrigin, locomotion);
            }

        }

        static void CopyZeroingRangeStartArea(
            UnityEngine.SceneManagement.Scene targetScene,
            Transform targetRoot,
            Transform station,
            Camera targetCamera)
        {
            var approximateVisual = FindDescendant(station, "FiringStationVisual");
            if (approximateVisual != null)
                UnityEngine.Object.DestroyImmediate(approximateVisual.gameObject);

            var previousCopy = FindDescendant(targetRoot, "ZeroingStartAreaCopy");
            if (previousCopy != null)
                UnityEngine.Object.DestroyImmediate(previousCopy.gameObject);

            var existingFiringPad = FindDescendant(targetScene, "FiringPad");
            if (existingFiringPad != null)
                UnityEngine.Object.DestroyImmediate(existingFiringPad.gameObject);

            var sourceScene = EditorSceneManager.OpenScene(ZeroingScenePath, OpenSceneMode.Additive);
            try
            {
                var sourceRoot = FindByTestId(sourceScene, "ZeroingRange.Root")?.transform;
                var sourceEnvironment = sourceRoot == null ? null : FindDescendant(sourceRoot, "Environment");
                var sourceVisual = sourceRoot == null ? null : FindDescendant(sourceRoot, "VisualPolish");
                var sourceAnchors = sourceRoot == null ? null : FindDescendant(sourceRoot, "TrainingAnchors");
                if (sourceEnvironment == null || sourceVisual == null || sourceAnchors == null)
                    throw new InvalidOperationException("ZeroingRange start-area source objects are missing.");

                var copyRoot = new GameObject("ZeroingStartAreaCopy");
                SceneManager.MoveGameObjectToScene(copyRoot, targetScene);
                copyRoot.transform.SetParent(targetRoot, false);
                EnsureId(copyRoot, "MovingTargetRange.Visual.ZeroingStartAreaCopy");

                foreach (var name in new[] { "FiringPad", "FiringLine", "ShootingBench" })
                    CloneStartAreaObject(FindDirectChild(sourceEnvironment, name), copyRoot.transform, targetScene);

                foreach (Transform child in sourceEnvironment)
                {
                    if (child.name.StartsWith("Sandbag_", StringComparison.Ordinal) ||
                        child.name.StartsWith("DistancePost_0m_", StringComparison.Ordinal) ||
                        child.name.StartsWith("DistanceBoard_0m_", StringComparison.Ordinal) ||
                        child.name.StartsWith("DistanceLabel_0m_", StringComparison.Ordinal))
                    {
                        CloneStartAreaObject(child, copyRoot.transform, targetScene);
                    }
                }

                foreach (var name in new[] { "RangeGate", "SafetyBoundary", "WeaponCrate_Left" })
                    CloneStartAreaObject(FindDirectChild(sourceVisual, name), copyRoot.transform, targetScene);
                CloneStartAreaObject(FindDirectChild(sourceAnchors, "WeaponReference_Blockout"),
                    copyRoot.transform, targetScene);

                RemapStartAreaTestIds(copyRoot);
                var environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
                if (environmentLayer >= 0)
                    SetLayerRecursively(copyRoot, environmentLayer);

                var sourceCamera = FindByTestId(sourceScene, "ZeroingRange.Camera.NoVR")?.GetComponent<Camera>();
                if (targetCamera != null && sourceCamera != null)
                {
                    targetCamera.transform.localPosition = sourceCamera.transform.localPosition;
                    targetCamera.transform.localRotation = sourceCamera.transform.localRotation;
                    targetCamera.transform.localScale = sourceCamera.transform.localScale;
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(sourceScene, true);
                SceneManager.SetActiveScene(targetScene);
            }
        }

        static Transform FindDirectChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
                throw new InvalidOperationException($"ZeroingRange start-area object is missing: {name}");
            return child;
        }

        static void CloneStartAreaObject(
            Transform source,
            Transform targetParent,
            UnityEngine.SceneManagement.Scene targetScene)
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = source.name;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            clone.transform.SetParent(targetParent, true);
        }

        static void RemapStartAreaTestIds(GameObject copyRoot)
        {
            foreach (var testId in copyRoot.GetComponentsInChildren<SceneTestId>(true))
            {
                switch (testId.Id)
                {
                    case "ZeroingRange.Environment.DistanceLabel.0m.Left":
                        testId.Id = "MovingTargetRange.Environment.DistanceLabel.0m.Left";
                        break;
                    case "ZeroingRange.Environment.DistanceLabel.0m.Right":
                        testId.Id = "MovingTargetRange.Environment.DistanceLabel.0m.Right";
                        break;
                    case "ZeroingRange.Visual.RangeGate":
                        testId.Id = "MovingTargetRange.Visual.RangeGate";
                        break;
                    case "ZeroingRange.Visual.SafetyBoundary":
                        testId.Id = "MovingTargetRange.Visual.SafetyBoundary";
                        break;
                    case "ZeroingRange.Visual.WeaponCrate.Left":
                        testId.Id = "MovingTargetRange.Visual.WeaponCrate.Left";
                        break;
                    case "ZeroingRange.Weapon.Reference":
                        testId.Id = "MovingTargetRange.Weapon.Reference";
                        break;
                }
            }
        }

        static void ConfigureMovingTargetRoute(
            UnityEngine.SceneManagement.Scene scene,
            Transform route,
            Transform movingTarget)
        {
            var bindingObject = FindByTestId(scene, "MovingTargetRange.Target.Binding");
            var binding = bindingObject?.GetComponent<MovingTargetRouteBinding>();
            if (route == null || movingTarget == null || binding == null || binding.HitSurface == null)
                throw new InvalidOperationException("Moving target route bindings are incomplete.");

            EnsureMovingTargetSilhouette(movingTarget);

            binding.Configure(binding.RightEndpoint, binding.LeftEndpoint, movingTarget, binding.HitSurface,
                binding.TargetCenter, binding.ImpactFeedbackRoot, movingTarget,
                movingTarget.GetComponentsInChildren<Renderer>(true));

            var dark = FindDescendant(scene, "EndpointPost_Right")?.GetComponent<Renderer>()?.sharedMaterial;
            var yellow = FindDescendant(scene, "EndpointFlag_Right")?.GetComponent<Renderer>()?.sharedMaterial;
            var holdIndicator = EnsurePrimitive(bindingObject.transform, "EndpointHoldIndicator", PrimitiveType.Sphere,
                new Vector3(0f, 0.38f, 0f), Vector3.one * 0.18f, yellow);
            var canShootIndicator = EnsurePrimitive(bindingObject.transform, "CanShootIndicator", PrimitiveType.Sphere,
                new Vector3(0f, 0.68f, 0f), Vector3.one * 0.14f, dark);
            holdIndicator.SetActive(true);
            canShootIndicator.SetActive(false);

            var driver = bindingObject.GetComponent<MovingTargetVisualDriver>() ??
                         bindingObject.AddComponent<MovingTargetVisualDriver>();
            driver.Configure(binding, holdIndicator, canShootIndicator);
            var timeline = bindingObject.GetComponent<MovingTargetFakeTimeline>() ??
                           bindingObject.AddComponent<MovingTargetFakeTimeline>();
            timeline.Configure(driver);

            EnsureText(binding.LeftEndpoint, "RouteLabel_Left", "LEFT  -20 m  <", new Vector3(0f, 2.65f, 0.48f));
            EnsureText(binding.RightEndpoint, "RouteLabel_Right", ">  +20 m  RIGHT", new Vector3(0f, 2.65f, 0.48f));

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(binding.ImpactFeedbackRoot.gameObject);
            var feedback = binding.ImpactFeedbackRoot.GetComponent<MovingTargetImpactFeedback>() ??
                           binding.ImpactFeedbackRoot.gameObject.AddComponent<MovingTargetImpactFeedback>();
            var markerMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/SimulatedShooting/Art/Materials/TargetImpactMarker.mat");
            var marker = EnsurePrimitive(binding.ImpactFeedbackRoot, "ConfirmedImpactMarker", PrimitiveType.Sphere,
                Vector3.zero, Vector3.one * 0.06f, markerMaterial ?? yellow).transform;
            feedback.Configure(marker);

            var environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            var targetLayer = LayerMask.NameToLayer(TargetLayerName);
            if (environmentLayer < 0 || targetLayer < 0)
                throw new InvalidOperationException("TrainingEnvironment/TrainingTarget layers are not configured.");

            var environment = FindByTestId(scene, "MovingTargetRange.Environment.Root");
            if (environment != null)
                SetLayerRecursively(environment, environmentLayer);
            SetLayerRecursively(route.gameObject, environmentLayer);
            SetLayerRecursively(movingTarget.gameObject, targetLayer);

            var adapter = binding.HitSurface.GetComponent<MovingTargetHitAdapter>() ??
                          binding.HitSurface.gameObject.AddComponent<MovingTargetHitAdapter>();
            adapter.Configure(binding.HitSurface, 1 << targetLayer, 1 << environmentLayer, feedback);
        }

        static void EnsureMovingTargetSilhouette(Transform movingTarget)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/SimulatedShooting/Art/Materials/MovingTargetSilhouette.mat");
            EnsurePrimitive(movingTarget, "Runner_Torso", PrimitiveType.Capsule,
                new Vector3(0f, 1.25f, 0f), new Vector3(0.24f, 0.42f, 0.11f), material,
                new Vector3(0f, 0f, -12f));
            EnsurePrimitive(movingTarget, "Runner_Head", PrimitiveType.Sphere,
                new Vector3(-0.10f, 1.82f, 0f), new Vector3(0.20f, 0.20f, 0.12f), material);
            EnsurePrimitive(movingTarget, "Runner_ArmForward", PrimitiveType.Capsule,
                new Vector3(-0.30f, 1.32f, 0f), new Vector3(0.10f, 0.34f, 0.08f), material,
                new Vector3(0f, 0f, 62f));
            EnsurePrimitive(movingTarget, "Runner_ArmRear", PrimitiveType.Capsule,
                new Vector3(0.28f, 1.25f, 0f), new Vector3(0.10f, 0.32f, 0.08f), material,
                new Vector3(0f, 0f, -58f));
            EnsurePrimitive(movingTarget, "Runner_LegForward", PrimitiveType.Capsule,
                new Vector3(-0.22f, 0.62f, 0f), new Vector3(0.13f, 0.48f, 0.10f), material,
                new Vector3(0f, 0f, -34f));
            EnsurePrimitive(movingTarget, "Runner_LegRear", PrimitiveType.Capsule,
                new Vector3(0.25f, 0.58f, 0f), new Vector3(0.13f, 0.48f, 0.10f), material,
                new Vector3(0f, 0f, 38f));
        }

        static void RemoveOutOfScopeNightObjects(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var id in new[] { "MovingTargetRange.Lighting.Night", "MovingTargetRange.Optic.LowLight" })
            {
                var gameObject = FindByTestId(scene, id);
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        static Transform EnsureAnchor(Transform parent, string name, Vector3 localPosition, string id)
        {
            var anchor = parent.Find(name) ?? CreateAnchor(name, parent, localPosition);
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            EnsureId(anchor.gameObject, id);
            return anchor;
        }

        static Transform CreateAnchor(string name, Transform parent, Vector3 localPosition)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            return anchor;
        }

        static GameObject EnsurePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEulerAngles = default)
        {
            var existing = parent.Find(name)?.gameObject;
            var gameObject = existing ?? GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.Euler(localEulerAngles);
            gameObject.transform.localScale = localScale;
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
            return gameObject;
        }

        static void EnsureText(Transform parent, string name, string value, Vector3 localPosition)
        {
            var textTransform = parent.Find(name) ?? CreateAnchor(name, parent, localPosition);
            textTransform.localPosition = localPosition;
            textTransform.localRotation = Quaternion.identity;
            var text = textTransform.GetComponent<TextMesh>();
            if (text == null)
                text = textTransform.gameObject.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = 0.035f;
            text.color = Color.white;
        }

        static void EnsureId(GameObject gameObject, string id)
        {
            var testId = gameObject.GetComponent<SceneTestId>() ?? gameObject.AddComponent<SceneTestId>();
            testId.Id = id;
        }

        static GameObject FindByTestId(UnityEngine.SceneManagement.Scene scene, string id)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }

        static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == name);
        }

        static Transform FindDescendant(UnityEngine.SceneManagement.Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(child => child.name == name);
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.layer = layer;
        }
    }
}
