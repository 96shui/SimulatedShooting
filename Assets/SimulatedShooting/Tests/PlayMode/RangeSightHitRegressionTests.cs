using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD30 + BDD09, task007: real production rifle/snapshot/raycast/burst/score path.
    public sealed class RangeSightHitRegressionTests
    {
        [UnityTest]
        public IEnumerator Bdd30_P2SightedShotsHitAcrossTheRouteUsingEachCurrentSnapshot()
            => VerifySight(false);

        [UnityTest]
        public IEnumerator Bdd31_P2SecondShotRecoversWithoutManuallyResettingRecoil()
            => VerifySight(true);

        IEnumerator VerifySight(bool preserveRecoil)
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("MovingTargetRangeScene");
            Object.FindObjectOfType<ZeroingRangeXRModeController>().SetVrModeForTests(false);
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>();
            controller.enabled = false;
            var services = ApplicationServices.CreateDefault();
            Assert.That(services.Presentation.Enter(TrainingMode.MovingTarget).Success, Is.True);
            var started = services.MovingTarget.StartSession(new MovingTargetSettingsDto { SpeedMetersPerSecond = 4 }, RandomSeed.Fixed(30));
            Assert.That(started.Success, Is.True, started.Message);
            var session = started.Data.SessionId;
            var confirmed = services.Presentation.ConfirmStart(session);
            Assert.That(confirmed.Success, Is.True, confirmed.Message);
            controller.ConfigureServices(services);
            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = session, HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true, FrontHandTracked = true, Stability01 = 1
            }).Success, Is.True);
            services.MovingTargetProgress.Tick(session, 3f);
            var route = Object.FindObjectOfType<MovingTargetRouteBinding>();
            var weapon = Object.FindObjectOfType<WeaponPrefabBinding>();
            var head = Object.FindObjectOfType<Camera>().transform.position;
            var snapshotMethod = typeof(FirstPersonTrainingWeaponController).GetMethod("BuildFireSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
            WeaponFireInputDto Aim(float progress)
            {
                route.ApplyNormalizedProgress(progress);
                if (!preserveRecoil) weapon.RecoilRoot.localRotation = Quaternion.identity;
                var sightRotation=Quaternion.Inverse(weapon.transform.rotation)*weapon.AimLinePoint.rotation;
                weapon.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(route.TargetCenter.position - head)*Quaternion.Inverse(sightRotation));
                weapon.transform.position += head - weapon.AimLinePoint.position;
                Physics.SyncTransforms();
                var snapshot = (WeaponFireInputDto)snapshotMethod.Invoke(controller, null);
                Assert.That(snapshot.Hit, Is.True, "Sighted target missed at progress " + progress);
                var local = route.TargetCenter.InverseTransformPoint(snapshot.HitPoint);
                Assert.That(new Vector2(local.x, local.y).magnitude, Is.LessThan(.02f));
                return snapshot;
            }
            for (var i = 0; i < 5; i++)
            {
                var progress = .05f + i * .18f;
                var first = services.WeaponFire.Tick(session, 0,
                    new WeaponTriggerStateInputDto { SessionId = session, Pressed = true, Held = true }, Aim(progress));
                Assert.That(first.Success, Is.True, first.Message);
                if (preserveRecoil)
                {
                    for (var step = 0; step < 10; step++) controller.SendMessage("UpdateRecoil", .01f);
                    TestContext.WriteLine("Range recoil angle at the next-shot deadline: " + Quaternion.Angle(Quaternion.identity, weapon.RecoilRoot.localRotation));
                }
                var second = services.WeaponFire.Tick(session, .12f,
                    new WeaponTriggerStateInputDto { SessionId = session, Released = true }, Aim(progress + .06f));
                Assert.That(second.Success, Is.True, second.Message);
            }
            var state = services.MovingTarget.GetSession(session);
            Assert.That(state.Data.HitCount, Is.EqualTo(10));
            Assert.That(services.WeaponControl.GetState(session).Data.CurrentMagazine, Is.Zero);
            foreach (var staticTarget in Object.FindObjectsOfType<TargetImpactSurface>())
                Assert.That(staticTarget.Impacts.Count, Is.Zero, "P2 must not put a moving-target impact on a static P1 target.");
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
        }
    }
}
