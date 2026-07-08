using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class ZeroingRangeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        private const string MaterialFolder = "Assets/SimulatedShooting/Art/Materials";

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

        [MenuItem("Tools/Simulated Shooting/Run Zeroing Range Scene")]
        public static void OpenAndPlay()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            Debug.Log("Running ZeroingRangeScene with the no-VR test camera.");
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
            AddTestId(CreateAnchor("WeaponSpawnPoint", anchors, new Vector3(0f, 1.25f, 0.35f)).gameObject,
                "ZeroingRange.WeaponSpawn");
            AddTestId(CreateAnchor("HudAnchor", anchors, new Vector3(0f, 1.55f, 1.5f)).gameObject,
                "ZeroingRange.HudAnchor");

            var noVrCamera = new GameObject("Camera_NoVR", typeof(Camera), typeof(AudioListener));
            noVrCamera.transform.SetParent(anchors);
            noVrCamera.transform.SetPositionAndRotation(shootingPosition.position, Quaternion.identity);
            var camera = noVrCamera.GetComponent<Camera>();
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
            AddTestId(xrOrigin, "ZeroingRange.Origin.VR");

            CreateFirstPersonTrainingWeapon(anchors, camera, xrOrigin.transform);
            xrOrigin.SetActive(false);
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

            var binding = weaponObject.GetComponent<WeaponPrefabBinding>();
            var controller = playerRoot.gameObject.AddComponent<FirstPersonTrainingWeaponController>();
            controller.ConfigureForScene(noVrCamera, binding, null, tracerRoot);
            ConfigureVrPoseSources(controller, xrOrigin);
        }

        private static void ConfigureVrPoseSources(FirstPersonTrainingWeaponController controller, Transform xrOrigin)
        {
            var headPose = xrOrigin != null
                ? xrOrigin.GetComponentsInChildren<Camera>(true).FirstOrDefault()?.transform
                : null;
            var rearHandPose = FindChildByNameTokens(xrOrigin, "Right", "Controller");
            var frontHandPose = FindChildByNameTokens(xrOrigin, "Left", "Controller");

            if (headPose != null)
                AddTestIdIfMissing(headPose.gameObject, "ZeroingRange.Origin.VR.HeadPose");
            if (rearHandPose != null)
                AddTestIdIfMissing(rearHandPose.gameObject, "ZeroingRange.Origin.VR.RearHandPose");
            if (frontHandPose != null)
                AddTestIdIfMissing(frontHandPose.gameObject, "ZeroingRange.Origin.VR.FrontHandPose");

            controller.ConfigureVrPoseSources(headPose, rearHandPose, frontHandPose);
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
            for (var layer = 0; layer < 3; layer++)
                CreateSphere($"Crown_{layer}", tree, new Vector3(0f, layer * height * 0.22f, 0f),
                    new Vector3(height * (0.55f - layer * 0.10f), height * 0.42f,
                        height * (0.55f - layer * 0.10f)), material);
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

            RenderSettings.skybox = GetSkyboxMaterial();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.50f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.32f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.13f, 0.10f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.46f, 0.57f, 0.65f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 210f;
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
            var path = $"{MaterialFolder}/RangeSkybox.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Procedural")) { name = "RangeSkybox" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_SkyTint", new Color(0.42f, 0.62f, 0.82f));
            material.SetColor("_GroundColor", new Color(0.24f, 0.24f, 0.19f));
            material.SetFloat("_AtmosphereThickness", 0.8f);
            material.SetFloat("_Exposure", 1.15f);
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
