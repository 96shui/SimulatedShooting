using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD12/14/15/17/24. Owns one combat session and evaluates outcomes only at a settled batch boundary.</summary>
    public sealed class TrenchService : ITrenchService, ICombatWorldInputPort, ICombatStateService, IHUDService, IDisposable
    {
        readonly CombatSceneDefinitionDto definition;
        readonly CombatConfigDto config;
        readonly ICombatClock clock;
        readonly ICombatRandom random;
        readonly Func<string> nextSessionId;
        readonly CombatCoreService core;
        readonly SquadFormationService squad;
        readonly HashSet<string> searched=new HashSet<string>(), inRange=new HashSet<string>(), killed=new HashSet<string>();
        readonly Dictionary<string,Vector3> estimates=new Dictionary<string,Vector3>();
        readonly Dictionary<string,CombatInputDto> received=new Dictionary<string,CombatInputDto>();
        readonly List<CombatInputDto> pending=new List<CombatInputDto>();
        string session="";
        long revision;
        double elapsed,lastNow;
        bool disposed,batch,starting,publishing,dirty,cancelled,hasResult;
        bool? pendingOutcome;
        CombatCoreSnapshotDto combat;
        TrenchSessionDto snapshot;
        TrenchResultDto result;
        HudDto hud;
        CombatVisualSnapshotDto visual;
        public ICombatCoreService Combat=>core;
        public ISquadCommandService SquadCommands=>squad;
        public event Action<TrenchSessionDto> SessionChanged;
        public event Action<TrenchResultDto> ResultReady;
        public event Action<HudDto> HudUpdated;
        public event Action<CombatPlayerSnapshotDto> PlayerChanged;
        public event Action<CombatSquadSnapshotDto> SquadChanged;
        public event Action<CombatVisualSnapshotDto> VisualChanged;

        public TrenchService(CombatSceneDefinitionDto definition,ICombatClock clock,ICombatRandom random,
            ICombatNavigationPort navigation,CombatConfigDto? config=null,Func<string> sessionIdFactory=null)
        {
            this.definition=definition; this.clock=clock??throw new ArgumentNullException(nameof(clock));
            this.random=random??throw new ArgumentNullException(nameof(random)); this.config=config??CombatConfigDto.Default;
            nextSessionId=sessionIdFactory??(()=>Guid.NewGuid().ToString("N"));
            core=new CombatCoreService(clock,this.config); squad=new SquadFormationService(clock,navigation,this.config);
            core.Changed+=OnCore; squad.Changed+=OnSquad;
        }
        public ServiceResult<IReadOnlyList<TrenchMapDto>> GetMaps()
        {
            var valid=ValidateMap("trench-a");
            return valid.Success?ServiceResult<IReadOnlyList<TrenchMapDto>>.Ok(Array.AsReadOnly(new[]{Map()})):Fail<IReadOnlyList<TrenchMapDto>>(valid.ErrorCode);
        }
        public ServiceResult<TrenchMapDto> SelectMap(string mapId)
        {var valid=ValidateMap(mapId);return valid.Success?ServiceResult<TrenchMapDto>.Ok(Map()):Fail<TrenchMapDto>(valid.ErrorCode);}
        public ServiceResult<TrenchBriefingDto> GetBriefing(string mapId,string weaponId,RandomSeed seed)
        {
            var valid=ValidateStart(mapId,weaponId); if(!valid.Success)return Fail<TrenchBriefingDto>(valid.ErrorCode);
            var members=new[]{new SquadMemberDto {MemberId="preview.player",Role=SquadMemberRole.Player,Health=config.PlayerHealth},
                new SquadMemberDto {MemberId="preview.teammate-2",Role=SquadMemberRole.TeammateTwo,Health=config.PlayerHealth,WorldPosition=Vector3.back*config.SquadSpacing},
                new SquadMemberDto {MemberId="preview.teammate-3",Role=SquadMemberRole.TeammateThree,Health=config.PlayerHealth,WorldPosition=Vector3.back*config.SquadSpacing*2}};
            return ServiceResult<TrenchBriefingDto>.Ok(new TrenchBriefingDto {MapId=mapId,EnemyEstimateMin=3,EnemyEstimateMax=5,
                PlannedSquad=new SquadStatusDto {Members=Array.AsReadOnly(members),CommandMenuAvailable=false},ProjectedMap=BuildMap(true)});
        }
        public ServiceResult<TrenchSessionDto> StartSession(string mapId,string weaponId,RandomSeed seed)
        {
            var valid=ValidateStart(mapId,weaponId); if(!valid.Success)return Fail<TrenchSessionDto>(valid.ErrorCode);
            if(publishing||batch||starting||(!cancelled&&!hasResult&&session.Length>0))return Fail<TrenchSessionDto>(ErrorCode.Busy);
            var id=nextSessionId(); if(string.IsNullOrWhiteSpace(id))return Fail<TrenchSessionDto>(ErrorCode.InvalidInput);
            var candidates=definition.SpawnPoints.OrderBy(p=>p.PointId,StringComparer.Ordinal).ToList();
            random.Reset(seed); var count=random.NextInt(3,6);
            if(count<3||count>5)return Fail<TrenchSessionDto>(ErrorCode.InvalidInput);
            var spawns=new List<CombatEntityVisualDto>(); var selected=new Dictionary<string,Vector3>();
            for(var i=0;i<count;i++)
            {
                var index=random.NextInt(0,candidates.Count); if(index<0||index>=candidates.Count)return Fail<TrenchSessionDto>(ErrorCode.InvalidInput);
                var point=candidates[index];candidates.RemoveAt(index);var enemyId=id+".enemy-"+(i+1).ToString("000",CultureInfo.InvariantCulture);
                spawns.Add(new CombatEntityVisualDto {EntityId=enemyId,Role=CombatEntityRole.Enemy,Position=point.WorldPosition,Forward=Vector3.forward});
                selected.Add(enemyId,point.EstimatePosition);
            }
            starting=true;
            try
            {
                var started=core.Start(id,TrainingMode.Trench,spawns); if(!started.Success)return Fail<TrenchSessionDto>(started.ErrorCode);
                session=id;combat=started.Data;revision=0;elapsed=0;lastNow=clock.Now;cancelled=hasResult=false;pendingOutcome=null;
                searched.Clear();inRange.Clear();killed.Clear();estimates.Clear();received.Clear();pending.Clear();
                foreach(var pair in selected)estimates.Add(pair.Key,pair.Value);
                squad.Start(id,Vector3.zero,Vector3.forward);dirty=true;
            }
            finally {starting=false;}
            Publish();return ServiceResult<TrenchSessionDto>.Ok(snapshot);
        }
        public ServiceResult<TrenchSessionDto> GetSession(string id)=>Matches(id)?ServiceResult<TrenchSessionDto>.Ok(snapshot):Fail<TrenchSessionDto>(ErrorCode.NotFound);
        public ServiceResult<TrenchResultDto> GetResult(string id)=>Matches(id)&&hasResult?ServiceResult<TrenchResultDto>.Ok(result):Fail<TrenchResultDto>(ErrorCode.NotFound);
        public ServiceResult<HudDto> GetHud(string id)=>Matches(id)?ServiceResult<HudDto>.Ok(hud):Fail<HudDto>(ErrorCode.NotFound);
        public ServiceResult<CombatPlayerSnapshotDto> GetPlayer(string id)=>Matches(id)?ServiceResult<CombatPlayerSnapshotDto>.Ok(new CombatPlayerSnapshotDto {SessionId=session,Revision=revision,Player=snapshot.Player}):Fail<CombatPlayerSnapshotDto>(ErrorCode.NotFound);
        public ServiceResult<CombatSquadSnapshotDto> GetSquadStatus(string id)=>Matches(id)?ServiceResult<CombatSquadSnapshotDto>.Ok(new CombatSquadSnapshotDto {SessionId=session,Revision=revision,Squad=snapshot.Squad}):Fail<CombatSquadSnapshotDto>(ErrorCode.NotFound);
        public ServiceResult<CombatVisualSnapshotDto> GetVisualSnapshot(string id)=>Matches(id)?ServiceResult<CombatVisualSnapshotDto>.Ok(visual):Fail<CombatVisualSnapshotDto>(ErrorCode.NotFound);

        public ServiceResult<Unit> Submit(CombatInputDto input)
        {
            var valid=Guard(input.SessionId);if(!valid.Success)return valid;
            if(cancelled||hasResult||combat.State!=SessionState.Running||!combat.TrackingValid)return Fail<Unit>(ErrorCode.InvalidState);
            if(string.IsNullOrWhiteSpace(input.EventId))return Fail<Unit>(ErrorCode.InvalidInput);
            if(received.TryGetValue(input.EventId,out var old))return old.Equals(input)?Ok():Fail<Unit>(ErrorCode.InvalidInput);
            if(input.Kind!=CombatInputKind.AreaPresence&&input.Kind!=CombatInputKind.NavigationResult)
            {
                var accepted=core.Submit(input);if(accepted.Success)received.Add(input.EventId,input);return accepted;
            }
            if(input.Tick!=clock.Tick)return Fail<Unit>(ErrorCode.InvalidState);
            if(input.Kind==CombatInputKind.AreaPresence&&!definition.SearchNodes.Any(n=>n.NodeId==input.EntityId))return Fail<Unit>(ErrorCode.NotFound);
            if(input.Kind==CombatInputKind.NavigationResult)
            {
                // Queue only structurally valid feedback. Request correlation is checked by the squad at the boundary.
                if(input.EntityId!=session+".teammate-2"&&input.EntityId!=session+".teammate-3")return Fail<Unit>(ErrorCode.NotFound);
                if(string.IsNullOrWhiteSpace(input.TargetId)||!Finite(input.Position)||!Finite(input.Direction)||input.Direction.sqrMagnitude<1e-8)return Fail<Unit>(ErrorCode.InvalidInput);
                var arrival=squad.ValidateArrival(input);if(!arrival.Success)return arrival;
            }
            received.Add(input.EventId,input);pending.Add(input);return Ok();
        }
        public ServiceResult<Unit> Advance(string id)
        {
            var valid=Guard(id);if(!valid.Success)return valid;
            if(hasResult||cancelled)return Ok();
            if(pending.Any(p=>p.Tick!=clock.Tick))return Fail<Unit>(ErrorCode.InvalidState);
            batch=true;
            try
            {
                var advanced=core.Advance(id);if(!advanced.Success)return advanced;
                AccumulateTime();combat=core.GetSnapshot(id).Data;
                foreach(var input in pending)
                {
                    if(input.Kind==CombatInputKind.AreaPresence)
                    {
                        if(input.Flag){inRange.Add(input.EntityId);dirty|=searched.Add(input.EntityId);}else inRange.Remove(input.EntityId);
                    }
                    else if(combat.State==SessionState.Running&&combat.TrackingValid)squad.Submit(input);
                }
                pending.Clear();ReconcileKills();UpdateSquad();Evaluate();
            }
            finally {batch=false;}
            Publish();return Ok();
        }
        public ServiceResult<TrenchSessionDto> MarkSearchNode(string id,string nodeId)
        {
            var valid=Boundary(id);if(!valid.Success)return Fail<TrenchSessionDto>(valid.ErrorCode);
            if(string.IsNullOrWhiteSpace(nodeId))return Fail<TrenchSessionDto>(ErrorCode.InvalidInput);
            if(!definition.SearchNodes.Any(n=>n.NodeId==nodeId))return Fail<TrenchSessionDto>(ErrorCode.NotFound);
            if(searched.Contains(nodeId))return ServiceResult<TrenchSessionDto>.Ok(snapshot);
            if(cancelled||hasResult||!inRange.Contains(nodeId)||combat.State!=SessionState.Running)return Fail<TrenchSessionDto>(ErrorCode.InvalidState);
            searched.Add(nodeId);dirty=true;Publish();return ServiceResult<TrenchSessionDto>.Ok(snapshot);
        }
        public ServiceResult<TrenchSessionDto> RegisterEnemyKilled(string id,string enemyId)
        {
            var valid=Boundary(id);if(!valid.Success)return Fail<TrenchSessionDto>(valid.ErrorCode);
            if(string.IsNullOrWhiteSpace(enemyId))return Fail<TrenchSessionDto>(ErrorCode.InvalidInput);
            if(!estimates.ContainsKey(enemyId))return Fail<TrenchSessionDto>(ErrorCode.NotFound);
            if(killed.Contains(enemyId))return ServiceResult<TrenchSessionDto>.Ok(snapshot);
            if(cancelled||hasResult||!combat.Visual.Entities.Any(e=>e.EntityId==enemyId&&e.State==CombatEntityState.Dead))return Fail<TrenchSessionDto>(ErrorCode.InvalidState);
            killed.Add(enemyId);dirty=true;Publish();return ServiceResult<TrenchSessionDto>.Ok(snapshot);
        }
        public ServiceResult<TrenchResultDto> CompleteIfReady(string id)
        {
            var valid=Boundary(id);if(!valid.Success)return Fail<TrenchResultDto>(valid.ErrorCode);
            if(hasResult)return ServiceResult<TrenchResultDto>.Ok(result);
            if(cancelled)return Fail<TrenchResultDto>(ErrorCode.InvalidState);
            batch=true;try{ReconcileKills();Evaluate();}finally{batch=false;}Publish();
            return hasResult?ServiceResult<TrenchResultDto>.Ok(result):Fail<TrenchResultDto>(ErrorCode.InvalidState);
        }
        public ServiceResult<TrenchResultDto> FailByPlayerDeath(string id)
        {
            var valid=Boundary(id);if(!valid.Success)return Fail<TrenchResultDto>(valid.ErrorCode);
            if(hasResult)return ServiceResult<TrenchResultDto>.Ok(result);
            if(cancelled||combat.Player.IsAlive)return Fail<TrenchResultDto>(ErrorCode.InvalidState);
            return CompleteIfReady(id);
        }
        public ServiceResult<Unit> Cancel(string id)
        {
            var valid=Guard(id);if(!valid.Success)return valid;if(cancelled)return Ok();
            batch=true;try{AccumulateTime();cancelled=true;pending.Clear();inRange.Clear();core.SetState(id,SessionState.Cancelled);squad.Stop();dirty=true;}finally{batch=false;}
            Publish();return Ok();
        }
        void Evaluate()
        {
            if(hasResult||pendingOutcome.HasValue||cancelled)return;
            if(!combat.Player.IsAlive)Finish(false);
            else if(combat.State==SessionState.Running&&searched.Count==definition.SearchNodes.Count&&killed.Count==estimates.Count)Finish(true);
        }
        void Finish(bool victory)
        {
            AccumulateTime();pendingOutcome=victory;
            core.SetState(session,victory?SessionState.Completed:SessionState.Failed);squad.Stop();dirty=true;
        }
        void ReconcileKills(){foreach(var entity in combat.Visual.Entities)if(entity.Role==CombatEntityRole.Enemy&&entity.State==CombatEntityState.Dead)dirty|=killed.Add(entity.EntityId);}
        void OnCore(CombatCoreSnapshotDto value)
        {
            if(starting)return;
            if(disposed||value.SessionId!=session||hasResult||cancelled)return;
            AccumulateTime();combat=value;dirty=true;
            // Death inside Advance must retain this batch's search facts for the failure statistics.
            if(!batch&&(value.State!=SessionState.Running||!value.TrackingValid)){pending.Clear();inRange.Clear();}
            if(!batch){batch=true;try{UpdateSquad();}finally{batch=false;}Publish();}
        }
        void OnSquad(CombatSquadSnapshotDto value){if(starting||value.SessionId!=session||disposed)return;dirty=true;if(!batch)Publish();}
        void UpdateSquad()
        {
            var player=combat.Visual.Entities[0];
            squad.UpdatePlayer(player.Position,player.Forward,!cancelled&&!hasResult&&combat.State==SessionState.Running&&combat.TrackingValid,combat.Player.Health);
        }
        void AccumulateTime()
        {
            if(!hasResult&&!cancelled&&combat.State==SessionState.Running&&combat.TrackingValid&&clock.Now>=lastNow)
            {elapsed+=clock.Now-lastNow;if(clock.Now>lastNow)dirty=true;}
            lastNow=clock.Now;
        }
        void Publish()
        {
            if(!dirty||starting||disposed||session.Length==0)return;dirty=false;revision++;
            var squadData=squad.GetSquadStatus(session).Data;
            var map=BuildMap(false);
            snapshot=new TrenchSessionDto {SessionId=session,Revision=revision,MapId=definition.MapId,State=cancelled?SessionState.Cancelled:combat.State,
                EnemyTotal=estimates.Count,EnemyKilled=killed.Count,SearchProgress01=(float)searched.Count/definition.SearchNodes.Count,
                Ammo=combat.Ammo,Player=combat.Player,Squad=squadData,MiniMap=map};
            hud=new HudDto {SessionId=session,Mode=TrainingMode.Trench,HudType=HudType.Trench,Ammo=combat.Ammo,Player=combat.Player,MiniMap=map,
                CanShoot=!cancelled&&!hasResult&&!pendingOutcome.HasValue&&combat.Weapon.CanShoot,Prompts=Array.Empty<HudPromptDto>(),
                TextLines=Array.AsReadOnly(new[]{Line("Enemies","消灭敌人",killed.Count+"/"+estimates.Count),Line("Search","搜索节点",searched.Count+"/"+definition.SearchNodes.Count),
                    Line("Elapsed","用时",elapsed.ToString("F1",CultureInfo.InvariantCulture)),Line("Squad","队伍",string.Join(" / ",squadData.Members.Select(m=>m.State.ToString())))})};
            visual=new CombatVisualSnapshotDto {SessionId=session,Revision=revision,Entities=combat.Visual.Entities.Concat(squad.GetVisualSnapshot(session).Data.Entities).ToArray()};
            bool newResult=pendingOutcome.HasValue;
            if(newResult)
            {
                result=new TrenchResultDto {SessionId=session,Revision=revision,Victory=pendingOutcome.Value,MapName="堑壕地图 A",EnemyKilled=killed.Count,EnemyTotal=estimates.Count,
                    SearchProgress01=snapshot.SearchProgress01,RemainingAmmo=combat.Ammo.CurrentMagazine+combat.Ammo.ReserveAmmo,Squad=squadData,ElapsedSeconds=(float)elapsed,ResultMap=map};
                hasResult=true;pendingOutcome=null;
            }
            publishing=true;
            try
            {
                SessionChanged?.Invoke(snapshot);if(disposed)return;HudUpdated?.Invoke(hud);if(disposed)return;
                PlayerChanged?.Invoke(new CombatPlayerSnapshotDto {SessionId=session,Revision=revision,Player=snapshot.Player});if(disposed)return;
                SquadChanged?.Invoke(new CombatSquadSnapshotDto {SessionId=session,Revision=revision,Squad=squadData});if(disposed)return;
                VisualChanged?.Invoke(visual);if(!disposed&&newResult)ResultReady?.Invoke(result);
            }
            finally{publishing=false;}
        }
        TrenchMapDto Map()=>new TrenchMapDto {MapId=definition.MapId,DisplayName="堑壕地图 A",Difficulty=DifficultyLevel.Medium,MinEnemyCount=3,MaxEnemyCount=5,
            SearchNodes=definition.SearchNodes,EnemyEstimateAreas=EstimateAreas()};
        IReadOnlyList<MapAreaDto> EstimateAreas()=>Array.AsReadOnly(definition.SpawnPoints.GroupBy(p=>p.EstimateAreaId).Select(g=>
        {var p=Project(g.First().EstimatePosition);return new MapAreaDto {AreaId=g.Key,Label="敌情预估",Severity=HudSeverity.Danger,NormalizedRect=new Rect(p.x-.02f,p.y-.02f,.04f,.04f)};}).ToArray());
        MiniMapDto BuildMap(bool preview)
        {
            var markers=new List<MapMarkerDto>();
            foreach(var point in definition.SpawnPoints.GroupBy(p=>p.EstimateAreaId).Select(g=>g.First()))markers.Add(Marker(point.EstimateAreaId,MarkerType.EnemyEstimate,point.EstimatePosition,"敌情预估"));
            var areas=EstimateAreas().ToList();
            foreach(var node in definition.SearchNodes)
            {
                var done=!preview&&searched.Contains(node.NodeId);markers.Add(Marker(node.NodeId,done?MarkerType.SearchedRoom:MarkerType.Objective,node.WorldPosition,done?"已搜索":"搜索节点"));
                if(done){var p=Project(node.WorldPosition);areas.Add(new MapAreaDto {AreaId=node.NodeId,Label="已搜索",Severity=HudSeverity.Info,NormalizedRect=new Rect(p.x-.015f,p.y-.015f,.03f,.03f)});}
            }
            if(!preview&&session.Length>0)
            {
                foreach(var member in squad.GetSquadStatus(session).Data.Members)markers.Add(Marker(member.MemberId,member.Role==SquadMemberRole.Player?MarkerType.Player:MarkerType.Teammate,member.WorldPosition,member.Role.ToString()));
                foreach(var enemy in killed.OrderBy(e=>e,StringComparer.Ordinal))markers.Add(Marker(enemy,MarkerType.EnemyKilled,estimates[enemy],"已消灭"));
            }
            if(preview)
            {
                markers.Add(Marker("preview.player",MarkerType.Player,Vector3.zero,"入口/玩家"));
                markers.Add(Marker("preview.teammate-2",MarkerType.Teammate,Vector3.back*config.SquadSpacing,"二号"));
                markers.Add(Marker("preview.teammate-3",MarkerType.Teammate,Vector3.back*config.SquadSpacing*2,"三号"));
            }
            return new MiniMapDto {MapId=definition.MapId,Visible=true,Markers=markers.AsReadOnly(),Areas=areas.AsReadOnly()};
        }
        MapMarkerDto Marker(string id,MarkerType type,Vector3 position,string label)=>new MapMarkerDto {MarkerId=id,Type=type,NormalizedPosition=Project(position),Label=label};
        Vector2 Project(Vector3 position)
        {var bounds=definition.Projections.First(p=>p.FloorId.Length==0).WorldBoundsXZ;return new Vector2(Mathf.Clamp01((position.x-bounds.x)/bounds.width),Mathf.Clamp01((position.z-bounds.y)/bounds.height));}
        static HudTextLineDto Line(string key,string label,string value)=>new HudTextLineDto {Key=key,Label=label,Value=value};
        ServiceResult<Unit> ValidateMap(string mapId)
        {
            if(disposed)return Fail<Unit>(ErrorCode.InvalidState);
            if(string.IsNullOrWhiteSpace(mapId))return Fail<Unit>(ErrorCode.InvalidInput);
            if(mapId!="trench-a")return Fail<Unit>(ErrorCode.NotFound);
            if(definition.Mode!=TrainingMode.Trench)return Fail<Unit>(ErrorCode.InvalidInput);
            return definition.Validate(config);
        }
        ServiceResult<Unit> ValidateStart(string map,string weapon)
        {var valid=ValidateMap(map);if(!valid.Success)return valid;return string.IsNullOrWhiteSpace(weapon)?Fail<Unit>(ErrorCode.InvalidState):weapon!="training-rifle"?Fail<Unit>(ErrorCode.NotFound):Ok();}
        ServiceResult<Unit> Guard(string id)=>!Matches(id)?Fail<Unit>(ErrorCode.NotFound):publishing||batch?Fail<Unit>(ErrorCode.Busy):Ok();
        ServiceResult<Unit> Boundary(string id){var guard=Guard(id);return !guard.Success?guard:pending.Count>0||core.HasPendingInputs?Fail<Unit>(ErrorCode.Busy):Ok();}
        bool Matches(string id)=>!disposed&&session.Length>0&&session==id;
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<T> Fail<T>(ErrorCode code)=>ServiceResult<T>.Fail(code,"Trench request rejected: "+code);
        public void Dispose()
        {
            if(disposed)return;disposed=true;core.Changed-=OnCore;squad.Changed-=OnSquad;core.Dispose();squad.Dispose();pending.Clear();received.Clear();
            SessionChanged=null;ResultReady=null;HudUpdated=null;PlayerChanged=null;SquadChanged=null;VisualChanged=null;
        }
    }
}
