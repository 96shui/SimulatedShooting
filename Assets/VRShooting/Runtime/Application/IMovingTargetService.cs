using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 移动靶训练核心。参见 docs/接口文档/05-移动目标服务.md。
    /// </summary>
    public interface IMovingTargetService
    {
        ServiceResult<IReadOnlyList<float>> GetAvailableSpeeds();

        ServiceResult<MovingTargetSessionDto> StartSession(MovingTargetSettingsDto settings, RandomSeed seed);

        ServiceResult<MovingTargetSessionDto> GetSession(string sessionId);

        ServiceResult<MovingTargetSessionDto> Tick(string sessionId, float deltaTime);

        ServiceResult<MovingTargetShotRecordDto> RecordShot(
            string sessionId,
            string sequenceId,
            int shotIndexInSequence,
            WeaponShotResultDto shot);

        ServiceResult<FireSequenceRecordDto> CompleteFireSequence(
            string sessionId,
            string sequenceId,
            WeaponFireStopReason stopReason);

        ServiceResult<MovingTargetResultDto> CompleteSession(string sessionId);
    }
}
