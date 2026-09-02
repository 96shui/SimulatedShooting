using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;

namespace VRShooting.Tests.PlayMode.Flow
{
    /// <summary>
    /// 无 VR 替身跑通 P2 快速两发与长按耗尽。追溯 docs/BDD/screens/09-移动靶HUD.feature.md。
    /// </summary>
    [TestFixture]
    public sealed class Screen09_AutoFireNoVrPlayModeTests
    {
        [UnityTest]
        public IEnumerator Screen09_NoVrInput_QuickTapFiresTwoShots()
        {
            var input = new ManualXRTrainingInput();
            var services = ApplicationServices.CreateDefault(input);
            var sessionId = StartLiveMovingTarget(services);

            services.MovingTargetProgress.Tick(sessionId, 3f);
            input.Press(XRTrainingInputButton.Trigger);
            input.Release(XRTrainingInputButton.Trigger);
            services.WeaponFire.Tick(
                sessionId,
                0.2f,
                new WeaponTriggerStateInputDto
                {
                    SessionId = sessionId,
                    Pressed = true,
                    Released = true
                },
                Snapshot(sessionId));

            yield return null;

            var moving = services.MovingTarget.GetSession(sessionId);
            Assert.IsTrue(moving.Success, moving.Message);
            Assert.AreEqual(2, moving.Data.ShotsFired);
            Assert.AreEqual(8, services.Ammo.GetAmmo(sessionId).Data.CurrentMagazine);
        }

        [UnityTest]
        public IEnumerator Screen09_NoVrInput_HeldTriggerConsumesTenRounds()
        {
            var input = new ManualXRTrainingInput();
            var services = ApplicationServices.CreateDefault(input);
            var sessionId = StartLiveMovingTarget(services);

            services.MovingTargetProgress.Tick(sessionId, 3f);
            services.WeaponFire.Tick(
                sessionId,
                2f,
                new WeaponTriggerStateInputDto
                {
                    SessionId = sessionId,
                    Pressed = true,
                    Held = true,
                    Value01 = 1f
                },
                Snapshot(sessionId));

            yield return null;

            Assert.AreEqual(10, services.MovingTarget.GetSession(sessionId).Data.ShotsFired);
            Assert.AreEqual(0, services.Ammo.GetAmmo(sessionId).Data.CurrentMagazine);
        }

        static string StartLiveMovingTarget(ApplicationServices services)
        {
            services.Presentation.Enter(TrainingMode.MovingTarget);
            var started = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f },
                RandomSeed.Fixed(9));
            Assert.IsTrue(started.Success, started.Message);
            var sessionId = started.Data.SessionId;
            Assert.IsTrue(services.Presentation.ConfirmStart(sessionId).Success);
            Assert.IsTrue(services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.9f
            }).Success);
            return sessionId;
        }

        static WeaponTriggerStateInputDto Idle(string sessionId)
        {
            return new WeaponTriggerStateInputDto { SessionId = sessionId };
        }

        static WeaponFireInputDto Snapshot(string sessionId)
        {
            return new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = UnityEngine.Vector3.zero,
                RawAimDirection = UnityEngine.Vector3.forward,
                AimDirection = UnityEngine.Vector3.forward,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = true,
                HitPoint = UnityEngine.Vector3.forward * 20f,
                HitObjectId = "MovingTargetRange.Target.Face"
            };
        }
    }
}
