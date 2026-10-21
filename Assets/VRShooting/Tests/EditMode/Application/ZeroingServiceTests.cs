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
            Assert.AreEqual(VerticalAdjustmentDirection.Clockwise, analysis.Data.VerticalDirection);
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
        public void AdjustImpactPoint_UsesSeparateAxesAndAffectsNextRoundOnce()
        {
            // BDD 06: three-shot mean is immutable; player moves only the pending correction.
            var session = StartZeroingSession();
            var originalOffset = session.FixedImpactOffsetCm;
            RecordThreeImpacts(session.SessionId, new Vector2(-8f, 12f));
            var initial = zeroing.CompleteRound(session.SessionId).Data;
            Assert.AreEqual(8f, initial.ProposedCorrectionCm.x, 0.001f);
            Assert.AreEqual(-12f, initial.ProposedCorrectionCm.y, 0.001f);
            Assert.AreEqual(Vector2.zero, initial.PreviewAverageOffsetCm);

            var horizontal = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            var vertical = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, -1);
            Assert.IsTrue(horizontal.Success, horizontal.Message);
            Assert.IsTrue(vertical.Success, vertical.Message);
            Assert.AreEqual(new Vector2(-8f, 12f), vertical.Data.AverageOffsetCm);
            Assert.AreEqual(new Vector2(10f, -12.064f), vertical.Data.ProposedCorrectionCm);
            Assert.That(Vector2.Distance(new Vector2(2f, -.064f), vertical.Data.PreviewAverageOffsetCm), Is.LessThan(.001f));

            var applied = zeroing.ApplyAdjustment(session.SessionId, 1);
            var duplicate = zeroing.ApplyAdjustment(session.SessionId, 1);
            Assert.IsTrue(applied.Success, applied.Message);
            Assert.IsTrue(duplicate.Success, duplicate.Message);
            Assert.AreEqual(new Vector2(10f, -12.064f), applied.Data.ProposedCorrectionCm);
            Assert.AreEqual(originalOffset + new Vector2(10f, -12.064f),
                zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm);
            Assert.IsTrue(zeroing.ContinueAfterAnalysis(session.SessionId).Success);

            var sameAim = new Vector2(-8f, 12f) - originalOffset;
            var nextShot = RecordImpact(session.SessionId, sameAim);
            Assert.IsTrue(nextShot.Success, nextShot.Message);
            Assert.That(Vector2.Distance(new Vector2(2f, -.064f), nextShot.Data.ImpactPointCm), Is.LessThan(.001f));
        }

        [Test]
        public void AdjustImpactPoint_RejectsIncompleteOrAppliedRoundAndInvalidDirection()
        {
            var session = StartZeroingSession();
            var incomplete = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            Assert.AreEqual(ErrorCode.InvalidState, incomplete.ErrorCode);
            RecordThreeImpacts(session.SessionId, new Vector2(-8f, 12f));
            var invalid = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, 0);
            Assert.AreEqual(ErrorCode.InvalidInput, invalid.ErrorCode);
            zeroing.ApplyAdjustment(session.SessionId, 1);
            var applied = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            Assert.AreEqual(ErrorCode.InvalidState, applied.ErrorCode);
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

        // BDD07: terminal rounds preserve the raw score and require no final correction.
        [TestCase(1,ResultGrade.Excellent)]
        [TestCase(2,ResultGrade.Good)]
        [TestCase(3,ResultGrade.Pass)]
        [TestCase(0,ResultGrade.Fail)]
        public void TerminalRound_ProvidesGradeWithoutApplyingLastAdjustment(int passRound,ResultGrade expected)
        {
            var session=StartZeroingSession();
            var finalRound=passRound==0?3:passRound;
            for(var round=1;round<finalRound;round++)FailRound(session.SessionId);
            RecordThreeImpacts(session.SessionId,passRound==0?new Vector2(12,12):Vector2.zero);
            var analysis=zeroing.CompleteRound(session.SessionId).Data;
            Assert.IsTrue(analysis.FinalResultAvailable);
            Assert.IsFalse(analysis.AdjustmentApplied);
            Assert.AreEqual(expected,zeroing.GetFinalResult(session.SessionId).Data.Grade);
            Assert.IsTrue(zeroing.ContinueAfterAnalysis(session.SessionId).Success);
            Assert.AreEqual(ErrorCode.InvalidState,zeroing.ApplyAdjustment(session.SessionId,finalRound).ErrorCode);
            Assert.AreEqual(ErrorCode.InvalidState,zeroing.AdjustImpactPoint(session.SessionId,finalRound,ZeroingAdjustmentAxis.Horizontal,1).ErrorCode);
            Assert.AreEqual(finalRound,zeroing.GetSession(session.SessionId).Data.CurrentRound);
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
            if (round < 3)
            {
                zeroing.ApplyAdjustment(sessionId, round);
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
