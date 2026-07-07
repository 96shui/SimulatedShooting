using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRShooting.Common
{
    public readonly struct ZeroingSessionDto
    {
        public string SessionId { get; init; }
        public int CurrentRound { get; init; }
        public int MaxRounds { get; init; }
        public int ShotsRemainingInRound { get; init; }
        public float DistanceMeters { get; init; }
        public Vector2 FixedImpactOffsetCm { get; init; }
        public bool CanShoot { get; init; }
        public SightAdjustmentDto CurrentAdjustment { get; init; }

        public static ZeroingSessionDto Empty => new ZeroingSessionDto
        {
            SessionId = string.Empty,
            MaxRounds = 3,
            DistanceMeters = 100f
        };
    }

    public readonly struct ZeroingShotDto
    {
        public int RoundIndex { get; init; }
        public int ShotIndex { get; init; }
        public Vector2 ImpactPointCm { get; init; }
        public float WeaponStability { get; init; }
        public bool InsideTenRing { get; init; }
    }

    public readonly struct SightAdjustmentDto
    {
        public float FrontSightDegrees { get; init; }
        public int RearSightClicks { get; init; }
    }

    public readonly struct ZeroingRoundAnalysisDto
    {
        public string SessionId { get; init; }
        public int RoundIndex { get; init; }
        public IReadOnlyList<ZeroingShotDto> Shots { get; init; }
        public Vector2 AverageOffsetCm { get; init; }
        public VerticalAdjustmentDirection VerticalDirection { get; init; }
        public float FrontSightDegreesToAdjust { get; init; }
        public HorizontalAdjustmentDirection HorizontalDirection { get; init; }
        public int RearSightClicksToAdjust { get; init; }
        public bool PassedTenRing { get; init; }
        public bool AdjustmentApplied { get; init; }

        public static ZeroingRoundAnalysisDto Empty => new ZeroingRoundAnalysisDto
        {
            SessionId = string.Empty,
            Shots = Array.Empty<ZeroingShotDto>()
        };
    }

    public readonly struct ShotInputDto
    {
        public Vector3 WeaponPosition { get; init; }
        public Vector3 AimDirection { get; init; }
        public float WeaponStability { get; init; }
        public double FireTime { get; init; }
    }

    public readonly struct ZeroingResultDto
    {
        public string SessionId { get; init; }
        public ResultGrade Grade { get; init; }
        public int PassedRoundIndex { get; init; }
        public IReadOnlyList<ZeroingRoundAnalysisDto> Rounds { get; init; }
    }
}
