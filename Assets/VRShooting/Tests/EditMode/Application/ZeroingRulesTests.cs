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
        public void GenerateFixedOffset_IsInsideTargetOutsideTenRing()
        {
            var offset = ZeroingRules.GenerateFixedOffset(RandomSeed.Fixed(100));
            var distance = offset.magnitude;

            Assert.Greater(distance, ZeroingRules.TenRingRadiusCm);
            Assert.LessOrEqual(distance, ZeroingRules.TargetHalfSizeCm);
        }

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
        public void ResolveVerticalDirection_BiasDown_UsesClockwise()
        {
            Assert.AreEqual(VerticalAdjustmentDirection.Clockwise, ZeroingRules.ResolveVerticalDirection(-3f));
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
    }
}
