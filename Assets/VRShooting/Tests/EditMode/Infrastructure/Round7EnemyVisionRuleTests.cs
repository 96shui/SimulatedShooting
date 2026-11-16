using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode
{
    // BDD36: circular discovery, vertical boundary, alignment and configuration propagation.
    public sealed class Round7EnemyVisionRuleTests
    {
        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void Bdd36_BackwardsEnemyDiscoversButOnlyAlignedEnemyFires(TrainingMode mode)
        {
            var clock = new FakeCombatClock();
            using var core = new CombatCoreService(clock);
            core.Start("circle", mode, new[] { new CombatEntityVisualDto { EntityId = "enemy", Role = CombatEntityRole.Enemy, Position = Vector3.forward * 10, Forward = Vector3.forward } });
            void Sight(string id, Vector3 forward) => core.Submit(new CombatInputDto { SessionId = "circle", EventId = id, Tick = clock.Tick,
                Kind = CombatInputKind.Perception, EntityId = "enemy", TargetId = "circle.player", Position = Vector3.forward * 10, Direction = forward, Flag = true });
            Sight("behind", Vector3.forward); core.Advance("circle");
            Assert.That(core.GetSnapshot("circle").Data.Visual.Entities.Single(e => e.EntityId == "enemy").State, Is.EqualTo(CombatEntityState.Attacking));
            clock.Advance(3); core.Advance("circle");
            Assert.That(core.GetSnapshot("circle").Data.Player.Health, Is.EqualTo(10));
            Sight("aligned", Vector3.back); core.Advance("circle");
            clock.Advance(.99); core.Advance("circle");
            Assert.That(core.GetSnapshot("circle").Data.Player.Health, Is.EqualTo(10));
            clock.Advance(.01); core.Advance("circle");
            Assert.That(core.GetSnapshot("circle").Data.Player.Health, Is.EqualTo(9));
        }

        [TestCase(30f, 2.5f, true)] [TestCase(30.01f, 0f, false)] [TestCase(0f, 2.51f, false)]
        public void Bdd36_RangeUsesHorizontalRadiusAndSeparateVerticalTolerance(float horizontal, float vertical, bool visible)
        {
            var clock = new FakeCombatClock();
            using var core = new CombatCoreService(clock);
            var position = new Vector3(horizontal, vertical, 0);
            core.Start("bounds", TrainingMode.Urban, new[] { new CombatEntityVisualDto { EntityId = "enemy", Role = CombatEntityRole.Enemy, Position = position, Forward = Vector3.right } });
            core.Submit(new CombatInputDto { SessionId = "bounds", EventId = "sight", Tick = clock.Tick, Kind = CombatInputKind.Perception,
                EntityId = "enemy", TargetId = "bounds.player", Position = position, Direction = Vector3.right, Flag = true });
            core.Advance("bounds");
            Assert.That(core.GetSnapshot("bounds").Data.Visual.Entities.Single(e => e.EntityId == "enemy").State,
                Is.EqualTo(visible ? CombatEntityState.Attacking : CombatEntityState.Idle));
        }

        [Test] public void Bdd36_InjectedVisionSurvivesHealthCopyAndReachesCore()
        {
            var custom = CombatConfigDto.Default.WithEnemyVision(22, 1.8f, 1.7f, 1.1f, .2f, 90).WithPlayerHealth(8);
            Assert.That(custom.Validate().Success, Is.True);
            using var core = new CombatCoreService(new FakeCombatClock(), custom);
            core.Start("custom", TrainingMode.Trench, new CombatEntityVisualDto[0]);
            Assert.That(core.Config.EnemyRange, Is.EqualTo(22));
            Assert.That(core.Config.EnemyVerticalTolerance, Is.EqualTo(1.8f));
            Assert.That(core.Config.EnemyHeadSampleHeight, Is.EqualTo(1.7f));
            Assert.That(core.Config.EnemyChestSampleHeight, Is.EqualTo(1.1f));
            Assert.That(core.Config.EnemyFootSampleHeight, Is.EqualTo(.2f));
            Assert.That(core.Config.EnemyTurnSpeedDegrees, Is.EqualTo(90));
            Assert.That(core.GetSnapshot("custom").Data.Player.Health, Is.EqualTo(8));
        }

        [TestCase(0, 2.5f, 1.65f, 1.2f, .15f, 180)]
        [TestCase(30, -1, 1.65f, 1.2f, .15f, 180)]
        [TestCase(30, 2.5f, float.NaN, 1.2f, .15f, 180)]
        [TestCase(30, 2.5f, 1.65f, 1.65f, .15f, 180)]
        [TestCase(30, 2.5f, 1.65f, 1.2f, 0, 180)]
        [TestCase(30, 2.5f, 1.65f, 1.2f, .15f, float.PositiveInfinity)]
        public void Bdd36_InvalidVisionConfigurationIsRejected(float range, float tolerance, float head, float chest, float foot, float speed)
            => Assert.That(CombatConfigDto.Default.WithEnemyVision(range, tolerance, head, chest, foot, speed).Validate().ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
    }
}
