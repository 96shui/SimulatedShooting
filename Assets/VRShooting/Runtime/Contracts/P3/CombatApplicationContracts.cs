using System;
using System.Threading;
using System.Threading.Tasks;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    public readonly struct CombatApplicationSnapshotDto
    {
        public ScreenId Screen { get; init; }
        public TrainingMode? Mode { get; init; }
        public bool Busy { get; init; }
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public ErrorCode Error { get; init; }
        public CombatSummaryDto? Summary { get; init; }
    }
}
namespace VRShooting.Application
{
    public interface ICombatSceneLoader
    {
        Task<ServiceResult<ICombatSceneLease>> LoadAsync(TrainingMode mode, CancellationToken cancellation);
    }
    // An exclusive scene instance. Loading must not create actors. Dispose releases only this lease's scene.
    public interface ICombatSceneLease : IDisposable
    {
        CombatSceneDefinitionDto Definition { get; }
        ICombatClock Clock { get; }
        ICombatRandom Random { get; }
        ICombatNavigationPort Navigation { get; }
        ServiceResult<Unit> Activate(ICombatCoreService core, ICombatWorldInputPort world, ICombatStateService state,
            IHUDService hud, ISquadCommandService squad, ICombatTickPort tick, string sessionId);
        void Deactivate();
    }
    public interface ICombatApplicationView { void Render(CombatApplicationSnapshotDto state); }
    public interface ICombatTickPort { ServiceResult<Unit> Advance(); }
}
