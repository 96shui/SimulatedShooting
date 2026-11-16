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
    // BDD36 / P3 task004,010,012: real production scene, mission, actors and Physics.
    public sealed class Round7EnemyVisionTests
    {
        ICombatSceneLease lease;
        CombatMission mission;
        CombatSceneRuntime runtime;
        CombatSceneBindings bindings;
        string targetId;
        CombatActorView Target => runtime.Actors[targetId];
        CombatEntityVisualDto TargetState => mission.Core.GetSnapshot(mission.SessionId).Data.Visual.Entities.Single(e => e.EntityId == targetId);

        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("MainScene");
            var load = new UnityCombatSceneLoader(() => false).LoadAsync(TrainingMode.Urban, CancellationToken.None);
            while (!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True, load.Result.Message);
            lease = load.Result.Data;
            runtime = (CombatSceneRuntime)lease.Navigation;
            runtime.ManualStepping = true;
            bindings = runtime.GetComponent<CombatSceneBindings>();
            mission = new CombatMission(lease);
            Assert.That(mission.Start(RandomSeed.Fixed(1)).Success, Is.True);
            Assert.That(lease.Activate(mission.Core, mission.World, mission.State, mission.Hud, mission.Squad, mission.Grenades, mission, mission.SessionId).Success, Is.True);
            runtime.InputOverride = new ManualXRTrainingInput();
            runtime.EnemyVisionDiagnostics = true;
            var assignments = mission.Urban.GetEnemyAssignments(mission.SessionId).Data;
            targetId = assignments.Single(a => a.RoomId == "room-12").EntityId;
            Debug.Log("[R7-Setup] " + string.Join(";", assignments.Select(a => a.RoomId + ":actor=" + runtime.Actors.ContainsKey(a.EntityId))));
        }

        void Place(Vector3 position)
        {
            var body = runtime.PlayerRoot.GetComponent<CharacterController>();
            body.enabled = false;
            runtime.PlayerRoot.position = position;
            body.enabled = true;
            runtime.FrameOverride = new CombatInputFrameDto { HeadTracked = true, RearHandTracked = true, FrontHandTracked = true,
                PlayerPosition = position, PlayerForward = Vector3.forward, MuzzlePosition = position + Vector3.up * 1.2f, AimDirection = Vector3.forward };
            Physics.SyncTransforms();
        }
        void Step(float seconds = .1f)
        {
            runtime.Step(seconds);
            Assert.That(runtime.LastFrameResult.Success, Is.True, runtime.LastFrameResult.Message);
        }
        void Open(string room)
        {
            var door = bindings.Doors.Single(d => d.RoomId == room);
            Place(door.Hinge.position + door.Hinge.forward);
            Step(.01f);
            var result = mission.Urban.OpenRoomDoor(mission.SessionId, room);
            Assert.That(result.Success, Is.True, result.Message);
            Physics.SyncTransforms();
        }
        void Record(string phase)
        {
            var player = runtime.FrameOverride.Value.PlayerPosition;
            var origin = Target.PerceptionOrigin.position;
            var delta = player + Vector3.up * 1.2f - origin;
            var hit = Physics.Raycast(origin, delta.normalized, out var contact, delta.magnitude - .25f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            Debug.Log($"[R7-Path] phase={phase} tick={runtime.Clock.Tick} player={player:F3} enemy={Target.transform.position:F3} origin={origin:F3} actor=True doors={string.Join(",",bindings.Doors.Select(d=>d.RoomId+":"+d.IsOpen))} oldRay={(hit?contact.collider.name:"clear")} state={TargetState.State} angle={Vector3.Angle(Target.transform.forward,Vector3.ProjectOnPlane(player-Target.transform.position,Vector3.up)):F2} health={mission.Core.GetSnapshot(mission.SessionId).Data.Player.Health}");
        }

        [UnityTest] public IEnumerator Bdd36_Room11ToRoom12DiscoversTurnsAndDamagesPlayer()
        {
            Assert.That(runtime.Actors.ContainsKey(targetId), Is.True, "room-12 actor must exist before entering");
            Open("room-11");
            Place(bindings.Points.Single(p => p.Kind == CombatPointKind.Room && p.RoomId == "room-11").transform.position);
            Step(); Record("room-11");
            Open("room-12");
            var room = bindings.Points.Single(p => p.Kind == CombatPointKind.Room && p.RoomId == "room-12");
            Place(room.transform.position);
            Target.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Target.transform.position - runtime.PlayerRoot.position, Vector3.up));
            Physics.SyncTransforms();
            var health = mission.Core.GetSnapshot(mission.SessionId).Data.Player.Health;
            Step(); Record("room-12-0");
            Assert.That(TargetState.State, Is.EqualTo(CombatEntityState.Attacking));
            Assert.That(Vector3.Angle(Target.transform.forward, Vector3.ProjectOnPlane(runtime.PlayerRoot.position - Target.transform.position, Vector3.up)), Is.LessThan(180), "Turning starts on the first clear frame, even from Idle");
            for (var i = 1; i < 30; i++) { Step(); Record("room-12-" + i); }
            Assert.That(TargetState.State, Is.EqualTo(CombatEntityState.Attacking));
            Assert.That(Vector3.Angle(Target.transform.forward, Vector3.ProjectOnPlane(runtime.PlayerRoot.position - Target.transform.position, Vector3.up)), Is.LessThanOrEqualTo(12));
            Assert.That(Target.ShotVisualFeedbackCount, Is.GreaterThan(0));
            Assert.That(mission.Core.GetSnapshot(mission.SessionId).Data.Player.Health, Is.LessThan(health));
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd36_Room11ToRoom12ClearInsideRoomDoesNotRequireDoorRegistration()
        {
            Place(bindings.Points.Single(p => p.Kind == CombatPointKind.Room && p.RoomId == "room-11").transform.position);
            Step(); Record("registered-closed-room-11");
            // Both players are inside the same room: its entrance door is not between them.
            // This uses the unmodified production startup door state, no fake visibility.
            Place(bindings.Points.Single(p => p.Kind == CombatPointKind.Room && p.RoomId == "room-12").transform.position);
            var direction = runtime.PlayerRoot.position + Vector3.up * 1.2f - Target.PerceptionOrigin.position;
            var blocked = Physics.Raycast(Target.PerceptionOrigin.position, direction.normalized, direction.magnitude - .25f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            Assert.That(blocked, Is.False, "Actual sight inside room must be clear for this reproduction");
            var probe = mission.World.Submit(new CombatInputDto { SessionId = mission.SessionId, EventId = "r7-probe-clear-sight", Tick = runtime.Clock.Tick,
                Kind = CombatInputKind.Perception, EntityId = targetId, TargetId = mission.SessionId + ".player",
                Position = Target.transform.position, Direction = Target.transform.forward, Flag = true });
            Debug.Log($"[R7-Boundary] clear=True registeredDoorOpen={mission.Urban.GetSession(mission.SessionId).Data.Floors.SelectMany(f=>f.Rooms).Single(r=>r.RoomId=="room-12").DoorOpen} perceptionAccepted={probe.Success} error={probe.ErrorCode}");
            Assert.That(probe.Success, Is.True, "Geometry facts must not be rejected by room-door registration");
            mission.Advance();
            for (var i = 0; i < 30; i++) { Step(); Record("registered-closed-room-12-" + i); }
            Assert.That(TargetState.State, Is.EqualTo(CombatEntityState.Attacking));
            Assert.That(Target.ShotVisualFeedbackCount, Is.GreaterThan(0));
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd36_ClosedDoorBlocksRoom12Enemy()
        {
            var door = bindings.Doors.Single(d => d.RoomId == "room-12");
            door.Hinge.localRotation = Quaternion.identity;
            var origin = Target.PerceptionOrigin.position;
            var leafCenter = door.Hinge.TransformPoint(new Vector3(0, 1.2f, 1));
            Place(origin + (leafCenter - origin) * 1.3f - Vector3.up * 1.2f);
            var sight = CombatEnemySight.Sample(origin, runtime.PlayerRoot.position, Target.transform, runtime.PlayerRoot, null, mission.Core.Config);
            Assert.That(sight.InCircle, Is.True);
            Assert.That(sight.Visible, Is.False);
            Assert.That(new[] { sight.HeadBlocker, sight.ChestBlocker, sight.FootBlocker }.All(c => c != null), Is.True);
            Assert.That(sight.ChestBlocker.GetComponentInParent<CombatDoorView>(), Is.SameAs(door));
            var rotation = Target.transform.rotation;
            for (var i = 0; i < 30; i++) Step();
            Assert.That(TargetState.State, Is.EqualTo(CombatEntityState.Idle));
            Assert.That(Target.transform.rotation, Is.EqualTo(rotation));
            Assert.That(Target.ShotVisualFeedbackCount, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd36_AuthoredFloorBlocksEvenWithWidenedVerticalTolerance()
        {
            Place(new Vector3(Target.transform.position.x, 3.25f, Target.transform.position.z));
            var config = mission.Core.Config;
            var sight = CombatEnemySight.Sample(Target.PerceptionOrigin.position, runtime.PlayerRoot.position, Target.transform, runtime.PlayerRoot, null,
                config.WithEnemyVision(config.EnemyRange, 10, config.EnemyHeadSampleHeight, config.EnemyChestSampleHeight, config.EnemyFootSampleHeight, config.EnemyTurnSpeedDegrees));
            Assert.That(sight.InCircle, Is.True, "Widen tolerance to isolate the real floor collider");
            Assert.That(sight.Visible, Is.False);
            Assert.That(sight.HeadBlocker, Is.Not.Null);
            Debug.Log($"[R7-Floor] head={sight.HeadBlocker.name} chest={sight.ChestBlocker.name} foot={sight.FootBlocker.name}");
            for (var i = 0; i < 30; i++) Step();
            Assert.That(TargetState.State, Is.EqualTo(CombatEntityState.Idle));
            Assert.That(Target.ShotVisualFeedbackCount, Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd36_LostActorIsRecreatedFromAuthoritativeSnapshot()
        {
            Object.DestroyImmediate(Target.gameObject);
            Place(bindings.Points.Single(p => p.Kind == CombatPointKind.Room && p.RoomId == "room-12").transform.position);
            Step();
            Assert.That(Target, Is.Not.Null);
            Assert.That(Target.gameObject.activeInHierarchy, Is.True);
            Assert.That(TargetState.State, Is.EqualTo(CombatEntityState.Attacking));
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd36_AllDoorsWithSameRoomIdFollowPublishedOpenState()
        {
            var original = bindings.Doors.Single(d => d.RoomId == "room-12");
            var duplicate = Object.Instantiate(original, runtime.transform);
            duplicate.name = "SecondDoor_room-12";
            duplicate.transform.position += Vector3.right * 40;
            duplicate.InitiallyOpen = false; duplicate.ResetView();
            bindings.Doors = bindings.Doors.Concat(new[] { duplicate }).ToArray();
            Place(original.Hinge.position + original.Hinge.forward); Step(.01f);
            Assert.That(mission.Urban.OpenRoomDoor(mission.SessionId, "room-12").Success, Is.True);
            foreach (var door in new[] { original, duplicate })
            {
                Assert.That(door.IsOpen, Is.True);
                Assert.That(door.Hinge.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).All(c => !c.enabled), Is.True);
            }
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            lease?.Deactivate(); mission?.Dispose(); lease?.Dispose(); lease = null; mission = null;
            yield return null; yield return null;
        }
    }
}
