using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimulatedShooting.Editor
{
    public static class MovingTargetRangeSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MovingTargetRangeScene.unity";
        const string MaterialFolder = "Assets/SimulatedShooting/Art/Materials";
        const string XrOriginPrefabPath =
            "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";


        [MenuItem("Tools/Simulated Shooting/Build Moving Target Range Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/SimulatedShooting/Art");
            EnsureFolder(MaterialFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MovingTargetRange").transform;
            AddTestId(root.gameObject, "MovingTargetRange.Root");

            var ground = GetMaterial("MovingTargetGround", new Color(0.20f, 0.22f, 0.14f), 0.02f);
            var concrete = GetMaterial("MovingTargetConcrete", new Color(0.31f, 0.32f, 0.29f), 0.04f);
            var earth = GetMaterial("MovingTargetEarth", new Color(0.28f, 0.21f, 0.13f), 0.01f);
            var route = GetMaterial("MovingTargetRoute", new Color(0.13f, 0.14f, 0.13f), 0.12f);
            var marker = GetMaterial("MovingTargetMarker", new Color(0.87f, 0.84f, 0.65f), 0.05f);
            var target = GetMaterial("MovingTargetSilhouette", new Color(0.035f, 0.04f, 0.035f), 0.08f);

            CreateEnvironment(root, ground, concrete, earth, route, marker);
            CreateRouteAndTarget(root, route, marker, target);
            CreatePlayerAnchors(root, concrete);
            CreateLighting(root);
            MarkRenderersStatic(root.Find("Environment"));
            MarkRenderersStatic(root.Find("MovingTargetRoute_40m"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[MovingTargetRangeSceneBuilder] Built MovingTargetRangeScene with a 100m range and 40m route.");
        }

        [MenuItem("Tools/Simulated Shooting/Open Moving Target Range Scene")]
        public static void OpenScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void CreateEnvironment(Transform root, Material ground, Material concrete, Material earth,
            Material route, Material marker)
        {
            var environment = CreateAnchor("Environment", root, Vector3.zero);
            AddTestId(environment.gameObject, "MovingTargetRange.Environment.Root");
            CreateCube("Ground", environment, new Vector3(0f, -0.2f, 58f), new Vector3(76f, 0.4f, 136f), ground, true);
            CreateCube("FiringPad", environment, new Vector3(0f, 0.02f, 0f), new Vector3(12f, 0.15f, 5f), concrete, true);
            CreateCube("SafetyLane", environment, new Vector3(0f, -0.01f, 52f), new Vector3(12f, 0.08f, 104f), concrete, false);
            CreateCube("TargetTrackBed", environment, new Vector3(0f, 0.03f, 100f), new Vector3(44f, 0.12f, 1.1f), route, false);
            CreateCube("TargetBackstop", environment, new Vector3(0f, 3f, 111f), new Vector3(62f, 6f, 3f), earth, true);
            CreateCube("BackstopTop", environment, new Vector3(0f, 6.2f, 112f), new Vector3(68f, 1.2f, 5f), earth, false);

            for (var distance = 20; distance <= 80; distance += 20)
            {
                CreateCube($"DistanceMark_{distance}m", environment, new Vector3(0f, 0.08f, distance),
                    new Vector3(11.5f, 0.025f, 0.08f), marker, false);
            }

            var day = CreateAnchor("Lighting_Day", environment, Vector3.zero);
            AddTestId(day.gameObject, "MovingTargetRange.Lighting.Day");
            var night = CreateAnchor("Lighting_Night", environment, Vector3.zero);
            AddTestId(night.gameObject, "MovingTargetRange.Lighting.Night");
            night.gameObject.SetActive(false);
        }

        static void CreateRouteAndTarget(Transform root, Material routeMaterial, Material markerMaterial,
            Material targetMaterial)
        {
            var route = CreateAnchor("MovingTargetRoute_40m", root, new Vector3(0f, 0f, 100f));
            AddTestId(route.gameObject, "MovingTargetRange.Route.Root");
            CreateCube("RouteRail", route, new Vector3(0f, 0.22f, 0f), new Vector3(40f, 0.08f, 0.12f), routeMaterial, false);

            var right = CreateAnchor("Anchor_MovingTarget_Right", route, new Vector3(20f, 0f, 0f));
            AddTestId(right.gameObject, "MovingTargetRange.Route.RightEndpoint");
            CreateEndpointMarker(right, "Right", markerMaterial);

            var left = CreateAnchor("Anchor_MovingTarget_Left", route, new Vector3(-20f, 0f, 0f));
            AddTestId(left.gameObject, "MovingTargetRange.Route.LeftEndpoint");
            CreateEndpointMarker(left, "Left", markerMaterial);

            var target = CreateAnchor("Target_Moving_SideProfile", root, right.position);
            AddTestId(target.gameObject, "MovingTargetRange.Target");
            CreateRunnerSilhouette(target, targetMaterial);

            var hitSurfaceObject = new GameObject("TargetHitSurface");
            hitSurfaceObject.transform.SetParent(target, false);
            hitSurfaceObject.transform.localPosition = new Vector3(0f, 1.05f, -0.02f);
            var hitSurface = hitSurfaceObject.AddComponent<BoxCollider>();
            hitSurface.size = new Vector3(0.9f, 1.9f, 0.08f);
            AddTestId(hitSurfaceObject, "MovingTargetRange.Target.HitSurface");

            var center = CreateAnchor("TargetCenter", target, new Vector3(0f, 1.05f, -0.07f));
            AddTestId(center.gameObject, "MovingTargetRange.Target.Center");
            var feedback = CreateAnchor("ImpactFeedback", target, Vector3.zero);
            AddTestId(feedback.gameObject, "MovingTargetRange.Target.ImpactFeedback");

            var bindingObject = new GameObject("MovingTargetRouteBinding");
            bindingObject.transform.SetParent(route, false);
            AddTestId(bindingObject, "MovingTargetRange.Target.Binding");
            var binding = bindingObject.AddComponent<MovingTargetRouteBinding>();
            binding.Configure(right, left, target, hitSurface, center, feedback);
        }

        static void CreateEndpointMarker(Transform endpoint, string suffix, Material material)
        {
            CreateCube($"EndpointPost_{suffix}", endpoint, new Vector3(0f, 1.25f, 0.45f),
                new Vector3(0.10f, 2.5f, 0.10f), material, false);
            CreateCube($"EndpointFlag_{suffix}", endpoint, new Vector3(suffix == "Right" ? -0.35f : 0.35f, 2.05f, 0.45f),
                new Vector3(0.7f, 0.42f, 0.04f), material, false);
        }

        static void CreateRunnerSilhouette(Transform parent, Material material)
        {
            CreateCapsule("Runner_Torso", parent, new Vector3(0f, 1.25f, 0f), new Vector3(0.24f, 0.42f, 0.11f),
                material, Quaternion.Euler(0f, 0f, -12f));
            CreateSphere("Runner_Head", parent, new Vector3(-0.10f, 1.82f, 0f), new Vector3(0.20f, 0.20f, 0.12f), material);
            CreateCapsule("Runner_ArmForward", parent, new Vector3(-0.30f, 1.32f, 0f), new Vector3(0.10f, 0.34f, 0.08f),
                material, Quaternion.Euler(0f, 0f, 62f));
            CreateCapsule("Runner_ArmRear", parent, new Vector3(0.28f, 1.25f, 0f), new Vector3(0.10f, 0.32f, 0.08f),
                material, Quaternion.Euler(0f, 0f, -58f));
            CreateCapsule("Runner_LegForward", parent, new Vector3(-0.22f, 0.62f, 0f), new Vector3(0.13f, 0.48f, 0.10f),
                material, Quaternion.Euler(0f, 0f, -34f));
            CreateCapsule("Runner_LegRear", parent, new Vector3(0.25f, 0.58f, 0f), new Vector3(0.13f, 0.48f, 0.10f),
                material, Quaternion.Euler(0f, 0f, 38f));
        }

        static void CreatePlayerAnchors(Transform root, Material concrete)
        {
            var anchors = CreateAnchor("TrainingAnchors", root, Vector3.zero);
            var shootingPosition = CreateAnchor("ShootingPosition", anchors, new Vector3(0f, 1.5f, 0f));
            AddTestId(shootingPosition.gameObject, "MovingTargetRange.ShootingPosition");
            var playerSpawn = CreateAnchor("PlayerSpawn", anchors, Vector3.zero);
            AddTestId(playerSpawn.gameObject, "MovingTargetRange.PlayerSpawn");
            var hudAnchor = CreateAnchor("HudAnchor", anchors, new Vector3(0f, 1.45f, 1.4f));
            AddTestId(hudAnchor.gameObject, "MovingTargetRange.Hud.Anchor");

            var weaponHooks = CreateAnchor("WeaponIntegrationAnchors", anchors, Vector3.zero);
            var muzzle = CreateAnchor("MuzzleHook", weaponHooks, new Vector3(0.35f, 1.2f, 0.7f));
            AddTestId(muzzle.gameObject, "MovingTargetRange.Weapon.Muzzle");
            var hitRay = CreateAnchor("HitRayHook", weaponHooks, muzzle.localPosition + Vector3.forward);
            AddTestId(hitRay.gameObject, "MovingTargetRange.Weapon.HitRay");
            var lowLight = CreateAnchor("LowLightOpticHook", weaponHooks, new Vector3(0f, 1.5f, 0.5f));
            AddTestId(lowLight.gameObject, "MovingTargetRange.Optic.LowLight");

            var cameraObject = new GameObject("Camera_NoVR", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(anchors, false);
            cameraObject.transform.position = shootingPosition.position;
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.05f, 100f) - shootingPosition.position);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 54f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 250f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            AddTestId(cameraObject, "MovingTargetRange.Camera.NoVR");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
            var xrOrigin = prefab == null
                ? new GameObject("XR Origin (VR)")
                : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            xrOrigin.name = "XR Origin (VR)";
            xrOrigin.transform.SetParent(anchors, false);
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
            P1XrFloorOriginUpgrader.ConfigureFloorOrigin(xrOrigin);
            AddTestId(xrOrigin, "MovingTargetRange.Origin.VR");

            var modeController = anchors.gameObject.AddComponent<ZeroingRangeXRModeController>();
            modeController.Configure(xrOrigin, camera);

            CreateCube("ShootingPositionMarker", anchors, new Vector3(0f, 0.08f, 0f),
                new Vector3(1.4f, 0.03f, 1.0f), concrete, false);
        }

        static void CreateLighting(Transform root)
        {
            var lighting = CreateAnchor("Lighting", root, Vector3.zero);
            var sunObject = new GameObject("Sun_Day", typeof(Light));
            sunObject.transform.SetParent(lighting, false);
            sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.shadows = LightShadows.Soft;
            AddTestId(sunObject, "MovingTargetRange.Lighting.Sun");

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.56f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.39f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.60f, 0.61f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 220f;
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, position, scale, material,
                Quaternion.identity, keepCollider);
        }

        static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material)
        {
            return CreatePrimitive(PrimitiveType.Sphere, name, parent, position, scale, material,
                Quaternion.identity, false);
        }

        static GameObject CreateCapsule(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation)
        {
            return CreatePrimitive(PrimitiveType.Capsule, name, parent, position, scale, material, rotation, false);
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, Quaternion rotation, bool keepCollider)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                Object.DestroyImmediate(instance.GetComponent<Collider>());
            return instance;
        }

        static Transform CreateAnchor(string name, Transform parent, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }

        static void AddTestId(GameObject gameObject, string id)
        {
            gameObject.AddComponent<SceneTestId>().Id = id;
        }

        static Material GetMaterial(string name, Color color, float smoothness)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void MarkRenderersStatic(Transform root)
        {
            var flags = StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.ReflectionProbeStatic;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var separator = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }

        static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(scene => scene.path != ScenePath))
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
