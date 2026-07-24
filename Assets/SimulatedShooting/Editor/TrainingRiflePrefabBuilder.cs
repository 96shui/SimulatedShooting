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
        const string QbzRoot = "Assets/SimulatedShooting/Prefabs/Weapons/QBC-191";
        const string QbzModelPath = QbzRoot + "/source/QBZ-191.obj";
        const string QbzTextureFolder = QbzRoot + "/textures";
        const string QbzMaterialFolder = QbzRoot + "/materials";
        const string QbzBodyMaterialPath = QbzMaterialFolder + "/QBZ191_Body_URP.mat";
        const string QbzMagazineMaterialPath = QbzMaterialFolder + "/QBZ191_Magazine_URP.mat";
        static readonly Vector3 QbzRearGripPosition = new Vector3(0.006f, -0.10f, -0.135f);
        static readonly Vector3 QbzFrontGripPosition = new Vector3(0.006f, -0.015f, 0.18f);
        static readonly Vector3 QbzMagazinePosition = new Vector3(-0.008f, -0.136f, 0.042f);

        [InitializeOnLoadMethod]
        static void QueueQbzVisualUpgrade()
        {
            EditorApplication.delayCall += UpgradeLegacyVisualIfNeeded;
        }

        static void UpgradeLegacyVisualIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
                EditorApplication.playModeStateChanged += HandlePlayModeChanged;
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(QbzModelPath);
            if (prefab == null || model == null || !NeedsQbzPrefabUpgrade(prefab))
            {
                return;
            }

            EnsurePrefab(rebuild: true);
            Debug.Log("[TrainingRiflePrefabBuilder] Replaced the legacy blockout visual with the licensed QBZ-191 model.");
        }

        static bool NeedsQbzPrefabUpgrade(GameObject prefab)
        {
            var model = prefab.transform.Find("WeaponRoot_training-rifle/RecoilRoot_training-rifle/Model_QBZ191");
            var rearGrip = prefab.transform.Find("WeaponRoot_training-rifle/Grip_training-rifle_RearHand");
            var frontGrip = prefab.transform.Find("WeaponRoot_training-rifle/Grip_training-rifle_FrontHand");
            var magazine = prefab.transform.Find(
                "WeaponRoot_training-rifle/RecoilRoot_training-rifle/Magazine_training-rifle");
            return model == null || rearGrip == null || frontGrip == null || magazine == null ||
                   !HasQbzMagazineMaterial(model) ||
                   Vector3.Distance(rearGrip.localPosition, QbzRearGripPosition) > 0.0001f ||
                   Vector3.Distance(frontGrip.localPosition, QbzFrontGripPosition) > 0.0001f ||
                   Vector3.Distance(magazine.localPosition, QbzMagazinePosition) > 0.0001f;
        }

        static bool HasQbzMagazineMaterial(Transform model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null && material.name.Contains("QBZ191_Magazine"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.delayCall += UpgradeLegacyVisualIfNeeded;
        }

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
            EnsureFolder(QbzMaterialFolder);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null && !rebuild &&
                prefab.GetComponent<WeaponPrefabBinding>()?.HasRequiredBinding == true &&
                prefab.GetComponent<TrainingRifleGrabInteractable>() != null)
            {
                return prefab;
            }

            ConfigureQbzImportSettings();
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
            recoilRoot.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.RecoilRoot";
            CreateQbzVisual(recoilRoot);

            // Grip anchors remain outside RecoilRoot so visual recoil never pulls the tracked hands.
            var rearGrip = CreateAnchor("Grip_training-rifle_RearHand", visualRoot, QbzRearGripPosition);
            rearGrip.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Grip.RearHand";
            var frontGrip = CreateAnchor("Grip_training-rifle_FrontHand", visualRoot, QbzFrontGripPosition);
            frontGrip.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Grip.FrontHand";
            var muzzle = CreateAnchor("Muzzle_training-rifle", recoilRoot, new Vector3(0.006f, -0.004f, 0.451f));
            muzzle.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Muzzle";
            var aimLine = CreateAnchor("AimLine_training-rifle", recoilRoot, new Vector3(0.006f, 0.047f, -0.118f));
            aimLine.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.AimLine";
            var magazine = CreateAnchor("Magazine_training-rifle", recoilRoot, QbzMagazinePosition);
            var leftShoulder = CreateAnchor("Shoulder_training-rifle_Left", visualRoot, new Vector3(-0.18f, -0.02f, -0.30f));
            leftShoulder.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Shoulder.Left";
            var rightShoulder = CreateAnchor("Shoulder_training-rifle_Right", visualRoot, new Vector3(0.18f, -0.02f, -0.30f));
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

        static void ConfigureQbzImportSettings()
        {
            ConfigureTexture(QbzTextureFolder + "/QBZ_DefaultMaterial_BaseColor.png", TextureImporterType.Default, true);
            ConfigureTexture(QbzTextureFolder + "/QBZ_DefaultMaterial_Normal.png", TextureImporterType.NormalMap, false);
            ConfigureTexture(QbzTextureFolder + "/QBZ191_Body_MetallicSmoothness.png", TextureImporterType.Default, false);
            ConfigureTexture(QbzTextureFolder + "/Magazine_Material.001_BaseColor.png", TextureImporterType.Default, true);
            ConfigureTexture(QbzTextureFolder + "/Magazine_Material.001_Normal.png", TextureImporterType.NormalMap, false);
            ConfigureTexture(QbzTextureFolder + "/QBZ191_Magazine_MetallicSmoothness.png", TextureImporterType.Default, false);

            var importer = AssetImporter.GetAtPath(QbzModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException($"QBZ-191 model could not be imported from {QbzModelPath}");
            }

            var changed = importer.globalScale != 1f ||
                          importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription ||
                          importer.isReadable ||
                          importer.meshCompression != ModelImporterMeshCompression.Medium ||
                          importer.importNormals != ModelImporterNormals.Import ||
                          importer.importTangents != ModelImporterTangents.CalculateMikk;
            if (!changed)
            {
                return;
            }

            importer.globalScale = 1f;
            // Keep the OBJ usemtl boundaries so the magazine remains an independent
            // submesh. The builder replaces the imported slots with project URP materials.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        static void ConfigureTexture(string path, TextureImporterType type, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException($"QBZ-191 texture could not be imported from {path}");
            }

            var changed = importer.textureType != type || importer.sRGBTexture != sRgb ||
                          importer.maxTextureSize != 2048 ||
                          importer.textureCompression != TextureImporterCompression.CompressedHQ;
            if (!changed)
            {
                return;
            }

            importer.textureType = type;
            importer.sRGBTexture = sRgb;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        static void CreateQbzVisual(Transform recoilRoot)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(QbzModelPath);
            if (modelAsset == null)
            {
                throw new System.InvalidOperationException($"QBZ-191 model is missing at {QbzModelPath}");
            }

            var model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (model == null)
            {
                throw new System.InvalidOperationException("QBZ-191 model could not be instantiated");
            }

            model.name = "Model_QBZ191";
            model.transform.SetParent(recoilRoot, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            model.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Visual.QBZ191";

            var bodyMaterial = GetQbzMaterial(
                QbzBodyMaterialPath,
                "QBZ191_Body_URP",
                QbzTextureFolder + "/QBZ_DefaultMaterial_BaseColor.png",
                QbzTextureFolder + "/QBZ_DefaultMaterial_Normal.png",
                QbzTextureFolder + "/QBZ191_Body_MetallicSmoothness.png");
            var magazineMaterial = GetQbzMaterial(
                QbzMagazineMaterialPath,
                "QBZ191_Magazine_URP",
                QbzTextureFolder + "/Magazine_Material.001_BaseColor.png",
                QbzTextureFolder + "/Magazine_Material.001_Normal.png",
                QbzTextureFolder + "/QBZ191_Magazine_MetallicSmoothness.png");

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var sourceSlots = renderer.sharedMaterials;
                var slotCount = Mathf.Max(1, sourceSlots.Length);
                var materials = new Material[slotCount];
                for (var index = 0; index < slotCount; index++)
                {
                    var sourceName = index < sourceSlots.Length && sourceSlots[index] != null
                        ? sourceSlots[index].name
                        : string.Empty;
                    materials[index] = IsQbzMagazineSubmesh(renderer, index) ||
                                       index == 1 || sourceName.Contains("Material.001") ||
                                       renderer.name.Contains("Magazine")
                        ? magazineMaterial
                        : bodyMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        static bool IsQbzMagazineSubmesh(Renderer renderer, int materialIndex)
        {
            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null ||
                materialIndex < 0 || materialIndex >= meshFilter.sharedMesh.subMeshCount)
            {
                return false;
            }

            // Fall back to the stable local-space envelope if material names or slot order
            // change in a future Unity/importer version.
            var bounds = meshFilter.sharedMesh.GetSubMesh(materialIndex).bounds;
            return bounds.size.x < 0.05f &&
                   bounds.size.y > 0.17f && bounds.size.y < 0.21f &&
                   bounds.size.z > 0.10f && bounds.size.z < 0.14f &&
                   Mathf.Abs(bounds.center.x + 0.0075f) < 0.015f &&
                   Mathf.Abs(bounds.center.z - 0.042f) < 0.025f;
        }

        static Material GetQbzMaterial(string materialPath, string materialName, string baseMapPath,
            string normalMapPath, string metallicSmoothnessPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.SetTexture("_BaseMap", LoadTexture(baseMapPath));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", LoadTexture(normalMapPath));
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", LoadTexture(metallicSmoothnessPath));
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D LoadTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new System.InvalidOperationException($"QBZ-191 texture is missing at {path}");
            }

            return texture;
        }

        static void AddWeaponColliders(GameObject root)
        {
            var receiver = root.AddComponent<BoxCollider>();
            receiver.center = new Vector3(0f, -0.03f, -0.04f);
            receiver.size = new Vector3(0.09f, 0.16f, 0.34f);

            var stock = root.AddComponent<BoxCollider>();
            stock.center = new Vector3(0f, -0.03f, -0.24f);
            stock.size = new Vector3(0.09f, 0.15f, 0.20f);

            var handguard = root.AddComponent<BoxCollider>();
            handguard.center = new Vector3(0f, 0f, 0.27f);
            handguard.size = new Vector3(0.09f, 0.12f, 0.38f);
        }

        static GameObject CreatePickupPrompt(Transform parent)
        {
            var prompt = new GameObject("Prompt_training-rifle_Pickup");
            prompt.transform.SetParent(parent, false);
            prompt.transform.localPosition = new Vector3(0f, 0.18f, -0.04f);
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
