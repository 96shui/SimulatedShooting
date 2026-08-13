using System;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public sealed class TrainingSessionService : ITrainingSessionService
    {
        const string DefaultZeroingMapId = "zeroing-range-100m";
        const string DefaultMovingTargetMapId = "moving-target-range";
        const string DefaultTrainingWeaponId = "training-rifle";

        readonly IGameEventBus eventBus;
        SessionRecord activeSession;

        public TrainingSessionService(IGameEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        public TrainingSessionDto Current => activeSession?.ToDto() ?? default;

        public bool HasActiveSession => activeSession != null && !activeSession.IsTerminal;

        public ServiceResult<TrainingSessionDto> Create(TrainingMode mode, string mapId, string weaponId, RandomSeed seed)
        {
            if (mode == TrainingMode.None)
            {
                return ServiceResult<TrainingSessionDto>.Fail(ErrorCode.InvalidInput, "mode is required");
            }

            if (HasActiveSession)
            {
                return ServiceResult<TrainingSessionDto>.Fail(
                    ErrorCode.InvalidState,
                    "active session already exists",
                    Current);
            }

            var proneFixed = mode == TrainingMode.Zeroing100m || mode == TrainingMode.MovingTarget;
            activeSession = new SessionRecord
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Mode = mode,
                State = SessionState.Preparing,
                MapId = string.IsNullOrEmpty(mapId) ? GetDefaultMapId(mode) : mapId,
                WeaponId = string.IsNullOrEmpty(weaponId) ? GetDefaultWeaponId(mode) : weaponId,
                Seed = seed,
                Player = proneFixed
                    ? new PlayerStatusDto
                    {
                        Health = PlayerStatusDto.Default.Health,
                        IsAlive = true,
                        Posture = PlayerPosture.Prone,
                        Shoulder = ShoulderSide.Right,
                        CornerShootingAvailable = false
                    }
                    : PlayerStatusDto.Default,
                PostureMode = TrainingPostureMode.ProneFixed,
                FiringStationId = proneFixed
                    ? TrainingPresentationRules.FiringStationIdFor(mode)
                    : string.Empty,
                ArtificialLocomotionAllowed = !proneFixed,
                Squad = SquadStatusDto.Empty,
                Ammo = AmmoDto.Empty
            };

            return ServiceResult<TrainingSessionDto>.Ok(activeSession.ToDto());
        }

        public ServiceResult<TrainingSessionDto> Start(string sessionId)
        {
            if (!TryGetMutableSession(sessionId, out var session, out var failResult))
            {
                return failResult;
            }

            if (session.State == SessionState.Running)
            {
                return ServiceResult<TrainingSessionDto>.Ok(session.ToDto());
            }

            if (session.State != SessionState.Preparing && session.State != SessionState.NotStarted)
            {
                return ServiceResult<TrainingSessionDto>.Fail(
                    ErrorCode.InvalidState,
                    $"cannot start from state {session.State}",
                    session.ToDto());
            }

            session.State = SessionState.Running;
            session.RunningSinceUtc = DateTime.UtcNow;

            var dto = session.ToDto();
            eventBus.Publish(new SessionStartedEvent { Session = dto });
            return ServiceResult<TrainingSessionDto>.Ok(dto);
        }

        public ServiceResult<TrainingSessionDto> Pause(string sessionId)
        {
            if (!TryGetMutableSession(sessionId, out var session, out var failResult))
            {
                return failResult;
            }

            if (session.State == SessionState.Paused)
            {
                return ServiceResult<TrainingSessionDto>.Ok(session.ToDto());
            }

            if (session.State != SessionState.Running)
            {
                return ServiceResult<TrainingSessionDto>.Fail(
                    ErrorCode.InvalidState,
                    $"cannot pause from state {session.State}",
                    session.ToDto());
            }

            session.AccumulateElapsed(DateTime.UtcNow);
            session.State = SessionState.Paused;
            session.RunningSinceUtc = null;

            return ServiceResult<TrainingSessionDto>.Ok(session.ToDto());
        }

        public ServiceResult<TrainingSessionDto> Resume(string sessionId)
        {
            if (!TryGetMutableSession(sessionId, out var session, out var failResult))
            {
                return failResult;
            }

            if (session.State == SessionState.Running)
            {
                return ServiceResult<TrainingSessionDto>.Ok(session.ToDto());
            }

            if (session.State != SessionState.Paused)
            {
                return ServiceResult<TrainingSessionDto>.Fail(
                    ErrorCode.InvalidState,
                    $"cannot resume from state {session.State}",
                    session.ToDto());
            }

            session.State = SessionState.Running;
            session.RunningSinceUtc = DateTime.UtcNow;

            return ServiceResult<TrainingSessionDto>.Ok(session.ToDto());
        }

        public ServiceResult<TrainingResultDto> End(string sessionId, SessionEndReason reason)
        {
            if (!TryGetMutableSession(sessionId, out var session, out var failResult))
            {
                return ServiceResult<TrainingResultDto>.Fail(failResult.ErrorCode, failResult.Message);
            }

            if (session.IsTerminal)
            {
                return ServiceResult<TrainingResultDto>.Fail(
                    ErrorCode.InvalidState,
                    "session already ended",
                    session.LastResult ?? default);
            }

            session.AccumulateElapsed(DateTime.UtcNow);
            session.State = reason == SessionEndReason.Cancelled
                ? SessionState.Cancelled
                : SessionState.Completed;
            session.RunningSinceUtc = null;

            var result = session.BuildResult(reason);
            session.LastResult = result;

            eventBus.Publish(new SessionEndedEvent
            {
                Session = session.ToDto(),
                Result = result,
                Reason = reason
            });

            return ServiceResult<TrainingResultDto>.Ok(result);
        }

        public ServiceResult<Unit> Cancel(string sessionId)
        {
            var endResult = End(sessionId, SessionEndReason.Cancelled);
            if (!endResult.Success)
            {
                return ServiceResult<Unit>.Fail(endResult.ErrorCode, endResult.Message);
            }

            return ServiceResult<Unit>.Ok(Unit.Value);
        }

        bool TryGetMutableSession(
            string sessionId,
            out SessionRecord session,
            out ServiceResult<TrainingSessionDto> failResult)
        {
            session = null;
            failResult = default;

            if (string.IsNullOrEmpty(sessionId))
            {
                failResult = ServiceResult<TrainingSessionDto>.Fail(ErrorCode.InvalidInput, "sessionId is required");
                return false;
            }

            if (activeSession == null || activeSession.SessionId != sessionId)
            {
                failResult = ServiceResult<TrainingSessionDto>.Fail(ErrorCode.NotFound, "session not found");
                return false;
            }

            session = activeSession;
            return true;
        }

        static string GetDefaultMapId(TrainingMode mode)
        {
            switch (mode)
            {
                case TrainingMode.Zeroing100m:
                    return DefaultZeroingMapId;
                case TrainingMode.MovingTarget:
                    return DefaultMovingTargetMapId;
                default:
                    return string.Empty;
            }
        }

        static string GetDefaultWeaponId(TrainingMode mode)
        {
            return mode == TrainingMode.Zeroing100m || mode == TrainingMode.MovingTarget
                ? DefaultTrainingWeaponId
                : string.Empty;
        }

        sealed class SessionRecord
        {
            public string SessionId { get; set; }
            public TrainingMode Mode { get; set; }
            public SessionState State { get; set; }
            public string MapId { get; set; }
            public string WeaponId { get; set; }
            public RandomSeed Seed { get; set; }
            public AmmoDto Ammo { get; set; }
            public PlayerStatusDto Player { get; set; }
            public TrainingPostureMode PostureMode { get; set; }
            public string FiringStationId { get; set; }
            public bool ArtificialLocomotionAllowed { get; set; }
            public SquadStatusDto Squad { get; set; }
            public ResultGrade CurrentGrade { get; set; }
            public string FailureReason { get; set; }
            public float ElapsedSeconds { get; set; }
            public DateTime? RunningSinceUtc { get; set; }
            public TrainingResultDto? LastResult { get; set; }

            public bool IsTerminal =>
                State == SessionState.Completed
                || State == SessionState.Failed
                || State == SessionState.Cancelled;

            public void AccumulateElapsed(DateTime nowUtc)
            {
                if (!RunningSinceUtc.HasValue)
                {
                    return;
                }

                ElapsedSeconds += (float)(nowUtc - RunningSinceUtc.Value).TotalSeconds;
                RunningSinceUtc = null;
            }

            public float GetElapsedSeconds(DateTime nowUtc)
            {
                if (!RunningSinceUtc.HasValue)
                {
                    return ElapsedSeconds;
                }

                return ElapsedSeconds + (float)(nowUtc - RunningSinceUtc.Value).TotalSeconds;
            }

            public TrainingSessionDto ToDto()
            {
                return new TrainingSessionDto
                {
                    SessionId = SessionId ?? string.Empty,
                    Mode = Mode,
                    State = State,
                    MapId = MapId ?? string.Empty,
                    WeaponId = WeaponId ?? string.Empty,
                    Seed = Seed,
                    ElapsedSeconds = GetElapsedSeconds(DateTime.UtcNow),
                    Ammo = Ammo,
                    Player = Player,
                    PostureMode = PostureMode,
                    FiringStationId = FiringStationId ?? string.Empty,
                    ArtificialLocomotionAllowed = ArtificialLocomotionAllowed,
                    Squad = Squad,
                    CurrentGrade = CurrentGrade,
                    FailureReason = FailureReason ?? string.Empty
                };
            }

            public TrainingResultDto BuildResult(SessionEndReason reason)
            {
                var dto = ToDto();
                return new TrainingResultDto
                {
                    SessionId = dto.SessionId,
                    Mode = dto.Mode,
                    Victory = reason == SessionEndReason.Completed,
                    Grade = CurrentGrade,
                    ElapsedSeconds = dto.ElapsedSeconds,
                    RemainingAmmo = dto.Ammo.CurrentMagazine + dto.Ammo.ReserveAmmo,
                    SummaryJson = string.Empty
                };
            }
        }
    }
}
