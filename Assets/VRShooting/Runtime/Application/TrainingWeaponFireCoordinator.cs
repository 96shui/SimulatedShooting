using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 组合 P2 倒计时/禁射、P1 单发回归和连射调度。
    /// </summary>
    public sealed class TrainingWeaponFireCoordinator : ITrainingWeaponFireCoordinator
    {
        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly ITrainingPresentationService presentation;
        readonly IWeaponControlService weapons;
        readonly IWeaponAutomaticFireService autoFire;
        readonly IMovingTargetService movingTarget;
        bool recording;

        public TrainingWeaponFireCoordinator(
            IGameEventBus eventBus,
            ITrainingSessionService trainingSessions,
            ITrainingPresentationService presentation,
            IWeaponControlService weapons,
            IWeaponAutomaticFireService autoFire,
            IMovingTargetService movingTarget)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
            this.presentation = presentation;
            this.weapons = weapons;
            this.autoFire = autoFire;
            this.movingTarget = movingTarget;

            this.eventBus.Subscribe<WeaponShotResultEvent>(OnWeaponShot);
            this.eventBus.Subscribe<WeaponFireSequenceChangedEvent>(OnSequenceChanged);
            this.eventBus.Subscribe<TrainingPresentationChangedEvent>(OnPresentationChanged);
        }

        public ServiceResult<WeaponFireSequenceStateDto> Tick(
            string sessionId,
            float deltaTime,
            WeaponTriggerStateInputDto trigger,
            WeaponFireInputDto latestFireSnapshot)
        {
            sessionId = ResolveSessionId(sessionId);
            if (string.IsNullOrEmpty(sessionId))
            {
                return ServiceResult<WeaponFireSequenceStateDto>.Fail(
                    ErrorCode.InvalidState,
                    "session id is required",
                    WeaponFireSequenceStateDto.Empty);
            }

            var fireMode = ResolveFireMode(sessionId);
            var started = EnsureAutoFireSession(sessionId, fireMode);
            if (!started.Success)
            {
                return started;
            }

            var allow = IsFiringAllowed(sessionId, fireMode);
            var autoState = autoFire.GetState(sessionId);
            if (autoState.Success && IsActive(autoState.Data.Phase) && !allow)
            {
                autoFire.Cancel(sessionId, ResolveStopReason(sessionId, fireMode));
            }

            var triggerToApply = allow
                ? WithSession(trigger, sessionId)
                : SuppressPressed(trigger, sessionId);
            autoFire.UpdateTrigger(triggerToApply);

            if (!allow)
            {
                return autoFire.GetState(sessionId);
            }

            var snapshot = latestFireSnapshot;
            if (string.IsNullOrEmpty(snapshot.SessionId))
            {
                snapshot = new WeaponFireInputDto
                {
                    SessionId = sessionId,
                    MuzzlePosition = latestFireSnapshot.MuzzlePosition,
                    RawAimDirection = latestFireSnapshot.RawAimDirection,
                    AimDirection = latestFireSnapshot.AimDirection,
                    WeaponPosition = latestFireSnapshot.WeaponPosition,
                    AimMotionOffsetCm = latestFireSnapshot.AimMotionOffsetCm,
                    Stability01 = latestFireSnapshot.Stability01,
                    TwoHandGripActive = latestFireSnapshot.TwoHandGripActive,
                    AimMode = latestFireSnapshot.AimMode,
                    ShoulderSide = latestFireSnapshot.ShoulderSide,
                    Hit = latestFireSnapshot.Hit,
                    HitPoint = latestFireSnapshot.HitPoint,
                    HitObjectId = latestFireSnapshot.HitObjectId
                };
            }

            return autoFire.Tick(sessionId, deltaTime, snapshot);
        }

        void OnWeaponShot(WeaponShotResultEvent evt)
        {
            if (recording || evt.Result.IsValidShot == false)
            {
                return;
            }

            var sessionId = evt.Result.SessionId;
            if (!trainingSessions.HasActiveSession || trainingSessions.Current.SessionId != sessionId)
            {
                return;
            }

            if (trainingSessions.Current.Mode != TrainingMode.MovingTarget)
            {
                return;
            }

            var sequence = autoFire.GetState(sessionId);
            if (!sequence.Success || string.IsNullOrEmpty(sequence.Data.SequenceId) || sequence.Data.ShotsFired <= 0)
            {
                return;
            }

            recording = true;
            try
            {
                movingTarget.RecordShot(
                    sessionId,
                    sequence.Data.SequenceId,
                    sequence.Data.ShotsFired,
                    evt.Result);
            }
            finally
            {
                recording = false;
            }
        }

        void OnSequenceChanged(WeaponFireSequenceChangedEvent evt)
        {
            CompleteIfStopped(evt.State);
        }

        void CompleteIfStopped(WeaponFireSequenceStateDto state)
        {
            if (state.Phase != WeaponFireSequencePhase.Stopped
                || !state.StopReason.HasValue
                || string.IsNullOrEmpty(state.SequenceId))
            {
                return;
            }

            if (!trainingSessions.HasActiveSession
                || trainingSessions.Current.SessionId != state.SessionId
                || trainingSessions.Current.Mode != TrainingMode.MovingTarget)
            {
                return;
            }

            movingTarget.CompleteFireSequence(state.SessionId, state.SequenceId, state.StopReason.Value);
        }

        void OnPresentationChanged(TrainingPresentationChangedEvent evt)
        {
            var dto = evt.Presentation;
            if (string.IsNullOrEmpty(dto.SessionId))
            {
                return;
            }

            if (dto.Phase != TrainingPresentationPhase.SessionResults
                && dto.Phase != TrainingPresentationPhase.Exiting
                && dto.Phase != TrainingPresentationPhase.AwaitingStartConfirmation)
            {
                return;
            }

            var state = autoFire.GetState(dto.SessionId);
            if (state.Success && IsActive(state.Data.Phase))
            {
                autoFire.Cancel(
                    dto.SessionId,
                    dto.Phase == TrainingPresentationPhase.SessionResults
                        ? WeaponFireStopReason.TrainingCompleted
                        : WeaponFireStopReason.WeaponBecameInvalid);
            }
        }

        ServiceResult<WeaponFireSequenceStateDto> EnsureAutoFireSession(string sessionId, WeaponFireMode fireMode)
        {
            var existing = autoFire.GetState(sessionId);
            if (existing.Success && existing.Data.FireMode == fireMode)
            {
                return existing;
            }

            var config = fireMode == WeaponFireMode.InitialTwoThenAutomatic
                ? WeaponAutoFireConfigDto.P2Default
                : WeaponAutoFireConfigDto.P1SingleShot;
            return autoFire.StartSession(sessionId, fireMode, config);
        }

        bool IsFiringAllowed(string sessionId, WeaponFireMode fireMode)
        {
            var presented = presentation.Get(sessionId);
            if (presented.Success && !presented.Data.ShootingAllowed)
            {
                return false;
            }

            var weapon = weapons.GetState(sessionId);
            if (!weapon.Success
                || weapon.Data.HoldState != WeaponHoldState.TwoHandHeld
                || !weapon.Data.RearHandTracked
                || !weapon.Data.FrontHandTracked)
            {
                return false;
            }

            if (fireMode != WeaponFireMode.InitialTwoThenAutomatic)
            {
                return true;
            }

            var moving = movingTarget.GetSession(sessionId);
            return moving.Success && moving.Data.CanShoot;
        }

        WeaponFireStopReason ResolveStopReason(string sessionId, WeaponFireMode fireMode)
        {
            var weapon = weapons.GetState(sessionId);
            if (!weapon.Success
                || weapon.Data.HoldState != WeaponHoldState.TwoHandHeld
                || !weapon.Data.RearHandTracked
                || !weapon.Data.FrontHandTracked)
            {
                return WeaponFireStopReason.WeaponBecameInvalid;
            }

            if (fireMode == WeaponFireMode.InitialTwoThenAutomatic)
            {
                var moving = movingTarget.GetSession(sessionId);
                if (moving.Success && moving.Data.Phase == TargetMovePhase.Completed)
                {
                    return WeaponFireStopReason.TrainingCompleted;
                }
            }

            return WeaponFireStopReason.ShootingBecameForbidden;
        }

        WeaponFireMode ResolveFireMode(string sessionId)
        {
            if (trainingSessions.HasActiveSession && trainingSessions.Current.SessionId == sessionId)
            {
                return trainingSessions.Current.Mode == TrainingMode.MovingTarget
                    ? WeaponFireMode.InitialTwoThenAutomatic
                    : WeaponFireMode.SingleShot;
            }

            var weapon = weapons.GetState(sessionId);
            return weapon.Success ? weapon.Data.FireMode : WeaponFireMode.SingleShot;
        }

        string ResolveSessionId(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                return sessionId;
            }

            return trainingSessions.HasActiveSession ? trainingSessions.Current.SessionId : string.Empty;
        }

        static bool IsActive(WeaponFireSequencePhase phase)
        {
            return phase == WeaponFireSequencePhase.InitialTwoShots
                   || phase == WeaponFireSequencePhase.ContinuousFire;
        }

        static WeaponTriggerStateInputDto WithSession(WeaponTriggerStateInputDto trigger, string sessionId)
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = sessionId,
                Value01 = trigger.Value01,
                Pressed = trigger.Pressed,
                Held = trigger.Held,
                Released = trigger.Released
            };
        }

        static WeaponTriggerStateInputDto SuppressPressed(WeaponTriggerStateInputDto trigger, string sessionId)
        {
            return new WeaponTriggerStateInputDto
            {
                SessionId = sessionId,
                Value01 = trigger.Value01,
                Pressed = false,
                Held = trigger.Held,
                Released = trigger.Released
            };
        }
    }
}
