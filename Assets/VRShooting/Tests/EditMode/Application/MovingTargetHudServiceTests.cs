using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// 移动靶 HUD 聚合。追溯 docs/BDD/screens/09-移动靶HUD.feature.md。
    /// </summary>
    [TestFixture]
    public class MovingTargetHudServiceTests
    {
        GameEventBus eventBus;
        TrainingSessionService sessions;
        WeaponControlService weapons;
        MovingTargetService movingTarget;
        MovingTargetHudService hud;

        [SetUp]
        public void SetUp()
        {
            eventBus = new GameEventBus();
            sessions = new TrainingSessionService(eventBus);
            weapons = new WeaponControlService(eventBus);
            movingTarget = new MovingTargetService(eventBus, sessions);
            hud = new MovingTargetHudService(eventBus, sessions, movingTarget, weapons);
        }

        [Test]
        public void Screen09_GetHud_AfterStartShowsCountdownAndTenAmmo()
        {
            var session = StartReadyHud();

            var result = hud.GetHud(session.SessionId);
            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(HudType.MovingTarget, result.Data.HudType);
            Assert.AreEqual("10/10", Find(result.Data, "ammo").Value);
            Assert.AreEqual("两发起射 / 长按连射", Find(result.Data, "fireMode").Value);
            Assert.AreEqual("0/10", Find(result.Data, "hits").Value);
            Assert.AreEqual("4m/s", Find(result.Data, "speed").Value);
            Assert.AreEqual("等待", Find(result.Data, "direction").Value);
            Assert.IsFalse(result.Data.CanShoot);
            Assert.That(result.Data.Prompts[0].Text, Does.Contain("等待开始"));
        }

        [Test]
        public void Screen09_GetHud_DuringRightToLeftShowsCanShootAndDirection()
        {
            var session = StartReadyHud();
            movingTarget.Tick(session.SessionId, 4f);

            var result = hud.GetHud(session.SessionId);
            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual("右→左", Find(result.Data, "direction").Value);
            Assert.AreEqual("可射击", Find(result.Data, "shootState").Value);
            Assert.IsTrue(result.Data.CanShoot);
            Assert.AreEqual(WeaponFireMode.InitialTwoThenAutomatic, result.Data.FireSequence.Value.FireMode);
        }

        [Test]
        public void Screen09_GetHud_LeftHoldShowsForbiddenPrompt()
        {
            var session = StartReadyHud();
            movingTarget.Tick(session.SessionId, 13f);

            var result = hud.GetHud(session.SessionId);
            Assert.IsFalse(result.Data.CanShoot);
            Assert.AreEqual("左端停留", Find(result.Data, "direction").Value);
            Assert.That(result.Data.Prompts[0].Text, Does.Contain("端点停留禁射"));
        }

        [Test]
        public void Screen09_GetHud_HitUpdatesHitCountWithoutReadingUiText()
        {
            var session = StartReadyHud();
            movingTarget.Tick(session.SessionId, 4f);
            movingTarget.RecordShot(session.SessionId, "seq", 1, new WeaponShotResultDto
            {
                SessionId = session.SessionId,
                IsValidShot = true,
                Hit = true,
                ShotSequence = 1
            });

            var result = hud.GetHud(session.SessionId);
            Assert.AreEqual("1/10", Find(result.Data, "hits").Value);
            Assert.AreEqual("10/10", Find(result.Data, "ammo").Value);
        }

        TrainingSessionDto StartReadyHud()
        {
            var started = movingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f },
                RandomSeed.Fixed(7));
            Assert.IsTrue(started.Success, started.Message);
            var weapon = weapons.StartSession(started.Data.SessionId, "training-rifle", TrainingMode.MovingTarget);
            Assert.IsTrue(weapon.Success, weapon.Message);
            Assert.AreEqual(10, weapon.Data.CurrentMagazine);
            return sessions.Current;
        }

        static HudTextLineDto Find(HudDto hud, string key)
        {
            for (var i = 0; i < hud.TextLines.Count; i++)
            {
                if (hud.TextLines[i].Key == key)
                {
                    return hud.TextLines[i];
                }
            }

            Assert.Fail("missing hud line " + key);
            return default;
        }
    }
}
