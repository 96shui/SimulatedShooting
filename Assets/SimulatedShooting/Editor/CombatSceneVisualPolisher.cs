using System;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimulatedShooting.Editor
{
    public static class CombatSceneVisualPolisher
    {
        const string ScenePath = "Assets/Scenes/CombatScene.unity";
        const string Art = "Assets/SimulatedShooting/Art";
        const string SoldierModel = Art + "/Characters/CC0LowPolySoldier/LowpolySoldier.fbx";
        const string SoldierTexture = Art + "/Characters/CC0LowPolySoldier/LowpolySoldier_Texture.png";
        const string PolishRootName = "VisualPolish_Realism";

        [MenuItem("Tools/Simulated Shooting/Scene 3/Apply Realism Polish")]
        public static void Apply()
        {
            ConfigureSoldierImport();
            var enemyMaterial = SoldierMaterial("EnemySoldier", new Color(.62f, .64f, .55f));
            var teammateMaterial = SoldierMaterial("TeammateSoldier", new Color(.48f, .58f, .46f));
            UpgradeActorPrefab("Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab", enemyMaterial, SolidMaterial("EnemyArmband", new Color(.62f, .12f, .1f)));
            UpgradeActorPrefab("Assets/SimulatedShooting/Prefabs/Combat/Actor_Teammate.prefab", teammateMaterial, SolidMaterial("TeammateArmband", new Color(.12f, .35f, .75f)));

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bindings = UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            if (bindings == null || bindings.GeometryRoot == null)
                throw new InvalidOperationException("CombatScene bindings or geometry root is missing.");

            FixCoplanarSurfaces(bindings.GeometryRoot);
            ApplyScannedMaterials(bindings.GeometryRoot);
            RebuildVisualDetails(bindings.GeometryRoot);
            ApplyAtmosphere();
            CombatSceneBuilder.RebuildNavigation();
            EditorSceneManager.MarkSceneDirty(bindings.gameObject.scene);
            EditorSceneManager.SaveScene(bindings.gameObject.scene);
            AssetDatabase.SaveAssets();
            CombatSceneBuilder.Validate();
            CombatSceneBuilder.Capture();
            CaptureEnemyPreview();
            Debug.Log("CombatScene realism polish applied without rebuilding the saved scene.");
        }

        static void ConfigureSoldierImport()
        {
            AssetDatabase.ImportAsset(SoldierModel, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(SoldierModel) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Soldier FBX did not import.");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        static Material SoldierMaterial(string name, Color tint)
        {
            var folder = Art + "/Characters/CC0LowPolySoldier";
            var path = folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SoldierTexture));
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", .18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material SolidMaterial(string name, Color color)
        {
            var path = Art + "/Characters/CC0LowPolySoldier/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void UpgradeActorPrefab(string path, Material material, Material armbandMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = root.GetComponent<CombatActorView>();
                foreach (var old in view.VisualRoot.GetComponentsInChildren<Transform>(true)
                             .Where(t => t.name == "CC0_SoldierModel" || t.name == "Model_QBZ191_Enemy" || t.name == "TrainingForceArmband")
                             .ToArray())
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (var renderer in view.VisualRoot.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = renderer.transform.IsChildOf(view.Muzzle) && renderer.gameObject == view.MuzzleFlash;

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierModel);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, view.VisualRoot);
                model.name = "CC0_SoldierModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;

                var bounds = CombinedBounds(model);
                var scale = 1.78f / Mathf.Max(.01f, bounds.size.y);
                model.transform.localScale = Vector3.one * scale;
                bounds = CombinedBounds(model);
                model.transform.position += Vector3.up * -bounds.min.y;
                PoseSoldier(view.VisualRoot, model.transform);
                AddRifleVisual(view.VisualRoot);
                AddArmband(view.VisualRoot, armbandMaterial);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Bounds CombinedBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static void PoseSoldier(Transform visualRoot, Transform model)
        {
            var leftArm = Find(model, "LeftArm");
            var leftForeArm = Find(model, "LeftForeArm");
            var leftHand = Find(model, "LeftHand");
            var rightArm = Find(model, "RightArm");
            var rightForeArm = Find(model, "RightForeArm");
            var rightHand = Find(model, "RightHand");
            AimBone(leftArm, leftForeArm, visualRoot.TransformPoint(new Vector3(-.3f, 1.34f, .12f)));
            AimBone(leftForeArm, leftHand, visualRoot.TransformPoint(new Vector3(-.07f, 1.2f, .38f)));
            AimBone(rightArm, rightForeArm, visualRoot.TransformPoint(new Vector3(.3f, 1.3f, .08f)));
            AimBone(rightForeArm, rightHand, visualRoot.TransformPoint(new Vector3(.1f, 1.17f, .08f)));
        }

        static void AddRifleVisual(Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art.Replace("/Art", "/Prefabs/Weapons/QBC-191/source/QBZ-191.obj"));
            var rifle = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            rifle.name = "Model_QBZ191_Enemy";
            rifle.transform.localPosition = new Vector3(.1f, 1.16f, .08f);
            rifle.transform.localRotation = Quaternion.identity;
            rifle.transform.localScale = Vector3.one;
            var materials = new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>(Art.Replace("/Art", "/Prefabs/Weapons/QBC-191/materials/QBZ191_Body_URP.mat")),
                AssetDatabase.LoadAssetAtPath<Material>(Art.Replace("/Art", "/Prefabs/Weapons/QBC-191/materials/QBZ191_Magazine_URP.mat"))
            };
            foreach (var renderer in rifle.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = materials;
        }

        static Transform Find(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        }

        static void AimBone(Transform bone, Transform child, Vector3 target)
        {
            bone.rotation = Quaternion.FromToRotation(child.position - bone.position, target - bone.position) * bone.rotation;
        }

        static void AddArmband(Transform parent, Material material)
        {
            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = "TrainingForceArmband";
            band.transform.SetParent(parent, false);
            band.transform.localPosition = new Vector3(.28f, 1.35f, 0);
            band.transform.localRotation = Quaternion.Euler(0, 0, 90);
            band.transform.localScale = new Vector3(.075f, .11f, .075f);
            UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());
            band.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void CaptureEnemyPreview()
        {
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab"));
            actor.transform.position = new Vector3(220, 0, 220);
            actor.transform.rotation = Quaternion.identity;
            SetLayer(actor.transform, 31);
            var cameraObject = new GameObject("EnemyPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = actor.transform.position + new Vector3(2.4f, 1.35f, 3.2f);
            camera.transform.LookAt(actor.transform.position + Vector3.up * .95f);
            camera.fieldOfView = 34;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.2f, .22f, .2f);
            camera.cullingMask = 1 << 31;
            var light = new GameObject("EnemyPreviewLight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << 31;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(35, -45, 0);
            var texture = new RenderTexture(720, 900, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(720, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 720, 900), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Logs/Scene3");
            File.WriteAllBytes("Logs/Scene3/enemy.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(light.gameObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(actor);
        }

        static void SetLayer(Transform root, int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        }

        static void FixCoplanarSurfaces(Transform geometry)
        {
            // Keep a visible 8 cm curb while preserving the validator's 10 cm actor-clearance margin.
            SetY(geometry, "Sidewalk", .03f);
            SetY(geometry, "Floor_1", -.125f);
        }

        static void SetY(Transform root, string name, float y)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name))
            {
                var position = child.position;
                position.y = y;
                child.position = position;
            }
        }

        static void ApplyScannedMaterials(Transform geometry)
        {
            var brick = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/FactoryBrick.mat");
            var windows = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/FactoryWindow.mat");
            var doors = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/FactoryDoor.mat");
            var earth = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Combat/Earth.mat");
            var normal = AssetDatabase.LoadAssetAtPath<Texture>(Art + "/Textures/BrownMud/brown_mud_2k.blend/textures/brown_mud_nor_gl_2k.exr");
            earth.SetTexture("_BumpMap", normal);
            earth.EnableKeyword("_NORMALMAP");
            earth.SetFloat("_BumpScale", .7f);
            earth.SetFloat("_Smoothness", .08f);
            EditorUtility.SetDirty(earth);

            var wallNames = new[] { "RearWall", "EastWall", "FrontWall", "FrontHeader", "WestWall", "WestEndWall", "DoorWallLower", "DoorWallUpper", "DoorLintel", "RoomDivider" };
            foreach (var renderer in geometry.GetComponentsInChildren<Renderer>(true))
            {
                if (wallNames.Contains(renderer.name)) renderer.sharedMaterial = brick;
                else if (renderer.name == "WindowRecess") renderer.sharedMaterial = windows;
                else if (renderer.name == "DoorLeaf") renderer.sharedMaterial = doors;
            }
        }

        static void RebuildVisualDetails(Transform geometry)
        {
            var old = geometry.Find(PolishRootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(PolishRootName).transform;
            root.SetParent(geometry, false);
            var sand = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Combat/Sandbags.mat");
            var concrete = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Combat/Concrete.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Combat/Steel.mat");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Combat/Timber.mat");

            foreach (var box in geometry.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Sandbag" && t.parent != root))
                if (box.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
            var trenchLines = new[]
            {
                (new Vector3(-2.15f,2.42f,2), new Vector3(0,0,1), 9),
                (new Vector3(2.15f,2.42f,8), new Vector3(0,0,1), 5),
                (new Vector3(12,2.42f,34.15f), new Vector3(1,0,0), 7),
                (new Vector3(26.15f,2.42f,38), new Vector3(0,0,1), 7)
            };
            foreach (var line in trenchLines)
                for (var i = 0; i < line.Item3; i++) Sack(root, line.Item1 + line.Item2 * (i * 1.1f), line.Item2.x != 0 ? 90 : 0, sand);

            for (var i = 0; i < 5; i++)
            {
                Box(root, "RoofParapet", new Vector3(17 + i * 5.5f, 11.25f, 64), new Vector3(5.2f, .65f, .3f), brick: AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/FactoryBrick.mat"));
                Box(root, "Rubble", new Vector3(5 + i * 2.1f, .16f, 61), new Vector3(.65f + i * .08f, .25f, .5f), concrete, new Vector3(i * 8, i * 21, i * 5));
            }
            for (var i = 0; i < 3; i++)
            {
                Cylinder(root, "RoadsideBarrel", new Vector3(45 + i * .72f, .48f, 48.2f + (i % 2) * .6f), new Vector3(.48f, .48f, .48f), steel);
                Box(root, "TimberPallet", new Vector3(39, .13f + i * .13f, 61.6f), new Vector3(1.8f, .1f, .16f), wood, new Vector3(0, i * 7, 0));
            }
            StreetLamp(root, new Vector3(-1, 0, 61), steel);
            StreetLamp(root, new Vector3(49, 0, 61), steel);
        }

        static void Sack(Transform parent, Vector3 position, float yaw, Material material)
        {
            var sack = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sack.name = "RoundedSandbag";
            sack.transform.SetParent(parent, false);
            sack.transform.position = position;
            sack.transform.rotation = Quaternion.Euler(0, yaw, 0);
            sack.transform.localScale = new Vector3(.62f, .22f, .38f);
            sack.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(sack.GetComponent<Collider>());
        }

        static void StreetLamp(Transform parent, Vector3 position, Material material)
        {
            Box(parent, "StreetLampPole", position + Vector3.up * 2.6f, new Vector3(.12f, 5.2f, .12f), material);
            Box(parent, "StreetLampArm", position + new Vector3(.55f, 5.1f, 0), new Vector3(1.1f, .1f, .1f), material);
            var light = new GameObject("StreetLampLight").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position + new Vector3(1.05f, 4.95f, 0);
            light.type = LightType.Point;
            light.range = 8;
            light.intensity = 1.4f;
            light.color = new Color(1f, .78f, .52f);
            light.shadows = LightShadows.Soft;
        }

        static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material brick, Vector3? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
            go.GetComponent<Renderer>().sharedMaterial = brick;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static void Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static void ApplyAtmosphere()
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/RangeSkybox.mat");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.56f, .64f, .72f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .35f, .32f);
            RenderSettings.ambientGroundColor = new Color(.16f, .14f, .11f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.49f, .52f, .53f);
            RenderSettings.fogStartDistance = 105;
            RenderSettings.fogEndDistance = 235;
            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.intensity = 1.05f;
                RenderSettings.sun.color = new Color(1f, .88f, .72f);
                RenderSettings.sun.shadows = LightShadows.Soft;
            }
            DynamicGI.UpdateEnvironment();
        }
    }
}
