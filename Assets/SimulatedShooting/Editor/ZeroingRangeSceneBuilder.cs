using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class ZeroingRangeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        private const string CoastalCliffInstallRequestPath = "Library/CoastalCliffInstall.request";
        private const string CoastalCliffInstallCompletePath = "Library/CoastalCliffInstall.complete";
        private const string MaterialFolder = "Assets/SimulatedShooting/Art/Materials";
        private const string TreeTexturePath =
            "Assets/SimulatedShooting/Art/Vegetation/RangeTree_Broadleaf.png";
        private const string ConcreteTextureFolder =
            "Assets/SimulatedShooting/Art/Textures/ConcreteFloor01";
        private const string ConcreteDiffusePath = ConcreteTextureFolder + "/concrete_floor_01_diff_2k.jpg";
        private const string ConcreteNormalPath = ConcreteTextureFolder + "/concrete_floor_01_nor_dx_2k.jpg";
        private const string ConcreteOcclusionPath = ConcreteTextureFolder + "/concrete_floor_01_ao_2k.jpg";
        private const string BrownMudTextureFolder =
            "Assets/SimulatedShooting/Art/Textures/BrownMud/brown_mud_2k.blend/textures";
        private const string BrownMudDiffusePath = BrownMudTextureFolder + "/brown_mud_diff_2k.jpg";
        private const string BrownMudNormalPath = BrownMudTextureFolder + "/brown_mud_nor_gl_2k.exr";
        private const string FactoryTextureFolder =
            "Assets/SimulatedShooting/Art/Architecture/ModularFactoryFacade/Textures";
        private const string FactoryBrickDiffusePath = FactoryTextureFolder + "/factory_brick_diff_1k.png";
        private const string FactoryBrickNormalPath = FactoryTextureFolder + "/factory_brick_nor_gl_1k.png";
        private const string FactoryDoorsDiffusePath = FactoryTextureFolder + "/factory_doors_diff_1k.png";
        private const string FactoryDoorsNormalPath = FactoryTextureFolder + "/factory_doors_nor_gl_1k.png";
        private const string FactoryGarageDiffusePath = FactoryTextureFolder + "/factory_garage_diff_1k.png";
        private const string FactoryGarageNormalPath = FactoryTextureFolder + "/factory_garage_nor_gl_1k.png";
        private const string FactoryWindowsDiffusePath = FactoryTextureFolder + "/factory_windows_diff_1k.png";
        private const string FactoryWindowsNormalPath = FactoryTextureFolder + "/factory_windows_nor_gl_1k.png";
        private const string CoastalCliffFolder =
            "Assets/SimulatedShooting/Art/Environment/CoastalCliff01";
        private const string CoastalCliffModelPath = CoastalCliffFolder + "/coastal_cliff_01_4k.fbx";
        private const string CoastalCliffDiffusePath =
            CoastalCliffFolder + "/textures/coastal_cliff_01_diff_4k.jpg";
        private const string CoastalCliffNormalPath =
            CoastalCliffFolder + "/textures/coastal_cliff_01_nor_gl_4k.exr";
        private const string CoastalCliffRoughnessPath =
            CoastalCliffFolder + "/textures/coastal_cliff_01_rough_4k.exr";
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

        [InitializeOnLoadMethod]
        private static void RunRequestedCoastalCliffInstall()
        {
            if (!File.Exists(CoastalCliffInstallRequestPath))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    InstallCoastalCliffBackdrop();
                    File.WriteAllText(CoastalCliffInstallCompletePath, "success");
                    File.Delete(CoastalCliffInstallRequestPath);
                }
                catch (System.Exception exception)
                {
                    File.WriteAllText(CoastalCliffInstallCompletePath, exception.ToString());
                    Debug.LogException(exception);
                }
            };
        }

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

        [MenuItem("Tools/Simulated Shooting/Install Coastal Cliff Backdrop")]
        public static void InstallCoastalCliffBackdrop()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureCoastalCliffImporters();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var environment = GameObject.Find("ZeroingRange/Environment")?.transform;
            if (environment == null)
                throw new System.InvalidOperationException("ZeroingRange/Environment was not found.");

            RemoveLegacyMountainPlaceholders(environment);
            CreateCoastalCliffBackdrop(environment);
            MarkRenderersStatic(environment);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ZeroingRangeSceneBuilder] Installed the LOD coastal cliff behind the 100m target.");
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
            ConfigureConcreteTextureImporter(ConcreteDiffusePath, TextureImporterType.Default, true, false);
            ConfigureConcreteTextureImporter(ConcreteNormalPath, TextureImporterType.NormalMap, false, true);
            ConfigureConcreteTextureImporter(ConcreteOcclusionPath, TextureImporterType.Default, false, false);
            ConfigureBrownMudTextureImporter(BrownMudDiffusePath, TextureImporterType.Default, true);
            ConfigureBrownMudTextureImporter(BrownMudNormalPath, TextureImporterType.NormalMap, false);
            ConfigureFactoryTextureImporter(FactoryBrickDiffusePath, TextureImporterType.Default, true);
            ConfigureFactoryTextureImporter(FactoryBrickNormalPath, TextureImporterType.NormalMap, false);
            ConfigureFactoryTextureImporter(FactoryDoorsDiffusePath, TextureImporterType.Default, true);
            ConfigureFactoryTextureImporter(FactoryDoorsNormalPath, TextureImporterType.NormalMap, false);
            ConfigureFactoryTextureImporter(FactoryGarageDiffusePath, TextureImporterType.Default, true);
            ConfigureFactoryTextureImporter(FactoryGarageNormalPath, TextureImporterType.NormalMap, false);
            ConfigureFactoryTextureImporter(FactoryWindowsDiffusePath, TextureImporterType.Default, true);
            ConfigureFactoryTextureImporter(FactoryWindowsNormalPath, TextureImporterType.NormalMap, false);
            ConfigureCoastalCliffImporters();
            var ground = GetMaterial("RangeGround", new Color(0.16f, 0.20f, 0.10f));
            var laneConcrete = GetConcreteMaterial("RangeConcrete", new Vector2(7f, 53f));
            var padConcrete = GetConcreteMaterial("RangeConcretePad", new Vector2(7.5f, 2f));
            var bermSurface = GetBrownMudMaterial();
            var factoryBrick = GetFactoryMaterial("FactoryBrick", FactoryBrickDiffusePath, FactoryBrickNormalPath,
                new Vector2(2.5f, 2f), Vector2.zero, new Color(0.58f, 0.62f, 0.62f), 0f, 0.08f);
            var factoryConcrete = GetConcreteMaterial("FactoryConcrete", new Vector2(1.8f, 2.2f));
            factoryConcrete.color = new Color(0.72f, 0.74f, 0.73f);
            factoryConcrete.SetFloat("_BumpScale", 0.95f);
            factoryConcrete.SetFloat("_OcclusionStrength", 0.78f);
            factoryConcrete.SetFloat("_Smoothness", 0.025f);
            EditorUtility.SetDirty(factoryConcrete);
            var factoryDoor = GetFactoryMaterial("FactoryDoor", FactoryDoorsDiffusePath, FactoryDoorsNormalPath,
                new Vector2(0.72f, 0.48f), new Vector2(0f, 0.05f), new Color(0.7f, 0.74f, 0.75f),
                0.15f, 0.14f);
            var factoryGarage = GetFactoryMaterial("FactoryGarage", FactoryGarageDiffusePath,
                FactoryGarageNormalPath, new Vector2(1f, 0.52f), Vector2.zero,
                new Color(0.62f, 0.67f, 0.69f), 0.35f, 0.18f);
            var factoryWindow = GetFactoryMaterial("FactoryWindow", FactoryWindowsDiffusePath,
                FactoryWindowsNormalPath, new Vector2(0.16f, 0.24f), new Vector2(0.06f, 0.08f),
                new Color(0.56f, 0.61f, 0.64f), 0.25f, 0.2f);
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
            CreateEnvironment(root, ground, laneConcrete, padConcrete, bermSurface, line, marker,
                darkMetal, sandbag, factoryBrick, factoryConcrete, factoryDoor, factoryGarage, factoryWindow);
            CreateVisualPolish(root, darkMetal, marker, line, crate);
            CreateAnchors(root, darkMetal);
            CreateTarget(root, target, targetBoard, ring, impactMarker, padConcrete);
            CreateLighting(root);
        }

        private static void CreateEnvironment(Transform root, Material ground, Material laneConcrete,
            Material padConcrete, Material bermSurface, Material line, Material marker,
            Material darkMetal, Material sandbag, Material factoryBrick, Material factoryConcrete,
            Material factoryDoor, Material factoryGarage, Material factoryWindow)
        {
            var environment = new GameObject("Environment").transform;
            environment.SetParent(root);
            CreateCube("Ground", environment, new Vector3(0f, -0.2f, 55f), new Vector3(42f, 0.4f, 130f), ground);
            var lane = CreateCube("RangeLane_100m", environment, new Vector3(0f, -0.02f, 52f),
                new Vector3(14f, 0.12f, 106f), laneConcrete);
            AddTestId(lane, "ZeroingRange.Environment.Lane");
            CreateCube("FiringPad", environment, new Vector3(0f, 0.01f, -0.6f), new Vector3(15f, 0.16f, 4f),
                padConcrete);
            CreateCube("FiringLine", environment, new Vector3(0f, 0.11f, 0f), new Vector3(15f, 0.03f, 0.09f), marker, false);

            CreateCube("LaneEdge_Left", environment, new Vector3(-6.9f, 0.09f, 52f), new Vector3(0.10f, 0.03f, 104f), line, false);
            CreateCube("LaneEdge_Right", environment, new Vector3(6.9f, 0.09f, 52f), new Vector3(0.10f, 0.03f, 104f), line, false);
            CreateCube("LaneGuide_Left", environment, new Vector3(-2.4f, 0.09f, 52f), new Vector3(0.06f, 0.03f, 104f), line, false);
            CreateCube("LaneGuide_Right", environment, new Vector3(2.4f, 0.09f, 52f), new Vector3(0.06f, 0.03f, 104f), line, false);
            for (var distance = 10; distance < 100; distance += 10)
                CreateCube($"LaneMark_{distance}m", environment, new Vector3(0f, 0.10f, distance),
                    new Vector3(13.5f, 0.025f, 0.06f), line, false);

            CreateFactoryBuildings(environment, padConcrete, darkMetal, factoryBrick, factoryConcrete, factoryDoor,
                factoryGarage, factoryWindow);
            CreateCube("TargetBackstop", environment, new Vector3(0f, 2.2f, 103f), new Vector3(16f, 4.5f, 1f),
                bermSurface);

            CreateBackground(environment);
            CreateForeground(environment, darkMetal, sandbag);

            foreach (var distance in new[] { 25f, 50f, 75f, 100f })
            {
                CreateDistanceMarker(environment, -6.2f, distance, marker, darkMetal);
                CreateDistanceMarker(environment, 6.2f, distance, marker, darkMetal);
            }

            MarkRenderersStatic(environment);
        }

        private static void CreateFactoryBuildings(Transform parent, Material foundation, Material roof,
            Material brick, Material concrete, Material door, Material garage, Material window)
        {
            var left = CreateAnchor("Buildings_Left", parent, Vector3.zero);
            AddTestId(left.gameObject, "ZeroingRange.Environment.Buildings.Left");
            CreateFactoryBuilding(left, -1f, "Admin", 11f, 5.8f, 4.8f, 10f, 9.4f, -2f, false, true, true,
                foundation, roof, concrete, door, garage, window);
            CreateFactoryBuilding(left, -1f, "Warehouse", 39f, 7.2f, 6.8f, 17f, 10.8f, 1.5f, true, false, false,
                foundation, roof, brick, door, garage, window);
            CreateFactoryBuilding(left, -1f, "Workshop", 73f, 6.4f, 5.2f, 13f, 9.2f, -1f, false, false, true,
                foundation, roof, concrete, door, garage, window);

            var right = CreateAnchor("Buildings_Right", parent, Vector3.zero);
            AddTestId(right.gameObject, "ZeroingRange.Environment.Buildings.Right");
            CreateFactoryBuilding(right, 1f, "Service", 20f, 6.2f, 5f, 12f, 10.2f, 2.5f, false, true, false,
                foundation, roof, concrete, door, garage, window);
            CreateFactoryBuilding(right, 1f, "Hangar", 51f, 8f, 7.4f, 20f, 9.4f, -1.5f, true, false, true,
                foundation, roof, brick, door, garage, window);
            CreateFactoryBuilding(right, 1f, "Utility", 84f, 5.6f, 4.6f, 10f, 11.1f, 2f, false, false, true,
                foundation, roof, concrete, door, garage, window);
        }

        private static void CreateFactoryBuilding(Transform parent, float side, string name, float z, float width,
            float height, float depth, float setback, float yaw, bool hasGarage, bool hasGableRoof, bool hasChimney,
            Material foundation, Material roof, Material wall, Material door, Material garage, Material window)
        {
            var building = CreateAnchor($"Building_{name}", parent,
                new Vector3(side * (setback + width * 0.5f), 0f, z));
            building.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateCube("Foundation", building, new Vector3(0f, 0.14f, 0f),
                new Vector3(width + 0.45f, 0.28f, depth + 0.45f), foundation);
            CreateCube("WallBody", building, new Vector3(0f, height * 0.5f + 0.28f, 0f),
                new Vector3(width, height, depth), wall);

            var annexWidth = width * 0.42f;
            var annexHeight = height * 0.58f;
            var annexDepth = depth * 0.44f;
            CreateCube("RearAnnex", building,
                new Vector3(side * (width * 0.5f + annexWidth * 0.5f - 0.18f), annexHeight * 0.5f + 0.28f,
                    depth * 0.2f),
                new Vector3(annexWidth, annexHeight, annexDepth), wall);

            var innerX = -side * (width * 0.5f + 0.055f);
            var damageSign = name.Length % 2 == 0 ? -1f : 1f;
            CreateCube("FacadeCornice", building,
                new Vector3(innerX, height - 0.22f, -damageSign * depth * 0.22f),
                new Vector3(0.15f, 0.22f, depth * 0.52f), foundation, false,
                Quaternion.Euler(damageSign * 3f, 0f, 0f));
            CreateCube("Pilaster_Near", building, new Vector3(innerX - side * 0.025f, height * 0.48f, -depth * 0.43f),
                new Vector3(0.19f, height * 0.9f, 0.38f), foundation, false);
            CreateCube("Pilaster_Far", building, new Vector3(innerX - side * 0.025f, height * 0.48f, depth * 0.43f),
                new Vector3(0.19f, height * 0.9f, 0.38f), foundation, false);
            CreateCube("Damage_SootPatch", building,
                new Vector3(innerX - side * 0.07f, height * 0.54f, damageSign * depth * 0.28f),
                new Vector3(0.12f, Mathf.Min(3.1f, height * 0.62f), 1.55f), roof, false,
                Quaternion.Euler(damageSign * 5f, 0f, 0f));
            CreateCube("Damage_CollapsedRoof", building,
                new Vector3(-side * width * 0.08f, height + 0.05f, damageSign * depth * 0.32f),
                new Vector3(width * 0.48f, 0.2f, depth * 0.3f), roof, false,
                Quaternion.Euler(damageSign * 13f, side * 5f, damageSign * 9f));
            if (hasGarage)
            {
                CreateCube("GarageDoor", building, new Vector3(innerX, 1.85f, 0f),
                    new Vector3(0.10f, 3.35f, 4.9f), garage, false);
                CreateCube("LoadingDock", building, new Vector3(innerX - side * 0.38f, 0.42f, 0f),
                    new Vector3(0.75f, 0.55f, 5.4f), foundation);
                CreateCube("GarageCanopy", building, new Vector3(innerX - side * 0.5f, 3.72f, 0f),
                    new Vector3(1f, 0.16f, 5.45f), roof, false);
            }
            else
            {
                CreateCube("EntryDoor", building, new Vector3(innerX, 1.35f, -depth * 0.24f),
                    new Vector3(0.10f, 2.45f, 1.55f), door, false);
                CreateCube("EntryCanopy", building, new Vector3(innerX - side * 0.45f, 2.72f, -depth * 0.24f),
                    new Vector3(0.9f, 0.14f, 2.05f), roof, false);
            }

            var windowCount = hasGarage ? 2 : 3;
            for (var index = 0; index < windowCount; index++)
            {
                var normalized = windowCount == 2 ? (index == 0 ? -0.34f : 0.34f) : -0.32f + index * 0.32f;
                var windowZ = normalized * depth;
                if (!hasGarage && index == 0)
                    windowZ = depth * 0.18f;
                var windowMaterial = index == name.Length % windowCount ? roof : window;
                CreateCube($"Window_{index:00}", building,
                    new Vector3(innerX, Mathf.Min(3.25f, height * 0.64f + 0.28f), windowZ),
                    new Vector3(0.11f, 1.35f, 1.55f), windowMaterial, false);
                CreateCube($"WindowLintel_{index:00}", building,
                    new Vector3(innerX - side * 0.035f, Mathf.Min(4f, height * 0.64f + 1.02f), windowZ),
                    new Vector3(0.16f, 0.12f, 1.82f), roof, false);
            }

            if (height > 6f)
            {
                for (var index = 0; index < 2; index++)
                {
                    var upperWindowZ = (index == 0 ? -0.24f : 0.24f) * depth;
                    CreateCube($"UpperWindow_{index:00}", building,
                        new Vector3(innerX, height - 1.35f, upperWindowZ),
                        new Vector3(0.11f, 1.25f, 1.65f), window, false);
                    CreateCube($"UpperWindowLintel_{index:00}", building,
                        new Vector3(innerX - side * 0.035f, height - 0.65f, upperWindowZ),
                        new Vector3(0.16f, 0.12f, 1.92f), roof, false);
                }
            }

            if (hasGableRoof)
            {
                var rise = 1.1f;
                var angle = Mathf.Atan2(rise, width * 0.5f) * Mathf.Rad2Deg;
                var slopeLength = Mathf.Sqrt(width * width * 0.25f + rise * rise);
                CreateCube("Roof_Left", building,
                    new Vector3(-width * 0.25f, height + 0.28f + rise * 0.5f, -damageSign * depth * 0.1f),
                    new Vector3(slopeLength + 0.35f, 0.18f, depth * 0.74f), roof, true,
                    Quaternion.Euler(-damageSign * 3f, 0f, angle));
                CreateCube("Roof_Right", building,
                    new Vector3(width * 0.25f, height + 0.18f + rise * 0.5f, damageSign * depth * 0.27f),
                    new Vector3(slopeLength * 0.78f, 0.18f, depth * 0.4f), roof, true,
                    Quaternion.Euler(damageSign * 8f, side * 4f, -angle + damageSign * 5f));
            }
            else
            {
                CreateCube("FlatRoof", building,
                    new Vector3(side * width * 0.06f, height + 0.28f, -damageSign * depth * 0.19f),
                    new Vector3(width * 0.86f, 0.3f, depth * 0.58f), roof, true,
                    Quaternion.Euler(damageSign * 4f, side * 2f, damageSign * 3f));
                CreateCube("Parapet_Inner", building,
                    new Vector3(-side * width * 0.5f, height + 0.62f, -damageSign * depth * 0.3f),
                    new Vector3(0.22f, 0.58f, depth * 0.34f), wall, true,
                    Quaternion.Euler(damageSign * 6f, 0f, damageSign * 4f));
                CreateCube("RoofAccess", building,
                    new Vector3(side * width * 0.18f, height + 0.92f, -depth * 0.2f),
                    new Vector3(1.55f, 1.65f, 2.25f), wall, true,
                    Quaternion.Euler(0f, 0f, side * 5f));
            }

            if (hasChimney)
                CreateCube("Chimney", building, new Vector3(side * width * 0.22f, height + 1.25f, depth * 0.22f),
                    new Vector3(0.75f, 2.2f, 0.75f), wall, true,
                    Quaternion.Euler(damageSign * 3f, 0f, side * 11f));
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
                ? new Vector3(0.002f, -0.018f, -0.025f)
                : new Vector3(-0.002f, -0.012f, -0.018f);
            var rotationOffset = handedness == InteractorHandedness.Right
                ? new Vector3(4f, 0f, -5f)
                : new Vector3(2f, 0f, 5f);
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

        private static void CreateBackground(Transform parent)
        {
            CreateCoastalCliffBackdrop(parent);

            CreateBillboardTreeLine(parent, -1f, "Left");
            CreateBillboardTreeLine(parent, 1f, "Right");
        }

        private static void CreateCoastalCliffBackdrop(Transform parent)
        {
            var existing = parent.Find("CoastalCliff_Backdrop");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CoastalCliffModelPath);
            if (model == null)
                throw new System.InvalidOperationException($"Coastal cliff model was not imported: {CoastalCliffModelPath}");

            var backdrop = CreateAnchor("CoastalCliff_Backdrop", parent, Vector3.zero);
            AddTestIdIfMissing(backdrop.gameObject, "ZeroingRange.Environment.MountainBackdrop");
            CreateCoastalCliffLayer(backdrop, model, "CliffLayer_Front", 105f, 0f, -2f, 118f, 180f);
            CreateCoastalCliffLayer(backdrop, model, "CliffLayer_RearLeft", 102f, -34f, 2.5f, 130f, 165f);
            CreateCoastalCliffLayer(backdrop, model, "CliffLayer_RearRight", 98f, 34f, 5.5f, 136f, 198f);
        }

        private static void CreateCoastalCliffLayer(Transform parent, GameObject model, string name,
            float desiredWidth, float x, float groundY, float frontZ, float yaw)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new System.InvalidOperationException("Coastal cliff model contains no renderers.");

            var initialBounds = CalculateRendererBounds(renderers);
            var scale = desiredWidth / initialBounds.size.x;
            instance.transform.localScale = Vector3.one * scale;
            var scaledBounds = CalculateRendererBounds(renderers);
            instance.transform.position += new Vector3(
                x - scaledBounds.center.x,
                groundY - scaledBounds.min.y,
                frontZ - scaledBounds.min.z);

            var material = GetCoastalCliffMaterial();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            ConfigureCoastalCliffLod(instance, renderers);
        }

        private static void ConfigureCoastalCliffLod(GameObject instance, Renderer[] renderers)
        {
            var lodGroup = instance.GetComponentInChildren<LODGroup>(true);
            if (lodGroup == null)
            {
                var lods = Enumerable.Range(0, 4)
                    .Select(index => renderers.Where(renderer =>
                            renderer.name.Contains($"LOD{index}") ||
                            renderer.transform.parent != null && renderer.transform.parent.name.Contains($"LOD{index}"))
                        .ToArray())
                    .Where(levelRenderers => levelRenderers.Length > 0)
                    .Select((levelRenderers, index) => new LOD(
                        new[] { 0.55f, 0.28f, 0.12f, 0.025f }[index], levelRenderers))
                    .ToArray();
                if (lods.Length < 3)
                    throw new System.InvalidOperationException("Coastal cliff FBX did not expose at least three LOD levels.");

                lodGroup = instance.AddComponent<LODGroup>();
                lodGroup.SetLODs(lods);
            }

            lodGroup.fadeMode = LODFadeMode.None;
            lodGroup.animateCrossFading = false;
            lodGroup.RecalculateBounds();
        }

        private static Bounds CalculateRendererBounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void RemoveLegacyMountainPlaceholders(Transform environment)
        {
            foreach (var name in new[] { "Mountain_Left", "Mountain_Centre", "Mountain_Right" })
            {
                var placeholder = environment.Find(name);
                if (placeholder != null)
                    Object.DestroyImmediate(placeholder.gameObject);
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

        private static void CreateBillboardTreeLine(Transform parent, float side, string sideName)
        {
            var material = GetTreeBillboardMaterial();
            var line = CreateAnchor($"TreeLine_{sideName}", parent, Vector3.zero);
            AddTestId(line.gameObject, $"ZeroingRange.Environment.TreeLine.{sideName}");
            var positions = side < 0f
                ? new[]
                {
                    new Vector3(-13.2f, 0f, 3f), new Vector3(-14.8f, 0f, 21f),
                    new Vector3(-12.8f, 0f, 28f), new Vector3(-14.2f, 0f, 55f),
                    new Vector3(-13.5f, 0f, 63f), new Vector3(-14.6f, 0f, 98f)
                }
                : new[]
                {
                    new Vector3(13.1f, 0f, 6f), new Vector3(14.5f, 0f, 32f),
                    new Vector3(12.9f, 0f, 38f), new Vector3(14.2f, 0f, 65f),
                    new Vector3(13.6f, 0f, 71f), new Vector3(14.8f, 0f, 101f)
                };
            var heights = side < 0f
                ? new[] { 8.8f, 10.2f, 7.9f, 11.3f, 9.4f, 10.6f }
                : new[] { 9.6f, 8.2f, 11f, 9.1f, 10.7f, 8.7f };

            for (var index = 0; index < 6; index++)
            {
                var height = heights[index];
                var tree = CreateAnchor($"Broadleaf_{sideName}_{index:00}", line, positions[index]);
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

        private static Material GetConcreteMaterial(string name, Vector2 tiling)
        {
            var material = GetMaterial(name, Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteDiffusePath));
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteDiffusePath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteNormalPath));
            material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ConcreteOcclusionPath));
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTextureScale("_MainTex", tiling);
            material.SetTextureScale("_BumpMap", tiling);
            material.SetTextureScale("_OcclusionMap", tiling);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetTextureOffset("_MainTex", Vector2.zero);
            material.SetTextureOffset("_BumpMap", Vector2.zero);
            material.SetTextureOffset("_OcclusionMap", Vector2.zero);
            material.SetFloat("_BumpScale", 0.65f);
            material.SetFloat("_OcclusionStrength", 0.6f);
            material.SetFloat("_Smoothness", 0.08f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetBrownMudMaterial()
        {
            var material = GetMaterial("RangeBermBrownMud", new Color(0.95f, 0.82f, 0.70f));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BrownMudDiffusePath));
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(BrownMudDiffusePath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BrownMudNormalPath));
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureScale("_MainTex", Vector2.one);
            material.SetTextureScale("_BumpMap", Vector2.one);
            material.SetFloat("_BumpScale", 0.55f);
            material.SetFloat("_Smoothness", 0.03f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetCoastalCliffMaterial()
        {
            var material = GetMaterial("CoastalCliffBackdrop", Color.white);
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(CoastalCliffDiffusePath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(CoastalCliffNormalPath);
            if (diffuse == null || normal == null)
                throw new System.InvalidOperationException("Coastal cliff PBR textures were not imported.");

            material.SetTexture("_BaseMap", diffuse);
            material.SetTexture("_MainTex", diffuse);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.75f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.08f);
            material.SetFloat("_Cull", 0f);
            material.doubleSidedGI = true;
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetFactoryMaterial(string name, string diffusePath, string normalPath,
            Vector2 scale, Vector2 offset, Color tint, float metallic, float smoothness)
        {
            var material = GetMaterial(name, tint);
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            material.SetTexture("_BaseMap", diffuse);
            material.SetTexture("_MainTex", diffuse);
            material.SetTexture("_BumpMap", normal);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureScale("_MainTex", scale);
            material.SetTextureScale("_BumpMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTextureOffset("_MainTex", offset);
            material.SetTextureOffset("_BumpMap", offset);
            material.SetFloat("_BumpScale", 0.7f);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureFactoryTextureImporter(string path, TextureImporterType textureType,
            bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Factory texture was not imported: {path}");

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.flipGreenChannel = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureCoastalCliffImporters()
        {
            var modelImporter = AssetImporter.GetAtPath(CoastalCliffModelPath) as ModelImporter;
            if (modelImporter == null)
                throw new System.InvalidOperationException($"Coastal cliff FBX was not imported: {CoastalCliffModelPath}");

            modelImporter.importAnimation = false;
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.meshCompression = ModelImporterMeshCompression.High;
            modelImporter.isReadable = false;
            modelImporter.optimizeMeshPolygons = true;
            modelImporter.optimizeMeshVertices = true;
            modelImporter.weldVertices = true;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.SaveAndReimport();

            ConfigureCoastalCliffTextureImporter(CoastalCliffDiffusePath, TextureImporterType.Default, true);
            ConfigureCoastalCliffTextureImporter(CoastalCliffNormalPath, TextureImporterType.NormalMap, false);
            ConfigureCoastalCliffTextureImporter(CoastalCliffRoughnessPath, TextureImporterType.Default, false);
        }

        private static void ConfigureCoastalCliffTextureImporter(string path, TextureImporterType textureType,
            bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Coastal cliff texture was not imported: {path}");

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.flipGreenChannel = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 2;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureBrownMudTextureImporter(string path, TextureImporterType textureType,
            bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Brown mud texture was not imported: {path}");

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.flipGreenChannel = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 2;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureConcreteTextureImporter(string path, TextureImporterType textureType,
            bool sRgb, bool flipGreenChannel)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Concrete texture was not imported: {path}");

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.flipGreenChannel = flipGreenChannel;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
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
