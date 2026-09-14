using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode
{
    // BDD24 all scenarios; BDD12/14 preview, BDD15 HUD/death, BDD17 results/retry.
    public sealed class Screen24_TrenchTests
    {
        FakeCombatClock clock;
        FakeCombatWorld navigation;
        TrenchService service;
        string id;
        int events;
        [SetUp] public void Setup()
        {
            clock = new FakeCombatClock(); navigation = new FakeCombatWorld(); events = 0;
            service = new TrenchService(P3Fixtures.TrenchDefinition, clock, new SeededCombatRandom(), navigation);
        }
        [TearDown] public void Cleanup() => service.Dispose();
        void Start(int seed = 3)
        {
            var started = service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(seed));
            Assert.That(started.Success, Is.True, started.Message); id = started.Data.SessionId;
        }
        CombatEntityVisualDto[] Enemies => service.GetVisualSnapshot(id).Data.Entities.Where(e => e.Role == CombatEntityRole.Enemy).ToArray();
        CombatShotDto Shoot()
        {
            service.Combat.SetGrip(new WeaponGripStateInputDto { SessionId = id, HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true, FrontHandTracked = true, Stability01 = 1 });
            var shot = service.Combat.Fire(new WeaponFireInputDto { SessionId = id, AimDirection = Vector3.forward });
            Assert.That(shot.Success); return shot.Data;
        }
        void Hit(string target, CombatShotDto shot)
        {
            Assert.That(service.Submit(new CombatInputDto { SessionId=id, Tick=clock.Tick, EventId="hit-" + ++events,
                Kind=CombatInputKind.Hit, EntityId=id+".player", TargetId=target, ShotId=shot.ShotId, Flag=true, Value=1 }).Success);
        }
        void Search()
        {
            Assert.That(service.Submit(new CombatInputDto { SessionId=id, Tick=clock.Tick, EventId="area-" + ++events,
                Kind=CombatInputKind.AreaPresence, EntityId="trench-a.node-001", Flag=true }).Success);
        }
        [Test] public void PreviewDoesNotCreateActorsOrConsumeRandom()
        {
            Assert.That(service.GetMaps().Data.Count,Is.EqualTo(1));
            Assert.That(service.GetBriefing("trench-a","training-rifle",RandomSeed.Fixed(3)).Success);
            Assert.That(service.GetSession("any").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(navigation.NavigationRequests.Count,Is.Zero);
            Start(); Assert.That(Enemies.Length,Is.InRange(3,5));
            Assert.That(service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(3)).ErrorCode,Is.EqualTo(ErrorCode.Busy));
        }
        [Test] public void FixedSeedAndSortedCandidatesProduceSamePositionsAcrossRetries()
        {
            Start(42); var positions = Enemies.Select(e=>e.Position).ToArray();
            service.Cancel(id); Start(42); Assert.That(Enemies.Select(e=>e.Position),Is.EqualTo(positions));
            service.Dispose(); service = new TrenchService(P3Fixtures.TrenchDefinition.WithSpawnPoints(P3Fixtures.TrenchDefinition.SpawnPoints.Reverse().ToArray()),clock,new SeededCombatRandom(),navigation);
            Start(42); Assert.That(Enemies.Select(e=>e.Position),Is.EqualTo(positions));
        }
        [TestCase(3)] [TestCase(5)] public void EnemyCountBoundariesCanBeInjected(int count)
        {
            service.Dispose(); service = new TrenchService(P3Fixtures.TrenchDefinition,clock,new CountRandom(count),navigation);
            Start(); Assert.That(Enemies.Length,Is.EqualTo(count)); Assert.That(Enemies.Select(e=>e.Position).Distinct().Count(),Is.EqualTo(count));
        }
        [Test] public void MapWeaponAndSceneErrorsDoNotCreateSession()
        {
            Assert.That(service.SelectMap("trench-b").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.StartSession("trench-a","",RandomSeed.Fixed(1)).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(service.GetBriefing("trench-a","unknown",RandomSeed.Fixed(1)).ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            service.Dispose(); service = new TrenchService(P3Fixtures.TrenchDefinition.WithSpawnPoints(Array.Empty<SceneSpawnPointDto>()),clock,new SeededCombatRandom(),navigation);
            Assert.That(service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(1)).ErrorCode,Is.EqualTo(ErrorCode.ResourceUnavailable));
        }
        [Test] public void SearchOnlyAndKillOnlyDoNotWin_ThenCombinedWinsOnce()
        {
            Start(); Search(); service.Advance(id); Assert.That(service.CompleteIfReady(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            service.Cancel(id); Start(); int results=0; service.ResultReady += _=>results++;
            foreach(var enemy in Enemies) { Hit(enemy.EntityId,Shoot()); Assert.That(service.Advance(id).Success); }
            Assert.That(service.CompleteIfReady(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Search(); service.Advance(id); var result=service.GetResult(id).Data;
            Assert.That(result.Victory); Assert.That(result.EnemyKilled,Is.EqualTo(result.EnemyTotal));
            Assert.That(result.RemainingAmmo,Is.EqualTo(150-result.EnemyTotal));
            Assert.That(service.CompleteIfReady(id).Data.Equals(result)); Assert.That(results,Is.EqualTo(1));
        }
        [Test] public void ValidationAndIdempotenceCannotFakeKillsOrSearch()
        {
            Start(); Assert.That(service.MarkSearchNode(id,"missing").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.MarkSearchNode(id,"trench-a.node-001").ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(service.RegisterEnemyKilled(id,Enemies[0].EntityId).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(service.FailByPlayerDeath(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Search(); Assert.That(service.CompleteIfReady(id).ErrorCode,Is.EqualTo(ErrorCode.Busy)); service.Advance(id);
            var revision=service.GetSession(id).Data.Revision;
            Assert.That(service.MarkSearchNode(id,"trench-a.node-001").Success); Assert.That(service.GetSession(id).Data.Revision,Is.EqualTo(revision));
            var enemy=Enemies[0]; Hit(enemy.EntityId,Shoot()); service.Advance(id); revision=service.GetSession(id).Data.Revision;
            Assert.That(service.RegisterEnemyKilled(id,enemy.EntityId).Success); Assert.That(service.GetSession(id).Data.Revision,Is.EqualTo(revision));
        }
        [Test] public void SameBatchDeathWinsAndRetainsFinalSearchAndKill()
        {
            service.Dispose(); service=new TrenchService(P3Fixtures.TrenchDefinition,clock,new SeededCombatRandom(),navigation,CombatConfigDto.Default.WithPlayerHealth(5));
            Start(); var enemies=Enemies;
            foreach(var enemy in enemies.Skip(1)) { Hit(enemy.EntityId,Shoot()); service.Advance(id); }
            var final=enemies[0]; var shot=Shoot();
            service.Submit(new CombatInputDto { SessionId=id,Tick=clock.Tick,EventId="sight",Kind=CombatInputKind.Perception,
                EntityId=final.EntityId,TargetId=id+".player",Position=Vector3.forward*10,Direction=Vector3.back,Flag=true }); service.Advance(id);
            clock.Advance(1); Hit(final.EntityId,shot); Search(); service.Advance(id);
            var result=service.GetResult(id).Data;
            Assert.That(result.Victory,Is.False); Assert.That(result.EnemyKilled,Is.EqualTo(result.EnemyTotal)); Assert.That(result.SearchProgress01,Is.EqualTo(1));
            Assert.That(service.FailByPlayerDeath(id).Data.Equals(result));
        }
        [Test] public void HudDoesNotLeakOffsetsAndEventsShareCurrentSnapshots()
        {
            Start(); int notifications=0;
            service.SessionChanged += s => { Assert.That(service.GetSession(id).Data.Revision,Is.EqualTo(s.Revision)); Assert.That(service.GetHud(id).Data.Ammo.CurrentMagazine,Is.EqualTo(s.Ammo.CurrentMagazine)); notifications++; };
            Shoot(); Assert.That(notifications,Is.GreaterThan(0));
            foreach(var marker in service.GetHud(id).Data.MiniMap.Markers.Where(m=>m.Type==MarkerType.EnemyEstimate))
                Assert.That(P3Fixtures.TrenchDefinition.SpawnPoints.Any(p => Vector2.Distance(marker.NormalizedPosition,new Vector2((p.EstimatePosition.x+10)/30,(p.EstimatePosition.z+10)/30))<.001f));
            Assert.That(service.GetSquadStatus(id).Data.Squad.Members.Count,Is.EqualTo(3));
        }
        [Test] public void CancelRetryAndPauseKeepOldResultsAndClockIsolated()
        {
            Start(); clock.Advance(2); service.Advance(id); service.Combat.SetState(id,SessionState.Paused); clock.Advance(100); service.Advance(id);
            service.Combat.SetState(id,SessionState.Running); Search(); service.Advance(id);
            foreach(var enemy in Enemies) { Hit(enemy.EntityId,Shoot()); service.Advance(id); }
            var old=service.GetResult(id).Data; Assert.That(old.ElapsedSeconds,Is.EqualTo(2).Within(.001)); var oldId=id;
            Assert.That(service.Cancel(id).Success); Assert.That(service.Cancel(id).Success); Start();
            Assert.That(id,Is.Not.EqualTo(oldId)); Assert.That(service.GetResult(oldId).ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.MarkSearchNode(oldId,"trench-a.node-001").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.GetSession(id).Data.EnemyKilled,Is.Zero); Assert.That(service.GetSession(id).Data.SearchProgress01,Is.Zero);
            Assert.That(old.SearchProgress01,Is.EqualTo(1));
        }
        [Test] public void FormationFollowsPolyline_OnlyAcknowledgementMovesTeammates()
        {
            using var squad=new SquadFormationService(clock,navigation);
            squad.Start("squad",Vector3.zero,Vector3.forward);
            squad.UpdatePlayer(Vector3.forward*2,Vector3.forward,true,100);
            var first=navigation.NavigationRequests.ToArray();
            foreach(var request in first) Assert.That(squad.Submit(Ack(request,true)).Success);
            squad.UpdatePlayer(new Vector3(2,0,2),Vector3.right,true,100);
            var next=navigation.NavigationRequests.Skip(first.Length).ToArray(); Assert.That(next.Length,Is.EqualTo(2));
            Assert.That(next[0].Destination,Is.EqualTo(new Vector3(.5f,0,2))); Assert.That(next[1].Destination,Is.EqualTo(new Vector3(0,0,1)));
            Assert.That(next[0].Forward,Is.EqualTo(Vector3.right)); Assert.That(next[1].Forward,Is.EqualTo(Vector3.back));
            Assert.That(squad.GetSquadStatus("squad").Data.Members[1].WorldPosition,Is.EqualTo(first[0].Destination));
            Assert.That(squad.GetAvailableCommands("squad").Data,Is.Empty);
            Assert.That(squad.Issue(new SquadCommandRequest {SessionId="squad"}).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(squad.OnReloadStarted("squad").ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
        }
        [Test] public void NavigationFailureWaitsOneSecond_AndPauseRejectsLateArrival()
        {
            using var squad=new SquadFormationService(clock,navigation); squad.Start("squad",Vector3.zero,Vector3.forward);
            navigation.NextError=ErrorCode.ResourceUnavailable; int failures=0; squad.NavigationFailed += _=>failures++;
            squad.UpdatePlayer(Vector3.forward*2,Vector3.forward,true,100); int count=navigation.NavigationRequests.Count;
            Assert.That(failures,Is.EqualTo(1)); Assert.That(squad.GetSquadStatus("squad").Data.Members[1].WorldPosition,Is.EqualTo(Vector3.back*1.5f));
            clock.Advance(.99); squad.UpdatePlayer(Vector3.forward*2,Vector3.forward,true,100); Assert.That(navigation.NavigationRequests.Count,Is.EqualTo(count));
            clock.Advance(.01); squad.UpdatePlayer(Vector3.forward*2,Vector3.forward,true,100); Assert.That(navigation.NavigationRequests.Count,Is.GreaterThan(count));
            var late=navigation.NavigationRequests.Last(); squad.UpdatePlayer(Vector3.forward*2,Vector3.forward,false,100);
            Assert.That(squad.Submit(Ack(late,true)).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
        }
        CombatInputDto Ack(CombatNavigationRequestDto r,bool success)=>new CombatInputDto {SessionId=r.SessionId,Tick=clock.Tick,
            EventId="ack-"+ ++events,Kind=CombatInputKind.NavigationResult,EntityId=r.EntityId,TargetId=r.RequestId,Flag=success,Position=r.Destination,Direction=r.Forward};
        [Test] public void CrossDomainEventIdCollisionAndUnknownNavigationAreRejected()
        {
            Start();var fact=new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="shared",Kind=CombatInputKind.PlayerPose,EntityId=id+".player",Direction=Vector3.forward};
            Assert.That(service.Submit(fact).Success);
            Assert.That(service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="shared",Kind=CombatInputKind.AreaPresence,EntityId="trench-a.node-001",Flag=true}).ErrorCode,Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="bad-arrival",Kind=CombatInputKind.NavigationResult,
                EntityId=id+".teammate-2",TargetId="nonexistent",Direction=Vector3.forward,Flag=true}).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            service.Advance(id);Assert.That(service.GetSession(id).Data.SearchProgress01,Is.Zero);
        }
        [Test] public void FailedNavigationCallbackCanDisposeWithoutContinuingToMove()
        {
            using var squad=new SquadFormationService(clock,navigation);squad.Start("squad",Vector3.zero,Vector3.forward);
            navigation.NextError=ErrorCode.ResourceUnavailable;squad.NavigationFailed+=_=>squad.Dispose();
            Assert.DoesNotThrow(()=>squad.UpdatePlayer(Vector3.forward*2,Vector3.forward,true,100));
            Assert.That(navigation.NavigationRequests.Count,Is.EqualTo(1));
        }
        sealed class CountRandom:ICombatRandom
        {
            readonly int count; bool first=true; public CountRandom(int count){this.count=count;}
            public void Reset(RandomSeed seed){first=true;}
            public int NextInt(int minInclusive,int maxExclusive){if(first){first=false;return count;} return minInclusive;}
        }
    }
}
