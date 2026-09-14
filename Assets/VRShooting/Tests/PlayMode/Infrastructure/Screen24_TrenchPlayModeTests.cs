using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.PlayMode
{
    public sealed class Screen24_TrenchPlayModeTests
    {
        // BDD12/14 preview-start, BDD15 service HUD, BDD17 victory/failure/retry; no real trench scene.
        [UnityTest] public IEnumerator FakeScene_BriefingCombatVictoryRetryFailure_UpdatesHudAndUnsubscribes()
        {
            var clock=new FakeCombatClock();var nav=new FakeCombatWorld();
            using var service=new TrenchService(P3Fixtures.TrenchDefinition,clock,new SeededCombatRandom(),nav,CombatConfigDto.Default.WithPlayerHealth(5));
            var scene=SceneManager.CreateScene("TrenchServiceTestScene");
            var view=new GameObject("Hud_Trench_Probe",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            SceneManager.MoveGameObjectToScene(view,scene);var probe=view.AddComponent<TrenchHudProbe>();probe.Bind(service);
            try
            {
                Assert.That(service.GetBriefing("trench-a","training-rifle",RandomSeed.Fixed(12)).Success); Assert.That(nav.NavigationRequests,Is.Empty);
                string id=service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(12)).Data.SessionId;
                Assert.That(probe.LastText,Does.Contain("30/120"));int results=0;service.ResultReady+=_=>results++;
                var targets=service.GetVisualSnapshot(id).Data.Entities.Where(e=>e.Role==CombatEntityRole.Enemy).ToArray();
                service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="search",Kind=CombatInputKind.AreaPresence,EntityId="trench-a.node-001",Flag=true});service.Advance(id);
                Assert.That(service.GetResult(id).Success,Is.False);
                foreach(var enemy in targets)
                {
                    service.Combat.SetGrip(new WeaponGripStateInputDto {SessionId=id,HoldState=WeaponHoldState.TwoHandHeld,RearHandTracked=true,FrontHandTracked=true});
                    var shot=service.Combat.Fire(new WeaponFireInputDto {SessionId=id,AimDirection=Vector3.forward}).Data;
                    service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId=shot.ShotId,Kind=CombatInputKind.Hit,
                        EntityId=id+".player",TargetId=enemy.EntityId,ShotId=shot.ShotId,Flag=true,Value=1});service.Advance(id);yield return null;
                }
                Assert.That(service.GetResult(id).Data.Victory);Assert.That(results,Is.EqualTo(1));Assert.That(service.GetHud(id).Data.CanShoot,Is.False);
                var oldId=id;service.Cancel(id);service.GetBriefing("trench-a","training-rifle",RandomSeed.Fixed(12));
                id=service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(12)).Data.SessionId;Assert.That(id,Is.Not.EqualTo(oldId));
                Assert.That(probe.LastText,Does.Contain("30/120"));var attacker=service.GetVisualSnapshot(id).Data.Entities.First(e=>e.Role==CombatEntityRole.Enemy);
                service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="see",Kind=CombatInputKind.Perception,EntityId=attacker.EntityId,
                    TargetId=id+".player",Position=Vector3.forward*10,Direction=Vector3.back,Flag=true});service.Advance(id);
                clock.Advance(1);service.Advance(id);Assert.That(service.GetResult(id).Data.Victory,Is.False);Assert.That(results,Is.EqualTo(2));
                Assert.That(probe.LastText,Does.StartWith("0:"));
                yield return SceneManager.UnloadSceneAsync(scene);
                var count=TrenchHudProbe.Updates;
                service.Cancel(id);service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(1));yield return null;
                Assert.That(TrenchHudProbe.Updates,Is.EqualTo(count));
            }
            finally {if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
        }
        [UnityTest] public IEnumerator FakeNavigation_AcknowledgesActualSquadPositions_AndStopsAtCancel()
        {
            var clock=new FakeCombatClock();var nav=new FakeCombatWorld();
            using var service=new TrenchService(P3Fixtures.TrenchDefinition,clock,new SeededCombatRandom(),nav);
            var id=service.StartSession("trench-a","training-rifle",RandomSeed.Fixed(1)).Data.SessionId;
            service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="pose",Kind=CombatInputKind.PlayerPose,EntityId=id+".player",Position=Vector3.forward*4,Direction=Vector3.forward});
            service.Advance(id);Assert.That(nav.NavigationRequests.Count,Is.EqualTo(2));
            foreach(var request in nav.NavigationRequests.ToArray())
                service.Submit(new CombatInputDto {SessionId=id,Tick=clock.Tick,EventId="ack-"+request.RequestId,Kind=CombatInputKind.NavigationResult,
                    EntityId=request.EntityId,TargetId=request.RequestId,Position=request.Destination,Direction=request.Forward,Flag=true});
            service.Advance(id);Assert.That(service.GetSquadStatus(id).Data.Squad.Members[1].WorldPosition,Is.EqualTo(Vector3.forward*2.5f));
            Assert.That(service.GetVisualSnapshot(id).Data.Entities.Single(e=>e.EntityId==id+".teammate-3").Forward,Is.EqualTo(Vector3.back));
            service.Cancel(id);var requests=nav.NavigationRequests.Count;clock.Advance(5);service.Advance(id);
            Assert.That(nav.NavigationRequests.Count,Is.EqualTo(requests));yield return null;
        }
    }
    public sealed class TrenchHudProbe:MonoBehaviour
    {
        IHUDService service;
        public static int Updates;
        public string LastText=>GetComponent<Text>().text;
        public void Bind(IHUDService value){service=value;service.HudUpdated+=Render;}
        void Render(HudDto dto){Updates++;GetComponent<Text>().text=dto.Player.Health+":"+dto.Ammo.CurrentMagazine+"/"+dto.Ammo.ReserveAmmo;}
        void OnDestroy(){if(service!=null)service.HudUpdated-=Render;}
    }
}
