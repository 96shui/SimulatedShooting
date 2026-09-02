using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// Advances the P2 countdown and route independently from weapon input.
    /// Presentation is the authority for when the countdown is allowed to start.
    /// </summary>
    public sealed class MovingTargetProgressCoordinator
    {
        readonly ITrainingSessionService trainingSessions;
        readonly ITrainingPresentationService presentation;
        readonly IMovingTargetService movingTarget;

        public MovingTargetProgressCoordinator(
            ITrainingSessionService trainingSessions,
            ITrainingPresentationService presentation,
            IMovingTargetService movingTarget)
        {
            this.trainingSessions = trainingSessions;
            this.presentation = presentation;
            this.movingTarget = movingTarget;
        }

        public ServiceResult<MovingTargetSessionDto> Tick(string sessionId, float deltaTime)
        {
            sessionId = ResolveSessionId(sessionId);
            if (string.IsNullOrEmpty(sessionId))
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(
                    ErrorCode.InvalidState,
                    "an active moving target session is required",
                    MovingTargetSessionDto.Empty);
            }

            var state = movingTarget.GetSession(sessionId);
            if (!state.Success)
            {
                return state;
            }

            var presented = presentation.Get(sessionId);
            if (!presented.Success
                || presented.Data.Mode != TrainingMode.MovingTarget
                || presented.Data.Phase != TrainingPresentationPhase.LiveFire)
            {
                return state;
            }

            return movingTarget.Tick(sessionId, deltaTime);
        }

        string ResolveSessionId(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                return sessionId;
            }

            if (!trainingSessions.HasActiveSession
                || trainingSessions.Current.Mode != TrainingMode.MovingTarget)
            {
                return string.Empty;
            }

            return trainingSessions.Current.SessionId;
        }
    }
}
