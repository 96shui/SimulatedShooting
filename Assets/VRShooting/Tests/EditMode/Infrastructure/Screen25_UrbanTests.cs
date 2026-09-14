using System;
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
    // BDD18/19/20/21/25: urban phases, door/check conditions, results and retry.
    public sealed class Screen25_UrbanTests
    {
        FakeCombatClock clock; UrbanService service; string id; int sequence;
        UrbanSessionDto State=>service.GetSession(id).Data;
        [SetUp] public void Setup(){clock=new FakeCombatClock();sequence=0;service=new UrbanService(P3Fixtures.UrbanDefinition,clock,new SeededCombatRandom(),new FakeCombatWorld());}
        [TearDown] public void Cleanup()=>service.Dispose();
        void Start(int seed=4){var r=service.StartSession("urban-a","training-rifle",RandomSeed.Fixed(seed));Assert.That(r.Success,Is.True,r.Message);id=r.Data.SessionId;}
        void Range(string key){Assert.That(service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="range-"+ ++sequence,Kind=CombatInputKind.AreaPresence,EntityId=key,Flag=true}).Success);service.Advance(id);}
        void Enter(){Range("urban-a.entrance");Assert.That(service.EnterBuilding(id,"urban-a.entrance").Success);}
        string[] Rooms=>P3Fixtures.UrbanDefinition.Floors.SelectMany(f=>f.Rooms).Select(r=>r.RoomId).ToArray();
        CombatShotDto Shot(){service.Combat.SetGrip(new WeaponGripStateInputDto {SessionId=id,HoldState=WeaponHoldState.TwoHandHeld,RearHandTracked=true,FrontHandTracked=true});return service.Combat.Fire(new WeaponFireInputDto {SessionId=id,AimDirection=Vector3.forward}).Data;}
        void Hit(string enemy,CombatShotDto shot){Assert.That(service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="hit-"+ ++sequence,Kind=CombatInputKind.Hit,EntityId=id+".player",TargetId=enemy,ShotId=shot.ShotId,Flag=true,Value=1}).Success);}
        void Kill(EncounterGroup? group=null){foreach(var enemy in service.GetEnemyAssignments(id).Data.Where(e=>!group.HasValue||e.Group==group)) {Hit(enemy.EntityId,Shot());service.Advance(id);}}
        void Check(string room){Range(room+".door");Assert.That(service.OpenRoomDoor(id,room).Success);Range(room+".check");Assert.That(service.MarkRoomSearched(id,room).Success);service.Advance(id);}
        [Test] public void SeedAndOwnershipAreStableAndCountsWithinBounds()
        {
            Start();Assert.That(State.StreetEnemyTotal,Is.InRange(1,2));Assert.That(State.BuildingEnemyTotal,Is.InRange(3,6));
            var positions=service.GetVisualSnapshot(id).Data.Entities.Where(e=>e.Role==CombatEntityRole.Enemy).Select(e=>e.Position).ToArray();
            var assignments=service.GetEnemyAssignments(id).Data.Select(e=>e.SpawnPointId).ToArray();
            service.Cancel(id);Start();Assert.That(service.GetEnemyAssignments(id).Data.Select(e=>e.SpawnPointId),Is.EqualTo(assignments));
            Assert.That(service.GetVisualSnapshot(id).Data.Entities.Where(e=>e.Role==CombatEntityRole.Enemy).Select(e=>e.Position),Is.EqualTo(positions));
        }
        [TestCase(false)] [TestCase(true)] public void RandomCountEndpoints(bool maximum)
        {
            service.Dispose();service=new UrbanService(P3Fixtures.UrbanDefinition,clock,new EndpointRandom(maximum),new FakeCombatWorld());Start();
            Assert.That(State.StreetEnemyTotal,Is.EqualTo(maximum?2:1));Assert.That(State.BuildingEnemyTotal,Is.EqualTo(maximum?6:3));
        }
        [Test] public void MapAndConfigurationAndUnknownIdsFailWithoutSideEffects()
        {
            Assert.That(service.SelectMap("urban-b").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.StartSession("urban-a","",RandomSeed.Fixed(1)).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Start();Assert.That(service.EnterBuilding(id,"bad").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.OpenRoomDoor(id,"bad").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.RegisterEnemyKilled(id,"bad").ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.ObserveRoom(id,Rooms[0]).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            service.Dispose();var d=P3Fixtures.UrbanDefinition;
            service=new UrbanService(new CombatSceneDefinitionDto {MapId=d.MapId,SceneId=d.SceneId,Mode=d.Mode,EntranceId=d.EntranceId,Projections=d.Projections,SpawnPoints=d.SpawnPoints,Floors=d.Floors.Take(2).ToArray()},clock,new SeededCombatRandom(),new FakeCombatWorld());
            Assert.That(service.StartSession("urban-a","training-rifle",RandomSeed.Fixed(1)).ErrorCode,Is.EqualTo(ErrorCode.InvalidInput));
        }
        [Test] public void RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies()
        {
            Start();Assert.That(service.EnterBuilding(id,"urban-a.entrance").ErrorCode,Is.EqualTo(ErrorCode.InvalidState));Shot();var original=id;Enter();
            Assert.That(State.SessionId,Is.EqualTo(original));Assert.That(State.Phase,Is.EqualTo(UrbanPhase.Building));
            Assert.That(service.GetHud(id).Data.Prompts.Any(p=>p.PromptId=="StreetRisk"));
            Assert.That(service.ExitBuilding(id,"urban-a.entrance").Success);Assert.That(State.Phase,Is.EqualTo(UrbanPhase.Street));Assert.That(State.Ammo.CurrentMagazine,Is.EqualTo(29));
        }
        [Test] public void DoorAndCheckAreSeparate_AliveRoomEnemyBlocksCheck()
        {
            Start();Enter();var room=service.GetEnemyAssignments(id).Data.First(e=>e.RoomId.Length>0).RoomId;
            Assert.That(service.OpenRoomDoor(id,room).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));Range(room+".door");service.OpenRoomDoor(id,room);
            Assert.That(State.Floors.SelectMany(f=>f.Rooms).Single(r=>r.RoomId==room).SearchState,Is.EqualTo(RoomSearchState.Searching));
            Range(room+".check");Assert.That(service.MarkRoomSearched(id,room).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            foreach(var enemy in service.GetEnemyAssignments(id).Data.Where(e=>e.RoomId==room)){Hit(enemy.EntityId,Shot());service.Advance(id);}
            Assert.That(service.MarkRoomSearched(id,room).Success);var revision=State.Revision;
            Assert.That(service.MarkRoomSearched(id,room).Success);Assert.That(service.OpenRoomDoor(id,room).Success);Assert.That(State.Revision,Is.EqualTo(revision));
        }
        [Test] public void EmptyRoomStillNeedsDoorAndCheckArea()
        {
            service.Dispose();service=new UrbanService(P3Fixtures.UrbanDefinition,clock,new EndpointRandom(false),new FakeCombatWorld());Start();Enter();
            var room=Rooms.First(r=>!service.GetEnemyAssignments(id).Data.Any(e=>e.RoomId==r));
            Range(room+".check");Assert.That(service.MarkRoomSearched(id,room).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Range(room+".door");service.OpenRoomDoor(id,room);Assert.That(service.MarkRoomSearched(id,room).Success);Assert.That(State.RoomsSearched,Is.EqualTo(1));
        }
        [Test] public void RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps()
        {
            Start();Enter();Kill(EncounterGroup.Building);foreach(var room in Rooms)Check(room);
            Assert.That(service.CompleteIfReady(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));Assert.That(State.RoomsSearched,Is.EqualTo(3));
            int outcomes=0;service.ResultReady+=_=>outcomes++;Kill(EncounterGroup.Street);var result=service.GetResult(id).Data;
            Assert.That(result.Victory);Assert.That(result.FloorMaps.Count,Is.EqualTo(3));Assert.That(State.Phase,Is.EqualTo(UrbanPhase.Results));
            Assert.That(service.CompleteIfReady(id).Data.Equals(result));Assert.That(outcomes,Is.EqualTo(1));
        }
        [Test] public void AllKillsWithoutRoomChecksCannotWin()
        {Start();Kill();Assert.That(service.CompleteIfReady(id).ErrorCode,Is.EqualTo(ErrorCode.InvalidState));Assert.That(State.RoomsSearched,Is.Zero);}
        [Test] public void DeathCompetesWithLastCheckAndKill_AndWins()
        {
            service.Dispose();service=new UrbanService(P3Fixtures.UrbanDefinition,clock,new EndpointRandom(false),new FakeCombatWorld(),CombatConfigDto.Default.WithPlayerHealth(5));
            Start();Enter();Kill(EncounterGroup.Building);foreach(var room in Rooms.Take(2))Check(room);
            var last=Rooms.Last();Range(last+".door");service.OpenRoomDoor(id,last);Range(last+".check");
            var enemy=service.GetEnemyAssignments(id).Data.Single(e=>e.Group==EncounterGroup.Street);var shot=Shot();
            service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="sight",Kind=CombatInputKind.Perception,EntityId=enemy.EntityId,TargetId=id+".player",Position=Vector3.forward*10,Direction=Vector3.back,Flag=true});service.Advance(id);
            clock.Advance(1);Hit(enemy.EntityId,shot);Assert.That(service.MarkRoomSearched(id,last).Success);service.Advance(id);
            var result=service.GetResult(id).Data;Assert.That(result.Victory,Is.False);Assert.That(result.RoomsSearched,Is.EqualTo(3));Assert.That(result.EnemyKilled,Is.EqualTo(result.EnemyTotal));
        }
        [Test] public void RetryResetsDoorsAndSnapshotsWithoutChangingOldResult()
        {
            Start();Enter();Kill();foreach(var room in Rooms)Check(room);var old=service.GetResult(id).Data;var oldId=id;
            service.Cancel(id);Start();Assert.That(service.GetSession(oldId).ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(State.Floors.SelectMany(f=>f.Rooms).All(r=>!r.DoorOpen&&r.SearchState==RoomSearchState.Unsearched));
            Assert.That(service.GetVisualSnapshot(id).Data.Doors.All(d=>!d.Open));Assert.That(old.RoomsSearched,Is.EqualTo(3));Assert.That(State.Ammo.CurrentMagazine,Is.EqualTo(30));
        }
        sealed class EndpointRandom:ICombatRandom
        {readonly bool max;public EndpointRandom(bool max){this.max=max;}public void Reset(RandomSeed seed){}public int NextInt(int minInclusive,int maxExclusive)=>max?maxExclusive-1:minInclusive;}
    }
}
