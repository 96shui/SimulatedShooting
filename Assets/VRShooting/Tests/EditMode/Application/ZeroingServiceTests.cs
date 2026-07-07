using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
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
        public void CompleteRound_CalculatesOffsetDirectionsAndAdjustmentAmounts()
        {
            var session = StartZeroingSession();

            Fire(session.SessionId, new Vector3(-8f, 12f, 100f));
            Fire(session.SessionId, new Vector3(-8f, 12f, 100f));
            Fire(session.SessionId, new Vector3(-8f, 12f, 100f));

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

            Fire(session.SessionId, new Vector3(-8f, 12f, 100f));
            Fire(session.SessionId, new Vector3(-8f, 12f, 100f));
            Fire(session.SessionId, new Vector3(-8f, 12f, 100f));
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

        ZeroingSessionDto StartZeroingSession()
        {
            var start = zeroing.StartSession(RandomSeed.Fixed(100), WeaponControlService.TrainingRifleId);
            Assert.IsTrue(start.Success, start.Message);
            var training = trainingSessions.Current;
            var weapon = weaponControl.StartSession(training.SessionId, training.WeaponId, training.Mode);
            Assert.IsTrue(weapon.Success, weapon.Message);
            return start.Data;
        }

        void Fire(string sessionId, Vector3 hitPoint)
        {
            var result = weaponControl.Fire(new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = Vector3.zero,
                AimDirection = Vector3.forward,
                WeaponPosition = Vector3.zero,
                Stability01 = 0.95f,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = true,
                HitPoint = hitPoint,
                HitObjectId = "Target_100m"
            });

            Assert.IsTrue(result.Success, result.Message);
        }
    }
}
