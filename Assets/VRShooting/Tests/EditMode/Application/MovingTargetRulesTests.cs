using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Common;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// 移动靶纯规则。追溯 docs/BDD/screens/08-移动靶设置.feature.md 与 11-移动靶结算.feature.md。
    /// </summary>
    [TestFixture]
    public class MovingTargetRulesTests
    {
        [Test]
        public void Screen08_AvailableSpeeds_AreThreeFourAndFive()
        {
            Assert.AreEqual(new[] { 3f, 4f, 5f }, MovingTargetRules.AvailableSpeeds);
            Assert.IsTrue(MovingTargetRules.IsAllowedSpeed(3f));
            Assert.IsTrue(MovingTargetRules.IsAllowedSpeed(4f));
            Assert.IsTrue(MovingTargetRules.IsAllowedSpeed(5f));
            Assert.IsFalse(MovingTargetRules.IsAllowedSpeed(2f));
            Assert.IsFalse(MovingTargetRules.IsAllowedSpeed(3.5f));
            Assert.IsFalse(MovingTargetRules.IsAllowedSpeed(6f));
        }

        [TestCase(0, ResultGrade.Fail)]
        [TestCase(2, ResultGrade.Fail)]
        [TestCase(3, ResultGrade.Pass)]
        [TestCase(4, ResultGrade.Good)]
        [TestCase(5, ResultGrade.Excellent)]
        [TestCase(8, ResultGrade.Excellent)]
        public void Screen11_ComputeGrade_UsesHitCountThresholds(int hits, ResultGrade expected)
        {
            Assert.AreEqual(expected, MovingTargetRules.ComputeGrade(hits));
        }

        [Test]
        public void Screen11_HitRate_IsZeroWhenNoShots()
        {
            Assert.AreEqual(0f, MovingTargetRules.ComputeHitRate01(0, 0));
            Assert.AreEqual(0.5f, MovingTargetRules.ComputeHitRate01(1, 2), 0.0001f);
        }

        [Test]
        public void Screen09_Advance_CountdownBoundaryLeavesTargetStationaryThenStartsRightToLeft()
        {
            var state = MovingTargetRules.CreateInitial();
            var afterTwo = MovingTargetRules.Advance(state, 4f, 2f);

            Assert.AreEqual(TargetMovePhase.WaitingCountdown, afterTwo.Phase);
            Assert.AreEqual(1f, afterTwo.CountdownSecondsRemaining, 0.0001f);
            Assert.AreEqual(0f, afterTwo.RouteProgress01);
            Assert.IsFalse(MovingTargetRules.CanShoot(afterTwo.Phase));

            var afterThree = MovingTargetRules.Advance(afterTwo, 4f, 1f);
            Assert.AreEqual(TargetMovePhase.MovingRightToLeft, afterThree.Phase);
            Assert.AreEqual(0f, afterThree.CountdownSecondsRemaining, 0.0001f);
            Assert.AreEqual(0f, afterThree.RouteProgress01);
            Assert.IsTrue(afterThree.CountdownElapsedThisTick);
            Assert.IsTrue(MovingTargetRules.CanShoot(afterThree.Phase));
        }

        [Test]
        public void Screen09_Advance_LargeStepConsumesCountdownHoldAndBothLegs()
        {
            var completed = MovingTargetRules.Advance(MovingTargetRules.CreateInitial(), 4f, 100f);

            Assert.AreEqual(TargetMovePhase.Completed, completed.Phase);
            Assert.AreEqual(0f, completed.RouteProgress01, 0.0001f);
            Assert.AreEqual(1f, completed.LegProgress01, 0.0001f);
            Assert.IsTrue(completed.CompletedThisTick);
            Assert.AreEqual(3f + 10f + 2f + 10f, completed.ElapsedSeconds, 0.001f);
        }

        [Test]
        public void Screen09_Advance_RightToLeftReachesLeftThenHoldsThenReturns()
        {
            var afterCountdown = MovingTargetRules.Advance(MovingTargetRules.CreateInitial(), 4f, 3f);
            var midLeg = MovingTargetRules.Advance(afterCountdown, 4f, 5f);
            Assert.AreEqual(TargetMovePhase.MovingRightToLeft, midLeg.Phase);
            Assert.AreEqual(0.5f, midLeg.RouteProgress01, 0.0001f);
            Assert.AreEqual(0.5f, midLeg.LegProgress01, 0.0001f);
            Assert.AreEqual("右→左", MovingTargetRules.DirectionLabel(midLeg.Phase));

            var atLeft = MovingTargetRules.Advance(midLeg, 4f, 5f);
            Assert.AreEqual(TargetMovePhase.LeftEndpointHold, atLeft.Phase);
            Assert.AreEqual(1f, atLeft.RouteProgress01, 0.0001f);
            Assert.AreEqual(2f, atLeft.EndpointHoldSecondsRemaining, 0.0001f);
            Assert.IsFalse(MovingTargetRules.CanShoot(atLeft.Phase));
            Assert.AreEqual("左端停留", MovingTargetRules.DirectionLabel(atLeft.Phase));

            var afterHold = MovingTargetRules.Advance(atLeft, 4f, 2f);
            Assert.AreEqual(TargetMovePhase.MovingLeftToRight, afterHold.Phase);
            Assert.IsTrue(MovingTargetRules.CanShoot(afterHold.Phase));
            Assert.AreEqual("左→右", MovingTargetRules.DirectionLabel(afterHold.Phase));

            var finished = MovingTargetRules.Advance(afterHold, 4f, 10f);
            Assert.AreEqual(TargetMovePhase.Completed, finished.Phase);
            Assert.AreEqual(0f, finished.RouteProgress01, 0.0001f);
        }
    }
}
