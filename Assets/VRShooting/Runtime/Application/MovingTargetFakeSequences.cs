using System.Collections.Generic;
using VRShooting.Common;

namespace VRShooting.Application
{
    /// <summary>
    /// 给 UI Presenter 与场景视觉驱动使用的 Fake DTO 序列，不含真实 Session 或 Transform。
    /// </summary>
    public static class MovingTargetFakeSequences
    {
        public static MovingTargetSettingsDto Settings(float speedMetersPerSecond = MovingTargetRules.DefaultSpeedMetersPerSecond)
        {
            return new MovingTargetSettingsDto { SpeedMetersPerSecond = speedMetersPerSecond };
        }

        public static IReadOnlyList<MovingTargetSessionDto> CreateStandardRun(string sessionId = "fake-moving-target")
        {
            return new[]
            {
                Snapshot(sessionId, TargetMovePhase.WaitingCountdown, 3f, 0f, 0f, 0f, false, "等待"),
                Snapshot(sessionId, TargetMovePhase.MovingRightToLeft, 0f, 0f, 0f, 0f, true, "右→左"),
                Snapshot(sessionId, TargetMovePhase.MovingRightToLeft, 0f, 0.5f, 0.5f, 0f, true, "右→左"),
                Snapshot(sessionId, TargetMovePhase.LeftEndpointHold, 0f, 1f, 1f, 2f, false, "左端停留"),
                Snapshot(sessionId, TargetMovePhase.MovingLeftToRight, 0f, 0.5f, 0.5f, 0f, true, "左→右"),
                Snapshot(sessionId, TargetMovePhase.Completed, 0f, 0f, 1f, 0f, false, "结束")
            };
        }

        public static MovingTargetResultDto CreateResult(
            string sessionId = "fake-moving-target",
            int hitCount = 4,
            float speedMetersPerSecond = 4f)
        {
            return new MovingTargetResultDto
            {
                SessionId = sessionId,
                SpeedMetersPerSecond = speedMetersPerSecond,
                TotalAmmoConsumed = 8,
                TotalShotsFired = 8,
                HitCount = hitCount,
                HitRate01 = MovingTargetRules.ComputeHitRate01(hitCount, 8),
                ElapsedSeconds = 25f,
                Grade = MovingTargetRules.ComputeGrade(hitCount),
                FireSequences = new[]
                {
                    new FireSequenceRecordDto
                    {
                        SequenceId = "seq-1",
                        StartTime = 4d,
                        ShotCount = 2,
                        HitCount = 1,
                        EnteredContinuousFire = false,
                        StopReason = WeaponFireStopReason.TriggerReleased,
                        Shots = new[]
                        {
                            Shot("seq-1", 1, 1, true, 0.2f),
                            Shot("seq-1", 2, 2, false, 0.25f)
                        }
                    },
                    new FireSequenceRecordDto
                    {
                        SequenceId = "seq-2",
                        StartTime = 8d,
                        ShotCount = 6,
                        HitCount = hitCount > 1 ? hitCount - 1 : 0,
                        EnteredContinuousFire = true,
                        StopReason = WeaponFireStopReason.TriggerReleased,
                        Shots = new[]
                        {
                            Shot("seq-2", 1, 3, true, 0.4f),
                            Shot("seq-2", 2, 4, true, 0.45f),
                            Shot("seq-2", 3, 5, true, 0.5f),
                            Shot("seq-2", 4, 6, false, 0.55f),
                            Shot("seq-2", 5, 7, hitCount >= 4, 0.6f),
                            Shot("seq-2", 6, 8, hitCount >= 5, 0.65f)
                        }
                    }
                }
            };
        }

        static MovingTargetSessionDto Snapshot(
            string sessionId,
            TargetMovePhase phase,
            float countdown,
            float route,
            float leg,
            float hold,
            bool canShoot,
            string direction)
        {
            return new MovingTargetSessionDto
            {
                SessionId = sessionId,
                SpeedMetersPerSecond = MovingTargetRules.DefaultSpeedMetersPerSecond,
                Phase = phase,
                FirePhase = WeaponFireSequencePhase.Idle,
                CountdownSecondsRemaining = countdown,
                CanShoot = canShoot,
                DirectionLabel = direction,
                RouteProgress01 = route,
                LegProgress01 = leg,
                EndpointHoldSecondsRemaining = hold
            };
        }

        static MovingTargetShotRecordDto Shot(string sequenceId, int index, int globalIndex, bool hit, float progress)
        {
            return new MovingTargetShotRecordDto
            {
                SequenceId = sequenceId,
                ShotIndexInSequence = index,
                GlobalShotIndex = globalIndex,
                FireTime = 4d + globalIndex,
                Hit = hit,
                TargetPhase = TargetMovePhase.MovingRightToLeft,
                RouteProgress01 = progress
            };
        }
    }
}
