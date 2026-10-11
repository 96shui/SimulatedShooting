using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class P3ProductionFlowTests
    {
        CombatApplicationCoordinator app;
        P3CombatUIRoot ui;
        CombatSceneRuntime runtime;
        ManualXRTrainingInput input;
        readonly System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem> suspendedDisplays=new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
        [UnityTest] public IEnumerator Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds()
        {
            var displays=new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            foreach(var display in displays)if(display.running){suspendedDisplays.Add(display);display.Stop();}
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            app=GameMain.Instance.Services.Combat;ui=GameMain.Instance.GetComponent<P3LiveUIController>().View;
            foreach(var mode in new[]{TrainingMode.Trench,TrainingMode.Urban})
            {
                app.OpenMode(mode);
                var load=app.SelectMapAsync(mode==TrainingMode.Trench?"trench-a":"urban-a",VRShooting.Contracts.RandomSeed.Fixed(20260916));
                while(!load.IsCompleted)yield return null;
                Assert.That(load.Result.Success,Is.True,load.Result.Message);
                runtime=Object.FindObjectOfType<CombatSceneRuntime>();
                Assert.That(runtime.IsVr,Is.False);
                if(mode==TrainingMode.Trench)app.Start();
                yield return null;
                Assert.That(Object.FindObjectsOfType<AudioListener>().Count(x=>x.isActiveAndEnabled),Is.EqualTo(1));
                Assert.That(Object.FindObjectsOfType<Camera>().Count(x=>x.isActiveAndEnabled&&x.targetTexture==null),Is.EqualTo(1),"Drone capture is a texture feed, not a second player output");
                var page=(RectTransform)ui.transform.Find("Screen_"+app.Snapshot.Screen);
                var corners=new Vector3[4];page.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var screen=RectTransformUtility.WorldToScreenPoint(null,corner);
                    Assert.That(screen.x,Is.InRange(-1f,Screen.width+1f),"Page horizontal clipping");
                    Assert.That(screen.y,Is.InRange(-1f,Screen.height+1f),"Page vertical clipping");
                }
                if(mode==TrainingMode.Urban)
                {
                    var squad=(RectTransform)page.Find("Hud_UrbanStreet_Squad");
                    var button=(RectTransform)page.Find("Button_UrbanStreetHud_EnterBuilding");
                    var squadBounds=new Vector3[4];var buttonBounds=new Vector3[4];
                    squad.GetWorldCorners(squadBounds);button.GetWorldCorners(buttonBounds);
                    Assert.That(squadBounds[1].y,Is.LessThan(buttonBounds[0].y),"Squad status must not overlap the entry button");
                }
                app.ReturnToMainMenu();
                Assert.That(Object.FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>().Count(x=>x.isActiveAndEnabled),Is.EqualTo(1),"Returning must disable the departing manager before reactivating MainScene");
                yield return null;yield return null;
                Assert.That(VRShooting.Unity.Player.PlayerFollowCamera.Instance.OutputEnabled,Is.True);
            }
        }
        // BDD14/17 and BDD18-21: real menu, scene lease, physics hits, DTO pages and retry/exit.
        [UnityTest] public IEnumerator Screen17_Trench_ProductionVictoryRetryAndReturn()=>Complete(TrainingMode.Trench);
        [UnityTest] public IEnumerator Screen21_Urban_ProductionVictoryRetryAndReturn()=>Complete(TrainingMode.Urban);
        [UnityTest] public IEnumerator Screen17_Trench_DeathThreeRetriesTrackingAndMovement()=>DeathFlow(TrainingMode.Trench);
        [UnityTest] public IEnumerator Screen21_Urban_DeathThreeRetriesTrackingAndMovement()=>DeathFlow(TrainingMode.Urban);
        // BDD14/18 cancellation: a discarded asynchronous load cannot hide the menu or steal the new scene.
        [UnityTest] public IEnumerator Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene()
        {
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            app=GameMain.Instance.Services.Combat;
            app.OpenMode(TrainingMode.Trench);
            var abandoned=app.SelectMapAsync("trench-a",VRShooting.Contracts.RandomSeed.Fixed(1));
            app.ReturnToMainMenu();app.OpenMode(TrainingMode.Urban);
            var selected=app.SelectMapAsync("urban-a",VRShooting.Contracts.RandomSeed.Fixed(2));
            while(!abandoned.IsCompleted||!selected.IsCompleted)yield return null;
            Assert.That(abandoned.Result.Success,Is.False);
            Assert.That(selected.Result.Success,Is.True,selected.Result.Message);
            Assert.That(Object.FindObjectsOfType<CombatSceneRuntime>().Length,Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("CombatScene"));
            Assert.That(app.Snapshot.Screen,Is.EqualTo(ScreenId.UrbanStreetHud));
            app.ReturnToMainMenu();yield return null;yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainScene"));
            Assert.That(Object.FindObjectsOfType<CombatSceneRuntime>().Length,Is.EqualTo(0));
        }
        IEnumerator DeathFlow(TrainingMode mode)
        {
            yield return RunDeathFlow(mode);
        }

        [UnityTest]
        public IEnumerator Bdd27_DesktopThrowSpawnsVisibleM67UntilExplosion()
        {
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            app=GameMain.Instance.Services.Combat;ui=GameMain.Instance.GetComponent<P3LiveUIController>().View;
            Assert.That(app.OpenMode(TrainingMode.Trench).Success,Is.True);
            var load=app.SelectMapAsync("trench-a",VRShooting.Contracts.RandomSeed.Fixed(20260916));
            while(!load.IsCompleted)yield return null;
            Assert.That(load.Result.Success,Is.True,load.Result.Message);
            runtime=Object.FindObjectOfType<CombatSceneRuntime>();runtime.ManualStepping=true;
            input=new ManualXRTrainingInput();runtime.InputOverride=input;
            ui.TrenchBriefingView.StartButton.onClick.Invoke();
            yield return CompleteOpening();
            runtime.FrameOverride=new CombatInputFrameDto {HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,
                PlayerPosition=runtime.PlayerRoot.position,PlayerForward=runtime.PlayerRoot.forward,
                MuzzlePosition=runtime.PlayerCamera.transform.position,AimDirection=runtime.PlayerCamera.transform.forward};
            var previousBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var previousEditorInput=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=
                UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            keyboard.MakeCurrent();UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
            try
            {
                var controls=new P3XRTrainingInput();runtime.InputOverride=controls;
                controls.Sample(false);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.H));
                yield return null;keyboard.MakeCurrent();controls.Sample(false);
                Assert.That(controls.GrenadePressed,Is.True,"Input System must expose the H press edge");
                runtime.Step(.02f);
                var tactic=app.Mission.Trench.GrenadeTactics;
                Assert.That(tactic.Current.HasValue,Is.True,"Desktop H must reach the production grenade command: "+
                    runtime.LastFrameResult.ErrorCode+" / "+app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.State+
                    " / locked="+runtime.CharacterActionsLocked+" / actors="+string.Join(",",runtime.Actors.Keys));
                var exploded=false;tactic.GrenadeExploded+=value=>exploded=true;
                // Isolate projectile lifetime from the separately tested player-death cancellation rule.
                var safePosition=runtime.PlayerRoot.position+Vector3.right*1000;
                Place(safePosition);
                runtime.FrameOverride=new CombatInputFrameDto {HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,
                    PlayerPosition=safePosition,PlayerForward=Vector3.forward,
                    MuzzlePosition=safePosition+Vector3.up*1.4f,AimDirection=Vector3.forward};
                var projectile=runtime.GetComponentsInChildren<CombatGrenadeProjectile>().Single();
                var model=projectile.transform.GetChild(0).gameObject;
                // The launched root pitches/tumbles; measure the authored model height in its upright frame.
                var launchRotation=projectile.transform.rotation;
                projectile.transform.rotation=Quaternion.identity;
                var uprightHeight=CombatGrenadeProjectile.GetModelBounds(model).size.y;
                projectile.transform.rotation=launchRotation;
                Assert.That(uprightHeight,Is.EqualTo(.22f).Within(.001f));
                Assert.That(Vector3.Distance(projectile.transform.lossyScale,Vector3.one),Is.LessThan(.0001f));
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(renderer.enabled&&renderer.gameObject.activeInHierarchy&&!renderer.forceRenderingOff,Is.True);
                    Assert.That(renderer.sharedMaterial,Is.Not.Null);
                    Assert.That(renderer.sharedMaterial.GetFloat("_Surface"),Is.EqualTo(0));
                }
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                yield return null;controls.Sample(false);
                runtime.Step(.75f);yield return null;
                Assert.That(projectile!=null&&projectile.gameObject.activeInHierarchy,Is.True,"Released grenade remains visible");
                Assert.That(Vector3.Distance(projectile.transform.position,tactic.ProjectilePosition),Is.LessThan(.001f));
                CaptureGrenadeEvidence(model);
                runtime.Step(.75f);yield return null;
                Assert.That(projectile!=null,Is.True,"Landed grenade remains until its service fuse expires");
                runtime.Step(2f);yield return null;
                Assert.That(exploded,Is.True,"The model must survive until the real service explosion: "+
                    runtime.LastFrameResult.ErrorCode+" / "+app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.State+
                    " / now="+runtime.Clock.Now+" / active="+tactic.Current.HasValue);
                Assert.That(runtime.GetComponentsInChildren<CombatGrenadeProjectile>().Length,Is.Zero);
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=previousBackground;
#if UNITY_EDITOR
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;
#endif
            }
        }

        IEnumerator RunDeathFlow(TrainingMode mode)
        {
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            app=GameMain.Instance.Services.Combat;ui=GameMain.Instance.GetComponent<P3LiveUIController>().View;
            Assert.That(app.OpenMode(mode).Success,Is.True);
            var load=app.SelectMapAsync(mode==TrainingMode.Trench?"trench-a":"urban-a",VRShooting.Contracts.RandomSeed.Fixed(20260916));
            while(!load.IsCompleted)yield return null;
            Assert.That(load.Result.Success,Is.True,load.Result.Message);
            runtime=Object.FindObjectOfType<CombatSceneRuntime>();runtime.ManualStepping=true;
            Assert.That(runtime.GetComponentsInChildren<CharacterController>().Count(c=>c.enabled),Is.EqualTo(1),"Only the selected player capsule may collide");
            var manager=runtime.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            Assert.That(manager!=null&&manager.isActiveAndEnabled,Is.True,"P3 owns an active interaction manager after hiding MainScene");
            Assert.That(runtime.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true).All(i=>i.interactionManager==manager),Is.True);
            input=new ManualXRTrainingInput();runtime.InputOverride=input;
            if(mode==TrainingMode.Trench)ui.TrenchBriefingView.StartButton.onClick.Invoke();
            yield return CompleteOpening();
            var liveRifles=runtime.GetComponentsInChildren<TrainingRifleGrabInteractable>(true).Where(g=>g.gameObject.activeInHierarchy).ToArray();
            Assert.That(liveRifles.Length,Is.EqualTo(1));
            Assert.That(liveRifles[0].interactionManager,Is.SameAs(manager));
            string previous="";
            for(int round=0;round<3;round++)
            {
                var id=app.Mission.SessionId;Assert.That(id,Is.Not.EqualTo(previous));previous=id;
                input.Clear();
                var start=runtime.PlayerRoot.position;
                input.SetMoveAxis(Vector2.up);
                Sample(start,start+Vector3.up*1.4f,Vector3.forward);
                Assert.That(Vector3.Distance(start,runtime.PlayerRoot.position),Is.GreaterThan(.01f),"Production character movement");
                input.SetMoveAxis(Vector2.zero);
                var headLocal=runtime.PlayerCamera.transform.localPosition;
                runtime.FrameOverride=new CombatInputFrameDto{HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,IsRealVr=true,
                    MuzzlePosition=start+Vector3.up*1.4f,AimDirection=Vector3.forward,PlayerPosition=runtime.PlayerRoot.position,PlayerForward=Vector3.forward,RequestedPosture=PlayerPosture.Crouching};
                runtime.Step(.02f);
                Assert.That(runtime.PlayerCamera.transform.localPosition,Is.EqualTo(headLocal),"VR posture must not move HMD");
                var enemy=runtime.Actors.Values.First(a=>a.EntityId.Contains(".enemy-")&&CanStandInFront(a));
                var at=enemy.transform.position+enemy.transform.forward*.8f;Place(at);
                Sample(at,at+Vector3.up*1.4f,Vector3.forward);
                var health=app.Mission.Core.GetSnapshot(id).Data.Player.Health;
                runtime.FrameOverride=new CombatInputFrameDto{HeadTracked=false,RearHandTracked=false,FrontHandTracked=false,
                    MuzzlePosition=at+Vector3.up*1.4f,AimDirection=Vector3.forward,PlayerPosition=at,PlayerForward=Vector3.forward};
                runtime.Step(20);
                Assert.That(app.Mission.Core.GetSnapshot(id).Data.Player.Health,Is.EqualTo(health),"Tracking loss stops enemy attacks");
                Sample(at,at+Vector3.up*1.4f,Vector3.forward);
                for(int second=0;second<15&&!app.Snapshot.Summary.HasValue;second++)runtime.Step(1.01f);
                Assert.That(app.Snapshot.Summary.HasValue,Is.True,"Real line-of-sight enemy attacks must reach failure");
                Assert.That(app.Snapshot.Summary.Value.Victory,Is.False);
                if(round<2)
                {
                    if(mode==TrainingMode.Trench)ui.TrenchResultsView.RetryButton.onClick.Invoke();else ui.UrbanResultsView.RetryButton.onClick.Invoke();
                    if(mode==TrainingMode.Trench)ui.TrenchBriefingView.StartButton.onClick.Invoke();
                    yield return CompleteOpening();
                    Assert.That(runtime.Actors.Values.All(a=>!a.IsDead),Is.True);
                    Assert.That(app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.Ammo.CurrentMagazine,Is.EqualTo(30));
                }
            }
            if(mode==TrainingMode.Trench)ui.TrenchResultsView.BackButton.onClick.Invoke();else ui.UrbanResultsView.BackButton.onClick.Invoke();
            Assert.That(app.Snapshot.Screen,Is.EqualTo(ScreenId.MainMenu));
            yield return null;
        }
        static void CaptureGrenadeEvidence(GameObject model)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var cameraObject=new GameObject("GrenadeEvidenceCamera");
            var target=new RenderTexture(256,256,24);var pixels=new Texture2D(256,256,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            var nodes=model.GetComponentsInChildren<Transform>(true);var layers=nodes.Select(n=>n.gameObject.layer).ToArray();
            try
            {
                foreach(var node in nodes)node.gameObject.layer=31;
                var bounds=CombatGrenadeProjectile.GetModelBounds(model);
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.orthographic=true;camera.orthographicSize=bounds.extents.magnitude*1.1f;
                camera.transform.position=bounds.center+new Vector3(1,.4f,-1).normalized;
                camera.transform.LookAt(bounds.center);camera.nearClipPlane=.01f;camera.farClipPlane=3;
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();
                Assert.That(pixels.GetPixels().Count(p=>p.maxColorComponent>.08f),Is.GreaterThan(100),
                    "The actual in-flight M67 renderer must produce visible pixels");
                var path=System.IO.Path.Combine(Application.dataPath,"../Evidence");
                System.IO.Directory.CreateDirectory(path);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(path,"P3-production-M67-in-flight.png"),pixels.EncodeToPNG());
            }
            finally
            {
                for(var i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];
                RenderTexture.active=previous;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(cameraObject);
            }
        }
        static bool CanStandInFront(CombatActorView actor)
        {
            var position=actor.transform.position+actor.transform.forward*.8f+Vector3.up*1.2f;
            return !Physics.CheckSphere(position,.15f,~(1<<2),QueryTriggerInteraction.Ignore);
        }
        IEnumerator Complete(TrainingMode mode)
        {
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            app=GameMain.Instance.Services.Combat;
            ui=GameMain.Instance.GetComponent<P3LiveUIController>().View;
            var button=Object.FindObjectsOfType<Button>(true).Single(b=>b.name=="Button_MainMenu_"+(mode==TrainingMode.Trench?"Trench":"Urban"));
            Assert.That(button.interactable,Is.True);button.onClick.Invoke();
            if(mode==TrainingMode.Trench)ui.TrenchMapView.SelectButton.onClick.Invoke();else ui.UrbanMapView.SelectButton.onClick.Invoke();
            float deadline=Time.realtimeSinceStartup+45;
            while(app.Snapshot.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(app.Snapshot.Error,Is.EqualTo(VRShooting.Contracts.ErrorCode.None));
            runtime=Object.FindObjectOfType<CombatSceneRuntime>();Assert.That(runtime,Is.Not.Null);
            runtime.ManualStepping=true;
            input=new ManualXRTrainingInput();runtime.InputOverride=input;
            if(mode==TrainingMode.Trench)
            {
                Assert.That(runtime.Actors.Count,Is.Zero,"Briefing must not spawn actors");
                ui.TrenchBriefingView.StartButton.onClick.Invoke();
            }
            Assert.That(app.Mission,Is.Not.Null);
            Assert.That(app.Mission.SessionId,Is.Not.Empty);
            yield return CompleteOpening();
            string firstSession=app.Mission.SessionId;
            input.Press(XRTrainingInputButton.RightGrip);input.Press(XRTrainingInputButton.LeftGrip);
            Sample(runtime.PlayerRoot.position,runtime.PlayerRoot.position+Vector3.up*1.4f,Vector3.forward);
            Assert.That(app.Mission.Core.GetSnapshot(firstSession).Data.Weapon.CanShoot,Is.True);
            // Input poses are substituted; targets, cover, door geometry, services and rendering are production.
            var targets=runtime.Actors.Values.Where(a=>a.EntityId.Contains(".enemy-")).ToArray();
            if(mode==TrainingMode.Urban)
            {
                var entry=runtime.Definition.EntranceWorldPosition.Value;
                Place(entry);Sample(entry,entry+Vector3.up*1.4f,Vector3.forward);
                yield return new WaitForSeconds(.15f);
                Assert.That(ui.UrbanStreetView.EnterBuildingButton.interactable,Is.True,"Entry prompt should reflect physical range: "+string.Join(";",app.Mission.Hud.GetHud(firstSession).Data.Prompts.Select(p=>p.PromptId+"="+p.IsEnabled)));
                ui.UrbanStreetView.EnterBuildingButton.onClick.Invoke();
                Assert.That(app.Mission.Urban.GetSession(firstSession).Data.Phase,Is.EqualTo(UrbanPhase.Building));
                var binding=runtime.GetComponent<CombatSceneBindings>();
                foreach(var room in binding.Points.Where(p=>p.Kind==CombatPointKind.Room))
                {
                    var near=room.Door.Hinge.position+room.Door.Hinge.forward;
                    Place(near);Sample(near,near+Vector3.up*1.4f,Vector3.forward);
                    Assert.That(app.Mission.Urban.OpenRoomDoor(firstSession,room.RoomId).Success,Is.True,room.RoomId);
                }
                yield return new WaitForSeconds(.8f);
            }
            foreach(var target in targets)
            {
                var center=target.HitCollider.bounds.center;
                var origin=ClearShotOrigin(target);
                Place(new Vector3(origin.x,target.transform.position.y,origin.z));
                input.Release(XRTrainingInputButton.Trigger);Sample(runtime.PlayerRoot.position,origin,(center-origin).normalized);
                input.Press(XRTrainingInputButton.Trigger);Sample(runtime.PlayerRoot.position,origin,(center-origin).normalized);
                Assert.That(target.IsDead, Is.False, "First bullet must leave one health unit");
                Assert.That(target.DeathAudioFeedbackCount,Is.Zero,"First hit must not play a death scream");
                input.Release(XRTrainingInputButton.Trigger);Sample(runtime.PlayerRoot.position,origin,(center-origin).normalized);
                input.Press(XRTrainingInputButton.Trigger);Sample(runtime.PlayerRoot.position,origin,(center-origin).normalized);
                Physics.Raycast(origin,(center-origin).normalized,out var diagnosticHit,10,~(1<<2),QueryTriggerInteraction.Ignore);
                Assert.That(target.IsDead,Is.True,target.EntityId+" must die from real physics hit; first="+(diagnosticHit.collider!=null?diagnosticHit.collider.name:"none")+"; overlap="+string.Join(",",Physics.OverlapSphere(origin,.01f,~(1<<2),QueryTriggerInteraction.Ignore).Select(c=>c.name))+"; ammo="+app.Mission.Core.GetSnapshot(firstSession).Data.Ammo.CurrentMagazine);
                Assert.That(target.HitFeedbackCount,Is.EqualTo(2));
                Assert.That(target.DeathAudioFeedbackCount,Is.EqualTo(1),"Second physical bullet triggers one death cry");
                Assert.That(target.DeathVoiceSource.isPlaying,Is.True,"Death cry must start with the collapse");
                target.ApplyDead(true);
                Assert.That(target.DeathAudioFeedbackCount,Is.EqualTo(1),"Repeated death snapshots must not replay the cry");
            }
            input.Release(XRTrainingInputButton.Trigger);
            if(mode==TrainingMode.Trench)
            {
                foreach(var node in runtime.Definition.SearchNodes)
                {Place(node.WorldPosition);Sample(node.WorldPosition,node.WorldPosition+Vector3.up*1.4f,Vector3.forward);}
            }
            else
            {
                foreach(var room in runtime.GetComponent<CombatSceneBindings>().Points.Where(p=>p.Kind==CombatPointKind.Room))
                {
                    Place(room.transform.position);Sample(room.transform.position,room.transform.position+Vector3.up*1.4f,Vector3.forward);
                    Assert.That(runtime.Interact().Success,Is.True,room.RoomId);
                }
            }
            Assert.That(app.Snapshot.Summary.HasValue,Is.True);
            Assert.That(app.Snapshot.Summary.Value.Victory,Is.True);
            Assert.That(ui.VisibleScreen,Is.EqualTo(mode==TrainingMode.Trench?ScreenId.TrenchResults:ScreenId.UrbanResults));
            Assert.That(targets.All(t=>t!=null&&t.IsDead),Is.True,"Corpses retained until retry");
            for(int retry=0;retry<1;retry++)
            {
                if(mode==TrainingMode.Trench)ui.TrenchResultsView.RetryButton.onClick.Invoke();else ui.UrbanResultsView.RetryButton.onClick.Invoke();
                if(mode==TrainingMode.Trench)ui.TrenchBriefingView.StartButton.onClick.Invoke();
                yield return CompleteOpening();
                Assert.That(app.Mission.SessionId,Is.Not.EqualTo(firstSession));
                Assert.That(app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.Ammo.CurrentMagazine,Is.EqualTo(30));
                Assert.That(runtime.Actors.Values.All(a=>!a.IsDead),Is.True);
            }
            Assert.That(app.ReturnToMainMenu().Success,Is.True);
            yield return null;yield return null;
            Assert.That(app.Mission,Is.Null);Assert.That(ui.gameObject.activeSelf,Is.False);
            Assert.That(Object.FindObjectOfType<CombatSceneRuntime>(),Is.Null);
        }
        void Place(Vector3 position)
        {
            var body=runtime.PlayerRoot.GetComponent<CharacterController>();body.enabled=false;
            runtime.PlayerRoot.position=position+Vector3.up*.06f;body.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator CompleteOpening()
        {
            // BDD28/task019: exercise the production flight and application commit, never skip recon.
            if(app.Mission.Trench==null)yield break;
            Assert.That(app.Snapshot.Screen,Is.EqualTo(ScreenId.TrenchDroneRecon));
            Assert.That(runtime.CharacterActionsLocked,Is.True);
            input.Clear();
            runtime.FrameOverride=new CombatInputFrameDto {HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,
                PlayerPosition=runtime.PlayerRoot.position,PlayerForward=runtime.PlayerRoot.forward};
            for(int frame=0;frame<1200&&!app.Mission.HasCombatStarted;frame++)
            {
                runtime.Step(.1f);
                Assert.That(runtime.LastFrameResult.Success,Is.True,app.Mission.Opening?.Phase+": "+runtime.LastFrameResult.Message);
                if(frame%20==0)yield return null;
            }
            Assert.That(app.Mission.HasCombatStarted,Is.True,"Production recon must reach combat");
            Assert.That(runtime.CharacterActionsLocked,Is.False);
            Assert.That(app.Snapshot.Screen,Is.EqualTo(ScreenId.TrenchHud));
            // Release every control before the next gameplay command.
            runtime.Step(.02f);input.AdvanceFrame();
            yield return null;
        }
        static Vector3 ClearShotOrigin(CombatActorView target)
        {
            var center=target.HitCollider.bounds.center;
            for(int i=0;i<16;i++)
            {
                var origin=center+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*1.2f;
                if(Physics.CheckSphere(origin,.12f,~(1<<2),QueryTriggerInteraction.Ignore))continue;
                if(Physics.Raycast(origin,center-origin,out var hit,2,~(1<<2),QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CombatActorView>()==target)return origin;
            }
            Assert.Fail("No unobstructed shot position around "+target.EntityId);return center;
        }
        void Sample(Vector3 position,Vector3 muzzle,Vector3 direction)
        {
            runtime.FrameOverride=new CombatInputFrameDto{HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,RearGripInRange=true,FrontGripInRange=true,
                MuzzlePosition=muzzle,AimDirection=direction,PlayerPosition=position,PlayerForward=Vector3.forward};
            runtime.Step(.02f);Assert.That(runtime.LastFrameResult.Success,Is.True,runtime.LastFrameResult.ErrorCode+": "+runtime.LastFrameResult.Message);input.AdvanceFrame();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            app?.ReturnToMainMenu();
            if(GameMain.Instance!=null)Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            foreach(var display in suspendedDisplays)display.Start();suspendedDisplays.Clear();
        }
    }
}
