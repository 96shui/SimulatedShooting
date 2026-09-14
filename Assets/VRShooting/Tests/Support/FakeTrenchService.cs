using System;
using System.Collections.Generic;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.P3.TestSupport
{
    /// <summary>Scripted fake only. Commands record intent; Publish/SetResult control all outcomes.</summary>
    public sealed class FakeTrenchService : ITrenchService, IDisposable
    {
        readonly FixtureFeed<TrenchSessionDto> frames = new FixtureFeed<TrenchSessionDto>(x => x.SessionId, x => x.Revision);
        readonly List<string> commands = new List<string>();
        TrenchResultDto result;
        bool hasResult;
        bool disposed;
        string cancelledId = string.Empty;
        public ErrorCode NextError { get; set; }
        IReadOnlyList<TrenchMapDto> maps = Array.AsReadOnly(new[] { P3Fixtures.TrenchMap });
        public IReadOnlyList<TrenchMapDto> Maps
        {
            get => maps;
            set => maps = Array.AsReadOnly(new List<TrenchMapDto>(value ?? Array.Empty<TrenchMapDto>()).ToArray());
        }
        public IReadOnlyList<string> Commands => commands.AsReadOnly();
        public int SubscriberCount => frames.SubscriberCount + (ResultReady?.GetInvocationList().Length ?? 0);
        public event Action<TrenchSessionDto> SessionChanged { add => frames.Published += value; remove => frames.Published -= value; }
        public event Action<TrenchResultDto> ResultReady;
        public void Bind(TrenchSessionDto snapshot)
        {
            frames.Bind(snapshot);
            hasResult = false; result = default; cancelledId = string.Empty; NextError = ErrorCode.None;
        }
        public bool Publish(TrenchSessionDto snapshot) => !hasResult && frames.Publish(snapshot);
        public bool SetResult(TrenchResultDto snapshot)
        {
            if (disposed || !frames.IsBound || snapshot.SessionId != frames.SessionId || snapshot.Revision <= frames.Current.Revision || hasResult) return false;
            result = snapshot; hasResult = true; ResultReady?.Invoke(snapshot); return true;
        }
        public ServiceResult<IReadOnlyList<TrenchMapDto>> GetMaps() =>
            disposed ? ServiceResult<IReadOnlyList<TrenchMapDto>>.Fail(ErrorCode.InvalidState) : ServiceResult<IReadOnlyList<TrenchMapDto>>.Ok(Maps);
        public ServiceResult<TrenchMapDto> SelectMap(string mapId)
        {
            var error = TakeError("SelectMap:" + mapId);
            if (error != ErrorCode.None) return ServiceResult<TrenchMapDto>.Fail(error);
            if (string.IsNullOrWhiteSpace(mapId)) return ServiceResult<TrenchMapDto>.Fail(ErrorCode.InvalidInput);
            foreach (var map in Maps)
                if (map.MapId == mapId) return ServiceResult<TrenchMapDto>.Ok(map);
            return ServiceResult<TrenchMapDto>.Fail(ErrorCode.NotFound);
        }
        public ServiceResult<TrenchSessionDto> StartSession(string mapId, string weaponId, RandomSeed seed)
        {
            var error = TakeError("StartSession:" + mapId);
            if (error != ErrorCode.None) return ServiceResult<TrenchSessionDto>.Fail(error);
            if (string.IsNullOrWhiteSpace(mapId)) return ServiceResult<TrenchSessionDto>.Fail(ErrorCode.InvalidInput);
            if (!HasMap(mapId)) return ServiceResult<TrenchSessionDto>.Fail(ErrorCode.NotFound);
            if (string.IsNullOrWhiteSpace(weaponId) || !frames.IsBound) return ServiceResult<TrenchSessionDto>.Fail(ErrorCode.InvalidState);
            if (weaponId != P3ContractIds.TrainingWeapon) return ServiceResult<TrenchSessionDto>.Fail(ErrorCode.NotFound);
            return GetSession(frames.SessionId);
        }
        public ServiceResult<TrenchSessionDto> GetSession(string sessionId)
        {
            var error = CheckSession(sessionId);
            return error == ErrorCode.None ? ServiceResult<TrenchSessionDto>.Ok(frames.Current) : ServiceResult<TrenchSessionDto>.Fail(error);
        }
        public ServiceResult<TrenchResultDto> GetResult(string sessionId)
        {
            var error = CheckSession(sessionId);
            if (error != ErrorCode.None) return ServiceResult<TrenchResultDto>.Fail(error);
            return hasResult ? ServiceResult<TrenchResultDto>.Ok(result) : ServiceResult<TrenchResultDto>.Fail(ErrorCode.InvalidState);
        }
        public ServiceResult<TrenchSessionDto> MarkSearchNode(string sessionId, string nodeId) => Command("MarkSearchNode", sessionId, nodeId);
        public ServiceResult<TrenchSessionDto> RegisterEnemyKilled(string sessionId, string enemyId) => Command("RegisterEnemyKilled", sessionId, enemyId);
        public ServiceResult<TrenchBriefingDto> GetBriefing(string mapId, string weaponId, RandomSeed seed)
        {
            if (disposed) return ServiceResult<TrenchBriefingDto>.Fail(ErrorCode.InvalidState);
            if (string.IsNullOrWhiteSpace(mapId)) return ServiceResult<TrenchBriefingDto>.Fail(ErrorCode.InvalidInput);
            if (!HasMap(mapId)) return ServiceResult<TrenchBriefingDto>.Fail(ErrorCode.NotFound);
            if (string.IsNullOrWhiteSpace(weaponId)) return ServiceResult<TrenchBriefingDto>.Fail(ErrorCode.InvalidState);
            return ServiceResult<TrenchBriefingDto>.Ok(new TrenchBriefingDto
            {
                MapId = mapId, PlannedSquad = P3Fixtures.Squad("preview"), ProjectedMap = P3Fixtures.MiniMap(mapId),
                EnemyEstimateMin = 3, EnemyEstimateMax = 5
            });
        }
        public ServiceResult<TrenchResultDto> CompleteIfReady(string sessionId) => ResultCommand("CompleteIfReady", sessionId);
        public ServiceResult<TrenchResultDto> FailByPlayerDeath(string sessionId) => ResultCommand("FailByPlayerDeath", sessionId);
        ServiceResult<TrenchResultDto> ResultCommand(string command, string sessionId)
        {
            var error = TakeError(command);
            return error == ErrorCode.None ? GetResult(sessionId) : ServiceResult<TrenchResultDto>.Fail(error);
        }
        public ServiceResult<Unit> Cancel(string sessionId)
        {
            var error = TakeError("Cancel:" + sessionId);
            if (error != ErrorCode.None) return ServiceResult<Unit>.Fail(error);
            if (!string.IsNullOrWhiteSpace(sessionId) && sessionId == cancelledId) return ServiceResult<Unit>.Ok(Unit.Value);
            error = CheckSession(sessionId);
            if (error != ErrorCode.None) return ServiceResult<Unit>.Fail(error);
            cancelledId = sessionId; frames.Clear(); result = default; hasResult = false;
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        ServiceResult<TrenchSessionDto> Command(string name, string sessionId, string argument)
        {
            var error = TakeError(name + ":" + argument);
            if (error == ErrorCode.None && string.IsNullOrWhiteSpace(argument)) error = ErrorCode.InvalidInput;
            if (error != ErrorCode.None) return ServiceResult<TrenchSessionDto>.Fail(error);
            // No AI/search/phase transitions: consumers explicitly inject their next frame.
            return GetSession(sessionId);
        }
        ErrorCode CheckSession(string id)
        {
            if (disposed) return ErrorCode.InvalidState;
            if (string.IsNullOrWhiteSpace(id)) return ErrorCode.InvalidInput;
            if (id == cancelledId) return ErrorCode.InvalidState;
            return frames.IsBound && id == frames.SessionId ? ErrorCode.None : ErrorCode.NotFound;
        }
        bool HasMap(string id)
        {
            foreach (var map in Maps) if (map.MapId == id) return true;
            return false;
        }
        ErrorCode TakeError(string command)
        {
            commands.Add(command);
            if (disposed) return ErrorCode.InvalidState;
            var error = NextError; NextError = ErrorCode.None; return error;
        }
        public void Dispose() { disposed = true; frames.Dispose(); ResultReady = null; result = default; hasResult = false; }
    }
}
