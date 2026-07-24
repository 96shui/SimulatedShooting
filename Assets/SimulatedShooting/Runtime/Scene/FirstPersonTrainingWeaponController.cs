using System;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class FirstPersonTrainingWeaponController : MonoBehaviour
    {
        const float MaxShotDistance = 150f;

        [SerializeField] private Camera viewCamera;
        [SerializeField] private WeaponPrefabBinding weaponBinding;
        [SerializeField] private TrainingRifleGrabInteractable grabInteractable;
        [SerializeField] private TargetImpactSurface targetSurface;
        [SerializeField] private Transform tracerRoot;
        [SerializeField] private WeaponFeedbackController feedbackController;
        [SerializeField] private Transform headPoseSource;
        [SerializeField] private Transform rearHandPoseSource;
        [SerializeField] private Transform frontHandPoseSource;
        [SerializeField] private bool preferVrPoseSources;
        [SerializeField] private float hipFieldOfView = 48f;
        [SerializeField] private float adsFieldOfView = 36f;
        [SerializeField] private float lookDegreesPerSecond = 82f;
        [SerializeField] private float frontHandAdjustSpeed = 0.28f;
        [SerializeField] private bool showDebugOverlay = true;

        readonly Vector3 rightShoulderLocal = new Vector3(0.22f, -0.26f, 0.42f);
        readonly Vector3 leftShoulderLocal = new Vector3(-0.22f, -0.26f, 0.42f);

        IGameEventBus eventBus;
        ApplicationServices services;
        IWeaponControlService weaponService;
        IXRTrainingInput input;
        IWeaponHapticOutput hapticOutput;
        XRTrainingInputCommandDispatcher dispatcher;
        IDisposable inputSubscription;
        Material tracerMaterial;

        WeaponControlStateDto currentState;
        WeaponShotResultDto lastShot;
        Vector3 headPosition;
        Vector2 frontHandOffset;
        float yaw;
        float pitch;
        Vector3 recoilRootBasePosition;
        Quaternion recoilRootBaseRotation = Quaternion.identity;
        WeaponRecoilImpulseDto activeRecoil;
        float recoilElapsed;
        bool recoilActive;
        bool simulatedRearGripHeld;
        bool simulatedFrontGripHeld;
        int tracerCounter;
        bool initialized;
        bool hasCurrentState;
        bool grabSubscribed;

        public bool IsInitialized => initialized;
        public bool HasRequiredWeaponBinding => weaponBinding != null && weaponBinding.HasRequiredBinding;
        public int CurrentMagazine => hasCurrentState ? currentState.CurrentMagazine : 0;
        public int ReserveAmmo => hasCurrentState ? currentState.ReserveAmmo : 0;
        public bool CanShoot => hasCurrentState && currentState.CanShoot;
        public ShoulderSide CurrentShoulder => hasCurrentState ? currentState.ShoulderSide : ShoulderSide.Right;
        public WeaponAimMode CurrentAimMode => hasCurrentState ? currentState.AimMode : WeaponAimMode.HipFire;
        public bool TwoHandGripActive => hasCurrentState && currentState.TwoHandGripActive;
        public WeaponHoldState CurrentHoldState => hasCurrentState ? currentState.HoldState : WeaponHoldState.OnRack;
        public float Stability01 => hasCurrentState ? currentState.Stability01 : 0f;
        public int TracerCount => tracerCounter;
        public bool LastShotWasValid => lastShot.IsValidShot;
        public bool LastShotHit => lastShot.Hit;
        public string LastHitObjectId => lastShot.HitObjectId ?? string.Empty;
        public Vector3 LastShotHitPoint => lastShot.HitPoint;
        public Vector3 LastShotMuzzlePosition => lastShot.MuzzlePosition;
        public Vector3 CurrentAimDirection => ResolveAimDirection();
        public bool HasVrPoseSources => headPoseSource != null && rearHandPoseSource != null && frontHandPoseSource != null;
        public bool UsingVrPoseSources => ShouldUseVrPoseSources();
        public TrainingRifleGrabInteractable GrabInteractable => grabInteractable;
        public WeaponFeedbackController FeedbackController => feedbackController;

        public string SessionId =>
            services != null && services.TrainingSessions.HasActiveSession
                ? services.TrainingSessions.Current.SessionId
                : string.Empty;

        public ApplicationServices Services => services;

        public void ConfigureForScene(
            Camera camera,
            WeaponPrefabBinding binding,
            TargetImpactSurface impactSurface,
            Transform tracerContainer)
        {
            viewCamera = camera;
            weaponBinding = binding;
            grabInteractable = binding != null ? binding.GetComponent<TrainingRifleGrabInteractable>() : null;
            targetSurface = impactSurface;
            tracerRoot = tracerContainer;
        }

        public void ConfigureVrPoseSources(Transform headPose, Transform rearHandPose, Transform frontHandPose)
        {
            headPoseSource = headPose;
            rearHandPoseSource = rearHandPose;
            frontHandPoseSource = frontHandPose;
            preferVrPoseSources = headPose != null && rearHandPose != null && frontHandPose != null;
        }

        public void ConfigureFeedback(WeaponFeedbackController feedback)
        {
            feedbackController = feedback;
        }

        public void ConfigureServices(ApplicationServices applicationServices, IXRTrainingInput trainingInput = null)
        {
            TearDownInputSubscription();
            services = applicationServices;
            eventBus = applicationServices?.EventBus;
            weaponService = applicationServices?.WeaponControl;
            input = trainingInput ?? applicationServices?.TrainingInput;
            hapticOutput = new InputSystemWeaponHapticOutput();
            initialized = false;
            hasCurrentState = false;
        }

        public void ConfigureInput(IXRTrainingInput trainingInput)
        {
            TearDownInputSubscription();
            input = trainingInput;
            initialized = false;
        }

        public void ConfigureHaptics(IWeaponHapticOutput output)
        {
            hapticOutput = output;
        }

        public bool ReloadOnceForTests()
        {
            EnsureInitialized();
            if (!initialized || string.IsNullOrEmpty(SessionId))
            {
                return false;
            }

            ApplyStateResult(weaponService.Reload(SessionId));
            return true;
        }

        private void OnDestroy()
        {
            UnsubscribeGrabInteractable();
            TearDownInputSubscription();
        }

        void TearDownInputSubscription()
        {
            inputSubscription?.Dispose();
            inputSubscription = null;
            dispatcher = null;
        }

        private void Awake()
        {
            ResolveSceneReferences();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            SubscribeGrabInteractable();
        }

        private void OnDisable()
        {
            UnsubscribeGrabInteractable();
            TearDownInputSubscription();
        }

        private void Update()
        {
            EnsureInitialized();
            if (!initialized)
            {
                return;
            }

            UpdateNoVrPose(Time.deltaTime);
            UpdateGripState();
            UpdateAimModeFromInput();
            dispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = ScreenId.ZeroingHud
            });
            UpdateRecoil(Time.deltaTime);
        }

        public bool FireOnceForTests()
        {
            EnsureInitialized();
            if (!initialized)
            {
                return false;
            }

            UpdateNoVrPose(0f);
            ForceTwoHandGripForTests();
            UpdateNoVrPose(0f);
            return FireCurrentWeapon();
        }

        public bool FireCurrentStateForTests()
        {
            EnsureInitialized();
            return initialized && FireCurrentWeapon();
        }

        public bool InitializeForTests()
        {
            EnsureInitialized();
            return initialized;
        }

        public void SetAimModeForTests(WeaponAimMode aimMode)
        {
            EnsureInitialized();
            if (!initialized)
            {
                return;
            }

            ApplyStateResult(weaponService.SetAimMode(SessionId, aimMode));
            UpdateNoVrPose(0f);
        }

        public void ToggleShoulderForTests()
        {
            EnsureInitialized();
            if (!initialized)
            {
                return;
            }

            ApplyStateResult(weaponService.ToggleShoulder(SessionId));
            UpdateNoVrPose(0f);
        }

        public void AdjustFrontHandForTests(Vector2 delta)
        {
            frontHandOffset = ClampFrontHandOffset(frontHandOffset + delta);
            EnsureInitialized();
            if (!initialized)
            {
                return;
            }

            ForceTwoHandGripForTests();
            UpdateNoVrPose(0f);
            UpdateGripState();
        }

        void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            ResolveSceneReferences();
            if (viewCamera == null || weaponBinding == null || !weaponBinding.HasRequiredBinding)
            {
                return;
            }

            if (!EnsureSharedServices())
            {
                return;
            }

            var sessionId = SessionId;
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            if (input == null)
            {
                input = services.TrainingInput ?? new InputSystemXRTrainingInput();
            }

            if (dispatcher == null && eventBus != null)
            {
                dispatcher = new XRTrainingInputCommandDispatcher(input, eventBus);
                inputSubscription = eventBus.Subscribe<XRTrainingInputCommandEvent>(HandleInputCommand);
            }

            weaponService = services.WeaponControl;
            var start = weaponService.GetState(sessionId);
            if (!start.Success)
            {
                start = weaponService.StartSession(sessionId, weaponBinding.WeaponId, TrainingMode.Zeroing100m);
            }

            if (!start.Success)
            {
                Debug.LogError($"Failed to start weapon session: {start.Message}", this);
                return;
            }

            currentState = start.Data;
            hasCurrentState = true;
            headPosition = viewCamera.transform.position;
            yaw = viewCamera.transform.eulerAngles.y;
            pitch = NormalizePitch(viewCamera.transform.eulerAngles.x);
            tracerMaterial = CreateTracerMaterial();
            CacheRecoilRootPose();
            hapticOutput ??= new InputSystemWeaponHapticOutput();
            initialized = true;
            SubscribeGrabInteractable();
            UpdateNoVrPose(0f);
            UpdateGripState();
        }

        bool EnsureSharedServices()
        {
            if (services != null && eventBus != null)
            {
                if (!services.TrainingSessions.HasActiveSession)
                {
                    var bootstrap = FindObjectOfType<ZeroingRangeSessionBootstrap>();
                    if (bootstrap != null)
                    {
                        bootstrap.EnsureZeroingSession();
                    }
                }

                return services.TrainingSessions.HasActiveSession;
            }

            var sceneBootstrap = FindObjectOfType<ZeroingRangeSessionBootstrap>();
            if (sceneBootstrap != null)
            {
                services = sceneBootstrap.EnsureServices();
                sceneBootstrap.EnsureZeroingSession();
                eventBus = services.EventBus;
                return services.TrainingSessions.HasActiveSession;
            }

            if (GameMain.Instance != null)
            {
                services = GameMain.Instance.Services;
                eventBus = services.EventBus;
                if (!services.TrainingSessions.HasActiveSession)
                {
                    services.Zeroing.StartSession(RandomSeed.Fixed(100), WeaponControlService.TrainingRifleId);
                }

                return services.TrainingSessions.HasActiveSession;
            }

            return false;
        }

        void ResolveSceneReferences()
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            }

            if (weaponBinding == null)
            {
                weaponBinding = GetComponentInChildren<WeaponPrefabBinding>(true);
            }

            if (grabInteractable == null && weaponBinding != null)
            {
                grabInteractable = weaponBinding.GetComponent<TrainingRifleGrabInteractable>();
            }

            if (targetSurface == null)
            {
                targetSurface = FindObjectOfType<TargetImpactSurface>();
            }

            if (tracerRoot == null)
            {
                var root = new GameObject("TracerRoot_training-rifle").transform;
                root.SetParent(transform, false);
                root.gameObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.TracerRoot";
                tracerRoot = root;
            }

            if (feedbackController == null)
            {
                feedbackController = GetComponent<WeaponFeedbackController>();
            }
        }

        void HandleInputCommand(XRTrainingInputCommandEvent evt)
        {
            if (evt.SourceScreen != ScreenId.ZeroingHud)
            {
                return;
            }

            switch (evt.CommandType)
            {
                case XRTrainingInputCommandType.Trigger:
                    FireCurrentWeapon();
                    break;
                case XRTrainingInputCommandType.Reload:
                    ApplyStateResult(weaponService.Reload(SessionId));
                    break;
                case XRTrainingInputCommandType.SwitchShoulder:
                    ApplyStateResult(weaponService.ToggleShoulder(SessionId));
                    break;
                case XRTrainingInputCommandType.RightGripPressed:
                    if (!ShouldUseVrPoseSources())
                    {
                        simulatedRearGripHeld = true;
                        UpdateGripState();
                    }
                    break;
                case XRTrainingInputCommandType.RightGripReleased:
                    if (!ShouldUseVrPoseSources())
                    {
                        simulatedRearGripHeld = false;
                        simulatedFrontGripHeld = false;
                        UpdateGripState();
                    }
                    break;
                case XRTrainingInputCommandType.LeftGripPressed:
                    if (!ShouldUseVrPoseSources() && simulatedRearGripHeld)
                    {
                        simulatedFrontGripHeld = true;
                        UpdateGripState();
                    }
                    break;
                case XRTrainingInputCommandType.LeftGripReleased:
                    if (!ShouldUseVrPoseSources())
                    {
                        simulatedFrontGripHeld = false;
                        UpdateGripState();
                    }
                    break;
            }
        }

        void UpdateAimModeFromInput()
        {
            if (input == null || !hasCurrentState)
            {
                return;
            }

            var desired = input.AimHeld ? WeaponAimMode.AimDownSights : WeaponAimMode.HipFire;
            if (desired != currentState.AimMode)
            {
                ApplyStateResult(weaponService.SetAimMode(SessionId, desired));
            }
        }

        void UpdateGripState()
        {
            if (weaponService == null)
            {
                return;
            }

            var useVr = ShouldUseVrPoseSources();
            var holdState = useVr && grabInteractable != null
                ? grabInteractable.HoldState
                : ResolveSimulatedHoldState();
            var rearTracked = holdState == WeaponHoldState.RearHandHeld || holdState == WeaponHoldState.TwoHandHeld;
            var frontTracked = holdState == WeaponHoldState.TwoHandHeld;
            var stability = ResolveGripStability(frontTracked);
            ApplyStateResult(weaponService.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = SessionId,
                HoldState = holdState,
                RearHandTracked = rearTracked,
                FrontHandTracked = frontTracked,
                Stability01 = stability
            }));
        }

        void UpdateNoVrPose(float deltaTime)
        {
            if (!initialized || viewCamera == null || weaponBinding == null || !hasCurrentState)
            {
                return;
            }

            if (ShouldUseVrPoseSources())
            {
                UpdateVrReferencesOnly();
                return;
            }

            var isAds = currentState.AimMode == WeaponAimMode.AimDownSights;
            if (!isAds)
            {
                var turnAxis = input != null ? input.TurnAxis : Vector2.zero;
                yaw += turnAxis.x * lookDegreesPerSecond * deltaTime;
                pitch = Mathf.Clamp(pitch - turnAxis.y * lookDegreesPerSecond * deltaTime, -55f, 70f);
            }

            var moveAxis = input != null ? input.MoveAxis : Vector2.zero;
            frontHandOffset = ClampFrontHandOffset(frontHandOffset + moveAxis * frontHandAdjustSpeed * deltaTime);

            var baseRotation = Quaternion.Euler(pitch, yaw, 0f);
            if (currentState.HoldState == WeaponHoldState.OnRack || currentState.HoldState == WeaponHoldState.Dropped)
            {
                viewCamera.transform.SetPositionAndRotation(headPosition, baseRotation * ResolveNoVrCameraRecoil());
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, hipFieldOfView, 0.35f);
                return;
            }

            var shoulderLocal = currentState.ShoulderSide == ShoulderSide.Right ? rightShoulderLocal : leftShoulderLocal;
            var rearHand = headPosition + baseRotation * shoulderLocal;
            var frontHand = rearHand + baseRotation * new Vector3(
                frontHandOffset.x,
                frontHandOffset.y,
                ResolveGripDistance());
            var direction = (frontHand - rearHand).sqrMagnitude > 0.0001f
                ? (frontHand - rearHand).normalized
                : baseRotation * Vector3.forward;

            var weaponRotation = Quaternion.LookRotation(direction, Vector3.up);
            weaponBinding.transform.SetPositionAndRotation(rearHand, weaponRotation);
            if (weaponBinding.RearHandGrip != null)
            {
                weaponBinding.transform.position += rearHand - weaponBinding.RearHandGrip.position;
            }

            if (isAds && weaponBinding.AimLinePoint != null)
            {
                var aim = weaponBinding.AimLinePoint;
                viewCamera.transform.SetPositionAndRotation(
                    aim.position - aim.forward * 0.08f + aim.up * 0.012f,
                    Quaternion.LookRotation(aim.forward, aim.up) * ResolveNoVrCameraRecoil());
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, adsFieldOfView, 0.35f);
            }
            else
            {
                viewCamera.transform.SetPositionAndRotation(headPosition, baseRotation * ResolveNoVrCameraRecoil());
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, hipFieldOfView, 0.35f);
            }
        }

        void UpdateVrReferencesOnly()
        {
            var headCamera = headPoseSource.GetComponent<Camera>();
            if (headCamera != null && headCamera.gameObject.activeInHierarchy)
            {
                viewCamera = headCamera;
            }
        }

        bool ShouldUseVrPoseSources()
        {
            return preferVrPoseSources &&
                   headPoseSource != null &&
                   rearHandPoseSource != null &&
                   frontHandPoseSource != null &&
                   headPoseSource.gameObject.activeInHierarchy &&
                   rearHandPoseSource.gameObject.activeInHierarchy &&
                   frontHandPoseSource.gameObject.activeInHierarchy;
        }

        WeaponHoldState ResolveSimulatedHoldState()
        {
            if (simulatedRearGripHeld && simulatedFrontGripHeld)
            {
                return WeaponHoldState.TwoHandHeld;
            }

            return simulatedRearGripHeld ? WeaponHoldState.RearHandHeld : WeaponHoldState.OnRack;
        }

        float ResolveGripStability(bool twoHandGripActive)
        {
            if (!twoHandGripActive)
            {
                return 0.35f;
            }

            if (ShouldUseVrPoseSources())
            {
                var handDistance = Vector3.Distance(rearHandPoseSource.position, frontHandPoseSource.position);
                var expectedDistance = ResolveGripDistance();
                var tolerance = Mathf.Max(0.18f, expectedDistance * 0.65f);
                return Mathf.Clamp01(1f - Mathf.Abs(handDistance - expectedDistance) / tolerance);
            }

            return Mathf.Clamp01(1f - frontHandOffset.magnitude / 0.32f);
        }

        float ResolveGripDistance()
        {
            if (weaponBinding != null && weaponBinding.RearHandGrip != null && weaponBinding.FrontHandGrip != null)
            {
                return Mathf.Max(0.20f,
                    Vector3.Distance(weaponBinding.RearHandGrip.position, weaponBinding.FrontHandGrip.position));
            }

            return 0.32f;
        }

        void SubscribeGrabInteractable()
        {
            if (grabSubscribed || grabInteractable == null)
            {
                return;
            }

            grabInteractable.HoldStateChanged += HandleGrabHoldStateChanged;
            grabSubscribed = true;
        }

        void UnsubscribeGrabInteractable()
        {
            if (!grabSubscribed || grabInteractable == null)
            {
                return;
            }

            grabInteractable.HoldStateChanged -= HandleGrabHoldStateChanged;
            grabSubscribed = false;
        }

        void HandleGrabHoldStateChanged(WeaponHoldState _, bool __, bool ___)
        {
            if (initialized)
            {
                UpdateGripState();
            }
        }

        void ForceTwoHandGripForTests()
        {
            simulatedRearGripHeld = true;
            simulatedFrontGripHeld = true;
            ApplyStateResult(weaponService.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = SessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = ResolveGripStability(true)
            }));
        }

        void CacheRecoilRootPose()
        {
            var recoilRoot = weaponBinding != null ? weaponBinding.RecoilRoot : null;
            if (recoilRoot == null)
            {
                return;
            }

            recoilRootBasePosition = recoilRoot.localPosition;
            recoilRootBaseRotation = recoilRoot.localRotation;
        }

        Quaternion ResolveNoVrCameraRecoil()
        {
            if (!recoilActive)
            {
                return Quaternion.identity;
            }

            var response = ResolveRecoilResponse(activeRecoil, recoilElapsed);
            return Quaternion.Euler(
                -activeRecoil.NoVrCameraPitchDegrees * response,
                activeRecoil.NoVrCameraYawDegrees * response,
                0f);
        }

        static float ResolveRecoilResponse(WeaponRecoilImpulseDto impulse, float elapsed)
        {
            if (!impulse.HasImpulse)
            {
                return 0f;
            }

            var kickDuration = Mathf.Max(0.001f, impulse.KickDurationSeconds);
            if (elapsed <= kickDuration)
            {
                return Mathf.SmoothStep(0f, 1f, elapsed / kickDuration);
            }

            var settleDuration = Mathf.Max(0.001f, impulse.SettleDurationSeconds);
            var settle = Mathf.Clamp01((elapsed - kickDuration) / settleDuration);
            return 1f - Mathf.SmoothStep(0f, 1f, settle);
        }

        static float ResolveAimMotionOffsetCm(Vector3 rawDirection, Vector3 effectiveDirection)
        {
            if (rawDirection.sqrMagnitude <= 0.0001f || effectiveDirection.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            var angleRadians = Vector3.Angle(rawDirection, effectiveDirection) * Mathf.Deg2Rad;
            return Mathf.Abs(Mathf.Tan(angleRadians) * ZeroingRules.DistanceMeters * 100f);
        }

        bool FireCurrentWeapon()
        {
            if (weaponBinding == null || weaponBinding.MuzzlePoint == null || string.IsNullOrEmpty(SessionId))
            {
                return false;
            }

            var sessionId = SessionId;
            var muzzle = weaponBinding.MuzzlePoint.position;
            var direction = ResolveAimDirection();
            var rawDirection = weaponBinding.transform.forward.normalized;
            var aimMotionOffsetCm = ResolveAimMotionOffsetCm(rawDirection, direction);
            var hit = Physics.Raycast(muzzle, direction, out var raycastHit, MaxShotDistance);
            var hitPoint = ResolveHitPointForService(raycastHit, hit, muzzle, direction);
            var hitObjectId = hit ? ResolveTestId(raycastHit.collider.transform) : string.Empty;

            var fire = weaponService.Fire(new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = muzzle,
                WeaponPosition = weaponBinding.transform.position,
                RawAimDirection = rawDirection,
                AimDirection = direction,
                AimMotionOffsetCm = aimMotionOffsetCm,
                Stability01 = currentState.Stability01,
                TwoHandGripActive = currentState.TwoHandGripActive,
                AimMode = currentState.AimMode,
                ShoulderSide = currentState.ShoulderSide,
                Hit = hit,
                HitPoint = hitPoint,
                HitObjectId = hitObjectId
            });

            lastShot = fire.Data;
            var state = weaponService.GetState(sessionId);
            if (state.Success)
            {
                currentState = state.Data;
            }

            if (!fire.Success)
            {
                return false;
            }

            var visualEnd = hit ? raycastHit.point : muzzle + direction * MaxShotDistance;
            SpawnTracer(muzzle, visualEnd, hit, raycastHit);
            RecordImpactIfNeeded(raycastHit, hit);
            ApplyRecoil(fire.Data.RecoilImpulse);
            hapticOutput?.SendShotImpulse(fire.Data.RecoilImpulse, fire.Data.FrontHandTracked);
            return true;
        }

        Vector3 ResolveHitPointForService(RaycastHit raycastHit, bool hit, Vector3 muzzle, Vector3 direction)
        {
            if (!hit)
            {
                return muzzle + direction * ZeroingRules.DistanceMeters;
            }

            var surface = raycastHit.collider.GetComponent<TargetImpactSurface>();
            if (surface == null && targetSurface != null && raycastHit.collider.transform == targetSurface.transform)
            {
                surface = targetSurface;
            }

            if (surface != null && surface.TryComputeOffsetCm(raycastHit.point, out var offsetCm))
            {
                return new Vector3(offsetCm.x, offsetCm.y, ZeroingRules.DistanceMeters);
            }

            return raycastHit.point;
        }

        void RecordImpactIfNeeded(RaycastHit raycastHit, bool hit)
        {
            if (!hit)
            {
                return;
            }

            var surface = raycastHit.collider.GetComponent<TargetImpactSurface>();
            if (surface == null && targetSurface != null && raycastHit.collider.transform == targetSurface.transform)
            {
                surface = targetSurface;
            }

            if (surface != null)
            {
                surface.TryRecordWorldPoint(raycastHit.point, out _);
            }
        }

        Vector3 ResolveAimDirection()
        {
            if (weaponBinding != null && weaponBinding.AimLinePoint != null)
            {
                return weaponBinding.AimLinePoint.forward.normalized;
            }

            if (weaponBinding != null && weaponBinding.MuzzlePoint != null)
            {
                return weaponBinding.MuzzlePoint.forward.normalized;
            }

            return viewCamera != null ? viewCamera.transform.forward : Vector3.forward;
        }

        void SpawnTracer(Vector3 start, Vector3 end, bool hit, RaycastHit raycastHit)
        {
            if (tracerRoot == null)
            {
                return;
            }

            tracerCounter++;
            if (feedbackController != null)
            {
                feedbackController.PlayValidShot(
                    tracerCounter,
                    start,
                    end,
                    hit,
                    hit ? raycastHit.point : end,
                    hit ? raycastHit.normal : Vector3.zero);
                return;
            }

            var tracer = new GameObject($"Tracer_training-rifle_{tracerCounter:000}");
            tracer.transform.SetParent(tracerRoot, true);
            tracer.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Tracer";
            var visual = tracer.AddComponent<BallisticTracerVisual>();
            visual.Configure(start, end, null, null, tracerMaterial, null, tracerCounter, null);
        }

        void ApplyRecoil(WeaponRecoilImpulseDto impulse)
        {
            activeRecoil = impulse;
            recoilElapsed = 0f;
            recoilActive = impulse.HasImpulse;
        }

        void UpdateRecoil(float deltaTime)
        {
            var recoilRoot = weaponBinding != null ? weaponBinding.RecoilRoot : null;
            if (recoilRoot == null)
            {
                return;
            }

            if (!recoilActive)
            {
                recoilRoot.localPosition = recoilRootBasePosition;
                recoilRoot.localRotation = recoilRootBaseRotation;
                return;
            }

            recoilElapsed += Mathf.Max(0f, deltaTime);
            var response = ResolveRecoilResponse(activeRecoil, recoilElapsed);
            var position = new Vector3(0f, activeRecoil.UpwardMeters, -activeRecoil.RearwardMeters) * response;
            var rotation = new Vector3(-activeRecoil.PitchDegrees, activeRecoil.YawDegrees, activeRecoil.RollDegrees) * response;
            recoilRoot.localPosition = recoilRootBasePosition + position;
            recoilRoot.localRotation = recoilRootBaseRotation * Quaternion.Euler(rotation);

            if (response <= 0f && recoilElapsed >= activeRecoil.KickDurationSeconds + activeRecoil.SettleDurationSeconds)
            {
                recoilActive = false;
                recoilRoot.localPosition = recoilRootBasePosition;
                recoilRoot.localRotation = recoilRootBaseRotation;
            }
        }

        void ApplyStateResult(VRShooting.Contracts.ServiceResult<WeaponControlStateDto> result)
        {
            if (result.Success)
            {
                currentState = result.Data;
                hasCurrentState = true;
            }
        }

        static Vector2 ClampFrontHandOffset(Vector2 value)
        {
            value.x = Mathf.Clamp(value.x, -0.18f, 0.18f);
            value.y = Mathf.Clamp(value.y, -0.14f, 0.14f);
            return value;
        }

        static float NormalizePitch(float value)
        {
            return value > 180f ? value - 360f : value;
        }

        static string ResolveTestId(Transform transform)
        {
            var current = transform;
            while (current != null)
            {
                var id = current.GetComponent<SceneTestId>();
                if (id != null)
                {
                    return id.Id;
                }

                current = current.parent;
            }

            return transform.name;
        }

        static Material CreateTracerMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "Runtime_Tracer_training-rifle" };
            material.color = new Color(1f, 0.72f, 0.12f, 1f);
            return material;
        }

        private void OnGUI()
        {
            if (!showDebugOverlay || !initialized)
            {
                return;
            }

            const int width = 420;
            GUI.Box(new Rect(12, 12, width, 124), string.Empty);
            GUI.Label(new Rect(24, 22, width - 20, 22), "TASK005 FIRST PERSON WEAPON");
            GUI.Label(new Rect(24, 44, width - 20, 22),
                $"MAG {currentState.CurrentMagazine}/3  RES {currentState.ReserveAmmo}  READY {currentState.CanShoot}");
            GUI.Label(new Rect(24, 66, width - 20, 22),
                $"MODE {currentState.AimMode}  SHOULDER {currentState.ShoulderSide}  STABILITY {currentState.Stability01:0.00}");
            GUI.Label(new Rect(24, 88, width - 20, 22),
                "Mouse/Arrows look, WASD front hand, RMB/Shift ADS, LMB/F fire, R reload, Q shoulder");
            GUI.Label(new Rect(24, 110, width - 20, 22),
                lastShot.IsValidShot ? $"LAST SHOT {(lastShot.Hit ? "HIT" : "MISS")} {lastShot.HitObjectId}" : "LAST SHOT none");
        }
    }
}
