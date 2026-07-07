using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public interface IZeroingService
    {
        ServiceResult<ZeroingSessionDto> StartSession(RandomSeed seed, string weaponId);
        ServiceResult<ZeroingSessionDto> GetSession(string sessionId);
        ServiceResult<ZeroingShotDto> RecordShot(string sessionId, ShotInputDto input);
        ServiceResult<ZeroingRoundAnalysisDto> CompleteRound(string sessionId);
        ServiceResult<ZeroingRoundAnalysisDto> ApplyAdjustment(string sessionId, int roundIndex);
        ServiceResult<ZeroingSessionDto> ContinueAfterAnalysis(string sessionId);
        ServiceResult<ZeroingResultDto> GetFinalResult(string sessionId);
    }
}
