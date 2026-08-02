using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class ZeroingRangeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        private const string MaterialFolder = "Assets/SimulatedShooting/Art/Materials";
        private const string TreeTexturePath =
            "Assets/SimulatedShooting/Art/Vegetation/RangeTree_Broadleaf.png";
        private const string LeftHandModelPath =
            "Assets/SimulatedShooting/Art/Hands/UnityXRHands/LeftHand.fbx";
        private const string RightHandModelPath =
            "Assets/SimulatedShooting/Art/Hands/UnityXRHands/RightHand.fbx";
        private const string ProjectileModelPath =
            "Assets/SimulatedShooting/Art/Ballistics/PichuliruFlatAmmunition/Projectile_556x45.fbx";
        private const string RifleShotPath =
            "Assets/SimulatedShooting/Audio/Weapons/rifle-sks-single-shot.wav";
        private const string PickupPath =
            "Assets/SimulatedShooting/Audio/Weapons/weapon-pickup-mechanical.wav";
        private const string FlybyPath =
            "Assets/SimulatedShooting/Audio/Weapons/bullet-flyby-cc0-hq.mp3";
        private const string ImpactThudPath =
            "Assets/SimulatedShooting/Audio/Impacts/target-impact-metal-thud.wav";
        private const string ImpactClinkPath =
            "Assets/SimulatedShooting/Audio/Impacts/target-impact-metal-clink.wav";
        private const string FootstepFolder =
            "Assets/SimulatedShooting/Audio/Footsteps";
        private static readonly Vector3 ComfortableWeaponSpawnPosition = new Vector3(0.40f, 1.10f, 0.55f);

        [MenuItem("Tools/Simulated Shooting/Build Zeroing Range Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/SimulatedShooting/Art");
            EnsureFolder(MaterialFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("ZeroingRange");
            AddTestId(root, "ZeroingRange.Root");
            root.AddComponent<ZeroingRangeSessionBootstrap>();
            CreateScene(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Simulated Shooting/Refresh Range Skybox And Trees")]
        public static void RefreshEnvironmentVisuals()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var environment = GameObject.Find("ZeroingRange/Environment")?.transform;
            if (environment == null)
                throw new System.InvalidOperationException("ZeroingRange/Environment was not found.");

            foreach (var sideName in new[] { "Left", "Right" })
            {
                var existing = environment.Find($"TreeLine_{sideName}");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
                CreateBillboardTreeLine(environment, sideName == "Left" ? -1f : 1f, sideName);
            }

            MarkRenderersStatic(environment);
            PostWarSkyboxInstaller.ApplyEnvironment(GetSkyboxMaterial());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ZeroingRangeSceneBuilder] Refreshed the 4K skybox and side tree lines.");
        }

        [MenuItem("Tools/Simulated Shooting/Remove Legacy Tree Crowns")]
        public static void RemoveLegacyTreeCrowns()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var environment = GameObject.Find("ZeroingRange/Environment")?.transform;
            if (environment == null)
                throw new System.InvalidOperationException("ZeroingRange/Environment was not found.");

            var crowns = environment.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name.StartsWith("Crown_"))
                .ToArray();
            foreach (var crown in crowns)
                Object.DestroyImmediate(crown.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ZeroingRangeSceneBuilder] Removed {crowns.Length} legacy tree crowns.");
        }

        [MenuItem("Tools/Simulated Shooting/Run Zeroing Range Scene")]
        public static void OpenAndPlay()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            Debug.Log("Running ZeroingRangeScene with the no-VR test camera.");
        }

        [MenuItem("Tools/Simulated Shooting/Upgrade Zeroing Range Hands and Feedback")]
        public static void UpgradeHandsAndFeedback()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var playerRoot = FindByTestId("ZeroingRange.Weapon.PlayerRoot");
            var weapon = FindByTestId("ZeroingRange.Weapon.TrainingRifle");
            var xrOriginObject = FindByTestId("ZeroingRange.Origin.VR");
            if (playerRoot == null || weapon == null || xrOriginObject == null)
            {
                Debug.LogError("[ZeroingRangeSceneBuilder] Cannot upgrade feedback: required scene anchors are missing.");
                return;
            }

            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var controller = playerRoot.GetComponent<FirstPersonTrainingWeaponController>();
            var tracerRoot = FindByTestId("ZeroingRange.Weapon.TracerRoot")?.transform;
            if (binding == null || controller == null || tracerRoot == null)
            {
                Debug.LogError("[ZeroingRangeSceneBuilder] Cannot upgrade feedback: weapon binding is incomplete.");
                return;
            }

            var gloveMaterial = GetMaterial("RangeSandbag", new Color(0.24f, 0.25f, 0.17f));
            P1XrFloorOriginUpgrader.ConfigureFloorOrigin(xrOriginObject);
            ConfigureComfortableWeaponSpawn(
                FindByTestId("ZeroingRange.WeaponSpawn")?.transform,
                weapon.transform);
            ConfigureHandVisuals(xrOriginObject.transform, binding, gloveMaterial);
            ConfigureWeaponFeedback(playerRoot.transform, binding, tracerRoot, xrOriginObject.transform, controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ZeroingRangeSceneBuilder] Upgraded virtual hands, weapon audio, footsteps, and ballistic feedback.");
        }

        private static void CreateScene(Transform root)
        {
            var ground = GetMaterial("RangeGround", new Color(0.16f, 0.20f, 0.10f));
            var concrete = GetMaterial("RangeConcrete", new Color(0.34f, 0.32f, 0.27f));
            var earth = GetMaterial("RangeEarth", new Color(0.22f, 0.17f, 0.10f));
            var foliage = GetMaterial("RangeFoliage", new Color(0.10f, 0.16f, 0.07f));
            var line = GetMaterial("RangeLine", new Color(0.78f, 0.76f, 0.66f));
            var darkMetal = GetMaterial("RangeDarkMetal", new Color(0.035f, 0.04f, 0.035f));
            var sandbag = GetMaterial("RangeSandbag", new Color(0.24f, 0.25f, 0.17f));
            var target = GetMaterial("TargetDark", new Color(0.035f, 0.04f, 0.035f));
            var targetBoard = GetMaterial("TargetBoard", new Color(0.76f, 0.74f, 0.65f));
            var ring = GetMaterial("TargetTenRing", new Color(0.88f, 0.86f, 0.75f));
            var impactMarker = GetMaterial("TargetImpactMarker", new Color(0.55f, 0.03f, 0.02f));
            var marker = GetMaterial("RangeMarker", new Color(0.70f, 0.58f, 0.18f));
            var crate = GetMaterial("RangeWeaponCrate", new Color(0.17f, 0.20f, 0.10f));

            TrainingRiflePrefabBuilder.EnsurePrefab();
            CreateEnvironment(root, ground, concrete, earth, foliage, line, marker, darkMetal, sandbag);
            CreateVisualPolish(root, darkMetal, marker, line, crate);
            CreateAnchors(root, darkMetal);
            CreateTarget(root, target, targetBoard, ring, impactMarker, concrete);
            CreateLighting(root);
        }

        private static void CreateEnvironment(Transform root, Material ground, Material concrete, Material earth,
            Material foliage, Material line, Material marker, Material darkMetal, Material sandbag)
        {
            var environment = new GameObject("Environment").transform;
            environment.SetParent(root);
            CreateCube("Ground", environment, new Vector3(0f, -0.2f, 55f), new Vector3(42f, 0.4f, 130f), ground);
            var lane = CreateCube("RangeLane_100m", environment, new Vector3(0f, -0.02f, 52f),
                new Vector3(14f, 0.12f, 106f), concrete);
            AddTestId(lane, "ZeroingRange.Environment.Lane");
            CreateCube("FiringPad", environment, new Vector3(0f, 0.01f, -0.6f), new Vector3(15f, 0.16f, 4f), concrete);
            CreateCube("FiringLine", environment, new Vector3(0f, 0.11f, 0f), new Vector3(15f, 0.03f, 0.09f), marker, false);

            CreateCube("LaneEdge_Left", environment, new Vector3(-6.9f, 0.09f, 52f), new Vector3(0.10f, 0.03f, 104f), line, false);
            CreateCube("LaneEdge_Right", environment, new Vector3(6.9f, 0.09f, 52f), new Vector3(0.10f, 0.03f, 104f), line, false);
            CreateCube("LaneGuide_Left", environment, new Vector3(-2.4f, 0.09f, 52f), new Vector3(0.06f, 0.03f, 104f), line, false);
            CreateCube("LaneGuide_Right", environment, new Vector3(2.4f, 0.09f, 52f), new Vector3(0.06f, 0.03f, 104f), line, false);
            for (var distance = 10; distance < 100; distance += 10)
                CreateCube($"LaneMark_{distance}m", environment, new Vector3(0f, 0.10f, distance),
                    new Vector3(13.5f, 0.025f, 0.06f), line, false);

            var leftBerm = CreateCube("Berm_Left", environment, new Vector3(-10.5f, 1.4f, 55f),
                new Vector3(8f, 3f, 118f), earth, true, Quaternion.Euler(0f, 0f, -18f));
            AddTestId(leftBerm, "ZeroingRange.Environment.Berm.Left");
            var rightBerm = CreateCube("Berm_Right", environment, new Vector3(10.5f, 1.4f, 55f),
                new Vector3(8f, 3f, 118f), earth, true, Quaternion.Euler(0f, 0f, 18f));
            AddTestId(rightBerm, "ZeroingRange.Environment.Berm.Right");
            CreateCube("BermTop_Left", environment, new Vector3(-14f, 3.35f, 55f), new Vector3(5f, 1f, 120f), foliage);
            CreateCube("BermTop_Right", environment, new Vector3(14f, 3.35f, 55f), new Vector3(5f, 1f, 120f), foliage);
            CreateCube("TargetBackstop", environment, new Vector3(0f, 2.2f, 103f), new Vector3(16f, 4.5f, 1f), earth);

            CreateBackground(environment, earth, foliage);
            CreateForeground(environment, darkMetal, sandbag);

            foreach (var distance in new[] { 25f, 50f, 75f, 100f })
            {
                CreateDistanceMarker(environment, -6.2f, distance, marker, darkMetal);
                CreateDistanceMarker(environment, 6.2f, distance, marker, darkMetal);
            }

            MarkRenderersStatic(environment);
        }

        private static void CreateVisualPolish(Transform root, Material darkMetal, Material marker, Material line,
            Material crateMaterial)
        {
            var visual = new GameObject("VisualPolish").transform;
            visual.SetParent(root);
            AddTestId(visual.gameObject, "ZeroingRange.Visual.Root");

            var rangeGate = CreateAnchor("RangeGate", visual, new Vector3(0f, 0f, 4.5f));
            AddTestId(rangeGate.gameObject, "ZeroingRange.Visual.RangeGate");
            CreateCube("GatePost_Left", rangeGate, new Vector3(-5.6f, 2f, 0f),
                new Vector3(0.18f, 4f, 0.18f), darkMetal, false);
            CreateCube("GatePost_Right", rangeGate, new Vector3(5.6f, 2f, 0f),
                new Vector3(0.18f, 4f, 0.18f), darkMetal, false);
            CreateCube("GateHeader", rangeGate, new Vector3(0f, 4f, 0f),
                new Vector3(11.4f, 0.22f, 0.22f), darkMetal, false);
            CreateCube("GateIdentificationStrip", rangeGate, new Vector3(0f, 3.82f, -0.02f),
                new Vector3(4.2f, 0.08f, 0.04f), marker, false);

            var targetFrame = CreateAnchor("TargetIdentificationFrame", visual, Vector3.zero);
            AddTestId(targetFrame.gameObject, "ZeroingRange.Visual.TargetFrame");
            CreateCube("TargetFrame_Left", targetFrame, new Vector3(-0.72f, 1.5f, 99.9f),
                new Vector3(0.08f, 1.7f, 0.08f), darkMetal, false);
            CreateCube("TargetFrame_Right", targetFrame, new Vector3(0.72f, 1.5f, 99.9f),
                new Vector3(0.08f, 1.7f, 0.08f), darkMetal, false);
            CreateCube("TargetFrame_Top", targetFrame, new Vector3(0f, 2.35f, 99.9f),
                new Vector3(1.52f, 0.08f, 0.08f), marker, false);
            CreateCube("TargetFrame_Bottom", targetFrame, new Vector3(0f, 0.65f, 99.9f),
                new Vector3(1.52f, 0.08f, 0.08f), darkMetal, false);

            var safetyBoundary = CreateAnchor("SafetyBoundary", visual, Vector3.zero);
            AddTestId(safetyBoundary.gameObject, "ZeroingRange.Visual.SafetyBoundary");
            CreateCube("SafetyBoundary_Left", safetyBoundary, new Vector3(-5.2f, 0.13f, 0f),
                new Vector3(3.8f, 0.025f, 0.12f), marker, false);
            CreateCube("SafetyBoundary_Right", safetyBoundary, new Vector3(5.2f, 0.13f, 0f),
                new Vector3(3.8f, 0.025f, 0.12f), marker, false);
            CreateCube("ShootingDirectionGuide", safetyBoundary, new Vector3(0f, 0.125f, 8f),
                new Vector3(0.08f, 0.02f, 10f), line, false);

            CreateWeaponCrate(visual, crateMaterial, darkMetal, marker);
            MarkRenderersStatic(visual);
        }

        private static void CreateWeaponCrate(Transform parent, Material crateMaterial, Material metal, Material marker)
        {
            var crate = CreateAnchor("WeaponCrate_Left", parent, new Vector3(-1f, 0.38f, 2.4f));
            AddTestId(crate.gameObject, "ZeroingRange.Visual.WeaponCrate.Left");

            CreateCube("CrateBody", crate, Vector3.zero, new Vector3(1.1f, 0.68f, 0.72f), crateMaterial);
            CreateCube("CrateLid", crate, new Vector3(0f, 0.38f, 0f),
                new Vector3(1.16f, 0.09f, 0.78f), crateMaterial, false);
            CreateCube("CrateBand_Left", crate, new Vector3(-0.42f, 0.02f, -0.37f),
                new Vector3(0.08f, 0.72f, 0.035f), metal, false);
            CreateCube("CrateBand_Right", crate, new Vector3(0.42f, 0.02f, -0.37f),
                new Vector3(0.08f, 0.72f, 0.035f), metal, false);
            CreateCube("CrateHandle", crate, new Vector3(0f, 0.02f, -0.405f),
                new Vector3(0.32f, 0.08f, 0.035f), metal, false);
            CreateCube("CrateIdentificationStrip", crate, new Vector3(0f, 0.2f, -0.407f),
                new Vector3(0.48f, 0.08f, 0.02f), marker, false);
        }

        private static void CreateAnchors(Transform root, Material darkMetal)
        {
            var anchors = new GameObject("TrainingAnchors").transform;
            anchors.SetParent(root);
            var shootingPosition = CreateAnchor("ShootingPosition", anchors, new Vector3(0f, 1.5f, 0f));
            AddTestId(shootingPosition.gameObject, "ZeroingRange.ShootingPosition");
            var weaponSpawn = CreateAnchor("WeaponSpawnPoint", anchors, ComfortableWeaponSpawnPosition);
            weaponSpawn.localRotation = Quaternion.Euler(0f, -90f, 0f);
            AddTestId(weaponSpawn.gameObject, "ZeroingRange.WeaponSpawn");
            AddTestId(CreateAnchor("HudAnchor", anchors, new Vector3(0f, 1.55f, 1.5f)).gameObject,
                "ZeroingRange.HudAnchor");

            var noVrCamera = new GameObject("Camera_NoVR", typeof(Camera), typeof(AudioListener));
            noVrCamera.transform.SetParent(anchors);
            noVrCamera.transform.SetPositionAndRotation(shootingPosition.position, Quaternion.identity);
            var camera = noVrCamera.GetComponent<Camera>();
            noVrCamera.tag = "MainCamera";
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 250f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.useOcclusionCulling = true;
            AddTestId(noVrCamera, "ZeroingRange.Camera.NoVR");

            var weaponReference = new GameObject("WeaponReference_Blockout").transform;
            weaponReference.SetParent(anchors, false);
            AddTestId(weaponReference.gameObject, "ZeroingRange.Weapon.Reference");
            CreateWeaponReference(weaponReference, darkMetal);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab");
            var xrOrigin = prefab == null ? new GameObject("XR Origin (VR)") : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            xrOrigin.name = "XR Origin (VR)";
            xrOrigin.transform.SetParent(anchors);
            xrOrigin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            P1XrFloorOriginUpgrader.ConfigureFloorOrigin(xrOrigin);
            AddTestId(xrOrigin, "ZeroingRange.Origin.VR");

            var gloveMaterial = GetMaterial("RangeSandbag", new Color(0.24f, 0.25f, 0.17f));
            ConfigureDirectInteractors(xrOrigin.transform, gloveMaterial);

            CreateFirstPersonTrainingWeapon(anchors, camera, xrOrigin.transform);
            var modeController = anchors.gameObject.AddComponent<ZeroingRangeXRModeController>();
            modeController.Configure(xrOrigin, camera);
        }

        private static void CreateFirstPersonTrainingWeapon(Transform anchors, Camera noVrCamera, Transform xrOrigin)
        {
            var playerRoot = CreateAnchor("WeaponPlayerRoot", anchors, Vector3.zero);
            AddTestId(playerRoot.gameObject, "ZeroingRange.Weapon.PlayerRoot");

            var tracerRoot = CreateAnchor("TracerRoot_training-rifle", playerRoot, Vector3.zero);
            AddTestId(tracerRoot.gameObject, "ZeroingRange.Weapon.TracerRoot");
            var debugInput = CreateAnchor("WeaponDebugInput", playerRoot, Vector3.zero);
            AddTestId(debugInput.gameObject, "ZeroingRange.Weapon.DebugInput");

            var prefab = TrainingRiflePrefabBuilder.EnsurePrefab();
            var weaponObject = prefab == null
                ? new GameObject("Weapon_training-rifle_Blockout")
                : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            weaponObject.name = "Weapon_training-rifle_Blockout";
            weaponObject.transform.SetParent(playerRoot, false);
            var weaponSpawn = anchors.Find("WeaponSpawnPoint");
            if (weaponSpawn != null)
            {
                ConfigureComfortableWeaponSpawn(weaponSpawn, weaponObject.transform);
            }

            var binding = weaponObject.GetComponent<WeaponPrefabBinding>();
            var controller = playerRoot.gameObject.AddComponent<FirstPersonTrainingWeaponController>();
            controller.ConfigureForScene(noVrCamera, binding, null, tracerRoot);
            ConfigureVrPoseSources(controller, xrOrigin);
            ConfigureHandVisuals(xrOrigin, binding,
                GetMaterial("RangeSandbag", new Color(0.24f, 0.25f, 0.17f)));
            ConfigureWeaponFeedback(playerRoot, binding, tracerRoot, xrOrigin, controller);
        }

        private static void ConfigureComfortableWeaponSpawn(Transform spawn, Transform weapon)
        {
            if (spawn == null)
            {
                return;
            }

            spawn.localPosition = ComfortableWeaponSpawnPosition;
            spawn.localRotation = Quaternion.Euler(0f, -90f, 0f);
            EditorUtility.SetDirty(spawn);

            if (weapon == null)
            {
                return;
            }

            weapon.SetPositionAndRotation(spawn.position, spawn.rotation);
            EditorUtility.SetDirty(weapon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(weapon);
        }

        private static void ConfigureVrPoseSources(FirstPersonTrainingWeaponController controller, Transform xrOrigin)
        {
            var headPose = xrOrigin != null
                ? xrOrigin.GetComponentsInChildren<Camera>(true).FirstOrDefault()?.transform
                : null;
            var rearHandPose = FindController(xrOrigin, "Right");
            var frontHandPose = FindController(xrOrigin, "Left");

            if (headPose != null)
                AddTestIdIfMissing(headPose.gameObject, "ZeroingRange.Origin.VR.HeadPose");
            if (rearHandPose != null)
                AddTestIdIfMissing(rearHandPose.gameObject, "ZeroingRange.Origin.VR.RearHandPose");
            if (frontHandPose != null)
                AddTestIdIfMissing(frontHandPose.gameObject, "ZeroingRange.Origin.VR.FrontHandPose");

            controller.ConfigureVrPoseSources(headPose, rearHandPose, frontHandPose);
        }

        private static void ConfigureDirectInteractors(Transform xrOrigin, Material handMaterial)
        {
            if (xrOrigin == null)
            {
                return;
            }

            if (xrOrigin.GetComponentInChildren<XRInteractionManager>(true) == null)
            {
                xrOrigin.gameObject.AddComponent<XRInteractionManager>();
            }

            DisableUnusedGazeInteractors(xrOrigin);

            CreateDirectInteractor(FindController(xrOrigin, "Right"), InteractorHandedness.Right,
                "<XRController>{RightHand}/gripPressed", handMaterial);
            CreateDirectInteractor(FindController(xrOrigin, "Left"), InteractorHandedness.Left,
                "<XRController>{LeftHand}/gripPressed", handMaterial);
        }

        private static void DisableUnusedGazeInteractors(Transform xrOrigin)
        {
            var behaviours = xrOrigin.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                var typeName = behaviour.GetType().Name;
                if (typeName == "GazeInputManager" || typeName == "XRGazeInteractor")
                {
                    behaviour.gameObject.SetActive(false);
                }
            }
        }

        private static void CreateDirectInteractor(
            Transform controller,
            InteractorHandedness handedness,
            string selectBinding,
            Material handMaterial)
        {
            if (controller == null)
            {
                return;
            }

            var suffix = handedness == InteractorHandedness.Right ? "Right" : "Left";
            var directObject = new GameObject($"DirectInteractor_{suffix}");
            directObject.transform.SetParent(controller, false);
            AddTestId(directObject, $"ZeroingRange.Origin.VR.{suffix}DirectInteractor");
            var collider = directObject.AddComponent<SphereCollider>();
            collider.radius = 0.11f;
            collider.isTrigger = true;

            var direct = directObject.AddComponent<XRDirectInteractor>();
            direct.handedness = handedness;
            direct.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.StateChange;
            direct.selectInput = new XRInputButtonReader("Grip Select")
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = new InputAction("Grip Select", InputActionType.Button, selectBinding)
            };

            EnsureHandVisualRoot(controller, handedness, handMaterial);
        }

        private static void ConfigureHandVisuals(
            Transform xrOrigin,
            WeaponPrefabBinding binding,
            Material handMaterial)
        {
            if (xrOrigin == null || binding == null)
            {
                return;
            }

            var grab = binding.GetComponent<TrainingRifleGrabInteractable>();
            ConfigureHandVisual(
                FindController(xrOrigin, "Right"),
                InteractorHandedness.Right,
                binding.RearHandGrip,
                grab,
                handMaterial);
            ConfigureHandVisual(
                FindController(xrOrigin, "Left"),
                InteractorHandedness.Left,
                binding.FrontHandGrip,
                grab,
                handMaterial);
        }

        private static GameObject EnsureHandVisualRoot(
            Transform controller,
            InteractorHandedness handedness,
            Material handMaterial)
        {
            if (controller == null)
            {
                return null;
            }

            var suffix = handedness == InteractorHandedness.Right ? "Right" : "Left";
            var hand = controller.Find($"VirtualHand_{suffix}")?.gameObject;
            if (hand == null)
            {
                hand = new GameObject($"VirtualHand_{suffix}");
                hand.transform.SetParent(controller, false);
            }

            hand.transform.localPosition = new Vector3(0f, -0.025f, 0.08f);
            hand.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            hand.transform.localScale = Vector3.one;

            var rootFilter = hand.GetComponent<MeshFilter>();
            if (rootFilter != null)
                Object.DestroyImmediate(rootFilter);
            var rootRenderer = hand.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                Object.DestroyImmediate(rootRenderer);
            var rootCollider = hand.GetComponent<Collider>();
            if (rootCollider != null)
                Object.DestroyImmediate(rootCollider);
            AddTestIdIfMissing(hand, $"ZeroingRange.Origin.VR.VirtualHand.{suffix}");

            var modelName = $"Model_{suffix}Hand";
            var model = hand.transform.Find(modelName)?.gameObject;
            if (model == null)
            {
                var modelPath = handedness == InteractorHandedness.Right
                    ? RightHandModelPath
                    : LeftHandModelPath;
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null)
                {
                    Debug.LogError($"[ZeroingRangeSceneBuilder] Missing virtual hand model: {modelPath}");
                    return hand;
                }

                model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (model == null)
                {
                    Debug.LogError($"[ZeroingRangeSceneBuilder] Could not instantiate virtual hand model: {modelPath}");
                    return hand;
                }

                model.name = modelName;
                model.transform.SetParent(hand.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                AddTestId(model, $"ZeroingRange.Origin.VR.HandVisual.{suffix}");
            }

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    materials[index] = handMaterial;
                }

                renderer.sharedMaterials = materials;
            }

            return hand;
        }

        private static void ConfigureHandVisual(
            Transform controller,
            InteractorHandedness handedness,
            Transform gripAnchor,
            TrainingRifleGrabInteractable grab,
            Material handMaterial)
        {
            var hand = EnsureHandVisualRoot(controller, handedness, handMaterial);
            if (hand == null)
            {
                return;
            }

            var suffix = handedness == InteractorHandedness.Right ? "Right" : "Left";
            var model = hand.transform.Find($"Model_{suffix}Hand");
            if (model == null)
            {
                return;
            }

            var visual = hand.GetComponent<VRControllerHandVisual>() ??
                         hand.AddComponent<VRControllerHandVisual>();
            var side = handedness == InteractorHandedness.Right
                ? VirtualHandSide.Right
                : VirtualHandSide.Left;
            var positionOffset = handedness == InteractorHandedness.Right
                ? new Vector3(0.002f, 0.050f, -0.020f)
                : new Vector3(-0.005f, -0.024f, -0.018f);
            var rotationOffset = handedness == InteractorHandedness.Right
                ? new Vector3(-90f, 0f, -5f)
                : new Vector3(0f, 0f, 180f);
            visual.Configure(side, model, grab, gripAnchor, positionOffset, rotationOffset);
        }

        private static void ConfigureWeaponFeedback(
            Transform playerRoot,
            WeaponPrefabBinding binding,
            Transform tracerRoot,
            Transform xrOrigin,
            FirstPersonTrainingWeaponController controller)
        {
            if (playerRoot == null || binding == null || controller == null)
            {
                return;
            }

            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileModelPath);
            var shot = AssetDatabase.LoadAssetAtPath<AudioClip>(RifleShotPath);
            var pickup = AssetDatabase.LoadAssetAtPath<AudioClip>(PickupPath);
            var flyby = AssetDatabase.LoadAssetAtPath<AudioClip>(FlybyPath);
            var impacts = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(ImpactThudPath),
                AssetDatabase.LoadAssetAtPath<AudioClip>(ImpactClinkPath)
            };
            var grab = binding.GetComponent<TrainingRifleGrabInteractable>();
            var feedback = playerRoot.GetComponent<WeaponFeedbackController>() ??
                           playerRoot.gameObject.AddComponent<WeaponFeedbackController>();
            feedback.Configure(
                binding.MuzzlePoint,
                tracerRoot,
                binding.transform,
                grab,
                projectile,
                shot,
                pickup,
                flyby,
                impacts);
            controller.ConfigureFeedback(feedback);

            var footsteps = playerRoot.GetComponent<PlayerFootstepAudio>() ??
                            playerRoot.gameObject.AddComponent<PlayerFootstepAudio>();
            var headPose = xrOrigin != null
                ? xrOrigin.GetComponentsInChildren<Camera>(true).FirstOrDefault()?.transform
                : null;
            var footstepClips = Enumerable.Range(1, 6)
                .Select(index => AssetDatabase.LoadAssetAtPath<AudioClip>(
                    $"{FootstepFolder}/footstep-concrete-{index:00}.ogg"))
                .Where(clip => clip != null)
                .ToArray();
            footsteps.Configure(headPose, footstepClips);
        }

        private static Transform FindController(Transform root, string handedness)
        {
            if (root == null)
            {
                return null;
            }

            var expectedName = $"{handedness} Controller";
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(transform => transform.name == expectedName) ??
                   FindChildByNameTokens(root, handedness, "Controller");
        }

        private static GameObject FindByTestId(string id)
        {
            return Object.FindObjectsOfType<SceneTestId>(true)
                .FirstOrDefault(candidate => candidate.Id == id)?.gameObject;
        }

        private static Transform FindChildByNameTokens(Transform root, params string[] tokens)
        {
            if (root == null)
            {
                return null;
            }

            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => tokens.All(token =>
                    transform.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static void CreateTarget(Transform root, Material targetMaterial, Material boardMaterial,
            Material ringMaterial, Material impactMarkerMaterial, Material supportMaterial)
        {
            var target = CreateAnchor("Target_Primary_100m", root, new Vector3(0f, 1.5f, 100f));
            AddTestId(target.gameObject, "ZeroingRange.Target.Primary");

            CreateCube("TargetBacker", target, new Vector3(0f, 0f, 0.03f), new Vector3(1.2f, 1.4f, 0.05f), targetMaterial);
            var face = CreateCube("TargetFace_50cm", target, Vector3.zero, new Vector3(0.5f, 0.5f, 0.02f), boardMaterial);
            AddTestId(face, "ZeroingRange.Target.Face");

            var targetCenter = CreateAnchor("TargetCenter", target, new Vector3(0f, 0f, -0.012f));
            AddTestId(targetCenter.gameObject, "ZeroingRange.Target.Center");
            var impactMarkers = CreateAnchor("ImpactMarkers", target, Vector3.zero);
            AddTestId(impactMarkers.gameObject, "ZeroingRange.Target.ImpactMarkers");

            var impactSurface = face.AddComponent<TargetImpactSurface>();
            impactSurface.Configure(face.GetComponent<Collider>(), targetCenter, impactMarkers, impactMarkerMaterial);

            CreateCube("TargetSilhouette_Torso", target, new Vector3(0f, -0.06f, -0.013f),
                new Vector3(0.32f, 0.34f, 0.008f), targetMaterial, false);
            CreateCylinder("TargetSilhouette_Head", target, new Vector3(0f, 0.17f, -0.014f),
                new Vector3(0.13f, 0.003f, 0.13f), targetMaterial, Quaternion.Euler(90f, 0f, 0f), false);

            var tenRing = CreateCylinder("TenRing_10cm", target, new Vector3(0f, 0f, -0.019f),
                new Vector3(0.1f, 0.0025f, 0.1f), ringMaterial, Quaternion.Euler(90f, 0f, 0f), false);
            AddTestId(tenRing, "ZeroingRange.Target.TenRing");

            CreateCube("TargetPost_Left", target, new Vector3(-0.18f, -0.95f, 0.08f),
                new Vector3(0.05f, 1.4f, 0.05f), supportMaterial);
            CreateCube("TargetPost_Right", target, new Vector3(0.18f, -0.95f, 0.08f),
                new Vector3(0.05f, 1.4f, 0.05f), supportMaterial);
        }

        private static void CreateBackground(Transform parent, Material earth, Material foliage)
        {
            CreateSphere("Mountain_Left", parent, new Vector3(-34f, 8f, 142f), new Vector3(55f, 16f, 35f), earth);
            CreateSphere("Mountain_Centre", parent, new Vector3(0f, 10f, 155f), new Vector3(70f, 22f, 38f), earth);
            CreateSphere("Mountain_Right", parent, new Vector3(38f, 9f, 145f), new Vector3(58f, 18f, 34f), earth);

            for (var index = 0; index < 18; index++)
            {
                var side = index % 2 == 0 ? -1f : 1f;
                var z = 12f + index * 5.2f;
                var x = side * (10.5f + index % 3);
                var height = 2.6f + index % 4 * 0.35f;
                CreateTree($"Tree_{index:00}", parent, new Vector3(x, 3.3f, z), height, foliage);
            }

            CreateBillboardTreeLine(parent, -1f, "Left");
            CreateBillboardTreeLine(parent, 1f, "Right");
        }

        private static void CreateForeground(Transform parent, Material darkMetal, Material sandbag)
        {
            CreateCube("ShootingBench", parent, new Vector3(0f, 0.72f, 2.8f), new Vector3(5f, 0.10f, 0.75f), darkMetal);
            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                for (var row = 0; row < 3; row++)
                for (var column = 0; column < 4 - row; column++)
                {
                    var x = side * (1.7f + column * 0.45f + row * 0.20f);
                    CreateCapsule($"Sandbag_{sideIndex}_{row}_{column}", parent,
                        new Vector3(x, 0.65f + row * 0.22f, 3.5f), new Vector3(0.20f, 0.18f, 0.20f),
                        sandbag, Quaternion.Euler(0f, 0f, 90f), false);
                }
            }
        }

        private static void CreateDistanceMarker(Transform parent, float x, float z, Material marker, Material post)
        {
            CreateCube($"DistancePost_{z:0}m_{x:0.0}", parent, new Vector3(x, 0.42f, z),
                new Vector3(0.05f, 0.8f, 0.05f), post);
            CreateCube($"DistanceBoard_{z:0}m_{x:0.0}", parent, new Vector3(x, 0.83f, z),
                new Vector3(0.45f, 0.55f, 0.04f), marker);
        }

        private static void CreateWeaponReference(Transform parent, Material material)
        {
            CreateCube("Receiver", parent, new Vector3(0f, 1.17f, 1.25f), new Vector3(0.34f, 0.22f, 1.25f), material, false);
            CreateCube("Barrel", parent, new Vector3(0f, 1.28f, 2.2f), new Vector3(0.07f, 0.07f, 1.5f), material, false);
            CreateCube("RearSight_Left", parent, new Vector3(-0.12f, 1.46f, 1.05f), new Vector3(0.055f, 0.27f, 0.08f), material, false);
            CreateCube("RearSight_Right", parent, new Vector3(0.12f, 1.46f, 1.05f), new Vector3(0.055f, 0.27f, 0.08f), material, false);
            CreateCube("RearSight_Top", parent, new Vector3(0f, 1.58f, 1.05f), new Vector3(0.295f, 0.05f, 0.08f), material, false);
            CreateCube("FrontSight", parent, new Vector3(0f, 1.40f, 2.85f), new Vector3(0.025f, 0.16f, 0.04f), material, false);
        }

        private static void CreateTree(string name, Transform parent, Vector3 position, float height, Material material)
        {
            var tree = new GameObject(name).transform;
            tree.SetParent(parent, false);
            tree.localPosition = position;
            CreateCylinder("Trunk", tree, Vector3.zero, new Vector3(0.15f, height * 0.3f, 0.15f), material,
                Quaternion.identity, false);
        }

        private static void CreateBillboardTreeLine(Transform parent, float side, string sideName)
        {
            var material = GetTreeBillboardMaterial();
            var line = CreateAnchor($"TreeLine_{sideName}", parent, Vector3.zero);
            AddTestId(line.gameObject, $"ZeroingRange.Environment.TreeLine.{sideName}");

            for (var index = 0; index < 6; index++)
            {
                var height = 7.5f + index % 3 * 1.1f;
                var x = side * (16.5f + index % 2 * 1.8f);
                var z = 12f + index * 16f;
                var tree = CreateAnchor($"Broadleaf_{sideName}_{index:00}", line, new Vector3(x, 3.85f, z));
                var size = new Vector3(height * 0.67f, height, 1f);
                var centre = new Vector3(0f, height * 0.5f, 0f);

                foreach (var angle in new[] { 0f, 90f, 180f, 270f })
                {
                    var billboard = CreatePrimitive(PrimitiveType.Quad, $"Billboard_{angle:0}", tree, centre,
                        size, material, Quaternion.Euler(0f, angle, 0f), false);
                    var renderer = billboard.GetComponent<Renderer>();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
            }
        }

        private static void CreateLighting(Transform root)
        {
            var lightObject = new GameObject("RangeSun", typeof(Light));
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            AddTestId(lightObject, "ZeroingRange.Lighting.Sun");
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            light.shadowStrength = 0.8f;

            PostWarSkyboxInstaller.ApplyEnvironment(GetSkyboxMaterial());
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool keepCollider = true, Quaternion? rotation = null)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, position, scale, material,
                rotation ?? Quaternion.identity, keepCollider);
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation, bool keepCollider = true)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, parent, position, scale, material, rotation, keepCollider);
        }

        private static GameObject CreateCapsule(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation, bool keepCollider = true)
        {
            return CreatePrimitive(PrimitiveType.Capsule, name, parent, position, scale, material, rotation, keepCollider);
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material)
        {
            return CreatePrimitive(PrimitiveType.Sphere, name, parent, position, scale, material, Quaternion.identity, false);
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, Quaternion rotation, bool keepCollider)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = rotation;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            return gameObject;
        }

        private static Transform CreateAnchor(string name, Transform parent, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }

        private static void AddTestId(GameObject gameObject, string id)
        {
            gameObject.AddComponent<SceneTestId>().Id = id;
        }

        private static void AddTestIdIfMissing(GameObject gameObject, string id)
        {
            var testId = gameObject.GetComponent<SceneTestId>();
            if (testId == null)
            {
                AddTestId(gameObject, id);
                return;
            }

            if (string.IsNullOrEmpty(testId.Id))
            {
                testId.Id = id;
            }
        }

        private static void MarkRenderersStatic(Transform root)
        {
            var flags = StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.ReflectionProbeStatic;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
        }

        private static Material GetMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", name.Contains("Metal") ? 0.3f : 0.05f);
                EditorUtility.SetDirty(material);
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", name.Contains("Metal") ? 0.3f : 0.05f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetSkyboxMaterial()
        {
            return PostWarSkyboxInstaller.GetOrCreateMaterial();
        }

        private static Material GetTreeBillboardMaterial()
        {
            var importer = AssetImporter.GetAtPath(TreeTexturePath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Tree texture was not imported: {TreeTexturePath}");

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.35f;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TreeTexturePath);
            var path = $"{MaterialFolder}/RangeTreeBillboard.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Unlit/Transparent Cutout");
            if (shader == null)
                throw new System.InvalidOperationException("Unity shader 'Unlit/Transparent Cutout' is unavailable.");

            if (material == null)
            {
                material = new Material(shader) { name = "RangeTreeBillboard" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.mainTexture = texture;
            material.SetFloat("_Cutoff", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var separator = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(scene => scene.path != ScenePath))
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
