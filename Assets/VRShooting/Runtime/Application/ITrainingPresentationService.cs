using System;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// P1/P2 共用展示状态机。参见 docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md。
    /// </summary>
    public interface ITrainingPresentationService
    {
        ServiceResult<TrainingPresentationDto> Enter(TrainingMode mode);

        ServiceResult<TrainingPresentationDto> Get(string sessionId);

        ServiceResult<TrainingPresentationDto> ConfirmStart(string sessionId);

        ServiceResult<TrainingPresentationDto> HandleWeaponPickup(TrainingWeaponPickupEvent pickup);

        ServiceResult<TrainingPresentationDto> ContinueNextRound(string sessionId);

        ServiceResult<TrainingPresentationDto> NotifyMovingTargetCountdownElapsed(string sessionId);

        ServiceResult<TrainingPresentationDto> NotifyTrainingCompleted(string sessionId);

        ServiceResult<TrainingPresentationDto> Retry(string sessionId);

        ServiceResult<TrainingPresentationDto> Exit(string sessionId);

        ServiceResult<TrainingLocomotionPolicyDto> GetLocomotionPolicy(string sessionId);

        event Action<TrainingPresentationDto> PresentationChanged;
    }
}
