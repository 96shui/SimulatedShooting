using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public class CombatScenePlayModeTests
    {
        CombatSceneBindings bindings;
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            bindings = Object.FindObjectOfType<CombatSceneBindings>();
        }

        [UnityTest]
        public IEnumerator Bdd14_19_20_StreetAllFloorsRoomsAndReturnAreReachable()
        {
            foreach (var door in bindings.Doors) door.Apply("route-open", true);
            yield return new WaitForSeconds(1);
            foreach (var point in bindings.Points.Where(p => p.RequiresNavigation))
            {
                Assert.That(NavMesh.SamplePosition(point.transform.position, out var sample, 0.75f, NavMesh.AllAreas), Is.True, point.Id);
                var path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(bindings.PlayerSpawn.position, sample.position, NavMesh.AllAreas, path), Is.True, point.Id);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), point.Id);
                Assert.That(NavMesh.CalculatePath(sample.position, bindings.PlayerSpawn.position, NavMesh.AllAreas, path), Is.True, point.Id);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), point.Id + " return");
            }
        }

        [UnityTest]
        public IEnumerator Bdd20_DoorOnlyChangesOnOutputAndDuplicateEventsAreIdempotent()
        {
            var door = bindings.Doors[0];
            Assert.That(door.IsOpen, Is.False);
            door.Apply("accepted-1", true);
            door.Apply("accepted-1", false);
            yield return new WaitForSeconds(1);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(door.Obstacle.enabled, Is.False);
            door.ResetView();
            Assert.That(door.IsOpen, Is.False);
            Assert.That(door.Obstacle.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Bdd15_17_21_HitDedupCorpseRetentionAndRetryCleanup()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            Assert.That(fixture.Actors.Count, Is.EqualTo(15));
            var actor = fixture.Actors[0];
            actor.PlayHit("hit-1"); actor.PlayHit("hit-1");
            Assert.That(actor.HitFeedbackCount, Is.EqualTo(1));
            actor.ApplyDead(true);
            yield return new WaitForSeconds(0.2f);
            Assert.That(actor.gameObject.activeSelf, Is.True);
            Assert.That(actor.IsDead, Is.True);
            Assert.That(actor.Agent.enabled, Is.False);
            for (var i = 0; i < 3; i++) { fixture.ResetFixture(); yield return null; }
            Assert.That(Object.FindObjectsOfType<CombatActorView>().Length, Is.EqualTo(15));
            Assert.That(fixture.Actors.All(a => !a.IsDead), Is.True);
            Assert.That(Object.FindObjectsOfType<Camera>().Count(c => c.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<AudioListener>().Count(c => c.isActiveAndEnabled), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bdd15_20_NavigationFailureAndArrivalReportFacts()
        {
            var actor = Object.FindObjectOfType<CombatSceneFixture>().Actors.Last();
            string outcome = null;
            actor.NavigationReported += (id, reached) => outcome = reached ? "arrived" : "failed";
            actor.MoveTo(new Vector3(9999, 0, 9999), Quaternion.identity);
            Assert.That(outcome, Is.EqualTo("failed"));
            actor.MoveTo(actor.transform.position + Vector3.right, Quaternion.Euler(0, 180, 0));
            var timeout = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < timeout && outcome != "arrived") yield return null;
            Assert.That(outcome, Is.EqualTo("arrived"));
        }

        [UnityTest]
        public IEnumerator Bdd19_20_ConfirmationRequiresRangeAndUnoccludedRay()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            var room = bindings.Points.First(p => p.Kind == CombatPointKind.Room);
            var player = fixture.Walker;
            player.InputEnabled = false;
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            var doorPosition = room.Door.transform.position + new Vector3(0, 1.2f, 1);
            player.transform.position = room.Door.transform.position + new Vector3(-.4f, 0, 1);
            Physics.SyncTransforms();
            var ray = new Ray(player.transform.position + Vector3.up * 1.2f, Vector3.right);
            Assert.That(room.Confirm(player.transform, ray), Is.True);
            Assert.That(room.Door.IsOpen, Is.True);
            player.transform.position = bindings.PlayerSpawn.position;
            Assert.That(room.Confirm(player.transform, new Ray(player.transform.position, doorPosition - player.transform.position)), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bdd15_20_WallsBlockPerceptionAndSceneUnloadRemovesActors()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            var actor = fixture.Actors.First();
            Assert.That(actor.CanSee(new Vector3(80, 1.5f, actor.transform.position.z), ~0), Is.False);
            var old = fixture.Actors.ToArray();
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            Assert.That(old.All(a => a == null), Is.True);
            Assert.That(Object.FindObjectsOfType<CombatActorView>().Length, Is.EqualTo(15));
        }

        [UnityTest]
        public IEnumerator Bdd19_20_CharacterColliderWalksFromTrenchThroughEveryRoom()
        {
            foreach (var door in bindings.Doors) door.Apply("walk-open", true);
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            foreach (var actor in fixture.Actors) actor.gameObject.SetActive(false);
            yield return new WaitForSeconds(1);
            var controller = fixture.Walker.GetComponent<CharacterController>();
            foreach (var point in bindings.Points.Where(p => p.Kind == CombatPointKind.Room).Concat(bindings.Points.Where(p => p.Kind == CombatPointKind.SearchNode).Take(1)))
            {
                var path = new NavMeshPath();
                Assert.That(NavMesh.SamplePosition(controller.transform.position, out var start, 1, NavMesh.AllAreas), Is.True);
                NavMesh.CalculatePath(start.position, point.transform.position, NavMesh.AllAreas, path);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                foreach (var corner in path.corners.Skip(1))
                {
                    for (int i = 0; i < 1500; i++)
                    {
                        var delta = corner - controller.transform.position;
                        delta.y = 0;
                        if (delta.magnitude < .18f) break;
                        controller.Move(Vector3.ClampMagnitude(delta, .1f) + Vector3.down * .05f);
                        if (i % 20 == 0) yield return null;
                    }
                    var remaining = corner - controller.transform.position; remaining.y = 0;
                    Assert.That(remaining.magnitude, Is.LessThan(.3f), point.Id + " physical corner " + corner +
                        " player " + controller.transform.position + " nearby " + string.Join(",", Physics.OverlapSphere(controller.transform.position+Vector3.up, 1, ~0, QueryTriggerInteraction.Ignore).Select(c=>c.name)) +
                        " door angle " + (point.Door != null ? point.Door.Hinge.localEulerAngles.ToString() : "n/a"));
                }
            }
        }

        [UnityTest]
        public IEnumerator Bdd14_19_XrAndNoVrHaveExclusiveCameraAndPreserveHeadPose()
        {
            var mode = bindings.GetComponent<ZeroingRangeXRModeController>();
            Assert.That(mode, Is.Not.Null);
            mode.SetVrModeForTests(true);
            yield return null;
            var head = mode.XrOrigin.GetComponentInChildren<Camera>();
            Assert.That(head, Is.Not.Null);
            var position = head.transform.localPosition;
            var rotation = head.transform.localRotation;
            Object.FindObjectOfType<CombatSceneFixture>().Actors[0].PlayShot("head-stability");
            Assert.That(head.transform.localPosition, Is.EqualTo(position));
            Assert.That(head.transform.localRotation, Is.EqualTo(rotation));
            Assert.That(Object.FindObjectsOfType<Camera>().Count(c=>c.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<AudioListener>().Count(c=>c.isActiveAndEnabled), Is.EqualTo(1));
            mode.SetVrModeForTests(false);
            Assert.That(mode.NoVrCamera.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Bdd17_21_MaximumActorsCorpsesPerformanceSample()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            foreach (var actor in fixture.Actors.Take(13)) actor.ApplyDead(true);
            var samples = new float[180];
            var timings = new FrameTiming[1];
            var gc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame");
            long allocated = 0;
            for (int i=0;i<samples.Length;i++)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                samples[i]=Time.unscaledDeltaTime*1000;
                if(gc.Valid) allocated += gc.LastValue;
            }
            var count=FrameTimingManager.GetLatestTimings(1,timings);
            System.Array.Sort(samples);
            System.IO.Directory.CreateDirectory("Logs/Scene3");
            System.IO.File.WriteAllText("Logs/Scene3/performance.txt",
                "Editor fixture / 13 corpses + 2 teammates / 180 frames\nDevice: " + SystemInfo.graphicsDeviceName +
                "\nCPU: " + SystemInfo.processorType + "\nResolution: " + Screen.width + "x" + Screen.height +
                "\nFrame mean ms: " + samples.Average() + "\nFrame p95 ms: " + samples[171] +
                "\nGC bytes/frame: " + (gc.Valid ? (allocated/180).ToString() : "unavailable") +
                "\nGPU ms: " + (count>0 && timings[0].gpuFrameTime>0 ? timings[0].gpuFrameTime.ToString() : "unavailable in batch Editor") +
                "\nMono used bytes: " + UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() +
                "\nNot a VR device budget acceptance. Target refresh rate remains unspecified.");
            gc.Dispose();
            Assert.That(fixture.Actors.Count(a=>a.IsDead), Is.EqualTo(13));
        }
    }
}
