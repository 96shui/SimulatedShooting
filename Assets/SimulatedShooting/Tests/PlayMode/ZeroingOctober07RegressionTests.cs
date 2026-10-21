using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD06 October 7 rollback, P1 task006/task012/task013.
    // Uses the saved scene and real snapshot/raycast/fire/record path, not injected hit coordinates.
    public sealed class ZeroingOctober07RegressionTests
    {
        FirstPersonTrainingWeaponController controller;
        WeaponPrefabBinding weapon;
        TargetImpactSurface target;
        ApplicationServices services;
        readonly List<ZeroingShotDto> recorded = new List<ZeroingShotDto>();
        System.IDisposable subscription;

        [UnitySetUp]
        public IEnumerator LoadCentralTarget()
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene", LoadSceneMode.Single);
            Object.FindObjectOfType<ZeroingRangeXRModeController>().SetVrModeForTests(false);
            var bootstrap = Object.FindObjectOfType<ZeroingRangeSessionBootstrap>();
            if (bootstrap == null)
                bootstrap = GameObject.Find("ZeroingRange").AddComponent<ZeroingRangeSessionBootstrap>();
            services = bootstrap.EnsureServices();
            Assert.That(services.Presentation.Enter(TrainingMode.Zeroing100m).Success, Is.True);
            Assert.That(services.Presentation.ConfirmStart("").Success, Is.True);
            controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>();
            // This fixture supplies a stable gun pose; live input must not move it between shots.
            controller.enabled = false;
            controller.ConfigureServices(services);
            Assert.That(controller.InitializeForTests(), Is.True);
            weapon = Object.FindObjectOfType<WeaponPrefabBinding>();
            target = Object.FindObjectsOfType<SceneTestId>()
                .Single(t => t.Id == "ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            var bound = typeof(FirstPersonTrainingWeaponController).GetField("targetSurface", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            Assert.That(bound, Is.SameAs(target), "P1 must score the central primary target.");
            Assert.That(target.TargetCenter.position.x, Is.EqualTo(0).Within(.001f));
            Assert.That(services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = controller.SessionId, HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true, FrontHandTracked = true, Stability01 = 1
            }).Success, Is.True);
            recorded.Clear();
            subscription = services.EventBus.Subscribe<ZeroingShotRecordedEvent>(e => recorded.Add(e.Shot));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Unsubscribe() => subscription?.Dispose();

        WeaponFireInputDto AimAndSnapshot(float xCm, float yCm)
        {
            var origin = new Vector3(0, 1.5f, 0);
            var point = target.TargetCenter.TransformPoint(new Vector3(xCm * .01f, yCm * .01f, 0));
            weapon.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(point - origin));
            weapon.transform.position += origin - weapon.MuzzlePoint.position;
            Physics.SyncTransforms();
            return (WeaponFireInputDto)typeof(FirstPersonTrainingWeaponController)
                .GetMethod("BuildFireSnapshot", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
        }

        [TestCase(0f, 0f)]
        [TestCase(20f, 0f)]
        [TestCase(-20f, 0f)]
        [TestCase(0f, 20f)]
        [TestCase(0f, -20f)]
        public void Bdd06_CentralTargetPhysicalHitAndCentimetres(float xCm, float yCm)
        {
            var offset = services.Zeroing.GetSession(controller.SessionId).Data.FixedImpactOffsetCm;
            var input = AimAndSnapshot(xCm, yCm);
            Assert.That(input.Hit, Is.True, $"Central target missed: muzzle={input.MuzzlePosition}, direction={input.AimDirection}, hit={input.HitPoint}");
            Assert.That(input.HitPoint.x, Is.EqualTo(xCm).Within(.02f));
            Assert.That(input.HitPoint.y, Is.EqualTo(yCm).Within(.02f));
            Assert.That(services.WeaponControl.Fire(input).Success, Is.True);
            Assert.That(target.Impacts.Count, Is.EqualTo(1));
            Assert.That(Vector2.Distance(target.Impacts[0].OffsetCm, new Vector2(xCm, yCm)), Is.LessThan(.03f));
            Assert.That(recorded.Count, Is.EqualTo(1));
            Assert.That(Vector2.Distance(recorded[0].ImpactPointCm, new Vector2(xCm, yCm) + offset), Is.LessThan(.03f));
        }

        [UnityTest]
        public IEnumerator Bdd06_CentralAimDefaultAdjustmentNextRoundThreeTenRingHits()
        {
            var id = controller.SessionId;
            for (var i = 0; i < 3; i++)
            {
                Assert.That(services.WeaponControl.Fire(AimAndSnapshot(0, 0)).Success, Is.True);
                // Unity removes each impact marker's temporary primitive collider at frame end.
                yield return null;
            }
            var analysis = services.Zeroing.CompleteRound(id).Data;
            Assert.That(analysis.PassedTenRing, Is.False, "Historical initial bias is outside the ten ring.");
            Assert.That(analysis.PreviewAverageOffsetCm.magnitude, Is.LessThan(.001f));
            Assert.That(services.Zeroing.ApplyAdjustment(id, 1).Success, Is.True);
            var firstApplied = services.Zeroing.GetSession(id).Data.FixedImpactOffsetCm;
            Assert.That(services.Zeroing.ApplyAdjustment(id, 1).Success, Is.True);
            Assert.That(services.Zeroing.GetSession(id).Data.FixedImpactOffsetCm, Is.EqualTo(firstApplied));
            Assert.That(services.Presentation.ContinueNextRound(id).Success, Is.True);
            for (var i = 0; i < 3; i++)
            {
                var shot = AimAndSnapshot(0, 0);
                Assert.That(shot.Hit, Is.True, "The central face must remain hittable after adjustment.");
                Assert.That(services.WeaponControl.Fire(shot).Success, Is.True);
                yield return null;
            }
            var next = services.Zeroing.CompleteRound(id).Data;
            Assert.That(next.RoundIndex, Is.EqualTo(2));
            Assert.That(next.Shots.Count, Is.EqualTo(3));
            Assert.That(next.Shots.All(s => s.InsideTenRing), Is.True);
            Assert.That(next.PassedTenRing, Is.True);
            Assert.That(target.Impacts.Count, Is.EqualTo(6));
        }

    }
}
