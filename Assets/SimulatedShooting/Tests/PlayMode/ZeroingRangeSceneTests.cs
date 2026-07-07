using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Common;
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
            Assert.That(materials.Length, Is.LessThanOrEqualTo(20));
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
            Assert.That(surface.Impacts.Count, Is.EqualTo(initialImpacts + 1));
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

        private static GameObject Find(string id)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }
    }
}
