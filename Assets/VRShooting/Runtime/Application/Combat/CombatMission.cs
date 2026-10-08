using System;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>Typed application facade; all gameplay decisions remain in TrenchService/UrbanService.</summary>
    public sealed class CombatMission : ICombatTickPort, IDisposable
    {
        readonly Func<DateTime> utcNow;
        readonly ICombatClock sceneClock;
        bool disposed;
        bool committingOpening;
        bool combatStartPublished;
        RandomSeed sessionSeed;
        public bool OpeningPaused { get; set; }
        public bool HasCombatStarted => combatStartPublished;
        CombatSummaryDto? summary;
        public CombatMission(ICombatSceneLease scene, Func<DateTime> utcNow = null)
        {
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            sceneClock=scene.Clock;
            Definition = scene.Definition;
            if (Definition.Mode == TrainingMode.Trench)
            {
                DroneScenePort=(scene as IDroneReconSceneLease)?.DroneReconScene;
                if(DroneScenePort!=null)
                {
                    DroneRecon=new DroneReconService(scene.Clock,DroneScenePort,suspended:()=>
                        OpeningPaused||Core==null||!Core.GetSnapshot(SessionId).Success||!Core.GetSnapshot(SessionId).Data.TrackingValid);
                    DroneRecon.Changed+=OnDrone;
                }
                Trench = new TrenchService(Definition, scene.Clock, scene.Random, scene.Navigation,recon:DroneRecon);
                Core = Trench.Combat; World = Trench; State = Trench; Hud = Trench; Squad = Trench.SquadCommands; Grenades = Trench.GrenadeTactics;
                Trench.SessionChanged += OnTrench; Trench.ResultReady += OnTrenchResult;
            }
            else if (Definition.Mode == TrainingMode.Urban)
            {
                Urban = new UrbanService(Definition, scene.Clock, scene.Random, scene.Navigation);
                Core = Urban.Combat; World = Urban; State = Urban; Hud = Urban; Squad = Urban.SquadCommands; Grenades = null;
                Urban.SessionChanged += OnUrban; Urban.ResultReady += OnUrbanResult;
            }
            else throw new ArgumentException("Only P3 modes can be composed", nameof(scene));
        }
        public CombatSceneDefinitionDto Definition { get; }
        public TrenchService Trench { get; }
        public DroneReconService DroneRecon { get; }
        public IDroneReconScenePort DroneScenePort { get; }
        public DroneReconSnapshotDto? Opening => DroneRecon!=null&&SessionId.Length>0&&DroneRecon.GetSnapshot(SessionId).Success
            ? DroneRecon.GetSnapshot(SessionId).Data : (DroneReconSnapshotDto?)null;
        public UrbanService Urban { get; }
        public ICombatCoreService Core { get; }
        public ICombatWorldInputPort World { get; }
        public ICombatStateService State { get; }
        public IHUDService Hud { get; }
        public ISquadCommandService Squad { get; }
        public ICombatGrenadeTacticService Grenades { get; }
        public string SessionId { get; private set; } = string.Empty;
        public CombatSummaryDto? Summary => summary;
        public event Action Changed;
        public event Action<TrainingSessionDto> CombatStarted;
        public ServiceResult<Unit> Start(RandomSeed seed)
        {
            if (disposed) return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            if (SessionId.Length > 0) return ServiceResult<Unit>.Fail(ErrorCode.Busy);
            sessionSeed=seed;
            if (Trench != null)
            {
                if(DroneRecon==null)return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable,"Trench drone scene port is missing");
                var result = Trench.StartSession(Definition.MapId, P3ContractIds.TrainingWeapon, seed);
                if (!result.Success) return ServiceResult<Unit>.Fail(result.ErrorCode, result.Message);
                SessionId = result.Data.SessionId;
                Changed?.Invoke();return ServiceResult<Unit>.Ok(Unit.Value);
            }
            else
            {
                var result = Urban.StartSession(Definition.MapId, P3ContractIds.TrainingWeapon, seed);
                if (!result.Success) return ServiceResult<Unit>.Fail(result.ErrorCode, result.Message);
                SessionId = result.Data.SessionId;
            }
            var forward=Definition.PlayerSpawnForward.sqrMagnitude>0 ? Definition.PlayerSpawnForward : UnityEngine.Vector3.forward;
            Core.Submit(new CombatInputDto {SessionId=SessionId,EventId="initial-player-pose",Tick=sceneClock.Tick,
                Kind=CombatInputKind.PlayerPose,EntityId=SessionId+".player",Position=Definition.PlayerSpawnPosition,Direction=forward});
            Core.Advance(SessionId);
            (Squad as SquadFormationService)?.Start(SessionId,Definition.PlayerSpawnPosition,forward);
            Changed?.Invoke(); return ServiceResult<Unit>.Ok(Unit.Value);
        }
        public ServiceResult<Unit> Advance()
        {
            if (disposed || SessionId.Length == 0) return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            if(Trench!=null && !combatStartPublished)
            {
                if(DroneRecon==null)return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable);
                var advanced=DroneRecon.Advance(SessionId);if(!advanced.Success)return advanced;
                if(disposed)return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
                if(Opening?.Phase!=DroneReconPhase.Ready)return ServiceResult<Unit>.Ok(Unit.Value);
                if(OpeningPaused||!Core.GetSnapshot(SessionId).Data.TrackingValid)return ServiceResult<Unit>.Ok(Unit.Value);
                committingOpening=true;
                try
                {
                    var begun=Trench.BeginCombat(SessionId);
                    if(!begun.Success)return ServiceResult<Unit>.Fail(begun.ErrorCode);
                    if(OpeningPaused||!Core.GetSnapshot(SessionId).Data.TrackingValid)return ServiceResult<Unit>.Ok(Unit.Value);
                    var released=DroneRecon.ConfirmCombatStarted(SessionId);
                    if(!released.Success)return released;
                    var committed=Trench.CompleteCombatStart(SessionId);
                    if(!committed.Success)return committed;
                    if(!combatStartPublished)
                    {
                        combatStartPublished=true;
                        var combat=Trench.GetSession(SessionId).Data;
                        CombatStarted?.Invoke(new TrainingSessionDto {SessionId=SessionId,Mode=Definition.Mode,
                            State=SessionState.Running,MapId=Definition.MapId,WeaponId=P3ContractIds.TrainingWeapon,
                            Seed=sessionSeed,Ammo=combat.Ammo,Player=combat.Player,Squad=combat.Squad,
                            PostureMode=TrainingPostureMode.CombatFree,ArtificialLocomotionAllowed=true});
                    }
                }
                finally {committingOpening=false;}
                Changed?.Invoke();return ServiceResult<Unit>.Ok(Unit.Value);
            }
            return Trench != null ? Trench.Advance(SessionId) : Urban.Advance(SessionId);
        }
        public ServiceResult<Unit> StartOpening()
        {
            if(disposed||Trench==null||DroneRecon==null||SessionId.Length==0)return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            var started=DroneRecon.Begin(SessionId);
            return started.Success?ServiceResult<Unit>.Ok(Unit.Value):ServiceResult<Unit>.Fail(started.ErrorCode,started.Message);
        }
        void OnDrone(DroneReconSnapshotDto value){if(!disposed&&!committingOpening&&value.SessionId==SessionId)Changed?.Invoke();}
        void OnTrench(TrenchSessionDto value) { if (!disposed && !committingOpening && value.SessionId == SessionId) Changed?.Invoke(); }
        void OnUrban(UrbanSessionDto value) { if (!disposed && value.SessionId == SessionId) Changed?.Invoke(); }
        void OnTrenchResult(TrenchResultDto result) => Result(result.SessionId, result.Victory, result.ElapsedSeconds, result.RemainingAmmo);
        void OnUrbanResult(UrbanResultDto result) => Result(result.SessionId, result.Victory, result.ElapsedSeconds, result.RemainingAmmo);
        void Result(string id, bool victory, float seconds, int ammo)
        {
            if (disposed || id != SessionId || summary.HasValue) return;
            summary = new CombatSummaryDto { SessionId = id, Mode = Definition.Mode, MapId = Definition.MapId,
                Victory = victory, ElapsedSeconds = seconds, RemainingAmmo = ammo, CompletedAtUtc = utcNow().ToUniversalTime() };
            Changed?.Invoke();
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true; Changed = null;
            CombatStarted=null;
            if(DroneRecon!=null){DroneRecon.Changed-=OnDrone;DroneRecon.Dispose();}
            if (Trench != null) { Trench.SessionChanged -= OnTrench; Trench.ResultReady -= OnTrenchResult; if (SessionId.Length > 0) Trench.Cancel(SessionId); Trench.Dispose(); }
            if (Urban != null) { Urban.SessionChanged -= OnUrban; Urban.ResultReady -= OnUrbanResult; if (SessionId.Length > 0) Urban.Cancel(SessionId); Urban.Dispose(); }
        }
    }
}
