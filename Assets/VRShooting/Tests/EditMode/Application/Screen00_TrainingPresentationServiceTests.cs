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
    /// P1/P2 卧姿展示状态机。追溯 docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md。
    /// 无 VR：用 <see cref="TrainingWeaponPickupEvent"/> 与 <c>WeaponControl.SetGripState</c> 作为取枪输入替身，不访问 XR 组件。
    /// </summary>
    [TestFixture]
    public class Screen00_TrainingPresentationServiceTests
    {
        GameEventBus eventBus;
        TrainingSessionService sessions;
        WeaponControlService weapons;
        ZeroingService zeroing;
        TrainingPresentationService presentation;
        readonly List<TrainingPresentationDto> snapshots = new List<TrainingPresentationDto>();

        [SetUp]
        public void SetUp()
        {
            eventBus = new GameEventBus();
            sessions = new TrainingSessionService(eventBus);
            weapons = new WeaponControlService(eventBus);
            zeroing = new ZeroingService(eventBus, sessions, weapons);
            presentation = new TrainingPresentationService(eventBus, sessions, weapons, zeroing);
            snapshots.Clear();
            presentation.PresentationChanged += dto => snapshots.Add(dto);
        }

        [TestCase(TrainingMode.Zeroing100m, ScreenId.ZeroingBriefing, TrainingPresentationRules.ZeroingFiringStationId)]
        [TestCase(TrainingMode.MovingTarget, ScreenId.MovingTargetSettings, TrainingPresentationRules.MovingTargetFiringStationId)]
        public void Screen00_EnterRange_AwaitsStartWithLargeUiAndNoShooting(
            TrainingMode mode,
            ScreenId expectedScreen,
            string expectedStationId)
        {
            var result = presentation.Enter(mode);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingStartConfirmation, result.Data.Phase);
            Assert.AreEqual(expectedScreen, result.Data.ActiveScreen);
            Assert.AreEqual(TrainingPostureMode.ProneFixed, result.Data.Posture);
            Assert.AreEqual(expectedStationId, result.Data.FiringStationId);
            Assert.IsTrue(result.Data.LargePanelVisible);
            Assert.IsFalse(result.Data.MinimalHudVisible);
            Assert.IsFalse(result.Data.ShootingAllowed);
            Assert.IsFalse(result.Data.ArtificialLocomotionAllowed);
            AssertPolicyDisabled(mode, result.Data.SessionId);
        }

        [TestCase(TrainingMode.Zeroing100m)]
        [TestCase(TrainingMode.MovingTarget)]
        public void Screen00_ConfirmStart_CreatesSessionAndWaitsForPickup(TrainingMode mode)
        {
            presentation.Enter(mode);

            var result = presentation.ConfirmStart(string.Empty);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingWeaponPickup, result.Data.Phase);
            Assert.IsFalse(string.IsNullOrEmpty(result.Data.SessionId));
            Assert.IsTrue(result.Data.LargePanelVisible);
            Assert.IsFalse(result.Data.ShootingAllowed);
            Assert.IsTrue(result.Data.AwaitingWeaponPickup);
            Assert.AreEqual(TrainingPostureMode.ProneFixed, sessions.Current.PostureMode);
            Assert.AreEqual(PlayerPosture.Prone, sessions.Current.Player.Posture);
            Assert.IsFalse(sessions.Current.ArtificialLocomotionAllowed);
            AssertPolicyDisabled(mode, result.Data.SessionId);
        }

        [Test]
        public void Screen00_PickupBeforeStart_KeepsLargeUiAndReturnsInvalidState()
        {
            presentation.Enter(TrainingMode.Zeroing100m);
            var before = presentation.Get(string.Empty).Data;

            var result = presentation.HandleWeaponPickup(ValidPickup("missing", WeaponControlService.TrainingRifleId));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ErrorCode.InvalidState, result.ErrorCode);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingStartConfirmation, result.Data.Phase);
            Assert.IsTrue(result.Data.LargePanelVisible);
            Assert.IsFalse(result.Data.ShootingAllowed);
            Assert.AreEqual(TrainingPresentationRules.ReasonPickupBeforeStart, result.Data.VisibilityReason);
            Assert.AreEqual(before.Phase, presentation.Get(string.Empty).Data.Phase);
        }

        [Test]
        public void Screen00_ZeroingValidPickup_HidesLargeUiAndAllowsShootingOnce()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m);

            var first = PickupRear(sessionId);
            var published = snapshots.Count;
            var duplicate = presentation.HandleWeaponPickup(ValidPickup(sessionId, WeaponControlService.TrainingRifleId));

            Assert.IsTrue(first.Success, first.Message);
            Assert.AreEqual(TrainingPresentationPhase.LiveFire, first.Data.Phase);
            Assert.IsFalse(first.Data.LargePanelVisible);
            Assert.IsTrue(first.Data.MinimalHudVisible);
            Assert.IsTrue(first.Data.ShootingAllowed);
            Assert.AreEqual(ScreenId.ZeroingHud, first.Data.ActiveScreen);
            Assert.IsTrue(duplicate.Success, duplicate.Message);
            Assert.AreEqual(published, snapshots.Count);
        }

        [Test]
        public void Screen00_MovingTargetValidPickup_EntersCountdownInsteadOfLiveFire()
        {
            var sessionId = StartAndConfirm(TrainingMode.MovingTarget);
            MovingTargetCountdownRequestedEvent? countdown = null;
            eventBus.Subscribe<MovingTargetCountdownRequestedEvent>(evt => countdown = evt);

            var result = PickupRear(sessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(TrainingPresentationPhase.LiveFire, result.Data.Phase);
            Assert.IsFalse(result.Data.LargePanelVisible);
            Assert.IsFalse(result.Data.ShootingAllowed);
            Assert.AreEqual(TrainingPresentationRules.ReasonP2Countdown, result.Data.VisibilityReason);
            Assert.AreEqual(ScreenId.MovingTargetHud, result.Data.ActiveScreen);
            Assert.IsTrue(countdown.HasValue);
            Assert.AreEqual(3f, countdown.Value.CountdownSeconds);
            Assert.AreEqual(sessionId, countdown.Value.SessionId);

            var elapsed = presentation.NotifyMovingTargetCountdownElapsed(sessionId);
            Assert.IsTrue(elapsed.Success, elapsed.Message);
            Assert.IsTrue(elapsed.Data.ShootingAllowed);
            Assert.AreEqual(TrainingPresentationRules.ReasonLiveFire, elapsed.Data.VisibilityReason);
        }

        [Test]
        public void Screen00_WrongWeaponOrSession_DoesNotStartLiveFire()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m);
            var before = presentation.Get(sessionId).Data;

            var wrongWeapon = presentation.HandleWeaponPickup(ValidPickup(sessionId, "other-rifle"));
            var wrongSession = presentation.HandleWeaponPickup(ValidPickup("other-session", WeaponControlService.TrainingRifleId));

            Assert.AreEqual(ErrorCode.InvalidState, wrongWeapon.ErrorCode);
            Assert.AreEqual(ErrorCode.InvalidState, wrongSession.ErrorCode);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingWeaponPickup, presentation.Get(sessionId).Data.Phase);
            Assert.IsTrue(presentation.Get(sessionId).Data.LargePanelVisible);
            Assert.IsFalse(presentation.Get(sessionId).Data.ShootingAllowed);
            Assert.AreEqual(before.Phase, presentation.Get(sessionId).Data.Phase);
        }

        [Test]
        public void Screen00_IllegalCommands_ReturnStableErrorAndKeepState()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m);

            var nextRound = presentation.ContinueNextRound(sessionId);
            var retry = presentation.Retry(sessionId);
            var complete = presentation.NotifyTrainingCompleted(sessionId);

            Assert.AreEqual(ErrorCode.InvalidState, nextRound.ErrorCode);
            Assert.AreEqual(ErrorCode.InvalidState, retry.ErrorCode);
            Assert.AreEqual(ErrorCode.InvalidState, complete.ErrorCode);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingWeaponPickup, presentation.Get(sessionId).Data.Phase);
        }

        [Test]
        public void Screen00_ZeroingThirdShot_ShowsRoundReviewAndDisablesShooting()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m, RandomSeed.Fixed(100));
            PickupRear(sessionId);

            RecordThreeImpacts(sessionId, new Vector2(-8f, 12f));

            var dto = presentation.Get(sessionId).Data;
            Assert.AreEqual(TrainingPresentationPhase.RoundReview, dto.Phase);
            Assert.IsTrue(dto.LargePanelVisible);
            Assert.IsFalse(dto.ShootingAllowed);
            Assert.AreEqual(ScreenId.ZeroingImpactAnalysis, dto.ActiveScreen);
        }

        [Test]
        public void Screen00_NextRound_WhileHeld_HidesLargeUiAndAllowsShooting()
        {
            var sessionId = FailRoundToReview(keepHeld: true);

            var next = presentation.ContinueNextRound(sessionId);

            Assert.IsTrue(next.Success, next.Message);
            Assert.AreEqual(TrainingPresentationPhase.LiveFire, next.Data.Phase);
            Assert.IsFalse(next.Data.LargePanelVisible);
            Assert.IsTrue(next.Data.MinimalHudVisible);
            Assert.IsTrue(next.Data.ShootingAllowed);
            Assert.IsFalse(next.Data.AwaitingWeaponPickup);
        }

        [Test]
        public void Screen00_NextRound_AfterDrop_HidesLargeUiAndWaitsForRegrip()
        {
            var sessionId = FailRoundToReview(keepHeld: false);

            var next = presentation.ContinueNextRound(sessionId);

            Assert.IsTrue(next.Success, next.Message);
            Assert.AreEqual(TrainingPresentationPhase.LiveFire, next.Data.Phase);
            Assert.IsFalse(next.Data.LargePanelVisible);
            Assert.IsTrue(next.Data.MinimalHudVisible);
            Assert.IsFalse(next.Data.ShootingAllowed);
            Assert.IsTrue(next.Data.AwaitingWeaponPickup);
            Assert.AreEqual(TrainingPresentationRules.ReasonAwaitingRearGrip, next.Data.VisibilityReason);

            var regrip = PickupRear(sessionId);
            Assert.IsTrue(regrip.Data.ShootingAllowed);
            Assert.IsFalse(regrip.Data.LargePanelVisible);
        }

        [Test]
        public void Screen00_PassedFirstRound_ContinueShowsFinalRating()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m, RandomSeed.Fixed(100));
            PickupRear(sessionId);
            RecordThreeImpacts(sessionId, new Vector2(1f, 1f));
            zeroing.ApplyAdjustment(sessionId, 1);

            var next = presentation.ContinueNextRound(sessionId);

            Assert.IsTrue(next.Success, next.Message);
            Assert.AreEqual(TrainingPresentationPhase.SessionResults, next.Data.Phase);
            Assert.IsTrue(next.Data.LargePanelVisible);
            Assert.IsFalse(next.Data.ShootingAllowed);
            Assert.AreEqual(ScreenId.ZeroingFinalRating, next.Data.ActiveScreen);
            Assert.AreEqual(ResultGrade.Excellent, zeroing.GetFinalResult(sessionId).Data.Grade);
        }

        [Test]
        public void Screen00_ThreeFailedRounds_ContinueShowsFinalRating()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m, RandomSeed.Fixed(100));
            PickupRear(sessionId);
            FailCurrentRoundAndContinue(sessionId);
            FailCurrentRoundAndContinue(sessionId);
            RecordThreeImpacts(sessionId, new Vector2(-8f, 12f));
            zeroing.ApplyAdjustment(sessionId, 3);

            var next = presentation.ContinueNextRound(sessionId);

            Assert.IsTrue(next.Success, next.Message);
            Assert.AreEqual(TrainingPresentationPhase.SessionResults, next.Data.Phase);
            Assert.AreEqual(ScreenId.ZeroingFinalRating, next.Data.ActiveScreen);
            Assert.AreEqual(ResultGrade.Fail, zeroing.GetFinalResult(sessionId).Data.Grade);
        }

        [Test]
        public void Screen00_P2Complete_ShowsResultsAndRetryReturnsToSettings()
        {
            var sessionId = StartAndConfirm(TrainingMode.MovingTarget);
            PickupRear(sessionId);

            var complete = presentation.NotifyTrainingCompleted(sessionId);
            Assert.IsTrue(complete.Success, complete.Message);
            Assert.AreEqual(TrainingPresentationPhase.SessionResults, complete.Data.Phase);
            Assert.IsTrue(complete.Data.LargePanelVisible);
            Assert.IsFalse(complete.Data.ShootingAllowed);
            Assert.AreEqual(ScreenId.MovingTargetResults, complete.Data.ActiveScreen);

            var retry = presentation.Retry(sessionId);
            Assert.IsTrue(retry.Success, retry.Message);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingStartConfirmation, retry.Data.Phase);
            Assert.AreEqual(ScreenId.MovingTargetSettings, retry.Data.ActiveScreen);
            Assert.IsTrue(string.IsNullOrEmpty(retry.Data.SessionId));
        }

        [Test]
        public void Screen00_OldSessionPickup_DoesNotPolluteNewSession()
        {
            var firstId = StartAndConfirm(TrainingMode.Zeroing100m);
            PickupRear(firstId);
            presentation.Exit(firstId);

            var secondEnter = presentation.Enter(TrainingMode.Zeroing100m);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingStartConfirmation, secondEnter.Data.Phase);

            var stale = presentation.HandleWeaponPickup(ValidPickup(firstId, WeaponControlService.TrainingRifleId));
            Assert.AreEqual(ErrorCode.InvalidState, stale.ErrorCode);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingStartConfirmation, presentation.Get(string.Empty).Data.Phase);
            Assert.IsFalse(presentation.Get(string.Empty).Data.ShootingAllowed);

            var secondId = presentation.ConfirmStart(string.Empty).Data.SessionId;
            Assert.AreNotEqual(firstId, secondId);
            var staleAfterStart = presentation.HandleWeaponPickup(ValidPickup(firstId, WeaponControlService.TrainingRifleId));
            Assert.AreEqual(ErrorCode.InvalidState, staleAfterStart.ErrorCode);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingWeaponPickup, presentation.Get(secondId).Data.Phase);
        }

        [Test]
        public void Screen00_RecordShotBeforePickup_IsRejected()
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m, RandomSeed.Fixed(100));

            var shot = RecordImpact(sessionId, Vector2.zero);

            Assert.IsFalse(shot.Success);
            Assert.AreEqual(ErrorCode.InvalidState, shot.ErrorCode);
            Assert.AreEqual(3, zeroing.GetSession(sessionId).Data.ShotsRemainingInRound);
        }

        string StartAndConfirm(TrainingMode mode, RandomSeed? seed = null)
        {
            presentation.Enter(mode);
            if (seed.HasValue)
            {
                var created = sessions.Create(mode, string.Empty, WeaponControlService.TrainingRifleId, seed.Value);
                Assert.IsTrue(created.Success, created.Message);
                var started = presentation.ConfirmStart(created.Data.SessionId);
                Assert.IsTrue(started.Success, started.Message);
                return started.Data.SessionId;
            }

            var result = presentation.ConfirmStart(string.Empty);
            Assert.IsTrue(result.Success, result.Message);
            return result.Data.SessionId;
        }

        ServiceResult<TrainingPresentationDto> PickupRear(string sessionId)
        {
            var grip = weapons.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.RearHandHeld,
                RearHandTracked = true,
                FrontHandTracked = false,
                Stability01 = 0.4f
            });
            Assert.IsTrue(grip.Success, grip.Message);
            return presentation.Get(sessionId);
        }

        void DropWeapon(string sessionId)
        {
            var grip = weapons.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.Dropped,
                RearHandTracked = false,
                FrontHandTracked = false,
                Stability01 = 0f
            });
            Assert.IsTrue(grip.Success, grip.Message);
        }

        string FailRoundToReview(bool keepHeld)
        {
            var sessionId = StartAndConfirm(TrainingMode.Zeroing100m, RandomSeed.Fixed(100));
            PickupRear(sessionId);
            RecordThreeImpacts(sessionId, new Vector2(-8f, 12f));
            if (!keepHeld)
            {
                DropWeapon(sessionId);
            }

            var applied = zeroing.ApplyAdjustment(sessionId, 1);
            Assert.IsTrue(applied.Success, applied.Message);
            return sessionId;
        }

        void FailCurrentRoundAndContinue(string sessionId)
        {
            RecordThreeImpacts(sessionId, new Vector2(-8f, 12f));
            var round = zeroing.GetSession(sessionId).Data.CurrentRound;
            var applied = zeroing.ApplyAdjustment(sessionId, round);
            Assert.IsTrue(applied.Success, applied.Message);
            var next = presentation.ContinueNextRound(sessionId);
            Assert.IsTrue(next.Success, next.Message);
            if (!next.Data.ShootingAllowed)
            {
                PickupRear(sessionId);
            }
        }

        void RecordThreeImpacts(string sessionId, Vector2 impactCm)
        {
            var offset = zeroing.GetSession(sessionId).Data.FixedImpactOffsetCm;
            var aim = impactCm - offset;
            Assert.IsTrue(RecordImpact(sessionId, aim).Success);
            Assert.IsTrue(RecordImpact(sessionId, aim).Success);
            Assert.IsTrue(RecordImpact(sessionId, aim).Success);
        }

        ServiceResult<ZeroingShotDto> RecordImpact(string sessionId, Vector2 aimCm)
        {
            return zeroing.RecordShot(sessionId, new ShotInputDto
            {
                WeaponPosition = Vector3.zero,
                AimDirection = new Vector3(aimCm.x, aimCm.y, ZeroingRules.DistanceMeters),
                WeaponStability = 0.95f,
                FireTime = 0d
            });
        }

        static TrainingWeaponPickupEvent ValidPickup(string sessionId, string weaponId)
        {
            return new TrainingWeaponPickupEvent
            {
                SessionId = sessionId,
                WeaponId = weaponId,
                PreviousState = WeaponHoldState.OnRack,
                CurrentState = WeaponHoldState.RearHandHeld
            };
        }

        void AssertPolicyDisabled(TrainingMode mode, string sessionId)
        {
            var policy = presentation.GetLocomotionPolicy(sessionId);
            Assert.IsTrue(policy.Success, policy.Message);
            Assert.AreEqual(mode, policy.Data.Mode);
            Assert.AreEqual(TrainingPostureMode.ProneFixed, policy.Data.Posture);
            Assert.IsFalse(policy.Data.AllowContinuousMove);
            Assert.IsFalse(policy.Data.AllowTeleport);
            Assert.IsFalse(policy.Data.AllowArtificialTurn);
            Assert.IsTrue(policy.Data.AllowRoomScaleHeadAndHandTracking);
        }
    }
}
