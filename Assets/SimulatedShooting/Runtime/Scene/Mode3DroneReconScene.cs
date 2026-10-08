using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace SimulatedShooting.Scene
{
    /// <summary>Live scene adapter for BDD28. Reports facts, never starts combat itself.</summary>
    public sealed class Mode3DroneReconScene : MonoBehaviour, IDroneReconScenePort, VRShooting.Unity.UI.IDroneReconVideoSource
    {
        [SerializeField] float cruiseHeight = 3.2f;
        [SerializeField] int videoWidth = 1024;
        [SerializeField] int videoHeight = 576;
        [SerializeField] string routeId="mode3.trench-recon";
        [SerializeField] string feedBindingId="mode3.drone-feed";
        CombatSceneRuntime runtime;
        CombatSceneBindings bindings;
        Transform drone;
        Camera capture;
        RenderTexture video;
        AudioSource rotorAudio;
        AudioClip rotorClip;
        Transform[] rotors;
        Terrain groundTerrain;
        readonly RaycastHit[] groundHits=new RaycastHit[256];
        readonly RaycastHit[] obstacleHits=new RaycastHit[256];
        readonly List<Vector3> route = new List<Vector3>();
        readonly List<GameObject> routeObjects = new List<GameObject>();
        readonly List<Vector3> travelled = new List<Vector3>();
        readonly List<Vector3> steps = new List<Vector3>();
        DroneReconStepCommandDto command;
        string session, sequence;
        Vector3 parkedPosition, phaseStart, originalDronePosition;
        Quaternion originalDroneRotation;
        float age;
        float flightSpeed;
        [SerializeField,Range(.1f,10f)] float patrolSpeedMetersPerSecond=2.5f;
        Quaternion fixedCameraRotation;
        Vector3 fixedCameraOffset;
        int waypoint;
        bool flying, suspended, configured;
        public event Action<DroneReconSceneFactDto> FactReceived;
        public RenderTexture Video => video;
        public Camera CaptureCamera => capture;
        public IReadOnlyList<Vector3> Route => route.AsReadOnly();
        public bool IsFlying => flying;
        public Texture ResolveVideo(string id) => id == command.FeedBindingId && video != null && video.IsCreated() ? video : null;

        public void Configure(CombatSceneRuntime owner, CombatSceneBindings source)
        {
            runtime=owner;bindings=source;drone=source.Drone;
            groundTerrain=source.GeometryRoot!=null?source.GeometryRoot.GetComponentInChildren<Terrain>():null;
            if(drone==null)return;
            var visualPrefab=source.DroneVisualPrefab??Resources.Load<GameObject>("Combat/Drone_Realistic");
            if(visualPrefab!=null && drone.Find("Drone_RealisticVisual")==null)
            {
                foreach(var renderer in drone.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                var visual=Instantiate(visualPrefab,drone);visual.name="Drone_RealisticVisual";
                visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
                foreach(var collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                foreach(var rigidbody in visual.GetComponentsInChildren<Rigidbody>(true)){rigidbody.isKinematic=true;rigidbody.detectCollisions=false;}
            }
            originalDronePosition=drone.position;originalDroneRotation=drone.rotation;
            var visualRoot=drone.Find("Drone_RealisticVisual")??drone;
            rotors=visualRoot.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Rotor").ToArray();
            configured=true;
        }

        public ServiceResult<Unit> Prepare(DroneReconStepCommandDto value)
        {
            if(!configured||runtime.SessionId!=value.SessionId||drone==null||bindings.GeometryRoot==null)
                return Fail("Missing drone, geometry or current session");
            if(value.RouteId!=routeId||value.FeedBindingId!=feedBindingId)
                return ServiceResult<Unit>.Fail(ErrorCode.NotFound,"Drone route/feed binding does not match this scene");
            if(!Finite(cruiseHeight)||cruiseHeight<.5f||!Finite(patrolSpeedMetersPerSecond)||patrolSpeedMetersPerSecond<=0||videoWidth<128||videoWidth>2048||videoHeight<128||videoHeight>2048)
                return ServiceResult<Unit>.Fail(ErrorCode.InvalidInput,"Invalid drone flight/video configuration");
            if(bindings.DroneReconWaypoints!=null&&bindings.DroneReconWaypoints.Any(t=>t==null||!Finite(t.position)))
                return ServiceResult<Unit>.Fail(ErrorCode.InvalidInput,"Invalid authored recon waypoint");
            if(session==value.SessionId && sequence==value.SequenceId)return Ok();
            if(!string.IsNullOrEmpty(session))return ServiceResult<Unit>.Fail(ErrorCode.Busy);
            session=value.SessionId;sequence=value.SequenceId;command=value;
            drone.name="Drone_Mode3_Recon";
            parkedPosition=bindings.TrenchEntry.position+bindings.TrenchEntry.forward*1.8f+bindings.TrenchEntry.right*.65f;
            if(NavMesh.SamplePosition(parkedPosition,out var landing,3f,NavMesh.AllAreas))parkedPosition=landing.position;
            else return Fail("No navigable launch point near the squad");
            parkedPosition.y=GroundHeight(parkedPosition)+.23f;
            drone.SetPositionAndRotation(parkedPosition,bindings.TrenchEntry.rotation);
            AddAnchor("Anchor_Mode3_DroneTakeoff",parkedPosition,transform);
            AddAnchor("Anchor_Mode3_DroneLanding",parkedPosition,transform);
            var routeRoot=AddAnchor("Route_Mode3_DroneRecon",parkedPosition,transform);
            route.Clear();travelled.Clear();
            var observations=(bindings.DroneReconWaypoints??Array.Empty<Transform>()).Select(t=>t.position).ToArray();
            if(observations.Length==0)
                observations=runtime.Definition.SearchNodes.OrderBy(n=>n.NodeId,StringComparer.Ordinal).Take(3).Select(n=>n.WorldPosition).ToArray();
            route.AddRange(DroneReconRoutePlanner.BuildFlyover(parkedPosition,observations,GroundHeight,cruiseHeight));
            if(route.Count==0)return Fail("Full trench survey route is empty");
            for(var i=0;i<route.Count;i++)AddAnchor("Waypoint_Mode3_DroneRecon_"+i.ToString("000"),route[i],routeRoot.transform);
            var cameraRoot=new GameObject("Camera_Mode3_DroneFeed");cameraRoot.transform.SetParent(drone,false);
            cameraRoot.transform.localPosition=new Vector3(0,-.12f,.2f);
            cameraRoot.transform.localRotation=Quaternion.Euler(35,0,0);
            fixedCameraRotation=cameraRoot.transform.rotation;
            fixedCameraOffset=cameraRoot.transform.position-drone.position;
            flightSpeed=Mathf.Min(value.FlightSpeedMetersPerSecond,patrolSpeedMetersPerSecond);
            capture=cameraRoot.AddComponent<Camera>();capture.fieldOfView=90;capture.nearClipPlane=.06f;
            capture.farClipPlane=300;capture.stereoTargetEye=StereoTargetEyeMask.None;capture.depth=-20;
            capture.cullingMask=~(1<<5); // Monitor canvases must never be captured recursively.
            video=new RenderTexture(videoWidth,videoHeight,24,RenderTextureFormat.ARGB32){name=value.FeedBindingId};
            video.Create();capture.targetTexture=video;capture.enabled=true;
            if(!video.IsCreated())return Fail("Drone RenderTexture allocation failed");
            rotorClip=Resources.Load<AudioClip>("Combat/Audio/DroneRotor_Mavic_Recorded");
            if(rotorClip==null)return Fail("Recorded DJI rotor audio is missing");
            rotorAudio=drone.gameObject.AddComponent<AudioSource>();rotorAudio.spatialBlend=1;
            rotorAudio.playOnAwake=false;rotorAudio.loop=true;rotorAudio.volume=.65f;
            rotorAudio.rolloffMode=AudioRolloffMode.Linear;rotorAudio.minDistance=4;rotorAudio.maxDistance=50;
            rotorAudio.dopplerLevel=0;rotorAudio.clip=rotorClip;
            return Ok();
        }

        public ServiceResult<Unit> PlayStep(DroneReconStepCommandDto value)
        {
            if(!Matches(value))return ServiceResult<Unit>.Fail(ErrorCode.NotFound);
            if(command.StepId==value.StepId&&command.Phase==value.Phase&&flying)return Ok();
            command=value;age=0;phaseStart=drone.position;steps.Clear();waypoint=0;flying=true;
            switch(value.Phase)
            {
                case DroneReconPhase.PlayerTakeoff:
                    steps.Add(new Vector3(parkedPosition.x,GroundHeight(parkedPosition)+cruiseHeight,parkedPosition.z));
                    travelled.Clear();travelled.Add(steps[0]);rotorAudio.Play();break;
                case DroneReconPhase.DroneRecon:steps.AddRange(route);break;
                case DroneReconPhase.DroneReturn:steps.AddRange(travelled.AsEnumerable().Reverse());break;
                case DroneReconPhase.DroneLanding:steps.Add(parkedPosition);break;
                default:flying=false;return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            }
            if(steps.Count==0){flying=false;return Fail("Empty flight step");}
            Report(DroneReconFactKind.FeedState);
            return Ok();
        }

        // The production runtime calls this with its injected/manual scene clock delta.
        public void AdvanceFlight(float seconds)
        {
            if(!flying||suspended||seconds<=0)return;
            if(!Finite(seconds)){Failure("Invalid flight clock delta");return;}
            if(capture==null||video==null||!video.IsCreated()){Failure("Live drone feed became unavailable");return;}
            age+=seconds;var before=drone.position;var target=steps[waypoint];
            var timed=command.Phase==DroneReconPhase.PlayerTakeoff||command.Phase==DroneReconPhase.DroneLanding;
            var next=before;
            if(timed)next=Vector3.Lerp(phaseStart,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/command.DurationSeconds)));
            else
            {
                var remaining=flightSpeed*seconds;
                // Consume residual distance across corners instead of pausing for a frame at each one.
                while(waypoint<steps.Count)
                {
                    target=steps[waypoint];var distance=Vector3.Distance(next,target);
                    var end=Vector3.MoveTowards(next,target,remaining);
                    if(Blocked(next,end)){Failure("Drone route blocked by scene geometry");return;}
                    next=end;
                    if(distance>remaining)break;
                    remaining-=distance;
                    if(command.Phase==DroneReconPhase.DroneRecon)travelled.Add(target);
                    waypoint++;
                    if(remaining<=0)break;
                }
            }
            var map=runtime.Definition.Projections.First(p=>p.FloorId.Length==0).WorldBoundsXZ;
            if(!Finite(next)||next.x<map.xMin||next.x>map.xMax||next.z<map.yMin||next.z>map.yMax)
            {Failure("Drone route left the configured map bounds");return;}
            if(timed&&Blocked(before,next)){Failure("Drone route blocked by scene geometry");return;}
            drone.position=next;
            if(!timed)
            {
                var direction=target-before;direction.y=0;
                if(direction.sqrMagnitude>.001f)drone.rotation=Quaternion.Slerp(drone.rotation,Quaternion.LookRotation(direction),seconds*3);
            }
            capture.transform.SetPositionAndRotation(drone.position+fixedCameraOffset,fixedCameraRotation);
            foreach(var rotor in rotors)if(rotor!=null)rotor.Rotate(Vector3.up,seconds*2400,Space.Self);
            FactReceived?.Invoke(new DroneReconSceneFactDto {SessionId=session,SequenceId=sequence,StepId=command.StepId,
                Phase=command.Phase,Kind=DroneReconFactKind.Telemetry,TelemetryValid=true,
                HeightAboveGroundMeters=Mathf.Max(0,next.y-GroundHeight(next)),SpeedMetersPerSecond=(next-before).magnitude/seconds});
            if(timed)
            {
                if(age<command.DurationSeconds)return;
                drone.position=target;waypoint++;
            }
            if(waypoint<steps.Count)return;
            flying=false;
            if(command.Phase==DroneReconPhase.DroneLanding)rotorAudio.Stop();
            Report(DroneReconFactKind.StepCompleted);
        }

        public ServiceResult<Unit> SetCharacterActionsLocked(string id,bool locked)
            => runtime==null?Fail("Scene runtime missing"):runtime.SetCharacterActionsLocked(id,locked);
        public ServiceResult<Unit> SetSuspended(string id,bool value)
        {if(id!=session)return ServiceResult<Unit>.Fail(ErrorCode.NotFound);suspended=value;
            if(rotorAudio!=null){if(value)rotorAudio.Pause();else if(flying)rotorAudio.UnPause();}return Ok();}
        public ServiceResult<Unit> RestorePlayerView(DroneReconStepCommandDto value)
        {
            if(!Matches(value))return ServiceResult<Unit>.Fail(ErrorCode.NotFound);
            command=value;flying=false;
            drone.position=parkedPosition;
            if(rotorAudio!=null)rotorAudio.Stop();
            if(capture!=null)capture.enabled=false;
            if(runtime.PlayerCamera==null)return Fail("Player camera missing");
            runtime.PlayerCamera.enabled=true;
            if(value.Phase==DroneReconPhase.RestoringPlayerView)Report(DroneReconFactKind.StepCompleted);
            return Ok();
        }
        public ServiceResult<Unit> StopAndReset(string id,string expectedSequence)
        {
            if(string.IsNullOrEmpty(session))return Ok();
            if(id!=session||expectedSequence!=sequence)return ServiceResult<Unit>.Fail(ErrorCode.NotFound);
            flying=false;suspended=false;
            if(rotorAudio!=null){rotorAudio.Stop();ReleaseObject(rotorAudio);rotorAudio=null;}
            // Imported recordings are shared assets, not runtime-created clips.
            rotorClip=null;
            if(capture!=null){capture.targetTexture=null;capture.enabled=false;ReleaseObject(capture.gameObject);capture=null;}
            if(video!=null){video.Release();ReleaseObject(video);video=null;}
            foreach(var item in routeObjects)if(item!=null)ReleaseObject(item);routeObjects.Clear();route.Clear();steps.Clear();travelled.Clear();
            if(drone!=null)drone.SetPositionAndRotation(originalDronePosition,originalDroneRotation);
            session=sequence=null;return Ok();
        }
        GameObject AddAnchor(string name,Vector3 position,Transform parent)
        {var root=new GameObject(name);root.transform.SetParent(parent,true);root.transform.position=position;routeObjects.Add(root);return root;}
        float GroundHeight(Vector3 point)
        {
            var rayOrigin=point+Vector3.up*250;
            var count=Physics.RaycastNonAlloc(rayOrigin,Vector3.down,groundHits,500,~0,QueryTriggerInteraction.Ignore);
            // A saturated query is incomplete; retain correctness in dense scene geometry.
            var hits=count==groundHits.Length
                ? Physics.RaycastAll(rayOrigin,Vector3.down,500,~0,QueryTriggerInteraction.Ignore):groundHits;
            if(hits!=groundHits)count=hits.Length;
            var ground=groundTerrain!=null?groundTerrain.SampleHeight(point)+groundTerrain.transform.position.y:point.y;
            for(var i=0;i<count;i++)
                if(hits[i].transform.IsChildOf(bindings.GeometryRoot))ground=Mathf.Max(ground,hits[i].point.y);
            return ground;
        }
        bool Blocked(Vector3 from,Vector3 to)
        {var delta=to-from;if(delta.sqrMagnitude<.000001f)return false;
            var count=Physics.SphereCastNonAlloc(from,.12f,delta.normalized,obstacleHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            var hits=count==obstacleHits.Length
                ? Physics.SphereCastAll(from,.12f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore):obstacleHits;
            if(hits!=obstacleHits)count=hits.Length;
            for(var i=0;i<count;i++)if(hits[i].transform.IsChildOf(bindings.GeometryRoot))return true;
            return false;}
        bool Matches(DroneReconStepCommandDto value)=>value.SessionId==session&&value.SequenceId==sequence;
        void Report(DroneReconFactKind kind)=>FactReceived?.Invoke(new DroneReconSceneFactDto {
            SessionId=session,SequenceId=sequence,StepId=command.StepId,Phase=command.Phase,Kind=kind,
            FeedAvailable=video!=null&&video.IsCreated(),FeedBindingId=command.FeedBindingId });
        void Failure(string reason){flying=false;rotorAudio?.Stop();FactReceived?.Invoke(new DroneReconSceneFactDto {
            SessionId=session,SequenceId=sequence,StepId=command.StepId,Phase=command.Phase,
            Kind=DroneReconFactKind.StepFailed,ErrorCode=ErrorCode.ResourceUnavailable,Reason=reason });}
        static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<Unit> Fail(string reason)=>ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable,reason);
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
        static void ReleaseObject(UnityEngine.Object value)
        {
            if(Application.isPlaying)Destroy(value);
            else DestroyImmediate(value);
        }
        void OnDestroy(){StopAndReset(session,sequence);FactReceived=null;}
    }
}

