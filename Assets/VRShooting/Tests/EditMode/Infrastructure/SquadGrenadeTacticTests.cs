using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application.Combat;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode
{
    public sealed class SquadGrenadeTacticTests
    {
        FakeCombatClock clock;
        CombatCoreService core;
        SquadFormationService squad;
        SquadGrenadeTacticService tactic;
        const string Session = "grenade-session";

        [SetUp] public void Setup()
        {
            clock = new FakeCombatClock();
            core = new CombatCoreService(clock);
            core.Start(Session, TrainingMode.Trench, new[]
            {
                Enemy("e1", new Vector3(3, 0, 3)), Enemy("e2", new Vector3(4, 0, 3)), Enemy("e3", new Vector3(20, 0, 20))
            });
            squad = new SquadFormationService(clock, new FakeCombatWorld()); squad.Start(Session, Vector3.zero, Vector3.forward);
            tactic = new SquadGrenadeTacticService(clock, core, squad);
            tactic.Configure(new GrenadeWorld());
            tactic.Start(Session);
        }
        [TearDown] public void Cleanup() { tactic.Dispose(); squad.Dispose(); core.Dispose(); }

        [Test] public void TwoNearbyEnemiesScheduleThrowAndFuseKillsOnlyBlastTargets()
        {
            GrenadeThrowPlanDto thrown = default; GrenadeExplosionDto exploded = default;
            tactic.GrenadeThrown += value => thrown = value; tactic.GrenadeExploded += value => exploded = value;
            tactic.RequestFirstTeammateThrow();
            Assert.That(tactic.Advance().Success);
            Assert.That(thrown.GrenadeId, Is.EqualTo(Session + ".grenade-001"));
            Assert.That(squad.GetSquadStatus(Session).Data.Members.Any(m => m.State == SquadMemberState.ThrowingGrenade));
            clock.Advance(4); Assert.That(tactic.Advance().Success);
            var snapshot = core.GetSnapshot(Session).Data;
            Assert.That(snapshot.Visual.Entities.Where(e => e.Role == CombatEntityRole.Enemy && e.State == CombatEntityState.Dead).Select(e => e.EntityId),
                Is.EquivalentTo(new[] { "e1", "e2" }));
            Assert.That(exploded.Targets, Is.EquivalentTo(new[] { "e1", "e2" }));
            Assert.That(squad.GetSquadStatus(Session).Data.Members.All(m => m.State != SquadMemberState.ThrowingGrenade));
        }

        [Test] public void SingleDistantEnemyThrowsTowardMaximumRange()
        {
            tactic.Dispose(); core.Dispose(); squad.Dispose();
            core = new CombatCoreService(clock); core.Start(Session, TrainingMode.Trench, new[] { Enemy("only", new Vector3(40, 0, 0)) });
            squad = new SquadFormationService(clock, new FakeCombatWorld()); squad.Start(Session, Vector3.zero, Vector3.forward);
            tactic = new SquadGrenadeTacticService(clock, core, squad); tactic.Configure(new GrenadeWorld()); tactic.Start(Session);
            GrenadeThrowPlanDto thrown=default; tactic.GrenadeThrown += plan => thrown=plan;
            tactic.RequestFirstTeammateThrow();
            Assert.That(tactic.Advance().Success);
            Assert.That(thrown.ThrowerId, Is.EqualTo(Session+".teammate-2"));
            Assert.That(thrown.Target.x, Is.EqualTo(18f).Within(.01f));
        }

        [Test] public void NextPressIsAcceptedThreeSecondsAfterPreviousThrow()
        {
            tactic.RequestFirstTeammateThrow(); tactic.Advance();
            clock.Advance(2.5); tactic.Advance();
            tactic.RequestFirstTeammateThrow(); tactic.Advance();
            Assert.That(tactic.Current.HasValue, Is.False);
            clock.Advance(.6);
            tactic.RequestFirstTeammateThrow(); tactic.Advance();
            Assert.That(tactic.Current.HasValue, Is.True);
            Assert.That(tactic.Current.Value.GrenadeId, Is.EqualTo(Session+".grenade-002"));
        }

        [Test] public void ExplosionIsIdempotentAndDoesNotResurrectCorpse()
        {
            tactic.RequestFirstTeammateThrow(); tactic.Advance(); clock.Advance(4); tactic.Advance();
            var before = core.GetSnapshot(Session).Data.Revision;
            var repeat = core.ApplyGrenadeExplosion(Session, "grenade-session.grenade-001", Session+".teammate-2", new Vector3(3.5f, 0, 3), 5, new[]{"e1","e2"});
            Assert.That(repeat.Success); Assert.That(repeat.Data, Is.Empty);
            Assert.That(core.GetSnapshot(Session).Data.Visual.Entities.Count(e => e.State == CombatEntityState.Dead), Is.EqualTo(2));
            Assert.That(core.GetSnapshot(Session).Data.Revision, Is.EqualTo(before));
        }

        sealed class GrenadeWorld : ICombatGrenadeWorld
        {
            public bool TryGetThrowPose(string memberId, out Vector3 feet, out Vector3 release)
            { feet=new Vector3(0,0,0); release=new Vector3(0,1,0); return memberId.EndsWith("teammate-2")||memberId.EndsWith("teammate-3"); }
            public bool Sweep(Vector3 from,Vector3 to,out Vector3 impact){impact=to;return false;}
            public bool IsExposed(Vector3 blast,Vector3 enemyFeet)=>true;
        }

        static CombatEntityVisualDto Enemy(string id, Vector3 position) => new CombatEntityVisualDto
        { EntityId = id, Role = CombatEntityRole.Enemy, Position = position, Forward = Vector3.back };
    }
}
