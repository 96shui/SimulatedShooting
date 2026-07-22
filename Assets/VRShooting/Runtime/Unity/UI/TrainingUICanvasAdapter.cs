using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRShooting.Unity.Player;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Keeps generated training UI usable by both desktop pointers and tracked XR rays.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class TrainingUICanvasAdapter : MonoBehaviour
    {
        static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        [SerializeField]
        float vrDistance = 1.75f;

        [SerializeField]
        float vrWorldScale = 0.0009f;

        [SerializeField]
        float vrVerticalOffset = -0.05f;

        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

        Canvas canvas;
        GraphicRaycaster desktopRaycaster;
        TrackedDeviceGraphicRaycaster trackedRaycaster;
        bool? forcedVrMode;
        Camera forcedVrCamera;
        Camera activeVrCamera;
        bool initialized;
        bool lastVrMode;
        bool placementPending;

        public bool IsVrMode => initialized && lastVrMode;

        public Camera ActiveVrCamera => activeVrCamera;

        public GraphicRaycaster DesktopRaycaster => desktopRaycaster;

        public TrackedDeviceGraphicRaycaster TrackedRaycaster => trackedRaycaster;

        public void Configure(Canvas targetCanvas)
        {
            canvas = targetCanvas != null ? targetCanvas : GetComponent<Canvas>();
            EnsureRaycasters();
            ApplyMode(ResolveVrMode(), ResolveVrCamera(), true);
        }

        void Awake()
        {
            Configure(GetComponent<Canvas>());
        }

        void LateUpdate()
        {
            var vrMode = ResolveVrMode();
            var vrCamera = vrMode ? ResolveVrCamera() : null;
            ApplyMode(vrMode, vrCamera, false);

            if (vrMode && placementPending && vrCamera != null)
            {
                PlaceCanvasInFrontOf(vrCamera);
            }
        }

        public void SetMode(bool vrMode, Camera vrCamera)
        {
            forcedVrMode = vrMode;
            forcedVrCamera = vrCamera;
            ApplyMode(vrMode, vrCamera, true);
        }

        public void SetVrModeForTests(bool enabled, Camera vrCamera = null)
        {
            SetMode(enabled, vrCamera);
        }

        public void ClearForcedModeForTests()
        {
            forcedVrMode = null;
            forcedVrCamera = null;
            ApplyMode(ResolveVrMode(), ResolveVrCamera(), true);
        }

        public void ForcePlacementForTests()
        {
            if (lastVrMode && activeVrCamera != null)
            {
                PlaceCanvasInFrontOf(activeVrCamera);
            }
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

        Camera ResolveVrCamera()
        {
            if (forcedVrCamera != null)
            {
                return forcedVrCamera;
            }

            var cameras = Resources.FindObjectsOfTypeAll<Camera>();
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
                    return candidate;
                }

                if (candidate.isActiveAndEnabled)
                {
                    fallback = candidate;
                }
            }

            return fallback;
        }

        void EnsureRaycasters()
        {
            desktopRaycaster = GetComponent<GraphicRaycaster>();
            if (desktopRaycaster == null)
            {
                desktopRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            trackedRaycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
            if (trackedRaycaster == null)
            {
                trackedRaycaster = gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            trackedRaycaster.ignoreReversedGraphics = false;
            trackedRaycaster.checkFor2DOcclusion = false;
            trackedRaycaster.checkFor3DOcclusion = false;
        }

        void ApplyMode(bool vrMode, Camera vrCamera, bool force)
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            EnsureRaycasters();
            var cameraChanged = activeVrCamera != vrCamera;
            if (!force && initialized && lastVrMode == vrMode && !cameraChanged)
            {
                return;
            }

            initialized = true;
            lastVrMode = vrMode;
            activeVrCamera = vrCamera;

            var rectTransform = transform as RectTransform;
            if (vrMode)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = vrCamera;
                desktopRaycaster.enabled = false;
                trackedRaycaster.enabled = true;

                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = ReferenceResolution;
                    rectTransform.localScale = Vector3.one * vrWorldScale;
                }

                placementPending = vrCamera != null;
                if (vrCamera != null)
                {
                    PlaceCanvasInFrontOf(vrCamera);
                }

                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            desktopRaycaster.enabled = true;
            trackedRaycaster.enabled = false;
            placementPending = false;

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localPosition = Vector3.zero;
            }
        }

        void PlaceCanvasInFrontOf(Camera vrCamera)
        {
            var forward = Vector3.ProjectOnPlane(vrCamera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = vrCamera.transform.forward;
            }

            forward.Normalize();
            var position = vrCamera.transform.position + forward * vrDistance + Vector3.up * vrVerticalOffset;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            placementPending = false;
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
