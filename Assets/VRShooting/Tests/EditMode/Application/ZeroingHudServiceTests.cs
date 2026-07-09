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
    /// task007 100m HUD 与结算 DTO 聚合测试。追溯 docs/BDD/screens/05-100m射击HUD.feature.md。
    /// </summary>
    [TestFixture]
    public class ZeroingHudServiceTests
    {
        GameEventBus eventBus;
        TrainingSessionService trainingSessions;
        WeaponControlService weaponControl;
        ZeroingService zeroing;
        ZeroingHudService hud;

        [SetUp]
        public void SetUp()
        {
            eventBus = new GameEventBus();
            trainingSessions = new TrainingSessionService(eventBus);
            weaponControl = new WeaponControlService(eventBus);
            zeroing = new ZeroingService(eventBus, trainingSessions, weaponControl);
            hud = new ZeroingHudService(eventBus, trainingSessions, zeroing, weaponControl, weaponControl);
        }

        [Test]
        public void GetHud_AtSessionStart_ShowsInitialZeroingFields()
        {
            var session = StartZeroingSession();

            var result = hud.GetHud(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(session.SessionId, result.Data.SessionId);
            Assert.AreEqual(TrainingMode.Zeroing100m, result.Data.Mode);
            Assert.AreEqual(HudType.Zeroing, result.Data.HudType);
            Assert.AreEqual("1/3", FindLine(result.Data, "round").Value);
            Assert.AreEqual("100m", FindLine(result.Data, "distance").Value);
            Assert.AreEqual("3/3", FindLine(result.Data, "ammo").Value);
            Assert.AreEqual("待记录 3 发", FindLine(result.Data, "impactRecord").Value);
            Assert.AreEqual("右肩", FindLine(result.Data, "shoulder").Value);
            Assert.IsTrue(result.Data.CanShoot);
            Assert.That(result.Data.Prompts[0].Text, Does.Contain("稳定据枪"));
        }

        [Test]
        public void WeaponFire_RefreshesAmmoAndImpactRecord()
        {
            var session = StartZeroingSession();
            HudDto lastHud = default;
            hud.HudUpdated += dto => lastHud = dto;

            var fire = FireTrainingShot(session.SessionId, true);
            Assert.IsTrue(fire.Success, fire.Message);

            var result = hud.GetHud(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual("2/3", FindLine(result.Data, "ammo").Value);
            Assert.That(FindLine(result.Data, "impactRecord").Value, Does.Contain("已记录 1/3"));
            Assert.That(FindLine(result.Data, "impactRecord").Value, Does.Contain("#01"));
            Assert.IsFalse(string.IsNullOrEmpty(lastHud.SessionId));
            Assert.AreEqual("2/3", FindLine(lastHud, "ammo").Value);
        }

        [Test]
        public void ZeroingShotRecorded_RefreshesImpactRecordWithoutWeaponAmmo()
        {
            var session = StartZeroingSession();

            RecordImpact(session.SessionId, Vector2.zero);

            var result = hud.GetHud(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual("3/3", FindLine(result.Data, "ammo").Value);
            Assert.That(FindLine(result.Data, "impactRecord").Value, Does.Contain("已记录 1/3"));
        }

        [Test]
        public void ReloadStarted_RefreshesPrompt()
        {
            var session = StartZeroingSession();
            var consume = weaponControl.ConsumeAmmo(session.SessionId, 2);
            Assert.IsTrue(consume.Success, consume.Message);

            var reload = weaponControl.StartReload(session.SessionId);
            Assert.IsTrue(reload.Success, reload.Message);

            var result = hud.GetHud(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.That(result.Data.Prompts[0].Text, Does.Contain("换弹中"));
            Assert.IsFalse(result.Data.Prompts[0].IsEnabled);
        }

        [Test]
        public void ToggleShoulder_RefreshesShoulderField()
        {
            var session = StartZeroingSession();

            var toggle = weaponControl.ToggleShoulder(session.SessionId);
            Assert.IsTrue(toggle.Success, toggle.Message);

            var result = hud.GetHud(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual("左肩", FindLine(result.Data, "shoulder").Value);
        }

        [Test]
        public void GetFinalResult_GradeComesFromZeroingService_NotHudText()
        {
            var session = StartZeroingSession();
            CompleteRoundWithImpacts(session.SessionId, new Vector2(1f, 1f));
            zeroing.ApplyAdjustment(session.SessionId, 1);

            var hudResult = hud.GetHud(session.SessionId);
            var finalResult = zeroing.GetFinalResult(session.SessionId);

            Assert.IsTrue(hudResult.Success, hudResult.Message);
            Assert.IsTrue(finalResult.Success, finalResult.Message);
            Assert.AreEqual(ResultGrade.Excellent, finalResult.Data.Grade);
            Assert.That(FindLine(hudResult.Data, "round").Value, Does.Not.Contain("优秀"));
            Assert.That(FindLine(hudResult.Data, "impactRecord").Value, Does.Not.Contain("优秀"));
        }

        [Test]
        public void CompleteRound_ProvidesAnalysisDtoQueryEntry()
        {
            var session = StartZeroingSession();
            RecordThreeImpacts(session.SessionId, new Vector2(-8f, 12f));

            var analysis = zeroing.CompleteRound(session.SessionId);

            Assert.IsTrue(analysis.Success, analysis.Message);
            Assert.AreEqual(3, analysis.Data.Shots.Count);
            Assert.AreEqual(VerticalAdjustmentDirection.CounterClockwise, analysis.Data.VerticalDirection);
            Assert.AreEqual(HorizontalAdjustmentDirection.Forward, analysis.Data.HorizontalDirection);
        }

        [Test]
        public void HudUpdatedEvent_PublishedWhenAmmoChanges()
        {
            var session = StartZeroingSession();
            var received = false;

            eventBus.Subscribe<HudUpdatedEvent>(_ => received = true);
            var fire = FireTrainingShot(session.SessionId, true);
            Assert.IsTrue(fire.Success, fire.Message);

            Assert.IsTrue(received);
        }

        [Test]
        public void RoundComplete_SetsCanShootFalse()
        {
            var session = StartZeroingSession();
            RecordThreeImpacts(session.SessionId, Vector2.zero);

            var result = hud.GetHud(session.SessionId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsFalse(result.Data.CanShoot);
            Assert.That(result.Data.Prompts[0].Text, Does.Contain("本轮射击已完成"));
        }

        ZeroingSessionDto StartZeroingSession()
        {
            if (trainingSessions.HasActiveSession)
            {
                trainingSessions.End(trainingSessions.Current.SessionId, SessionEndReason.Completed);
            }

            var start = zeroing.StartSession(RandomSeed.Fixed(100), WeaponControlService.TrainingRifleId);
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

        ServiceResult<WeaponShotResultDto> FireTrainingShot(string sessionId, bool hit)
        {
            return weaponControl.Fire(new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = Vector3.zero,
                AimDirection = Vector3.forward,
                WeaponPosition = Vector3.zero,
                Stability01 = 0.95f,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = hit,
                HitPoint = new Vector3(0f, 0f, ZeroingRules.DistanceMeters),
                HitObjectId = "Target_100m"
            });
        }

        static HudTextLineDto FindLine(HudDto hud, string key)
        {
            for (var i = 0; i < hud.TextLines.Count; i++)
            {
                if (hud.TextLines[i].Key == key)
                {
                    return hud.TextLines[i];
                }
            }

            return default;
        }
    }
}
