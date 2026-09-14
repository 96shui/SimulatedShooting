using System;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    /// <summary>Injectable prototype defaults, not a production combat service.</summary>
    public readonly struct CombatConfigDto
    {
        public float PlayerHealth { get; init; }
        public float EnemyDamage { get; init; }
        public float EnemyAttackInterval { get; init; }
        public float EnemyRange { get; init; }
        public float EnemyHalfViewAngle { get; init; }
        public float StandingSpeed { get; init; }
        public float CrouchingSpeed { get; init; }
        public float ProneSpeed { get; init; }
        public float StandingEyeHeight { get; init; }
        public float CrouchingEyeHeight { get; init; }
        public float ProneEyeHeight { get; init; }
        public float SnapTurnDegrees { get; init; }
        public float ReloadSeconds { get; init; }
        public float SquadSpacing { get; init; }
        public float NavigationRetrySeconds { get; init; }
        public float SpawnOffsetRadius { get; init; }
        public float TargetRefreshHz { get; init; }
        public float CpuBudgetMilliseconds { get; init; }
        public float GpuBudgetMilliseconds { get; init; }
        public float LoadBudgetSeconds { get; init; }
        public int SteadyStateGcBytesPerFrame { get; init; }
        public WeaponFireMode FireMode { get; init; }
        public AmmoDto InitialAmmo { get; init; }
        public static CombatConfigDto Default => new CombatConfigDto
        {
            PlayerHealth = 100f,
            EnemyDamage = 10f,
            EnemyAttackInterval = 1f,
            EnemyRange = 30f,
            EnemyHalfViewAngle = 60f,
            StandingSpeed = 1.5f,
            CrouchingSpeed = 0.9f,
            ProneSpeed = 0.4f,
            StandingEyeHeight = 1.65f,
            CrouchingEyeHeight = 1.1f,
            ProneEyeHeight = 0.65f,
            SnapTurnDegrees = 30f,
            ReloadSeconds = 2f,
            SquadSpacing = 1.5f,
            NavigationRetrySeconds = 1f,
            SpawnOffsetRadius = 2f,
            TargetRefreshHz = 72f,
            CpuBudgetMilliseconds = 11f,
            GpuBudgetMilliseconds = 11f,
            LoadBudgetSeconds = 5f,
            SteadyStateGcBytesPerFrame = 0,
            FireMode = WeaponFireMode.SingleShot,
            InitialAmmo = new AmmoDto { CurrentMagazine = 30, ReserveAmmo = 120, MagazineCapacity = 30 }
        };

        public ServiceResult<Unit> Validate()
        {
            if (!Positive(PlayerHealth)) return Invalid(nameof(PlayerHealth));
            if (!Positive(EnemyDamage)) return Invalid(nameof(EnemyDamage));
            if (!Positive(EnemyAttackInterval)) return Invalid(nameof(EnemyAttackInterval));
            if (!Positive(EnemyRange)) return Invalid(nameof(EnemyRange));
            if (!Positive(EnemyHalfViewAngle)) return Invalid(nameof(EnemyHalfViewAngle));
            if (!Positive(StandingSpeed)) return Invalid(nameof(StandingSpeed));
            if (!Positive(CrouchingSpeed)) return Invalid(nameof(CrouchingSpeed));
            if (!Positive(ProneSpeed)) return Invalid(nameof(ProneSpeed));
            if (!Positive(StandingEyeHeight)) return Invalid(nameof(StandingEyeHeight));
            if (!Positive(CrouchingEyeHeight)) return Invalid(nameof(CrouchingEyeHeight));
            if (!Positive(ProneEyeHeight)) return Invalid(nameof(ProneEyeHeight));
            if (!Positive(SnapTurnDegrees)) return Invalid(nameof(SnapTurnDegrees));
            if (!Positive(ReloadSeconds)) return Invalid(nameof(ReloadSeconds));
            if (!Positive(SquadSpacing)) return Invalid(nameof(SquadSpacing));
            if (!Positive(NavigationRetrySeconds)) return Invalid(nameof(NavigationRetrySeconds));
            if (!Positive(SpawnOffsetRadius)) return Invalid(nameof(SpawnOffsetRadius));
            if (!Positive(TargetRefreshHz)) return Invalid(nameof(TargetRefreshHz));
            if (!Positive(CpuBudgetMilliseconds)) return Invalid(nameof(CpuBudgetMilliseconds));
            if (!Positive(GpuBudgetMilliseconds)) return Invalid(nameof(GpuBudgetMilliseconds));
            if (!Positive(LoadBudgetSeconds)) return Invalid(nameof(LoadBudgetSeconds));
            if (EnemyHalfViewAngle > 180 || SnapTurnDegrees > 180) return Invalid("angles");
            if (StandingSpeed < CrouchingSpeed || CrouchingSpeed < ProneSpeed) return Invalid("posture speeds");
            if (StandingEyeHeight < CrouchingEyeHeight || CrouchingEyeHeight < ProneEyeHeight) return Invalid("posture heights");
            if (SteadyStateGcBytesPerFrame < 0) return Invalid(nameof(SteadyStateGcBytesPerFrame));
            if (CpuBudgetMilliseconds > 1000f / TargetRefreshHz || GpuBudgetMilliseconds > 1000f / TargetRefreshHz) return Invalid("frame budgets");
            if (FireMode != WeaponFireMode.SingleShot) return Invalid(nameof(FireMode));
            if (InitialAmmo.MagazineCapacity != 30 || InitialAmmo.CurrentMagazine != 30 || InitialAmmo.ReserveAmmo != 120 || InitialAmmo.IsReloading) return Invalid(nameof(InitialAmmo));
            return ServiceResult<Unit>.Ok(Unit.Value);
        }

        // Value-copy helper used by data editors and parameterized fixtures.
        public CombatConfigDto WithPlayerHealth(float health) => new CombatConfigDto
        {
            PlayerHealth = health,
            EnemyDamage = EnemyDamage,
            EnemyAttackInterval = EnemyAttackInterval,
            EnemyRange = EnemyRange,
            EnemyHalfViewAngle = EnemyHalfViewAngle,
            StandingSpeed = StandingSpeed,
            CrouchingSpeed = CrouchingSpeed,
            ProneSpeed = ProneSpeed,
            StandingEyeHeight = StandingEyeHeight,
            CrouchingEyeHeight = CrouchingEyeHeight,
            ProneEyeHeight = ProneEyeHeight,
            SnapTurnDegrees = SnapTurnDegrees,
            ReloadSeconds = ReloadSeconds,
            SquadSpacing = SquadSpacing,
            NavigationRetrySeconds = NavigationRetrySeconds,
            SpawnOffsetRadius = SpawnOffsetRadius,
            TargetRefreshHz = TargetRefreshHz,
            CpuBudgetMilliseconds = CpuBudgetMilliseconds,
            GpuBudgetMilliseconds = GpuBudgetMilliseconds,
            LoadBudgetSeconds = LoadBudgetSeconds,
            SteadyStateGcBytesPerFrame = SteadyStateGcBytesPerFrame, FireMode = FireMode, InitialAmmo = InitialAmmo
        };
        static bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
        static ServiceResult<Unit> Invalid(string field) => ServiceResult<Unit>.Fail(ErrorCode.InvalidInput, "Invalid combat configuration: " + field);
    }
}
