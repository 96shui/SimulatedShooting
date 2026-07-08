using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    [TestFixture]
    public class ZeroingServiceTests
    {
        GameEventBus eventBus;
        TrainingSessionService trainingSessions;
        WeaponControlService weaponControl;
        ZeroingService zeroing;

        [SetUp]
        public void SetUp()
        {
            eventBus = new GameEventBus();
            trainingSessions = new TrainingSessionService(eventBus);
            weaponControl = new WeaponControlService(eventBus);
            zeroing = new ZeroingService(eventBus, trainingSessions, weaponControl);
        }

        [Test]
        public void StartSession_FixedSeed_ProducesReproducibleOffset()
        {
            var first = StartZeroingSession(RandomSeed.Fixed(100));
            var secondSession = StartZeroingSession(RandomSeed.Fixed(100));

            Assert.AreEqual(first.FixedImpactOffsetCm.x, secondSession.FixedImpactOffsetCm.x, 0.0001f);
            Assert.AreEqual(first.FixedImpactOffsetCm.y, secondSession.FixedImpactOffsetCm.y, 0.0001f);
        }

        [Test]
        public void CompleteRound_CalculatesOffsetDirectionsAndAdjustmentAmounts()
        {
            var session = StartZeroingSession();

            RecordThreeImpacts(session.SessionId, new Vector2(-8f, 12f));

            var analysis = zeroing.CompleteRound(session.SessionId);

            Assert.IsTrue(analysis.Success, analysis.Message);
            Assert.AreEqual(3, analysis.Data.Shots.Count);
            Assert.AreEqual(-8f, analysis.Data.AverageOffsetCm.x, 0.01f);
            Assert.AreEqual(12f, analysis.Data.AverageOffsetCm.y, 0.01f);
            Assert.AreEqual(VerticalAdjustmentDirection.CounterClockwise, analysis.Data.VerticalDirection);
            Assert.AreEqual(HorizontalAdjustmentDirection.Forward, analysis.Data.HorizontalDirection);
            Assert.AreEqual(188f, analysis.Data.FrontSightDegreesToAdjust, 0.01f);
            Assert.AreEqual(4, analysis.Data.RearSightClicksToAdjust);
            Assert.IsFalse(analysis.Data.PassedTenRing);
        }

        [Test]
        public void ApplyAdjustment_IsIdempotentForSameRound()
        {
            var session = StartZeroingSession();

            RecordThreeImpacts(session.SessionId, new Vector2(-8f, 12f));
            var analysis = zeroing.CompleteRound(session.SessionId);

            var first = zeroing.ApplyAdjustment(session.SessionId, analysis.Data.RoundIndex);
            var second = zeroing.ApplyAdjustment(session.SessionId, analysis.Data.RoundIndex);
            var state = zeroing.GetSession(session.SessionId);

            Assert.IsTrue(first.Success, first.Message);
            Assert.IsTrue(second.Success, second.Message);
            Assert.IsTrue(second.Data.AdjustmentApplied);
            Assert.AreEqual(first.Data.FrontSightDegreesToAdjust, second.Data.FrontSightDegreesToAdjust);
            Assert.AreEqual(188f, state.Data.CurrentAdjustment.FrontSightDegrees, 0.01f);
            Assert.AreEqual(4, state.Data.CurrentAdjustment.RearSightClicks);
        }

        [Test]
        public void RecordShot_AcceptsUpToThreeShotsPerRound()
        {
            var session = StartZeroingSession();

            Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
        }

        [Test]
        public void RecordShot_FourthShot_ReturnsInvalidState()
        {
            var session = StartZeroingSession();
            RecordThreeImpacts(session.SessionId, Vector2.zero);

            var fourth = RecordImpact(session.SessionId, Vector2.zero);

            Assert.IsFalse(fourth.Success);
            Assert.AreEqual(ErrorCode.InvalidState, fourth.ErrorCode);
        }

        [Test]
        public void CompleteRound_BeforeThreeShots_ReturnsInvalidState()
        {
            var session = StartZeroingSession();
            RecordImpact(session.SessionId, Vector2.zero);

            var analysis = zeroing.CompleteRound(session.SessionId);

            Assert.IsFalse(analysis.Success);
            Assert.AreEqual(ErrorCode.InvalidState, analysis.ErrorCode);
        }

        [Test]
        public void RecordShot_AllInsideTenRing_PassedTenRingTrue()
        {
            var session = StartZeroingSession();
            RecordThreeImpacts(session.SessionId, new Vector2(1f, 1f));

            var analysis = zeroing.CompleteRound(session.SessionId);

            Assert.IsTrue(analysis.Success, analysis.Message);
            Assert.IsTrue(analysis.Data.PassedTenRing);
        }

        [Test]
        public void GetFinalResult_PassRound1_ReturnsExcellent()
        {
            var session = StartZeroingSession();
            CompleteRoundWithImpacts(session.SessionId, new Vector2(1f, 1f));
            zeroing.ApplyAdjustment(session.SessionId, 1);

            var result = zeroing.GetFinalResult(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(ResultGrade.Excellent, result.Data.Grade);
            Assert.AreEqual(1, result.Data.PassedRoundIndex);
        }

        [Test]
        public void GetFinalResult_PassRound2_ReturnsGood()
        {
            var session = StartZeroingSession();
            CompleteRoundWithImpacts(session.SessionId, new Vector2(-8f, 12f));
            zeroing.ApplyAdjustment(session.SessionId, 1);
            zeroing.ContinueAfterAnalysis(session.SessionId);
            CompleteRoundWithImpacts(session.SessionId, new Vector2(1f, 1f));
            zeroing.ApplyAdjustment(session.SessionId, 2);

            var result = zeroing.GetFinalResult(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(ResultGrade.Good, result.Data.Grade);
            Assert.AreEqual(2, result.Data.PassedRoundIndex);
        }

        [Test]
        public void GetFinalResult_PassRound3_ReturnsPass()
        {
            var session = StartZeroingSession();
            FailRound(session.SessionId);
            FailRound(session.SessionId);
            CompleteRoundWithImpacts(session.SessionId, new Vector2(1f, 1f));
            zeroing.ApplyAdjustment(session.SessionId, 3);

            var result = zeroing.GetFinalResult(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(ResultGrade.Pass, result.Data.Grade);
            Assert.AreEqual(3, result.Data.PassedRoundIndex);
        }

        [Test]
        public void GetFinalResult_AllRoundsFail_ReturnsFail()
        {
            var session = StartZeroingSession();
            FailRound(session.SessionId);
            FailRound(session.SessionId);
            FailRound(session.SessionId);

            var result = zeroing.GetFinalResult(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(ResultGrade.Fail, result.Data.Grade);
            Assert.AreEqual(0, result.Data.PassedRoundIndex);
        }

        [Test]
        public void ContinueAfterAnalysis_WithoutApply_ReturnsInvalidState()
        {
            var session = StartZeroingSession();
            CompleteRoundWithImpacts(session.SessionId, new Vector2(-8f, 12f));

            var next = zeroing.ContinueAfterAnalysis(session.SessionId);

            Assert.IsFalse(next.Success);
            Assert.AreEqual(ErrorCode.InvalidState, next.ErrorCode);
        }

        [Test]
        public void ContinueAfterAnalysis_StartsNextRound_ResetsShotsToThree()
        {
            var session = StartZeroingSession();
            CompleteRoundWithImpacts(session.SessionId, new Vector2(-8f, 12f));
            zeroing.ApplyAdjustment(session.SessionId, 1);

            var next = zeroing.ContinueAfterAnalysis(session.SessionId);
            var state = zeroing.GetSession(session.SessionId);

            Assert.IsTrue(next.Success, next.Message);
            Assert.AreEqual(2, state.Data.CurrentRound);
            Assert.AreEqual(3, state.Data.ShotsRemainingInRound);
            Assert.IsTrue(state.Data.CanShoot);
        }

        [Test]
        public void GetSession_NotFound_ReturnsNotFound()
        {
            var result = zeroing.GetSession("missing-session");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ErrorCode.NotFound, result.ErrorCode);
        }

        [Test]
        public void WeaponShotResultEvent_RecordsShotUsingTargetOffsetConvention()
        {
            var session = StartZeroingSession();
            var offset = session.FixedImpactOffsetCm;
            var aim = new Vector2(-8f, 12f) - offset;

            eventBus.Publish(new WeaponShotResultEvent
            {
                Result = new WeaponShotResultDto
                {
                    SessionId = session.SessionId,
                    IsValidShot = true,
                    Hit = true,
                    HitPoint = new Vector3(aim.x, aim.y, ZeroingRules.DistanceMeters),
                    AimDirection = Vector3.forward,
                    MuzzlePosition = Vector3.zero
                }
            });

            var state = zeroing.GetSession(session.SessionId);
            Assert.IsTrue(state.Success, state.Message);
            Assert.AreEqual(2, state.Data.ShotsRemainingInRound);
            Assert.AreEqual(1, state.Data.CurrentRound);
        }

        ZeroingSessionDto StartZeroingSession(RandomSeed? seed = null)
        {
            if (trainingSessions.HasActiveSession)
            {
                trainingSessions.End(trainingSessions.Current.SessionId, SessionEndReason.Completed);
            }

            var start = zeroing.StartSession(seed ?? RandomSeed.Fixed(100), WeaponControlService.TrainingRifleId);
            Assert.IsTrue(start.Success, start.Message);
            var training = trainingSessions.Current;
            var weapon = weaponControl.StartSession(training.SessionId, training.WeaponId, training.Mode);
            Assert.IsTrue(weapon.Success, weapon.Message);
            return start.Data;
        }

        void CompleteRoundWithImpacts(string sessionId, Vector2 impactCm)
        {
            RecordThreeImpacts(sessionId, impactCm);
            var analysis = zeroing.CompleteRound(sessionId);
            Assert.IsTrue(analysis.Success, analysis.Message);
        }

        void FailRound(string sessionId)
        {
            CompleteRoundWithImpacts(sessionId, new Vector2(-8f, 12f));
            var round = zeroing.GetSession(sessionId).Data.CurrentRound;
            zeroing.ApplyAdjustment(sessionId, round);
            if (round < 3)
            {
                var next = zeroing.ContinueAfterAnalysis(sessionId);
                Assert.IsTrue(next.Success, next.Message);
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
    }
}
