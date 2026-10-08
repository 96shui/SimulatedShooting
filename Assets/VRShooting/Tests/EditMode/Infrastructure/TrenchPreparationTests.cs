using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode.Infrastructure
{
    // BDD28: preparation gates, real opening completion and one seeded set of actors.
    public sealed class TrenchPreparationTests
    {
        sealed class Scene : IDroneReconScenePort
        {
            public DroneReconStepCommandDto Current;
            public event Action<DroneReconSceneFactDto> FactReceived;
            public ServiceResult<Unit> Prepare(DroneReconStepCommandDto value) => Ok();
            public ServiceResult<Unit> PlayStep(DroneReconStepCommandDto value) { Current=value; return Ok(); }
            public ServiceResult<Unit> SetCharacterActionsLocked(string id,bool value) => Ok();
            public ServiceResult<Unit> SetSuspended(string id,bool value) => Ok();
            public ServiceResult<Unit> RestorePlayerView(DroneReconStepCommandDto value) { Current=value; return Ok(); }
            public ServiceResult<Unit> StopAndReset(string id,string sequence) => Ok();
            public void Report(DroneReconFactKind kind) => FactReceived?.Invoke(new DroneReconSceneFactDto {
                SessionId=Current.SessionId,SequenceId=Current.SequenceId,StepId=Current.StepId,
                Phase=Current.Phase,Kind=kind,FeedAvailable=true,FeedBindingId=Current.FeedBindingId });
            static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
        }

        [Test]
        public void PreparingRejectsCombatAndCannotBeResumedThroughCoreState()
        {
            var clock=new FakeCombatClock(); var world=new FakeCombatWorld();
            using var trench=new TrenchService(P3Fixtures.TrenchDefinition,clock,new SeededCombatRandom(),world);
            var result=trench.PrepareSession("trench-a","training-rifle",RandomSeed.Fixed(42));
            Assert.That(result.Success,Is.True);
            var id=result.Data.SessionId;
            Assert.That(result.Data.State,Is.EqualTo(SessionState.Preparing));
            Assert.That(trench.Combat.GetSnapshot(id).Data.State,Is.EqualTo(SessionState.Preparing));
            Assert.That(trench.GetHud(id).Data.CanShoot,Is.False);
            Assert.That(trench.Combat.SetGrip(new WeaponGripStateInputDto { SessionId=id,
                HoldState=WeaponHoldState.TwoHandHeld,RearHandTracked=true,FrontHandTracked=true }).ErrorCode,
                Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(trench.Combat.Reload(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(trench.Combat.ApplyGrenadeExplosion(id,"blocked-grenade",id+".teammate-2",
                Vector3.zero,5,Array.Empty<string>()).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(trench.Combat.SetState(id,SessionState.Running).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(trench.BeginCombat(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(trench.Advance(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(trench.Submit(new CombatInputDto { SessionId=id,EventId="blocked-search",
                Tick=clock.Tick,Kind=CombatInputKind.AreaPresence,EntityId="trench-a.node-001",Flag=true }).ErrorCode,
                Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(world.NavigationRequests.Count,Is.Zero);
            Assert.That(trench.GetResult(id).Success,Is.False);
            Assert.That(trench.Cancel(id).Success,Is.True);
            Assert.That(trench.BeginCombat(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
        }

        [Test]
        public void OnlyCompletedOpeningStartsTheSameActorsAndStartIsIdempotent()
        {
            var clock=new FakeCombatClock(); var scene=new Scene();
            using var drone=new DroneReconService(clock,scene);
            using var trench=new TrenchService(P3Fixtures.TrenchDefinition,clock,new SeededCombatRandom(),
                new FakeCombatWorld(),recon:drone);
            var prepared=trench.PrepareSession("trench-a","training-rifle",RandomSeed.Fixed(42)).Data;
            var id=prepared.SessionId;
            var enemies=trench.GetVisualSnapshot(id).Data.Entities.Where(e=>e.Role==CombatEntityRole.Enemy)
                .Select(e=>(e.EntityId,e.Position)).ToArray();
            Assert.That(drone.Begin(id).Success,Is.True);
            Assert.That(trench.BeginCombat(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            scene.Report(DroneReconFactKind.FeedState);scene.Report(DroneReconFactKind.StepCompleted);
            clock.Advance(3);drone.Advance(id);
            for(var step=0;step<4;step++)
            {
                Assert.That(trench.BeginCombat(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
                scene.Report(DroneReconFactKind.StepCompleted);drone.Advance(id);
            }
            Assert.That(trench.BeginCombat(id).Data.State,Is.EqualTo(SessionState.Running));
            Assert.That(trench.Combat.SetGrip(new WeaponGripStateInputDto {SessionId=id,HoldState=WeaponHoldState.TwoHandHeld,
                RearHandTracked=true,FrontHandTracked=true}).ErrorCode,Is.EqualTo(ErrorCode.InvalidState),
                "Core commands stay locked between the Running snapshot and the scene unlock acknowledgement.");
            Assert.That(trench.CompleteCombatStart(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            foreach(var blockedState in new[]{SessionState.Paused,SessionState.Completed,SessionState.Failed})
                Assert.That(trench.Combat.SetState(id,blockedState).ErrorCode,Is.EqualTo(ErrorCode.InvalidState),
                    "The core cannot leave the opening commit through a generic state command.");
            var revision=trench.GetSession(id).Data.Revision;
            Assert.That(trench.BeginCombat(id).Data.Revision,Is.EqualTo(revision));
            Assert.That(trench.GetVisualSnapshot(id).Data.Entities.Where(e=>e.Role==CombatEntityRole.Enemy)
                .Select(e=>(e.EntityId,e.Position)).ToArray(),Is.EqualTo(enemies));
            Assert.That(trench.Combat.GetSnapshot(id).Data.Weapon.CanShoot,Is.False,
                "Opening inputs cannot establish a grip for the first combat shot.");
            Assert.That(drone.ConfirmCombatStarted(id).Success,Is.True);
            Assert.That(trench.CompleteCombatStart(id).Success,Is.True);
            Assert.That(trench.Combat.SetGrip(new WeaponGripStateInputDto {SessionId=id,HoldState=WeaponHoldState.TwoHandHeld,
                RearHandTracked=true,FrontHandTracked=true}).Success,Is.True);
        }
    }
}
