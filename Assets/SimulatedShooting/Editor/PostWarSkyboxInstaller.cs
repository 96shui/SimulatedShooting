using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SimulatedShooting.Editor
{
    public static class PostWarSkyboxInstaller
    {
        private const string ScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        private const string TexturePath =
            "Assets/SimulatedShooting/Art/Skyboxes/PostWarRuins_360.png";
        private const string MaterialPath =
            "Assets/SimulatedShooting/Art/Materials/RangeSkybox.mat";
        private const string PreviewPath =
            "docs/codex-reports/evidence/post-war-skybox-scene-preview.png";

        [MenuItem("Tools/Simulated Shooting/Install Post-War Ruins Skybox")]
        public static void InstallAndOpen()
        {
            var material = GetOrCreateMaterial();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyEnvironment(material);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            DynamicGI.UpdateEnvironment();
            Selection.activeObject = material;

            Debug.Log("[PostWarSkyboxInstaller] Installed the PICO panoramic skybox and opened ZeroingRangeScene.");
        }

        [MenuItem("Tools/Simulated Shooting/Capture Post-War Skybox Preview")]
        public static void CapturePreview()
        {
            var material = GetOrCreateMaterial();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyEnvironment(material);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            var cameraObject = GameObject.Find("Camera_NoVR");
            var camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            if (camera == null)
                throw new InvalidOperationException("Camera_NoVR was not found in ZeroingRangeScene.");

            const int width = 1920;
            const int height = 1080;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            try
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                frame.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                frame.Apply();

                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                    throw new InvalidOperationException("Unity project root could not be resolved.");

                var outputPath = Path.Combine(projectRoot, PreviewPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? projectRoot);
                File.WriteAllBytes(outputPath, frame.EncodeToPNG());
                Debug.Log($"[PostWarSkyboxInstaller] Captured scene preview: {outputPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        internal static Material GetOrCreateMaterial()
        {
            ConfigureTextureImporter();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null)
                throw new InvalidOperationException($"Skybox texture was not imported: {TexturePath}");

            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
                throw new InvalidOperationException("Unity shader 'Skybox/Panoramic' is unavailable.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "RangeSkybox" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", texture);
            material.SetColor("_Tint", new Color(0.75f, 0.77f, 0.80f, 1f));
            material.SetFloat("_Exposure", 0.65f);
            material.SetFloat("_Rotation", 0f);
            if (material.HasProperty("_Mapping"))
                material.SetFloat("_Mapping", 1f);
            if (material.HasProperty("_ImageType"))
                material.SetFloat("_ImageType", 0f);
            if (material.HasProperty("_MirrorOnBack"))
                material.SetFloat("_MirrorOnBack", 0f);

            EditorUtility.SetDirty(material);
            return material;
        }

        internal static void ApplyEnvironment(Material material)
        {
            RenderSettings.skybox = material;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.40f, 0.43f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.33f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.14f, 0.12f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.7f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.38f, 0.41f, 0.42f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 230f;
        }

        private static void ConfigureTextureImporter()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Texture importer was not found: {TexturePath}");

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 1;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone",
                overridden = true,
                maxTextureSize = 4096,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Uncompressed,
                compressionQuality = 100
            });
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 4096,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 100
            });
            importer.SaveAndReimport();
        }
    }
}
