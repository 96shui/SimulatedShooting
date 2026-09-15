using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode
{
    // BDD14 load/cancel; BDD17/21 retry/return/save failure. World and loader are explicit substitutes.
    public sealed class Screen26_CombatApplicationTests
    {
        CombatApplicationFixture fixture;
        CombatApplicationCoordinator app;
        CombatApplicationFixture.Lease lease;
        [SetUp] public void Setup()
        {
            fixture = new CombatApplicationFixture();
            app = new CombatApplicationCoordinator(new UIRouter(new GameEventBus()), fixture, fixture);
        }
        [TearDown] public void Cleanup() => app.Dispose();
        void Prepare(TrainingMode mode)
        {
            Assert.That(app.OpenMode(mode).Success);
            var pending = app.SelectMapAsync(mode == TrainingMode.Trench ? "trench-a" : "urban-a", RandomSeed.Fixed(7));
            lease = new CombatApplicationFixture.Lease(mode); fixture.Complete(fixture.Loads.Count - 1, lease);
            Assert.That(Done(pending).Success);
        }
        static ServiceResult<Unit> Done(Task<ServiceResult<Unit>> task)
        { Assert.That(task.IsCompleted, "Controlled loader completion must be synchronous"); return task.GetAwaiter().GetResult(); }
        void Die()
        {
            var id = app.Mission.SessionId;
            var core = app.Mission.Core;
            var enemy = core.GetSnapshot(id).Data.Visual.Entities.First(e => e.Role == CombatEntityRole.Enemy);
            Assert.That(app.Mission.World.Submit(new CombatInputDto { SessionId = id, EventId = "death-sight", Tick = lease.Clock.Tick,
                Kind = CombatInputKind.Perception, EntityId = enemy.EntityId, TargetId = id + ".player", Position = Vector3.forward * 10,
                Direction = Vector3.back, Flag = true }).Success);
            app.Advance(); lease.TestClock.Advance(10); app.Advance();
            Assert.That(app.Snapshot.Summary.HasValue);
        }
        [Test] public void BriefingDoesNotCreateActors_DuplicateStartIsBusy()
        {
            Prepare(TrainingMode.Trench);
            Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.TrenchBriefing));
            Assert.That(app.Mission.SessionId, Is.Empty); Assert.That(lease.Activations, Is.Zero);
            Assert.That(app.Start().Success); Assert.That(lease.Activations, Is.EqualTo(1));
            Assert.That(app.Start().ErrorCode, Is.EqualTo(ErrorCode.Busy));
            Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.TrenchHud));
        }
        [Test] public void LoadFailureLeavesRecoverableMap_AndBusyRejectsAnotherSelection()
        {
            app.OpenMode(TrainingMode.Trench); var pending = app.SelectMapAsync("trench-a", RandomSeed.Fixed(1));
            Assert.That(app.Snapshot.Busy); Assert.That(Done(app.SelectMapAsync("trench-a", RandomSeed.Fixed(1))).ErrorCode, Is.EqualTo(ErrorCode.Busy));
            fixture.Loads[0].SetException(new IOException("load failed"));
            Assert.That(Done(pending).ErrorCode, Is.EqualTo(ErrorCode.ResourceUnavailable));
            Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.TrenchMapSelection)); Assert.That(app.Snapshot.Busy, Is.False); Assert.That(app.Mission, Is.Null);
        }
        [Test] public void QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban()
        {
            app.OpenMode(TrainingMode.Trench); var old = app.SelectMapAsync("trench-a", RandomSeed.Fixed(1));
            Prepare(TrainingMode.Urban); var id = app.Mission.SessionId;
            var stale = new CombatApplicationFixture.Lease(TrainingMode.Trench); fixture.Complete(0, stale);
            Assert.That(Done(old).ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(fixture.Tokens[0].IsCancellationRequested); Assert.That(stale.Disposals, Is.EqualTo(1));
            Assert.That(stale.Activations, Is.Zero); Assert.That(lease.Disposals, Is.Zero);
            Assert.That(app.Mission.SessionId, Is.EqualTo(id)); Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.UrbanStreetHud));
        }
        [TestCase(false)] [TestCase(true)] public void CancelOrDisposeDuringLoadCleansLateScene(bool dispose)
        {
            app.OpenMode(TrainingMode.Trench); var pending = app.SelectMapAsync("trench-a", RandomSeed.Fixed(1));
            if (dispose) app.Dispose(); else Assert.That(app.ReturnToMainMenu().Success);
            var stale = new CombatApplicationFixture.Lease(TrainingMode.Trench); fixture.Complete(0, stale); Done(pending);
            Assert.That(stale.Disposals, Is.EqualTo(1)); Assert.That(stale.Activations, Is.Zero); Assert.That(app.Mission, Is.Null);
            if (!dispose) Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.MainMenu));
        }
        [Test] public void WrongSceneOrActivationFailureNeverLeavesRunningSession()
        {
            app.OpenMode(TrainingMode.Trench); var pending = app.SelectMapAsync("trench-a", RandomSeed.Fixed(1));
            var wrong = new CombatApplicationFixture.Lease(TrainingMode.Urban); fixture.Complete(0, wrong);
            Assert.That(Done(pending).Success, Is.False); Assert.That(wrong.Disposals, Is.EqualTo(1));
            Prepare(TrainingMode.Trench); lease.FailActivation = true;
            Assert.That(app.Start().Success, Is.False); Assert.That(app.Mission, Is.Null);
            Assert.That(lease.Core.GetSnapshot(lease.SessionId).Success, Is.False); Assert.That(lease.Disposals, Is.EqualTo(1));
        }
        [TestCase(TrainingMode.Trench)] [TestCase(TrainingMode.Urban)]
        public void ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(TrainingMode mode)
        {
            Prepare(mode); if (mode == TrainingMode.Trench) app.Start();
            string previous = null;
            for (var i = 0; i < 3; i++)
            {
                var mission = app.Mission; var id = mission.SessionId; Assert.That(id, Is.Not.EqualTo(previous)); previous = id;
                Die(); var oldSummary = app.Snapshot.Summary.Value;
                Assert.That(app.Retry().Success); if (mode == TrainingMode.Trench) app.Start();
                Assert.That(mission.Core.GetSnapshot(id).Success, Is.False);
                Assert.That(app.Mission.World.Submit(new CombatInputDto { SessionId = id, EventId = "late", Tick = lease.Clock.Tick }).Success, Is.False);
                Assert.That(app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.Ammo.CurrentMagazine, Is.EqualTo(30));
                Assert.That(oldSummary.SessionId, Is.EqualTo(id)); Assert.That(fixture.Saved, Is.Empty);
            }
            Die(); var latest = app.Snapshot.Summary.Value; Assert.That(app.ReturnToMainMenu().Success);
            Assert.That(fixture.Saved.Single().SessionId, Is.EqualTo(latest.SessionId)); Assert.That(lease.Disposals, Is.EqualTo(1));
        }
        [Test] public void PersistenceFailureKeepsStoppedResultAndAllowsRetry()
        {
            Prepare(TrainingMode.Trench); app.Start(); Die(); var id = app.Mission.SessionId;
            fixture.SaveFails = true; Assert.That(app.ReturnToMainMenu().ErrorCode, Is.EqualTo(ErrorCode.PersistenceFailed));
            Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.TrenchResults)); Assert.That(app.Mission.SessionId, Is.EqualTo(id));
            Assert.That(lease.Disposals, Is.Zero); Assert.That(app.Snapshot.Busy, Is.False);
            fixture.SaveFails = false; Assert.That(app.ReturnToMainMenu().Success); Assert.That(fixture.Saved.Count, Is.EqualTo(1));
        }
        [Test] public void PresenterUnsubscribes_AndCallbacksCannotReenterLifecycle()
        {
            var view = new View(); var presenter = new CombatApplicationPresenter(app, view);
            ErrorCode reentry = ErrorCode.None; app.Changed += _ => reentry = app.OpenMode(TrainingMode.Urban).ErrorCode;
            Prepare(TrainingMode.Trench); Assert.That(reentry, Is.EqualTo(ErrorCode.Busy));
            Assert.That(view.Last.Screen, Is.EqualTo(ScreenId.TrenchBriefing)); var count = view.Count; presenter.Dispose(); app.Start();
            Assert.That(view.Count, Is.EqualTo(count));
        }
        [Test] public void DefaultRootNeverRegistersFakeScenes()
        {
            var services = ApplicationServices.CreateDefault();
            try { services.Combat.OpenMode(TrainingMode.Trench); Assert.That(services.Combat.SelectMapAsync("trench-a", RandomSeed.Fixed(1)).Result.ErrorCode, Is.EqualTo(ErrorCode.ResourceUnavailable)); }
            finally { services.Combat.Dispose(); }
        }
        [Test] public void SceneActivationAndCleanupCannotReenterModeSwitch()
        {
            app.OpenMode(TrainingMode.Urban); var pending = app.SelectMapAsync("urban-a", RandomSeed.Fixed(1));
            lease = new CombatApplicationFixture.Lease(TrainingMode.Urban);
            ErrorCode activation = ErrorCode.None, cleanup = ErrorCode.None;
            lease.DuringActivation = () => activation = app.OpenMode(TrainingMode.Trench).ErrorCode;
            lease.DuringDeactivation = () => cleanup = app.OpenMode(TrainingMode.Trench).ErrorCode;
            fixture.Complete(0, lease); Assert.That(Done(pending).Success); Assert.That(activation, Is.EqualTo(ErrorCode.Busy));
            Assert.That(app.ReturnToMainMenu().Success); Assert.That(cleanup, Is.EqualTo(ErrorCode.Busy));
            Assert.That(app.Snapshot.Screen, Is.EqualTo(ScreenId.MainMenu));
        }
        [Test] public void SaveCallbackCannotStartOrDiscardAnotherSession()
        {
            Prepare(TrainingMode.Trench); app.Start(); Die(); var id = app.Mission.SessionId;
            ErrorCode callback = ErrorCode.None; fixture.DuringSave = () => callback = app.OpenMode(TrainingMode.Urban).ErrorCode;
            Assert.That(app.ReturnToMainMenu().Success); Assert.That(callback, Is.EqualTo(ErrorCode.Busy));
            Assert.That(fixture.Saved.Single().SessionId, Is.EqualTo(id)); Assert.That(app.Mission, Is.Null);
        }
        sealed class View : ICombatApplicationView
        { public int Count; public CombatApplicationSnapshotDto Last; public void Render(CombatApplicationSnapshotDto state) { Count++; Last = state; } }
    }
}
