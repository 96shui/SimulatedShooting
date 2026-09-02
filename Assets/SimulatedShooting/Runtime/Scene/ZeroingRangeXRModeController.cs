using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class ZeroingRangeXRModeController : MonoBehaviour
    {
        [SerializeField] private GameObject xrOrigin;
        [SerializeField] private Camera noVrCamera;
        [SerializeField] private bool autoDetectVrDisplayInEditor;

        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        readonly List<Camera> disabledExternalCameras = new List<Camera>();
        readonly List<AudioListener> disabledExternalListeners = new List<AudioListener>();
        bool? forcedVrMode;
        bool lastVrMode;
        bool initialized;

        public bool IsVrMode => initialized && lastVrMode;
        public GameObject XrOrigin => xrOrigin;
        public Camera NoVrCamera => noVrCamera;

        public void Configure(GameObject origin, Camera fallbackCamera)
        {
            xrOrigin = origin;
            noVrCamera = fallbackCamera;
            ApplyMode(ResolveVrMode(), force: true);
        }

        void Awake()
        {
            ApplyMode(ResolveVrMode(), force: true);
        }

        void Update()
        {
            ApplyMode(ResolveVrMode(), force: false);
        }

        void LateUpdate()
        {
            ApplyMode(ResolveVrMode(), force: false);
        }

        void OnDisable()
        {
            RestoreExternalPlayerView();
        }

        public void SetVrModeForTests(bool enabled)
        {
            forcedVrMode = enabled;
            ApplyMode(enabled, force: true);
        }

        public void ClearForcedModeForTests()
        {
            forcedVrMode = null;
            ApplyMode(ResolveVrMode(), force: true);
        }

        bool ResolveVrMode()
        {
            if (forcedVrMode.HasValue)
            {
                return forcedVrMode.Value;
            }

#if UNITY_EDITOR
            if (!autoDetectVrDisplayInEditor)
            {
                return false;
            }
#endif

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

        void ApplyMode(bool vrMode, bool force)
        {
            var modeChanged = force || !initialized || vrMode != lastVrMode;

            lastVrMode = vrMode;
            initialized = true;
            if (vrMode && modeChanged)
            {
                PrepareInteractionManager();
            }

            if (xrOrigin != null)
            {
                xrOrigin.SetActive(vrMode);
            }

            if (noVrCamera != null)
            {
                noVrCamera.enabled = !vrMode;
                var listener = noVrCamera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = !vrMode;
                }
            }

            if (vrMode && xrOrigin != null)
            {
                EnsureSingleVrCameraAndListener();
            }

            EnsureExclusivePlayerView(vrMode ? ResolveVrCamera() : noVrCamera);
        }

        Camera ResolveVrCamera()
        {
            if (xrOrigin == null)
            {
                return null;
            }

            var cameras = xrOrigin.GetComponentsInChildren<Camera>(true);
            for (var index = 0; index < cameras.Length; index++)
            {
                if (cameras[index] != null && cameras[index].isActiveAndEnabled)
                {
                    return cameras[index];
                }
            }

            return cameras.Length > 0 ? cameras[0] : null;
        }

        void EnsureExclusivePlayerView(Camera desiredCamera)
        {
            if (desiredCamera == null)
            {
                return;
            }

            var activeCameras = FindObjectsOfType<Camera>();
            for (var index = 0; index < activeCameras.Length; index++)
            {
                var camera = activeCameras[index];
                if (camera == null || camera == desiredCamera || !camera.enabled)
                {
                    continue;
                }

                camera.enabled = false;
                if (!disabledExternalCameras.Contains(camera))
                {
                    disabledExternalCameras.Add(camera);
                }

                var listener = camera.GetComponent<AudioListener>();
                if (listener != null && listener.enabled)
                {
                    listener.enabled = false;
                    if (!disabledExternalListeners.Contains(listener))
                    {
                        disabledExternalListeners.Add(listener);
                    }
                }
            }
        }

        void RestoreExternalPlayerView()
        {
            for (var index = 0; index < disabledExternalCameras.Count; index++)
            {
                if (disabledExternalCameras[index] != null)
                {
                    disabledExternalCameras[index].enabled = true;
                }
            }

            for (var index = 0; index < disabledExternalListeners.Count; index++)
            {
                if (disabledExternalListeners[index] != null)
                {
                    disabledExternalListeners[index].enabled = true;
                }
            }

            disabledExternalCameras.Clear();
            disabledExternalListeners.Clear();
        }

        void PrepareInteractionManager()
        {
            if (xrOrigin == null)
            {
                return;
            }

            var sceneManager = xrOrigin.GetComponentInChildren<XRInteractionManager>(true);
            XRInteractionManager externalManager = null;
            var managers = Resources.FindObjectsOfTypeAll<XRInteractionManager>();
            for (var index = 0; index < managers.Length; index++)
            {
                var candidate = managers[index];
                if (candidate != null && candidate != sceneManager && candidate.isActiveAndEnabled)
                {
                    externalManager = candidate;
                    break;
                }
            }

            if (externalManager == null)
            {
                if (sceneManager != null)
                {
                    sceneManager.enabled = true;
                }

                return;
            }

            if (sceneManager != null)
            {
                sceneManager.enabled = false;
            }

            var interactors = xrOrigin.GetComponentsInChildren<XRBaseInteractor>(true);
            for (var index = 0; index < interactors.Length; index++)
            {
                interactors[index].interactionManager = externalManager;
            }

            var rifle = FindObjectOfType<TrainingRifleGrabInteractable>(true);
            if (rifle != null)
            {
                rifle.interactionManager = externalManager;
            }
        }

        void EnsureSingleVrCameraAndListener()
        {
            var cameras = xrOrigin.GetComponentsInChildren<Camera>(true);
            Camera activeCamera = null;
            for (var index = 0; index < cameras.Length; index++)
            {
                var enable = activeCamera == null;
                cameras[index].enabled = enable;
                if (enable)
                {
                    activeCamera = cameras[index];
                }
            }

            var listeners = xrOrigin.GetComponentsInChildren<AudioListener>(true);
            var keptListener = false;
            for (var index = 0; index < listeners.Length; index++)
            {
                var sameCamera = activeCamera != null && listeners[index].gameObject == activeCamera.gameObject;
                listeners[index].enabled = sameCamera && !keptListener;
                keptListener |= listeners[index].enabled;
            }

            if (activeCamera != null && !keptListener)
            {
                activeCamera.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
