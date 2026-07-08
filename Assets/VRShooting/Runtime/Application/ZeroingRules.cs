using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    /// <summary>
    /// 100m 射校纯规则算法（task006）。可在 EditMode 中独立测试，不依赖场景或输入设备。
    /// </summary>
    public static class ZeroingRules
    {
        public const int MaxRounds = 3;
        public const int ShotsPerRound = 3;
        public const float DistanceMeters = 100f;
        public const float TenRingRadiusCm = 5f;
        public const float TargetHalfSizeCm = 25f;
        public const float CmPerRearSightClick = 2f;
        public const float CmPerFrontSightDegree = 0.064f;

        public static Vector2 GenerateFixedOffset(RandomSeed seed)
        {
            var rng = new System.Random(seed.Value);
            var angle = rng.NextDouble() * Math.PI * 2d;
            var minRadius = TenRingRadiusCm + 0.01f;
            var maxRadius = TargetHalfSizeCm;
            var radius = minRadius + rng.NextDouble() * (maxRadius - minRadius);
            return new Vector2(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));
        }

        public static Vector2 ResolveAimPointCm(Vector3 aimDirection, float distanceMeters = DistanceMeters)
        {
            if (Mathf.Abs(aimDirection.z) < 0.0001f)
            {
                return Vector2.zero;
            }

            var scale = distanceMeters / aimDirection.z;
            return new Vector2(aimDirection.x * scale, aimDirection.y * scale);
        }

        public static bool UsesTargetOffsetHitPointConvention(Vector3 hitPoint)
        {
            return Mathf.Abs(hitPoint.z - DistanceMeters) < 0.5f;
        }

        public static Vector2 ResolveAimPointFromWeaponShot(WeaponShotResultDto result)
        {
            if (UsesTargetOffsetHitPointConvention(result.HitPoint))
            {
                return new Vector2(result.HitPoint.x, result.HitPoint.y);
            }

            if (result.Hit && result.AimDirection.sqrMagnitude > 0.0001f)
            {
                return ResolveAimPointFromRay(result.MuzzlePosition, result.AimDirection.normalized, DistanceMeters);
            }

            return ResolveAimPointCm(result.AimDirection);
        }

        public static Vector2 ResolveAimPointFromRay(Vector3 origin, Vector3 direction, float targetDistanceMeters)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            var normalized = direction.normalized;
            if (Mathf.Abs(normalized.z) < 0.0001f)
            {
                return Vector2.zero;
            }

            var travel = targetDistanceMeters / normalized.z;
            var hit = origin + normalized * travel;
            return new Vector2(hit.x * 100f, hit.y * 100f);
        }

        public static Vector2 WorldPointToTargetOffsetCm(
            Vector3 worldPoint,
            Vector3 targetCenter,
            Vector3 targetRight,
            Vector3 targetUp)
        {
            var offset = worldPoint - targetCenter;
            var localX = Vector3.Dot(offset, targetRight.normalized);
            var localY = Vector3.Dot(offset, targetUp.normalized);
            return new Vector2(localX, localY) * 100f;
        }

        public static Vector2 ComputeImpactPoint(Vector2 aimPointCm, Vector2 fixedImpactOffsetCm)
        {
            return aimPointCm + fixedImpactOffsetCm;
        }

        public static bool IsInsideTenRing(Vector2 impactPointCm)
        {
            return impactPointCm.magnitude <= TenRingRadiusCm;
        }

        public static ZeroingRoundAnalysisDto BuildRoundAnalysis(
            string sessionId,
            int roundIndex,
            IReadOnlyList<ZeroingShotDto> shots,
            bool adjustmentApplied)
        {
            var average = Vector2.zero;
            for (var i = 0; i < shots.Count; i++)
            {
                average += shots[i].ImpactPointCm;
            }

            if (shots.Count > 0)
            {
                average /= shots.Count;
            }

            var passed = shots.Count == ShotsPerRound;
            for (var i = 0; i < shots.Count; i++)
            {
                passed &= shots[i].InsideTenRing;
            }

            return new ZeroingRoundAnalysisDto
            {
                SessionId = sessionId ?? string.Empty,
                RoundIndex = roundIndex,
                Shots = shots is ZeroingShotDto[] array ? array : CopyShots(shots),
                AverageOffsetCm = average,
                VerticalDirection = ResolveVerticalDirection(average.y),
                FrontSightDegreesToAdjust = ComputeFrontSightDegrees(average.y),
                HorizontalDirection = ResolveHorizontalDirection(average.x),
                RearSightClicksToAdjust = ComputeRearSightClicks(average.x),
                PassedTenRing = passed,
                AdjustmentApplied = adjustmentApplied
            };
        }

        public static SightAdjustmentDto ApplyAdjustment(
            SightAdjustmentDto current,
            ZeroingRoundAnalysisDto analysis)
        {
            var frontSight = current.FrontSightDegrees;
            if (analysis.VerticalDirection != VerticalAdjustmentDirection.None)
            {
                frontSight += analysis.FrontSightDegreesToAdjust;
            }

            var rearSight = current.RearSightClicks;
            if (analysis.HorizontalDirection != HorizontalAdjustmentDirection.None)
            {
                rearSight += analysis.RearSightClicksToAdjust;
            }

            return new SightAdjustmentDto
            {
                FrontSightDegrees = frontSight,
                RearSightClicks = rearSight
            };
        }

        public static ResultGrade ComputeFinalGrade(int passedRoundIndex)
        {
            return passedRoundIndex switch
            {
                1 => ResultGrade.Excellent,
                2 => ResultGrade.Good,
                3 => ResultGrade.Pass,
                _ => ResultGrade.Fail
            };
        }

        public static int ResolvePassedRoundIndex(IReadOnlyList<ZeroingRoundAnalysisDto> analyses)
        {
            for (var i = 0; i < analyses.Count; i++)
            {
                if (analyses[i].PassedTenRing)
                {
                    return analyses[i].RoundIndex;
                }
            }

            return 0;
        }

        public static float ComputeFrontSightDegrees(float verticalOffsetCm)
        {
            return Mathf.Ceil(Mathf.Abs(verticalOffsetCm) / CmPerFrontSightDegree);
        }

        public static int ComputeRearSightClicks(float horizontalOffsetCm)
        {
            return Mathf.CeilToInt(Mathf.Abs(horizontalOffsetCm) / CmPerRearSightClick);
        }

        public static VerticalAdjustmentDirection ResolveVerticalDirection(float verticalOffsetCm)
        {
            if (Mathf.Abs(verticalOffsetCm) < 0.001f)
            {
                return VerticalAdjustmentDirection.None;
            }

            return verticalOffsetCm > 0f
                ? VerticalAdjustmentDirection.CounterClockwise
                : VerticalAdjustmentDirection.Clockwise;
        }

        public static HorizontalAdjustmentDirection ResolveHorizontalDirection(float horizontalOffsetCm)
        {
            if (Mathf.Abs(horizontalOffsetCm) < 0.001f)
            {
                return HorizontalAdjustmentDirection.None;
            }

            return horizontalOffsetCm < 0f
                ? HorizontalAdjustmentDirection.Forward
                : HorizontalAdjustmentDirection.Backward;
        }

        static ZeroingShotDto[] CopyShots(IReadOnlyList<ZeroingShotDto> shots)
        {
            var copy = new ZeroingShotDto[shots.Count];
            for (var i = 0; i < shots.Count; i++)
            {
                copy[i] = shots[i];
            }

            return copy;
        }
    }
}
