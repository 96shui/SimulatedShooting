using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// BDD 00/08/09：P2 倒计时只能在有效取枪后推进，且路线时钟不依赖武器 MonoBehaviour。
    /// </summary>
    [TestFixture]
    public sealed class MovingTargetProgressCoordinatorTests
    {
        [Test]
        public void Tick_BeforeWeaponPickup_DoesNotConsumeCountdown()
        {
            var services = ApplicationServices.CreateDefault();
            var sessionId = StartAwaitingPickup(services);

            var tick = services.MovingTargetProgress.Tick(sessionId, 10f);

            Assert.That(tick.Success, Is.True, tick.Message);
            Assert.That(tick.Data.Phase, Is.EqualTo(TargetMovePhase.WaitingCountdown));
            Assert.That(tick.Data.CountdownSecondsRemaining, Is.EqualTo(3f).Within(0.001f));
            Assert.That(
                services.Presentation.Get(sessionId).Data.Phase,
                Is.EqualTo(TrainingPresentationPhase.AwaitingWeaponPickup));
        }

        [Test]
        public void Tick_AfterWeaponPickup_AdvancesCountdownWithoutWeaponFireTick()
        {
            var services = ApplicationServices.CreateDefault();
            var sessionId = StartAwaitingPickup(services);
            var pickup = services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.RearHandHeld,
                RearHandTracked = true,
                FrontHandTracked = false,
                Stability01 = 0.9f
            });

            Assert.That(pickup.Success, Is.True, pickup.Message);
            Assert.That(
                services.Presentation.Get(sessionId).Data.Phase,
                Is.EqualTo(TrainingPresentationPhase.LiveFire));

            var tick = services.MovingTargetProgress.Tick(sessionId, 3f);

            Assert.That(tick.Success, Is.True, tick.Message);
            Assert.That(tick.Data.Phase, Is.EqualTo(TargetMovePhase.MovingRightToLeft));
            Assert.That(tick.Data.CanShoot, Is.True);
            Assert.That(services.Presentation.Get(sessionId).Data.ShootingAllowed, Is.True);
        }

        [Test]
        public void Retry_ReleasesCompletedSessionResourcesAfterResultsWereReadable()
        {
            var services = ApplicationServices.CreateDefault();
            var sessionId = StartAwaitingPickup(services);
            Assert.That(
                services.AutomaticFire.StartSession(
                    sessionId,
                    WeaponFireMode.InitialTwoThenAutomatic,
                    WeaponAutoFireConfigDto.P2Default).Success,
                Is.True);
            Assert.That(services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.RearHandHeld,
                RearHandTracked = true,
                FrontHandTracked = false,
                Stability01 = 0.9f
            }).Success, Is.True);

            Assert.That(services.MovingTargetProgress.Tick(sessionId, 100f).Success, Is.True);
            Assert.That(services.MovingTarget.CompleteSession(sessionId).Success, Is.True);
            Assert.That(
                services.Presentation.Get(sessionId).Data.Phase,
                Is.EqualTo(TrainingPresentationPhase.SessionResults));

            var retry = services.Presentation.Retry(sessionId);

            Assert.That(retry.Success, Is.True, retry.Message);
            Assert.That(services.MovingTarget.GetSession(sessionId).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            Assert.That(services.AutomaticFire.GetState(sessionId).Success, Is.False);
            Assert.That(services.WeaponControl.GetState(sessionId).Success, Is.False);
        }

        static string StartAwaitingPickup(ApplicationServices services)
        {
            Assert.That(services.Presentation.Enter(TrainingMode.MovingTarget).Success, Is.True);
            var started = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f },
                RandomSeed.Fixed(205));
            Assert.That(started.Success, Is.True, started.Message);
            Assert.That(services.Presentation.ConfirmStart(started.Data.SessionId).Success, Is.True);
            return started.Data.SessionId;
        }
    }
}
