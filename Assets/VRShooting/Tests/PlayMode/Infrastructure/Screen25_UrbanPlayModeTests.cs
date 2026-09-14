using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.PlayMode
{
    public sealed class Screen25_UrbanPlayModeTests
    {
        // BDD18–21/25: production services with scene fact substitutes, not a real urban scene.
        [UnityTest] public IEnumerator SceneFactsDriveStreetBuildingDoorsVictoryAndRetry()
        {
            var clock=new FakeCombatClock();using var service=new UrbanService(P3Fixtures.UrbanDefinition,clock,new SeededCombatRandom(),new FakeCombatWorld());
            var id=service.StartSession("urban-a","training-rifle",RandomSeed.Fixed(4)).Data.SessionId;int sequence=0,results=0;
            service.SessionChanged+=s=>{Assert.That(service.GetSession(id).Data.Revision,Is.EqualTo(s.Revision));Assert.That(service.GetHud(id).Data.Ammo.CurrentMagazine,Is.EqualTo(s.Ammo.CurrentMagazine));};
            service.ResultReady+=_=>results++;
            void Range(string key){service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="r-"+ ++sequence,Kind=CombatInputKind.AreaPresence,EntityId=key,Flag=true});service.Advance(id);}
            Range("urban-a.entrance");service.EnterBuilding(id,"urban-a.entrance");Assert.That(service.GetHud(id).Data.HudType,Is.EqualTo(HudType.UrbanBuilding));
            foreach(var enemy in service.GetEnemyAssignments(id).Data)
            {
                service.Combat.SetGrip(new WeaponGripStateInputDto {SessionId=id,HoldState=WeaponHoldState.TwoHandHeld,RearHandTracked=true,FrontHandTracked=true});
                var shot=service.Combat.Fire(new WeaponFireInputDto {SessionId=id,AimDirection=Vector3.forward}).Data;
                service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="h-"+ ++sequence,Kind=CombatInputKind.Hit,EntityId=id+".player",TargetId=enemy.EntityId,ShotId=shot.ShotId,Flag=true,Value=1});
                service.Advance(id);yield return null;
            }
            Assert.That(service.GetResult(id).Success,Is.False);
            foreach(var room in P3Fixtures.UrbanDefinition.Floors.SelectMany(f=>f.Rooms))
            {
                Range(room.RoomId+".door");Assert.That(service.OpenRoomDoor(id,room.RoomId).Success);
                Assert.That(service.GetVisualSnapshot(id).Data.Doors.Single(d=>d.RoomId==room.RoomId).Open);
                Range(room.RoomId+".check");Assert.That(service.MarkRoomSearched(id,room.RoomId).Success);service.Advance(id);yield return null;
            }
            var result=service.GetResult(id).Data;Assert.That(result.Victory);Assert.That(result.FloorMaps.All(m=>m.Markers.Any(x=>x.Type==MarkerType.SearchedRoom)));
            Assert.That(results,Is.EqualTo(1));var oldId=id;service.Cancel(id);
            // Remove the current-session assertion while Start publishes its new ID.
            service.Dispose();using var retry=new UrbanService(P3Fixtures.UrbanDefinition,clock,new SeededCombatRandom(),new FakeCombatWorld());
            id=retry.StartSession("urban-a","training-rifle",RandomSeed.Fixed(4)).Data.SessionId;
            Assert.That(id,Is.Not.EqualTo(oldId));Assert.That(retry.GetSession(id).Data.RoomsSearched,Is.Zero);
            Assert.That(retry.GetVisualSnapshot(id).Data.Doors.All(d=>!d.Open));Assert.That(result.RoomsSearched,Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator BuildingFailureKeepsCountsAndOldSessionInputsAreRejected()
        {
            var clock=new FakeCombatClock();using var service=new UrbanService(P3Fixtures.UrbanDefinition,clock,new SeededCombatRandom(),new FakeCombatWorld(),CombatConfigDto.Default.WithPlayerHealth(5));
            var id=service.StartSession("urban-a","training-rifle",RandomSeed.Fixed(1)).Data.SessionId;
            service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="entry",Kind=CombatInputKind.AreaPresence,EntityId="urban-a.entrance",Flag=true});service.Advance(id);service.EnterBuilding(id,"urban-a.entrance");
            var enemy=service.GetEnemyAssignments(id).Data.First(e=>e.Group==EncounterGroup.Street);
            service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="sight",Kind=CombatInputKind.Perception,EntityId=enemy.EntityId,TargetId=id+".player",Position=Vector3.forward*10,Direction=Vector3.back,Flag=true});service.Advance(id);
            clock.Advance(1);service.Advance(id);yield return null;
            Assert.That(service.GetResult(id).Data.Victory,Is.False);Assert.That(service.GetSession(id).Data.Phase,Is.EqualTo(UrbanPhase.Results));
            var oldId=id;service.Cancel(id);id=service.StartSession("urban-a","training-rifle",RandomSeed.Fixed(1)).Data.SessionId;
            Assert.That(service.Submit(new CombatInputDto {SessionId=oldId,Tick=clock.Tick,EventId="old",Kind=CombatInputKind.AreaPresence,EntityId="urban-a.entrance",Flag=true}).ErrorCode,Is.EqualTo(ErrorCode.NotFound));
            Assert.That(service.GetSession(id).Data.Player.IsAlive);Assert.That(service.GetHud(id).Data.HudType,Is.EqualTo(HudType.UrbanStreet));
        }
    }
}
