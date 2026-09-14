using System;
using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>P3.Contracts.v1; commands return data, never Unity objects.</summary>
    public interface IUrbanService
    {
        ServiceResult<IReadOnlyList<UrbanMapDto>> GetMaps();
        ServiceResult<UrbanMapDto> SelectMap(string mapId);
        ServiceResult<UrbanSessionDto> StartSession(string mapId, string weaponId, RandomSeed seed);
        ServiceResult<UrbanSessionDto> GetSession(string sessionId);
        ServiceResult<UrbanResultDto> GetResult(string sessionId);
        ServiceResult<UrbanSessionDto> EnterBuilding(string sessionId, string entranceId);
        ServiceResult<UrbanSessionDto> ExitBuilding(string sessionId, string entranceId);
        ServiceResult<UrbanSessionDto> OpenRoomDoor(string sessionId, string roomId);
        ServiceResult<UrbanSessionDto> ObserveRoom(string sessionId, string roomId);
        ServiceResult<UrbanSessionDto> MarkRoomSearched(string sessionId, string roomId);
        ServiceResult<UrbanSessionDto> RegisterEnemyKilled(string sessionId, string enemyId);
        ServiceResult<UrbanResultDto> CompleteIfReady(string sessionId);
        ServiceResult<UrbanResultDto> FailByPlayerDeath(string sessionId);
        ServiceResult<Unit> Cancel(string sessionId);
        event Action<UrbanSessionDto> SessionChanged;
        event Action<UrbanResultDto> ResultReady;
    }
}
