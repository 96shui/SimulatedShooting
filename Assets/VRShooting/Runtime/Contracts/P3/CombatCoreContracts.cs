using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    public enum CombatFeedbackKind { ShotFired, EnemyHit, EnemyDied, EnemyAttack, PlayerDamaged, PlayerDied }
    public readonly struct CombatFeedbackDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        readonly string eventId;
        public string EventId { get => eventId ?? string.Empty; init => eventId = value ?? string.Empty; }
        public long Tick { get; init; }
        public CombatFeedbackKind Kind { get; init; }
        readonly string entityId;
        public string EntityId { get => entityId ?? string.Empty; init => entityId = value ?? string.Empty; }
        readonly string targetId;
        public string TargetId { get => targetId ?? string.Empty; init => targetId = value ?? string.Empty; }
        readonly string shotId;
        public string ShotId { get => shotId ?? string.Empty; init => shotId = value ?? string.Empty; }
        public float Damage { get; init; }
    }
    public readonly struct CombatShotDto
    {
        readonly string shotId;
        public string ShotId { get => shotId ?? string.Empty; init => shotId = value ?? string.Empty; }
        public WeaponShotResultDto Shot { get; init; }
    }
    public readonly struct CombatCoreSnapshotDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        public SessionState State { get; init; }
        public PlayerStatusDto Player { get; init; }
        public AmmoDto Ammo { get; init; }
        public WeaponControlStateDto Weapon { get; init; }
        public CombatVisualSnapshotDto Visual { get; init; }
    }
    public readonly struct CombatLocomotionIntentDto
    {
        public Vector3 LocalVelocity { get; init; }
        public float SnapTurnDegrees { get; init; }
        public PlayerPosture Posture { get; init; }
        public float? SimulatedEyeHeight { get; init; }
        public float BodyHeight { get; init; }
        public float DeltaSeconds { get; init; }
    }
    public readonly struct CombatRayHitDto
    {
        public bool Hit { get; init; }
        readonly string entityId;
        public string EntityId { get => entityId ?? string.Empty; init => entityId = value ?? string.Empty; }
        public Vector3 Point { get; init; }
    }
    public readonly struct CombatInputFrameDto
    {
        public bool HeadTracked { get; init; }
        public bool RearHandTracked { get; init; }
        public bool FrontHandTracked { get; init; }
        public bool RearGripInRange { get; init; }
        public bool FrontGripInRange { get; init; }
        public bool IsRealVr { get; init; }
        public Vector3 MuzzlePosition { get; init; }
        public Vector3 AimDirection { get; init; }
        public Vector3 PlayerPosition { get; init; }
        public Vector3 PlayerForward { get; init; }
        public PlayerPosture? RequestedPosture { get; init; }
    }
}
namespace VRShooting.Application
{
    public interface ICombatCoreService : ICombatWorldInputPort, IDisposable
    {
        ServiceResult<CombatCoreSnapshotDto> Start(string sessionId, TrainingMode mode, IReadOnlyList<CombatEntityVisualDto> enemies);
        ServiceResult<CombatCoreSnapshotDto> GetSnapshot(string sessionId);
        ServiceResult<Unit> SetState(string sessionId, SessionState state);
        ServiceResult<Unit> SetTracking(string sessionId, bool tracked);
        ServiceResult<Unit> SetGrip(WeaponGripStateInputDto grip);
        ServiceResult<Unit> SetPosture(string sessionId, PlayerPosture posture);
        ServiceResult<Unit> ToggleShoulder(string sessionId);
        ServiceResult<Unit> SetCornerAvailable(string sessionId, bool available);
        ServiceResult<CombatShotDto> Fire(WeaponFireInputDto input);
        ServiceResult<Unit> Reload(string sessionId);
        ServiceResult<Unit> Advance(string sessionId);
        event Action<CombatCoreSnapshotDto> Changed;
        event Action<CombatFeedbackDto> Feedback;
    }
    public interface ICombatLocomotionPort { ServiceResult<Unit> Apply(CombatLocomotionIntentDto intent); }
    public interface ICombatShotQuery { CombatRayHitDto Cast(Vector3 origin, Vector3 direction); }
}
