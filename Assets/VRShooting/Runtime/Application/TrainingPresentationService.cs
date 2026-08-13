using System;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public sealed class TrainingPresentationService : ITrainingPresentationService
    {
        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly IWeaponControlService weaponControl;
        readonly IZeroingService zeroing;
        PresentationRecord current;
        TrainingPresentationDto lastPublished;

        public TrainingPresentationService(
            IGameEventBus eventBus,
            ITrainingSessionService trainingSessions,
            IWeaponControlService weaponControl,
            IZeroingService zeroing)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
            this.weaponControl = weaponControl;
            this.zeroing = zeroing;

            eventBus.Subscribe<TrainingWeaponPickupEvent>(OnWeaponPickupEvent);
            eventBus.Subscribe<ZeroingRoundCompletedEvent>(OnZeroingRoundCompleted);
        }

        public event Action<TrainingPresentationDto> PresentationChanged;

        public ServiceResult<TrainingPresentationDto> Enter(TrainingMode mode)
        {
            if (!TrainingPresentationRules.IsProneFixedMode(mode))
            {
                return FailUnchanged(ErrorCode.InvalidInput, "presentation only supports P1/P2 modes");
            }

            if (current != null
                && current.Mode == mode
                && current.Phase == TrainingPresentationPhase.AwaitingStartConfirmation
                && string.IsNullOrEmpty(current.SessionId))
            {
                return ServiceResult<TrainingPresentationDto>.Ok(BuildDto());
            }

            ReleaseActiveSession();
            current = new PresentationRecord
            {
                Mode = mode,
                Phase = TrainingPresentationPhase.AwaitingStartConfirmation,
                FiringStationId = TrainingPresentationRules.FiringStationIdFor(mode),
                HoldState = WeaponHoldState.OnRack
            };
            return PublishOk();
        }

        public ServiceResult<TrainingPresentationDto> Get(string sessionId)
        {
            if (current == null)
            {
                return ServiceResult<TrainingPresentationDto>.Fail(ErrorCode.NotFound, "presentation not found");
            }

            if (!string.IsNullOrEmpty(sessionId)
                && !string.IsNullOrEmpty(current.SessionId)
                && current.SessionId != sessionId)
            {
                return ServiceResult<TrainingPresentationDto>.Fail(ErrorCode.NotFound, "presentation not found");
            }

            return ServiceResult<TrainingPresentationDto>.Ok(BuildDto());
        }

        public ServiceResult<TrainingPresentationDto> ConfirmStart(string sessionId)
        {
            if (current == null || !TrainingPresentationRules.IsProneFixedMode(current.Mode))
            {
                return FailUnchanged(ErrorCode.InvalidState, "enter a P1/P2 range before confirming start");
            }

            if (current.Phase == TrainingPresentationPhase.AwaitingWeaponPickup
                && (string.IsNullOrEmpty(sessionId) || current.SessionId == sessionId))
            {
                return ServiceResult<TrainingPresentationDto>.Ok(BuildDto());
            }

            if (current.Phase != TrainingPresentationPhase.AwaitingStartConfirmation
                && current.Phase != TrainingPresentationPhase.ModeEntry)
            {
                return FailUnchanged(ErrorCode.InvalidState, "start is only valid while awaiting confirmation");
            }

            TrainingSessionDto session;
            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!trainingSessions.HasActiveSession || trainingSessions.Current.SessionId != sessionId)
                {
                    return FailUnchanged(ErrorCode.NotFound, "session not found");
                }

                session = trainingSessions.Current;
            }
            else if (trainingSessions.HasActiveSession
                     && trainingSessions.Current.Mode == current.Mode)
            {
                session = trainingSessions.Current;
            }
            else
            {
                var create = trainingSessions.Create(
                    current.Mode,
                    string.Empty,
                    TrainingPresentationRules.TrainingRifleId,
                    RandomSeed.Fixed(0));
                if (!create.Success)
                {
                    return FailUnchanged(create.ErrorCode, create.Message);
                }

                session = create.Data;
            }

            var start = trainingSessions.Start(session.SessionId);
            if (!start.Success)
            {
                return FailUnchanged(start.ErrorCode, start.Message);
            }

            var weapon = weaponControl.StartSession(session.SessionId, session.WeaponId, session.Mode);
            if (!weapon.Success)
            {
                return FailUnchanged(weapon.ErrorCode, weapon.Message);
            }

            current.SessionId = session.SessionId;
            current.Phase = TrainingPresentationPhase.AwaitingWeaponPickup;
            current.HoldState = WeaponHoldState.OnRack;
            current.P2CountdownPending = false;
            current.PromptReason = null;
            return PublishOk();
        }

        public ServiceResult<TrainingPresentationDto> HandleWeaponPickup(TrainingWeaponPickupEvent pickup)
        {
            return ApplyWeaponPickup(pickup);
        }

        public ServiceResult<TrainingPresentationDto> ContinueNextRound(string sessionId)
        {
            if (!TryRequireSession(sessionId, out var fail))
            {
                return fail;
            }

            if (current.Mode != TrainingMode.Zeroing100m)
            {
                return FailUnchanged(ErrorCode.InvalidState, "next round is only valid for 100m zeroing");
            }

            if (current.Phase != TrainingPresentationPhase.RoundReview)
            {
                return FailUnchanged(ErrorCode.InvalidState, "next round is only valid during round review");
            }

            var continued = zeroing.ContinueAfterAnalysis(current.SessionId);
            if (!continued.Success)
            {
                return FailUnchanged(continued.ErrorCode, continued.Message);
            }

            if (continued.Data.ShotsRemainingInRound == 0)
            {
                current.Phase = TrainingPresentationPhase.SessionResults;
                current.P2CountdownPending = false;
                current.PromptReason = null;
                return PublishOk();
            }

            current.Phase = TrainingPresentationPhase.LiveFire;
            current.P2CountdownPending = false;
            current.PromptReason = null;
            return PublishOk();
        }

        public ServiceResult<TrainingPresentationDto> NotifyMovingTargetCountdownElapsed(string sessionId)
        {
            if (!TryRequireSession(sessionId, out var fail))
            {
                return fail;
            }

            if (current.Mode != TrainingMode.MovingTarget
                || current.Phase != TrainingPresentationPhase.LiveFire
                || !current.P2CountdownPending)
            {
                return FailUnchanged(ErrorCode.InvalidState, "countdown is not pending");
            }

            current.P2CountdownPending = false;
            current.PromptReason = null;
            return PublishOk();
        }

        public ServiceResult<TrainingPresentationDto> NotifyTrainingCompleted(string sessionId)
        {
            if (!TryRequireSession(sessionId, out var fail))
            {
                return fail;
            }

            if (current.Phase != TrainingPresentationPhase.LiveFire
                && current.Phase != TrainingPresentationPhase.RoundReview)
            {
                return FailUnchanged(ErrorCode.InvalidState, "training is not in a completable phase");
            }

            current.Phase = TrainingPresentationPhase.SessionResults;
            current.P2CountdownPending = false;
            current.PromptReason = null;
            return PublishOk();
        }

        public ServiceResult<TrainingPresentationDto> Retry(string sessionId)
        {
            if (!TryRequireSession(sessionId, out var fail))
            {
                return fail;
            }

            if (current.Phase != TrainingPresentationPhase.SessionResults)
            {
                return FailUnchanged(ErrorCode.InvalidState, "retry is only valid from session results");
            }

            var mode = current.Mode;
            ReleaseActiveSession();
            current = new PresentationRecord
            {
                Mode = mode,
                Phase = TrainingPresentationPhase.AwaitingStartConfirmation,
                FiringStationId = TrainingPresentationRules.FiringStationIdFor(mode),
                HoldState = WeaponHoldState.OnRack
            };
            return PublishOk();
        }

        public ServiceResult<TrainingPresentationDto> Exit(string sessionId)
        {
            if (current == null)
            {
                return ServiceResult<TrainingPresentationDto>.Fail(ErrorCode.NotFound, "presentation not found");
            }

            if (!string.IsNullOrEmpty(sessionId)
                && !string.IsNullOrEmpty(current.SessionId)
                && current.SessionId != sessionId)
            {
                return FailUnchanged(ErrorCode.NotFound, "presentation not found");
            }

            if (current.Phase == TrainingPresentationPhase.Exiting)
            {
                return ServiceResult<TrainingPresentationDto>.Ok(BuildDto());
            }

            ReleaseActiveSession();
            current.SessionId = string.Empty;
            current.Phase = TrainingPresentationPhase.Exiting;
            current.HoldState = WeaponHoldState.OnRack;
            current.P2CountdownPending = false;
            current.PromptReason = null;
            return PublishOk();
        }

        public ServiceResult<TrainingLocomotionPolicyDto> GetLocomotionPolicy(string sessionId)
        {
            if (current != null
                && (string.IsNullOrEmpty(sessionId)
                    || string.IsNullOrEmpty(current.SessionId)
                    || current.SessionId == sessionId))
            {
                return ServiceResult<TrainingLocomotionPolicyDto>.Ok(
                    TrainingPresentationRules.CreateLocomotionPolicy(current.Mode));
            }

            if (!string.IsNullOrEmpty(sessionId)
                && trainingSessions.HasActiveSession
                && trainingSessions.Current.SessionId == sessionId
                && TrainingPresentationRules.IsProneFixedMode(trainingSessions.Current.Mode))
            {
                return ServiceResult<TrainingLocomotionPolicyDto>.Ok(
                    TrainingPresentationRules.CreateLocomotionPolicy(trainingSessions.Current.Mode));
            }

            return ServiceResult<TrainingLocomotionPolicyDto>.Fail(ErrorCode.NotFound, "locomotion policy not found");
        }

        void OnWeaponPickupEvent(TrainingWeaponPickupEvent pickup)
        {
            ApplyWeaponPickup(pickup);
        }

        void OnZeroingRoundCompleted(ZeroingRoundCompletedEvent evt)
        {
            if (current == null
                || current.Mode != TrainingMode.Zeroing100m
                || current.SessionId != evt.SessionId
                || current.Phase != TrainingPresentationPhase.LiveFire)
            {
                return;
            }

            current.Phase = TrainingPresentationPhase.RoundReview;
            current.P2CountdownPending = false;
            current.PromptReason = null;
            Publish();
        }

        ServiceResult<TrainingPresentationDto> ApplyWeaponPickup(TrainingWeaponPickupEvent pickup)
        {
            if (current == null)
            {
                return ServiceResult<TrainingPresentationDto>.Fail(ErrorCode.InvalidState, "presentation not found");
            }

            if (current.Phase == TrainingPresentationPhase.AwaitingStartConfirmation
                || current.Phase == TrainingPresentationPhase.ModeEntry)
            {
                current.PromptReason = TrainingPresentationRules.ReasonPickupBeforeStart;
                Publish();
                return ServiceResult<TrainingPresentationDto>.Fail(
                    ErrorCode.InvalidState,
                    "confirm start before picking up the weapon",
                    BuildDto());
            }

            if (string.IsNullOrEmpty(current.SessionId) || pickup.SessionId != current.SessionId)
            {
                return FailUnchanged(ErrorCode.InvalidState, "weapon pickup session does not match");
            }

            var expectedWeaponId = trainingSessions.HasActiveSession
                ? trainingSessions.Current.WeaponId
                : TrainingPresentationRules.TrainingRifleId;
            if (string.IsNullOrEmpty(pickup.WeaponId) || pickup.WeaponId != expectedWeaponId)
            {
                return FailUnchanged(ErrorCode.InvalidState, "weapon pickup does not match the training rifle");
            }

            if (current.Phase == TrainingPresentationPhase.AwaitingWeaponPickup)
            {
                if (!TrainingPresentationRules.IsValidRearPickup(pickup.PreviousState, pickup.CurrentState))
                {
                    return FailUnchanged(ErrorCode.InvalidState, "rear-hand pickup is required");
                }

                current.HoldState = pickup.CurrentState;
                current.Phase = TrainingPresentationPhase.LiveFire;
                current.P2CountdownPending = current.Mode == TrainingMode.MovingTarget;
                current.PromptReason = null;
                var dto = Publish();
                if (current.P2CountdownPending)
                {
                    eventBus.Publish(new MovingTargetCountdownRequestedEvent
                    {
                        SessionId = current.SessionId,
                        Presentation = dto,
                        CountdownSeconds = TrainingPresentationRules.MovingTargetCountdownSeconds
                    });
                }

                return ServiceResult<TrainingPresentationDto>.Ok(dto);
            }

            if (current.Phase == TrainingPresentationPhase.LiveFire
                || current.Phase == TrainingPresentationPhase.RoundReview
                || current.Phase == TrainingPresentationPhase.SessionResults)
            {
                current.HoldState = pickup.CurrentState;
                current.PromptReason = null;
                return PublishOk();
            }

            return FailUnchanged(ErrorCode.InvalidState, "weapon pickup is not valid in the current phase");
        }

        bool TryRequireSession(string sessionId, out ServiceResult<TrainingPresentationDto> fail)
        {
            fail = default;
            if (current == null)
            {
                fail = ServiceResult<TrainingPresentationDto>.Fail(ErrorCode.NotFound, "presentation not found");
                return false;
            }

            if (string.IsNullOrEmpty(current.SessionId))
            {
                fail = FailUnchanged(ErrorCode.NotFound, "session not found");
                return false;
            }

            if (!string.IsNullOrEmpty(sessionId) && current.SessionId != sessionId)
            {
                fail = FailUnchanged(ErrorCode.NotFound, "session not found");
                return false;
            }

            return true;
        }

        void ReleaseActiveSession()
        {
            if (trainingSessions.HasActiveSession)
            {
                trainingSessions.Cancel(trainingSessions.Current.SessionId);
            }
        }

        ServiceResult<TrainingPresentationDto> FailUnchanged(ErrorCode errorCode, string message)
        {
            return ServiceResult<TrainingPresentationDto>.Fail(
                errorCode,
                message,
                current == null ? TrainingPresentationDto.Empty : BuildDto());
        }

        ServiceResult<TrainingPresentationDto> PublishOk()
        {
            return ServiceResult<TrainingPresentationDto>.Ok(Publish());
        }

        TrainingPresentationDto Publish()
        {
            var dto = BuildDto();
            if (SnapshotsEqual(lastPublished, dto))
            {
                return dto;
            }

            lastPublished = dto;
            eventBus.Publish(new TrainingPresentationChangedEvent { Presentation = dto });
            PresentationChanged?.Invoke(dto);
            return dto;
        }

        static bool SnapshotsEqual(TrainingPresentationDto left, TrainingPresentationDto right)
        {
            return left.SessionId == right.SessionId
                   && left.Mode == right.Mode
                   && left.Phase == right.Phase
                   && left.ActiveScreen == right.ActiveScreen
                   && left.LargePanelVisible == right.LargePanelVisible
                   && left.MinimalHudVisible == right.MinimalHudVisible
                   && left.ShootingAllowed == right.ShootingAllowed
                   && left.AwaitingWeaponPickup == right.AwaitingWeaponPickup
                   && left.FiringStationId == right.FiringStationId
                   && left.VisibilityReason == right.VisibilityReason;
        }

        TrainingPresentationDto BuildDto()
        {
            return TrainingPresentationRules.Project(
                current.SessionId,
                current.Mode,
                current.Phase,
                current.FiringStationId,
                TrainingPresentationRules.IsWeaponHeld(current.HoldState),
                current.P2CountdownPending,
                current.PromptReason);
        }

        sealed class PresentationRecord
        {
            public string SessionId = string.Empty;
            public TrainingMode Mode;
            public TrainingPresentationPhase Phase;
            public string FiringStationId = string.Empty;
            public WeaponHoldState HoldState;
            public bool P2CountdownPending;
            public string PromptReason;
        }
    }
}
