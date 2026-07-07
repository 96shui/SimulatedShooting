using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Application.Events;
using VRShooting.Contracts;
using VRShooting.Common;

namespace VRShooting.Application
{
    public sealed class ZeroingService : IZeroingService
    {
        const int MaxRounds = 3;
        const int ShotsPerRound = 3;
        const float DistanceMeters = 100f;
        const float TenRingRadiusCm = 5f;
        const float CmPerRearSightClick = 2f;
        const float CmPerFrontSightDegree = 0.064f;

        readonly IGameEventBus eventBus;
        readonly ITrainingSessionService trainingSessions;
        readonly IWeaponControlService weaponControl;
        readonly Dictionary<string, ZeroingSessionRecord> sessions = new Dictionary<string, ZeroingSessionRecord>();

        public ZeroingService(
            IGameEventBus eventBus,
            ITrainingSessionService trainingSessions,
            IWeaponControlService weaponControl)
        {
            this.eventBus = eventBus;
            this.trainingSessions = trainingSessions;
            this.weaponControl = weaponControl;

            eventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
            eventBus.Subscribe<WeaponShotResultEvent>(OnWeaponShotResult);
        }

        public ServiceResult<ZeroingSessionDto> StartSession(RandomSeed seed, string weaponId)
        {
            var sessionResult = trainingSessions.HasActiveSession
                ? ServiceResult<TrainingSessionDto>.Ok(trainingSessions.Current)
                : trainingSessions.Create(TrainingMode.Zeroing100m, "zeroing-range-100m", weaponId, seed);

            if (!sessionResult.Success)
            {
                return ServiceResult<ZeroingSessionDto>.Fail(sessionResult.ErrorCode, sessionResult.Message, ZeroingSessionDto.Empty);
            }

            var startResult = trainingSessions.Start(sessionResult.Data.SessionId);
            if (!startResult.Success)
            {
                return ServiceResult<ZeroingSessionDto>.Fail(startResult.ErrorCode, startResult.Message, ZeroingSessionDto.Empty);
            }

            var record = EnsureRecord(startResult.Data);
            return ServiceResult<ZeroingSessionDto>.Ok(record.ToSessionDto());
        }

        public ServiceResult<ZeroingSessionDto> GetSession(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingSessionDto>.Fail(failure, "zeroing session not found", ZeroingSessionDto.Empty);
            }

            return ServiceResult<ZeroingSessionDto>.Ok(record.ToSessionDto());
        }

