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
            var service = CreateStartedService();

            var state = service.GetState(SessionId).Data;

            Assert.That(state.WeaponId, Is.EqualTo(WeaponControlService.TrainingRifleId));
            Assert.That(state.CurrentMagazine, Is.EqualTo(3));
            Assert.That(state.ReserveAmmo, Is.EqualTo(6));
            Assert.That(state.CanShoot, Is.True);
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
            var service = CreateStartedService();

            var result = service.SetGripState(SessionId, true, 0.72f);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.TwoHandGripActive, Is.True);
            Assert.That(result.Data.Stability01, Is.EqualTo(0.72f).Within(0.001f));
        }

        static WeaponControlService CreateStartedService()
        {
            var service = new WeaponControlService(new GameEventBus());
            var result = service.StartSession(SessionId, WeaponControlService.TrainingRifleId, TrainingMode.Zeroing100m);
            Assert.That(result.Success, Is.True);
            return service;
        }

        static WeaponFireInputDto CreateFireInput(bool hit = false)
        {
            return new WeaponFireInputDto
            {
                SessionId = SessionId,
                MuzzlePosition = Vector3.zero,
                WeaponPosition = Vector3.zero,
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
