using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRShooting.Unity.Player;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Selects the desktop or HMD view for MainScene and coordinates the UI canvas mode.
    /// </summary>
    [DefaultExecutionOrder(-750)]
    [DisallowMultipleComponent]
    public sealed class MainMenuXRModeController : MonoBehaviour
    {
        const string MainSceneName = "MainScene";

        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

        bool? forcedVrMode;
        bool initialized;
        bool lastVrMode;
        Camera vrCamera;
        MainMenuUI mainMenuUi;

        public bool IsVrMode => initialized && lastVrMode;

        public Camera VrCamera => vrCamera;

        public static MainMenuXRModeController EnsureExists(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            return host.GetComponent<MainMenuXRModeController>() ?? host.AddComponent<MainMenuXRModeController>();
        }

        void Awake()
        {
            RefreshForScene(SceneManager.GetActiveScene());
        }

        void Update()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != MainSceneName)
            {
                return;
            }

            ApplyMode(scene, ResolveVrMode(), false);
        }

        public void RefreshForScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != MainSceneName)
            {
                mainMenuUi = null;
                return;
            }

            ApplyMode(scene, ResolveVrMode(), true);
        }

        public void SetVrModeForTests(bool enabled)
        {
            forcedVrMode = enabled;
            RefreshForScene(SceneManager.GetActiveScene());
        }

        public void ClearForcedModeForTests()
        {
            forcedVrMode = null;
            RefreshForScene(SceneManager.GetActiveScene());
        }

        bool ResolveVrMode()
        {
            if (forcedVrMode.HasValue)
            {
                return forcedVrMode.Value;
            }

            displays.Clear();
            SubsystemManager.GetInstances(displays);
            for (var index = 0; index < displays.Count; index++)
            {
                if (displays[index] != null && displays[index].running)
                {
                    return true;
                }
            }

            return false;
        }

        void ApplyMode(UnityEngine.SceneManagement.Scene scene, bool vrMode, bool force)
        {
            var resolvedVrCamera = vrMode ? ResolveXrCamera(scene) : null;
            var uiChanged = mainMenuUi == null || mainMenuUi.gameObject.scene != scene;
            if (uiChanged)
            {
                mainMenuUi = FindSceneComponent<MainMenuUI>(scene);
            }

            var uiModeChanged = force || !initialized || lastVrMode != vrMode || vrCamera != resolvedVrCamera || uiChanged;

            initialized = true;
            lastVrMode = vrMode;
            vrCamera = resolvedVrCamera;

            if (vrMode && vrCamera != null)
            {
                if (PlayerFollowCamera.Instance != null)
                {
                    PlayerFollowCamera.Instance.SetOutputEnabled(false);
                }

                SetXrOriginActive(scene, true, vrCamera);
                SetSceneCamera(scene, vrCamera);
            }
            else
            {
                var fallback = PlayerFollowCamera.EnsureExists();
                fallback.SetOutputEnabled(true);
                SetSceneCamera(scene, null);
                SetXrOriginActive(scene, false, null);
            }

            if (mainMenuUi == null)
            {
                mainMenuUi = FindSceneComponent<MainMenuUI>(scene);
            }

            if (mainMenuUi != null && uiModeChanged)
            {
                var adapter = mainMenuUi.GetComponent<TrainingUICanvasAdapter>();
                if (adapter != null)
                {
                    adapter.SetMode(vrMode && vrCamera != null, vrCamera);
                }
            }
        }

        static Camera ResolveXrCamera(UnityEngine.SceneManagement.Scene scene)
        {
            var cameras = Resources.FindObjectsOfTypeAll<Camera>();
            Camera xrFallback = null;
            Camera fallback = null;
            for (var index = 0; index < cameras.Length; index++)
            {
                var candidate = cameras[index];
                if (candidate == null || !candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (candidate.GetComponent<PlayerFollowCamera>() != null)
                {
                    continue;
                }

                if (IsUnderXrOrigin(candidate.transform))
                {
                    if (candidate.gameObject.scene == scene)
                    {
                        return candidate;
                    }

                    xrFallback = candidate;
                    continue;
                }

                if (candidate.gameObject.scene == scene)
                {
                    fallback = candidate;
                }
            }

            return xrFallback != null ? xrFallback : fallback;
        }

        static void SetSceneCamera(UnityEngine.SceneManagement.Scene scene, Camera desiredCamera)
        {
            var roots = scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var cameras = roots[rootIndex].GetComponentsInChildren<Camera>(true);
                for (var cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
                {
                    cameras[cameraIndex].enabled = cameras[cameraIndex] == desiredCamera;
                }

                var listeners = roots[rootIndex].GetComponentsInChildren<AudioListener>(true);
                for (var listenerIndex = 0; listenerIndex < listeners.Length; listenerIndex++)
                {
                    listeners[listenerIndex].enabled = desiredCamera != null && listeners[listenerIndex].gameObject == desiredCamera.gameObject;
                }
            }

            if (desiredCamera == null)
            {
                return;
            }

            desiredCamera.enabled = true;
            var desiredListener = desiredCamera.GetComponent<AudioListener>();
            if (desiredListener == null)
            {
                desiredListener = desiredCamera.gameObject.AddComponent<AudioListener>();
            }

            desiredListener.enabled = true;
        }

        static void SetXrOriginActive(UnityEngine.SceneManagement.Scene scene, bool active, Camera resolvedCamera)
        {
            if (resolvedCamera != null)
            {
                var current = resolvedCamera.transform;
                Transform xrRoot = null;
                while (current != null)
                {
                    if (current.name.IndexOf("XR Origin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        xrRoot = current;
                    }

                    current = current.parent;
                }

                if (xrRoot != null)
                {
                    xrRoot.gameObject.SetActive(active);
                    return;
                }
            }

            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                if (roots[index].name.IndexOf("XR Origin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    roots[index].SetActive(active);
                }
            }
        }

        static T FindSceneComponent<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                var component = roots[index].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        static bool IsUnderXrOrigin(Transform candidate)
        {
            var current = candidate;
            while (current != null)
            {
                if (current.name.IndexOf("XR Origin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
