using System;
using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface ICombatClock { double Now { get; } long Tick { get; } }
    public interface ICombatRandom
    {
        void Reset(RandomSeed seed);
        int NextInt(int minInclusive, int maxExclusive);
    }
    public interface ICombatWorldInputPort { ServiceResult<Unit> Submit(CombatInputDto input); }
    public interface ICombatNavigationPort { ServiceResult<Unit> Move(CombatNavigationRequestDto request); }
    public interface ICombatSceneView { void Apply(CombatVisualSnapshotDto snapshot); }
    public interface ICombatStateService
    {
        ServiceResult<CombatPlayerSnapshotDto> GetPlayer(string sessionId);
        ServiceResult<CombatSquadSnapshotDto> GetSquadStatus(string sessionId);
        ServiceResult<CombatVisualSnapshotDto> GetVisualSnapshot(string sessionId);
        event Action<CombatPlayerSnapshotDto> PlayerChanged;
        event Action<CombatSquadSnapshotDto> SquadChanged;
        event Action<CombatVisualSnapshotDto> VisualChanged;
    }
    public interface ICombatSummaryStore
    {
        ServiceResult<Unit> SaveLatest(CombatSummaryDto summary);
        ServiceResult<CombatSummaryDto> GetLatest(TrainingMode mode);
    }
    public interface ISquadCommandService
    {
        ServiceResult<SquadStatusDto> GetSquadStatus(string sessionId);
        ServiceResult<IReadOnlyList<SquadCommandType>> GetAvailableCommands(string sessionId);
        ServiceResult<SquadCommandResult> Issue(SquadCommandRequest request);
        ServiceResult<SquadStatusDto> OnReloadStarted(string sessionId);
    }
}
