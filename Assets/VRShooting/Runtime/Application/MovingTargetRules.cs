using System;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 移动靶速度、路线推进和评级纯函数。追溯 docs/BDD/screens/08-移动靶设置.feature.md 与 11-移动靶结算.feature.md。
    /// </summary>
    public static class MovingTargetRules
    {
        public const float RouteLengthMeters = 40f;
        public const float CountdownSeconds = 3f;
        public const float LeftEndpointHoldSeconds = 2f;
        public const int TotalAmmo = 10;
        public const float DefaultSpeedMetersPerSecond = 4f;
        public const float ProgressEpsilon = 0.0001f;

        public static readonly float[] AvailableSpeeds =
        {
            3f,
            4f,
            5f
        };

        public static bool IsAllowedSpeed(float speedMetersPerSecond)
        {
            for (var i = 0; i < AvailableSpeeds.Length; i++)
            {
                if (AreEqual(AvailableSpeeds[i], speedMetersPerSecond))
                {
                    return true;
                }
            }

            return false;
        }

        public static ResultGrade ComputeGrade(int hitCount)
        {
            if (hitCount >= 5)
            {
                return ResultGrade.Excellent;
            }

            if (hitCount == 4)
            {
                return ResultGrade.Good;
            }

            if (hitCount == 3)
            {
                return ResultGrade.Pass;
            }

            return ResultGrade.Fail;
        }

        public static float ComputeHitRate01(int hitCount, int shotsFired)
        {
            if (shotsFired <= 0)
            {
                return 0f;
            }

            return Clamp01(hitCount / (float)shotsFired);
        }

        public static bool CanShoot(TargetMovePhase phase)
        {
            return phase == TargetMovePhase.MovingRightToLeft
                   || phase == TargetMovePhase.MovingLeftToRight;
        }

        public static string DirectionLabel(TargetMovePhase phase)
        {
            switch (phase)
            {
                case TargetMovePhase.MovingRightToLeft:
                    return "右→左";
                case TargetMovePhase.MovingLeftToRight:
                    return "左→右";
                case TargetMovePhase.LeftEndpointHold:
                    return "左端停留";
                case TargetMovePhase.Completed:
                    return "结束";
                default:
                    return "等待";
            }
        }

        public static MovingTargetSimState CreateInitial()
        {
            return new MovingTargetSimState
            {
                Phase = TargetMovePhase.WaitingCountdown,
                CountdownSecondsRemaining = CountdownSeconds,
                RouteProgress01 = 0f,
                LegProgress01 = 0f,
                EndpointHoldSecondsRemaining = 0f,
                ElapsedSeconds = 0f
            };
        }

        public static MovingTargetSimState Advance(MovingTargetSimState state, float speedMetersPerSecond, float deltaTime)
        {
            if (state.Phase == TargetMovePhase.Completed || deltaTime <= 0f || float.IsNaN(deltaTime))
            {
                return state;
            }

            if (speedMetersPerSecond <= 0f || float.IsNaN(speedMetersPerSecond))
            {
                return state;
            }

            var remaining = deltaTime;
            var countdownElapsed = false;
            var completed = false;
            const int maxSteps = 8;

            for (var step = 0; step < maxSteps && remaining > ProgressEpsilon; step++)
            {
                if (state.Phase == TargetMovePhase.Completed)
                {
                    break;
                }

                switch (state.Phase)
                {
                    case TargetMovePhase.WaitingCountdown:
                        remaining = ConsumeCountdown(ref state, remaining, ref countdownElapsed);
                        break;
                    case TargetMovePhase.MovingRightToLeft:
                        remaining = AdvanceRightToLeft(ref state, speedMetersPerSecond, remaining);
                        break;
                    case TargetMovePhase.LeftEndpointHold:
                        remaining = ConsumeHold(ref state, remaining);
                        break;
                    case TargetMovePhase.MovingLeftToRight:
                        remaining = AdvanceLeftToRight(ref state, speedMetersPerSecond, remaining, ref completed);
                        break;
                    default:
                        remaining = 0f;
                        break;
                }
            }

            state.CountdownElapsedThisTick = countdownElapsed;
            state.CompletedThisTick = completed;
            return state;
        }

        static float ConsumeCountdown(ref MovingTargetSimState state, float remaining, ref bool countdownElapsed)
        {
            var consume = Math.Min(remaining, state.CountdownSecondsRemaining);
            state.CountdownSecondsRemaining -= consume;
            state.ElapsedSeconds += consume;
            remaining -= consume;
            if (state.CountdownSecondsRemaining <= ProgressEpsilon)
            {
                state.CountdownSecondsRemaining = 0f;
                state.Phase = TargetMovePhase.MovingRightToLeft;
                state.RouteProgress01 = 0f;
                state.LegProgress01 = 0f;
                countdownElapsed = true;
            }

            return remaining;
        }

        static float AdvanceRightToLeft(ref MovingTargetSimState state, float speed, float remaining)
        {
            var remainingDistance = (1f - state.RouteProgress01) * RouteLengthMeters;
            var timeToEnd = remainingDistance / speed;
            if (remaining + ProgressEpsilon >= timeToEnd)
            {
                state.ElapsedSeconds += timeToEnd;
                remaining -= timeToEnd;
                state.RouteProgress01 = 1f;
                state.LegProgress01 = 1f;
                state.Phase = TargetMovePhase.LeftEndpointHold;
                state.EndpointHoldSecondsRemaining = LeftEndpointHoldSeconds;
                return remaining;
            }

            state.RouteProgress01 = Clamp01(state.RouteProgress01 + speed * remaining / RouteLengthMeters);
            state.LegProgress01 = state.RouteProgress01;
            state.ElapsedSeconds += remaining;
            return 0f;
        }

        static float ConsumeHold(ref MovingTargetSimState state, float remaining)
        {
            var consume = Math.Min(remaining, state.EndpointHoldSecondsRemaining);
            state.EndpointHoldSecondsRemaining -= consume;
            state.ElapsedSeconds += consume;
            remaining -= consume;
            state.RouteProgress01 = 1f;
            state.LegProgress01 = 1f;
            if (state.EndpointHoldSecondsRemaining <= ProgressEpsilon)
            {
                state.EndpointHoldSecondsRemaining = 0f;
                state.Phase = TargetMovePhase.MovingLeftToRight;
            }

            return remaining;
        }

        static float AdvanceLeftToRight(ref MovingTargetSimState state, float speed, float remaining, ref bool completed)
        {
            var remainingDistance = state.RouteProgress01 * RouteLengthMeters;
            var timeToEnd = remainingDistance / speed;
            if (remaining + ProgressEpsilon >= timeToEnd)
            {
                state.ElapsedSeconds += timeToEnd;
                remaining -= timeToEnd;
                state.RouteProgress01 = 0f;
                state.LegProgress01 = 1f;
                state.Phase = TargetMovePhase.Completed;
                state.EndpointHoldSecondsRemaining = 0f;
                completed = true;
                return remaining;
            }

            state.RouteProgress01 = Clamp01(state.RouteProgress01 - speed * remaining / RouteLengthMeters);
            state.LegProgress01 = Clamp01(1f - state.RouteProgress01);
            state.ElapsedSeconds += remaining;
            return 0f;
        }

        static bool AreEqual(float left, float right)
        {
            return Math.Abs(left - right) <= 0.0001f;
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }

    public struct MovingTargetSimState
    {
        public TargetMovePhase Phase;
        public float CountdownSecondsRemaining;
        public float RouteProgress01;
        public float LegProgress01;
        public float EndpointHoldSecondsRemaining;
        public float ElapsedSeconds;
        public bool CountdownElapsedThisTick;
        public bool CompletedThisTick;
    }
}
