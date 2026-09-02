using System;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Scene
{
    /// <summary>
    /// P2 composition adapter. It is the only production bridge between application DTOs,
    /// the scene visual driver and the scene-provided UI anchors.
    /// </summary>
    [DefaultExecutionOrder(600)]
    [DisallowMultipleComponent]
    public sealed class MovingTargetRangeRuntimeAdapter : MonoBehaviour
    {
        [SerializeField] TrainingRangeSceneBindings rangeBindings;
        [SerializeField] MovingTargetVisualDriver visualDriver;

        ApplicationServices services;
        IDisposable stateSubscription;
        bool initialized;
        string visualSessionId = string.Empty;

        public bool IsInitialized => initialized;
        public TrainingRangeSceneBindings RangeBindings => rangeBindings;
        public MovingTargetVisualDriver VisualDriver => visualDriver;

        public void Configure(
            TrainingRangeSceneBindings bindings,
            MovingTargetVisualDriver driver)
        {
            rangeBindings = bindings;
            visualDriver = driver;
        }

        void Awake()
        {
            ResolveSceneReferences();
        }

        void Start()
        {
            TryInitialize();
        }

        void Update()
        {
            if (!initialized && !TryInitialize())
            {
                return;
            }

            if (!services.TrainingSessions.HasActiveSession
                || services.TrainingSessions.Current.Mode != TrainingMode.MovingTarget)
            {
                return;
            }

            services.MovingTargetProgress.Tick(
                services.TrainingSessions.Current.SessionId,
                Time.deltaTime);
        }

        void OnDisable()
        {
            stateSubscription?.Dispose();
            stateSubscription = null;
            services = null;
            initialized = false;
            visualSessionId = string.Empty;
        }

        public void ApplyState(MovingTargetSessionDto session)
        {
            if (visualDriver == null)
            {
                return;
            }

            visualDriver.Apply(new MovingTargetVisualState(
                session.RouteProgress01,
                ResolveDirection(session.Phase),
                session.Phase == TargetMovePhase.LeftEndpointHold,
                session.CanShoot,
                session.SpeedMetersPerSecond));
        }

        bool TryInitialize()
        {
            if (initialized)
            {
                return true;
            }

            ResolveSceneReferences();
            services = GameMain.Instance?.Services;
            if (services == null || rangeBindings == null || visualDriver == null)
            {
                return false;
            }

            var ui = FindInScene<MovingTargetRangeUI>();
            if (ui == null)
            {
                ui = MovingTargetRangeUI.EnsureExistsInScene(services);
            }

            var eventCamera = FindActiveSceneCamera();
            if (!ui.BindToSceneAnchors(
                    rangeBindings.LargeUiAnchor,
                    rangeBindings.MinimalHudAnchor,
                    eventCamera))
            {
                services = null;
                return false;
            }

            stateSubscription = services.EventBus.Subscribe<MovingTargetStateChangedEvent>(OnStateChanged);
            initialized = true;

            if (services.TrainingSessions.HasActiveSession
                && services.TrainingSessions.Current.Mode == TrainingMode.MovingTarget)
            {
                var current = services.MovingTarget.GetSession(services.TrainingSessions.Current.SessionId);
                if (current.Success)
                {
                    ApplyState(current.Data);
                }
            }

            return true;
        }

        void ResolveSceneReferences()
        {
            if (rangeBindings == null)
            {
                rangeBindings = FindInScene<TrainingRangeSceneBindings>();
            }

            if (visualDriver == null)
            {
                visualDriver = FindInScene<MovingTargetVisualDriver>();
            }
        }

        void OnStateChanged(MovingTargetStateChangedEvent evt)
        {
            var isCurrent = services.TrainingSessions.HasActiveSession
                            && services.TrainingSessions.Current.Mode == TrainingMode.MovingTarget
                            && services.TrainingSessions.Current.SessionId == evt.Session.SessionId;
            if (!isCurrent && visualSessionId != evt.Session.SessionId)
            {
                return;
            }

            visualSessionId = evt.Session.SessionId;
            ApplyState(evt.Session);
        }

        T FindInScene<T>() where T : Component
        {
            var roots = gameObject.scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                var candidate = roots[index].GetComponentInChildren<T>(true);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        Camera FindActiveSceneCamera()
        {
            var roots = gameObject.scene.GetRootGameObjects();
            Camera fallback = null;
            for (var index = 0; index < roots.Length; index++)
            {
                var cameras = roots[index].GetComponentsInChildren<Camera>(true);
                for (var cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
                {
                    if (cameras[cameraIndex].isActiveAndEnabled)
                    {
                        return cameras[cameraIndex];
                    }

                    fallback ??= cameras[cameraIndex];
                }
            }

            return fallback;
        }

        static MovingTargetTravelDirection ResolveDirection(TargetMovePhase phase)
        {
            switch (phase)
            {
                case TargetMovePhase.MovingRightToLeft:
                    return MovingTargetTravelDirection.RightToLeft;
                case TargetMovePhase.MovingLeftToRight:
                    return MovingTargetTravelDirection.LeftToRight;
                default:
                    return MovingTargetTravelDirection.Stationary;
            }
        }
    }
}
