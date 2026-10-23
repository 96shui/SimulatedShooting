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
    // BDD30: real sight/muzzle/target-plane samples and the shared corrected shot result.
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
            var sightRotation=Quaternion.Inverse(weapon.transform.rotation)*weapon.AimLinePoint.rotation;
            weapon.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(point - origin)*Quaternion.Inverse(sightRotation));
            weapon.transform.position += origin - weapon.AimLinePoint.position;
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
            var corrected = expected + offset;
            var onFace = Mathf.Abs(corrected.x) <= 25f && Mathf.Abs(corrected.y) <= 25f;
            Assert.That(input.Hit, Is.True);
            Assert.That(input.HasTargetPlaneSample && input.TargetPlaneRayValid, Is.True);
            Assert.That(Vector2.Distance(input.TargetPlanePointCm, expected), Is.LessThan(.03f));
            Assert.That(input.HitPoint.x, Is.EqualTo(expected.x).Within(.02f));
            Assert.That(input.HitPoint.y, Is.EqualTo(expected.y).Within(.02f));
            Assert.That(Physics.Raycast(input.MuzzlePosition, input.AimDirection, out var physical, 150f), Is.True);
            Assert.That(services.WeaponControl.Fire(input).Success, Is.True);
            Assert.That(target.Impacts.Count, Is.EqualTo(onFace ? 1 : 0));
            if (onFace)
                Assert.That(Vector2.Distance(target.Impacts[0].OffsetCm, corrected), Is.LessThan(.03f));
            Assert.That(recorded.Count, Is.EqualTo(1));
            Assert.That(Vector2.Distance(recorded[0].ImpactPointCm, new Vector2(xCm, yCm) + offset), Is.LessThan(.03f));
            var tracer = Object.FindObjectOfType<BallisticTracerVisual>();
            Assert.That(tracer, Is.Not.Null);
            var end = (Vector3)typeof(BallisticTracerVisual)
                .GetField("end", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tracer);
            Assert.That(Vector3.Distance(end, target.TargetCenter.TransformPoint(new Vector3(corrected.x * .01f, corrected.y * .01f, 0))), Is.LessThan(.0003f),
                "Tracer, bullet hole and score must share the corrected service coordinate.");
        }

        [TestCase(40f, 0f)]
        [TestCase(0f, 40f)]
        [TestCase(0f, -40f)]
        public void Bdd30_OffFaceUsesSameTargetPlaneCentimetres(float xCm, float yCm)
        {
            var input = AimAndSnapshot(xCm, yCm);
            Assert.That(input.Hit, Is.False);
            Assert.That(input.HasTargetPlaneSample && input.TargetPlaneRayValid, Is.True);
            Assert.That(input.TargetPlanePointCm.x, Is.EqualTo(xCm).Within(.03f));
            Assert.That(input.TargetPlanePointCm.y, Is.EqualTo(yCm).Within(.03f));
            var bias = services.Zeroing.GetSession(controller.SessionId).Data.FixedImpactOffsetCm;
            Assert.That(services.WeaponControl.Fire(input).Success, Is.True);
            Assert.That(Vector2.Distance(recorded.Last().ImpactPointCm, new Vector2(xCm, yCm) + bias), Is.LessThan(.03f));
            Assert.That(recorded.Last().InsideTenRing, Is.False);
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
            Assert.That(snapshot.AimDirection.magnitude, Is.EqualTo(1f).Within(.00001f));
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
        public IEnumerator Bdd30_BackwardAndParallelShotsCountAsMissesWithoutTargetHoles()
        {
            foreach (var direction in new[] { Vector3.back, Vector3.right, Vector3.up })
            {
                var sightRotation=Quaternion.Inverse(weapon.transform.rotation)*weapon.AimLinePoint.rotation;
                weapon.transform.SetPositionAndRotation(new Vector3(0, 1.5f, 0), Quaternion.LookRotation(direction)*Quaternion.Inverse(sightRotation));
                Physics.SyncTransforms();
                var snapshot = (WeaponFireInputDto)typeof(FirstPersonTrainingWeaponController)
                    .GetMethod("BuildFireSnapshot", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                Assert.That(snapshot.HasTargetPlaneSample, Is.True);
                Assert.That(snapshot.TargetPlaneRayValid, Is.False);
                Assert.That(services.WeaponControl.Fire(snapshot).Success, Is.True);
                Assert.That(recorded.Last().MissedTargetPlane, Is.True);
                Assert.That(recorded.Last().InsideTenRing, Is.False);
                yield return null;
            }
            Assert.That(recorded.Count, Is.EqualTo(3));
            Assert.That(target.Impacts.Count, Is.Zero);
            Assert.That(services.Zeroing.CompleteRound(controller.SessionId).Data.PassedTenRing, Is.False);
        }

        [UnityTest]
        public IEnumerator Bdd30_TriggerVolumesAreIgnoredButSolidObstructionsRemainMisses()
        {
            var initial = AimAndSnapshot(0, 0);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = initial.MuzzlePosition + initial.AimDirection * 2f;
            blocker.transform.localScale = Vector3.one * .5f;
            try
            {
                blocker.GetComponent<Collider>().isTrigger = true;
                Physics.SyncTransforms();
                Assert.That(AimAndSnapshot(0, 0).TargetPlaneRayValid, Is.True);
                blocker.GetComponent<Collider>().isTrigger = false;
                Physics.SyncTransforms();
                var blocked = AimAndSnapshot(0, 0);
                Assert.That(blocked.TargetPlaneRayValid, Is.False);
                Assert.That(services.WeaponControl.Fire(blocked).Success, Is.True);
                Assert.That(recorded.Last().MissedTargetPlane, Is.True);
                Assert.That(target.Impacts.Count, Is.Zero);
            }
            finally { Object.Destroy(blocker); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bdd30_ManualCorrectionIsAppliedToFutureHolesAndScoreExactlyOnce()
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
            Assert.That(Vector2.Distance(recorded.Last().ImpactPointCm, preview), Is.LessThan(.03f));
            Assert.That(Vector2.Distance(target.Impacts.Last().OffsetCm, preview), Is.LessThan(.03f));
            Assert.That(recorded.Last().InsideTenRing, Is.False, "Do not force an off-centre shot into the ten ring.");
        }

        [UnityTest]
        public IEnumerator Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles()
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
            Assert.That(next.PassedTenRing, Is.True);
            Assert.That(next.AverageOffsetCm.magnitude, Is.LessThan(.03f));
            Assert.That(target.Impacts.Count, Is.EqualTo(6));
            Assert.That(target.Impacts.Skip(3).All(s => s.InsideTenRing), Is.True,
                "The archived physical hits remain where the gun was aimed.");
            TestContext.WriteLine($"First mean={analysis.AverageOffsetCm}; correction={analysis.ProposedCorrectionCm}; " +
                $"next mean={next.AverageOffsetCm}; service ten-ring={next.PassedTenRing}; " +
                $"visible next-round ten-ring={target.Impacts.Skip(3).All(s => s.InsideTenRing)}");
        }



    }
}
