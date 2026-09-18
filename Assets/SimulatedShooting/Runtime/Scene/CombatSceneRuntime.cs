using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using CommonUsages=UnityEngine.XR.CommonUsages;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Combat;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Scene
{
    public sealed class CombatSceneClock:ICombatClock
    {
        public double Now{get;private set;}
        public long Tick{get;private set;}
        public void Step(float seconds){Now+=seconds;Tick++;}
    }

    // Owns physical facts and visuals; all damage, ammo, search and outcome rules remain in services.
    public sealed class CombatSceneRuntime:MonoBehaviour,ICombatNavigationPort
    {
        CombatSceneBindings binding;
        TrainingMode mode;
        readonly List<GameObject> hiddenMainRoots=new List<GameObject>();
        readonly Dictionary<string,CombatActorView> actors=new Dictionary<string,CombatActorView>();
        readonly Dictionary<string,CombatNavigationRequestDto> navigation=new Dictionary<string,CombatNavigationRequestDto>();
        readonly Queue<(CombatNavigationRequestDto request,bool arrived,Vector3 position,Vector3 forward)> navigationResults=new Queue<(CombatNavigationRequestDto,bool,Vector3,Vector3)>();
        readonly Dictionary<string,bool> presence=new Dictionary<string,bool>();
        readonly P3XRTrainingInput hardware=new P3XRTrainingInput();
        ICombatCoreService core;
        ICombatWorldInputPort world;
        ICombatStateService state;
        ICombatTickPort tick;
        CombatInputController controller;
        GameObject rifle;
        WeaponPrefabBinding weapon;
        TrainingRifleGrabInteractable grab;
        XRInteractionManager interactionManager;
        Transform player;
        Camera eye;
        CharacterController body;
        GameObject xrOrigin;
        Camera desktopEye;
        VRShooting.Unity.Player.PlayerFollowCamera mainFollowCamera;
        bool restoreMainFollowOutput;
        string session="";
        bool active,vr;
        float pitch,recoil;
        long sequence;
        WeaponFeedbackController weaponFeedback;
        readonly List<Sprite> mapSprites=new List<Sprite>();
        bool wasTracked=true;
        public CombatSceneClock Clock {get;}=new CombatSceneClock();
        public CombatSceneDefinitionDto Definition{get;private set;}
        public IXRTrainingInput InputOverride{get;set;}
        public CombatInputFrameDto? FrameOverride{get;set;}
        public bool ManualStepping{get;set;}
        public Transform PlayerRoot=>player;
        public Camera PlayerCamera=>eye;
        public IReadOnlyDictionary<string,CombatActorView> Actors=>actors;
        public string SessionId=>session;
        public bool IsVr=>vr;
        public ServiceResult<Unit> LastFrameResult {get;private set;}

        public void Prepare(CombatSceneBindings bindings,TrainingMode selected,bool? vrAvailability=null)
        {
            binding=bindings;mode=selected;
            foreach(var fixture in GetComponentsInChildren<CombatSceneFixture>(true))fixture.enabled=false;
            var xrMode=binding.GetComponent<ZeroingRangeXRModeController>();
            xrOrigin=xrMode.XrOrigin;desktopEye=xrMode.NoVrCamera;xrMode.enabled=false;
            foreach(var walker in GetComponentsInChildren<CombatSceneWalker>(true))walker.enabled=false;
            var displays=new List<XRDisplaySubsystem>();SubsystemManager.GetInstances(displays);vr=vrAvailability??displays.Any(d=>d.running);
            // Both rigs reference the same XRI action asset. Disable the departing rig
            // before enabling the arriving rig so its OnDisable cannot cancel live input.
            xrOrigin.SetActive(false);desktopEye.gameObject.SetActive(!vr);
            desktopEye.transform.parent.gameObject.SetActive(!vr);
            desktopEye.enabled=!vr;
            var desktopListener=desktopEye.GetComponent<AudioListener>();if(desktopListener!=null)desktopListener.enabled=!vr;
            eye=vr?xrOrigin.GetComponentsInChildren<Camera>(true).First():desktopEye;
            eye.enabled=true;
            player=vr?xrOrigin.transform:desktopEye.transform.parent;
            body=player.GetComponent<CharacterController>()??player.gameObject.AddComponent<CharacterController>();
            player.gameObject.layer=2;
            body.radius=.22f;body.height=1.7f;body.center=new Vector3(0,.85f,0);body.stepOffset=.25f;body.slopeLimit=50;
            foreach(var behaviour in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour!=null && behaviour.GetType().Namespace?.Contains("Locomotion")==true)behaviour.enabled=false;
            var main=UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainScene");
            if(main.IsValid())foreach(var root in main.GetRootGameObjects())
                if(root.activeSelf){hiddenMainRoots.Add(root);root.SetActive(false);}
            mainFollowCamera=VRShooting.Unity.Player.PlayerFollowCamera.Instance;
            if(mainFollowCamera!=null){restoreMainFollowOutput=mainFollowCamera.OutputEnabled;mainFollowCamera.SetOutputEnabled(false);}
            interactionManager=GetComponent<XRInteractionManager>()??gameObject.AddComponent<XRInteractionManager>();
            interactionManager.enabled=true;
            foreach(var manager in xrOrigin.GetComponentsInChildren<XRInteractionManager>(true))manager.enabled=false;
            foreach(var group in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRInteractionGroup>(true))group.interactionManager=interactionManager;
            foreach(var interactor in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))interactor.interactionManager=interactionManager;
            // Bullets ignore the held-rifle layer, but near-hand overlap queries must still find it.
            foreach(var caster in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.SphereInteractionCaster>(true))
                caster.physicsLayerMask=caster.physicsLayerMask.value|(1<<2);
            xrOrigin.SetActive(vr);
            Definition=CombatSceneDefinitionBuilder.Build(binding,mode);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(gameObject.scene);
            var liveUi=VRShooting.Unity.Bootstrap.GameMain.Instance?.GetComponent<VRShooting.Unity.UI.P3LiveUIController>();
            if(liveUi!=null)
            {
                liveUi.View.GetComponent<VRShooting.Unity.UI.TrainingUICanvasAdapter>().SetMode(vr,eye);
                foreach(var map in binding.Maps)
                {
                    var sprite=Sprite.Create(map.Plan,new Rect(0,0,map.Plan.width,map.Plan.height),Vector2.one*.5f);mapSprites.Add(sprite);
                    foreach(var mini in liveUi.View.GetComponentsInChildren<VRShooting.Unity.UI.P3MiniMapView>(true))
                    {mini.SetMapResource(map.Id,sprite);if(map.Id.EndsWith("f"))mini.SetMapResource("floor-"+map.Id[6],sprite);}
                }
            }
            binding.WeaponAnchor.gameObject.SetActive(false);
            ResetPlayer();
        }
        void ResetPlayer()
        {
            body.enabled=false;
            var spawn=mode==TrainingMode.Trench?binding.TrenchEntry:binding.UrbanEntry;
            player.SetPositionAndRotation(spawn.position+Vector3.up*.06f,spawn.rotation);
            body.enabled=true;
            if(!vr){eye.transform.localPosition=new Vector3(0,1.65f,0);eye.transform.localRotation=Quaternion.identity;}
            pitch=0;
        }
        public ServiceResult<Unit> Activate(ICombatCoreService combat,ICombatWorldInputPort facts,ICombatStateService visual,ICombatTickPort advance,string id)
        {
            Deactivate();
            if(binding.TrainingRiflePrefab==null)return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable,"Training rifle binding missing");
            core=combat;world=facts;state=visual;tick=advance;session=id;ResetPlayer();
            foreach(var door in binding.Doors)door.ResetView();
            rifle=Instantiate(binding.TrainingRiflePrefab,transform);
            weapon=rifle.GetComponent<WeaponPrefabBinding>();grab=rifle.GetComponent<TrainingRifleGrabInteractable>();
            var spawn=player.position;
            rifle.transform.SetPositionAndRotation(spawn+player.forward*.55f+Vector3.up*1.05f,player.rotation);
            if(grab!=null){grab.interactionManager=interactionManager;grab.enabled=vr;grab.CaptureRackPose();}
            SetLayer(rifle,2); // IgnoreRaycast excludes the held rifle from its own physical shots.
            weaponFeedback=rifle.AddComponent<WeaponFeedbackController>();
            weaponFeedback.Configure(weapon.MuzzlePoint,rifle.transform,weapon.MuzzlePoint,grab,null,binding.RifleShotClip,null,null,new[]{binding.EnemyPrefab.HitClip});
            hardware.Reset();
            state.VisualChanged+=ApplyVisual;core.Feedback+=Feedback;
            var first=state.GetVisualSnapshot(session);if(first.Success)ApplyVisual(first.Data);
            controller=new CombatInputController(session,mode,core,new InputRouter(this),Clock,
                new CombatPhysicsShotQuery(~(1<<2),~(1<<2)),new CombatCharacterLocomotion(body,vr?null:eye.transform));
            active=true;presence.Clear();navigation.Clear();navigationResults.Clear();
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        void Update(){if(active&&!ManualStepping)Step(Time.deltaTime);}
        public void Step(float delta)
        {
            if(!active)return;
            Clock.Step(delta);
            var snapshot=core.GetSnapshot(session);if(!snapshot.Success)return;
            if(snapshot.Data.State!=SessionState.Running)
            {
                foreach(var actor in actors.Values)if(actor.Agent.enabled&&actor.Agent.isOnNavMesh)actor.Agent.isStopped=true;
                return;
            }
            hardware.Sample(vr);
            var input=InputOverride??hardware;
            if(!vr&&InputOverride==null)
            {
                if(input.RightGripHeld)
                {
                    var mouse=Mouse.current?.delta.ReadValue()??Vector2.zero;
                    player.Rotate(0,mouse.x*.12f,0);pitch=Mathf.Clamp(pitch-mouse.y*.12f,-75,75);
                    eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
                    rifle.transform.SetPositionAndRotation(eye.transform.position+eye.transform.right*(snapshot.Data.Player.Shoulder==ShoulderSide.Left?-.13f:.13f)-eye.transform.up*.17f+eye.transform.forward*.28f,eye.transform.rotation);
                }
            }
            recoil=Mathf.MoveTowards(recoil,0,delta*20);
            if(weapon!=null)weapon.RecoilRoot.localRotation=Quaternion.Euler(-recoil,0,0);
            var frame=FrameOverride??Frame(snapshot.Data,input);
            bool tracked=frame.HeadTracked&&frame.RearHandTracked&&frame.FrontHandTracked;
            if(!tracked||tracked!=wasTracked){presence.Clear();navigation.Clear();navigationResults.Clear();}
            wasTracked=tracked;
            foreach(var actor in actors.Values)
                if(actor.Agent.enabled&&actor.Agent.isOnNavMesh)actor.Agent.isStopped=!tracked;
            core.SetTracking(session,tracked);
            if(tracked)
            {
                CollectPresence(frame.PlayerPosition);
                while(navigationResults.Count>0)
                {
                    var n=navigationResults.Dequeue();
                    world.Submit(new CombatInputDto{SessionId=session,EventId="navigation-"+ ++sequence,Tick=Clock.Tick,Kind=CombatInputKind.NavigationResult,
                        EntityId=n.request.EntityId,TargetId=n.request.RequestId,Position=n.position,Direction=n.forward,Flag=n.arrived});
                }
                CollectPerception(frame.PlayerPosition);
            }
            LastFrameResult=controller.Step(frame,delta);
            if(!LastFrameResult.Success)return;
            LastFrameResult=tick.Advance();
            if(tracked&&input.ConfirmPressed)Interact();
            if(input.BackPressed)VRShooting.Unity.Bootstrap.GameMain.Instance?.Services.Combat.BackToMaps();
        }
        CombatInputFrameDto Frame(CombatCoreSnapshotDto snapshot,IXRTrainingInput input)
        {
            var head=InputDevices.GetDeviceAtXRNode(XRNode.Head);var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool rightTracked=!vr||Tracked(right),leftTracked=!vr||Tracked(left),headTracked=!vr||Tracked(head);
            var r=HandPosition(right);var l=HandPosition(left);
            bool rear=!vr||grab.RearHandSelected||Vector3.Distance(r,weapon.RearHandGrip.position)<grab.RearGrabRadius;
            bool front=!vr||grab.FrontHandSelected||Vector3.Distance(l,weapon.FrontHandGrip.position)<grab.FrontGrabRadius;
            return new CombatInputFrameDto{HeadTracked=headTracked,RearHandTracked=rightTracked,FrontHandTracked=leftTracked,RearGripInRange=rear,FrontGripInRange=front,
                IsRealVr=vr,MuzzlePosition=weapon.MuzzlePoint.position,AimDirection=weapon.AimLinePoint.forward,PlayerPosition=player.position,PlayerForward=player.forward,
                RequestedPosture=hardware.PosturePressed?(PlayerPosture?)((PlayerPosture)(((int)snapshot.Player.Posture+1)%3)):null};
        }
        Vector3 HandPosition(UnityEngine.XR.InputDevice device)
        {
            device.TryGetFeatureValue(CommonUsages.devicePosition,out var position);
            var offset=eye.transform.parent;
            return offset.TransformPoint(position);
        }
        static bool Tracked(UnityEngine.XR.InputDevice device)=>device.isValid&&device.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked)&&tracked;
        void CollectPresence(Vector3 position)
        {
            bool corner=false;
            foreach(var point in binding.Points)
            {
                if(point.RegionId!=Definition.MapId)continue;
                var p=position+Vector3.up;
                bool inside=point.Volume!=null&&point.Volume.bounds.Contains(p);
                if(point.Kind==CombatPointKind.Corner)corner|=inside;
                if(point.Kind==CombatPointKind.SearchNode)Presence(point.Id,inside);
                if(mode!=TrainingMode.Urban)continue;
                if(point.Kind==CombatPointKind.Entrance)Presence(Definition.EntranceId,inside);
                if(point.Kind==CombatPointKind.Floor)
                {
                    // Floor facts cover the authored floor rectangle at its actual vertical level.
                    var projection=Definition.Projections.First(f=>f.FloorId==point.FloorId);
                    Presence(point.FloorId,Mathf.Abs(position.y-point.transform.position.y)<1.5f&&projection.WorldBoundsXZ.Contains(new Vector2(position.x,position.z)));
                }
                if(point.Kind==CombatPointKind.Room)
                {
                    Presence(point.RoomId+".check",inside);
                    var door=point.Door.Hinge.position+point.Door.Hinge.forward;
                    Presence(point.RoomId+".door",Vector3.Distance(position,door)<3f && Mathf.Abs(position.y-point.transform.position.y)<1.2f);
                }
            }
            core.SetCornerAvailable(session,corner);
        }
        void Presence(string id,bool inside)
        {
            if(presence.TryGetValue(id,out var old)&&old==inside)return;
            var accepted=world.Submit(new CombatInputDto{SessionId=session,EventId="area-"+ ++sequence,Tick=Clock.Tick,Kind=CombatInputKind.AreaPresence,EntityId=id,Flag=inside});
            if(accepted.Success)presence[id]=inside;
        }
        void CollectPerception(Vector3 playerPosition)
        {
            var snapshot=core.GetSnapshot(session).Data.Visual;
            foreach(var entity in snapshot.Entities)
            {
                if(entity.Role!=CombatEntityRole.Enemy||entity.State==CombatEntityState.Dead||!actors.TryGetValue(entity.EntityId,out var actor))continue;
                var direction=playerPosition+Vector3.up*1.2f-actor.PerceptionOrigin.position;
                bool visible=!Physics.Raycast(actor.PerceptionOrigin.position,direction.normalized,out var hit,Mathf.Max(0,direction.magnitude-.25f),~(1<<2),QueryTriggerInteraction.Ignore);
                if(mode==TrainingMode.Urban && world is UrbanService urban)
                {
                    var assignment=urban.GetEnemyAssignments(session).Data.First(a=>a.EntityId==entity.EntityId);
                    if(assignment.RoomId.Length>0)
                    {var door=binding.Doors.First(d=>d.RoomId==assignment.RoomId);visible&=door.IsOpen;}
                }
                world.Submit(new CombatInputDto{SessionId=session,EventId="perception-"+ ++sequence,Tick=Clock.Tick,Kind=CombatInputKind.Perception,
                    EntityId=entity.EntityId,TargetId=session+".player",Position=actor.transform.position,Direction=actor.transform.forward,Flag=visible});
            }
        }
        public ServiceResult<Unit> Interact()
        {
            if(!(world is UrbanService urban))return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            var current=urban.GetSession(session);if(!current.Success)return ServiceResult<Unit>.Fail(current.ErrorCode);
            if(presence.TryGetValue(Definition.EntranceId,out var atEntry)&&atEntry)
            {
                var result=current.Data.Phase==UrbanPhase.Street?urban.EnterBuilding(session,Definition.EntranceId):urban.ExitBuilding(session,Definition.EntranceId);
                return result.Success?ServiceResult<Unit>.Ok(Unit.Value):ServiceResult<Unit>.Fail(result.ErrorCode);
            }
            var room=binding.Points.Where(p=>p.Kind==CombatPointKind.Room).OrderBy(p=>Vector3.Distance(player.position,p.transform.position)).FirstOrDefault(p=>
                presence.TryGetValue(p.RoomId+(p.Door.IsOpen?".check":".door"),out var present)&&present);
            if(room==null)return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            var action=room.Door.IsOpen?urban.MarkRoomSearched(session,room.RoomId):urban.OpenRoomDoor(session,room.RoomId);
            tick.Advance();
            return action.Success?ServiceResult<Unit>.Ok(Unit.Value):ServiceResult<Unit>.Fail(action.ErrorCode);
        }
        void ApplyVisual(CombatVisualSnapshotDto snapshot)
        {
            if(snapshot.SessionId!=session)return;
            foreach(var entity in snapshot.Entities)
            {
                if(entity.Role==CombatEntityRole.Player)continue;
                if(!actors.TryGetValue(entity.EntityId,out var actor))
                {
                    var at=entity.Position;
                    actor=Instantiate(entity.Role==CombatEntityRole.Enemy?binding.EnemyPrefab:binding.TeammatePrefab,at,Quaternion.LookRotation(entity.Forward),transform);
                    actor.EntityId=entity.EntityId;actor.gameObject.AddComponent<CombatEntityBinding>().EntityId=entity.EntityId;
                    actor.NavigationReported+=NavigationReported;actors.Add(entity.EntityId,actor);
                    if(entity.Role==CombatEntityRole.Enemy)actor.Agent.enabled=false;
                    foreach(var c in actor.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(body,c);
                }
                bool dead=entity.State==CombatEntityState.Dead;
                if(dead!=actor.IsDead)actor.ApplyDead(dead);
            }
            foreach(var door in snapshot.Doors)
            {var target=binding.Doors.FirstOrDefault(d=>d.RoomId==door.RoomId);if(target!=null&&target.IsOpen!=door.Open)target.Apply(session+"-door-"+snapshot.Revision+"-"+door.RoomId,door.Open);}
        }
        public ServiceResult<Unit> Move(CombatNavigationRequestDto request)
        {
            if(!active||request.SessionId!=session||!actors.TryGetValue(request.EntityId,out var actor))return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable);
            navigation[request.EntityId]=request;actor.MoveTo(request.Destination,Quaternion.LookRotation(request.Forward));
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        void NavigationReported(string id,bool arrived)
        {
            if(navigation.TryGetValue(id,out var request)&&actors.TryGetValue(id,out var actor))
            {navigationResults.Enqueue((request,arrived,actor.transform.position,actor.transform.forward));navigation.Remove(id);}
        }
        void Feedback(CombatFeedbackDto feedback)
        {
            if(feedback.SessionId!=session)return;
            if(feedback.Kind==CombatFeedbackKind.ShotFired)
            {
                recoil=2;
                if(weaponFeedback!=null)
                {
                    var origin=FrameOverride?.MuzzlePosition??weapon.MuzzlePoint.position;
                    var direction=FrameOverride?.AimDirection??weapon.AimLinePoint.forward;
                    var hit=Physics.Raycast(origin,direction,out var contact,100,~(1<<2),QueryTriggerInteraction.Ignore);
                    weaponFeedback.PlayValidShot((int)++sequence,origin,hit?contact.point:origin+direction*100,hit,contact.point,contact.normal);
                }
                if(vr) {InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0,.35f,.08f);InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0,.2f,.06f);}
            }
            if(actors.TryGetValue(feedback.Kind==CombatFeedbackKind.EnemyHit?feedback.TargetId:feedback.EntityId,out var actor))
            {
                if(feedback.Kind==CombatFeedbackKind.EnemyHit)actor.PlayHit(feedback.EventId);
                if(feedback.Kind==CombatFeedbackKind.EnemyAttack)actor.PlayShot(feedback.EventId);
            }
        }
        public void Deactivate()
        {
            active=false;
            if(state!=null)state.VisualChanged-=ApplyVisual;if(core!=null)core.Feedback-=Feedback;
            controller?.Dispose();controller=null;
            foreach(var actor in actors.Values)if(actor!=null){actor.NavigationReported-=NavigationReported;actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
            actors.Clear();navigation.Clear();navigationResults.Clear();presence.Clear();
            if(rifle!=null){if(grab!=null)grab.ResetToRack();rifle.SetActive(false);Destroy(rifle);}
            rifle=null;weapon=null;grab=null;core=null;world=null;state=null;tick=null;session="";
        }
        public void RestoreMainScene()
        {
            if(hiddenMainRoots.Count==0)return;
            if(interactionManager!=null)interactionManager.enabled=false;
            // Do this before reactivating MainScene, including the asynchronous unload path.
            if(xrOrigin!=null)xrOrigin.SetActive(false);
            if(eye!=null){eye.enabled=false;var listener=eye.GetComponent<AudioListener>();if(listener!=null)listener.enabled=false;}
            if(mainFollowCamera!=null){mainFollowCamera.SetOutputEnabled(restoreMainFollowOutput);mainFollowCamera=null;}
            foreach(var root in hiddenMainRoots)if(root!=null)root.SetActive(true);hiddenMainRoots.Clear();
            var main=UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainScene");
            if(main.IsValid()&&main.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
            var liveUi=VRShooting.Unity.Bootstrap.GameMain.Instance?.GetComponent<VRShooting.Unity.UI.P3LiveUIController>();
            if(liveUi!=null)liveUi.View.GetComponent<VRShooting.Unity.UI.TrainingUICanvasAdapter>().ClearForcedModeForTests();
        }
        void OnDestroy(){Deactivate();RestoreMainScene();foreach(var sprite in mapSprites)if(sprite!=null)Destroy(sprite);mapSprites.Clear();}
        static void SetLayer(GameObject root,int layer){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
        sealed class InputRouter:IXRTrainingInput
        {
            readonly CombatSceneRuntime owner;
            IXRTrainingInput Input=>owner.InputOverride??owner.hardware;
            public InputRouter(CombatSceneRuntime owner){this.owner=owner;}
            public bool ConfirmPressed=>Input.ConfirmPressed;
            public bool BackPressed=>Input.BackPressed;
            public bool TriggerPressed=>Input.TriggerPressed;
            public bool TriggerHeld=>Input.TriggerHeld;
            public bool TriggerReleased=>Input.TriggerReleased;
            public float RightTriggerValue=>Input.RightTriggerValue;
            public bool RightGripPressed=>Input.RightGripPressed;
            public bool RightGripHeld=>Input.RightGripHeld;
            public bool RightGripReleased=>Input.RightGripReleased;
            public bool LeftGripPressed=>Input.LeftGripPressed;
            public bool LeftGripHeld=>Input.LeftGripHeld;
            public bool LeftGripReleased=>Input.LeftGripReleased;
            public bool ReloadPressed=>Input.ReloadPressed;
            public bool SwitchShoulderPressed=>Input.SwitchShoulderPressed;
            public bool AimPressed=>Input.AimPressed;
            public bool AimHeld=>Input.AimHeld;
            public bool CommandMenuHeld=>Input.CommandMenuHeld;
            public Vector2 TurnAxis=>Input.TurnAxis;
            public Vector2 MoveAxis=>Input.MoveAxis;
        }
    }
}
