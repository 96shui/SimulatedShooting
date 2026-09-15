using System;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>Typed application facade; all gameplay decisions remain in TrenchService/UrbanService.</summary>
    public sealed class CombatMission : ICombatTickPort, IDisposable
    {
        readonly Func<DateTime> utcNow;
        bool disposed;
        CombatSummaryDto? summary;
        public CombatMission(ICombatSceneLease scene, Func<DateTime> utcNow = null)
        {
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            Definition = scene.Definition;
            if (Definition.Mode == TrainingMode.Trench)
            {
                Trench = new TrenchService(Definition, scene.Clock, scene.Random, scene.Navigation);
                Core = Trench.Combat; World = Trench; State = Trench; Hud = Trench; Squad = Trench.SquadCommands;
                Trench.SessionChanged += OnTrench; Trench.ResultReady += OnTrenchResult;
            }
            else if (Definition.Mode == TrainingMode.Urban)
            {
                Urban = new UrbanService(Definition, scene.Clock, scene.Random, scene.Navigation);
                Core = Urban.Combat; World = Urban; State = Urban; Hud = Urban; Squad = Urban.SquadCommands;
                Urban.SessionChanged += OnUrban; Urban.ResultReady += OnUrbanResult;
            }
            else throw new ArgumentException("Only P3 modes can be composed", nameof(scene));
        }
        public CombatSceneDefinitionDto Definition { get; }
        public TrenchService Trench { get; }
        public UrbanService Urban { get; }
        public ICombatCoreService Core { get; }
        public ICombatWorldInputPort World { get; }
        public ICombatStateService State { get; }
        public IHUDService Hud { get; }
        public ISquadCommandService Squad { get; }
        public string SessionId { get; private set; } = string.Empty;
        public CombatSummaryDto? Summary => summary;
        public event Action Changed;
        public ServiceResult<Unit> Start(RandomSeed seed)
        {
            if (disposed) return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            if (SessionId.Length > 0) return ServiceResult<Unit>.Fail(ErrorCode.Busy);
            if (Trench != null)
            {
                var result = Trench.StartSession(Definition.MapId, P3ContractIds.TrainingWeapon, seed);
                if (!result.Success) return ServiceResult<Unit>.Fail(result.ErrorCode, result.Message);
                SessionId = result.Data.SessionId;
            }
            else
            {
                var result = Urban.StartSession(Definition.MapId, P3ContractIds.TrainingWeapon, seed);
                if (!result.Success) return ServiceResult<Unit>.Fail(result.ErrorCode, result.Message);
                SessionId = result.Data.SessionId;
            }
            Changed?.Invoke(); return ServiceResult<Unit>.Ok(Unit.Value);
        }
        public ServiceResult<Unit> Advance()
        {
            if (disposed || SessionId.Length == 0) return ServiceResult<Unit>.Fail(ErrorCode.InvalidState);
            return Trench != null ? Trench.Advance(SessionId) : Urban.Advance(SessionId);
        }
        void OnTrench(TrenchSessionDto value) { if (!disposed && value.SessionId == SessionId) Changed?.Invoke(); }
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
            if (Trench != null) { Trench.SessionChanged -= OnTrench; Trench.ResultReady -= OnTrenchResult; if (SessionId.Length > 0) Trench.Cancel(SessionId); Trench.Dispose(); }
            if (Urban != null) { Urban.SessionChanged -= OnUrban; Urban.ResultReady -= OnUrbanResult; if (SessionId.Length > 0) Urban.Cancel(SessionId); Urban.Dispose(); }
        }
    }
}
