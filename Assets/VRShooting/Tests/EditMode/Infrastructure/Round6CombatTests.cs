using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode
{
    // BDD35 / P3 task004/010/015/020.
    public sealed class Round6CombatTests
    {
        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void Bdd35_P3StartsWithFourSpares(TrainingMode mode)
        {
            var config=CombatConfigDto.ForMode(mode);
            Assert.That(config.Validate().Success,Is.True);
            Assert.That(config.InitialAmmo.MagazineCapacity,Is.EqualTo(30));
            Assert.That(config.InitialAmmo.CurrentMagazine,Is.EqualTo(30));
            Assert.That(config.InitialAmmo.ReserveAmmo,Is.EqualTo(120));
            Assert.That(config.InitialAmmo.ReserveMagazines,Is.EqualTo(4));
        }

        [TestCase(false)] [TestCase(true)]
        public void Bdd35_NoEnemyGrenadeUsesPlayerForwardOrThrowerFallback(bool fallback)
        {
            var clock=new FakeCombatClock();
            using var core=new CombatCoreService(clock);
            Assert.That(core.Start("round6",TrainingMode.Trench,Array.Empty<CombatEntityVisualDto>()).Success);
            var player=new Vector3(6,2,3);
            Assert.That(core.Submit(new CombatInputDto{SessionId="round6",EventId="pose",Tick=clock.Tick,
                Kind=CombatInputKind.PlayerPose,EntityId="round6.player",Position=player,Direction=fallback?Vector3.up:new Vector3(1,.3f,0)}).Success);
            core.Advance("round6");
            using var squad=new SquadFormationService(clock,new FakeCombatWorld());
            squad.Start("round6",player,Vector3.left);
            using var tactic=new SquadGrenadeTacticService(clock,core,squad);
            tactic.Configure(new EmptyWorld()); tactic.Start("round6");
            tactic.RequestFirstTeammateThrow(); Assert.That(tactic.Advance().Success);
            Assert.That(tactic.Current.HasValue,Is.True,"An empty enemy snapshot must still produce a throw");
            var plan=tactic.Current.Value;
            var expected=player+(fallback?Vector3.left:Vector3.right)*CombatConfigDto.Default.GrenadeDetectionRange+Vector3.up*.08f;
            Assert.That(Vector3.Distance(plan.Target,expected),Is.LessThan(.0001f));
            Assert.That(plan.ThrowerId,Is.EqualTo("round6.teammate-2"));
            clock.Advance(4); Assert.That(tactic.Advance().Success);
            Assert.That(tactic.Current.HasValue,Is.False);
        }
        sealed class EmptyWorld:ICombatGrenadeWorld
        {
            public bool TryGetThrowPose(string id,out Vector3 feet,out Vector3 release){feet=new Vector3(2,2,3);release=feet+Vector3.up;return true;}
            public bool Sweep(Vector3 from,Vector3 to,out Vector3 hit){hit=to;return false;}
            public bool IsExposed(Vector3 blast,Vector3 enemy)=>true;
        }
    }
}
