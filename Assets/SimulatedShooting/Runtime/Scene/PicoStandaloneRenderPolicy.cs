using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace SimulatedShooting.Scene
{
    // Keep the desktop artwork while budgeting separately for standalone stereo rendering.
    public sealed class PicoStandaloneRenderPolicy : MonoBehaviour
    {
        public const string ProfilePath = "Rendering/PicoStandalone";
        public const float EyeResolutionScale = 1.25f;
        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        XRDisplaySubsystem configuredDisplay;
        int logAfterFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("PicoStandaloneRenderPolicy");
            DontDestroyOnLoad(go);
            go.AddComponent<PicoStandaloneRenderPolicy>();
#endif
        }

        void Awake()
        {
            QualitySettings.renderPipeline = Resources.Load<RenderPipelineAsset>(ProfilePath);
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture.SetGlobalAnisotropicFilteringLimits(4, 8);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 72;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        void SceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.targetTexture != null || camera.stereoTargetEye == StereoTargetEyeMask.None) continue;
                    camera.nearClipPlane = .05f;
                    camera.farClipPlane = 300f;
                    camera.allowMSAA = true;
                    camera.allowDynamicResolution = false;
                }
        }

        void Update()
        {
            if (configuredDisplay == null || !configuredDisplay.running)
            {
                SubsystemManager.GetInstances(displays);
                foreach (var display in displays)
                {
                    if (!display.running) continue;
                    configuredDisplay = display;
                    display.scaleOfAllRenderTargets = EyeResolutionScale;
                    XRSettings.renderViewportScale = 1f;
                    logAfterFrame = Time.frameCount + 3;
                    break;
                }
            }
            if (logAfterFrame < 0 || Time.frameCount < logAfterFrame) return;
            logAfterFrame = -1;
            Debug.Log("[PICO Render] eye=" + XRSettings.eyeTextureWidth + "x" + XRSettings.eyeTextureHeight
                + " scale=" + configuredDisplay.scaleOfAllRenderTargets
                + " pipeline=" + QualitySettings.renderPipeline + " targetHz=72 MSAA=4 clips=.05/300");
        }

        void OnDestroy() => SceneManager.sceneLoaded -= SceneLoaded;
    }
}
