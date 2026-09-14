using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD23: scene-independent combat. Submit the complete fact batch before Advance.</summary>
    public sealed class CombatCoreService : ICombatCoreService
    {
        readonly ICombatClock clock;
        readonly CombatConfigDto config;
        readonly WeaponControlService weapons = new WeaponControlService();
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly Dictionary<string, Enemy> enemyById = new Dictionary<string, Enemy>(StringComparer.Ordinal);
        readonly Dictionary<string, CombatInputDto> received = new Dictionary<string, CombatInputDto>(StringComparer.Ordinal);
        readonly List<CombatInputDto> pending = new List<CombatInputDto>();
        readonly HashSet<string> issuedShots = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> spentShots = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> sessionIds = new HashSet<string>(StringComparer.Ordinal);
        readonly List<CombatFeedbackDto> feedback = new List<CombatFeedbackDto>();
        string session = "";
        string playerId = "";
        SessionState state;
        float health;
        PlayerPosture posture;
        bool corner, tracked, disposed, dirty, publishing;
        Vector3 position, forward;
        long revision, attackSequence, lastTick;
        double lastNow, reloadAt = double.PositiveInfinity;
        CombatCoreSnapshotDto snapshot;
        public event Action<CombatCoreSnapshotDto> Changed;
        public event Action<CombatFeedbackDto> Feedback;
        public bool HasPendingInputs => pending.Count > 0;

        public CombatCoreService(ICombatClock clock, CombatConfigDto? config = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.config = config ?? CombatConfigDto.Default;
            if (!this.config.Validate().Success) throw new ArgumentException("Invalid combat configuration", nameof(config));
        }

        public ServiceResult<CombatCoreSnapshotDto> Start(string sessionId, TrainingMode mode, IReadOnlyList<CombatEntityVisualDto> spawns)
        {
            if (disposed || publishing) return Fail<CombatCoreSnapshotDto>(ErrorCode.InvalidState);
            if (string.IsNullOrWhiteSpace(sessionId) || (mode != TrainingMode.Trench && mode != TrainingMode.Urban) || spawns == null ||
                !ValidClock()) return Fail<CombatCoreSnapshotDto>(ErrorCode.InvalidInput);
            if (sessionIds.Contains(sessionId)) return Fail<CombatCoreSnapshotDto>(ErrorCode.InvalidState);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spawn in spawns)
                if (string.IsNullOrWhiteSpace(spawn.EntityId) || spawn.EntityId == sessionId + ".player" || !ids.Add(spawn.EntityId) ||
                    spawn.Role != CombatEntityRole.Enemy || !Finite(spawn.Position) || !ValidDirection(spawn.Forward))
                    return Fail<CombatCoreSnapshotDto>(ErrorCode.InvalidInput);
            weapons.ReleaseSession(session);
            session = sessionId; playerId = session + ".player"; sessionIds.Add(session);
            weapons.StartSession(session, WeaponControlService.TrainingRifleId, mode);
            enemies.Clear(); enemyById.Clear(); received.Clear(); pending.Clear(); issuedShots.Clear(); spentShots.Clear(); feedback.Clear();
            foreach (var spawn in spawns)
            {
                var enemy = new Enemy { Id = spawn.EntityId, Position = spawn.Position, Forward = spawn.Forward.normalized };
                enemies.Add(enemy); enemyById.Add(enemy.Id, enemy);
            }
            enemies.Sort((a,b) => StringComparer.Ordinal.Compare(a.Id,b.Id));
            health = config.PlayerHealth; posture = PlayerPosture.Standing; corner = false; tracked = true;
            position = Vector3.zero; forward = Vector3.forward; state = SessionState.Running;
            revision = 0; attackSequence = 0; lastNow = clock.Now; lastTick = clock.Tick;
            reloadAt = double.PositiveInfinity; dirty = true; Publish();
            return ServiceResult<CombatCoreSnapshotDto>.Ok(snapshot);
        }

        public ServiceResult<CombatCoreSnapshotDto> GetSnapshot(string sessionId) => Matches(sessionId)
            ? ServiceResult<CombatCoreSnapshotDto>.Ok(snapshot) : Fail<CombatCoreSnapshotDto>(ErrorCode.NotFound);

        public ServiceResult<Unit> SetState(string sessionId, SessionState next)
        {
            var guard = Guard(sessionId); if (!guard.Success) return guard;
            if (next != SessionState.Running && next != SessionState.Paused && next != SessionState.Completed && next != SessionState.Failed && next != SessionState.Cancelled)
                return Failure(ErrorCode.InvalidInput);
            if (state == next) return Ok();
            if (Terminal(state)) return Failure(ErrorCode.InvalidState);
            state = next; Suspend(); dirty = true; Publish(); return Ok();
        }
        public ServiceResult<Unit> SetTracking(string sessionId, bool value)
        {
            var guard = Guard(sessionId); if (!guard.Success) return guard;
            if (Terminal(state)) return Ok();
            if (tracked == value) return Ok();
            tracked = value; Suspend();
            weapons.SetGripState(new WeaponGripStateInputDto { SessionId = session, HoldState = WeaponHoldState.Dropped });
            dirty = true; Publish(); return Ok();
        }
        public ServiceResult<Unit> SetGrip(WeaponGripStateInputDto grip)
        {
            var guard = Guard(grip.SessionId, true); if (!guard.Success) return guard;
            if (!Finite(grip.Stability01) || grip.Stability01 < 0 || grip.Stability01 > 1) return Failure(ErrorCode.InvalidInput);
            var before = weapons.GetState(session).Data;
            var result = weapons.SetGripState(grip);
            if (!result.Success) return Failure(result.ErrorCode);
            if (!before.Equals(result.Data)) { dirty = true; Publish(); } return Ok();
        }
        public ServiceResult<Unit> SetPosture(string sessionId, PlayerPosture value)
        {
            var guard = Guard(sessionId, true); if (!guard.Success) return guard;
            if (value < PlayerPosture.Standing || value > PlayerPosture.Prone) return Failure(ErrorCode.InvalidInput);
            if (posture != value) { posture = value; dirty = true; Publish(); } return Ok();
        }
        public ServiceResult<Unit> ToggleShoulder(string sessionId)
        {
            var guard = Guard(sessionId, true); if (!guard.Success) return guard;
            weapons.ToggleShoulder(session); dirty = true; Publish(); return Ok();
        }
        public ServiceResult<Unit> SetCornerAvailable(string sessionId, bool value)
        {
            var guard = Guard(sessionId, true); if (!guard.Success) return guard;
            if (corner != value) { corner = value; dirty = true; Publish(); } return Ok();
        }
        public ServiceResult<CombatShotDto> Fire(WeaponFireInputDto input)
        {
            var guard = Guard(input.SessionId, true); if (!guard.Success) return Fail<CombatShotDto>(guard.ErrorCode);
            if (!Finite(input.MuzzlePosition) || !ValidDirection(input.AimDirection) ||
                !Finite(input.RawAimDirection) || !Finite(input.Stability01) || !Finite(input.AimMotionOffsetCm) ||
                (input.AimMode != WeaponAimMode.HipFire && input.AimMode != WeaponAimMode.AimDownSights))
                return Fail<CombatShotDto>(ErrorCode.InvalidInput);
            var weapon = weapons.GetState(session).Data;
            if (!weapon.CanShoot) return Fail<CombatShotDto>(ErrorCode.InvalidState);
            // Shoulder and hold are authoritative service state. World hit facts arrive after the shot.
            var shot = weapons.Fire(new WeaponFireInputDto
            {
                SessionId = session, MuzzlePosition = input.MuzzlePosition, AimDirection = input.AimDirection,
                RawAimDirection = input.RawAimDirection, AimMode = input.AimMode, ShoulderSide = weapon.ShoulderSide,
                Stability01 = weapon.Stability01, AimMotionOffsetCm = input.AimMotionOffsetCm
            });
            if (!shot.Success) return Fail<CombatShotDto>(shot.ErrorCode);
            var id = session + ".shot-" + shot.Data.ShotSequence;
            issuedShots.Add(id); dirty = true;
            Emit(CombatFeedbackKind.ShotFired, id, playerId, "", id); Publish();
            return ServiceResult<CombatShotDto>.Ok(new CombatShotDto { ShotId = id, Shot = shot.Data });
        }
        public ServiceResult<Unit> Reload(string sessionId)
        {
            var guard = Guard(sessionId, true); if (!guard.Success) return guard;
            var ammo = weapons.GetAmmo(session).Data;
            if (ammo.IsReloading) return Failure(ErrorCode.Busy);
            if (!weapons.GetState(session).Data.TwoHandGripActive) return Failure(ErrorCode.InvalidState);
            var result = weapons.StartReload(session); if (!result.Success) return Failure(result.ErrorCode);
            if (result.Data.IsReloading) { reloadAt = clock.Now + config.ReloadSeconds; dirty = true; Publish(); }
            return Ok();
        }

        public ServiceResult<Unit> Submit(CombatInputDto input)
        {
            var guard = Guard(input.SessionId); if (!guard.Success) return guard;
            if (string.IsNullOrWhiteSpace(input.EventId)) return Failure(ErrorCode.InvalidInput);
            if (received.TryGetValue(input.EventId, out var existing))
                return existing.Equals(input) ? Ok() : Failure(ErrorCode.InvalidInput);
            if (!Active || input.Tick != clock.Tick || input.Tick < lastTick) return Failure(ErrorCode.InvalidState);
            if (!Finite(input.Position) || !Finite(input.Direction) || !Finite(input.Value)) return Failure(ErrorCode.InvalidInput);
            switch (input.Kind)
            {
                case CombatInputKind.PlayerPose:
                    if (input.EntityId != playerId) return Failure(ErrorCode.NotFound);
                    if (!ValidDirection(input.Direction)) return Failure(ErrorCode.InvalidInput);
                    break;
                case CombatInputKind.Perception:
                    if (!enemyById.ContainsKey(input.EntityId) || input.TargetId != playerId) return Failure(ErrorCode.NotFound);
                    if (!ValidDirection(input.Direction)) return Failure(ErrorCode.InvalidInput);
                    break;
                case CombatInputKind.Hit:
                    if (!input.Flag || input.Value <= 0) return Failure(ErrorCode.InvalidInput);
                    if (input.TargetId == playerId)
                    {
                        if (!enemyById.ContainsKey(input.EntityId)) return Failure(ErrorCode.NotFound);
                        if (input.Value != config.EnemyDamage) return Failure(ErrorCode.InvalidInput);
                    }
                    else
                    {
                        if (!enemyById.ContainsKey(input.TargetId) || input.EntityId != playerId) return Failure(ErrorCode.NotFound);
                        if (!issuedShots.Contains(input.ShotId)) return Failure(ErrorCode.InvalidInput);
                    }
                    break;
                default: return Failure(ErrorCode.InvalidState);
            }
            received.Add(input.EventId, input); pending.Add(input); return Ok();
        }

        public ServiceResult<Unit> Advance(string sessionId)
        {
            var guard = Guard(sessionId); if (!guard.Success) return guard;
            // Never let a late input silently migrate into a different fact batch.
            foreach (var input in pending) if (input.Tick != clock.Tick) return Failure(ErrorCode.InvalidState);
            lastNow = clock.Now; lastTick = clock.Tick;
            if (!Active) { pending.Clear(); return Ok(); }
            foreach (var input in pending)
            {
                if (input.Kind != CombatInputKind.PlayerPose) continue;
                if (position != input.Position || forward != input.Direction.normalized)
                { position = input.Position; forward = input.Direction.normalized; dirty = true; }
            }
            foreach (var input in pending)
            {
                if (input.Kind != CombatInputKind.Perception) continue;
                var enemy = enemyById[input.EntityId];
                if (enemy.Dead) continue;
                if (enemy.Position != input.Position || enemy.Forward != input.Direction.normalized) dirty = true;
                enemy.Position = input.Position; enemy.Forward = input.Direction.normalized; enemy.Visible = input.Flag;
            }
            foreach (var enemy in enemies)
            {
                var canAttack = SeesPlayer(enemy);
                if (enemy.CanAttack != canAttack || enemy.State == CombatEntityState.Spawned)
                {
                    enemy.CanAttack = canAttack;
                    if (!enemy.Dead) enemy.State = canAttack ? CombatEntityState.Attacking : CombatEntityState.Idle;
                    enemy.NextAttack = canAttack ? clock.Now + config.EnemyAttackInterval : double.PositiveInfinity;
                    dirty = true;
                }
            }
            // External impact and automatic attacks share the same per-enemy deadline.
            foreach (var input in pending)
                if (input.Kind == CombatInputKind.Hit && input.TargetId == playerId && Active)
                {
                    var enemy = enemyById[input.EntityId];
                    if (enemy.CanAttack && Due(enemy)) Attack(enemy, input.EventId);
                }
            // Stable chronological order, not dictionary iteration. Stop immediately on death.
            while (Active)
            {
                Enemy next = null;
                foreach (var enemy in enemies)
                    if (enemy.CanAttack && Due(enemy) && (next == null || enemy.NextAttack < next.NextAttack)) next = enemy;
                if (next == null) break;
                Attack(next, session + ".attack-" + ++attackSequence);
            }
            // Already-fired projectiles can hit in the same batch as player death; failure remains authoritative.
            foreach (var input in pending)
            {
                if (input.Kind != CombatInputKind.Hit || input.TargetId == playerId || !spentShots.Add(input.ShotId)) continue;
                var enemy = enemyById[input.TargetId]; if (enemy.Dead) continue;
                enemy.Dead = true; enemy.State = CombatEntityState.Dead; enemy.CanAttack = false; enemy.NextAttack = double.PositiveInfinity;
                dirty = true;
                Emit(CombatFeedbackKind.EnemyHit, input.EventId, playerId, enemy.Id, input.ShotId);
                Emit(CombatFeedbackKind.EnemyDied, input.EventId, playerId, enemy.Id, input.ShotId);
            }
            pending.Clear();
            if (Active && clock.Now + 1e-8 >= reloadAt)
            { weapons.CompleteReload(session); reloadAt = double.PositiveInfinity; dirty = true; }
            Publish(); return Ok();
        }
        void Attack(Enemy enemy, string eventId)
        {
            enemy.NextAttack += config.EnemyAttackInterval;
            Emit(CombatFeedbackKind.EnemyAttack, eventId, enemy.Id, playerId);
            var damage = Mathf.Min(health, config.EnemyDamage);
            health = Mathf.Max(0, health - config.EnemyDamage); dirty = true;
            Emit(CombatFeedbackKind.PlayerDamaged, eventId, enemy.Id, playerId, "", damage);
            if (health > 0) return;
            state = SessionState.Failed;
            // Keep the current hit batch until its shot facts have been processed.
            StopTimers();
            Emit(CombatFeedbackKind.PlayerDied, eventId, enemy.Id, playerId);
        }
        bool SeesPlayer(Enemy enemy)
        {
            var offset = position - enemy.Position;
            return !enemy.Dead && enemy.Visible && offset.magnitude <= config.EnemyRange &&
                (offset.sqrMagnitude < 1e-8 || Vector3.Angle(enemy.Forward, offset) <= config.EnemyHalfViewAngle);
        }
        bool Due(Enemy enemy) => enemy.NextAttack <= clock.Now + 1e-8;
        void Suspend() { pending.Clear(); StopTimers(); }
        void StopTimers()
        {
            reloadAt = double.PositiveInfinity; weapons.CancelReload(session);
            foreach (var enemy in enemies)
            {
                enemy.Visible = false; enemy.CanAttack = false; enemy.NextAttack = double.PositiveInfinity;
                if (!enemy.Dead) enemy.State = CombatEntityState.Idle;
            }
        }
        void Emit(CombatFeedbackKind kind, string id, string source, string target, string shot = "", float damage = 0)
        {
            feedback.Add(new CombatFeedbackDto { SessionId = session, EventId = id, Tick = clock.Tick,
                Kind = kind, EntityId = source, TargetId = target, ShotId = shot, Damage = damage });
        }
        void Publish()
        {
            if (!dirty) return;
            revision++;
            var weapon = weapons.GetState(session).Data;
            var entities = new CombatEntityVisualDto[enemies.Count + 1];
            entities[0] = new CombatEntityVisualDto { EntityId = playerId, Role = CombatEntityRole.Player,
                Position = position, Forward = forward, State = health > 0 ? CombatEntityState.Idle : CombatEntityState.Dead };
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                entities[i + 1] = new CombatEntityVisualDto { EntityId = enemy.Id, Role = CombatEntityRole.Enemy,
                    State = enemy.State, Position = enemy.Position, Forward = enemy.Forward, CorpseVisible = enemy.Dead };
            }
            snapshot = new CombatCoreSnapshotDto
            {
                SessionId = session, Revision = revision, State = state, TrackingValid = tracked,
                Player = new PlayerStatusDto { Health = health, IsAlive = health > 0, Posture = posture,
                    Shoulder = weapon.ShoulderSide, CornerShootingAvailable = corner },
                Ammo = weapons.GetAmmo(session).Data,
                Weapon = new WeaponControlStateDto { SessionId = session, WeaponId = weapon.WeaponId,
                    CurrentMagazine = weapon.CurrentMagazine, ReserveAmmo = weapon.ReserveAmmo, CanShoot = Active && weapon.CanShoot,
                    FireMode = weapon.FireMode, ShoulderSide = weapon.ShoulderSide, AimMode = weapon.AimMode,
                    HoldState = weapon.HoldState, RearHandTracked = weapon.RearHandTracked, FrontHandTracked = weapon.FrontHandTracked,
                    TwoHandGripActive = weapon.TwoHandGripActive, Stability01 = weapon.Stability01 },
                Visual = new CombatVisualSnapshotDto { SessionId = session, Revision = revision, Entities = entities }
            };
            dirty = false; publishing = true;
            try
            {
                Changed?.Invoke(snapshot);
                // A consumer may dispose during a callback (for example when its scene unloads).
                for (var i = 0; i < feedback.Count && !disposed; i++) Feedback?.Invoke(feedback[i]);
            }
            finally { feedback.Clear(); publishing = false; }
        }
        bool Active => state == SessionState.Running && health > 0 && tracked;
        bool Matches(string id) => !disposed && session.Length > 0 && session == id;
        bool ValidClock() => !double.IsNaN(clock.Now) && !double.IsInfinity(clock.Now) && clock.Now >= 0 && clock.Tick >= 0 &&
            clock.Now + config.EnemyAttackInterval > clock.Now && clock.Now + config.ReloadSeconds > clock.Now;
        ServiceResult<Unit> Guard(string id, bool requireActive = false)
        {
            if (!Matches(id)) return Failure(ErrorCode.NotFound);
            if (publishing) return Failure(ErrorCode.Busy);
            if (!ValidClock() || clock.Now < lastNow || clock.Tick < lastTick) return Failure(ErrorCode.InvalidInput);
            return requireActive && !Active ? Failure(ErrorCode.InvalidState) : Ok();
        }
        static bool Terminal(SessionState value) => value == SessionState.Failed || value == SessionState.Completed || value == SessionState.Cancelled;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        static bool ValidDirection(Vector3 value) => Finite(value) && Finite(value.sqrMagnitude) && value.sqrMagnitude > 1e-8;
        static ServiceResult<Unit> Ok() => ServiceResult<Unit>.Ok(Unit.Value);
        static ServiceResult<T> Fail<T>(ErrorCode code) => ServiceResult<T>.Fail(code, "Combat request rejected: " + code);
        static ServiceResult<Unit> Failure(ErrorCode code) => Fail<Unit>(code);
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; weapons.ReleaseSession(session); pending.Clear(); received.Clear(); feedback.Clear();
            issuedShots.Clear(); spentShots.Clear(); enemies.Clear(); enemyById.Clear(); sessionIds.Clear(); Changed = null; Feedback = null;
        }
        sealed class Enemy
        {
            public string Id;
            public Vector3 Position, Forward;
            public bool Visible, CanAttack, Dead;
            public double NextAttack = double.PositiveInfinity;
            public CombatEntityState State = CombatEntityState.Spawned;
        }
    }
}
