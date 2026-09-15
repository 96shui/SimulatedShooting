using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.PlayMode
{
    // BDD14/17/18/19/20/21. Production application+services; UGUI test View and scene facts are substitutes.
    // These tests are NOT the task013 production UI/scene E2E gate.
    public sealed class Screen26_CombatCompositionPlayModeTests
    {
        [UnityTest] public IEnumerator TrenchButtonsDriveVictoryRetryFailureAndPersistenceRecovery() => Run(TrainingMode.Trench);
        [UnityTest] public IEnumerator UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry() => Run(TrainingMode.Urban);
        IEnumerator Run(TrainingMode mode)
        {
            var fixture = new CombatApplicationFixture();
            using var app = new CombatApplicationCoordinator(new UIRouter(new GameEventBus()), fixture, fixture);
            using var view = new ButtonView(app);
            app.OpenMode(mode); view.Select.onClick.Invoke();
            Assert.That(view.Status.text, Does.Contain("Busy")); Assert.That(view.Start.interactable, Is.False);
            var lease = new CombatApplicationFixture.Lease(mode); fixture.Complete(0, lease);
            yield return new WaitUntil(() => view.Load.IsCompleted); Assert.That(view.Load.Result.Success);
            if (mode == TrainingMode.Trench) { Assert.That(lease.Activations, Is.Zero); view.Start.onClick.Invoke(); }
            var id = app.Mission.SessionId; var mission = app.Mission; var sequence = 0;
            void Range(string key)
            {
                Assert.That(mission.World.Submit(new CombatInputDto { SessionId = id, Tick = lease.Clock.Tick, EventId = "area-" + ++sequence,
                    Kind = CombatInputKind.AreaPresence, EntityId = key, Flag = true }).Success);
                Assert.That(app.Advance().Success);
            }
            if (mode == TrainingMode.Urban)
            {
                Range(P3ContractIds.UrbanEntrance); Assert.That(mission.Urban.EnterBuilding(id, P3ContractIds.UrbanEntrance).Success);
                Assert.That(app.Snapshot.SessionId, Is.EqualTo(id)); Assert.That(view.Status.text, Does.Contain("UrbanBuildingHud"));
            }
            foreach (var enemy in mission.Core.GetSnapshot(id).Data.Visual.Entities.Where(e => e.Role == CombatEntityRole.Enemy).ToArray())
            {
                Assert.That(mission.Core.SetGrip(new WeaponGripStateInputDto { SessionId = id, HoldState = WeaponHoldState.TwoHandHeld,
                    RearHandTracked = true, FrontHandTracked = true }).Success);
                var shot = mission.Core.Fire(new WeaponFireInputDto { SessionId = id, AimDirection = Vector3.forward }); Assert.That(shot.Success);
                Assert.That(mission.World.Submit(new CombatInputDto { SessionId = id, Tick = lease.Clock.Tick, EventId = "hit-" + ++sequence,
                    Kind = CombatInputKind.Hit, EntityId = id + ".player", TargetId = enemy.EntityId, ShotId = shot.Data.ShotId, Flag = true, Value = 1 }).Success);
                app.Advance(); yield return null;
            }
            if (mode == TrainingMode.Trench) foreach (var node in lease.Definition.SearchNodes) Range(node.NodeId);
            else foreach (var room in lease.Definition.Floors.SelectMany(f => f.Rooms))
            {
                Range(room.RoomId + ".door"); Assert.That(mission.Urban.OpenRoomDoor(id, room.RoomId).Success);
                Range(room.RoomId + ".check"); Assert.That(mission.Urban.MarkRoomSearched(id, room.RoomId).Success); app.Advance(); yield return null;
            }
            Assert.That(app.Snapshot.Summary.Value.Victory); Assert.That(view.Status.text, Does.Contain("Victory"));
            var oldId = id; view.Retry.onClick.Invoke(); if (mode == TrainingMode.Trench) view.Start.onClick.Invoke();
            Assert.That(mission.Core.GetSnapshot(oldId).Success, Is.False); mission = app.Mission; id = mission.SessionId;
            Assert.That(id, Is.Not.EqualTo(oldId)); Assert.That(lease.Activations, Is.EqualTo(2));
            var attacker = mission.Core.GetSnapshot(id).Data.Visual.Entities.First(e => e.Role == CombatEntityRole.Enemy);
            mission.World.Submit(new CombatInputDto { SessionId = id, EventId = "failure", Tick = lease.Clock.Tick, Kind = CombatInputKind.Perception,
                EntityId = attacker.EntityId, TargetId = id + ".player", Position = Vector3.forward * 10, Direction = Vector3.back, Flag = true });
            app.Advance(); lease.TestClock.Advance(10); app.Advance(); yield return null;
            Assert.That(app.Snapshot.Summary.Value.Victory, Is.False); Assert.That(view.Status.text, Does.Contain("Failure"));
            fixture.SaveFails = true; view.Return.onClick.Invoke(); Assert.That(view.Status.text, Does.Contain("PersistenceFailed"));
            Assert.That(lease.Disposals, Is.Zero); fixture.SaveFails = false; view.Return.onClick.Invoke(); yield return null;
            Assert.That(view.Status.text, Does.Contain("MainMenu")); Assert.That(fixture.Saved.Single().SessionId, Is.EqualTo(id));
            Assert.That(lease.Disposals, Is.EqualTo(1)); Assert.That(app.Mission, Is.Null);
        }
        sealed class ButtonView : ICombatApplicationView, IDisposable
        {
            readonly CombatApplicationCoordinator app;
            readonly CombatApplicationPresenter presenter;
            readonly GameObject root = new GameObject("Screen_CombatApplication_TestOnly", typeof(RectTransform));
            public readonly Button Select, Start, Retry, Return;
            public readonly Text Status;
            public Task<ServiceResult<Unit>> Load;
            public ButtonView(CombatApplicationCoordinator app)
            {
                this.app = app;
                Select = Button("Button_Test_Select", () => Load = app.SelectMapAsync(app.Snapshot.Mode == TrainingMode.Trench ? "trench-a" : "urban-a", RandomSeed.Fixed(4)));
                Start = Button("Button_Test_Start", () => app.Start()); Retry = Button("Button_Test_Retry", () => app.Retry()); Return = Button("Button_Test_Return", () => app.ReturnToMainMenu());
                var text = new GameObject("Text_Test_State", typeof(RectTransform), typeof(Text)); text.transform.SetParent(root.transform);
                Status = text.GetComponent<Text>(); presenter = new CombatApplicationPresenter(app, this);
            }
            Button Button(string name, Action click)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Button)); obj.transform.SetParent(root.transform);
                var button = obj.GetComponent<Button>(); button.onClick.AddListener(() => click()); return button;
            }
            public void Render(CombatApplicationSnapshotDto snapshot)
            {
                Status.text = snapshot.Screen + " " + (snapshot.Busy ? "Busy" : "Ready") + " " + snapshot.Error + " " +
                    (snapshot.Summary.HasValue ? (snapshot.Summary.Value.Victory ? "Victory" : "Failure") : "");
                Start.interactable = !snapshot.Busy && snapshot.Screen == ScreenId.TrenchBriefing;
                Retry.interactable = !snapshot.Busy && snapshot.Summary.HasValue;
                Select.interactable = !snapshot.Busy && (snapshot.Screen == ScreenId.TrenchMapSelection || snapshot.Screen == ScreenId.UrbanMapSelection);
            }
            public void Dispose() { presenter.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
