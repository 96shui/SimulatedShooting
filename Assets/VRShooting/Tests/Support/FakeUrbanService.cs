using System;
using System.Collections.Generic;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.P3.TestSupport
{
    /// <summary>Scripted fake only. Commands record intent; Publish/SetResult control all outcomes.</summary>
    public sealed class FakeUrbanService : IUrbanService, IDisposable
    {
        readonly FixtureFeed<UrbanSessionDto> frames = new FixtureFeed<UrbanSessionDto>(x => x.SessionId, x => x.Revision);
        readonly List<string> commands = new List<string>();
        UrbanResultDto result;
        bool hasResult;
        bool disposed;
        string cancelledId = string.Empty;
        public ErrorCode NextError { get; set; }
        IReadOnlyList<UrbanMapDto> maps = Array.AsReadOnly(new[] { P3Fixtures.UrbanMap });
        public IReadOnlyList<UrbanMapDto> Maps
        {
            get => maps;
            set => maps = Array.AsReadOnly(new List<UrbanMapDto>(value ?? Array.Empty<UrbanMapDto>()).ToArray());
        }
        public IReadOnlyList<string> Commands => commands.AsReadOnly();
        public int SubscriberCount => frames.SubscriberCount + (ResultReady?.GetInvocationList().Length ?? 0);
        public event Action<UrbanSessionDto> SessionChanged { add => frames.Published += value; remove => frames.Published -= value; }
        public event Action<UrbanResultDto> ResultReady;
        public void Bind(UrbanSessionDto snapshot)
        {
            frames.Bind(snapshot);
            hasResult = false; result = default; cancelledId = string.Empty; NextError = ErrorCode.None;
        }
        public bool Publish(UrbanSessionDto snapshot) => !hasResult && frames.Publish(snapshot);
        public bool SetResult(UrbanResultDto snapshot)
        {
            if (disposed || !frames.IsBound || snapshot.SessionId != frames.SessionId || snapshot.Revision <= frames.Current.Revision || hasResult) return false;
            result = snapshot; hasResult = true; ResultReady?.Invoke(snapshot); return true;
        }
        public ServiceResult<IReadOnlyList<UrbanMapDto>> GetMaps() =>
            disposed ? ServiceResult<IReadOnlyList<UrbanMapDto>>.Fail(ErrorCode.InvalidState) : ServiceResult<IReadOnlyList<UrbanMapDto>>.Ok(Maps);
        public ServiceResult<UrbanMapDto> SelectMap(string mapId)
        {
            var error = TakeError("SelectMap:" + mapId);
            if (error != ErrorCode.None) return ServiceResult<UrbanMapDto>.Fail(error);
            if (string.IsNullOrWhiteSpace(mapId)) return ServiceResult<UrbanMapDto>.Fail(ErrorCode.InvalidInput);
            foreach (var map in Maps)
                if (map.MapId == mapId) return ServiceResult<UrbanMapDto>.Ok(map);
            return ServiceResult<UrbanMapDto>.Fail(ErrorCode.NotFound);
        }
        public ServiceResult<UrbanSessionDto> StartSession(string mapId, string weaponId, RandomSeed seed)
        {
            var error = TakeError("StartSession:" + mapId);
            if (error != ErrorCode.None) return ServiceResult<UrbanSessionDto>.Fail(error);
            if (string.IsNullOrWhiteSpace(mapId)) return ServiceResult<UrbanSessionDto>.Fail(ErrorCode.InvalidInput);
            if (!HasMap(mapId)) return ServiceResult<UrbanSessionDto>.Fail(ErrorCode.NotFound);
            if (string.IsNullOrWhiteSpace(weaponId) || !frames.IsBound) return ServiceResult<UrbanSessionDto>.Fail(ErrorCode.InvalidState);
            if (weaponId != P3ContractIds.TrainingWeapon) return ServiceResult<UrbanSessionDto>.Fail(ErrorCode.NotFound);
            return GetSession(frames.SessionId);
        }
        public ServiceResult<UrbanSessionDto> GetSession(string sessionId)
        {
            var error = CheckSession(sessionId);
            return error == ErrorCode.None ? ServiceResult<UrbanSessionDto>.Ok(frames.Current) : ServiceResult<UrbanSessionDto>.Fail(error);
        }
        public ServiceResult<UrbanResultDto> GetResult(string sessionId)
        {
            var error = CheckSession(sessionId);
            if (error != ErrorCode.None) return ServiceResult<UrbanResultDto>.Fail(error);
            return hasResult ? ServiceResult<UrbanResultDto>.Ok(result) : ServiceResult<UrbanResultDto>.Fail(ErrorCode.InvalidState);
        }
        public ServiceResult<UrbanSessionDto> EnterBuilding(string sessionId, string entranceId) => Command("EnterBuilding", sessionId, entranceId);
        public ServiceResult<UrbanSessionDto> ExitBuilding(string sessionId, string entranceId) => Command("ExitBuilding", sessionId, entranceId);
        public ServiceResult<UrbanSessionDto> OpenRoomDoor(string sessionId, string roomId) => Command("OpenRoomDoor", sessionId, roomId);
        public ServiceResult<UrbanSessionDto> MarkRoomSearched(string sessionId, string roomId) => Command("MarkRoomSearched", sessionId, roomId);
        public ServiceResult<UrbanSessionDto> RegisterEnemyKilled(string sessionId, string enemyId) => Command("RegisterEnemyKilled", sessionId, enemyId);
        public ServiceResult<UrbanSessionDto> ObserveRoom(string sessionId, string roomId)
        {
            var error = TakeError("ObserveRoom:" + roomId);
            if (error == ErrorCode.None) error = CheckSession(sessionId);
            return ServiceResult<UrbanSessionDto>.Fail(error == ErrorCode.None ? ErrorCode.InvalidState : error, "Later capability");
        }
        public ServiceResult<UrbanResultDto> CompleteIfReady(string sessionId) => ResultCommand("CompleteIfReady", sessionId);
        public ServiceResult<UrbanResultDto> FailByPlayerDeath(string sessionId) => ResultCommand("FailByPlayerDeath", sessionId);
        ServiceResult<UrbanResultDto> ResultCommand(string command, string sessionId)
        {
            var error = TakeError(command);
            return error == ErrorCode.None ? GetResult(sessionId) : ServiceResult<UrbanResultDto>.Fail(error);
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
        ServiceResult<UrbanSessionDto> Command(string name, string sessionId, string argument)
        {
            var error = TakeError(name + ":" + argument);
            if (error == ErrorCode.None && string.IsNullOrWhiteSpace(argument)) error = ErrorCode.InvalidInput;
            if (error != ErrorCode.None) return ServiceResult<UrbanSessionDto>.Fail(error);
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
