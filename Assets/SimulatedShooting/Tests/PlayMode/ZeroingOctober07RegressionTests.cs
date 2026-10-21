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
    // BDD06: physical zeroing from the c57823f base, current chest target/UI.
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
            var expected = new Vector2(xCm, yCm);
            var onFace = true;
            Assert.That(input.Hit, Is.EqualTo(onFace));
            Assert.That(input.HitPoint.x, Is.EqualTo(expected.x).Within(.02f));
            Assert.That(input.HitPoint.y, Is.EqualTo(expected.y).Within(.02f));
            Assert.That(Physics.Raycast(input.MuzzlePosition, input.AimDirection, out var physical, 150f), Is.True);
            Assert.That(services.WeaponControl.Fire(input).Success, Is.True);
            Assert.That(target.Impacts.Count, Is.EqualTo(onFace ? 1 : 0));
            if (onFace)
                Assert.That(Vector2.Distance(target.Impacts[0].OffsetCm, expected), Is.LessThan(.03f));
            Assert.That(recorded.Count, Is.EqualTo(1));
            Assert.That(Vector2.Distance(recorded[0].ImpactPointCm, new Vector2(xCm, yCm) + offset), Is.LessThan(.03f));
            var tracer = Object.FindObjectOfType<BallisticTracerVisual>();
            Assert.That(tracer, Is.Not.Null);
            var end = (Vector3)typeof(BallisticTracerVisual)
                .GetField("end", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tracer);
            Assert.That(Vector3.Distance(end, physical.point), Is.LessThan(.0003f),
                "The archived tracer must end at the physical raycast hit.");
        }

        [TestCase(40f, 0f)]
        [TestCase(0f, 40f)]
        [TestCase(0f, -40f)]
        public void September25_OffFaceSnapshotUsesArchivedPhysicalHit(float xCm, float yCm)
        {
            var input = AimAndSnapshot(xCm, yCm);
            Assert.That(input.Hit, Is.False);
            var hit = Physics.Raycast(input.MuzzlePosition, input.AimDirection, out var physical, 150f);
            var expected = hit ? physical.point : input.MuzzlePosition + input.AimDirection * ZeroingRules.DistanceMeters;
            Assert.That(Vector3.Distance(input.HitPoint, expected), Is.LessThan(.0003f));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(8f, 25f, 15f)]
        [TestCase(-12f, -20f, -30f)]
        public void September25_ShotAndTracerUseFrozenActualMuzzlePosition(float pitch, float yaw, float roll)
        {
            weapon.transform.SetPositionAndRotation(new Vector3(.3f, 1.4f, .5f), Quaternion.Euler(pitch, yaw, roll));
            weapon.RecoilRoot.localRotation = Quaternion.Euler(-2f, 1f, 3f);
            Physics.SyncTransforms();
            var snapshot = (WeaponFireInputDto)typeof(FirstPersonTrainingWeaponController)
                .GetMethod("BuildFireSnapshot", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            var muzzle = weapon.MuzzlePoint.position;
            Assert.That(Vector3.Distance(snapshot.MuzzlePosition, muzzle), Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(snapshot.AimDirection, weapon.AimLinePoint.forward.normalized), Is.LessThan(.00001f));
            Assert.That(services.WeaponControl.Fire(snapshot).Success, Is.True);
            weapon.transform.position += Vector3.right;
            Assert.That(Vector3.Distance(controller.LastShotMuzzlePosition, muzzle), Is.LessThan(.00001f));
            var tracer = Object.FindObjectOfType<BallisticTracerVisual>();
            var start = (Vector3)typeof(BallisticTracerVisual)
                .GetField("start", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tracer);
            Assert.That(Vector3.Distance(start, muzzle), Is.LessThan(.00001f),
                "Moving the weapon after firing must not move the recorded bullet origin.");
        }

        [UnityTest]
        public IEnumerator September25_ManualPreviewDoesNotMovePhysicalBulletHoles()
        {
            var id = controller.SessionId;
            for (var i = 0; i < 3; i++)
            {
                Assert.That(services.WeaponControl.Fire(AimAndSnapshot(0, 0)).Success, Is.True);
                yield return null;
            }
            Assert.That(services.Zeroing.AdjustImpactPoint(id, 1, ZeroingAdjustmentAxis.Horizontal, 1).Success, Is.True);
            for (var i = 0; i < 100; i++)
                Assert.That(services.Zeroing.AdjustImpactPoint(id, 1, ZeroingAdjustmentAxis.Vertical, 1).Success, Is.True);
            var before = services.Zeroing.CompleteRound(id).Data;
            var preview = before.PreviewAverageOffsetCm;
            Assert.That(preview.x, Is.EqualTo(2f).Within(.001f));
            Assert.That(preview.y, Is.EqualTo(6.4f).Within(.01f));
            Assert.That(services.Zeroing.ApplyAdjustment(id, 1).Success, Is.True);
            Assert.That(services.Presentation.ContinueNextRound(id).Success, Is.True);
            Assert.That(services.WeaponControl.Fire(AimAndSnapshot(0, 0)).Success, Is.True);
            Assert.That(Vector2.Distance(recorded.Last().ImpactPointCm, before.AverageOffsetCm), Is.LessThan(.03f));
            Assert.That(target.Impacts.Last().OffsetCm.magnitude, Is.LessThan(.03f));
            Assert.That(recorded.Last().InsideTenRing, Is.False, "Do not force an off-centre shot into the ten ring.");
        }

        [UnityTest]
        public IEnumerator September25_CentralAimKeepsPhysicalHitsAndArchivedAdjustmentBookkeeping()
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
            Assert.That(next.PassedTenRing, Is.False);
            Assert.That(Vector2.Distance(next.AverageOffsetCm, analysis.AverageOffsetCm), Is.LessThan(.03f));
            Assert.That(target.Impacts.Count, Is.EqualTo(6));
            Assert.That(target.Impacts.Skip(3).All(s => s.InsideTenRing), Is.True,
                "The archived physical hits remain where the gun was aimed.");
            TestContext.WriteLine($"First mean={analysis.AverageOffsetCm}; correction={analysis.ProposedCorrectionCm}; " +
                $"next mean={next.AverageOffsetCm}; service ten-ring={next.PassedTenRing}; " +
                $"visible next-round ten-ring={target.Impacts.Skip(3).All(s => s.InsideTenRing)}");
        }



    }
}
