using System;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application.Combat;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;
using VRShooting.Input;
using VRShooting.Application;

namespace VRShooting.Tests.EditMode
{
    // task020 / BDD23: 三个弹匣与动作结束装填、提前换弹与无效请求、取消换弹与新局复位.
    public sealed class CombatMagazineReloadTests
    {
        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void ThreeMagazinesRunOutAfterNinetyShots(TrainingMode mode)
        {
            var clock = new FakeCombatClock();
            using (var core = Start(clock, mode))
            {
                for (int magazine = 0; magazine < 3; magazine++)
                {
                    Assert.That(core.GetSnapshot("reload").Data.Ammo.ReserveMagazines, Is.EqualTo(2 - magazine));
                    for (int shot = 0; shot < 30; shot++) Assert.That(Fire(core).Success);
                    Assert.That(Fire(core).Success, Is.False);
                    if (magazine == 2) break;
                    Assert.That(core.Reload("reload").Success);
                    var started = core.GetSnapshot("reload").Data;
                    Assert.That(started.ReloadStartedAt, Is.EqualTo(clock.Now));
                    Assert.That(started.ReloadDurationSeconds, Is.EqualTo(2));
                    Assert.That(Fire(core).Success, Is.False);
                    Assert.That(core.Reload("reload").ErrorCode, Is.EqualTo(ErrorCode.Busy));
                    clock.Advance(1.99); core.Advance("reload");
                    Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.Zero);
                    clock.Advance(.01); core.Advance("reload");
                    Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(30));
                }
                Assert.That(core.GetSnapshot("reload").Data.Ammo.ReserveAmmo, Is.Zero);
                Assert.That(core.Reload("reload").ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            }
        }

        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void TacticalReloadSpendsAWholeMagazineOnlyAtCompletion(TrainingMode mode)
        {
            var clock = new FakeCombatClock();
            using (var core = Start(clock, mode))
            {
                Assert.That(core.Reload("reload").Success); // Full: no animation and no consumption.
                Assert.That(core.GetSnapshot("reload").Data.Ammo.IsReloading, Is.False);
                Fire(core); Assert.That(core.Reload("reload").Success);
                Assert.That(core.GetSnapshot("reload").Data.Ammo.ReserveAmmo, Is.EqualTo(60));
                clock.Advance(2); core.Advance("reload");
                Assert.That(core.GetSnapshot("reload").Data.Ammo.ReserveAmmo, Is.EqualTo(30));
                Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(30));
                core.Advance("reload");
                Assert.That(core.GetSnapshot("reload").Data.Ammo.ReserveAmmo, Is.EqualTo(30));
            }
        }

        [TestCase(SessionState.Paused)] [TestCase(SessionState.Completed)]
        [TestCase(SessionState.Failed)] [TestCase(SessionState.Cancelled)]
        public void CancellationPreservesOriginalMagazineAndBothSpares(SessionState state)
        {
            var clock = new FakeCombatClock();
            using (var core = Start(clock, TrainingMode.Trench))
            {
                Fire(core); core.Reload("reload"); core.SetState("reload", state);
                clock.Advance(100); core.Advance("reload");
                var ammo = core.GetSnapshot("reload").Data.Ammo;
                Assert.That(ammo.CurrentMagazine, Is.EqualTo(29)); Assert.That(ammo.ReserveAmmo, Is.EqualTo(60));
                Assert.That(ammo.IsReloading, Is.False);
                core.Start("new-round", TrainingMode.Trench, Array.Empty<CombatEntityVisualDto>());
                Assert.That(core.GetSnapshot("new-round").Data.Ammo.CurrentMagazine, Is.EqualTo(30));
                Assert.That(core.GetSnapshot("new-round").Data.Ammo.ReserveMagazines, Is.EqualTo(2));
                Assert.That(core.Reload("reload").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            }
        }

        [Test] public void TrackingLossCancelsWithoutDelayedCompletion()
        {
            var clock = new FakeCombatClock();
            using (var core = Start(clock, TrainingMode.Urban))
            {
                Fire(core); core.Reload("reload"); core.SetTracking("reload", false);
                clock.Advance(20); core.SetTracking("reload", true); core.Advance("reload");
                Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(29));
                Assert.That(core.GetSnapshot("reload").Data.Ammo.ReserveMagazines, Is.EqualTo(2));
                Assert.That(core.GetSnapshot("reload").Data.Ammo.IsReloading, Is.False);
            }
        }

        [Test] public void P1AndP2KeepTheirAmmoRules()
        {
            var weapons = new WeaponControlService();
            var p1 = weapons.StartSession("p1", "training-rifle", TrainingMode.Zeroing100m).Data;
            var p2 = weapons.StartSession("p2", "training-rifle", TrainingMode.MovingTarget).Data;
            Assert.That(p1.CurrentMagazine, Is.EqualTo(4)); Assert.That(p1.ReserveAmmo, Is.EqualTo(8));
            Assert.That(p2.CurrentMagazine, Is.EqualTo(10)); Assert.That(p2.ReserveAmmo, Is.Zero);
        }

        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void ReloadInputLocksSupportGripInBothModes(TrainingMode mode)
        {
            var clock = new FakeCombatClock();
            using var core = Start(clock, mode);
            var input = new ManualXRTrainingInput();
            using var controller = new CombatInputController("reload", mode, core, input, clock, new NoHit(), new NoMove());
            var frame = new CombatInputFrameDto { HeadTracked = true, RearHandTracked = true, FrontHandTracked = true,
                RearGripInRange = true, FrontGripInRange = true, IsRealVr = true,
                AimDirection = Vector3.forward, PlayerForward = Vector3.forward };
            void Step(double seconds = .01)
            {
                clock.Advance(seconds); Assert.That(controller.Step(frame, (float)seconds).Success); input.AdvanceFrame();
            }
            input.Press(XRTrainingInputButton.RightGrip); input.Press(XRTrainingInputButton.LeftGrip); Step();
            Fire(core); input.Press(XRTrainingInputButton.Reload); Step();
            Assert.That(core.GetSnapshot("reload").Data.Ammo.IsReloading, Is.True);
            Assert.That(core.GetSnapshot("reload").Data.Weapon.TwoHandGripActive, Is.False);
            input.Release(XRTrainingInputButton.LeftGrip); Step(.2);
            input.Press(XRTrainingInputButton.LeftGrip); input.Press(XRTrainingInputButton.Trigger); Step(.2);
            Assert.That(core.GetSnapshot("reload").Data.Weapon.TwoHandGripActive, Is.False);
            Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            Step(2);
            Assert.That(core.GetSnapshot("reload").Data.Ammo.IsReloading, Is.False);
            Assert.That(core.GetSnapshot("reload").Data.Weapon.TwoHandGripActive, Is.False);
            input.Release(XRTrainingInputButton.LeftGrip); input.Release(XRTrainingInputButton.Trigger); Step();
            input.Press(XRTrainingInputButton.LeftGrip); Step();
            Assert.That(core.GetSnapshot("reload").Data.Weapon.TwoHandGripActive, Is.True);
            input.Press(XRTrainingInputButton.Trigger); Step();
            Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(29));
        }
        // BDD23 / task004: P3 uses the same two-shot and held-fire scheduler as P2.
        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void AutomaticFireUsesP2TimingAndLatestShotForEveryBullet(TrainingMode mode)
        {
            var clock = new FakeCombatClock();
            using var core = Start(clock, mode);
            var interval = WeaponAutoFireConfigDto.P2Default.ShotIntervalSeconds;
            Assert.That(core.GetSnapshot("reload").Data.Weapon.FireMode, Is.EqualTo(WeaponFireMode.InitialTwoThenAutomatic));
            System.Collections.Generic.IReadOnlyList<CombatShotDto> Tick(float dt, bool press, bool held, bool release, Vector3 direction)
            {
                clock.Advance(dt);
                var result = core.TickAutomaticFire(new WeaponTriggerStateInputDto { SessionId = "reload", Pressed = press, Held = held, Released = release },
                    dt, new WeaponFireInputDto { SessionId = "reload", AimDirection = direction, RawAimDirection = direction }, true);
                Assert.That(result.Success, Is.True, result.Message);
                return result.Data;
            }
            Assert.That(Tick(0, true, true, false, Vector3.forward).Count, Is.EqualTo(1));
            Assert.That(Tick(interval * .5f, false, false, true, Vector3.right).Count, Is.Zero);
            var second = Tick(interval * .5f, false, false, false, Vector3.right);
            Assert.That(second.Count, Is.EqualTo(1));
            Assert.That(second[0].Shot.AimDirection, Is.EqualTo(Vector3.right));
            Assert.That(Tick(interval * 3, false, false, false, Vector3.forward).Count, Is.Zero);
            Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(28));
            Assert.That(Tick(0, true, true, false, Vector3.forward).Count, Is.EqualTo(1));
            Assert.That(Tick(interval, false, true, false, Vector3.forward).Count, Is.EqualTo(1));
            Assert.That(Tick(interval, false, true, false, Vector3.forward).Count, Is.EqualTo(1));
            Assert.That(Tick(interval, false, false, true, Vector3.forward).Count, Is.Zero);
        }

        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(TrainingMode mode)
        {
            var clock = new FakeCombatClock();
            using var core = Start(clock, mode);
            var interval = WeaponAutoFireConfigDto.P2Default.ShotIntervalSeconds;
            System.Collections.Generic.IReadOnlyList<CombatShotDto> Tick(float dt, bool press, bool held, bool release)
            {
                clock.Advance(dt);
                var r = core.TickAutomaticFire(new WeaponTriggerStateInputDto { SessionId = "reload", Pressed = press, Held = held, Released = release },
                    dt, new WeaponFireInputDto { SessionId = "reload", AimDirection = Vector3.forward, RawAimDirection = Vector3.forward }, true);
                Assert.That(r.Success, Is.True, r.Message); return r.Data;
            }
            Assert.That(Tick(0, true, true, false).Count, Is.EqualTo(1));
            Assert.That(core.Reload("reload").Success, Is.True);
            Assert.That(Tick(interval, false, true, false).Count, Is.Zero);
            clock.Advance(2); core.Advance("reload");
            Assert.That(Tick(interval, false, true, false).Count, Is.Zero, "held trigger must not restart after reload");
            Tick(0, false, false, true);
            for (var i = 0; i < 29; i++) Assert.That(Fire(core).Success, Is.True);
            Assert.That(Tick(0, true, true, false).Count, Is.Zero, "one remaining round cannot start a P2 sequence");
            Assert.That(core.GetSnapshot("reload").Data.Ammo.CurrentMagazine, Is.EqualTo(1));
        }

        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void AlreadyReleasedTriggerRearmsWhenReloadCancelsPendingBurst(TrainingMode mode)
        {
            var clock = new FakeCombatClock(); using var core = Start(clock, mode);
            System.Collections.Generic.IReadOnlyList<CombatShotDto> Tick(float dt, bool pressed, bool held, bool released)
            {
                clock.Advance(dt);
                var result = core.TickAutomaticFire(new WeaponTriggerStateInputDto { SessionId = "reload", Pressed = pressed, Held = held, Released = released },
                    dt, new WeaponFireInputDto { SessionId = "reload", AimDirection = Vector3.forward, RawAimDirection = Vector3.forward }, true);
                Assert.That(result.Success, Is.True); return result.Data;
            }
            Assert.That(Tick(0, true, true, false).Count, Is.EqualTo(1));
            Assert.That(Tick(.01f, false, false, true).Count, Is.Zero);
            Assert.That(core.Reload("reload").Success, Is.True);
            Tick(.01f, false, false, false);
            clock.Advance(2); core.Advance("reload");
            Assert.That(Tick(0, true, true, false).Count, Is.EqualTo(1), "a trigger released before reload should be ready on the first new press");
        }

        sealed class NoHit : ICombatShotQuery
        { public CombatRayHitDto Cast(Vector3 origin, Vector3 direction) => new CombatRayHitDto(); }
        sealed class NoMove : ICombatLocomotionPort
        { public ServiceResult<Unit> Apply(CombatLocomotionIntentDto intent) => ServiceResult<Unit>.Ok(Unit.Value); }

        static CombatCoreService Start(FakeCombatClock clock, TrainingMode mode)
        {
            var core = new CombatCoreService(clock);
            Assert.That(core.Start("reload", mode, Array.Empty<CombatEntityVisualDto>()).Success);
            Assert.That(core.SetGrip(new WeaponGripStateInputDto { SessionId = "reload", HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true, FrontHandTracked = true, Stability01 = 1 }).Success);
            return core;
        }
        static ServiceResult<CombatShotDto> Fire(CombatCoreService core) => core.Fire(new WeaponFireInputDto
            { SessionId = "reload", AimDirection = Vector3.forward });
    }
}
