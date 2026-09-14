using System;
using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>P3.Contracts.v1; commands return data, never Unity objects.</summary>
    public interface ITrenchService
    {
        ServiceResult<IReadOnlyList<TrenchMapDto>> GetMaps();
        ServiceResult<TrenchMapDto> SelectMap(string mapId);
        ServiceResult<TrenchBriefingDto> GetBriefing(string mapId, string weaponId, RandomSeed seed);
        ServiceResult<TrenchSessionDto> StartSession(string mapId, string weaponId, RandomSeed seed);
        ServiceResult<TrenchSessionDto> GetSession(string sessionId);
        ServiceResult<TrenchResultDto> GetResult(string sessionId);
        ServiceResult<TrenchSessionDto> MarkSearchNode(string sessionId, string nodeId);
        ServiceResult<TrenchSessionDto> RegisterEnemyKilled(string sessionId, string enemyId);
        ServiceResult<TrenchResultDto> CompleteIfReady(string sessionId);
        ServiceResult<TrenchResultDto> FailByPlayerDeath(string sessionId);
        ServiceResult<Unit> Cancel(string sessionId);
        event Action<TrenchSessionDto> SessionChanged;
        event Action<TrenchResultDto> ResultReady;
    }
}
