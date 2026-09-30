using System;
using System.Linq;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD27/task015: deterministic, session-owned grenade tactic.</summary>
    public sealed class SquadGrenadeTacticService : ICombatGrenadeTacticService
    {
        readonly ICombatClock clock;
        readonly ICombatCoreService core;
        readonly ICombatGrenadeSquadPort squad;
        readonly CombatConfigDto config;
        ICombatGrenadeWorld world;
        string session="";
        long sequence;
        double nextAllowed, pausedAt, lastFlightTime;
        bool active, disposed, suspended, landed, released;
        bool requested;
        Vector3 projectile;
        public event Action<GrenadeThrowPlanDto> GrenadeThrown;
        public event Action<GrenadeExplosionDto> GrenadeExploded;
        public event Action GrenadeCancelled;
        public GrenadeThrowPlanDto? Current { get; private set; }
        public float Elapsed => Current.HasValue ? Mathf.Clamp01((float)((clock.Now-Current.Value.ThrowTime)/Math.Max(.01,Current.Value.ExplosionTime-Current.Value.ThrowTime))) : 0;
        public Vector3 ProjectilePosition => projectile;
        public SquadGrenadeTacticService(ICombatClock clock,ICombatCoreService core,ICombatGrenadeSquadPort squad,CombatConfigDto? config=null)
        {
            this.clock=clock??throw new ArgumentNullException(nameof(clock));
            this.core=core??throw new ArgumentNullException(nameof(core));
            this.squad=squad??throw new ArgumentNullException(nameof(squad));
            this.config=config??CombatConfigDto.Default;
            if(!this.config.Validate().Success)throw new ArgumentException("Invalid configuration");
            core.Changed+=OnCore;
        }
        public void Configure(ICombatGrenadeWorld value)=>world=value;
        public void RequestFirstTeammateThrow(){if(active&&!disposed)requested=true;}
        public ServiceResult<Unit> Start(string id)
        {
            if(disposed)return Fail(ErrorCode.InvalidState);
            if(string.IsNullOrWhiteSpace(id))return Fail(ErrorCode.InvalidInput);
            Clear();session=id;active=true;suspended=false;sequence=0;nextAllowed=clock.Now;return Ok();
        }
        void OnCore(CombatCoreSnapshotDto snapshot)
        {
            if(!active||snapshot.SessionId!=session)return;
            bool pause=snapshot.State!=SessionState.Running||!snapshot.TrackingValid;
            if(pause&&!suspended){suspended=true;pausedAt=clock.Now;Clear();}
            else if(!pause&&suspended){nextAllowed+=Math.Max(0,clock.Now-pausedAt);suspended=false;}
        }
        public ServiceResult<Unit> Advance()
        {
            if(disposed||!active)return Fail(ErrorCode.InvalidState);
            if(world==null||suspended)return Ok();
            var snapshot=core.GetSnapshot(session);if(!snapshot.Success)return Fail(snapshot.ErrorCode);
            if(snapshot.Data.State!=SessionState.Running||!snapshot.Data.TrackingValid)return Ok();
            if(Current.HasValue)
            {
                var plan=Current.Value;
                if(!released&&clock.Now>=plan.ThrowTime+plan.WindupSeconds)
                {released=true;squad.SetGrenadeState(session,plan.ThrowerId,false);}
                var until=Math.Min(clock.Now,plan.ThrowTime+plan.WindupSeconds+plan.FlightSeconds);
                while(!landed&&lastFlightTime<until)
                {
                    lastFlightTime=Math.Min(until,lastFlightTime+1.0/60);
                    var next=GrenadeTrajectory.Position(plan,lastFlightTime);
                    if(world.Sweep(projectile,next,out var impact)){projectile=impact;landed=true;}
                    else projectile=next;
                }
                if(clock.Now+1e-8<plan.ExplosionTime)return Ok();
                var exposed=snapshot.Data.Visual.Entities.Where(e=>e.Role==CombatEntityRole.Enemy&&e.State!=CombatEntityState.Dead&&world.IsExposed(projectile,e.Position)).Select(e=>e.EntityId).ToArray();
                var result=core.ApplyGrenadeExplosion(session,plan.GrenadeId,plan.ThrowerId,projectile,plan.BlastRadius,exposed);
                if(!result.Success)return Fail(result.ErrorCode);
                Current=null;
                GrenadeExploded?.Invoke(new GrenadeExplosionDto{SessionId=session,GrenadeId=plan.GrenadeId,ThrowerId=plan.ThrowerId,Position=projectile,Targets=result.Data,Time=clock.Now});
                return Ok();
            }
            if(clock.Now+1e-8<nextAllowed||!requested)return Ok();
            requested=false;
            GrenadeThrowPlanDto best=default;int bestCount=1;
            var enemies=snapshot.Data.Visual.Entities.Where(e=>e.Role==CombatEntityRole.Enemy&&e.State!=CombatEntityState.Dead).OrderBy(e=>e.EntityId,StringComparer.Ordinal).ToArray();
            foreach(var member in squad.GetMembers(session).Where(m=>m.Role==SquadMemberRole.TeammateTwo&&m.State!=SquadMemberState.Down))
            {
                if(!world.TryGetThrowPose(member.MemberId,out var feet,out var origin))continue;
                var nearby=enemies.Where(e=>Vector3.Distance(e.Position,feet)<=config.GrenadeDetectionRange).ToArray();
                for(int i=0;i<nearby.Length;i++)for(int j=i+1;j<nearby.Length;j++)
                {
                    var target=(nearby[i].Position+nearby[j].Position)*.5f+Vector3.up*.08f;
                    int count=nearby.Count(e=>Vector3.Distance(e.Position,target)<=config.GrenadeBlastRadius&&world.IsExposed(target,e.Position));
                    if(count<=bestCount)continue;
                    var previous=origin;bool blocked=false;
                    for(int step=1;step<=32;step++)
                    {
                        var next=GrenadeTrajectory.Position(origin,target,step/32f);
                        if(world.Sweep(previous,next,out _)){blocked=true;break;}previous=next;
                    }
                    if(blocked)continue;
                    float flight=Mathf.Max(.15f,Vector3.Distance(origin,target)/config.GrenadeThrowSpeed);
                    bestCount=count;best=new GrenadeThrowPlanDto{SessionId=session,ThrowerId=member.MemberId,GrenadeId=session+".grenade-"+(sequence+1).ToString("000"),
                        Origin=origin,Target=target,ThrowTime=clock.Now,WindupSeconds=.65f,FlightSeconds=flight,
                        ExplosionTime=clock.Now+.65f+flight+config.GrenadeFuseSeconds,BlastRadius=config.GrenadeBlastRadius,ThrowSpeed=config.GrenadeThrowSpeed};
                }
            }
            if(bestCount<2)return Ok();
            var accepted=squad.SetGrenadeState(session,best.ThrowerId,true);if(!accepted.Success)return accepted;
            sequence++;Current=best;projectile=best.Origin;released=landed=false;lastFlightTime=best.ThrowTime+best.WindupSeconds;
            nextAllowed=best.ExplosionTime+config.GrenadeCooldownSeconds;GrenadeThrown?.Invoke(best);return Ok();
        }
        void Clear()
        {
            requested=false;
            if(!Current.HasValue)return;
            var plan=Current.Value;Current=null;squad.SetGrenadeState(session,plan.ThrowerId,false);GrenadeCancelled?.Invoke();
        }
        public void Cancel(){Clear();active=false;requested=false;}
        public void Dispose(){if(disposed)return;Cancel();disposed=true;core.Changed-=OnCore;world=null;GrenadeThrown=null;GrenadeExploded=null;GrenadeCancelled=null;}
        static ServiceResult<Unit> Ok()=>ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<Unit> Fail(ErrorCode code)=>ServiceResult<Unit>.Fail(code,"Grenade tactic rejected: "+code);
    }
}
