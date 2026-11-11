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
        public bool MissedTargetPlane { get; init; }
    }

    public readonly struct SightAdjustmentDto
    {
        // Signed positions: positive = clockwise / forward, negative = counterclockwise / backward.
        // Zero is the neutral mechanical position, not the new-session baseline.
        public float FrontSightDegrees { get; init; }
        public int RearSightClicks { get; init; }
    }

    public enum ZeroingAdjustmentAxis
    {
        Horizontal,
        Vertical
    }

    public readonly struct ZeroingRoundAnalysisDto
    {
        public string SessionId { get; init; }
        public int RoundIndex { get; init; }
        public IReadOnlyList<ZeroingShotDto> Shots { get; init; }
        // Mean of the three-shot calculation set; zero if no set exists.
        public Vector2 AverageOffsetCm { get; init; }
        public bool CalculationSetAvailable { get; init; }
        public bool SpreadRulePassed { get; init; }
        public bool SpreadRejected { get; init; }
        // Encoded so that default(ZeroingRoundAnalysisDto) also means no discarded shot.
        readonly int discardedShotIndexPlusOne;
        public int DiscardedShotIndex { get => discardedShotIndexPlusOne - 1; init => discardedShotIndexPlusOne = value + 1; }
        public Vector2 AppliedCorrectionCm { get; init; }
        public Vector2 ProposedCorrectionCm { get; init; }
        public Vector2 PreviewAverageOffsetCm { get; init; }
        public SightAdjustmentDto CurrentAdjustment { get; init; }
        public Vector2 FixedImpactOffsetCm { get; init; }
        public SightAdjustmentDto ProposedAdjustment { get; init; }
        public float PredictedOffsetMagnitudeCm { get; init; }
        public bool PredictedOffsetPasses { get; init; }
        public VerticalAdjustmentDirection VerticalDirection { get; init; }
        public float FrontSightDegreesToAdjust { get; init; }
        public HorizontalAdjustmentDirection HorizontalDirection { get; init; }
        public int RearSightClicksToAdjust { get; init; }
        public bool PassedTenRing { get; init; }
        public bool AdjustmentApplied { get; init; }
        public bool FinalResultAvailable { get; init; }

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