        public ServiceResult<ZeroingShotDto> RecordShot(string sessionId, ShotInputDto input)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingShotDto>.Fail(failure, "zeroing session not found");
            }

            if (record.CurrentShots.Count >= ShotsPerRound)
            {
                return ServiceResult<ZeroingShotDto>.Fail(ErrorCode.InvalidState, "round is already complete");
            }

            var impact = record.FixedImpactOffsetCm;
            var shot = record.AddShot(impact, Mathf.Clamp01(input.WeaponStability));
            eventBus.Publish(new ZeroingShotRecordedEvent { SessionId = record.SessionId, Shot = shot });

            if (record.CurrentShots.Count == ShotsPerRound)
            {
                CompleteRoundInternal(record, true);
            }

            return ServiceResult<ZeroingShotDto>.Ok(shot);
        }

        public ServiceResult<ZeroingRoundAnalysisDto> CompleteRound(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(failure, "zeroing session not found", ZeroingRoundAnalysisDto.Empty);
            }

            if (record.CurrentShots.Count < ShotsPerRound)
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(ErrorCode.InvalidState, "round requires 3 shots", ZeroingRoundAnalysisDto.Empty);
            }

            return ServiceResult<ZeroingRoundAnalysisDto>.Ok(CompleteRoundInternal(record, false));
        }

        public ServiceResult<ZeroingRoundAnalysisDto> ApplyAdjustment(string sessionId, int roundIndex)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(failure, "zeroing session not found", ZeroingRoundAnalysisDto.Empty);
            }

            var analysis = CompleteRoundInternal(record, false);
            if (analysis.RoundIndex != roundIndex)
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(ErrorCode.InvalidInput, "round index mismatch", analysis);
            }

            if (!analysis.AdjustmentApplied)
            {
                record.CurrentAdjustment = new SightAdjustmentDto
                {
                    FrontSightDegrees = record.CurrentAdjustment.FrontSightDegrees + analysis.FrontSightDegreesToAdjust,
                    RearSightClicks = record.CurrentAdjustment.RearSightClicks + analysis.RearSightClicksToAdjust
                };
                record.AppliedRounds.Add(roundIndex);
                analysis = record.BuildAnalysis();
                record.UpsertAnalysis(analysis);
                eventBus.Publish(new ZeroingAdjustmentAppliedEvent { SessionId = record.SessionId, Analysis = analysis });
            }

            return ServiceResult<ZeroingRoundAnalysisDto>.Ok(analysis);
        }

        public ServiceResult<ZeroingSessionDto> ContinueAfterAnalysis(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingSessionDto>.Fail(failure, "zeroing session not found", ZeroingSessionDto.Empty);
            }

            var analysis = CompleteRoundInternal(record, false);
            if (!analysis.AdjustmentApplied)
            {
                return ServiceResult<ZeroingSessionDto>.Fail(ErrorCode.InvalidState, "adjustment must be applied first", record.ToSessionDto());
            }

            if (analysis.PassedTenRing || record.CurrentRound >= MaxRounds)
            {
                return ServiceResult<ZeroingSessionDto>.Ok(record.ToSessionDto());
            }

            record.CurrentRound++;
            record.CurrentShots.Clear();
            weaponControl.Reload(sessionId);
            var dto = record.ToSessionDto();
            eventBus.Publish(new ZeroingRoundStartedEvent { SessionId = sessionId, Session = dto });
            return ServiceResult<ZeroingSessionDto>.Ok(dto);
        }

        public ServiceResult<ZeroingResultDto> GetFinalResult(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingResultDto>.Fail(failure, "zeroing session not found");
            }

            var passedRound = 0;
            for (var i = 0; i < record.Analyses.Count; i++)
            {
                if (record.Analyses[i].PassedTenRing)
                {
                    passedRound = record.Analyses[i].RoundIndex;
                    break;
                }
            }

            var grade = ResultGrade.Fail;
            if (passedRound == 1)
            {
                grade = ResultGrade.Excellent;
            }
            else if (passedRound == 2)
            {
                grade = ResultGrade.Good;
            }
            else if (passedRound == 3)
            {
                grade = ResultGrade.Pass;
            }

            return ServiceResult<ZeroingResultDto>.Ok(new ZeroingResultDto
            {
                SessionId = record.SessionId,
                Grade = grade,
                PassedRoundIndex = passedRound,
                Rounds = record.Analyses.ToArray()
            });
        }

        void OnSessionStarted(SessionStartedEvent evt)
        {
            if (evt.Session.Mode != TrainingMode.Zeroing100m)
            {
                return;
            }

            EnsureRecord(evt.Session);
        }

        void OnWeaponShotResult(WeaponShotResultEvent evt)
        {
            if (!evt.Result.IsValidShot || !TryGetRecord(evt.Result.SessionId, out var record, out _))
            {
                return;
            }

            if (record.CurrentShots.Count >= ShotsPerRound)
            {
                return;
            }

            var impact = new Vector2(evt.Result.HitPoint.x, evt.Result.HitPoint.y) + record.FixedImpactOffsetCm;
            var shot = record.AddShot(impact, 1f);
            eventBus.Publish(new ZeroingShotRecordedEvent { SessionId = record.SessionId, Shot = shot });

            if (record.CurrentShots.Count == ShotsPerRound)
            {
                CompleteRoundInternal(record, true);
            }
        }

        ZeroingRoundAnalysisDto CompleteRoundInternal(ZeroingSessionRecord record, bool publish)
        {
            var analysis = record.BuildAnalysis();
            var isNew = record.UpsertAnalysis(analysis);
            if (publish && isNew)
            {
                eventBus.Publish(new ZeroingRoundCompletedEvent { SessionId = record.SessionId, Analysis = analysis });
            }

            return analysis;
        }

        ZeroingSessionRecord EnsureRecord(TrainingSessionDto session)
        {
            if (sessions.TryGetValue(session.SessionId, out var existing))
            {
                return existing;
            }

            var record = new ZeroingSessionRecord
            {
                SessionId = session.SessionId,
                CurrentRound = 1,
                FixedImpactOffsetCm = Vector2.zero
            };
            sessions[session.SessionId] = record;
            return record;
        }

        bool TryGetRecord(string sessionId, out ZeroingSessionRecord record, out ErrorCode failure)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = trainingSessions.Current.SessionId;
            }

            if (!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out record))
            {
                failure = ErrorCode.None;
                return true;
            }

            record = null;
            failure = ErrorCode.NotFound;
            return false;
        }

        sealed class ZeroingSessionRecord
        {
            public string SessionId { get; set; }
            public int CurrentRound { get; set; }
            public Vector2 FixedImpactOffsetCm { get; set; }
            public SightAdjustmentDto CurrentAdjustment { get; set; }
            public List<ZeroingShotDto> CurrentShots { get; } = new List<ZeroingShotDto>();
            public List<ZeroingRoundAnalysisDto> Analyses { get; } = new List<ZeroingRoundAnalysisDto>();
            public HashSet<int> AppliedRounds { get; } = new HashSet<int>();

            public ZeroingShotDto AddShot(Vector2 impactPointCm, float stability)
            {
                var shot = new ZeroingShotDto
                {
                    RoundIndex = CurrentRound,
                    ShotIndex = CurrentShots.Count + 1,
                    ImpactPointCm = impactPointCm,
                    WeaponStability = stability,
                    InsideTenRing = impactPointCm.magnitude <= TenRingRadiusCm
                };
                CurrentShots.Add(shot);
                return shot;
            }

            public ZeroingSessionDto ToSessionDto()
            {
                return new ZeroingSessionDto
                {
                    SessionId = SessionId ?? string.Empty,
                    CurrentRound = CurrentRound,
                    MaxRounds = MaxRounds,
                    ShotsRemainingInRound = Math.Max(0, ShotsPerRound - CurrentShots.Count),
                    DistanceMeters = DistanceMeters,
                    FixedImpactOffsetCm = FixedImpactOffsetCm,
                    CanShoot = CurrentShots.Count < ShotsPerRound,
                    CurrentAdjustment = CurrentAdjustment
                };
            }

            public ZeroingRoundAnalysisDto BuildAnalysis()
            {
                var average = Vector2.zero;
                for (var i = 0; i < CurrentShots.Count; i++)
                {
                    average += CurrentShots[i].ImpactPointCm;
                }

                if (CurrentShots.Count > 0)
                {
                    average /= CurrentShots.Count;
                }

                var passed = CurrentShots.Count == ShotsPerRound;
                for (var i = 0; i < CurrentShots.Count; i++)
                {
                    passed &= CurrentShots[i].InsideTenRing;
                }

                return new ZeroingRoundAnalysisDto
                {
                    SessionId = SessionId ?? string.Empty,
                    RoundIndex = CurrentRound,
                    Shots = CurrentShots.ToArray(),
                    AverageOffsetCm = average,
                    VerticalDirection = ResolveVertical(average.y),
                    FrontSightDegreesToAdjust = Mathf.Ceil(Mathf.Abs(average.y) / CmPerFrontSightDegree),
                    HorizontalDirection = ResolveHorizontal(average.x),
                    RearSightClicksToAdjust = Mathf.CeilToInt(Mathf.Abs(average.x) / CmPerRearSightClick),
                    PassedTenRing = passed,
                    AdjustmentApplied = AppliedRounds.Contains(CurrentRound)
                };
            }

            public bool UpsertAnalysis(ZeroingRoundAnalysisDto analysis)
            {
                for (var i = 0; i < Analyses.Count; i++)
                {
                    if (Analyses[i].RoundIndex == analysis.RoundIndex)
                    {
                        Analyses[i] = analysis;
                        return false;
                    }
                }

                Analyses.Add(analysis);
                return true;
            }

            static VerticalAdjustmentDirection ResolveVertical(float y)
            {
                if (Mathf.Abs(y) < 0.001f)
                {
                    return VerticalAdjustmentDirection.None;
                }

                return y > 0f ? VerticalAdjustmentDirection.CounterClockwise : VerticalAdjustmentDirection.Clockwise;
            }

            static HorizontalAdjustmentDirection ResolveHorizontal(float x)
            {
                if (Mathf.Abs(x) < 0.001f)
                {
                    return HorizontalAdjustmentDirection.None;
                }

                return x < 0f ? HorizontalAdjustmentDirection.Forward : HorizontalAdjustmentDirection.Backward;
            }
        }
    }
}
