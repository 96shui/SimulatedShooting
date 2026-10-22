using NUnit.Framework;
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
        public void GenerateFixedOffset_FixedSeed_IsReproducible()
        {
            var first = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));
            var second = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));

            Assert.AreEqual(first.x, second.x, 0.0001f);
            Assert.AreEqual(first.y, second.y, 0.0001f);
        }

        [Test]
        public void GenerateFixedOffset_DifferentSeeds_ProduceDifferentOffsets()
        {
            var first = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));
            var second = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(200));

            Assert.IsFalse(Mathf.Approximately(first.x, second.x) && Mathf.Approximately(first.y, second.y));
        }

        [Test]
        public void Bdd29_InitialBiasIsSmallAndReproducibleAcrossSeeds()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var offset = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(seed));
                Assert.That(offset.magnitude, Is.InRange(3f, 8f));
                Assert.That(offset, Is.EqualTo(ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(seed))));
            }
        }

        [TestCase(10f, 0f, true)]
        [TestCase(6f, 8f, true)]
        [TestCase(-10f, 0f, true)]
        [TestCase(10.001f, 0f, false)]
        [TestCase(8f, 8f, false)]
        public void Bdd29_TenRingUsesTheLargerInclusiveCircularBoundary(float x, float y, bool expected)
            => Assert.That(ZeroingRules.IsInsideTenRing(new Vector2(x, y)), Is.EqualTo(expected));

        [Test]
        public void ComputeFrontSightDegrees_UsesCeilingAnd064CmPerDegree()
        {
            Assert.AreEqual(188f, ZeroingRules.ComputeFrontSightDegrees(12f), 0.01f);
            Assert.AreEqual(1f, ZeroingRules.ComputeFrontSightDegrees(0.01f), 0.01f);
        }

        [Test]
        public void ComputeRearSightClicks_UsesCeilingAndTwoCmPerClick()
        {
            Assert.AreEqual(4, ZeroingRules.ComputeRearSightClicks(8f));
            Assert.AreEqual(2, ZeroingRules.ComputeRearSightClicks(3.1f));
            Assert.AreEqual(2, ZeroingRules.ComputeRearSightClicks(4f));
        }

        [Test]
        public void September25_VerticalSuggestionUsesArchivedDirections()
        {
            Assert.AreEqual(VerticalAdjustmentDirection.Clockwise, ZeroingRules.ResolveVerticalDirection(-3f));
            Assert.AreEqual(VerticalAdjustmentDirection.CounterClockwise, ZeroingRules.ResolveVerticalDirection(3f));
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
        public void Bdd29_RayProjectionAccountsForMuzzleDepth()
        {
            var origin = new Vector3(.2f, .4f, 3f);
            var aim = ZeroingRules.ResolveAimPointFromRay(origin, new Vector3(.3f, -.2f, 97f), 100f);
            Assert.That(aim.x, Is.EqualTo(50f).Within(.001f));
            Assert.That(aim.y, Is.EqualTo(20f).Within(.001f));
        }
    }
}
