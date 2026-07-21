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

            return RecordShotInternal(record, input);
        }

        public ServiceResult<ZeroingRoundAnalysisDto> CompleteRound(string sessionId)
        {
            if (!TryGetRecord(sessionId, out var record, out var failure))
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(failure, "zeroing session not found", ZeroingRoundAnalysisDto.Empty);
            }

            if (record.CurrentShots.Count < ZeroingRules.ShotsPerRound)
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

            if (record.CurrentShots.Count < ZeroingRules.ShotsPerRound)
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(ErrorCode.InvalidState, "round requires 3 shots", ZeroingRoundAnalysisDto.Empty);
            }

            var analysis = CompleteRoundInternal(record, false);
            if (analysis.RoundIndex != roundIndex)
            {
                return ServiceResult<ZeroingRoundAnalysisDto>.Fail(ErrorCode.InvalidInput, "round index mismatch", analysis);
            }

            if (!analysis.AdjustmentApplied)
            {
                record.CurrentAdjustment = ZeroingRules.ApplyAdjustment(record.CurrentAdjustment, analysis);
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

            if (analysis.PassedTenRing || record.CurrentRound >= ZeroingRules.MaxRounds)
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

            var passedRound = ZeroingRules.ResolvePassedRoundIndex(record.Analyses);
            return ServiceResult<ZeroingResultDto>.Ok(new ZeroingResultDto
            {
                SessionId = record.SessionId,
                Grade = ZeroingRules.ComputeFinalGrade(passedRound),
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

            var aimPoint = ZeroingRules.ResolveAimPointFromWeaponShot(evt.Result);
            var input = new ShotInputDto
            {
                WeaponPosition = evt.Result.MuzzlePosition,
                AimDirection = new Vector3(aimPoint.x, aimPoint.y, ZeroingRules.DistanceMeters),
                WeaponStability = evt.Result.Stability01,
                FireTime = 0d
            };

            RecordShotInternal(record, input);
        }

        ServiceResult<ZeroingShotDto> RecordShotInternal(ZeroingSessionRecord record, ShotInputDto input)
        {
            if (record.CurrentShots.Count >= ZeroingRules.ShotsPerRound)
            {
                return ServiceResult<ZeroingShotDto>.Fail(ErrorCode.InvalidState, "round is already complete");
            }

            var aimPoint = ZeroingRules.ResolveAimPointCm(input.AimDirection);
            var impact = ZeroingRules.ComputeImpactPoint(aimPoint, record.FixedImpactOffsetCm);
            var shot = record.AddShot(impact, Mathf.Clamp01(input.WeaponStability));
            eventBus.Publish(new ZeroingShotRecordedEvent { SessionId = record.SessionId, Shot = shot });

            if (record.CurrentShots.Count == ZeroingRules.ShotsPerRound)
            {
                CompleteRoundInternal(record, true);
            }

            return ServiceResult<ZeroingShotDto>.Ok(shot);
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
                FixedImpactOffsetCm = ZeroingRules.GenerateFixedOffset(session.Seed)
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
                    InsideTenRing = ZeroingRules.IsInsideTenRing(impactPointCm)
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
                    MaxRounds = ZeroingRules.MaxRounds,
                    ShotsRemainingInRound = Math.Max(0, ZeroingRules.ShotsPerRound - CurrentShots.Count),
                    DistanceMeters = ZeroingRules.DistanceMeters,
                    FixedImpactOffsetCm = FixedImpactOffsetCm,
                    CanShoot = CurrentShots.Count < ZeroingRules.ShotsPerRound,
                    CurrentAdjustment = CurrentAdjustment
                };
            }

            public ZeroingRoundAnalysisDto BuildAnalysis()
            {
                return ZeroingRules.BuildRoundAnalysis(
                    SessionId,
                    CurrentRound,
                    CurrentShots,
                    AppliedRounds.Contains(CurrentRound));
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
        }
    }
}
