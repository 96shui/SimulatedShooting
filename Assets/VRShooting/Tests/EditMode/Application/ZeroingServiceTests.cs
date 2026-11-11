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
        public void Bdd06_NewSessionStartsWithForwardOneClickAndClockwiseFiveDegrees()
        {
            var session = StartZeroingSession(RandomSeed.Fixed(100));
            var state = zeroing.GetSession(session.SessionId).Data;
            Assert.That(state.CurrentAdjustment.RearSightClicks, Is.EqualTo(1));
            Assert.That(state.CurrentAdjustment.FrontSightDegrees, Is.EqualTo(5f));
            Assert.That(state.FixedImpactOffsetCm, Is.EqualTo(ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100))));
        }

        [Test]
        public void Bdd06_ActualAppliedCorrectionAccumulatesFromBaselineAndIsIdempotent()
        {
            var session = StartZeroingSession();
            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f));
            var before = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(before.ProposedCorrectionCm, Is.EqualTo(Vector2.zero));
            for (var i = 0; i < 4; i++) zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            for (var i = 0; i < 37; i++) zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, -1);
            Assert.IsTrue(zeroing.ApplyAdjustment(session.SessionId, 1).Success);
            var applied = zeroing.GetSession(session.SessionId).Data;
            Assert.That(applied.CurrentAdjustment.RearSightClicks, Is.EqualTo(5));
            Assert.That(applied.CurrentAdjustment.FrontSightDegrees, Is.EqualTo(-180f));
            Assert.That(applied.FixedImpactOffsetCm.x, Is.EqualTo(session.FixedImpactOffsetCm.x + 8f).Within(.001f));
            Assert.That(applied.FixedImpactOffsetCm.y, Is.EqualTo(session.FixedImpactOffsetCm.y - 11.84f).Within(.001f));
            Assert.IsTrue(zeroing.ApplyAdjustment(session.SessionId, 1).Success);
            Assert.That(zeroing.GetSession(session.SessionId).Data.CurrentAdjustment, Is.EqualTo(applied.CurrentAdjustment));
        }

        [Test]
        public void Bdd06_ApplyAdjustmentWithSmallResidualPassesImmediatelyAndGradesWithoutAnotherShot()
        {
            var session = StartZeroingSession();
            for (var i = 0; i < 4; i++) Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            var before = zeroing.CompleteRound(session.SessionId).Data;
            Assert.IsFalse(before.PassedTenRing);
            var raw = before.Shots[0].ImpactPointCm;

            AdjustTowardCenter(session.SessionId, 1);
            var applied = zeroing.ApplyAdjustment(session.SessionId, 1);
            Assert.IsTrue(applied.Success, applied.Message);
            Assert.That((applied.Data.AverageOffsetCm + applied.Data.AppliedCorrectionCm).magnitude, Is.LessThan(5f));
            Assert.IsTrue(applied.Data.AdjustmentApplied);
            Assert.IsTrue(applied.Data.PassedTenRing);
            Assert.IsTrue(applied.Data.FinalResultAvailable);
            Assert.That(applied.Data.Shots[0].ImpactPointCm, Is.EqualTo(raw));
            Assert.That(zeroing.GetFinalResult(session.SessionId).Data.Grade, Is.EqualTo(ResultGrade.Excellent));
            Assert.That(zeroing.ContinueAfterAnalysis(session.SessionId).Data.CurrentRound, Is.EqualTo(1));
            Assert.IsTrue(zeroing.ApplyAdjustment(session.SessionId, 1).Success, "Repeated apply remains idempotent.");
        }

        [Test]
        public void Bdd07_AdjustmentPassInSecondRoundReturnsGood()
        {
            var session = StartZeroingSession();
            for (var i = 0; i < 4; i++) Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            var first = zeroing.ApplyAdjustment(session.SessionId, 1);
            Assert.IsTrue(first.Success, first.Message);
            Assert.IsFalse(first.Data.PassedTenRing);
            Assert.That(zeroing.ContinueAfterAnalysis(session.SessionId).Data.CurrentRound, Is.EqualTo(2));
            for (var i = 0; i < 4; i++) Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            AdjustTowardCenter(session.SessionId, 2);
            var second = zeroing.ApplyAdjustment(session.SessionId, 2);
            Assert.IsTrue(second.Success, second.Message);
            Assert.IsTrue(second.Data.PassedTenRing);
            Assert.IsTrue(second.Data.FinalResultAvailable);
            Assert.That(zeroing.GetFinalResult(session.SessionId).Data.Grade, Is.EqualTo(ResultGrade.Good));
        }

        [Test]
        public void Bdd06_ActualAimAndCorrectionAreNotCompressedOrClamped()
        {
            var aim = new Vector2(40f, -32f);
            var offset = new Vector2(-3f, 4f);
            var impact = ZeroingRules.ComputeImpactPoint(aim, offset);
            Assert.That(impact.x, Is.EqualTo(37f).Within(.001f));
            Assert.That(impact.y, Is.EqualTo(-28f).Within(.001f));
            Assert.That(ZeroingRules.IsInsideTenRing(impact), Is.False);
        }

        [Test]
        public void Bdd06_ApplyingSuggestedCorrectionRespectsMechanicalStepsAndIsIdempotent()
        {
            var session = StartZeroingSession();
            for (var i = 0; i < 4; i++) RecordImpact(session.SessionId, Vector2.zero);
            var before = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(before.PassedTenRing, Is.False);
            var original = before.Shots[0].ImpactPointCm;
            AdjustTowardCenter(session.SessionId, 1);
            Assert.That(zeroing.ApplyAdjustment(session.SessionId, 1).Success, Is.True);
            var effective = zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm;
            Assert.That(Mathf.Abs(effective.x), Is.LessThanOrEqualTo(1.001f));
            Assert.That(Mathf.Abs(effective.y), Is.LessThanOrEqualTo(.161f));
            zeroing.ApplyAdjustment(session.SessionId, 1);
            Assert.That(zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm, Is.EqualTo(effective));
            Assert.That(before.Shots[0].ImpactPointCm, Is.EqualTo(original));
            Assert.That(zeroing.CompleteRound(session.SessionId).Data.PassedTenRing, Is.True);
            Assert.That(zeroing.ContinueAfterAnalysis(session.SessionId).Data.CurrentRound, Is.EqualTo(1));
        }

        [Test]
        public void Bdd30_HelpRecommendationStaysBasedOnRoundImpactsWhilePendingCorrectionChanges()
        {
            var session = StartZeroingSession();
            RecordFourImpacts(session.SessionId, new Vector2(-8, 12));
            var before = zeroing.CompleteRound(session.SessionId).Data;
            var after = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1).Data;
            Assert.That(after.RearSightClicksToAdjust, Is.EqualTo(4));
            Assert.That(after.FrontSightDegreesToAdjust, Is.EqualTo(190));
            Assert.That(after.VerticalDirection, Is.EqualTo(VerticalAdjustmentDirection.CounterClockwise));
            Assert.That(after.ProposedCorrectionCm, Is.Not.EqualTo(before.ProposedCorrectionCm));
            Assert.That(after.AverageOffsetCm, Is.EqualTo(before.AverageOffsetCm));
        }

        [Test]
        public void Bdd30_UnprojectableMissConsumesARoundSlotAndNeverBecomesATenRing()
        {
            var session = StartZeroingSession();
            ZeroingShotDto shot = default;
            eventBus.Subscribe<ZeroingShotRecordedEvent>(e => shot = e.Shot);
            eventBus.Publish(new WeaponShotResultEvent { Result = new WeaponShotResultDto
            {
                SessionId = session.SessionId, IsValidShot = true, ShotSequence = 1,
                HasTargetPlaneSample = true, TargetPlaneRayValid = false, TargetPlanePointCm = -session.FixedImpactOffsetCm
            }});
            Assert.That(shot.MissedTargetPlane, Is.True);
            Assert.That(shot.InsideTenRing, Is.False);
            Assert.That(zeroing.GetSession(session.SessionId).Data.ShotsRemainingInRound, Is.EqualTo(3));
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

            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f));

            var analysis = zeroing.CompleteRound(session.SessionId);

            Assert.IsTrue(analysis.Success, analysis.Message);
            Assert.AreEqual(4, analysis.Data.Shots.Count);
            Assert.AreEqual(-8f, analysis.Data.AverageOffsetCm.x, 0.01f);
            Assert.AreEqual(12f, analysis.Data.AverageOffsetCm.y, 0.01f);
            Assert.AreEqual(VerticalAdjustmentDirection.CounterClockwise, analysis.Data.VerticalDirection);
            Assert.AreEqual(HorizontalAdjustmentDirection.Forward, analysis.Data.HorizontalDirection);
            Assert.AreEqual(190f, analysis.Data.FrontSightDegreesToAdjust, 0.01f);
            Assert.AreEqual(4, analysis.Data.RearSightClicksToAdjust);
            Assert.IsFalse(analysis.Data.PassedTenRing);
        }

        [Test]
        public void ApplyAdjustment_IsIdempotentForSameRound()
        {
            var session = StartZeroingSession();

            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f));
            var analysis = zeroing.CompleteRound(session.SessionId);

            AdjustTowardCenter(session.SessionId, analysis.Data.RoundIndex);
            var first = zeroing.ApplyAdjustment(session.SessionId, analysis.Data.RoundIndex);
            var second = zeroing.ApplyAdjustment(session.SessionId, analysis.Data.RoundIndex);
            var state = zeroing.GetSession(session.SessionId);

            Assert.IsTrue(first.Success, first.Message);
            Assert.IsTrue(second.Success, second.Message);
            Assert.IsTrue(second.Data.AdjustmentApplied);
            Assert.AreEqual(first.Data.FrontSightDegreesToAdjust, second.Data.FrontSightDegreesToAdjust);
            // The actual pending correction is 185°; the mechanical baseline is +5°.
            Assert.AreEqual(-185f, state.Data.CurrentAdjustment.FrontSightDegrees, 0.01f);
            Assert.AreEqual(5, state.Data.CurrentAdjustment.RearSightClicks);
        }

        [Test]
        public void Bdd30_ManualPreviewCommitsSignedCorrectionWithoutChangingHistory()
        {
            // BDD 06: three-shot mean is immutable; player moves only the pending correction.
            var session = StartZeroingSession();
            var originalOffset = session.FixedImpactOffsetCm;
            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f));
            var initial = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(initial.ProposedCorrectionCm, Is.EqualTo(Vector2.zero));
            Assert.That(initial.PreviewAverageOffsetCm, Is.EqualTo(new Vector2(-8f, 12f)));

            var horizontal = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            var vertical = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, -1);
            Assert.IsTrue(horizontal.Success, horizontal.Message);
            Assert.IsTrue(vertical.Success, vertical.Message);
            Assert.AreEqual(new Vector2(-8f, 12f), vertical.Data.AverageOffsetCm);
            Assert.That(Vector2.Distance(new Vector2(2f, -.32f), vertical.Data.ProposedCorrectionCm), Is.LessThan(.001f));
            Assert.That(Vector2.Distance(new Vector2(-6f, 11.68f), vertical.Data.PreviewAverageOffsetCm), Is.LessThan(.001f));

            var applied = zeroing.ApplyAdjustment(session.SessionId, 1);
            var duplicate = zeroing.ApplyAdjustment(session.SessionId, 1);
            Assert.IsTrue(applied.Success, applied.Message);
            Assert.IsTrue(duplicate.Success, duplicate.Message);
            Assert.That(Vector2.Distance(new Vector2(2f, -.32f), applied.Data.ProposedCorrectionCm), Is.LessThan(.001f));
            Assert.AreEqual(originalOffset + applied.Data.ProposedCorrectionCm,
                zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm);
            Assert.IsTrue(zeroing.ContinueAfterAnalysis(session.SessionId).Success);

            var sameAim = new Vector2(-8f, 12f) - originalOffset;
            var nextShot = RecordImpact(session.SessionId, sameAim);
            Assert.IsTrue(nextShot.Success, nextShot.Message);
            Assert.That(Vector2.Distance(vertical.Data.PreviewAverageOffsetCm, nextShot.Data.ImpactPointCm), Is.LessThan(.001f));
        }

        [Test]
        public void AdjustImpactPoint_RejectsIncompleteOrAppliedRoundAndInvalidDirection()
        {
            var session = StartZeroingSession();
            var incomplete = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            Assert.AreEqual(ErrorCode.InvalidState, incomplete.ErrorCode);
            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f));
            var invalid = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, 0);
            Assert.AreEqual(ErrorCode.InvalidInput, invalid.ErrorCode);
            zeroing.ApplyAdjustment(session.SessionId, 1);
            var applied = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1);
            Assert.AreEqual(ErrorCode.InvalidState, applied.ErrorCode);
        }

        [TestCase(24f, -1)]
        [TestCase(-24f, 1)]
        public void Bdd06_LargeOnTargetSuggestedAngleRemainsAdjustable(float biasCm, int direction)
        {
            var session = StartZeroingSession();
            RecordFourImpacts(session.SessionId, new Vector2(0f, biasCm));
            var before = zeroing.CompleteRound(session.SessionId).Data;
            // ±64cm is outside the 50cm target and no longer contributes to the mean.
            Assert.That(before.FrontSightDegreesToAdjust, Is.EqualTo(375f).Within(.01f));
            var step = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, direction);
            Assert.That(step.Success, Is.True);
            Assert.That(step.Data.ProposedCorrectionCm.y,
                Is.EqualTo(before.ProposedCorrectionCm.y + direction * .32f).Within(.001f),
                "A large on-target suggestion must still allow a full five-degree step.");
            Assert.That(step.Data.ProposedCorrectionCm.x, Is.EqualTo(before.ProposedCorrectionCm.x));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Bdd06_VerticalCorrectionSaturatesAfterMultipleTurnsAndAppliesOnce(int direction)
        {
            var session = StartZeroingSession();
            // Start on the opposite side of zero so both directions cross it.
            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f * direction));
            var before = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(before.ProposedCorrectionCm.y, Is.Zero);

            ZeroingRoundAnalysisDto crossedZero = default;
            ZeroingRoundAnalysisDto saturated = default;
            // Preserve the original 1600° traversal using 320 five-degree steps.
            for (var i = 0; i < 320; i++)
            {
                var step = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, direction);
                Assert.That(step.Success, Is.True, step.Message);
                if (i == 39) crossedZero = step.Data;
                if (i == 239) saturated = step.Data;
            }

            var adjusted = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(crossedZero.ProposedCorrectionCm.y * direction, Is.GreaterThan(0f));
            Assert.That(saturated.ProposedCorrectionCm.y, Is.EqualTo(direction * 49.92f).Within(.001f));
            Assert.That(adjusted.ProposedCorrectionCm.y, Is.EqualTo(direction * 49.92f).Within(.001f),
                "Further same-direction steps must not increase the saturated correction.");
            Assert.That(adjusted.ProposedCorrectionCm.x, Is.EqualTo(before.ProposedCorrectionCm.x));
            Assert.That(adjusted.AverageOffsetCm, Is.EqualTo(before.AverageOffsetCm));
            Assert.That(zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm, Is.EqualTo(session.FixedImpactOffsetCm));
            Assert.That(zeroing.ApplyAdjustment(session.SessionId, 1).Success, Is.True);
            var applied = zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm;
            Assert.That(applied.y - session.FixedImpactOffsetCm.y, Is.EqualTo(direction * 49.92f).Within(.001f));
            Assert.That(applied.x - session.FixedImpactOffsetCm.x, Is.EqualTo(adjusted.ProposedCorrectionCm.x).Within(.001f));
            Assert.That(zeroing.ApplyAdjustment(session.SessionId, 1).Success, Is.True);
            Assert.That(zeroing.GetSession(session.SessionId).Data.FixedImpactOffsetCm, Is.EqualTo(applied));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Bdd06_VerticalCorrectionExitsSaturationOnFirstReverseStep(int direction)
        {
            var session = StartZeroingSession();
            RecordFourImpacts(session.SessionId, new Vector2(-8f, 12f * direction));
            zeroing.CompleteRound(session.SessionId);
            for (var i = 0; i < 320; i++)
                Assert.That(zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, direction).Success, Is.True);

            var saturated = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(saturated.ProposedCorrectionCm.y, Is.EqualTo(direction * 49.92f).Within(.001f));
            var reversed = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, -direction);
            Assert.That(reversed.Success, Is.True, reversed.Message);
            Assert.That(reversed.Data.ProposedCorrectionCm.y, Is.EqualTo(direction * 49.60f).Within(.001f),
                "反向一步立即按 0.32cm 退出饱和，不做超出量抵扣");
        }

        [Test]
        public void RecordShot_AcceptsUpToFourShotsPerRound()
        {
            var session = StartZeroingSession();

            Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
            Assert.IsTrue(RecordImpact(session.SessionId, Vector2.zero).Success);
        }

        [Test]
        public void RecordShot_FifthShot_ReturnsInvalidState()
        {
            var session = StartZeroingSession();
            RecordFourImpacts(session.SessionId, Vector2.zero);

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
            RecordFourImpacts(session.SessionId, new Vector2(1f, 1f));

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
            RecordFourImpacts(session.SessionId,passRound==0?new Vector2(12,12):Vector2.zero);
            var analysis=zeroing.CompleteRound(session.SessionId).Data;
            Assert.IsTrue(analysis.FinalResultAvailable);
            Assert.IsFalse(analysis.AdjustmentApplied);
            Assert.AreEqual(expected,zeroing.GetFinalResult(session.SessionId).Data.Grade);
            Assert.IsTrue(zeroing.ContinueAfterAnalysis(session.SessionId).Success);
            if (passRound != 0)
            {
                Assert.AreEqual(ErrorCode.InvalidState,zeroing.ApplyAdjustment(session.SessionId,finalRound).ErrorCode);
                Assert.AreEqual(ErrorCode.InvalidState,zeroing.AdjustImpactPoint(session.SessionId,finalRound,ZeroingAdjustmentAxis.Horizontal,1).ErrorCode);
            }
            else Assert.IsTrue(zeroing.ApplyAdjustment(session.SessionId, finalRound).Success);
            Assert.AreEqual(finalRound,zeroing.GetSession(session.SessionId).Data.CurrentRound);
        }

        [Test]
        public void ContinueAfterAnalysis_StartsNextRound_ResetsShotsToFour()
        {
            var session = StartZeroingSession();
            CompleteRoundWithImpacts(session.SessionId, new Vector2(-8f, 12f));
            zeroing.ApplyAdjustment(session.SessionId, 1);

            var next = zeroing.ContinueAfterAnalysis(session.SessionId);
            var state = zeroing.GetSession(session.SessionId);

            Assert.IsTrue(next.Success, next.Message);
            Assert.AreEqual(2, state.Data.CurrentRound);
            Assert.AreEqual(4, state.Data.ShotsRemainingInRound);
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
            Assert.AreEqual(3, state.Data.ShotsRemainingInRound);
            Assert.AreEqual(1, state.Data.CurrentRound);
        }

        // BDD06: rejection must be enforced by the service as well as by the UI.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Bdd06_SpreadRoundRejectsBothCommandsAndAdvances(int valid)
        {
            var session = StartZeroingSession();
            for (var i = 0; i < 4; i++)
                eventBus.Publish(new WeaponShotResultEvent { Result = new WeaponShotResultDto
                {
                    SessionId = session.SessionId, IsValidShot = true, ShotSequence = i + 1,
                    HasTargetPlaneSample = true, TargetPlaneRayValid = i < valid,
                    TargetPlanePointCm = new Vector2(8, 8) - session.FixedImpactOffsetCm
                }});
            var analysis = zeroing.CompleteRound(session.SessionId).Data;
            Assert.That(analysis.SpreadRejected, Is.True);
            Assert.That(zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Horizontal, 1).ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(zeroing.ApplyAdjustment(session.SessionId, 1).ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(zeroing.ContinueAfterAnalysis(session.SessionId).Data.CurrentRound, Is.EqualTo(2));
        }

        [Test]
        public void Bdd06_PendingCorrectionStartsAtZeroAndClockwiseMovesUp()
        {
            var session = StartZeroingSession();
            var aim = new Vector2(8, 8) - session.FixedImpactOffsetCm;
            for (var i = 0; i < 4; i++) Assert.That(RecordImpact(session.SessionId, aim).Success, Is.True);
            Assert.That(zeroing.CompleteRound(session.SessionId).Data.ProposedCorrectionCm, Is.EqualTo(Vector2.zero));
            var changed = zeroing.AdjustImpactPoint(session.SessionId, 1, ZeroingAdjustmentAxis.Vertical, 1);
            Assert.That(changed.Data.ProposedCorrectionCm.y, Is.EqualTo(.32f).Within(.0001f));
            var applied = zeroing.ApplyAdjustment(session.SessionId, 1);
            Assert.That(applied.Data.AppliedCorrectionCm.y, Is.EqualTo(.32f).Within(.0001f));
            Assert.That(zeroing.ApplyAdjustment(session.SessionId, 1).Data.FixedImpactOffsetCm, Is.EqualTo(applied.Data.FixedImpactOffsetCm));
        }

        void AdjustTowardCenter(string id, int round)
        {
            var average = zeroing.CompleteRound(id).Data.AverageOffsetCm;
            var x = Mathf.RoundToInt(-average.x / 2f);
            var y = Mathf.RoundToInt(-average.y / .32f);
            for (var i = 0; i < Mathf.Abs(x); i++) Assert.IsTrue(zeroing.AdjustImpactPoint(id, round, ZeroingAdjustmentAxis.Horizontal, x > 0 ? 1 : -1).Success);
            for (var i = 0; i < Mathf.Abs(y); i++) Assert.IsTrue(zeroing.AdjustImpactPoint(id, round, ZeroingAdjustmentAxis.Vertical, y > 0 ? 1 : -1).Success);
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
            RecordFourImpacts(sessionId, impactCm);
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

        void RecordFourImpacts(string sessionId, Vector2 impactCm)
        {
            var offset = zeroing.GetSession(sessionId).Data.FixedImpactOffsetCm;
            var aim = impactCm - offset;
            Assert.IsTrue(RecordImpact(sessionId, aim).Success);
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
