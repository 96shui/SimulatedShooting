using System;
using System.Collections.Generic;

namespace VRShooting.Common
{
    /// <summary>
    /// 移动靶配置、会话、逐发记录与结算 DTO。参见 docs/接口文档/05-移动目标服务.md。
    /// </summary>
    public readonly struct MovingTargetSettingsDto
    {
        public float SpeedMetersPerSecond { get; init; }

        public static MovingTargetSettingsDto Default => new MovingTargetSettingsDto
        {
            SpeedMetersPerSecond = 4f
        };
    }

    public readonly struct MovingTargetSessionDto
    {
        public string SessionId { get; init; }
        public float SpeedMetersPerSecond { get; init; }
        public TargetMovePhase Phase { get; init; }
        public WeaponFireSequencePhase FirePhase { get; init; }
        public float CountdownSecondsRemaining { get; init; }
        public int ShotsFired { get; init; }
        public int HitCount { get; init; }
        public bool CanShoot { get; init; }
        public string DirectionLabel { get; init; }
        public float RouteProgress01 { get; init; }
        public float LegProgress01 { get; init; }
        public float EndpointHoldSecondsRemaining { get; init; }

        public static MovingTargetSessionDto Empty => new MovingTargetSessionDto
        {
            SessionId = string.Empty,
            DirectionLabel = string.Empty
        };
    }

    public readonly struct MovingTargetShotRecordDto
    {
        public string SequenceId { get; init; }
        public int ShotIndexInSequence { get; init; }
        public int GlobalShotIndex { get; init; }
        public double FireTime { get; init; }
        public bool Hit { get; init; }
        public TargetMovePhase TargetPhase { get; init; }
        public float RouteProgress01 { get; init; }

        public static MovingTargetShotRecordDto Empty => new MovingTargetShotRecordDto
        {
            SequenceId = string.Empty
        };
    }

    public readonly struct FireSequenceRecordDto
    {
        public string SequenceId { get; init; }
        public double StartTime { get; init; }
        public int ShotCount { get; init; }
        public int HitCount { get; init; }
        public bool EnteredContinuousFire { get; init; }
        public WeaponFireStopReason StopReason { get; init; }
        public IReadOnlyList<MovingTargetShotRecordDto> Shots { get; init; }

        public static FireSequenceRecordDto Empty => new FireSequenceRecordDto
        {
            SequenceId = string.Empty,
            Shots = Array.Empty<MovingTargetShotRecordDto>()
        };
    }

    public readonly struct MovingTargetResultDto
    {
        public string SessionId { get; init; }
        public float SpeedMetersPerSecond { get; init; }
        public int TotalAmmoConsumed { get; init; }
        public int TotalShotsFired { get; init; }
        public int HitCount { get; init; }
        public float HitRate01 { get; init; }
        public float ElapsedSeconds { get; init; }
        public ResultGrade Grade { get; init; }
        public IReadOnlyList<FireSequenceRecordDto> FireSequences { get; init; }

        public static MovingTargetResultDto Empty => new MovingTargetResultDto
        {
            SessionId = string.Empty,
            FireSequences = Array.Empty<FireSequenceRecordDto>()
        };
    }
}
