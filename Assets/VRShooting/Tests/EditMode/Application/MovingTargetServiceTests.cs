using System.Collections.Generic;
using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// 移动靶服务。追溯 docs/BDD/screens/08-移动靶设置.feature.md、09-移动靶HUD.feature.md、11-移动靶结算.feature.md。
    /// </summary>
    [TestFixture]
    public class MovingTargetServiceTests
    {
        GameEventBus eventBus;
        TrainingSessionService sessions;
        MovingTargetService movingTarget;

        [SetUp]
        public void SetUp()
        {
            eventBus = new GameEventBus();
            sessions = new TrainingSessionService(eventBus);
            movingTarget = new MovingTargetService(eventBus, sessions);
        }

        [Test]
        public void Screen08_GetAvailableSpeeds_ReturnsConfiguredValues()
        {
            var result = movingTarget.GetAvailableSpeeds();

            Assert.IsTrue(result.Success);
            CollectionAssert.AreEqual(new[] { 3f, 4f, 5f }, result.Data);
        }

        [TestCase(3f)]
        [TestCase(4f)]
        [TestCase(5f)]
        public void Screen08_StartSession_AcceptsAllowedSpeeds(float speed)
        {
            var result = movingTarget.StartSession(Settings(speed), RandomSeed.Fixed(11));

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(speed, result.Data.SpeedMetersPerSecond);
            Assert.AreEqual(TargetMovePhase.WaitingCountdown, result.Data.Phase);
            Assert.AreEqual(3f, result.Data.CountdownSecondsRemaining);
            Assert.IsFalse(result.Data.CanShoot);
            Assert.AreEqual(TrainingMode.MovingTarget, sessions.Current.Mode);
            Assert.AreEqual(TrainingPostureMode.ProneFixed, sessions.Current.PostureMode);
            Assert.IsFalse(sessions.Current.ArtificialLocomotionAllowed);
        }

        [TestCase(0f)]
        [TestCase(2f)]
        [TestCase(3.5f)]
        [TestCase(6f)]
        public void Screen08_StartSession_RejectsIllegalSpeed(float speed)
        {
            var result = movingTarget.StartSession(Settings(speed), RandomSeed.Fixed(11));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ErrorCode.InvalidInput, result.ErrorCode);
        }

        [Test]
        public void Screen08_StartSession_DuplicateStartReturnsInvalidState()
        {
            Assert.IsTrue(movingTarget.StartSession(Settings(4f), RandomSeed.Fixed(11)).Success);
            var duplicate = movingTarget.StartSession(Settings(4f), RandomSeed.Fixed(11));

            Assert.IsFalse(duplicate.Success);
            Assert.AreEqual(ErrorCode.InvalidState, duplicate.ErrorCode);
        }

        [Test]
        public void Screen09_StartSession_LiveFireSpeedChangeReturnsInvalidState()
        {
            var started = movingTarget.StartSession(Settings(4f), RandomSeed.Fixed(11));
            movingTarget.Tick(started.Data.SessionId, 4f);

            var changed = movingTarget.StartSession(Settings(5f), RandomSeed.Fixed(11));
            Assert.IsFalse(changed.Success);
            Assert.AreEqual(ErrorCode.InvalidState, changed.ErrorCode);
        }

        [Test]
        public void Screen09_Tick_ZeroSecondTransitionStartsMovementWithoutProgress()
        {
            var sessionId = Start(4f);
            var result = movingTarget.Tick(sessionId, 3f);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(TargetMovePhase.MovingRightToLeft, result.Data.Phase);
            Assert.AreEqual(0f, result.Data.RouteProgress01, 0.0001f);
            Assert.IsTrue(result.Data.CanShoot);
        }

        [Test]
        public void Screen09_Tick_PublishesCountdownElapsedOnce()
        {
            var sessionId = Start(4f);
            var events = new List<MovingTargetCountdownElapsedEvent>();
            eventBus.Subscribe<MovingTargetCountdownElapsedEvent>(evt => events.Add(evt));

            movingTarget.Tick(sessionId, 3f);
            movingTarget.Tick(sessionId, 1f);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(sessionId, events[0].SessionId);
        }

        [Test]
        public void Screen09_Tick_CompletesFortyMetreOutAndBackAtFourMetersPerSecond()
        {
            var sessionId = Start(4f);
            var afterRtl = movingTarget.Tick(sessionId, 13f);
            Assert.AreEqual(TargetMovePhase.LeftEndpointHold, afterRtl.Data.Phase);
            Assert.AreEqual(1f, afterRtl.Data.RouteProgress01, 0.0001f);
            Assert.AreEqual(2f, afterRtl.Data.EndpointHoldSecondsRemaining, 0.0001f);
            Assert.IsFalse(afterRtl.Data.CanShoot);

            var afterHold = movingTarget.Tick(sessionId, 2f);
            Assert.AreEqual(TargetMovePhase.MovingLeftToRight, afterHold.Data.Phase);
            Assert.IsTrue(afterHold.Data.CanShoot);

            var completed = movingTarget.Tick(sessionId, 10f);
            Assert.AreEqual(TargetMovePhase.Completed, completed.Data.Phase);
            Assert.AreEqual(0f, completed.Data.RouteProgress01, 0.0001f);
            Assert.IsFalse(completed.Data.CanShoot);
        }

        [Test]
        public void Screen09_Tick_LargeStepReachesResultsAndEndsTrainingSession()
        {
            var sessionId = Start(5f);
            MovingTargetSessionCompletedEvent? completed = null;
            eventBus.Subscribe<MovingTargetSessionCompletedEvent>(evt => completed = evt);

            var result = movingTarget.Tick(sessionId, 100f);

            Assert.AreEqual(TargetMovePhase.Completed, result.Data.Phase);
            Assert.IsTrue(completed.HasValue);
            Assert.AreEqual(SessionState.Completed, sessions.Current.State);
            Assert.AreEqual(ErrorCode.NotFound, movingTarget.CompleteSession("missing").ErrorCode);
            Assert.IsTrue(movingTarget.CompleteSession(sessionId).Success);
        }

        [Test]
        public void Screen09_RecordShot_IgnoresCountdownHoldAndCompleted()
        {
            var sessionId = Start(4f);
            Assert.AreEqual(ErrorCode.InvalidState, Record(sessionId, "seq", 1, true).ErrorCode);

            movingTarget.Tick(sessionId, 13f);
            Assert.AreEqual(TargetMovePhase.LeftEndpointHold, movingTarget.GetSession(sessionId).Data.Phase);
            Assert.AreEqual(ErrorCode.InvalidState, Record(sessionId, "seq", 1, true).ErrorCode);

            movingTarget.Tick(sessionId, 12f);
            Assert.AreEqual(TargetMovePhase.Completed, movingTarget.GetSession(sessionId).Data.Phase);
            Assert.AreEqual(ErrorCode.InvalidState, Record(sessionId, "seq", 1, true).ErrorCode);
            Assert.AreEqual(0, movingTarget.GetSession(sessionId).Data.ShotsFired);
        }

        [Test]
        public void Screen09_RecordShot_ScoresMovingHitsAndIsIdempotentForDuplicateShotId()
        {
            var sessionId = Start(4f);
            movingTarget.Tick(sessionId, 4f);

            var first = Record(sessionId, "seq-a", 1, true);
            var duplicate = Record(sessionId, "seq-a", 1, false);
            var miss = Record(sessionId, "seq-a", 2, false);

            Assert.IsTrue(first.Success, first.Message);
            Assert.IsTrue(first.Data.Hit);
            Assert.AreEqual(1, first.Data.GlobalShotIndex);
            Assert.AreEqual(first.Data.FireTime, duplicate.Data.FireTime);
            Assert.IsTrue(duplicate.Data.Hit);
            Assert.IsFalse(miss.Data.Hit);
            Assert.AreEqual(2, movingTarget.GetSession(sessionId).Data.ShotsFired);
            Assert.AreEqual(1, movingTarget.GetSession(sessionId).Data.HitCount);
            Assert.AreEqual(WeaponFireSequencePhase.InitialTwoShots, movingTarget.GetSession(sessionId).Data.FirePhase);
        }

        [Test]
        public void Screen09_CompleteFireSequence_MarksContinuousBurstAndStopReason()
        {
            var sessionId = Start(4f);
            movingTarget.Tick(sessionId, 4f);
            Record(sessionId, "seq-b", 1, true);
            Record(sessionId, "seq-b", 2, true);
            Record(sessionId, "seq-b", 3, false);

            var completed = movingTarget.CompleteFireSequence(sessionId, "seq-b", WeaponFireStopReason.TriggerReleased);
            Assert.IsTrue(completed.Success, completed.Message);
            Assert.IsTrue(completed.Data.EnteredContinuousFire);
            Assert.AreEqual(3, completed.Data.ShotCount);
            Assert.AreEqual(2, completed.Data.HitCount);
            Assert.AreEqual(WeaponFireStopReason.TriggerReleased, completed.Data.StopReason);
            Assert.AreEqual(ErrorCode.InvalidState, Record(sessionId, "seq-b", 4, true).ErrorCode);
        }

        [Test]
        public void Screen11_CompleteSession_BeforeRouteFinishedIsInvalidState()
        {
            var sessionId = Start(4f);
            var result = movingTarget.CompleteSession(sessionId);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ErrorCode.InvalidState, result.ErrorCode);
        }

        [TestCase(0, ResultGrade.Fail)]
        [TestCase(3, ResultGrade.Pass)]
        [TestCase(4, ResultGrade.Good)]
        [TestCase(5, ResultGrade.Excellent)]
        public void Screen11_CompleteSession_MapsHitsToGradeAndKeepsShotTrace(int hits, ResultGrade expected)
        {
            var sessionId = Start(4f);
            movingTarget.Tick(sessionId, 4f);
            for (var i = 0; i < 8; i++)
            {
                Record(sessionId, "seq-grade", i + 1, i < hits);
            }

            movingTarget.Tick(sessionId, 100f);
            var result = movingTarget.CompleteSession(sessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(expected, result.Data.Grade);
            Assert.AreEqual(hits, result.Data.HitCount);
            Assert.AreEqual(8, result.Data.TotalShotsFired);
            Assert.AreEqual(8, result.Data.TotalAmmoConsumed);
            Assert.AreEqual(MovingTargetRules.ComputeHitRate01(hits, 8), result.Data.HitRate01);
            Assert.AreEqual(4f, result.Data.SpeedMetersPerSecond);
            Assert.AreEqual(1, result.Data.FireSequences.Count);
            Assert.AreEqual(8, result.Data.FireSequences[0].Shots.Count);
            Assert.AreEqual(WeaponFireStopReason.TrainingCompleted, result.Data.FireSequences[0].StopReason);
        }

        [Test]
        public void Screen11_Retry_ClearsOldStateAndReplaysWithFixedSeed()
        {
            var first = movingTarget.StartSession(Settings(4f), RandomSeed.Fixed(42));
            var firstSnapshot = movingTarget.Tick(first.Data.SessionId, 6f);
            Record(first.Data.SessionId, "old", 1, true);
            movingTarget.Tick(first.Data.SessionId, 100f);

            var second = movingTarget.StartSession(Settings(4f), RandomSeed.Fixed(42));
            Assert.IsTrue(second.Success, second.Message);
            Assert.AreNotEqual(first.Data.SessionId, second.Data.SessionId);
            Assert.AreEqual(TargetMovePhase.WaitingCountdown, second.Data.Phase);
            Assert.AreEqual(0, second.Data.ShotsFired);

            var replayed = movingTarget.Tick(second.Data.SessionId, 6f);
            Assert.AreEqual(firstSnapshot.Data.Phase, replayed.Data.Phase);
            Assert.AreEqual(firstSnapshot.Data.RouteProgress01, replayed.Data.RouteProgress01, 0.0001f);
        }

        [Test]
        public void Screen09_Tick_NotifiesPresentationCountdownAndResults()
        {
            var weapons = new WeaponControlService(eventBus);
            var zeroing = new ZeroingService(eventBus, sessions, weapons);
            var presentation = new TrainingPresentationService(eventBus, sessions, weapons, zeroing);

            Assert.IsTrue(presentation.Enter(TrainingMode.MovingTarget).Success);
            var confirmed = presentation.ConfirmStart(string.Empty);
            Assert.IsTrue(confirmed.Success, confirmed.Message);
            var sessionId = confirmed.Data.SessionId;
            Assert.IsTrue(movingTarget.StartSession(Settings(4f), RandomSeed.Fixed(11)).Success);

            var pickup = presentation.HandleWeaponPickup(new TrainingWeaponPickupEvent
            {
                SessionId = sessionId,
                WeaponId = TrainingPresentationRules.TrainingRifleId,
                PreviousState = WeaponHoldState.OnRack,
                CurrentState = WeaponHoldState.RearHandHeld
            });
            Assert.IsTrue(pickup.Success, pickup.Message);
            Assert.IsTrue(pickup.Data.MinimalHudVisible);

            movingTarget.Tick(sessionId, 3f);
            Assert.AreEqual(TrainingPresentationPhase.LiveFire, presentation.Get(sessionId).Data.Phase);
            Assert.AreEqual(TrainingPresentationRules.ReasonLiveFire, presentation.Get(sessionId).Data.VisibilityReason);

            movingTarget.Tick(sessionId, 100f);
            var after = presentation.Get(sessionId);
            Assert.AreEqual(TrainingPresentationPhase.SessionResults, after.Data.Phase);
            Assert.AreEqual(ScreenId.MovingTargetResults, after.Data.ActiveScreen);
            Assert.IsTrue(after.Data.LargePanelVisible);
        }

        [Test]
        public void Screen09_PauseAndCancel_HaveDeterministicTickBehaviour()
        {
            var sessionId = Start(4f);
            movingTarget.Tick(sessionId, 1f);
            Assert.IsTrue(sessions.Pause(sessionId).Success);

            var paused = movingTarget.Tick(sessionId, 5f);
            Assert.AreEqual(2f, paused.Data.CountdownSecondsRemaining, 0.0001f);
            Assert.AreEqual(TargetMovePhase.WaitingCountdown, paused.Data.Phase);

            Assert.IsTrue(sessions.Resume(sessionId).Success);
            var resumed = movingTarget.Tick(sessionId, 2f);
            Assert.AreEqual(TargetMovePhase.MovingRightToLeft, resumed.Data.Phase);

            Assert.IsTrue(sessions.Cancel(sessionId).Success);
            var cancelled = movingTarget.Tick(sessionId, 1f);
            Assert.IsFalse(cancelled.Success);
            Assert.AreEqual(ErrorCode.InvalidState, cancelled.ErrorCode);
        }

        [Test]
        public void Screen09_NegativeDeltaTime_IsInvalidInput()
        {
            var sessionId = Start(4f);
            var result = movingTarget.Tick(sessionId, -1f);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(ErrorCode.InvalidInput, result.ErrorCode);
        }

        [Test]
        public void FakeSequences_ExposeUiAndSceneKeyframes()
        {
            var frames = MovingTargetFakeSequences.CreateStandardRun();
            Assert.AreEqual(TargetMovePhase.WaitingCountdown, frames[0].Phase);
            Assert.AreEqual(TargetMovePhase.Completed, frames[frames.Count - 1].Phase);

            var result = MovingTargetFakeSequences.CreateResult(hitCount: 4);
            Assert.AreEqual(ResultGrade.Good, result.Grade);
            Assert.AreEqual(2, result.FireSequences.Count);
        }

        string Start(float speed)
        {
            var result = movingTarget.StartSession(Settings(speed), RandomSeed.Fixed(11));
            Assert.IsTrue(result.Success, result.Message);
            return result.Data.SessionId;
        }

        ServiceResult<MovingTargetShotRecordDto> Record(string sessionId, string sequenceId, int index, bool hit)
        {
            return movingTarget.RecordShot(sessionId, sequenceId, index, new WeaponShotResultDto
            {
                SessionId = sessionId,
                IsValidShot = true,
                Hit = hit,
                ShotSequence = index
            });
        }

        static MovingTargetSettingsDto Settings(float speed)
        {
            return new MovingTargetSettingsDto { SpeedMetersPerSecond = speed };
        }
    }
}
