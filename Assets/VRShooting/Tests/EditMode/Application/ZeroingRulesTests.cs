using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    [TestFixture]
    public class ZeroingRulesTests
    {
        [Test]
        public void Bdd06_OppositeAdjustmentsCrossZeroWithoutExtraQuantizationStep()
        {
            var baseline = new SightAdjustmentDto { RearSightClicks = 1, FrontSightDegrees = 5f };
            var opposite = ZeroingRules.ApplyCorrection(baseline, new Vector2(-6f, .64f));
            Assert.That(opposite.RearSightClicks, Is.EqualTo(-2));
            Assert.That(opposite.FrontSightDegrees, Is.EqualTo(-5f));
            var same = ZeroingRules.ApplyCorrection(opposite, new Vector2(8f, -.96f));
            Assert.That(same.RearSightClicks, Is.EqualTo(2));
            Assert.That(same.FrontSightDegrees, Is.EqualTo(10f));

            var byDirection = ZeroingRules.ApplyAdjustment(baseline, new ZeroingRoundAnalysisDto
            {
                HorizontalDirection = HorizontalAdjustmentDirection.Backward, RearSightClicksToAdjust = 3,
                VerticalDirection = VerticalAdjustmentDirection.CounterClockwise, FrontSightDegreesToAdjust = 10
            });
            Assert.That(byDirection.RearSightClicks, Is.EqualTo(-2));
            Assert.That(byDirection.FrontSightDegrees, Is.EqualTo(-5f));
        }

        [Test]
        public void Bdd06_AnswerPredictsFixedResidualOnceFromActualPendingCorrection()
        {
            var shots = new[] { new ZeroingShotDto(), new ZeroingShotDto(), new ZeroingShotDto() };
            var correction = new Vector2(-8f, 11.84f);
            var before = ZeroingRules.BuildRoundAnalysis("answer", 1, shots, false, correction, new Vector2(10f, -12f));
            Assert.That(before.FixedImpactOffsetCm, Is.EqualTo(new Vector2(10f, -12f)));
            Assert.That(before.ProposedAdjustment.RearSightClicks, Is.EqualTo(-4));
            Assert.That(before.ProposedAdjustment.FrontSightDegrees, Is.EqualTo(-185f));
            Assert.That(before.PredictedOffsetMagnitudeCm, Is.EqualTo(2.00639f).Within(.001f));
            Assert.That(before.PredictedOffsetPasses, Is.True);

            var after = ZeroingRules.BuildRoundAnalysis("answer", 1, shots, true, correction, new Vector2(2f, -.16f));
            Assert.That(after.PredictedOffsetMagnitudeCm, Is.EqualTo(2.00639f).Within(.001f));
            Assert.That(after.PredictedOffsetPasses, Is.True);
            var notPassing = ZeroingRules.BuildRoundAnalysis("answer", 1, shots, false, Vector2.zero, new Vector2(3.01f, 4f));
            Assert.That(notPassing.PredictedOffsetPasses, Is.False);
        }
        [Test]
        public void Bdd06_OnlyAimOnBoardKeepsImpactWithinBoard()
        {
            Assert.That(ZeroingRules.ComputeImpactPoint(new Vector2(24, 0), new Vector2(8, 0)), Is.EqualTo(new Vector2(24.99f, 0)));
            Assert.That(ZeroingRules.ComputeImpactPoint(new Vector2(40, 0), new Vector2(8, 0)), Is.EqualTo(new Vector2(48, 0)));
            Assert.That(ZeroingRules.ComputeImpactPoint(new Vector2(10, 0), new Vector2(3, 0)), Is.EqualTo(new Vector2(13, 0)));
        }
        [Test]
        public void Bdd06_AverageUsesOnlyHitsOnTheTarget()
        {
            var shots = new[] {
                new ZeroingShotDto { ImpactPointCm = new Vector2(4, 6) },
                new ZeroingShotDto { ImpactPointCm = new Vector2(40, 10) },
                new ZeroingShotDto { ImpactPointCm = new Vector2(1, 2), MissedTargetPlane = true }
            };
            Assert.That(ZeroingRules.ComputeAverageOffset(shots), Is.EqualTo(new Vector2(4, 6)));
        }
        [Test]
        public void GenerateFixedOffset_FixedSeed_IsReproducible()
        {
            var first = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));
            var second = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));

            Assert.AreEqual(first.x, second.x, 0.0001f);
            Assert.AreEqual(first.y, second.y, 0.0001f);
        }

        [Test]
        public void Bdd06_RuntimeSeedIsNonFixedAndDifferentOnEachRequest()
        {
            var issued = new HashSet<int>();
            for (var i = 0; i < 1000; i++)
            {
                var seed = RandomSeed.CreateRandom();
                Assert.That(seed.IsFixed, Is.False);
                Assert.That(issued.Add(seed.Value), Is.True);
                Assert.That(ZeroingRules.GenerateFixedOffset(seed),
                    Is.EqualTo(ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(seed.Value))));
            }
        }

        [Test]
        public void GenerateFixedOffset_DifferentSeeds_ProduceDifferentOffsets()
        {
            var first = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));
            var second = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(200));

            Assert.IsFalse(Mathf.Approximately(first.x, second.x) && Mathf.Approximately(first.y, second.y));
        }

        [Test]
        public void Bdd06_GenerateFixedOffset_UsesIndependentAxesWithInclusiveEndpointsAndAllQuadrants()
        {
            var quadrants = new int[4];
            var xMin = false; var xMax = false; var yMin = false; var yMax = false;
            for (var seed = 0; seed < 100000; seed++)
            {
                var offset = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(seed));
                var x = Mathf.Abs(offset.x); var y = Mathf.Abs(offset.y);
                Assert.That(x, Is.InRange(5f, 20f));
                Assert.That(y, Is.InRange(5f, 20f));
                Assert.That(ZeroingRules.ComputeImpactPoint(Vector2.zero, offset), Is.EqualTo(offset));
                xMin |= x == 5f; xMax |= x == 20f; yMin |= y == 5f; yMax |= y == 20f;
                quadrants[(offset.x > 0 ? 1 : 0) + (offset.y > 0 ? 2 : 0)]++;
            }
            Assert.That(xMin && xMax && yMin && yMax, Is.True, "Both endpoints must be reachable on both axes.");
            foreach (var count in quadrants) Assert.That(count, Is.InRange(23000, 27000));
        }

        [TestCase(3f, 4f, true, true)]
        [TestCase(3.01f, 4f, true, false)]
        [TestCase(3f, 4f, false, false)]
        public void Bdd06_AdjustmentPass_UsesAppliedFixedOffsetAndIncludesFiveCentimetres(
            float x, float y, bool applied, bool expected)
        {
            var shots = new[] {
                new ZeroingShotDto { ImpactPointCm = new Vector2(15, 15) },
                new ZeroingShotDto { ImpactPointCm = new Vector2(15, 15) },
                new ZeroingShotDto { ImpactPointCm = new Vector2(15, 15) }
            };
            var result = ZeroingRules.BuildRoundAnalysis("test", 1, shots, applied,
                Vector2.zero, new Vector2(x, y));
            Assert.That(result.PassedTenRing, Is.EqualTo(expected));
            Assert.That(result.FinalResultAvailable, Is.EqualTo(expected));
        }

        [Test]
        public void ComputeFrontSightDegrees_RoundsSuggestionUpToFiveDegreeSteps()
        {
            // Recommendation rounds up to a complete 5° step, at 0.064cm per degree.
            Assert.AreEqual(190f, ZeroingRules.ComputeFrontSightDegrees(12f), 0.01f);
            Assert.AreEqual(5f, ZeroingRules.ComputeFrontSightDegrees(0.01f), 0.01f);
        }

        [Test]
        public void ComputeRearSightClicks_UsesCeilingAndTwoCmPerClick()
        {
            Assert.AreEqual(4, ZeroingRules.ComputeRearSightClicks(8f));
            Assert.AreEqual(2, ZeroingRules.ComputeRearSightClicks(3.1f));
            Assert.AreEqual(2, ZeroingRules.ComputeRearSightClicks(4f));
        }

        [Test]
        public void Bdd30_VerticalSuggestionMatchesTheSignedCompensation()
        {
            Assert.AreEqual(VerticalAdjustmentDirection.CounterClockwise, ZeroingRules.ResolveVerticalDirection(-3f));
            Assert.AreEqual(VerticalAdjustmentDirection.Clockwise, ZeroingRules.ResolveVerticalDirection(3f));
        }

        [Test]
        public void ResolveHorizontalDirection_BiasRight_UsesBackward()
        {
            Assert.AreEqual(HorizontalAdjustmentDirection.Backward, ZeroingRules.ResolveHorizontalDirection(3f));
        }

        [Test]
        public void ComputeFinalGrade_MapsPassedRoundToGrade()
        {
            Assert.AreEqual(ResultGrade.Excellent, ZeroingRules.ComputeFinalGrade(1));
            Assert.AreEqual(ResultGrade.Good, ZeroingRules.ComputeFinalGrade(2));
            Assert.AreEqual(ResultGrade.Pass, ZeroingRules.ComputeFinalGrade(3));
            Assert.AreEqual(ResultGrade.Fail, ZeroingRules.ComputeFinalGrade(0));
        }
        [Test]
        public void ResolveAimPointFromWeaponShot_UsesTargetOffsetConvention()
        {
            var result = new WeaponShotResultDto
            {
                Hit = true,
                HitPoint = new Vector3(-8f, 12f, ZeroingRules.DistanceMeters),
                AimDirection = Vector3.forward
            };

            var aim = ZeroingRules.ResolveAimPointFromWeaponShot(result);

            Assert.AreEqual(-8f, aim.x, 0.01f);
            Assert.AreEqual(12f, aim.y, 0.01f);
        }

        [Test]
        public void Bdd30_RayProjectionSubtractsTheMuzzleOrigin()
        {
            var origin = new Vector3(.2f, .4f, 3f);
            var aim = ZeroingRules.ResolveAimPointFromRay(origin, new Vector3(.3f, -.2f, 97f), 100f);
            Assert.That(aim.x, Is.EqualTo(50f).Within(.001f));
            Assert.That(aim.y, Is.EqualTo(20f).Within(.001f));
        }
    }
}
