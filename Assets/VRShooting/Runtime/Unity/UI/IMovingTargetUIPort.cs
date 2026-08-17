using System;
using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// DTO-only boundary used by the moving-target UI. The view and presenter do not know
    /// about scene objects, weapons, targets, or concrete gameplay services.
    /// </summary>
    public interface IMovingTargetUIPort : IDisposable
    {
        event Action<TrainingPresentationDto> PresentationChanged;

        event Action<HudDto> HudUpdated;

        event Action<MovingTargetResultDto> ResultUpdated;

        ServiceResult<IReadOnlyList<float>> GetAvailableSpeeds();

        ServiceResult<TrainingPresentationDto> GetPresentation();

        ServiceResult<HudDto> GetHud(string sessionId);

        ServiceResult<MovingTargetResultDto> GetResult(string sessionId);

        ServiceResult<TrainingPresentationDto> Start(MovingTargetSettingsDto settings);

        ServiceResult<TrainingPresentationDto> Retry(string sessionId);

        ServiceResult<TrainingPresentationDto> Exit(string sessionId);
    }
}
