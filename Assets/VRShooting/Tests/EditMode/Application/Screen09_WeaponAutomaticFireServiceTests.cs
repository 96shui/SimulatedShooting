using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// P2 两发起射/长按连射与 P1 单发回归。
    /// 追溯 docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md、09-移动靶HUD.feature.md。
    /// </summary>
    [TestFixture]
    public sealed class Screen09_WeaponAutomaticFireServiceTests
    {
        const string SessionId = "task007-autofire";
        static readonly WeaponAutoFireConfigDto P2Config = new WeaponAutoFireConfigDto
        {
            InitialShotCount = 2,
            ShotIntervalSeconds = 0.1f
        };

        WeaponControlService weapons;
        WeaponAutomaticFireService autoFire;
        List<WeaponShotResultDto> shots;
        List<WeaponFireSequenceStateDto> sequences;

        [SetUp]
        public void SetUp()
        {
            var eventBus = new GameEventBus();
            weapons = new WeaponControlService(eventBus);
            autoFire = new WeaponAutomaticFireService(weapons, weapons, eventBus);
            shots = new List<WeaponShotResultDto>();
            sequences = new List<WeaponFireSequenceStateDto>();
            eventBus.Subscribe<WeaponShotResultEvent>(evt =>
            {
                if (evt.Result.IsValidShot)
                {
                    shots.Add(evt.Result);
                }
            });
            eventBus.Subscribe<WeaponFireSequenceChangedEvent>(evt => sequences.Add(evt.State));
        }

        [Test]
        public void Screen09_QuickPressRelease_FiresExactlyTwoShotsWithOneSequence()
        {
            StartMovingTargetWeapon();

            autoFire.UpdateTrigger(PressAndRelease());
            var result = autoFire.Tick(SessionId, 0.2f, Snapshot());

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(2, shots.Count);
            Assert.AreEqual(2, result.Data.ShotsFired);
            Assert.AreEqual(shots[0].ShotSequence, 1);
            Assert.AreEqual(shots[1].ShotSequence, 2);
            Assert.AreEqual(result.Data.SequenceId, sequences[0].SequenceId);
            Assert.AreEqual(WeaponFireSequencePhase.Stopped, result.Data.Phase);
            Assert.AreEqual(WeaponFireStopReason.TriggerReleased, result.Data.StopReason);
            Assert.AreEqual(8, weapons.GetAmmo(SessionId).Data.CurrentMagazine);
        }

        [Test]
        public void Screen09_HeldPastSecondShot_ContinuesThenStopsOnRelease()
        {
            StartMovingTargetWeapon();

            autoFire.UpdateTrigger(Press());
            autoFire.Tick(SessionId, 0.25f, Snapshot());
            Assert.AreEqual(3, shots.Count);
            Assert.AreEqual(WeaponFireSequencePhase.ContinuousFire, autoFire.GetState(SessionId).Data.Phase);

            autoFire.UpdateTrigger(Release());
            var stopped = autoFire.Tick(SessionId, 0.2f, Snapshot());

            Assert.AreEqual(3, shots.Count);
            Assert.AreEqual(WeaponFireSequencePhase.Stopped, stopped.Data.Phase);
            Assert.AreEqual(WeaponFireStopReason.TriggerReleased, stopped.Data.StopReason);
        }

        [Test]
        public void Screen09_HeldUntilEmpty_FiresExactlyTenAndNeverGoesNegative()
        {
            StartMovingTargetWeapon();

            autoFire.UpdateTrigger(Press());
            var result = autoFire.Tick(SessionId, 2f, Snapshot());

            Assert.AreEqual(10, shots.Count);
            Assert.AreEqual(10, result.Data.ShotsFired);
            Assert.AreEqual(0, weapons.GetAmmo(SessionId).Data.CurrentMagazine);
            Assert.AreEqual(WeaponFireStopReason.AmmoDepleted, result.Data.StopReason);
            Assert.That(weapons.GetAmmo(SessionId).Data.CurrentMagazine, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Screen09_NewSequenceWithOneAmmo_RejectsEntireBurst()
        {
            StartMovingTargetWeapon();
            weapons.ConsumeAmmo(SessionId, 9);

            autoFire.UpdateTrigger(Press());
            var result = autoFire.Tick(SessionId, 0.2f, Snapshot());

            Assert.AreEqual(0, shots.Count);
            Assert.AreEqual(1, weapons.GetAmmo(SessionId).Data.CurrentMagazine);
            Assert.AreEqual(WeaponFireSequencePhase.Idle, result.Data.Phase);
        }

        [Test]
        public void Screen09_CancelForForbidden_DoesNotResumeWhileHeld()
        {
            StartMovingTargetWeapon();
            autoFire.UpdateTrigger(Press());
            autoFire.Tick(SessionId, 0.25f, Snapshot());
            var fired = shots.Count;
            Assert.That(fired, Is.GreaterThanOrEqualTo(2));

            autoFire.Cancel(SessionId, WeaponFireStopReason.ShootingBecameForbidden);
            Assert.IsFalse(autoFire.GetState(SessionId).Data.TriggerArmedForNewSequence);

            autoFire.UpdateTrigger(Hold());
            autoFire.Tick(SessionId, 0.5f, Snapshot());
            Assert.AreEqual(fired, shots.Count);

            autoFire.UpdateTrigger(Release());
            autoFire.Tick(SessionId, 0f, Snapshot());
            autoFire.UpdateTrigger(Press());
            autoFire.Tick(SessionId, 0.2f, Snapshot());
            Assert.That(shots.Count, Is.GreaterThan(fired));
        }

        [Test]
        public void Screen09_LargeTick_CatchesUpWithoutExceedingAmmo()
        {
            StartMovingTargetWeapon();
            autoFire.UpdateTrigger(Press());
            autoFire.Tick(SessionId, 100f, Snapshot());
            autoFire.Tick(SessionId, 100f, Snapshot());

            Assert.AreEqual(10, shots.Count);
            Assert.AreEqual(0, weapons.GetAmmo(SessionId).Data.CurrentMagazine);
        }

        [Test]
        public void Screen09_RepeatedZeroTick_IsIdempotent()
        {
            StartMovingTargetWeapon();
            autoFire.UpdateTrigger(PressAndRelease());
            autoFire.Tick(SessionId, 0.2f, Snapshot());
            var count = shots.Count;
            var sequenceId = autoFire.GetState(SessionId).Data.SequenceId;

            autoFire.Tick(SessionId, 0f, Snapshot());
            autoFire.Tick(SessionId, 0f, Snapshot());

            Assert.AreEqual(count, shots.Count);
            Assert.AreEqual(sequenceId, autoFire.GetState(SessionId).Data.SequenceId);
        }

        [Test]
        public void Screen09_InvalidConfig_IsRejected()
        {
            weapons.StartSession(SessionId, WeaponControlService.TrainingRifleId, TrainingMode.MovingTarget);
            var badCount = autoFire.StartSession(SessionId, WeaponFireMode.InitialTwoThenAutomatic, new WeaponAutoFireConfigDto
            {
                InitialShotCount = 3,
                ShotIntervalSeconds = 0.1f
            });
            var badInterval = autoFire.StartSession(SessionId, WeaponFireMode.InitialTwoThenAutomatic, new WeaponAutoFireConfigDto
            {
                InitialShotCount = 2,
                ShotIntervalSeconds = 0f
            });

            Assert.AreEqual(ErrorCode.InvalidInput, badCount.ErrorCode);
            Assert.AreEqual(ErrorCode.InvalidInput, badInterval.ErrorCode);
        }

        [Test]
        public void Screen00_P1HeldTrigger_FiresOnlyOneShotPerPress()
        {
            StartZeroingWeapon();
            autoFire.StartSession(SessionId, WeaponFireMode.SingleShot, WeaponAutoFireConfigDto.P1SingleShot);

            autoFire.UpdateTrigger(Press());
            autoFire.Tick(SessionId, 1f, Snapshot());
            Assert.AreEqual(1, shots.Count);

            autoFire.Tick(SessionId, 1f, Snapshot());
            Assert.AreEqual(1, shots.Count);

            autoFire.UpdateTrigger(Release());
            autoFire.Tick(SessionId, 0f, Snapshot());
            autoFire.UpdateTrigger(Press());
            autoFire.Tick(SessionId, 0f, Snapshot());
            Assert.AreEqual(2, shots.Count);
        }

        [Test]
        public void Task007_ReserveAmmo_IsAtomicAndReleasedOnCancel()
        {
            StartMovingTargetWeapon();
            var reserved = weapons.ReserveAmmo(SessionId, 2, "seq-test");
            Assert.IsTrue(reserved.Success, reserved.Message);
            Assert.AreEqual(10, weapons.GetAmmo(SessionId).Data.CurrentMagazine);

            var tooMuch = weapons.ReserveAmmo(SessionId, 9, "seq-other");
            Assert.IsFalse(tooMuch.Success);

            weapons.ReleaseAmmoReservation(SessionId, "seq-test");
            var second = weapons.ReserveAmmo(SessionId, 2, "seq-2");
            Assert.IsTrue(second.Success, second.Message);
        }

        void StartMovingTargetWeapon()
        {
            Assert.IsTrue(weapons.StartSession(SessionId, WeaponControlService.TrainingRifleId, TrainingMode.MovingTarget).Success);
            Assert.IsTrue(weapons.SetGripState(TwoHandGrip()).Success);
            Assert.IsTrue(autoFire.StartSession(SessionId, WeaponFireMode.InitialTwoThenAutomatic, P2Config).Success);
        }

        void StartZeroingWeapon()
        {
            Assert.IsTrue(weapons.StartSession(SessionId, WeaponControlService.TrainingRifleId, TrainingMode.Zeroing100m).Success);
            Assert.IsTrue(weapons.SetGripState(TwoHandGrip()).Success);
        }

        static WeaponGripStateInputDto TwoHandGrip()
        {
            return new WeaponGripStateInputDto
            {
                SessionId = SessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.9f
            };
        }

        static WeaponTriggerStateInputDto Press()
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = SessionId,
                Value01 = 1f,
                Pressed = true,
                Held = true
            };
        }

        static WeaponTriggerStateInputDto Hold()
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = SessionId,
                Value01 = 1f,
                Held = true
            };
        }

        static WeaponTriggerStateInputDto Release()
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = SessionId,
                Released = true
            };
        }

        static WeaponTriggerStateInputDto PressAndRelease()
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = SessionId,
                Value01 = 0f,
                Pressed = true,
                Held = false,
                Released = true
            };
        }

        static WeaponFireInputDto Snapshot(bool hit = true)
        {
            return new WeaponFireInputDto
            {
                SessionId = SessionId,
                MuzzlePosition = Vector3.zero,
                RawAimDirection = Vector3.forward,
                AimDirection = Vector3.forward,
                WeaponPosition = Vector3.zero,
                Stability01 = 0.9f,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = hit,
                HitPoint = Vector3.forward * 40f,
                HitObjectId = hit ? "MovingTargetRange.Target.Face" : string.Empty
            };
        }
    }
}
