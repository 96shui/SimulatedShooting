using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD23/27/29, P3 task004/010/015/019/020. Production mission, scene and input adapter.
    public sealed class Mode4AlignmentPlayModeTests
    {
        const string Evidence = "docs/codex-reports/evidence/mode4-ai-shoulder-2026-11-12";
        CombatApplicationCoordinator app;
        CombatSceneRuntime runtime;
        CombatMission mission => app.Mission;
        string id => mission.SessionId;
        P3CombatUIRoot ui;
        XRController right;

        [UnitySetUp] public IEnumerator Setup()
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            app = GameMain.Instance.Services.Combat;
            Assert.That(app.OpenMode(TrainingMode.Urban).Success, Is.True);
            var load = app.SelectMapAsync("urban-a", RandomSeed.Fixed(31));
            while (!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True, load.Result.Message);
            runtime = Object.FindObjectOfType<CombatSceneRuntime>(); runtime.ManualStepping = true;
            runtime.InputOverride = new ManualXRTrainingInput();
            ui = GameMain.Instance.GetComponent<P3LiveUIController>().View;
            // Keep support fire from killing the subject before the AI/throw assertions.
            foreach (var actor in runtime.Actors.Where(p=>p.Key.Contains("teammate"))) actor.Value.SetActionsLocked(true);
            Frame(runtime.PlayerRoot.position);
        }

        void Frame(Vector3 position)
        {
            var body = runtime.PlayerRoot.GetComponent<CharacterController>(); body.enabled = false;
            runtime.PlayerRoot.position = position; body.enabled = true;
            runtime.FrameOverride = new CombatInputFrameDto { HeadTracked=true, RearHandTracked=true, FrontHandTracked=true,
                RearGripInRange=true, FrontGripInRange=true, PlayerPosition=position, PlayerForward=runtime.PlayerRoot.forward,
                MuzzlePosition=runtime.PlayerCamera.transform.position, AimDirection=Vector3.up };
            Physics.SyncTransforms();
        }
        void Step(float seconds=.02f)
        {
            runtime.Step(seconds);
            Assert.That(runtime.LastFrameResult.Success, Is.True, runtime.LastFrameResult.Message);
        }
        CombatActorView OpenEnemyRoom()
        {
            var target = mission.Urban.GetEnemyAssignments(id).Data.First(a=>a.RoomId.Length>0);
            Assert.That(mission.World.Submit(new CombatInputDto { SessionId=id, EventId="open-room", Tick=runtime.Clock.Tick,
                Kind=CombatInputKind.AreaPresence, EntityId=target.RoomId+".door", Flag=true }).Success, Is.True);
            Assert.That(mission.Advance().Success, Is.True);
            Assert.That(mission.Urban.OpenRoomDoor(id,target.RoomId).Success, Is.True);
            return runtime.Actors[target.EntityId];
        }

        [UnityTest] public IEnumerator Bdd23_UrbanAssignmentEnemyTurnsAimsFiresAndStopsWhenOccluded()
        {
            var enemy = OpenEnemyRoom();
            var position=enemy.PerceptionOrigin.position+enemy.transform.forward*1.2f+enemy.transform.right*.5f;
            position.y=enemy.GroundedFeetPosition.y+.05f;
            Frame(position);
            runtime.PlayerCamera.transform.rotation=Quaternion.LookRotation(enemy.PerceptionOrigin.position-runtime.PlayerCamera.transform.position);
            Step(.01f);
            Assert.That(mission.Core.GetSnapshot(id).Data.Player.Health, Is.EqualTo(10));
            var health=ui.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Hud_UrbanBuilding_Health");
            Assert.That(health.text, Does.Contain("10 / 10"));
            var cells=health.transform.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("Hud_Combat_HealthCell_")).ToArray();
            Assert.That(cells.Length, Is.EqualTo(10)); Assert.That(cells.All(c=>c.enabled), Is.True);
            Capture("urban-health-10");
            var before=enemy.transform.rotation; Step(.05f);
            Assert.That(Quaternion.Angle(before,enemy.transform.rotation), Is.InRange(.01f,9.01f));
            Assert.That(enemy.transform.forward.y, Is.EqualTo(0).Within(.001f));
            Assert.That(enemy.ShotVisualFeedbackCount, Is.Zero);
            for(var i=0;i<60&&enemy.ShotVisualFeedbackCount==0;i++) Step(.02f);
            Assert.That(enemy.ShotVisualFeedbackCount, Is.EqualTo(1));
            Assert.That(enemy.ShotAudioFeedbackCount, Is.EqualTo(1));
            Assert.That(Vector3.Angle(enemy.transform.forward,Vector3.ProjectOnPlane(position-enemy.transform.position,Vector3.up)), Is.LessThan(12));
            Assert.That(mission.Urban.GetVisualSnapshot(id).Data.Entities.Single(e=>e.EntityId==enemy.EntityId).Forward,
                Is.EqualTo(enemy.transform.forward));
            Capture("urban-enemy-aim-and-fire");
            // The production presenter refreshes at 10 Hz, independently of the injected combat clock.
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(health.text, Does.Contain("9 / 10"));
            Assert.That(cells.Count(c=>c.enabled), Is.EqualTo(9));
            Capture("urban-health-after-hit-9");
            var shotCount=enemy.ShotVisualFeedbackCount;
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position=(enemy.PerceptionOrigin.position+position+Vector3.up*1.2f)*.5f;
            obstacle.transform.localScale=new Vector3(2,3,.2f); Physics.SyncTransforms();
            Step(.02f); Step(2);
            Assert.That(enemy.ShotVisualFeedbackCount, Is.EqualTo(shotCount));
            Object.Destroy(obstacle);
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd27_UrbanRightAThrowsVisibleGrenadeExplodesAndUsesCooldown()
        {
            Assert.That(mission.Grenades, Is.TypeOf<SquadGrenadeTacticService>());
            var bindings=Object.FindObjectOfType<CombatSceneBindings>();
            Assert.That(bindings.GrenadePrefab, Is.Not.Null); Assert.That(bindings.GrenadeExplosionClip, Is.Not.Null);
            var enemy=OpenEnemyRoom();
            var teammate=runtime.Actors[id+".teammate-2"];
            teammate.SetActionsLocked(false); teammate.Agent.enabled=false;
            teammate.transform.position=enemy.transform.position+enemy.transform.forward*2;
            // Prevent support bullets while preserving the actual throw animation and world sweep.
            teammate.SetCombatAiming(false);
            Frame(enemy.transform.position+enemy.transform.forward*4);
            runtime.PlayerCamera.transform.rotation=Quaternion.LookRotation(enemy.PerceptionOrigin.position-runtime.PlayerCamera.transform.position);
            InputSystem.RegisterLayout(@"{""name"":""UrbanGrenadeController"",""extend"":""XRController"",""controls"":[{""name"":""primaryButton"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32}]}");
            right=(XRController)InputSystem.AddDevice("UrbanGrenadeController");
            InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
            var input=new P3XRTrainingInput(); runtime.InputOverride=input;
            input.Sample(true); Step(); Assert.That(mission.Grenades.Current.HasValue, Is.False);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("primaryButton"),1f);
            yield return null; input.Sample(true); Step();
            Assert.That(mission.Grenades.Current.HasValue, Is.True);
            var plan=mission.Grenades.Current.Value;
            Assert.That(plan.ThrowerId, Is.EqualTo(id+".teammate-2"));
            Assert.That(runtime.GetComponentsInChildren<CombatGrenadeProjectile>().Length, Is.EqualTo(1));
            yield return null; input.Sample(true); Assert.That(input.GrenadePressed, Is.False);
            GrenadeExplosionDto blast=default; mission.Grenades.GrenadeExploded+=v=>blast=v;
            for(var i=0;i<180&&mission.Grenades.Current.HasValue;i++) { input.Sample(true); Step(.02f); }
            Assert.That(blast.GrenadeId, Is.EqualTo(plan.GrenadeId));
            Assert.That(blast.Targets, Does.Contain(enemy.EntityId));
            Assert.That(mission.Urban.GetSession(id).Data.BuildingEnemyKilled, Is.GreaterThanOrEqualTo(1));
            var effect=runtime.GetComponentInChildren<CombatExplosionVfx>(); Assert.That(effect, Is.Not.Null);
            Assert.That(effect.GetComponent<AudioSource>().clip, Is.SameAs(bindings.GrenadeExplosionClip));
            Assert.That(effect.GetComponent<AudioSource>().isPlaying, Is.True);
            // Render after the real explosion has updated its camera-facing sprite and removed the projectile.
            yield return null;
            var eye=runtime.PlayerCamera.transform;
            var aim=blast.Position+Vector3.up*.85f;
            var found=false;
            foreach(var distance in new[]{3f,2f,1.4f})
            {
                for(var angle=0;angle<360;angle+=30)
                {
                    var candidate=blast.Position+Vector3.up*1.45f+Quaternion.Euler(0,angle,0)*Vector3.forward*distance;
                    if(Physics.CheckSphere(candidate,.1f,~(1<<2),QueryTriggerInteraction.Ignore) ||
                        Physics.Linecast(candidate,aim,~(1<<2),QueryTriggerInteraction.Ignore))continue;
                    eye.SetPositionAndRotation(candidate,Quaternion.LookRotation(aim-candidate));found=true;break;
                }
                if(found)break;
            }
            Assert.That(found, Is.True,"Explosion evidence requires an unobstructed camera inside scene geometry");
            Debug.Log("Mode4Evidence grenade="+blast.GrenadeId+" position="+blast.Position+" camera="+eye.position+" kills="+blast.Targets.Count);
            yield return new WaitForSecondsRealtime(.12f); // Billboard and 10 Hz presenter observe the confirmed explosion.

            Capture("urban-teammate-grenade-explosion");
            mission.Grenades.RequestFirstTeammateThrow(); mission.Grenades.Advance();
            Assert.That(mission.Grenades.Current.HasValue, Is.False,"Throw deadline must retain the shared three-second cooldown");
            yield return null;
        }

        void Capture(string name) => PresentationEvidence.Capture(runtime.PlayerCamera,name,Evidence,true);

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(right!=null&&right.added)InputSystem.RemoveDevice(right);
            InputSystem.RemoveLayout("UrbanGrenadeController");
            app?.ReturnToMainMenu();
            if(GameMain.Instance!=null)Object.Destroy(GameMain.Instance.gameObject);
            yield return null; yield return null;
        }
    }
}
