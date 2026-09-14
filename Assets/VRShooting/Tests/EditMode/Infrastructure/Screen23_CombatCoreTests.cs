using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode
{
    // BDD 23 all scenarios; BDD15 ammo/posture/death, BDD19 status, BDD21 death.
    public sealed class Screen23_CombatCoreTests
    {
        FakeCombatClock clock;
        CombatCoreService core;
        int eventNumber;
        const string Session = "combat-test";
        const string Enemy = "enemy-1";
        string Player => Session + ".player";
        CombatCoreSnapshotDto State => core.GetSnapshot(Session).Data;

        [SetUp] public void Setup()
        {
            clock = new FakeCombatClock(); eventNumber = 0;
            core = new CombatCoreService(clock);
            Assert.That(core.Start(Session, TrainingMode.Trench, new[] { Spawn() }).Success);
        }
        [TearDown] public void Cleanup() => core.Dispose();
        static CombatEntityVisualDto Spawn(string id = Enemy) => new CombatEntityVisualDto
        { EntityId = id, Role = CombatEntityRole.Enemy, Position = new Vector3(0, 0, 10), Forward = Vector3.back };
        void Grip() => Assert.That(core.SetGrip(new WeaponGripStateInputDto
        { SessionId = Session, HoldState = WeaponHoldState.TwoHandHeld, RearHandTracked = true, FrontHandTracked = true, Stability01 = 1 }).Success);
        CombatShotDto Fire()
        {
            var result = core.Fire(new WeaponFireInputDto { SessionId = Session, AimDirection = Vector3.forward });
            Assert.That(result.Success, Is.True, result.Message); return result.Data;
        }
        CombatInputDto Sight(bool visible = true, Vector3? position = null, Vector3? forward = null) => new CombatInputDto
        { SessionId = Session, EventId = "sight-" + ++eventNumber, Tick = clock.Tick, Kind = CombatInputKind.Perception,
            EntityId = Enemy, TargetId = Player, Position = position ?? new Vector3(0, 0, 10), Direction = forward ?? Vector3.back, Flag = visible };
        CombatInputDto Hit(string shot, string id = "hit") => new CombatInputDto
        { SessionId = Session, EventId = id, Tick = clock.Tick, Kind = CombatInputKind.Hit,
            EntityId = Player, TargetId = Enemy, ShotId = shot, Flag = true, Value = 1 };
        void See() { Assert.That(core.Submit(Sight()).Success); Assert.That(core.Advance(Session).Success); }
        void Advance(double seconds) { clock.Advance(seconds); Assert.That(core.Advance(Session).Success); }

        [Test] public void Ammo_DualGrip_SingleShot_AndTimedReloadConserveTotal()
        {
            Assert.That(State.Ammo.CurrentMagazine, Is.EqualTo(30)); Assert.That(State.Ammo.ReserveAmmo, Is.EqualTo(120));
            Assert.That(core.Fire(new WeaponFireInputDto { SessionId = Session, AimDirection = Vector3.forward }).Success, Is.False);
            Assert.That(State.Ammo.CurrentMagazine, Is.EqualTo(30)); Grip(); Fire();
            Assert.That(State.Weapon.FireMode, Is.EqualTo(WeaponFireMode.SingleShot));
            Assert.That(core.Reload(Session).Success); Assert.That(State.Weapon.CanShoot, Is.False);
            Advance(1.99); Assert.That(State.Ammo.CurrentMagazine, Is.EqualTo(29));
            Advance(.01); Assert.That(State.Ammo.CurrentMagazine, Is.EqualTo(30)); Assert.That(State.Ammo.ReserveAmmo, Is.EqualTo(119));
        }
        [Test] public void EmptyAmmoAndInvalidGrip_DoNotConsume()
        {
            Grip(); for (int i = 0; i < 30; i++) Fire();
            Assert.That(State.Weapon.CanShoot, Is.False);
            Assert.That(core.Fire(new WeaponFireInputDto { SessionId = Session, AimDirection = Vector3.forward }).Success, Is.False);
            Assert.That(State.Ammo.ReserveAmmo, Is.EqualTo(120));
            Assert.That(core.SetGrip(new WeaponGripStateInputDto { SessionId = Session, HoldState = WeaponHoldState.TwoHandHeld }).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
        }
        [Test] public void VisibleRangeAngle_AndOcclusionGateAttacks()
        {
            Advance(10); Assert.That(State.Player.Health, Is.EqualTo(100));
            core.Submit(Sight(position: new Vector3(0, 0, 31))); core.Advance(Session); Advance(2);
            core.Submit(Sight(forward: Vector3.forward)); core.Advance(Session); Advance(2);
            Assert.That(State.Player.Health, Is.EqualTo(100)); See(); Advance(1); Assert.That(State.Player.Health, Is.EqualTo(90));
            core.Submit(Sight(false)); core.Advance(Session); Advance(20); Assert.That(State.Player.Health, Is.EqualTo(90));
        }
        [Test] public void LargeTickMatchesSmallTicks_AndDoesNotAttackBeforeDiscovery()
        {
            Advance(100); See(); Advance(3); var large = State.Player.Health;
            core.Dispose(); Setup(); See(); for (int i = 0; i < 30; i++) Advance(.1);
            Assert.That(State.Player.Health, Is.EqualTo(large)); Assert.That(large, Is.EqualTo(70));
        }
        [Test] public void HitAndShotIds_AreIdempotent_CorpsePersists()
        {
            Grip(); var shot = Fire(); int hits = 0, deaths = 0;
            core.Feedback += f => { if (f.Kind == CombatFeedbackKind.EnemyHit) hits++; if (f.Kind == CombatFeedbackKind.EnemyDied) deaths++; };
            var hit = Hit(shot.ShotId); Assert.That(core.Submit(hit).Success); Assert.That(core.Submit(hit).Success);
            core.Advance(Session); Assert.That(core.Submit(hit).Success);
            Assert.That(core.Submit(Hit(shot.ShotId, "different-id")).Success); core.Advance(Session);
            Advance(100); Assert.That(hits, Is.EqualTo(1)); Assert.That(deaths, Is.EqualTo(1));
            var enemy = State.Visual.Entities.Single(e => e.EntityId == Enemy);
            Assert.That(enemy.State, Is.EqualTo(CombatEntityState.Dead)); Assert.That(enemy.CorpseVisible);
            Assert.That(State.Player.Health, Is.EqualTo(100));
        }
        [Test] public void UnknownShotAndInvalidInput_DoNotPublishOrChange()
        {
            int changes = 0; core.Changed += _ => changes++;
            Assert.That(core.Submit(Hit("forged")).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(core.Submit(new CombatInputDto { SessionId = "old", EventId = "event" }).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            Assert.That(core.SetPosture(Session, (PlayerPosture)999).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(core.Fire(new WeaponFireInputDto { SessionId = Session, AimDirection = new Vector3(float.NaN,0,0) }).Success, Is.False);
            Assert.That(changes, Is.Zero);
        }
        [TestCase(SessionState.Paused)] [TestCase(SessionState.Completed)] [TestCase(SessionState.Cancelled)]
        public void LifecycleCancelsReloadAndAttacks(SessionState state)
        {
            Grip(); Fire(); core.Reload(Session); See(); core.SetState(Session, state); Advance(100);
            Assert.That(State.Player.Health, Is.EqualTo(100)); Assert.That(State.Ammo.CurrentMagazine, Is.EqualTo(29));
            Assert.That(State.Ammo.ReserveAmmo, Is.EqualTo(120)); Assert.That(State.Ammo.IsReloading, Is.False);
            Assert.That(State.Weapon.CanShoot, Is.False);
            if (state != SessionState.Paused) Assert.That(core.SetState(Session, SessionState.Running).ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
        }
        [Test] public void TrackingLossSuspendsAttacksAndRequiresNewPerception()
        {
            Grip(); See(); core.SetTracking(Session, false); Advance(100);
            Assert.That(State.Weapon.CanShoot, Is.False); Assert.That(State.Player.Health, Is.EqualTo(100));
            core.SetTracking(Session, true); Advance(10); Assert.That(State.Player.Health, Is.EqualTo(100));
            See(); Advance(1); Assert.That(State.Player.Health, Is.EqualTo(90));
        }
        [Test] public void DeathIsClampedAndOnce_SameBatchEnemyKillDoesNotRestorePlayer()
        {
            core.Dispose(); core = new CombatCoreService(clock, CombatConfigDto.Default.WithPlayerHealth(5));
            core.Start(Session, TrainingMode.Urban, new[] { Spawn() }); Grip(); var shot = Fire(); See();
            int deaths = 0; core.Feedback += f => { if (f.Kind == CombatFeedbackKind.PlayerDied) deaths++; };
            clock.Advance(1); core.Submit(Hit(shot.ShotId)); core.Advance(Session); Advance(30);
            Assert.That(State.Player.Health, Is.Zero); Assert.That(State.Player.IsAlive, Is.False);
            Assert.That(State.State, Is.EqualTo(SessionState.Failed)); Assert.That(deaths, Is.EqualTo(1));
            Assert.That(State.Visual.Entities.Single(e => e.EntityId == Enemy).CorpseVisible);
        }
        [Test] public void DamageEventDuplicate_ConsumesOnlyOneAttackDeadline()
        {
            See(); clock.Advance(1);
            var damage = new CombatInputDto { SessionId = Session, EventId = "damage-1", Tick = clock.Tick,
                Kind = CombatInputKind.Hit, EntityId = Enemy, TargetId = Player, Value = 10, Flag = true };
            Assert.That(core.Submit(damage).Success); Assert.That(core.Submit(damage).Success); core.Advance(Session);
            Assert.That(State.Player.Health, Is.EqualTo(90)); Assert.That(core.Submit(damage).Success);
            Advance(1); Assert.That(State.Player.Health, Is.EqualTo(80));
        }
        [Test] public void NewSessionInvalidatesOldFactsAndDisposalStopsNotifications()
        {
            See(); Assert.That(core.Start("new-session", TrainingMode.Urban, new[] { Spawn() }).Success);
            Assert.That(core.Submit(Sight()).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            Assert.That(core.GetSnapshot(Session).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            core.Dispose(); Assert.That(core.Advance("new-session").Success, Is.False);
        }
        [Test] public void PostureShoulderAndCornerAreServiceState()
        {
            core.SetPosture(Session, PlayerPosture.Prone); core.ToggleShoulder(Session); core.SetCornerAvailable(Session, true);
            Assert.That(State.Player.Posture, Is.EqualTo(PlayerPosture.Prone)); Assert.That(State.Player.Shoulder, Is.EqualTo(ShoulderSide.Left));
            Assert.That(State.Player.CornerShootingAvailable);
            var revision = State.Revision; core.SetPosture(Session, PlayerPosture.Prone); Assert.That(State.Revision, Is.EqualTo(revision));
        }
        [Test] public void TerminalSnapshotIsImmutable_WhenTrackingChanges()
        {
            Grip(); core.SetState(Session, SessionState.Completed); var before = State;
            Assert.That(core.SetTracking(Session, false).Success);
            Assert.That(State.Revision, Is.EqualTo(before.Revision));
            Assert.That(State.Weapon.Equals(before.Weapon));
        }
        [Test] public void DisposeDuringFeedback_DoesNotLeaveCallbacksOrThrow()
        {
            Grip(); int calls = 0;
            core.Feedback += _ => { calls++; core.Dispose(); };
            Assert.DoesNotThrow(() => core.Fire(new WeaponFireInputDto { SessionId = Session, AimDirection = Vector3.forward }));
            Assert.That(calls, Is.EqualTo(1)); Assert.That(core.GetSnapshot(Session).Success, Is.False);
        }
        [Test] public void UnrepresentableDamageAndClock_AreRejected()
        {
            Assert.That(CombatConfigDto.Default.WithPlayerHealth(float.MaxValue).Validate().ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            var before = State.Revision; clock.Advance(double.MaxValue);
            Assert.That(core.Advance(Session).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput)); Assert.That(State.Revision, Is.EqualTo(before));
        }
        [Test] public void DuplicateIdWithDifferentPayload_AndLateBatchAreRejected()
        {
            var sight = Sight(); Assert.That(core.Submit(sight).Success);
            Assert.That(core.Submit(new CombatInputDto { SessionId = Session, EventId = sight.EventId,
                Tick = clock.Tick, Kind = CombatInputKind.Perception, EntityId = Enemy, TargetId = Player,
                Position = sight.Position, Direction = sight.Direction, Flag = false }).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            clock.Advance(1); Assert.That(core.Advance(Session).ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(State.Player.Health, Is.EqualTo(100));
            core.SetState(Session, SessionState.Paused); Assert.That(core.Advance(Session).Success);
        }
        [Test] public void OldShotCannotPenetrateMultipleEnemies_AndInvalidStartPreservesSession()
        {
            core.Start("two-enemies", TrainingMode.Trench, new[] { Spawn("a"), Spawn("b") });
            const string id = "two-enemies";
            core.SetGrip(new WeaponGripStateInputDto { SessionId = id, HoldState = WeaponHoldState.TwoHandHeld, RearHandTracked = true, FrontHandTracked = true });
            var shot = core.Fire(new WeaponFireInputDto { SessionId = id, AimDirection = Vector3.forward }).Data;
            foreach (var target in new[] { "a", "b" }) core.Submit(new CombatInputDto { SessionId = id, EventId = target,
                Tick = clock.Tick, Kind = CombatInputKind.Hit, EntityId = id + ".player", TargetId = target, ShotId = shot.ShotId, Value = 1, Flag = true });
            core.Advance(id);
            Assert.That(core.GetSnapshot(id).Data.Visual.Entities.Count(e => e.CorpseVisible), Is.EqualTo(1));
            Assert.That(core.Start("invalid", TrainingMode.Trench, new[] { Spawn("same"), Spawn("same") }).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(core.GetSnapshot(id).Success);
        }
    }
}
