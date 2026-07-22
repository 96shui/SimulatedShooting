using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRShooting.Common;
using VRShooting.Input;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class ZeroingRangeSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene", LoadSceneMode.Single);
            Physics.SyncTransforms();
            EnsureBootstrap();
        }

        static void EnsureBootstrap()
        {
            if (Object.FindObjectOfType<ZeroingRangeSessionBootstrap>() != null)
            {
                return;
            }

            var root = GameObject.Find("ZeroingRange") ?? new GameObject("ZeroingRange");
            if (root.GetComponent<ZeroingRangeSessionBootstrap>() == null)
            {
                root.AddComponent<ZeroingRangeSessionBootstrap>();
            }
        }

        [Test]
        public void Task004_SceneContainsStableTrainingAnchorsForVrAndNoVr()
        {
            Assert.That(Find("ZeroingRange.ShootingPosition"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Target.Primary"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>(), Is.Not.Null);
        }

        [Test]
        public void Task004_TargetHasSpecifiedDimensionsAtOneHundredMetres()
        {
            var shootingPosition = Find("ZeroingRange.ShootingPosition").transform;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var face = Find("ZeroingRange.Target.Face").GetComponent<Renderer>();
            var tenRing = Find("ZeroingRange.Target.TenRing").GetComponent<Renderer>();

            Assert.That(Vector3.Distance(shootingPosition.position, target.position), Is.EqualTo(100f).Within(0.001f));
            Assert.That(face.bounds.size.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(face.bounds.size.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(tenRing.bounds.size.x, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(tenRing.bounds.size.y, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void Task004_TargetCanBeHitFromShootingPosition()
        {
            var origin = Find("ZeroingRange.ShootingPosition").transform.position;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var direction = (target.position - origin).normalized;

            Assert.That(Physics.Raycast(origin, direction, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task004_FirstPersonCompositionFramesTargetThroughRangeLane()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var target = Find("ZeroingRange.Target.Primary").transform;
            var viewportPoint = camera.WorldToViewportPoint(target.position);

            Assert.That(viewportPoint.z, Is.GreaterThan(0f));
            Assert.That(viewportPoint.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(viewportPoint.y, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(Find("ZeroingRange.Environment.Lane"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Environment.Berm.Left"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Environment.Berm.Right"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Reference"), Is.Not.Null);
        }

        [Test]
        public void Task012_RaycastHitReturnsStableImpactCoordinates()
        {
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var origin = Find("ZeroingRange.ShootingPosition").transform.position;
            var direction = (surface.TargetCenter.position - origin).normalized;

            Assert.That(surface.TryRecordRay(new Ray(origin, direction), 101f, out var impact), Is.True);
            Assert.That(impact.OffsetCm.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(impact.OffsetCm.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(impact.InsideTenRing, Is.True);
            Assert.That(surface.TenRingRadiusCm, Is.EqualTo(5f).Within(0.001f));
            Assert.That(Find("ZeroingRange.Target.Center"), Is.Not.Null);
            Assert.That(surface.Impacts.Count, Is.EqualTo(1));
            Assert.That(surface.ImpactMarkerRoot.childCount, Is.EqualTo(1));
        }

        [Test]
        public void Task012_ImpactCoordinatesUseCentimetresAndMatchVisualMarker()
        {
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var worldPoint = surface.TargetCenter.TransformPoint(new Vector3(0.12f, -0.08f, 0f));

            Assert.That(surface.TryRecordWorldPoint(worldPoint, out var impact), Is.True);
            Assert.That(impact.OffsetCm.x, Is.EqualTo(12f).Within(0.01f));
            Assert.That(impact.OffsetCm.y, Is.EqualTo(-8f).Within(0.01f));
            Assert.That(impact.InsideTenRing, Is.False);
            Assert.That(surface.ImpactMarkerRoot.GetChild(0).position.x,
                Is.EqualTo(impact.WorldPoint.x).Within(0.001f));
            Assert.That(surface.ImpactMarkerRoot.GetChild(0).position.y,
                Is.EqualTo(impact.WorldPoint.y).Within(0.001f));
        }

        [Test]
        public void Task012_ImpactOutsideFiftyCentimetreFaceIsRejected()
        {
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var outsideFace = surface.TargetCenter.TransformPoint(new Vector3(0.251f, 0f, 0f));

            Assert.That(surface.TryRecordWorldPoint(outsideFace, out _), Is.False);
            Assert.That(surface.Impacts, Is.Empty);
            Assert.That(surface.ImpactMarkerRoot.childCount, Is.Zero);
        }

        [Test]
        public void Task016_VisualPolishKeepsTargetAndShootingBayRecognisable()
        {
            Assert.That(Find("ZeroingRange.Visual.Root"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Visual.RangeGate"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Visual.TargetFrame"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Visual.WeaponCrate.Left"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Lighting.Sun"), Is.Not.Null);
        }

        [Test]
        public void Task016_WeaponCrateAppearsInLowerLeftWithoutBlockingTarget()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var crate = Find("ZeroingRange.Visual.WeaponCrate.Left").transform;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var crateViewportPoint = camera.WorldToViewportPoint(crate.position + Vector3.up * 0.3f);
            var targetDirection = (target.position - camera.transform.position).normalized;

            Assert.That(crateViewportPoint.z, Is.GreaterThan(0f));
            Assert.That(crateViewportPoint.x, Is.InRange(0f, 0.45f));
            Assert.That(crateViewportPoint.y, Is.InRange(0f, 0.45f));
            Assert.That(Physics.Raycast(camera.transform.position, targetDirection, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task016_SceneStaysWithinPrototypeRenderingBudget()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            var materials = renderers.SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            var activeLights = roots.SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .Where(light => light.isActiveAndEnabled)
                .ToArray();
            var hasEnabledPostProcessing = roots
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component => component != null && component.enabled && component.GetType().Name == "Volume");

            Assert.That(renderers.Length, Is.LessThanOrEqualTo(256));
            Assert.That(materials.Length, Is.LessThanOrEqualTo(24));
            Assert.That(activeLights.Length, Is.EqualTo(1));
            Assert.That(activeLights[0].type, Is.EqualTo(LightType.Directional));
            Assert.That(hasEnabledPostProcessing, Is.False);
        }

        [Test]
        public void Task016_NoVrCameraHasClearTargetSightlineAndVrSafeSettings()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var target = Find("ZeroingRange.Target.Primary").transform;
            var hudAnchor = Find("ZeroingRange.HudAnchor");
            var direction = (target.position - camera.transform.position).normalized;

            Assert.That(camera.allowHDR, Is.False);
            Assert.That(camera.allowMSAA, Is.True);
            Assert.That(camera.useOcclusionCulling, Is.True);
            Assert.That(hudAnchor, Is.Not.Null);
            Assert.That(Physics.Raycast(camera.transform.position, direction, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task005_FirstPersonTrainingWeaponHasRequiredVisibleBindings()
        {
            var playerRoot = Find("ZeroingRange.Weapon.PlayerRoot");
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            Assert.That(playerRoot, Is.Not.Null);
            Assert.That(weapon, Is.Not.Null);

            var controller = playerRoot.GetComponent<FirstPersonTrainingWeaponController>();
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(binding, Is.Not.Null);
            Assert.That(binding.HasRequiredBinding, Is.True);
            Assert.That(controller.HasVrPoseSources, Is.True);
            Assert.That(Find("ZeroingRange.Weapon.Grip.RearHand"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Grip.FrontHand"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Muzzle"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.AimLine"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.TracerRoot"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR.HeadPose"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR.RearHandPose"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR.FrontHandPose"), Is.Not.Null);
        }

        [Test]
        public void Task005_TrainingRifleUsesLicensedQbz191Visual()
        {
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var model = binding.RecoilRoot.Find("Model_QBZ191");

            Assert.That(model, Is.Not.Null, "The procedural blockout should be replaced by the QBZ-191 model");
            Assert.That(Find("ZeroingRange.Weapon.Visual.QBZ191"), Is.EqualTo(model.gameObject));

            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var triangleCount = filters.Sum(filter =>
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    return 0L;
                }

                return Enumerable.Range(0, mesh.subMeshCount)
                    .Sum(subMesh => (long)mesh.GetIndexCount(subMesh) / 3L);
            });

            Assert.That(filters.Length, Is.GreaterThan(0));
            Assert.That(renderers.Length, Is.GreaterThan(0));
            Assert.That(triangleCount, Is.GreaterThan(35000), "The first-person model lost its expected visual detail");
            Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
                .All(material => material != null && material.mainTexture != null), Is.True);
            Assert.That(Vector3.Distance(binding.RearHandGrip.localPosition,
                new Vector3(0.006f, -0.10f, -0.135f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(binding.FrontHandGrip.localPosition,
                new Vector3(0.006f, -0.015f, 0.18f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(binding.RearHandGrip.position, binding.FrontHandGrip.position),
                Is.InRange(0.30f, 0.35f));
            Assert.That(binding.RecoilRoot.Find("Receiver"), Is.Null, "Legacy blockout geometry is still present");
        }

        [Test]
        public void Task005_NoVrFirstPersonWeaponFiresVisibleTracerAndTargetImpact()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var surface = Find("ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var initialImpacts = surface.Impacts.Count;

            Assert.That(controller.InitializeForTests(), Is.True);
            var fired = controller.FireOnceForTests();
            Physics.SyncTransforms();

            Assert.That(fired, Is.True);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(2));
            Assert.That(controller.LastShotWasValid, Is.True);
            Assert.That(controller.TracerCount, Is.EqualTo(1));
            Assert.That(surface.Impacts.Count, Is.EqualTo(initialImpacts + 1),
                $"Shot hit={controller.LastShotHit}, object='{controller.LastHitObjectId}', " +
                $"muzzle={controller.LastShotMuzzlePosition}, hitPoint={controller.LastShotHitPoint}, " +
                $"aim={controller.CurrentAimDirection}");
            Assert.That(Find("ZeroingRange.Weapon.Tracer"), Is.Not.Null);
        }

        [Test]
        public void Task005_AdsViewAlignsToGunLineAndFrontHandChangesAimDirection()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            Assert.That(controller.InitializeForTests(), Is.True);
            var before = controller.CurrentAimDirection;

            controller.AdjustFrontHandForTests(new Vector2(0.12f, 0.08f));
            var after = controller.CurrentAimDirection;
            controller.SetAimModeForTests(WeaponAimMode.AimDownSights);

            Assert.That(Vector3.Angle(before, after), Is.GreaterThan(1f));
            Assert.That(controller.CurrentAimMode, Is.EqualTo(WeaponAimMode.AimDownSights));
            Assert.That(Vector3.Angle(camera.transform.forward, controller.CurrentAimDirection), Is.LessThan(0.5f));
        }

        [Test]
        public void Task005_ShoulderSwitchUpdatesNoVrWeaponState()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            controller.ToggleShoulderForTests();

            Assert.That(controller.CurrentMagazine, Is.EqualTo(2));
            Assert.That(controller.CurrentShoulder, Is.EqualTo(ShoulderSide.Left));
        }

        [Test]
        public void Task005_ReloadRestoresMagazineAfterSpendingAmmo()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(0));
            Assert.That(controller.ReloadOnceForTests(), Is.True);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(3));
        }

        [Test]
        public void Task005_006_FireUpdatesSharedZeroingSession()
        {
            var bootstrap = Object.FindObjectOfType<ZeroingRangeSessionBootstrap>();
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.HasActiveSession, Is.True);
            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.FireOnceForTests(), Is.True);

            var zeroing = bootstrap.Services.Zeroing.GetSession(bootstrap.ActiveSessionId);
            Assert.That(zeroing.Success, Is.True, zeroing.Message);
            Assert.That(zeroing.Data.ShotsRemainingInRound, Is.EqualTo(2));
            Assert.That(zeroing.Data.CurrentRound, Is.EqualTo(1));
        }

        [Test]
        public void Task013_RiflePrefabHasDirectGrabPhysicsAndStableRecoilHierarchy()
        {
            var weapon = Find("ZeroingRange.Weapon.TrainingRifle");
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var grab = weapon.GetComponent<TrainingRifleGrabInteractable>();
            var body = weapon.GetComponent<Rigidbody>();

            Assert.That(grab, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(weapon.GetComponents<BoxCollider>().Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(grab.selectMode, Is.EqualTo(InteractableSelectMode.Multiple));
            Assert.That(binding.RearHandGrip.IsChildOf(binding.RecoilRoot), Is.False);
            Assert.That(binding.FrontHandGrip.IsChildOf(binding.RecoilRoot), Is.False);
            Assert.That(Find("ZeroingRange.Weapon.PickupPrompt"), Is.Not.Null);
        }

        [Test]
        public void Task013_NoVrModeStartsOnRackAndCannotShootBeforeGrip()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();

            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.OnRack));
            Assert.That(controller.CanShoot, Is.False);
            Assert.That(controller.CurrentMagazine, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease()
        {
            var mode = ResolveOrCreateXRModeController();
            Assert.That(mode, Is.Not.Null, "Task013 XR mode controller is missing");
            Assert.That(mode.NoVrCamera, Is.Not.Null, "Task013 no-VR camera binding is missing");
            Assert.That(mode.XrOrigin, Is.Not.Null, "Task013 XR Origin binding is missing");
            mode.SetVrModeForTests(true);

            var grab = Find("ZeroingRange.Weapon.TrainingRifle").GetComponent<TrainingRifleGrabInteractable>();
            var right = Find("ZeroingRange.Origin.VR.RightDirectInteractor").GetComponent<XRDirectInteractor>();
            var left = Find("ZeroingRange.Origin.VR.LeftDirectInteractor").GetComponent<XRDirectInteractor>();
            var manager = mode.XrOrigin.GetComponentInChildren<XRInteractionManager>(true);

            // This test drives selection through the manager, so hardware actions must stay on the
            // deterministic no-device path while the interaction manager processes strength values.
            right.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            left.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            yield return null;

            Assert.That(grab.RearGrabRadius, Is.EqualTo(0.10f).Within(0.001f));
            Assert.That(grab.FrontGrabRadius, Is.EqualTo(0.12f).Within(0.001f));
            right.transform.position = grab.RearAttach.position;
            left.transform.position = grab.FrontAttach.position;
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)right), Is.True);
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)left), Is.False, "front hand cannot become the primary grab");

            manager.SelectEnter((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            yield return null;
            Assert.That(grab.HoldState, Is.EqualTo(WeaponHoldState.RearHandHeld));

            left.transform.position = grab.FrontAttach.position;
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)left), Is.True);
            manager.SelectEnter((IXRSelectInteractor)left, (IXRSelectInteractable)grab);
            yield return null;
            Assert.That(grab.HoldState, Is.EqualTo(WeaponHoldState.TwoHandHeld));

            manager.SelectExit((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            yield return null;
            Assert.That(grab.HoldState, Is.EqualTo(WeaponHoldState.Dropped));
            Assert.That(grab.FrontHandSelected, Is.False);
        }

        [UnityTest]
        public IEnumerator Task013_RuntimeModeKeepsExactlyOneCameraAndAudioListener()
        {
            var mode = ResolveOrCreateXRModeController();
            Assert.That(mode, Is.Not.Null, "Task013 XR mode controller is missing");
            Assert.That(mode.NoVrCamera, Is.Not.Null, "Task013 no-VR camera binding is missing");
            Assert.That(mode.XrOrigin, Is.Not.Null, "Task013 XR Origin binding is missing");

            mode.SetVrModeForTests(false);
            yield return null;
            Assert.That(mode.NoVrCamera.isActiveAndEnabled, Is.True);
            Assert.That(mode.XrOrigin.GetComponentsInChildren<Camera>(true)
                .Count(camera => camera != null && camera.isActiveAndEnabled), Is.Zero);
            Assert.That(CountPlayerAudioListeners(mode), Is.EqualTo(1));

            mode.SetVrModeForTests(true);
            yield return null;
            Assert.That(mode.NoVrCamera.isActiveAndEnabled, Is.False);
            Assert.That(mode.XrOrigin.GetComponentsInChildren<Camera>(true)
                .Count(camera => camera != null && camera.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(CountPlayerAudioListeners(mode), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Task013_ShotProducesLocalRecoilAndTwoHandHapticsThenSettles()
        {
            var controller = Find("ZeroingRange.Weapon.PlayerRoot").GetComponent<FirstPersonTrainingWeaponController>();
            var binding = Find("ZeroingRange.Weapon.TrainingRifle").GetComponent<WeaponPrefabBinding>();
            var haptics = new ManualWeaponHapticOutput();
            controller.ConfigureHaptics(haptics);
            Assert.That(controller.InitializeForTests(), Is.True);
            var initialPosition = binding.RecoilRoot.localPosition;
            var initialRotation = binding.RecoilRoot.localRotation;

            Assert.That(controller.FireOnceForTests(), Is.True);
            yield return new WaitForSeconds(0.06f);

            Assert.That(haptics.ImpulseCount, Is.EqualTo(1));
            Assert.That(haptics.LastFrontHandHeld, Is.True);
            Assert.That(Quaternion.Angle(initialRotation, binding.RecoilRoot.localRotation), Is.InRange(2.3f, 4.5f));
            Assert.That(Vector3.Distance(initialPosition, binding.RecoilRoot.localPosition), Is.InRange(0.02f, 0.05f));

            yield return new WaitForSeconds(0.40f);
            Assert.That(Quaternion.Angle(initialRotation, binding.RecoilRoot.localRotation), Is.LessThan(0.1f));
            Assert.That(Vector3.Distance(initialPosition, binding.RecoilRoot.localPosition), Is.LessThan(0.001f));
        }

        private static GameObject Find(string id)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }

        private static int CountPlayerAudioListeners(ZeroingRangeXRModeController mode)
        {
            var noVrCount = mode.NoVrCamera.GetComponents<AudioListener>()
                .Count(listener => listener != null && listener.isActiveAndEnabled);
            var vrCount = mode.XrOrigin.GetComponentsInChildren<AudioListener>(true)
                .Count(listener => listener != null && listener.isActiveAndEnabled);
            return noVrCount + vrCount;
        }

        private static ZeroingRangeXRModeController ResolveOrCreateXRModeController()
        {
            var existing = Object.FindObjectsOfType<ZeroingRangeXRModeController>(true).FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var noVrObject = Find("ZeroingRange.Camera.NoVR");
            var xrOrigin = Find("ZeroingRange.Origin.VR");
            if (noVrObject == null || xrOrigin == null)
            {
                return null;
            }

            var mode = noVrObject.transform.parent.gameObject.AddComponent<ZeroingRangeXRModeController>();
            mode.Configure(xrOrigin, noVrObject.GetComponent<Camera>());
            return mode;
        }
    }
}
