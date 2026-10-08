using System;
using System.Collections;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class CombatFeedbackIntegrationTests
    {
        // BDD23 task019: real map, perception ray, core attacks and production feedback adapter.
        [UnityTest] public IEnumerator TrenchEnemyAttacksShowShotTracerAudioAndPlayerBlood() => Validate(TrainingMode.Trench);
        [UnityTest] public IEnumerator UrbanEnemyAttacksShowShotTracerAudioAndPlayerBlood() => Validate(TrainingMode.Urban);

        IEnumerator Validate(TrainingMode mode)
        {
            yield return SceneManager.LoadSceneAsync("MainScene");
            var load = new UnityCombatSceneLoader(() => false).LoadAsync(mode, CancellationToken.None);
            while (!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True, load.Result.Message);
            var lease = load.Result.Data;
            CombatCoreService core = null;
            try
            {
                var runtime = (CombatSceneRuntime)lease.Navigation;
                runtime.ManualStepping = true;
                const string session = "task019-production-feedback";
                const string enemyId = "task019-enemy";
                core = new CombatCoreService(lease.Clock);
                var spawn = lease.Definition.SpawnPoints[0].WorldPosition;
                core.Start(session, mode, new[] { new CombatEntityVisualDto {
                    EntityId = enemyId, Role = CombatEntityRole.Enemy, Position = spawn, Forward = Vector3.forward } });
                var state = new CoreState(core, session);
                Assert.That(runtime.Activate(core, core, state, null, new Tick(core, session), session).Success);
                var listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>().Where(l => l.isActiveAndEnabled).ToArray();
                Assert.That(listeners.Length, Is.EqualTo(1), string.Join(";", listeners.Select(l => l.name + "@" + l.gameObject.scene.name)));
                var enemy = runtime.Actors[enemyId];
                Assert.That(enemy.SoldierAnimation.Rifle.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy), Is.True, "Enemy must hold a visible rifle");
                var position = enemy.PerceptionOrigin.position + enemy.transform.forward * 1.2f;
                position.y = enemy.GroundedFeetPosition.y + .05f;
                var body = runtime.PlayerRoot.GetComponent<CharacterController>();
                body.enabled = false;
                runtime.PlayerRoot.SetPositionAndRotation(position, Quaternion.LookRotation(-enemy.transform.forward));
                body.enabled = true;
                Physics.SyncTransforms();
                runtime.InputOverride = new ManualXRTrainingInput();
                runtime.FrameOverride = new CombatInputFrameDto {
                    HeadTracked = true, RearHandTracked = true, FrontHandTracked = true,
                    PlayerPosition = position, PlayerForward = -enemy.transform.forward,
                    MuzzlePosition = position + Vector3.up * 1.2f, AimDirection = -enemy.transform.forward };
                runtime.Step(.01f);
                Assert.That(core.GetSnapshot(session).Data.Player.Health, Is.EqualTo(2));
                runtime.Step(1.01f);
                Assert.That(core.GetSnapshot(session).Data.Player.Health, Is.EqualTo(1), "Production line of sight must reach the service");
                Assert.That(enemy.ShotVisualFeedbackCount, Is.EqualTo(1));
                Assert.That(enemy.ShotAudioFeedbackCount, Is.EqualTo(1));
                Assert.That(enemy.Audio.isPlaying, Is.True);
                var tracer = runtime.GetComponentsInChildren<BallisticTracerVisual>().Single(t => t.name.StartsWith("Tracer_Enemy_"));
                Assert.That(tracer.HasProjectileVisual, Is.True);
                Assert.That(tracer.GetComponent<LineRenderer>().endWidth, Is.GreaterThan(.005f));
                Assert.That(runtime.PlayerDamageFeedbackCount, Is.EqualTo(1));
                var mist = runtime.GetComponentsInChildren<SceneTestId>().Single(t => t.Id == "Combat.Player.FleshImpactFeedback");
                Assert.That(Vector3.Distance(mist.transform.position, runtime.PlayerHitPoint), Is.LessThan(.08f));
                Assert.That(mist.GetComponentInChildren<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null);
                yield return new WaitForSeconds(.02f);
                PresentationEvidence.Capture(runtime.PlayerCamera, mode + "-enemy-shot-player-blood");
                yield return new WaitForSeconds(.1f);
                Assert.That(enemy.SoldierAnimation.Animator.GetCurrentAnimatorStateInfo(0).IsName("Shot"), Is.True);
                runtime.Step(1.01f);
                Assert.That(core.GetSnapshot(session).Data.State, Is.EqualTo(SessionState.Failed));
                Assert.That(runtime.PlayerDamageFeedbackCount, Is.EqualTo(2), "The fatal shot also has blood feedback");
                Assert.That(enemy.ShotAudioFeedbackCount, Is.EqualTo(2));
                runtime.Step(10);
                Assert.That(enemy.ShotVisualFeedbackCount, Is.EqualTo(2), "No new shots after death");
            }
            finally { core?.Dispose(); lease.Dispose(); }
            yield return null;
        }

        sealed class Tick : ICombatTickPort
        {
            readonly CombatCoreService core; readonly string session;
            public Tick(CombatCoreService core, string session) { this.core = core; this.session = session; }
            public ServiceResult<Unit> Advance() => core.Advance(session);
        }
        sealed class CoreState : ICombatStateService
        {
            readonly CombatCoreService core; readonly string session;
            public CoreState(CombatCoreService core, string session)
            {
                this.core = core; this.session = session;
                core.Changed += value => { VisualChanged?.Invoke(value.Visual); PlayerChanged?.Invoke(GetPlayer(session).Data); };
            }
            public ServiceResult<CombatVisualSnapshotDto> GetVisualSnapshot(string id) => ServiceResult<CombatVisualSnapshotDto>.Ok(core.GetSnapshot(id).Data.Visual);
            public ServiceResult<CombatPlayerSnapshotDto> GetPlayer(string id) => ServiceResult<CombatPlayerSnapshotDto>.Ok(new CombatPlayerSnapshotDto {
                SessionId = session, Revision = core.GetSnapshot(id).Data.Revision, Player = core.GetSnapshot(id).Data.Player });
            public ServiceResult<CombatSquadSnapshotDto> GetSquadStatus(string id) => ServiceResult<CombatSquadSnapshotDto>.Ok(default);
            public event Action<CombatVisualSnapshotDto> VisualChanged;
            public event Action<CombatPlayerSnapshotDto> PlayerChanged;
            public event Action<CombatSquadSnapshotDto> SquadChanged { add { } remove { } }
        }
    }
}
