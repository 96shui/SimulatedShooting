using SimulatedShooting.Scene;
using UnityEditor;
using UnityEngine;
using VRShooting.Application.Weapons;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class TrainingRiflePrefabBuilder
    {
        public const string PrefabPath = "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab";

        const string MaterialFolder = "Assets/SimulatedShooting/Art/Materials";

        [MenuItem("Tools/Simulated Shooting/Build Training Rifle Prefab")]
        public static void BuildMenuItem()
        {
            EnsurePrefab(rebuild: true);
        }

        public static GameObject EnsurePrefab(bool rebuild = false)
        {
            EnsureFolder("Assets/SimulatedShooting/Prefabs");
            EnsureFolder("Assets/SimulatedShooting/Prefabs/Weapons");
            EnsureFolder("Assets/SimulatedShooting/Art");
            EnsureFolder(MaterialFolder);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null && !rebuild &&
                prefab.GetComponent<WeaponPrefabBinding>()?.HasRequiredBinding == true &&
                prefab.GetComponent<TrainingRifleGrabInteractable>() != null)
            {
                return prefab;
            }

            var root = BuildPrefabInstance();
            prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        static GameObject BuildPrefabInstance()
        {
            var root = new GameObject("Weapon_training-rifle_Blockout");
            root.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.TrainingRifle";
            var binding = root.AddComponent<WeaponPrefabBinding>();
            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 3.6f;
            rigidbody.drag = 0.08f;
            rigidbody.angularDrag = 0.12f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var visualRoot = CreateAnchor("WeaponRoot_training-rifle", root.transform, Vector3.zero);
            var recoilRoot = CreateAnchor("RecoilRoot_training-rifle", visualRoot, Vector3.zero);
            var dark = GetMaterial("TrainingRifleDarkMetal", new Color(0.035f, 0.045f, 0.04f));
            var polymer = GetMaterial("TrainingRiflePolymer", new Color(0.09f, 0.11f, 0.08f));
            var sight = GetMaterial("TrainingRifleSightPaint", new Color(0.78f, 0.66f, 0.22f));

            CreateCube("Receiver", recoilRoot, new Vector3(0f, -0.03f, 0.50f), new Vector3(0.18f, 0.15f, 0.62f), dark);
            CreateCube("Stock_training-rifle", recoilRoot, new Vector3(0f, -0.04f, -0.02f), new Vector3(0.15f, 0.13f, 0.42f), polymer);
            CreateCube("Handguard", recoilRoot, new Vector3(0f, -0.05f, 0.96f), new Vector3(0.17f, 0.12f, 0.52f), polymer);
            CreateCylinder("Barrel", recoilRoot, new Vector3(0f, 0.045f, 1.42f), new Vector3(0.022f, 0.48f, 0.022f), dark, Quaternion.Euler(90f, 0f, 0f));
            CreateCylinder("MuzzleDevice", recoilRoot, new Vector3(0f, 0.045f, 1.88f), new Vector3(0.035f, 0.08f, 0.035f), dark, Quaternion.Euler(90f, 0f, 0f));
            CreateCube("RearSight_Base", recoilRoot, new Vector3(0f, 0.08f, 0.34f), new Vector3(0.16f, 0.035f, 0.08f), dark);
            CreateCube("RearSight_Notch_Left", recoilRoot, new Vector3(-0.055f, 0.135f, 0.34f), new Vector3(0.025f, 0.08f, 0.03f), dark);
            CreateCube("RearSight_Notch_Right", recoilRoot, new Vector3(0.055f, 0.135f, 0.34f), new Vector3(0.025f, 0.08f, 0.03f), dark);
            CreateCube("FrontSight_Post", recoilRoot, new Vector3(0f, 0.14f, 1.68f), new Vector3(0.025f, 0.16f, 0.028f), sight);
            CreateCube("RearHandGrip_Visual", recoilRoot, new Vector3(0f, -0.23f, 0.26f), new Vector3(0.085f, 0.25f, 0.085f), polymer, Quaternion.Euler(-12f, 0f, 0f));
            CreateCube("FrontHandGrip_Visual", recoilRoot, new Vector3(0f, -0.20f, 0.90f), new Vector3(0.075f, 0.23f, 0.075f), polymer, Quaternion.Euler(8f, 0f, 0f));
            CreateCube("Magazine_training-rifle", recoilRoot, new Vector3(0f, -0.25f, 0.56f), new Vector3(0.12f, 0.30f, 0.09f), polymer, Quaternion.Euler(-8f, 0f, 0f));

            var rearGrip = CreateAnchor("Grip_training-rifle_RearHand", visualRoot, new Vector3(0f, -0.18f, 0.25f));
            rearGrip.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Grip.RearHand";
            var frontGrip = CreateAnchor("Grip_training-rifle_FrontHand", visualRoot, new Vector3(0f, -0.16f, 0.90f));
            frontGrip.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Grip.FrontHand";
            var muzzle = CreateAnchor("Muzzle_training-rifle", recoilRoot, new Vector3(0f, 0.045f, 1.96f));
            muzzle.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Muzzle";
            var aimLine = CreateAnchor("AimLine_training-rifle", recoilRoot, new Vector3(0f, 0.14f, 0.34f));
            aimLine.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.AimLine";
            var magazine = recoilRoot.Find("Magazine_training-rifle");
            var leftShoulder = CreateAnchor("Shoulder_training-rifle_Left", visualRoot, new Vector3(-0.18f, -0.02f, -0.16f));
            leftShoulder.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Shoulder.Left";
            var rightShoulder = CreateAnchor("Shoulder_training-rifle_Right", visualRoot, new Vector3(0.18f, -0.02f, -0.16f));
            rightShoulder.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Shoulder.Right";

            binding.Configure(
                WeaponControlService.TrainingRifleId,
                visualRoot,
                recoilRoot,
                muzzle,
                aimLine,
                rearGrip,
                frontGrip,
                magazine,
                leftShoulder,
                rightShoulder);

            AddWeaponColliders(root);
            var prompt = CreatePickupPrompt(root.transform);
            var grabInteractable = root.AddComponent<TrainingRifleGrabInteractable>();
            grabInteractable.Configure(binding, prompt);
            return root;
        }

        static void AddWeaponColliders(GameObject root)
        {
            var receiver = root.AddComponent<BoxCollider>();
            receiver.center = new Vector3(0f, -0.03f, 0.52f);
            receiver.size = new Vector3(0.20f, 0.19f, 0.68f);

            var stock = root.AddComponent<BoxCollider>();
            stock.center = new Vector3(0f, -0.04f, -0.02f);
            stock.size = new Vector3(0.17f, 0.16f, 0.42f);

            var handguard = root.AddComponent<BoxCollider>();
            handguard.center = new Vector3(0f, -0.04f, 1.13f);
            handguard.size = new Vector3(0.19f, 0.17f, 0.90f);
        }

        static GameObject CreatePickupPrompt(Transform parent)
        {
            var prompt = new GameObject("Prompt_training-rifle_Pickup");
            prompt.transform.SetParent(parent, false);
            prompt.transform.localPosition = new Vector3(0f, 0.30f, 0.28f);
            prompt.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            prompt.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.PickupPrompt";
            var text = prompt.AddComponent<TextMesh>();
            text.text = "GRIP TO PICK UP";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = 0.012f;
            text.color = new Color(1f, 0.88f, 0.22f, 1f);
            prompt.SetActive(false);
            return prompt;
        }

        static Transform CreateAnchor(string name, Transform parent, Vector3 localPosition)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            return anchor;
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material,
            Quaternion? rotation = null)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, localPosition, scale, material, rotation ?? Quaternion.identity);
        }

        static GameObject CreateCylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale,
            Material material, Quaternion rotation)
        {
            return CreatePrimitive(PrimitiveType.Cylinder, name, parent, localPosition, scale, material, rotation);
        }

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 scale, Material material, Quaternion rotation)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = rotation;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            return gameObject;
        }

        static Material GetMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.24f);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var separator = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }
    }
}
