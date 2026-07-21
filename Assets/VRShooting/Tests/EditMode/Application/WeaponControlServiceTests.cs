using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    [TestFixture]
    public sealed class WeaponControlServiceTests
    {
        const string SessionId = "task005-editmode";

        [Test]
        public void Task005_StartSession_ProvidesTrainingRifleState()
        {
            var service = CreateStartedService(twoHandHeld: false);

            var state = service.GetState(SessionId).Data;

            Assert.That(state.WeaponId, Is.EqualTo(WeaponControlService.TrainingRifleId));
            Assert.That(state.CurrentMagazine, Is.EqualTo(3));
            Assert.That(state.ReserveAmmo, Is.EqualTo(6));
            Assert.That(state.CanShoot, Is.False);
            Assert.That(state.HoldState, Is.EqualTo(WeaponHoldState.OnRack));
            Assert.That(state.TwoHandGripActive, Is.False);
            Assert.That(state.ShoulderSide, Is.EqualTo(ShoulderSide.Right));
            Assert.That(state.AimMode, Is.EqualTo(WeaponAimMode.HipFire));
        }

        [Test]
        public void Task005_Fire_ConsumesAmmoAndReturnsGunLineResult()
        {
            var service = CreateStartedService();

            var result = service.Fire(CreateFireInput(hit: true));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.IsValidShot, Is.True);
            Assert.That(result.Data.CurrentMagazine, Is.EqualTo(2));
            Assert.That(result.Data.ReserveAmmo, Is.EqualTo(6));
            Assert.That(result.Data.AimDirection, Is.EqualTo(Vector3.forward));
            Assert.That(result.Data.Hit, Is.True);
            Assert.That(service.GetState(SessionId).Data.CurrentMagazine, Is.EqualTo(2));
        }

        [Test]
        public void Task005_Fire_WhenMagazineEmpty_DoesNotConsumeAmmoAgain()
        {
            var service = CreateStartedService();
            service.Fire(CreateFireInput());
            service.Fire(CreateFireInput());
            service.Fire(CreateFireInput());

            var blocked = service.Fire(CreateFireInput());

            Assert.That(blocked.Success, Is.False);
            Assert.That(blocked.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(blocked.Data.IsValidShot, Is.False);
            Assert.That(blocked.Data.CurrentMagazine, Is.EqualTo(0));
            Assert.That(service.GetState(SessionId).Data.CurrentMagazine, Is.EqualTo(0));
        }

        [Test]
        public void Task005_Reload_TransfersReserveAmmoToMagazine()
        {
            var service = CreateStartedService();
            service.Fire(CreateFireInput());
            service.Fire(CreateFireInput());

            var reload = service.Reload(SessionId);

            Assert.That(reload.Success, Is.True);
            Assert.That(reload.Data.CurrentMagazine, Is.EqualTo(3));
            Assert.That(reload.Data.ReserveAmmo, Is.EqualTo(4));
            Assert.That(reload.Data.CanShoot, Is.True);
        }

        [Test]
        public void Task005_ToggleShoulder_ChangesShoulderWithoutChangingAmmo()
        {
            var service = CreateStartedService();

            var result = service.ToggleShoulder(SessionId);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.ShoulderSide, Is.EqualTo(ShoulderSide.Left));
            Assert.That(result.Data.CurrentMagazine, Is.EqualTo(3));
        }

        [Test]
        public void Task005_SetAimMode_TracksAdsState()
        {
            var service = CreateStartedService();

            var result = service.SetAimMode(SessionId, WeaponAimMode.AimDownSights);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.AimMode, Is.EqualTo(WeaponAimMode.AimDownSights));
            Assert.That(service.GetState(SessionId).Data.AimMode, Is.EqualTo(WeaponAimMode.AimDownSights));
        }

        [Test]
        public void Task005_SetGripState_TracksTwoHandStability()
        {
            var service = CreateStartedService(twoHandHeld: false);

            var result = service.SetGripState(CreateGripInput(WeaponHoldState.TwoHandHeld, 0.72f));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.TwoHandGripActive, Is.True);
            Assert.That(result.Data.Stability01, Is.EqualTo(0.72f).Within(0.001f));
        }

        [Test]
        public void Task013_Fire_RequiresTrackedTwoHandHold()
        {
            var service = CreateStartedService(twoHandHeld: false);
            var rearHand = service.SetGripState(CreateGripInput(WeaponHoldState.RearHandHeld, 0.4f));

            var blocked = service.Fire(CreateFireInput());

            Assert.That(rearHand.Success, Is.True);
            Assert.That(rearHand.Data.CanShoot, Is.False);
            Assert.That(blocked.Success, Is.False);
            Assert.That(blocked.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(blocked.Data.HoldState, Is.EqualTo(WeaponHoldState.RearHandHeld));
            Assert.That(service.GetState(SessionId).Data.CurrentMagazine, Is.EqualTo(3));
        }

        [Test]
        public void Task013_Fire_SnapshotsGripStabilityAndDeterministicRecoil()
        {
            var first = CreateStartedService();
            var firstShot = first.Fire(CreateFireInput());
            var second = CreateStartedService();
            var repeatedShot = second.Fire(CreateFireInput());

            Assert.That(firstShot.Success, Is.True);
            Assert.That(firstShot.Data.Stability01, Is.EqualTo(0.82f).Within(0.001f));
            Assert.That(firstShot.Data.ShotSequence, Is.EqualTo(1));
            Assert.That(firstShot.Data.RecoilImpulse.PitchDegrees, Is.InRange(2.5f, 4f));
            Assert.That(firstShot.Data.RecoilImpulse.RearwardMeters, Is.InRange(0.02f, 0.04f));
            Assert.That(firstShot.Data.RecoilImpulse.YawDegrees,
                Is.EqualTo(repeatedShot.Data.RecoilImpulse.YawDegrees).Within(0.0001f));
        }

        [Test]
        public void Task013_SetGripState_RejectsImpossibleHandTrackingCombination()
        {
            var service = CreateStartedService(twoHandHeld: false);

            var result = service.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = SessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = false,
                Stability01 = 1f
            });

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(service.GetState(SessionId).Data.HoldState, Is.EqualTo(WeaponHoldState.OnRack));
        }

        static WeaponControlService CreateStartedService(bool twoHandHeld = true)
        {
            var service = new WeaponControlService(new GameEventBus());
            var result = service.StartSession(SessionId, WeaponControlService.TrainingRifleId, TrainingMode.Zeroing100m);
            Assert.That(result.Success, Is.True);
            if (twoHandHeld)
            {
                Assert.That(service.SetGripState(CreateGripInput(WeaponHoldState.TwoHandHeld, 0.82f)).Success, Is.True);
            }

            return service;
        }

        static WeaponGripStateInputDto CreateGripInput(WeaponHoldState holdState, float stability)
        {
            return new WeaponGripStateInputDto
            {
                SessionId = SessionId,
                HoldState = holdState,
                RearHandTracked = holdState == WeaponHoldState.RearHandHeld || holdState == WeaponHoldState.TwoHandHeld,
                FrontHandTracked = holdState == WeaponHoldState.TwoHandHeld,
                Stability01 = stability
            };
        }

        static WeaponFireInputDto CreateFireInput(bool hit = false)
        {
            return new WeaponFireInputDto
            {
                SessionId = SessionId,
                MuzzlePosition = Vector3.zero,
                WeaponPosition = Vector3.zero,
                RawAimDirection = Vector3.forward,
                AimDirection = Vector3.forward,
                Stability01 = 0.9f,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = hit,
                HitPoint = hit ? Vector3.forward * 100f : Vector3.zero,
                HitObjectId = hit ? "ZeroingRange.Target.Face" : string.Empty
            };
        }
    }
}
