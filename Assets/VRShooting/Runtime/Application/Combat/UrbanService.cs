using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD18–21/25: one session across street/building, explicit checks and boundary-only outcomes.</summary>
    public sealed class UrbanService : IUrbanService, ICombatWorldInputPort, ICombatStateService, IHUDService, IDisposable
    {
        readonly CombatSceneDefinitionDto definition;
        readonly CombatConfigDto config;
        readonly ICombatClock clock;
        readonly ICombatRandom random;
        readonly Func<string> nextSessionId;
        readonly CombatCoreService core;
        readonly SquadFormationService squad;
        readonly Dictionary<string,RoomState> rooms=new Dictionary<string,RoomState>(StringComparer.Ordinal);
        readonly Dictionary<string,SceneSpawnPointDto> enemies=new Dictionary<string,SceneSpawnPointDto>(StringComparer.Ordinal);
        readonly HashSet<string> killed=new HashSet<string>(),inRange=new HashSet<string>();
        readonly Dictionary<string,CombatInputDto> received=new Dictionary<string,CombatInputDto>();
        readonly List<CombatInputDto> pending=new List<CombatInputDto>();
        string session="",floor="";
        UrbanPhase phase;
        long revision;
        double elapsed,lastNow;
        bool disposed,batch,starting,publishing,dirty,cancelled,hasResult;
        bool? pendingOutcome;
        CombatCoreSnapshotDto combat;
        UrbanSessionDto snapshot;
        UrbanResultDto result;
        HudDto hud;
        CombatVisualSnapshotDto visual;
        public ICombatCoreService Combat=>core;
        public ISquadCommandService SquadCommands=>squad;
        public event Action<UrbanSessionDto> SessionChanged;
        public event Action<UrbanResultDto> ResultReady;
        public event Action<HudDto> HudUpdated;
        public event Action<CombatPlayerSnapshotDto> PlayerChanged;
        public event Action<CombatSquadSnapshotDto> SquadChanged;
        public event Action<CombatVisualSnapshotDto> VisualChanged;

        public UrbanService(CombatSceneDefinitionDto definition,ICombatClock clock,ICombatRandom random,ICombatNavigationPort navigation,
            CombatConfigDto? config=null,Func<string> sessionIdFactory=null)
        {
            this.definition=definition;this.clock=clock??throw new ArgumentNullException(nameof(clock));
            this.random=random??throw new ArgumentNullException(nameof(random));this.config=config??CombatConfigDto.Default;
            nextSessionId=sessionIdFactory??(()=>Guid.NewGuid().ToString("N"));
            core=new CombatCoreService(clock,this.config);squad=new SquadFormationService(clock,navigation,this.config);
            core.Changed+=OnCore;squad.Changed+=OnSquad;
        }
        public ServiceResult<IReadOnlyList<UrbanMapDto>> GetMaps()
        {var valid=ValidateMap("urban-a");return valid.Success?ServiceResult<IReadOnlyList<UrbanMapDto>>.Ok(Array.AsReadOnly(new[]{Map()})):Fail<IReadOnlyList<UrbanMapDto>>(valid.ErrorCode);}
        public ServiceResult<UrbanMapDto> SelectMap(string mapId)
        {var valid=ValidateMap(mapId);return valid.Success?ServiceResult<UrbanMapDto>.Ok(Map()):Fail<UrbanMapDto>(valid.ErrorCode);}
        public ServiceResult<UrbanSessionDto> StartSession(string mapId,string weaponId,RandomSeed seed)
        {
            var valid=ValidateMap(mapId);if(!valid.Success)return Fail<UrbanSessionDto>(valid.ErrorCode);
            if(string.IsNullOrWhiteSpace(weaponId))return Fail<UrbanSessionDto>(ErrorCode.InvalidState);
            if(weaponId!="training-rifle")return Fail<UrbanSessionDto>(ErrorCode.NotFound);
            if(publishing||batch||starting||(session.Length>0&&!hasResult&&!cancelled))return Fail<UrbanSessionDto>(ErrorCode.Busy);
            var id=nextSessionId();if(string.IsNullOrWhiteSpace(id))return Fail<UrbanSessionDto>(ErrorCode.InvalidInput);
            random.Reset(seed);var chosen=new Dictionary<string,SceneSpawnPointDto>();var spawns=new List<CombatEntityVisualDto>();
            foreach(var group in new[]{EncounterGroup.Street,EncounterGroup.Building})
            {
                var min=group==EncounterGroup.Street?1:3;var max=group==EncounterGroup.Street?2:6;
                var count=random.NextInt(min,max+1);if(count<min||count>max)return Fail<UrbanSessionDto>(ErrorCode.InvalidInput);
                var candidates=definition.SpawnPoints.Where(p=>p.Group==group).OrderBy(p=>p.PointId,StringComparer.Ordinal).ToList();
                for(var i=0;i<count;i++)
                {
                    var index=random.NextInt(0,candidates.Count);if(index<0||index>=candidates.Count)return Fail<UrbanSessionDto>(ErrorCode.InvalidInput);
                    var point=candidates[index];candidates.RemoveAt(index);var enemyId=id+".enemy-"+(spawns.Count+1).ToString("000",CultureInfo.InvariantCulture);
                    chosen.Add(enemyId,point);spawns.Add(new CombatEntityVisualDto {EntityId=enemyId,Role=CombatEntityRole.Enemy,Position=point.WorldPosition,Forward=Vector3.forward});
                }
            }
            starting=true;
            try
            {
                var started=core.Start(id,TrainingMode.Urban,spawns);if(!started.Success)return Fail<UrbanSessionDto>(started.ErrorCode);
                session=id;combat=started.Data;revision=0;elapsed=0;lastNow=clock.Now;phase=UrbanPhase.Street;floor="";
                cancelled=hasResult=false;pendingOutcome=null;killed.Clear();inRange.Clear();received.Clear();pending.Clear();rooms.Clear();enemies.Clear();
                foreach(var pair in chosen)enemies.Add(pair.Key,pair.Value);
                foreach(var f in definition.Floors)foreach(var r in f.Rooms)rooms.Add(r.RoomId,new RoomState {Definition=r,FloorId=f.FloorId});
                squad.Start(id,Vector3.zero,Vector3.forward);dirty=true;
            }
            finally{starting=false;}
            Publish();return ServiceResult<UrbanSessionDto>.Ok(snapshot);
        }
        public ServiceResult<UrbanSessionDto> GetSession(string id)=>Matches(id)?ServiceResult<UrbanSessionDto>.Ok(snapshot):Fail<UrbanSessionDto>(ErrorCode.NotFound);
        public ServiceResult<UrbanResultDto> GetResult(string id)=>Matches(id)&&hasResult?ServiceResult<UrbanResultDto>.Ok(result):Fail<UrbanResultDto>(ErrorCode.NotFound);
        public ServiceResult<HudDto> GetHud(string id)=>Matches(id)?ServiceResult<HudDto>.Ok(hud):Fail<HudDto>(ErrorCode.NotFound);
        public ServiceResult<CombatPlayerSnapshotDto> GetPlayer(string id)=>Matches(id)?ServiceResult<CombatPlayerSnapshotDto>.Ok(new CombatPlayerSnapshotDto {SessionId=session,Revision=revision,Player=snapshot.Player}):Fail<CombatPlayerSnapshotDto>(ErrorCode.NotFound);
        public ServiceResult<CombatSquadSnapshotDto> GetSquadStatus(string id)=>Matches(id)?ServiceResult<CombatSquadSnapshotDto>.Ok(new CombatSquadSnapshotDto {SessionId=session,Revision=revision,Squad=snapshot.Squad}):Fail<CombatSquadSnapshotDto>(ErrorCode.NotFound);
        public ServiceResult<CombatVisualSnapshotDto> GetVisualSnapshot(string id)=>Matches(id)?ServiceResult<CombatVisualSnapshotDto>.Ok(visual):Fail<CombatVisualSnapshotDto>(ErrorCode.NotFound);
        public ServiceResult<IReadOnlyList<CombatEnemyAssignmentDto>> GetEnemyAssignments(string id)=>Matches(id)
            ?ServiceResult<IReadOnlyList<CombatEnemyAssignmentDto>>.Ok(Array.AsReadOnly(enemies.Select(e=>new CombatEnemyAssignmentDto
                {EntityId=e.Key,SpawnPointId=e.Value.PointId,Group=e.Value.Group,FloorId=e.Value.FloorId,RoomId=e.Value.RoomId}).ToArray()))
            :Fail<IReadOnlyList<CombatEnemyAssignmentDto>>(ErrorCode.NotFound);

        public ServiceResult<UrbanSessionDto> EnterBuilding(string id,string entranceId)=>ChangePhase(id,entranceId,UrbanPhase.Building);
        public ServiceResult<UrbanSessionDto> ExitBuilding(string id,string entranceId)=>ChangePhase(id,entranceId,UrbanPhase.Street);
        ServiceResult<UrbanSessionDto> ChangePhase(string id,string entrance,UrbanPhase next)
        {
            var guard=Boundary(id);if(!guard.Success)return Fail<UrbanSessionDto>(guard.ErrorCode);
            if(entrance!=definition.EntranceId)return Fail<UrbanSessionDto>(ErrorCode.NotFound);
            if(!Active)return Fail<UrbanSessionDto>(ErrorCode.InvalidState);
            if(phase==next)return ServiceResult<UrbanSessionDto>.Ok(snapshot);
            if(!inRange.Contains(entrance))return Fail<UrbanSessionDto>(ErrorCode.InvalidState);
            phase=next;floor=next==UrbanPhase.Building?definition.Floors[0].FloorId:"";
            inRange.RemoveWhere(key=>key!=definition.EntranceId);dirty=true;Publish();return ServiceResult<UrbanSessionDto>.Ok(snapshot);
        }
        public ServiceResult<UrbanSessionDto> OpenRoomDoor(string id,string roomId)
        {
            var guard=RoomGuard(id,roomId);if(!guard.Success)return Fail<UrbanSessionDto>(guard.ErrorCode);
            var room=rooms[roomId];if(room.Open)return ServiceResult<UrbanSessionDto>.Ok(snapshot);
            if(!Active||phase!=UrbanPhase.Building||!inRange.Contains(roomId+".door"))return Fail<UrbanSessionDto>(ErrorCode.InvalidState);
            room.Open=true;room.State=RoomSearchState.Searching;floor=room.FloorId;dirty=true;Publish();return ServiceResult<UrbanSessionDto>.Ok(snapshot);
        }
        public ServiceResult<UrbanSessionDto> ObserveRoom(string id,string roomId)
        {var guard=RoomGuard(id,roomId);return Fail<UrbanSessionDto>(guard.Success?ErrorCode.InvalidState:guard.ErrorCode);}
        public ServiceResult<UrbanSessionDto> MarkRoomSearched(string id,string roomId)
        {
            var guard=RoomGuard(id,roomId);if(!guard.Success)return Fail<UrbanSessionDto>(guard.ErrorCode);
            var room=rooms[roomId];if(room.State==RoomSearchState.Searched)return ServiceResult<UrbanSessionDto>.Ok(snapshot);
            if(!Active||phase!=UrbanPhase.Building||!room.Open||!inRange.Contains(roomId+".check")||
                enemies.Any(e=>e.Value.RoomId==roomId&&!killed.Contains(e.Key)))return Fail<UrbanSessionDto>(ErrorCode.InvalidState);
            room.State=RoomSearchState.Searched;floor=room.FloorId;dirty=true;Publish();return ServiceResult<UrbanSessionDto>.Ok(snapshot);
        }
        public ServiceResult<Unit> Submit(CombatInputDto input)
        {
            var guard=Guard(input.SessionId);if(!guard.Success)return guard;if(!Active)return Fail<Unit>(ErrorCode.InvalidState);
            if(string.IsNullOrWhiteSpace(input.EventId)||!Finite(input.Position)||!Finite(input.Direction)||!Finite(input.Value))return Fail<Unit>(ErrorCode.InvalidInput);
            if(received.TryGetValue(input.EventId,out var old))return old.Equals(input)?Ok():Fail<Unit>(ErrorCode.InvalidInput);
            if(input.Tick!=clock.Tick)return Fail<Unit>(ErrorCode.InvalidState);
            if(input.Kind==CombatInputKind.AreaPresence)
            {
                if(!KnownArea(input.EntityId))return Fail<Unit>(ErrorCode.NotFound);
            }
            else if(input.Kind==CombatInputKind.NavigationResult)
            {var arrival=squad.ValidateArrival(input);if(!arrival.Success)return arrival;}
            else
            {
                if(input.Kind==CombatInputKind.Perception&&input.Flag&&enemies.TryGetValue(input.EntityId,out var enemy)&&
                    enemy.RoomId.Length>0&&!rooms[enemy.RoomId].Open)return Fail<Unit>(ErrorCode.InvalidState);
                var accepted=core.Submit(input);if(accepted.Success)received.Add(input.EventId,input);return accepted;
            }
            received.Add(input.EventId,input);pending.Add(input);return Ok();
        }
        public ServiceResult<Unit> Advance(string id)
        {
            var guard=Guard(id);if(!guard.Success)return guard;if(hasResult||cancelled)return Ok();
            if(pending.Any(p=>p.Tick!=clock.Tick))return Fail<Unit>(ErrorCode.InvalidState);
            batch=true;
            try
            {
                var advanced=core.Advance(id);if(!advanced.Success)return advanced;AccumulateTime();combat=core.GetSnapshot(id).Data;
                foreach(var input in pending)
                {
                    if(input.Kind==CombatInputKind.AreaPresence)
                    {
                        if(input.Flag)dirty|=inRange.Add(input.EntityId);else dirty|=inRange.Remove(input.EntityId);
                        if(input.Flag&&phase==UrbanPhase.Building&&definition.Floors.Any(f=>f.FloorId==input.EntityId)&&floor!=input.EntityId){floor=input.EntityId;dirty=true;}
                    }
                    else if(Active)squad.Submit(input);
                }
                pending.Clear();ReconcileKills();UpdateSquad();Evaluate();
            }
            finally{batch=false;}
            Publish();return Ok();
        }
        public ServiceResult<UrbanSessionDto> RegisterEnemyKilled(string id,string enemyId)
        {
            var guard=Boundary(id);if(!guard.Success)return Fail<UrbanSessionDto>(guard.ErrorCode);
            if(!enemies.ContainsKey(enemyId??""))return Fail<UrbanSessionDto>(ErrorCode.NotFound);
            if(killed.Contains(enemyId))return ServiceResult<UrbanSessionDto>.Ok(snapshot);
            if(cancelled||hasResult||!combat.Visual.Entities.Any(e=>e.EntityId==enemyId&&e.State==CombatEntityState.Dead))return Fail<UrbanSessionDto>(ErrorCode.InvalidState);
            killed.Add(enemyId);dirty=true;Publish();return ServiceResult<UrbanSessionDto>.Ok(snapshot);
        }
        public ServiceResult<UrbanResultDto> CompleteIfReady(string id)
        {
            var guard=Boundary(id);if(!guard.Success)return Fail<UrbanResultDto>(guard.ErrorCode);
            if(hasResult)return ServiceResult<UrbanResultDto>.Ok(result);if(cancelled)return Fail<UrbanResultDto>(ErrorCode.InvalidState);
            batch=true;try{ReconcileKills();Evaluate();}finally{batch=false;}Publish();
            return hasResult?ServiceResult<UrbanResultDto>.Ok(result):Fail<UrbanResultDto>(ErrorCode.InvalidState);
        }
        public ServiceResult<UrbanResultDto> FailByPlayerDeath(string id)
        {
            var guard=Boundary(id);if(!guard.Success)return Fail<UrbanResultDto>(guard.ErrorCode);
            if(hasResult)return ServiceResult<UrbanResultDto>.Ok(result);
            return cancelled||combat.Player.IsAlive?Fail<UrbanResultDto>(ErrorCode.InvalidState):CompleteIfReady(id);
        }
        public ServiceResult<Unit> Cancel(string id)
        {
            var guard=Guard(id);if(!guard.Success)return guard;if(cancelled)return Ok();
            batch=true;try{AccumulateTime();cancelled=true;pending.Clear();inRange.Clear();core.SetState(id,SessionState.Cancelled);squad.Stop();dirty=true;}finally{batch=false;}
            Publish();return Ok();
        }
        void ReconcileKills(){foreach(var e in combat.Visual.Entities)if(e.Role==CombatEntityRole.Enemy&&e.State==CombatEntityState.Dead)dirty|=killed.Add(e.EntityId);}
        void Evaluate()
        {
            if(hasResult||pendingOutcome.HasValue||cancelled)return;
            if(!combat.Player.IsAlive)Finish(false);
            else if(Active&&killed.Count==enemies.Count&&rooms.Values.All(r=>r.State==RoomSearchState.Searched))Finish(true);
        }
        void Finish(bool victory)
        {AccumulateTime();pendingOutcome=victory;phase=UrbanPhase.Results;core.SetState(session,victory?SessionState.Completed:SessionState.Failed);squad.Stop();dirty=true;}
        void OnCore(CombatCoreSnapshotDto value)
        {
            if(starting||disposed||value.SessionId!=session||hasResult||cancelled)return;
            AccumulateTime();combat=value;dirty=true;
            if(!batch&&(value.State!=SessionState.Running||!value.TrackingValid)){pending.Clear();inRange.Clear();}
            if(!batch){batch=true;try{UpdateSquad();}finally{batch=false;}Publish();}
        }
        void OnSquad(CombatSquadSnapshotDto value){if(starting||disposed||value.SessionId!=session)return;dirty=true;if(!batch)Publish();}
        void UpdateSquad(){var p=combat.Visual.Entities[0];squad.UpdatePlayer(p.Position,p.Forward,Active,combat.Player.Health);}
        void AccumulateTime()
        {if(!cancelled&&!hasResult&&combat.State==SessionState.Running&&combat.TrackingValid&&clock.Now>=lastNow){elapsed+=clock.Now-lastNow;if(clock.Now>lastNow)dirty=true;}lastNow=clock.Now;}
        int Total(EncounterGroup group)=>enemies.Count(e=>e.Value.Group==group);
        int Killed(EncounterGroup group)=>enemies.Count(e=>e.Value.Group==group&&killed.Contains(e.Key));
        int Searched=>rooms.Values.Count(r=>r.State==RoomSearchState.Searched);
        bool Active=>!cancelled&&!hasResult&&combat.State==SessionState.Running&&combat.TrackingValid&&combat.Player.IsAlive;
        void Publish()
        {
            if(!dirty||starting||disposed||session.Length==0)return;dirty=false;revision++;
            var squadData=squad.GetSquadStatus(session).Data;var map=BuildMap(phase==UrbanPhase.Street?"":floor);
            var floors=BuildFloors();var streetTotal=Total(EncounterGroup.Street);var streetKilled=Killed(EncounterGroup.Street);
            snapshot=new UrbanSessionDto {SessionId=session,Revision=revision,MapId=definition.MapId,State=cancelled?SessionState.Cancelled:combat.State,Phase=phase,
                StreetEnemyTotal=streetTotal,StreetEnemyKilled=streetKilled,StreetCleared=streetTotal==streetKilled,
                BuildingEnemyTotal=Total(EncounterGroup.Building),BuildingEnemyKilled=Killed(EncounterGroup.Building),RoomsSearched=Searched,RoomsTotal=rooms.Count,
                Ammo=combat.Ammo,Player=combat.Player,Squad=squadData,MiniMap=map,CurrentFloorId=phase==UrbanPhase.Street?"":floor,Floors=floors};
            var prompts=new List<HudPromptDto>();
            if(phase==UrbanPhase.Building&&streetKilled<streetTotal)prompts.Add(new HudPromptDto {PromptId="StreetRisk",Text="街道尚有敌人，任务仍需清除街道"});
            if(Active&&inRange.Contains(definition.EntranceId))prompts.Add(new HudPromptDto {PromptId=phase==UrbanPhase.Street?"EnterBuilding":"ExitBuilding",Text=phase==UrbanPhase.Street?"进入建筑":"返回街道",IsInteractive=true,IsEnabled=true});
            if(Active&&phase==UrbanPhase.Building)foreach(var room in rooms.Values)
            {
                var key=room.Definition.RoomId;
                if(!room.Open&&inRange.Contains(key+".door"))prompts.Add(new HudPromptDto {PromptId=key+".OpenDoor",Text="开门",IsInteractive=true,IsEnabled=true});
                else if(room.Open&&room.State!=RoomSearchState.Searched&&inRange.Contains(key+".check"))prompts.Add(new HudPromptDto {PromptId=key+".CheckRoom",Text="检查房间",IsInteractive=true,IsEnabled=!enemies.Any(e=>e.Value.RoomId==key&&!killed.Contains(e.Key))});
            }
            hud=new HudDto {SessionId=session,Mode=TrainingMode.Urban,HudType=phase==UrbanPhase.Street?HudType.UrbanStreet:HudType.UrbanBuilding,
                Ammo=combat.Ammo,Player=combat.Player,MiniMap=map,CanShoot=Active&&!pendingOutcome.HasValue&&combat.Weapon.CanShoot,Prompts=prompts.AsReadOnly(),
                TextLines=Array.AsReadOnly(new[]{Line("Street","街道敌人",streetKilled+"/"+streetTotal),Line("Building","建筑敌人",snapshot.BuildingEnemyKilled+"/"+snapshot.BuildingEnemyTotal),
                    Line("Rooms","房间检查",Searched+"/"+rooms.Count),Line("Elapsed","用时",elapsed.ToString("F1",CultureInfo.InvariantCulture))})};
            visual=new CombatVisualSnapshotDto {SessionId=session,Revision=revision,Entities=combat.Visual.Entities.Concat(squad.GetVisualSnapshot(session).Data.Entities).ToArray(),
                Doors=rooms.Select(r=>new CombatDoorVisualDto {RoomId=r.Key,Open=r.Value.Open}).ToArray()};
            var newResult=pendingOutcome.HasValue;
            if(newResult)
            {
                result=new UrbanResultDto {SessionId=session,Revision=revision,Victory=pendingOutcome.Value,StreetCleared=snapshot.StreetCleared,
                    BuildingSearchProgress01=(float)Searched/rooms.Count,RoomsSearched=Searched,RoomsTotal=rooms.Count,EnemyKilled=killed.Count,EnemyTotal=enemies.Count,
                    RemainingAmmo=combat.Ammo.CurrentMagazine+combat.Ammo.ReserveAmmo,Squad=squadData,ResultMap=BuildMap(""),FloorMaps=floors.Select(f=>f.MiniMap).ToArray(),ElapsedSeconds=(float)elapsed};
                hasResult=true;pendingOutcome=null;
            }
            publishing=true;
            try
            {
                SessionChanged?.Invoke(snapshot);if(disposed)return;HudUpdated?.Invoke(hud);if(disposed)return;
                PlayerChanged?.Invoke(new CombatPlayerSnapshotDto {SessionId=session,Revision=revision,Player=combat.Player});if(disposed)return;
                SquadChanged?.Invoke(new CombatSquadSnapshotDto {SessionId=session,Revision=revision,Squad=squadData});if(disposed)return;
                VisualChanged?.Invoke(visual);if(!disposed&&newResult)ResultReady?.Invoke(result);
            }
            finally{publishing=false;}
        }
        IReadOnlyList<FloorDto> BuildFloors()=>Array.AsReadOnly(definition.Floors.Select(f=>new FloorDto {FloorId=f.FloorId,DisplayName=f.DisplayName,
            MiniMap=BuildMap(f.FloorId),Rooms=f.Rooms.Select(r=>new RoomDto {RoomId=r.RoomId,DisplayName=r.DisplayName,MapPosition=r.MapPosition,
                DoorOpen=rooms[r.RoomId].Open,SearchState=rooms[r.RoomId].State,HasPossibleEnemyArea=r.HasPossibleEnemyArea||definition.SpawnPoints.Any(p=>p.RoomId==r.RoomId)}).ToArray()}).ToArray());
        MiniMapDto BuildMap(string floorId)
        {
            var markers=new List<MapMarkerDto>();
            if(floorId.Length==0&&definition.EntranceWorldPosition.HasValue)markers.Add(Marker(definition.EntranceId,MarkerType.BuildingEntrance,definition.EntranceWorldPosition.Value,floorId,"建筑入口"));
            foreach(var p in definition.SpawnPoints.Where(p=>p.FloorId==floorId).GroupBy(p=>p.EstimateAreaId).Select(g=>g.First()))markers.Add(Marker(p.EstimateAreaId,MarkerType.EnemyEstimate,p.EstimatePosition,floorId,"敌情预估"));
            foreach(var id in killed.OrderBy(k=>k,StringComparer.Ordinal)){var e=enemies[id];if(e.FloorId==floorId)markers.Add(Marker(id,MarkerType.EnemyKilled,e.EstimatePosition,floorId,"已消灭"));}
            foreach(var room in rooms.Values.Where(r=>r.FloorId==floorId&&r.Definition.MapPosition.HasValue))markers.Add(new MapMarkerDto
            {MarkerId=room.Definition.RoomId,Type=room.State==RoomSearchState.Searched?MarkerType.SearchedRoom:MarkerType.UnsearchedRoom,NormalizedPosition=room.Definition.MapPosition.Value,Label=room.Definition.DisplayName});
            if(session.Length>0&&floorId==(phase==UrbanPhase.Street?"":floor))foreach(var member in squad.GetSquadStatus(session).Data.Members)markers.Add(Marker(member.MemberId,member.Role==SquadMemberRole.Player?MarkerType.Player:MarkerType.Teammate,member.WorldPosition,floorId,member.Role.ToString()));
            return new MiniMapDto {Visible=true,MapId=floorId.Length==0?definition.MapId:floorId,Markers=markers.AsReadOnly(),Areas=Array.Empty<MapAreaDto>()};
        }
        MapMarkerDto Marker(string id,MarkerType type,Vector3 p,string floorId,string label)
        {var b=definition.Projections.First(f=>f.FloorId==floorId).WorldBoundsXZ;return new MapMarkerDto {MarkerId=id,Type=type,Label=label,NormalizedPosition=new Vector2(Mathf.Clamp01((p.x-b.x)/b.width),Mathf.Clamp01((p.z-b.y)/b.height))};}
        UrbanMapDto Map()=>new UrbanMapDto {MapId=definition.MapId,DisplayName="城镇地图 A",BuildingEntranceId=definition.EntranceId,
            StreetEnemyMin=1,StreetEnemyMax=2,BuildingEnemyMin=3,BuildingEnemyMax=6,Floors=definition.Floors};
        bool KnownArea(string id)=>id==definition.EntranceId||definition.Floors.Any(f=>f.FloorId==id)||rooms.Keys.Any(r=>r+".door"==id||r+".check"==id);
        static HudTextLineDto Line(string key,string label,string value)=>new HudTextLineDto {Key=key,Label=label,Value=value};
        ServiceResult<Unit> ValidateMap(string map)
        {if(disposed)return Fail<Unit>(ErrorCode.InvalidState);if(string.IsNullOrWhiteSpace(map))return Fail<Unit>(ErrorCode.InvalidInput);if(map!="urban-a")return Fail<Unit>(ErrorCode.NotFound);return definition.Mode!=TrainingMode.Urban?Fail<Unit>(ErrorCode.InvalidInput):definition.Validate(config);}
        ServiceResult<Unit> RoomGuard(string id,string room){var guard=Guard(id);return !guard.Success?guard:!rooms.ContainsKey(room??"")?Fail<Unit>(ErrorCode.NotFound):Ok();}
        ServiceResult<Unit> Guard(string id)
        {
            if(!Matches(id))return Fail<Unit>(ErrorCode.NotFound);if(publishing||batch)return Fail<Unit>(ErrorCode.Busy);
            return double.IsNaN(clock.Now)||double.IsInfinity(clock.Now)||clock.Now<lastNow?Fail<Unit>(ErrorCode.InvalidInput):Ok();
        }
        ServiceResult<Unit> Boundary(string id){var guard=Guard(id);return !guard.Success?guard:pending.Count>0||core.HasPendingInputs?Fail<Unit>(ErrorCode.Busy):Ok();}
        bool Matches(string id)=>!disposed&&session.Length>0&&session==id;
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<T> Fail<T>(ErrorCode code)=>ServiceResult<T>.Fail(code,"Urban request rejected: "+code);
        public void Dispose(){if(disposed)return;disposed=true;core.Changed-=OnCore;squad.Changed-=OnSquad;core.Dispose();squad.Dispose();pending.Clear();received.Clear();SessionChanged=null;ResultReady=null;HudUpdated=null;PlayerChanged=null;SquadChanged=null;VisualChanged=null;}
        sealed class RoomState{public RoomDto Definition;public string FloorId;public bool Open;public RoomSearchState State;}
    }
}
