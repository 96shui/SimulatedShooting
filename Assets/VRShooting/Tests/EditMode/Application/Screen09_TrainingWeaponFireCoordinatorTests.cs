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
    /// 连射与移动靶禁射/倒计时/结算的应用协调。
    /// 追溯 docs/BDD/screens/09-移动靶HUD.feature.md、00-P1P2卧姿固定射击交互.feature.md。
    /// </summary>
    [TestFixture]
    public sealed class Screen09_TrainingWeaponFireCoordinatorTests
    {
        GameEventBus eventBus;
        TrainingSessionService sessions;
        WeaponControlService weapons;
        ZeroingService zeroing;
        MovingTargetService movingTarget;
        TrainingPresentationService presentation;
        WeaponAutomaticFireService autoFire;
        TrainingWeaponFireCoordinator coordinator;
        List<WeaponShotResultDto> shots;

        [SetUp]
        public void SetUp()
        {
            eventBus = new GameEventBus();
            sessions = new TrainingSessionService(eventBus);
            weapons = new WeaponControlService(eventBus);
            zeroing = new ZeroingService(eventBus, sessions, weapons);
            movingTarget = new MovingTargetService(eventBus, sessions);
            presentation = new TrainingPresentationService(eventBus, sessions, weapons, zeroing);
            autoFire = new WeaponAutomaticFireService(weapons, weapons, eventBus);
            coordinator = new TrainingWeaponFireCoordinator(
                eventBus,
                sessions,
                presentation,
                weapons,
                autoFire,
                movingTarget);
            shots = new List<WeaponShotResultDto>();
            eventBus.Subscribe<WeaponShotResultEvent>(evt =>
            {
                if (evt.Result.IsValidShot)
                {
                    shots.Add(evt.Result);
                }
            });
        }

        [Test]
        public void Screen09_Coordinator_IgnoresTriggerDuringCountdownThenFiresTwoOnQuickTap()
        {
            var sessionId = StartMovingTargetLive();
            coordinator.Tick(sessionId, 1f, Press(sessionId), Snapshot(sessionId));
            Assert.AreEqual(0, shots.Count);
            Assert.IsFalse(movingTarget.GetSession(sessionId).Data.CanShoot);

            coordinator.Tick(sessionId, 2f, Release(sessionId), Snapshot(sessionId));
            Assert.IsTrue(movingTarget.GetSession(sessionId).Data.CanShoot);

            coordinator.Tick(sessionId, 0.2f, PressAndRelease(sessionId), Snapshot(sessionId));
            Assert.AreEqual(2, shots.Count);
            Assert.AreEqual(2, movingTarget.GetSession(sessionId).Data.ShotsFired);
            Assert.AreEqual(2, movingTarget.GetSession(sessionId).Data.HitCount);
        }

        [Test]
        public void Screen09_Coordinator_StopsOnLeftHoldAndDoesNotResumeWhileHeld()
        {
            var sessionId = StartMovingTargetLive();
            coordinator.Tick(sessionId, 3f, Idle(sessionId), Snapshot(sessionId));

            coordinator.Tick(sessionId, 0.25f, Press(sessionId), Snapshot(sessionId));
            var beforeHold = shots.Count;
            Assert.That(beforeHold, Is.GreaterThanOrEqualTo(2));

            coordinator.Tick(sessionId, 10f, Hold(sessionId), Snapshot(sessionId));
            Assert.AreEqual(TargetMovePhase.LeftEndpointHold, movingTarget.GetSession(sessionId).Data.Phase);
            var duringHold = shots.Count;

            coordinator.Tick(sessionId, 2f, Hold(sessionId), Snapshot(sessionId));
            Assert.AreEqual(duringHold, shots.Count);
            Assert.IsFalse(autoFire.GetState(sessionId).Data.TriggerArmedForNewSequence);

            coordinator.Tick(sessionId, 1f, Hold(sessionId), Snapshot(sessionId));
            Assert.AreEqual(duringHold, shots.Count);
        }

        [Test]
        public void Screen09_Coordinator_CompletesTrainingAndKeepsFireSequences()
        {
            var sessionId = StartMovingTargetLive();
            coordinator.Tick(sessionId, 3f, Idle(sessionId), Snapshot(sessionId));
            coordinator.Tick(sessionId, 0.2f, PressAndRelease(sessionId), Snapshot(sessionId));
            coordinator.Tick(sessionId, 100f, Idle(sessionId), Snapshot(sessionId));

            var result = movingTarget.CompleteSession(sessionId);
            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(2, result.Data.TotalShotsFired);
            Assert.AreEqual(1, result.Data.FireSequences.Count);
            Assert.AreEqual(2, result.Data.FireSequences[0].Shots.Count);
            Assert.AreEqual(TrainingPresentationPhase.SessionResults, presentation.Get(sessionId).Data.Phase);
        }

        [Test]
        public void Screen00_Coordinator_P1HeldTriggerStaysSingleShotAndCompletesRound()
        {
            presentation.Enter(TrainingMode.Zeroing100m);
            var started = zeroing.StartSession(RandomSeed.Fixed(7), WeaponControlService.TrainingRifleId);
            Assert.IsTrue(started.Success, started.Message);
            var sessionId = started.Data.SessionId;
            Assert.IsTrue(presentation.ConfirmStart(sessionId).Success);
            Assert.IsTrue(weapons.SetGripState(TwoHand(sessionId)).Success);

            for (var i = 0; i < 3; i++)
            {
                coordinator.Tick(sessionId, 0.5f, Press(sessionId), ZeroingSnapshot(sessionId));
                coordinator.Tick(sessionId, 0.5f, Hold(sessionId), ZeroingSnapshot(sessionId));
                coordinator.Tick(sessionId, 0f, Release(sessionId), ZeroingSnapshot(sessionId));
            }

            Assert.AreEqual(3, shots.Count);
            var analysis = zeroing.CompleteRound(sessionId);
            Assert.IsTrue(analysis.Success, analysis.Message);
            Assert.AreEqual(3, analysis.Data.Shots.Count);
        }

        string StartMovingTargetLive()
        {
            presentation.Enter(TrainingMode.MovingTarget);
            var started = movingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f },
                RandomSeed.Fixed(21));
            Assert.IsTrue(started.Success, started.Message);
            var sessionId = started.Data.SessionId;
            Assert.IsTrue(presentation.ConfirmStart(sessionId).Success, "confirm start");
            Assert.IsTrue(weapons.SetGripState(TwoHand(sessionId)).Success);
            return sessionId;
        }

        static WeaponGripStateInputDto TwoHand(string sessionId)
        {
            return new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.9f
            };
        }

        static WeaponTriggerStateInputDto Idle(string sessionId)
        {
            return new WeaponTriggerStateInputDto { SessionId = sessionId };
        }

        static WeaponTriggerStateInputDto Press(string sessionId)
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = sessionId,
                Value01 = 1f,
                Pressed = true,
                Held = true
            };
        }

        static WeaponTriggerStateInputDto Hold(string sessionId)
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = sessionId,
                Value01 = 1f,
                Held = true
            };
        }

        static WeaponTriggerStateInputDto Release(string sessionId)
        {
            return new WeaponTriggerStateInputDto { SessionId = sessionId, Released = true };
        }

        static WeaponTriggerStateInputDto PressAndRelease(string sessionId)
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = sessionId,
                Pressed = true,
                Released = true
            };
        }

        static WeaponFireInputDto Snapshot(string sessionId, bool hit = true)
        {
            return new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = Vector3.zero,
                RawAimDirection = Vector3.forward,
                AimDirection = Vector3.forward,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = hit,
                HitPoint = Vector3.forward * 20f,
                HitObjectId = "MovingTargetRange.Target.Face"
            };
        }

        static WeaponFireInputDto ZeroingSnapshot(string sessionId)
        {
            return new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = Vector3.zero,
                RawAimDirection = Vector3.forward,
                AimDirection = Vector3.forward,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = true,
                HitPoint = new Vector3(1f, 1f, ZeroingRules.DistanceMeters),
                HitObjectId = "Target_100m"
            };
        }
    }
}
