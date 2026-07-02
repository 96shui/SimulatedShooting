using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 训练 Session 生命周期。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public interface ITrainingSessionService
    {
        TrainingSessionDto Current { get; }

        bool HasActiveSession { get; }

        ServiceResult<TrainingSessionDto> Create(TrainingMode mode, string mapId, string weaponId, RandomSeed seed);

        ServiceResult<TrainingSessionDto> Start(string sessionId);

        ServiceResult<TrainingSessionDto> Pause(string sessionId);

        ServiceResult<TrainingSessionDto> Resume(string sessionId);

        ServiceResult<TrainingResultDto> End(string sessionId, SessionEndReason reason);

        ServiceResult<Unit> Cancel(string sessionId);
    }
}
