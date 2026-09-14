using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD24: path targets are intentions; only acknowledged navigation changes actor positions.</summary>
    public sealed class SquadFormationService : ISquadCommandService, ICombatWorldInputPort, IDisposable
    {
        readonly ICombatClock clock;
        readonly ICombatNavigationPort navigation;
        readonly CombatConfigDto config;
        readonly List<Vector3> path = new List<Vector3>();
        readonly Member[] members = {new Member(),new Member()};
        readonly Dictionary<string,CombatInputDto> acknowledged = new Dictionary<string,CombatInputDto>();
        string session="";
        Vector3 playerPosition, playerForward;
        float playerHealth;
        long revision, sequence;
        bool active, disposed, publishing, dirty;
        SquadStatusDto snapshot;
        CombatVisualSnapshotDto visual;
        public event Action<CombatSquadSnapshotDto> Changed;
        public event Action<CombatNavigationRequestDto> NavigationFailed;
        public SquadFormationService(ICombatClock clock,ICombatNavigationPort navigation,CombatConfigDto? config=null)
        {
            this.clock=clock??throw new ArgumentNullException(nameof(clock));
            this.navigation=navigation??throw new ArgumentNullException(nameof(navigation)); this.config=config??CombatConfigDto.Default;
            if(!this.config.Validate().Success) throw new ArgumentException("Invalid configuration");
        }
        public ServiceResult<Unit> Start(string id,Vector3 position,Vector3 forward)
        {
            if(disposed||publishing) return Fail(ErrorCode.InvalidState);
            if(string.IsNullOrWhiteSpace(id)||!Finite(position)||!Direction(forward)) return Fail(ErrorCode.InvalidInput);
            session=id; playerPosition=position; playerForward=forward.normalized; playerHealth=config.PlayerHealth;
            revision=sequence=0; active=true; acknowledged.Clear(); path.Clear();
            path.Add(position-playerForward*config.SquadSpacing*2); path.Add(position);
            for(var i=0;i<2;i++)
            {
                members[i]=new Member { Id=session+".teammate-"+(i+2), Position=position-playerForward*config.SquadSpacing*(i+1),
                    Forward=i==0?playerForward:-playerForward, State=SquadMemberState.Following };
            }
            dirty=true; Publish(); return Ok();
        }
        public ServiceResult<Unit> UpdatePlayer(Vector3 position,Vector3 forward,bool canMove,float health)
        {
            if(disposed||session.Length==0||publishing) return Fail(ErrorCode.InvalidState);
            if(!Finite(position)||!Direction(forward)||!Finite(health)||health<0) return Fail(ErrorCode.InvalidInput);
            if(playerPosition!=position||playerForward!=forward.normalized||playerHealth!=health) dirty=true;
            playerPosition=position; playerForward=forward.normalized; playerHealth=health;
            if(Vector3.Distance(path[path.Count-1],position)>.0001f) path.Add(position);
            active=canMove&&health>0;
            for(var i=0;i<2;i++)
            {
                if(disposed)return Fail(ErrorCode.InvalidState);
                var member=members[i];
                if(!active)
                {
                    if(member.Waiting||member.State!=SquadMemberState.HoldingPosition) dirty=true;
                    member.Waiting=false; member.State=SquadMemberState.HoldingPosition; member.RetryAt=clock.Now; continue;
                }
                if(member.Waiting)
                {
                    if(clock.Now+1e-8>=member.ExpiresAt) Failure(member);
                    continue;
                }
                if(clock.Now+1e-8<member.RetryAt) continue;
                var target=Sample(config.SquadSpacing*(i+1),out var heading);
                if(i==1) heading=-heading;
                if(Vector3.Distance(target,member.Position)<.01f && Vector3.Angle(member.Forward,heading)<1) continue;
                var request=new CombatNavigationRequestDto {SessionId=session,RequestId=session+".nav-"+ ++sequence,
                    EntityId=member.Id,Tick=clock.Tick,Destination=target,Forward=heading};
                member.Request=request; member.Waiting=true; member.ExpiresAt=clock.Now+config.NavigationRetrySeconds;
                member.State=SquadMemberState.Following; dirty=true;
                if(!navigation.Move(request).Success) Failure(member);
            }
            if(disposed)return Fail(ErrorCode.InvalidState);
            TrimPath(); Publish(); return Ok();
        }
        public ServiceResult<Unit> Submit(CombatInputDto input)
        {
            var valid=ValidateArrival(input);if(!valid.Success)return valid;
            if(acknowledged.ContainsKey(input.EventId))return Ok();
            var member=Array.Find(members,m=>m.Id==input.EntityId);
            acknowledged.Add(input.EventId,input);
            if(input.Flag)
            {
                member.Position=input.Position; member.Forward=input.Direction.normalized; member.Waiting=false;
                member.State=SquadMemberState.Following; member.RetryAt=clock.Now; dirty=true;
            }
            else Failure(member);
            Publish(); return Ok();
        }
        internal ServiceResult<Unit> ValidateArrival(CombatInputDto input)
        {
            if(!Matches(input.SessionId)) return Fail(ErrorCode.NotFound);
            if(publishing) return Fail(ErrorCode.Busy);
            if(string.IsNullOrWhiteSpace(input.EventId)) return Fail(ErrorCode.InvalidInput);
            if(acknowledged.TryGetValue(input.EventId,out var old)) return old.Equals(input)?Ok():Fail(ErrorCode.InvalidInput);
            if(!active||input.Kind!=CombatInputKind.NavigationResult||input.Tick!=clock.Tick) return Fail(ErrorCode.InvalidState);
            if(!Finite(input.Position)||!Direction(input.Direction)) return Fail(ErrorCode.InvalidInput);
            Member member=null; foreach(var candidate in members) if(candidate.Id==input.EntityId) member=candidate;
            if(member==null) return Fail(ErrorCode.NotFound);
            if(!member.Waiting||member.Request.RequestId!=input.TargetId) return Fail(ErrorCode.InvalidState);
            return Ok();
        }
        void Failure(Member member)
        {
            member.Waiting=false; member.RetryAt=clock.Now+config.NavigationRetrySeconds;
            member.State=SquadMemberState.HoldingPosition; dirty=true;
            publishing=true;
            try { NavigationFailed?.Invoke(member.Request); } finally { publishing=false; }
        }
        Vector3 Sample(float distance,out Vector3 heading)
        {
            for(var i=path.Count-1;i>0;i--)
            {
                var segment=path[i]-path[i-1]; var length=segment.magnitude;
                if(length<.0001f) continue;
                if(distance<=length) { heading=segment/length; return path[i]-heading*distance; }
                distance-=length;
            }
            heading=(path[1]-path[0]).normalized; return path[0]-heading*distance;
        }
        void TrimPath()
        {
            float total=0; for(var i=1;i<path.Count;i++) total+=Vector3.Distance(path[i],path[i-1]);
            while(path.Count>2)
            {
                var first=Vector3.Distance(path[0],path[1]);
                if(total-first<config.SquadSpacing*2) break; total-=first; path.RemoveAt(0);
            }
        }
        public ServiceResult<SquadStatusDto> GetSquadStatus(string id)=>Matches(id)?ServiceResult<SquadStatusDto>.Ok(snapshot):ServiceResult<SquadStatusDto>.Fail(ErrorCode.NotFound);
        public ServiceResult<CombatVisualSnapshotDto> GetVisualSnapshot(string id)=>Matches(id)?ServiceResult<CombatVisualSnapshotDto>.Ok(visual):ServiceResult<CombatVisualSnapshotDto>.Fail(ErrorCode.NotFound);
        public ServiceResult<IReadOnlyList<SquadCommandType>> GetAvailableCommands(string id)=>Matches(id)?ServiceResult<IReadOnlyList<SquadCommandType>>.Ok(Array.Empty<SquadCommandType>()):ServiceResult<IReadOnlyList<SquadCommandType>>.Fail(ErrorCode.NotFound);
        public ServiceResult<SquadCommandResult> Issue(SquadCommandRequest request)=>ServiceResult<SquadCommandResult>.Fail(Matches(request.SessionId)?ErrorCode.InvalidState:ErrorCode.NotFound);
        public ServiceResult<SquadStatusDto> OnReloadStarted(string id)=>ServiceResult<SquadStatusDto>.Fail(Matches(id)?ErrorCode.InvalidState:ErrorCode.NotFound);
        public void Stop() { if(Matches(session)) UpdatePlayer(playerPosition,playerForward,false,playerHealth); }
        void Publish()
        {
            if(!dirty||disposed) return; revision++; dirty=false;
            var data=new SquadMemberDto[3]; var entities=new CombatEntityVisualDto[2];
            data[0]=new SquadMemberDto { MemberId=session+".player",Role=SquadMemberRole.Player,Health=playerHealth,WorldPosition=playerPosition,
                State=playerHealth>0?SquadMemberState.Normal:SquadMemberState.Down };
            for(var i=0;i<2;i++)
            {
                var member=members[i]; data[i+1]=new SquadMemberDto {MemberId=member.Id,Role=i==0?SquadMemberRole.TeammateTwo:SquadMemberRole.TeammateThree,
                    WorldPosition=member.Position,State=member.State,Health=config.PlayerHealth};
                entities[i]=new CombatEntityVisualDto {EntityId=member.Id,Role=CombatEntityRole.Teammate,Position=member.Position,
                    Forward=member.Forward,State=CombatEntityState.Idle};
            }
            snapshot=new SquadStatusDto {Members=Array.AsReadOnly(data),CommandMenuAvailable=false};
            visual=new CombatVisualSnapshotDto {SessionId=session,Revision=revision,Entities=entities};
            publishing=true; try { Changed?.Invoke(new CombatSquadSnapshotDto {SessionId=session,Revision=revision,Squad=snapshot}); } finally {publishing=false;}
        }
        bool Matches(string id)=>!disposed&&session.Length>0&&session==id;
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        static bool Direction(Vector3 v)=>Finite(v)&&Finite(v.sqrMagnitude)&&v.sqrMagnitude>.000001f;
        static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<Unit> Fail(ErrorCode code)=>ServiceResult<Unit>.Fail(code,"Squad request rejected: "+code);
        public void Dispose(){disposed=true;Changed=null;NavigationFailed=null;path.Clear();acknowledged.Clear();}
        sealed class Member
        {
            public string Id; public Vector3 Position,Forward; public SquadMemberState State;
            public bool Waiting; public double RetryAt,ExpiresAt; public CombatNavigationRequestDto Request;
        }
    }
}
