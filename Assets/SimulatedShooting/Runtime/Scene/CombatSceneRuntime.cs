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
    public sealed class CombatSceneRuntime:MonoBehaviour,ICombatNavigationPort,ICombatGrenadeWorld
    {
        CombatSceneBindings binding;
        TrainingMode mode;
        readonly List<GameObject> hiddenMainRoots=new List<GameObject>();
        readonly Dictionary<string,CombatActorView> actors=new Dictionary<string,CombatActorView>();
        readonly Dictionary<string,CombatNavigationRequestDto> navigation=new Dictionary<string,CombatNavigationRequestDto>();
        readonly Queue<(CombatNavigationRequestDto request,bool arrived,Vector3 position,Vector3 forward)> navigationResults=new Queue<(CombatNavigationRequestDto,bool,Vector3,Vector3)>();
        readonly Dictionary<string,bool> presence=new Dictionary<string,bool>();
        readonly Dictionary<string,CombatGrenadeProjectile> grenadesInFlight=new Dictionary<string,CombatGrenadeProjectile>();
        readonly P3XRTrainingInput hardware=new P3XRTrainingInput();
        ICombatCoreService core;
        ICombatWorldInputPort world;
        ICombatStateService state;
        ICombatTickPort tick;
        ICombatGrenadeTacticService grenades;
        Material grenadeMaterial;
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
        bool characterActionsLocked;
        bool awaitingInputRelease;
        Mode3DroneReconScene droneRecon;
        public Mode3DroneReconScene DroneRecon => droneRecon;
        public bool CharacterActionsLocked => characterActionsLocked;
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
            body.radius=.22f;body.height=StandingEyeHeight()+.10f;body.center=new Vector3(0,body.height*.5f,0);body.stepOffset=.25f;body.slopeLimit=50;
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
            ResetPlayer();
            Definition=CombatSceneDefinitionBuilder.Build(binding,mode,
                new Vector3(eye.transform.position.x,player.position.y,eye.transform.position.z));
            if(mode==TrainingMode.Trench)
            {
                droneRecon=GetComponent<Mode3DroneReconScene>()??gameObject.AddComponent<Mode3DroneReconScene>();
                droneRecon.Configure(this,binding);
            }
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
            binding.WeaponAnchor.gameObject.SetActive(mode==TrainingMode.Trench);
        }
        void ResetPlayer()
        {
            body.enabled=false;
            var spawn=mode==TrainingMode.Trench?binding.TrenchEntry:binding.UrbanEntry;
            var standing=CombatSpawnPlacement.Resolve(binding.GeometryRoot,spawn.position,body.radius,body.height);
            player.SetPositionAndRotation(standing,spawn.rotation);
            if(vr)
            {
                var origin=player.GetComponent<Unity.XR.CoreUtils.XROrigin>();
                if(origin!=null&&origin.CurrentTrackingOriginMode==UnityEngine.XR.TrackingOriginModeFlags.Device)
                    origin.CameraYOffset=1.65f;
                if(origin!=null&&origin.CameraFloorOffsetObject!=null)
                {
                    var calibration=player.GetComponent<CombatStandingEyeCalibration>()??player.gameObject.AddComponent<CombatStandingEyeCalibration>();
                    calibration.Configure(eye,origin.CameraFloorOffsetObject.transform,StandingEyeHeight()*.75f,()=>PlaceCombatUi(standing.y));
                }
                var headOffset=Vector3.ProjectOnPlane(eye.transform.position-player.position,Vector3.up);
                player.position-=headOffset;
                var localHead=player.InverseTransformPoint(eye.transform.position);
                body.center=new Vector3(localHead.x,body.height*.5f,localHead.z);
            }
            body.enabled=true;
            if(!vr){eye.transform.localPosition=new Vector3(0,StandingEyeHeight()*.75f,0);eye.transform.localRotation=Quaternion.identity;}
            pitch=0;
            PlaceCombatUi(standing.y);
        }
        float StandingEyeHeight()
        {
            if(binding.TeammatePrefab==null)return 1.85f;
            var heads=binding.TeammatePrefab.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Head").ToArray();
            var boots=binding.TeammatePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.EndsWith("_Boots",StringComparison.Ordinal)).ToArray();
            if(heads.Length==0||boots.Length==0)return 1.85f;
            return Mathf.Clamp(heads.Max(t=>t.position.y)-boots.Min(r=>r.bounds.min.y),1.5f,2.1f);
        }
        void PlaceCombatUi(float floorHeight)
        {
            var ui=VRShooting.Unity.Bootstrap.GameMain.Instance?.GetComponent<VRShooting.Unity.UI.P3LiveUIController>();
            if(ui!=null)
            {
                var adapter=ui.View.GetComponent<VRShooting.Unity.UI.TrainingUICanvasAdapter>();
                adapter.ConfigureVrLayout(1.6f,.0008f,floorHeight+.10f);
                adapter.SetMode(vr,eye);
            }
        }
        public ServiceResult<Unit> Activate(ICombatCoreService combat,ICombatWorldInputPort facts,ICombatStateService visual,ICombatGrenadeTacticService grenadeTactics,ICombatTickPort advance,string id)
        {
            Deactivate();
            if(binding.TrainingRiflePrefab==null)return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable,"Training rifle binding missing");
            if(grenadeTactics!=null&&binding.GrenadePrefab==null&&Resources.Load<GameObject>("Combat/Grenade_M67")==null&&Resources.Load<GameObject>("Combat/m67_low")==null)
                return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable,"M67 grenade model missing");
            core=combat;world=facts;state=visual;grenades=grenadeTactics;tick=advance;session=id;ResetPlayer();
            characterActionsLocked=core.GetSnapshot(id).Data.State==SessionState.Preparing;
            foreach(var door in binding.Doors)door.ResetView();
            if(mode==TrainingMode.Trench)binding.WeaponAnchor.gameObject.SetActive(false);
            rifle=Instantiate(binding.TrainingRiflePrefab,transform);
            weapon=rifle.GetComponent<WeaponPrefabBinding>();grab=rifle.GetComponent<TrainingRifleGrabInteractable>();
            var spawn=player.position;
            var standingFeet=new Vector3(eye.transform.position.x,player.position.y,eye.transform.position.z);
            var correctedRack=binding.WeaponAnchor.position+standingFeet-binding.TrenchEntry.position;
            rifle.transform.SetPositionAndRotation(mode==TrainingMode.Trench?correctedRack:spawn+player.forward*.55f+Vector3.up*1.05f,
                mode==TrainingMode.Trench?binding.WeaponAnchor.rotation:player.rotation);
            if(grab!=null){grab.interactionManager=interactionManager;grab.enabled=vr&&!characterActionsLocked;grab.CaptureRackPose();}
            SetLayer(rifle,2); // IgnoreRaycast excludes the held rifle from its own physical shots.
            // The spawned rifle starts close to the player capsule. It must not push the
            // player backwards or block continuous movement while being held in VR.
            foreach(var collider in rifle.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(body,collider,true);
            weaponFeedback=rifle.AddComponent<WeaponFeedbackController>();
            weaponFeedback.Configure(weapon.MuzzlePoint,rifle.transform,weapon.MuzzlePoint,grab,null,binding.RifleShotClip,null,null,null);
            hardware.Reset();
            state.VisualChanged+=ApplyVisual;core.Feedback+=Feedback;
            if(grenades!=null){grenades.GrenadeThrown+=OnGrenadeThrown;grenades.GrenadeExploded+=OnGrenadeExploded;grenades.GrenadeCancelled+=OnGrenadeCancelled;}
            if(grenades!=null)grenades.Configure(this);
            var first=state.GetVisualSnapshot(session);if(first.Success)ApplyVisual(first.Data);
            controller=new CombatInputController(session,mode,core,new InputRouter(this),Clock,
                new CombatPhysicsShotQuery(~(1<<2),~(1<<2)),new CombatCharacterLocomotion(body,vr?null:eye.transform));
            active=true;presence.Clear();navigation.Clear();navigationResults.Clear();
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        void Update(){if(active&&!ManualStepping)Step(Time.deltaTime);}
        public ServiceResult<Unit> SetCharacterActionsLocked(string id,bool locked)
        {
            if(!active||id!=session)return ServiceResult<Unit>.Fail(ErrorCode.NotFound);
            if(!locked&&core.GetSnapshot(id).Data.State!=SessionState.Running)return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            if(characterActionsLocked&&!locked){awaitingInputRelease=true;hardware.ClearOpeningToggles();}
            characterActionsLocked=locked;
            foreach(var actor in actors.Values)actor.SetActionsLocked(locked);
            if(grab!=null)grab.enabled=vr&&!locked&&!awaitingInputRelease;
            presence.Clear();navigation.Clear();navigationResults.Clear();
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        public void Step(float delta)
        {
            if(!active)return;
            Clock.Step(delta);
            var snapshot=core.GetSnapshot(session);if(!snapshot.Success)return;
            if(snapshot.Data.State!=SessionState.Running||characterActionsLocked)
            {
                foreach(var actor in actors.Values)if(actor.Agent.enabled&&actor.Agent.isOnNavMesh)actor.Agent.isStopped=true;
                if(snapshot.Data.State==SessionState.Preparing||characterActionsLocked&&snapshot.Data.State==SessionState.Running)
                {
                    hardware.Sample(vr);
                    var preparingInput=InputOverride??hardware;
                    var preparingFrame=FrameOverride??Frame(snapshot.Data,preparingInput);
                    core.SetTracking(session,preparingFrame.HeadTracked&&preparingFrame.RearHandTracked&&preparingFrame.FrontHandTracked);
                    LastFrameResult=tick.Advance();
                    droneRecon?.AdvanceFlight(delta);
                }
                return;
            }
            hardware.Sample(vr);
            var input=InputOverride??hardware;
            if(awaitingInputRelease)
            {
                var releaseFrame=FrameOverride??Frame(snapshot.Data,input);
                core.SetTracking(session,releaseFrame.HeadTracked&&releaseFrame.RearHandTracked&&releaseFrame.FrontHandTracked);
                var openingGrenadeResult=AdvanceGrenadeCommand(input);
                var neutral=!input.TriggerHeld&&!input.RightGripHeld&&!input.LeftGripHeld&&
                    input.MoveAxis.sqrMagnitude<.01f&&input.TurnAxis.sqrMagnitude<.01f&&!hardware.GrenadeHeld;
                if(!vr)neutral&=Keyboard.current?.eKey.isPressed!=true&&Keyboard.current?.gKey.isPressed!=true&&Keyboard.current?.hKey.isPressed!=true;
                if(neutral){awaitingInputRelease=false;if(grab!=null)grab.enabled=vr;}
                // The neutral frame is not a command. Held controls never reach locomotion/grab/fire.
                LastFrameResult=tick.Advance();
                if(LastFrameResult.Success&&!openingGrenadeResult.Success)LastFrameResult=openingGrenadeResult;
                return;
            }
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
            // Squad commands do not depend on the rifle's grip/input pipeline succeeding.
            var grenadeResult=AdvanceGrenadeCommand(input);
            LastFrameResult=controller.Step(frame,delta);
            if(!LastFrameResult.Success)return;
            LastFrameResult=tick.Advance();
            if(LastFrameResult.Success&&!grenadeResult.Success)LastFrameResult=grenadeResult;
            if(tracked&&input.ConfirmPressed)Interact();
            if(input.BackPressed)VRShooting.Unity.Bootstrap.GameMain.Instance?.Services.Combat.BackToMaps();
        }
        ServiceResult<Unit> AdvanceGrenadeCommand(IXRTrainingInput input)
        {
            if(grenades==null)return ServiceResult<Unit>.Ok(Unit.Value);
            if(mode==TrainingMode.Trench&&input is P3XRTrainingInput p3&&p3.GrenadePressed)
                grenades.RequestFirstTeammateThrow();
            // The tactic service still enforces Running, tracking, thrower state and cooldown.
            return grenades.Advance();
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
                    if(mode==TrainingMode.Trench && NavMesh.SamplePosition(at,out var spawnHit,1.25f,NavMesh.AllAreas))
                        at=spawnHit.position;
                    actor=Instantiate(entity.Role==CombatEntityRole.Enemy?binding.EnemyPrefab:binding.TeammatePrefab,at,Quaternion.LookRotation(entity.Forward),transform);
                    if(actor.ShotClip==null)actor.ShotClip=binding.RifleShotClip;
                    actor.EntityId=entity.EntityId;actor.gameObject.AddComponent<CombatEntityBinding>().EntityId=entity.EntityId;
                    actor.NavigationReported+=NavigationReported;actors.Add(entity.EntityId,actor);
                    actor.SetActionsLocked(characterActionsLocked);
                    if(entity.Role==CombatEntityRole.Enemy)
                    {
                        actor.Agent.enabled=false;
                        if(mode==TrainingMode.Trench)actor.transform.position=entity.Position;
                    }
                    if(mode==TrainingMode.Trench)
                        actor.MatchVisualToTerrain(binding.GeometryRoot.GetComponentInChildren<Terrain>());
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
            if(characterActionsLocked)return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
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
                    var fleshHit = hit && contact.collider.GetComponentInParent<CombatActorView>() != null;
                    weaponFeedback.PlayValidShot((int)++sequence,origin,hit?contact.point:origin+direction*100,
                        hit && !fleshHit,contact.point,contact.normal,false);
                }
                if(vr) {InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0,.35f,.08f);InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0,.2f,.06f);}
            }
            if(actors.TryGetValue(feedback.Kind==CombatFeedbackKind.EnemyHit?feedback.TargetId:feedback.EntityId,out var actor))
            {
                if(feedback.Kind==CombatFeedbackKind.EnemyHit)
                {
                    actor.PlayHit(feedback.EventId);
                    if (weaponFeedback != null && feedback.EntityId == session + ".player")
                    {
                        var origin = FrameOverride?.MuzzlePosition ?? weapon.MuzzlePoint.position;
                        var direction = FrameOverride?.AimDirection ?? weapon.AimLinePoint.forward;
                        var point = CombatBloodImpactLocator.Resolve(actor, new Ray(origin, direction), out var normal);
                        weaponFeedback.PlayConfirmedFleshImpact(point, normal, (int)++sequence);
                    }
                }
                if(feedback.Kind==CombatFeedbackKind.EnemyAttack)
                    actor.PlayShot(feedback.EventId, player.position + Vector3.up * 1.2f);
            }
        }

        void OnGrenadeThrown(GrenadeThrowPlanDto plan)
        {
            if (!active || plan.SessionId != session) return;
            if (actors.TryGetValue(plan.ThrowerId, out var thrower))
            {
                var toward=plan.Target-thrower.transform.position;toward.y=0;
                if(toward.sqrMagnitude>.001f)thrower.transform.rotation=Quaternion.LookRotation(toward);
                thrower.PlayGrenadeThrow();
            }
            var prefab = binding.GrenadePrefab ?? Resources.Load<GameObject>("Combat/Grenade_M67") ?? Resources.Load<GameObject>("Combat/m67_low");
            if(prefab==null){Debug.LogWarning("Grenade model resource is missing.",this);return;}
            // Keep the imported FBX's unit scale/pivot separate from the world-space projectile.
            var grenade=new GameObject("Grenade_M67_"+plan.GrenadeId);
            grenade.transform.SetParent(transform,true);
            grenade.transform.position=plan.Origin;
            var model=Instantiate(prefab,grenade.transform);
            model.transform.localPosition=Vector3.zero;
            model.transform.localRotation=Quaternion.identity;
            foreach(var child in model.GetComponentsInChildren<Transform>(true))
            {child.gameObject.SetActive(true);child.gameObject.layer=0;}
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var lod in model.GetComponentsInChildren<LODGroup>(true))lod.ForceLOD(0);
            var renderers=model.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)Debug.LogWarning("Grenade prefab has no renderers: "+prefab.name,this);
            if(renderers.Length>0)
            {
                var bounds=renderers[0].bounds;for(var i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                if(bounds.size.y>.001f)model.transform.localScale*=.22f/bounds.size.y;
                // The model rotates about its centre and rests above the collision surface.
                bounds=renderers[0].bounds;for(var i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                model.transform.position+=grenade.transform.position-bounds.center;
                foreach(var mesh in renderers){mesh.enabled=true;mesh.forceRenderingOff=false;}
                if(grenadeMaterial==null)
                {
                    var shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
                    if(shader!=null)
                    {
                        grenadeMaterial=new Material(shader);
                        var albedo=Resources.Load<Texture2D>("Combat/textures/DefaultMaterial_BaseColor");
                        var normal=Resources.Load<Texture2D>("Combat/textures/DefaultMaterial_Normal");
                        var metal=Resources.Load<Texture2D>("Combat/textures/DefaultMaterial_Metallic");
                        if(albedo!=null){if(grenadeMaterial.HasProperty("_BaseMap"))grenadeMaterial.SetTexture("_BaseMap",albedo);if(grenadeMaterial.HasProperty("_MainTex"))grenadeMaterial.SetTexture("_MainTex",albedo);}
                        if(normal!=null&&grenadeMaterial.HasProperty("_BumpMap")){grenadeMaterial.SetTexture("_BumpMap",normal);grenadeMaterial.EnableKeyword("_NORMALMAP");}
                        if(metal!=null&&grenadeMaterial.HasProperty("_MetallicGlossMap")){grenadeMaterial.SetTexture("_MetallicGlossMap",metal);grenadeMaterial.EnableKeyword("_METALLICGLOSSMAP");}
                        if(grenadeMaterial.HasProperty("_Metallic"))grenadeMaterial.SetFloat("_Metallic",.25f);
                        if(grenadeMaterial.HasProperty("_Surface"))grenadeMaterial.SetFloat("_Surface",0);
                        if(grenadeMaterial.HasProperty("_Cull"))grenadeMaterial.SetFloat("_Cull",0);
                        if(albedo!=null&&grenadeMaterial.HasProperty("_EmissionMap")){grenadeMaterial.SetTexture("_EmissionMap",albedo);grenadeMaterial.SetColor("_EmissionColor",Color.white*.12f);grenadeMaterial.EnableKeyword("_EMISSION");}
                        if(grenadeMaterial.HasProperty("_Smoothness"))grenadeMaterial.SetFloat("_Smoothness",.4f);
                    }
                }
                if(grenadeMaterial!=null)foreach(var mesh in renderers)mesh.sharedMaterial=grenadeMaterial;
            }
            var projectile = grenade.GetComponent<CombatGrenadeProjectile>() ?? grenade.AddComponent<CombatGrenadeProjectile>();
            grenadesInFlight[plan.GrenadeId] = projectile;
            projectile.Launch(plan, () => { if (grenadesInFlight.ContainsKey(plan.GrenadeId)) { Destroy(grenade); grenadesInFlight.Remove(plan.GrenadeId); } }, () => grenades.ProjectilePosition);
        }

        void OnGrenadeExploded(GrenadeExplosionDto explosion)
        {
            if (explosion.SessionId != session) return;
            if (grenadesInFlight.TryGetValue(explosion.GrenadeId, out var projectile))
            {
                if (projectile != null) Destroy(projectile.gameObject);
                grenadesInFlight.Remove(explosion.GrenadeId);
            }
            var blast = new GameObject("VFX_GrenadeExplosion_" + explosion.GrenadeId);
            blast.transform.SetParent(transform); blast.transform.position = explosion.Position;
            blast.AddComponent<CombatExplosionVfx>();
        }
        void OnGrenadeCancelled()
        {
            foreach(var projectile in grenadesInFlight.Values)if(projectile!=null)Destroy(projectile.gameObject);
            grenadesInFlight.Clear();
        }
        public bool TryGetThrowPose(string memberId,out Vector3 feet,out Vector3 release)
        {
            if(actors.TryGetValue(memberId,out var actor)&&actor!=null&&!actor.IsDead)
            {
                // Navigation/perception nodes do not follow the terrain-adjusted soldier mesh.
                feet=actor.GroundedFeetPosition;
                release=actor.VisualRoot!=null
                    ? actor.VisualRoot.TransformPoint(new Vector3(.18f,1.53f,.62f))
                    : feet+Vector3.up*1.53f+actor.transform.forward*.62f;
                release.y=Mathf.Max(release.y,feet.y+1.25f);
                return true;
            }
            feet=release=default;return false;
        }
        public bool Sweep(Vector3 from,Vector3 to,out Vector3 impact)
        {
            var direction=to-from;var distance=direction.magnitude;
            impact=to;
            if(distance>.001f)
            {
                var hits=Physics.SphereCastAll(from,.045f,direction.normalized,distance,~(1<<2),QueryTriggerInteraction.Ignore);
                float closest=float.PositiveInfinity;
                foreach(var hit in hits)
                {
                    if(hit.collider.GetComponentInParent<CombatActorView>()!=null||hit.distance>=closest)continue;
                    closest=hit.distance;impact=hit.point+hit.normal*.085f;
                }
                if(closest<float.PositiveInfinity)return true;
            }
            return false;
        }
        public bool IsExposed(Vector3 blast,Vector3 enemyFeet)
        {
            var start=blast+Vector3.up*.1f;var target=enemyFeet+Vector3.up*1.05f;var direction=target-start;var distance=direction.magnitude;
            if(distance<.01f)return true;
            foreach(var hit in Physics.RaycastAll(start,direction.normalized,distance-.04f,~(1<<2),QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponentInParent<CombatActorView>()==null)return false;
            return true;
        }
        public void Deactivate()
        {
            active=false;
            awaitingInputRelease=false;
            if(state!=null)state.VisualChanged-=ApplyVisual;if(core!=null)core.Feedback-=Feedback;
            if(grenades!=null){grenades.GrenadeThrown-=OnGrenadeThrown;grenades.GrenadeExploded-=OnGrenadeExploded;grenades.GrenadeCancelled-=OnGrenadeCancelled;}
            controller?.Dispose();controller=null;
            foreach(var actor in actors.Values)if(actor!=null){actor.NavigationReported-=NavigationReported;actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
            actors.Clear();navigation.Clear();navigationResults.Clear();presence.Clear();
            foreach(var projectile in grenadesInFlight.Values)if(projectile!=null)Destroy(projectile.gameObject);
            grenadesInFlight.Clear();
            if(grenadeMaterial!=null){Destroy(grenadeMaterial);grenadeMaterial=null;}
            if(rifle!=null){if(grab!=null)grab.ResetToRack();rifle.SetActive(false);Destroy(rifle);}
            if(binding!=null&&mode==TrainingMode.Trench)binding.WeaponAnchor.gameObject.SetActive(true);
            rifle=null;weapon=null;grab=null;core=null;world=null;state=null;grenades=null;tick=null;session="";
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
