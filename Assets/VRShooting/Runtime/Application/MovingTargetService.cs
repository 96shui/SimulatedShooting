using System;
using System.Collections.Generic;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public sealed class MovingTargetService : IMovingTargetService
    {
        const string DefaultMapId = "moving-target-range";
        const string DefaultWeaponId = "training-rifle";

        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly Dictionary<string, SessionRecord> sessions = new Dictionary<string, SessionRecord>();

        public MovingTargetService(IGameEventBus eventBus, ITrainingSessionService trainingSessions)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
        }

        public ServiceResult<IReadOnlyList<float>> GetAvailableSpeeds()
        {
            return ServiceResult<IReadOnlyList<float>>.Ok(MovingTargetRules.AvailableSpeeds);
        }

        public ServiceResult<MovingTargetSessionDto> StartSession(MovingTargetSettingsDto settings, RandomSeed seed)
        {
            if (!MovingTargetRules.IsAllowedSpeed(settings.SpeedMetersPerSecond))
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(
                    ErrorCode.InvalidInput,
                    "speed must be 3, 4 or 5 m/s",
                    MovingTargetSessionDto.Empty);
            }

            if (trainingSessions.HasActiveSession)
            {
                var active = trainingSessions.Current;
                if (active.Mode != TrainingMode.MovingTarget)
                {
                    return ServiceResult<MovingTargetSessionDto>.Fail(
                        ErrorCode.InvalidState,
                        "active session is not a moving target session",
                        MovingTargetSessionDto.Empty);
                }

                if (sessions.TryGetValue(active.SessionId, out var existing)
                    && existing.Sim.Phase != TargetMovePhase.Completed)
                {
                    var reason = existing.Sim.Phase == TargetMovePhase.WaitingCountdown
                                 && AreEqual(existing.SpeedMetersPerSecond, settings.SpeedMetersPerSecond)
                        ? "moving target session already started"
                        : "cannot change speed or restart during an active moving target session";
                    return ServiceResult<MovingTargetSessionDto>.Fail(
                        ErrorCode.InvalidState,
                        reason,
                        existing.ToDto());
                }
            }

            var sessionResult = trainingSessions.HasActiveSession
                ? ServiceResult<TrainingSessionDto>.Ok(trainingSessions.Current)
                : trainingSessions.Create(TrainingMode.MovingTarget, DefaultMapId, DefaultWeaponId, seed);
            if (!sessionResult.Success)
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(
                    sessionResult.ErrorCode,
                    sessionResult.Message,
                    MovingTargetSessionDto.Empty);
            }

            var record = new SessionRecord
            {
                SessionId = sessionResult.Data.SessionId,
                Seed = seed,
                SpeedMetersPerSecond = settings.SpeedMetersPerSecond,
                Sim = MovingTargetRules.CreateInitial()
            };
            sessions[record.SessionId] = record;

            var startResult = trainingSessions.Start(sessionResult.Data.SessionId);
            if (!startResult.Success)
            {
                sessions.Remove(record.SessionId);
                return ServiceResult<MovingTargetSessionDto>.Fail(
                    startResult.ErrorCode,
                    startResult.Message,
                    MovingTargetSessionDto.Empty);
            }

            return ServiceResult<MovingTargetSessionDto>.Ok(PublishState(record));
        }

        public ServiceResult<MovingTargetSessionDto> GetSession(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure, out var message))
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(failure, message, MovingTargetSessionDto.Empty);
            }

            return ServiceResult<MovingTargetSessionDto>.Ok(record.ToDto());
        }

        public ServiceResult<MovingTargetSessionDto> Tick(string sessionId, float deltaTime)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure, out var message))
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(failure, message, MovingTargetSessionDto.Empty);
            }

            if (float.IsNaN(deltaTime) || deltaTime < 0f)
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(
                    ErrorCode.InvalidInput,
                    "deltaTime must be a finite non-negative value",
                    record.ToDto());
            }

            if (record.Sim.Phase == TargetMovePhase.Completed)
            {
                return ServiceResult<MovingTargetSessionDto>.Ok(record.ToDto());
            }

            if (IsCancelled(record.SessionId))
            {
                return ServiceResult<MovingTargetSessionDto>.Fail(
                    ErrorCode.InvalidState,
                    "session has been cancelled",
                    record.ToDto());
            }

            if (IsPaused(record.SessionId))
            {
                return ServiceResult<MovingTargetSessionDto>.Ok(record.ToDto());
            }

            record.Sim = MovingTargetRules.Advance(record.Sim, record.SpeedMetersPerSecond, deltaTime);
            if (record.Sim.CountdownElapsedThisTick)
            {
                eventBus.Publish(new MovingTargetCountdownElapsedEvent
                {
                    SessionId = record.SessionId,
                    Session = record.ToDto()
                });
            }

            if (record.Sim.CompletedThisTick)
            {
                FinalizeOpenSequences(record, WeaponFireStopReason.TrainingCompleted);
                var result = record.BuildResult();
                record.Result = result;
                if (trainingSessions.HasActiveSession && trainingSessions.Current.SessionId == record.SessionId)
                {
                    trainingSessions.End(record.SessionId, SessionEndReason.Completed);
                }

                eventBus.Publish(new MovingTargetSessionCompletedEvent
                {
                    SessionId = record.SessionId,
                    Result = result
                });
            }

            return ServiceResult<MovingTargetSessionDto>.Ok(PublishState(record));
        }

        public ServiceResult<MovingTargetShotRecordDto> RecordShot(
            string sessionId,
            string sequenceId,
            int shotIndexInSequence,
            WeaponShotResultDto shot)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure, out var message))
            {
                return ServiceResult<MovingTargetShotRecordDto>.Fail(failure, message, MovingTargetShotRecordDto.Empty);
            }

            if (string.IsNullOrEmpty(sequenceId) || shotIndexInSequence < 1)
            {
                return ServiceResult<MovingTargetShotRecordDto>.Fail(
                    ErrorCode.InvalidInput,
                    "sequenceId and shotIndexInSequence are required",
                    MovingTargetShotRecordDto.Empty);
            }

            if (record.TryGetShot(sequenceId, shotIndexInSequence, out var existing))
            {
                return ServiceResult<MovingTargetShotRecordDto>.Ok(existing);
            }

            if (!shot.IsValidShot)
            {
                return ServiceResult<MovingTargetShotRecordDto>.Fail(
                    ErrorCode.InvalidInput,
                    "shot is not valid",
                    MovingTargetShotRecordDto.Empty);
            }

            if (!MovingTargetRules.CanShoot(record.Sim.Phase))
            {
                return ServiceResult<MovingTargetShotRecordDto>.Fail(
                    ErrorCode.InvalidState,
                    "shots are not scored during countdown, endpoint hold or results",
                    MovingTargetShotRecordDto.Empty);
            }

            if (record.TryGetSequence(sequenceId, out var sequence) && sequence.Completed)
            {
                return ServiceResult<MovingTargetShotRecordDto>.Fail(
                    ErrorCode.InvalidState,
                    "fire sequence already stopped",
                    MovingTargetShotRecordDto.Empty);
            }

            sequence ??= record.StartSequence(sequenceId, record.Sim.ElapsedSeconds);
            var scored = new MovingTargetShotRecordDto
            {
                SequenceId = sequenceId,
                ShotIndexInSequence = shotIndexInSequence,
                GlobalShotIndex = record.ShotsFired + 1,
                FireTime = record.Sim.ElapsedSeconds,
                Hit = shot.Hit,
                TargetPhase = record.Sim.Phase,
                RouteProgress01 = record.Sim.RouteProgress01
            };
            sequence.AddShot(scored);
            record.CurrentSequenceId = sequenceId;
            record.FirePhase = shotIndexInSequence >= 3
                ? WeaponFireSequencePhase.ContinuousFire
                : WeaponFireSequencePhase.InitialTwoShots;

            eventBus.Publish(new MovingTargetShotRecordedEvent
            {
                SessionId = record.SessionId,
                Shot = scored
            });
            PublishState(record);
            return ServiceResult<MovingTargetShotRecordDto>.Ok(scored);
        }

        public ServiceResult<FireSequenceRecordDto> CompleteFireSequence(
            string sessionId,
            string sequenceId,
            WeaponFireStopReason stopReason)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure, out var message))
            {
                return ServiceResult<FireSequenceRecordDto>.Fail(failure, message, FireSequenceRecordDto.Empty);
            }

            if (!record.TryGetSequence(sequenceId, out var sequence))
            {
                return ServiceResult<FireSequenceRecordDto>.Fail(
                    ErrorCode.NotFound,
                    "fire sequence not found",
                    FireSequenceRecordDto.Empty);
            }

            if (sequence.Completed)
            {
                return ServiceResult<FireSequenceRecordDto>.Ok(sequence.ToDto());
            }

            sequence.Complete(stopReason);
            record.FirePhase = WeaponFireSequencePhase.Stopped;
            var dto = sequence.ToDto();
            eventBus.Publish(new MovingTargetFireSequenceCompletedEvent
            {
                SessionId = record.SessionId,
                Sequence = dto
            });
            PublishState(record);
            return ServiceResult<FireSequenceRecordDto>.Ok(dto);
        }

        public ServiceResult<MovingTargetResultDto> CompleteSession(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure, out var message))
            {
                return ServiceResult<MovingTargetResultDto>.Fail(failure, message, MovingTargetResultDto.Empty);
            }

            if (record.Sim.Phase != TargetMovePhase.Completed)
            {
                return ServiceResult<MovingTargetResultDto>.Fail(
                    ErrorCode.InvalidState,
                    "route is not complete",
                    MovingTargetResultDto.Empty);
            }

            if (!record.Result.HasValue)
            {
                FinalizeOpenSequences(record, WeaponFireStopReason.TrainingCompleted);
                record.Result = record.BuildResult();
            }

            return ServiceResult<MovingTargetResultDto>.Ok(record.Result.Value);
        }

        internal void ReleaseSession(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                sessions.Remove(sessionId);
            }
        }

        bool TryGetRecord(string sessionId, out SessionRecord record, out ErrorCode failure, out string message)
        {
            record = null;
            failure = ErrorCode.NotFound;
            message = "moving target session not found";

            if (string.IsNullOrEmpty(sessionId))
            {
                failure = ErrorCode.InvalidInput;
                message = "sessionId is required";
                return false;
            }

            return sessions.TryGetValue(sessionId, out record);
        }

        bool IsPaused(string sessionId)
        {
            return trainingSessions.HasActiveSession
                   && trainingSessions.Current.SessionId == sessionId
                   && trainingSessions.Current.State == SessionState.Paused;
        }

        bool IsCancelled(string sessionId)
        {
            if (trainingSessions.HasActiveSession || string.IsNullOrEmpty(trainingSessions.Current.SessionId))
            {
                return false;
            }

            var current = trainingSessions.Current;
            return current.SessionId == sessionId && current.State == SessionState.Cancelled;
        }

        static void FinalizeOpenSequences(SessionRecord record, WeaponFireStopReason reason)
        {
            for (var i = 0; i < record.Sequences.Count; i++)
            {
                if (!record.Sequences[i].Completed)
                {
                    record.Sequences[i].Complete(reason);
                }
            }

            record.FirePhase = WeaponFireSequencePhase.Stopped;
        }

        MovingTargetSessionDto PublishState(SessionRecord record)
        {
            var dto = record.ToDto();
            eventBus.Publish(new MovingTargetStateChangedEvent { Session = dto });
            return dto;
        }

        static bool AreEqual(float left, float right)
        {
            return Math.Abs(left - right) <= 0.0001f;
        }

        sealed class SessionRecord
        {
            public string SessionId;
            public RandomSeed Seed;
            public float SpeedMetersPerSecond;
            public MovingTargetSimState Sim;
            public WeaponFireSequencePhase FirePhase;
            public string CurrentSequenceId = string.Empty;
            public readonly List<SequenceRecord> Sequences = new List<SequenceRecord>();
            public MovingTargetResultDto? Result;

            public int ShotsFired
            {
                get
                {
                    var count = 0;
                    for (var i = 0; i < Sequences.Count; i++)
                    {
                        count += Sequences[i].Shots.Count;
                    }

                    return count;
                }
            }

            public int HitCount
            {
                get
                {
                    var count = 0;
                    for (var i = 0; i < Sequences.Count; i++)
                    {
                        count += Sequences[i].HitCount;
                    }

                    return count;
                }
            }

            public MovingTargetSessionDto ToDto()
            {
                return new MovingTargetSessionDto
                {
                    SessionId = SessionId,
                    SpeedMetersPerSecond = SpeedMetersPerSecond,
                    Phase = Sim.Phase,
                    FirePhase = FirePhase,
                    CountdownSecondsRemaining = Sim.CountdownSecondsRemaining,
                    ShotsFired = ShotsFired,
                    HitCount = HitCount,
                    CanShoot = MovingTargetRules.CanShoot(Sim.Phase),
                    DirectionLabel = MovingTargetRules.DirectionLabel(Sim.Phase),
                    RouteProgress01 = Sim.RouteProgress01,
                    LegProgress01 = Sim.LegProgress01,
                    EndpointHoldSecondsRemaining = Sim.EndpointHoldSecondsRemaining
                };
            }

            public MovingTargetResultDto BuildResult()
            {
                var sequences = new FireSequenceRecordDto[Sequences.Count];
                for (var i = 0; i < Sequences.Count; i++)
                {
                    sequences[i] = Sequences[i].ToDto();
                }

                var shotsFired = ShotsFired;
                var hits = HitCount;
                return new MovingTargetResultDto
                {
                    SessionId = SessionId,
                    SpeedMetersPerSecond = SpeedMetersPerSecond,
                    TotalAmmoConsumed = shotsFired,
                    TotalShotsFired = shotsFired,
                    HitCount = hits,
                    HitRate01 = MovingTargetRules.ComputeHitRate01(hits, shotsFired),
                    ElapsedSeconds = Sim.ElapsedSeconds,
                    Grade = MovingTargetRules.ComputeGrade(hits),
                    FireSequences = sequences
                };
            }

            public SequenceRecord StartSequence(string sequenceId, double startTime)
            {
                var sequence = new SequenceRecord
                {
                    SequenceId = sequenceId,
                    StartTime = startTime
                };
                Sequences.Add(sequence);
                return sequence;
            }

            public bool TryGetSequence(string sequenceId, out SequenceRecord sequence)
            {
                for (var i = 0; i < Sequences.Count; i++)
                {
                    if (Sequences[i].SequenceId == sequenceId)
                    {
                        sequence = Sequences[i];
                        return true;
                    }
                }

                sequence = null;
                return false;
            }

            public bool TryGetShot(string sequenceId, int shotIndexInSequence, out MovingTargetShotRecordDto shot)
            {
                shot = default;
                if (!TryGetSequence(sequenceId, out var sequence))
                {
                    return false;
                }

                return sequence.TryGetShot(shotIndexInSequence, out shot);
            }
        }

        sealed class SequenceRecord
        {
            public string SequenceId;
            public double StartTime;
            public WeaponFireStopReason StopReason;
            public bool Completed;
            public readonly List<MovingTargetShotRecordDto> Shots = new List<MovingTargetShotRecordDto>();

            public int HitCount
            {
                get
                {
                    var count = 0;
                    for (var i = 0; i < Shots.Count; i++)
                    {
                        if (Shots[i].Hit)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

            public void AddShot(MovingTargetShotRecordDto shot)
            {
                Shots.Add(shot);
            }

            public void Complete(WeaponFireStopReason reason)
            {
                Completed = true;
                StopReason = reason;
            }

            public bool TryGetShot(int shotIndexInSequence, out MovingTargetShotRecordDto shot)
            {
                for (var i = 0; i < Shots.Count; i++)
                {
                    if (Shots[i].ShotIndexInSequence == shotIndexInSequence)
                    {
                        shot = Shots[i];
                        return true;
                    }
                }

                shot = default;
                return false;
            }

            public FireSequenceRecordDto ToDto()
            {
                return new FireSequenceRecordDto
                {
                    SequenceId = SequenceId,
                    StartTime = StartTime,
                    ShotCount = Shots.Count,
                    HitCount = HitCount,
                    EnteredContinuousFire = Shots.Count >= 3,
                    StopReason = StopReason,
                    Shots = Shots.ToArray()
                };
            }
        }
    }
}
