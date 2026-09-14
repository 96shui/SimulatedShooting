using System;
using System.Collections.Generic;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.P3.TestSupport
{
    public sealed class FakeCombatStateService : ICombatStateService, ISquadCommandService, IDisposable
    {
        public FixtureFeed<CombatPlayerSnapshotDto> Players { get; } = new FixtureFeed<CombatPlayerSnapshotDto>(x => x.SessionId, x => x.Revision);
        public FixtureFeed<CombatSquadSnapshotDto> Squads { get; } = new FixtureFeed<CombatSquadSnapshotDto>(x => x.SessionId, x => x.Revision);
        public FixtureFeed<CombatVisualSnapshotDto> Visuals { get; } = new FixtureFeed<CombatVisualSnapshotDto>(x => x.SessionId, x => x.Revision);
        public event Action<CombatPlayerSnapshotDto> PlayerChanged { add => Players.Published += value; remove => Players.Published -= value; }
        public event Action<CombatSquadSnapshotDto> SquadChanged { add => Squads.Published += value; remove => Squads.Published -= value; }
        public event Action<CombatVisualSnapshotDto> VisualChanged { add => Visuals.Published += value; remove => Visuals.Published -= value; }
        public ServiceResult<CombatPlayerSnapshotDto> GetPlayer(string sessionId) => Read(Players, sessionId);
        public ServiceResult<CombatSquadSnapshotDto> GetSquadStatus(string sessionId) => Read(Squads, sessionId);
        public ServiceResult<CombatVisualSnapshotDto> GetVisualSnapshot(string sessionId) => Read(Visuals, sessionId);
        ServiceResult<SquadStatusDto> ISquadCommandService.GetSquadStatus(string sessionId)
        {
            var snapshot = GetSquadStatus(sessionId);
            return snapshot.Success ? ServiceResult<SquadStatusDto>.Ok(snapshot.Data.Squad) : ServiceResult<SquadStatusDto>.Fail(snapshot.ErrorCode);
        }
        public ServiceResult<IReadOnlyList<SquadCommandType>> GetAvailableCommands(string sessionId)
        {
            var snapshot = GetSquadStatus(sessionId);
            return snapshot.Success ? ServiceResult<IReadOnlyList<SquadCommandType>>.Ok(Array.Empty<SquadCommandType>()) : ServiceResult<IReadOnlyList<SquadCommandType>>.Fail(snapshot.ErrorCode);
        }
        public ServiceResult<SquadCommandResult> Issue(SquadCommandRequest request) => ServiceResult<SquadCommandResult>.Fail(ErrorCode.InvalidState, "P3 does not support tactical commands");
        public ServiceResult<SquadStatusDto> OnReloadStarted(string sessionId) => ServiceResult<SquadStatusDto>.Fail(ErrorCode.InvalidState, "Cover reload is Later");
        static ServiceResult<T> Read<T>(FixtureFeed<T> feed, string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return ServiceResult<T>.Fail(ErrorCode.InvalidInput);
            return feed.IsBound && feed.SessionId == id ? ServiceResult<T>.Ok(feed.Current) : ServiceResult<T>.Fail(ErrorCode.NotFound);
        }
        public void Dispose() { Players.Dispose(); Squads.Dispose(); Visuals.Dispose(); }
    }
}
