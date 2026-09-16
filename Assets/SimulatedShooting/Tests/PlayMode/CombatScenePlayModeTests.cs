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
        [UnityTest]
        public IEnumerator Bdd19_ArtRenderProfileRestoresQualityWhenDisabled()
        {
            var profile = bindings.GetComponent<CombatSceneRenderProfile>();
            Assert.That(profile, Is.Not.Null);
            Assert.That(QualitySettings.renderPipeline, Is.EqualTo(profile.Profile));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(90));
            profile.enabled = false;
            Assert.That(QualitySettings.renderPipeline, Is.Not.EqualTo(profile.Profile));
            profile.enabled = true;
            Assert.That(QualitySettings.renderPipeline, Is.EqualTo(profile.Profile));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bdd19_AllHousesHaveDummiesAndPhysicalEntrances()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            foreach (var actor in fixture.Actors) actor.gameObject.SetActive(false);
            var controller = fixture.Walker.GetComponent<CharacterController>();
            foreach (Transform house in bindings.GeometryRoot.Find("Town_PerimeterBuildings"))
            {
                var interior = house.Find("Interior");
                Assert.That(interior, Is.Not.Null, house.name);
                Assert.That(interior.Find("TrainingDummy").GetComponentsInChildren<Renderer>().Any(r => r.enabled), Is.True);
                var entrance = interior.Find("WalkEntrance");
                var outside = entrance.position - house.forward * 2;
                var inside = entrance.position + house.forward * 2;
                var path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(bindings.UrbanEntry.position, inside, NavMesh.AllAreas, path), Is.True, house.name);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), house.name);
                controller.enabled = false;
                controller.transform.position = bindings.UrbanEntry.position + Vector3.up*.05f;
                controller.enabled = true;
                foreach (var corner in path.corners.Skip(1).Concat(path.corners.Reverse().Skip(1)))
                {
                    for (int i = 0; i < 1800; i++)
                    {
                        var delta = corner - controller.transform.position; delta.y = 0;
                        if (delta.magnitude < .18f) break;
                        controller.Move(Vector3.ClampMagnitude(delta,.1f) + Vector3.down*.03f);
                        if (i % 30 == 0) yield return null;
                    }
                    var remaining = corner - controller.transform.position; remaining.y = 0;
                    Assert.That(remaining.magnitude, Is.LessThan(.3f), house.name + " street route " + corner + " player " + controller.transform.position);
                    Assert.That(controller.transform.position.y, Is.GreaterThan(-.25f), house.name);
                }
                controller.enabled = false; controller.transform.position = outside + Vector3.up * .1f; controller.enabled = true;
                foreach (var target in new[] { inside, outside })
                {
                    for (int i = 0; i < 150; i++)
                    {
                        var delta = target - controller.transform.position; delta.y = 0;
                        if (delta.magnitude < .1f) break;
                        controller.Move(Vector3.ClampMagnitude(delta,.08f) + Vector3.down*.03f);
                        if (i % 20 == 0) yield return null;
                    }
                    var remaining = target - controller.transform.position; remaining.y = 0;
                    Assert.That(remaining.magnitude, Is.LessThan(.2f), house.name + " doorway " + controller.transform.position);
                    Assert.That(controller.transform.position.y, Is.GreaterThan(-.2f), house.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Bdd19_DistrictGroundGridAndElevatedGuardsPreventFalls()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            foreach (var actor in fixture.Actors) actor.gameObject.SetActive(false);
            var controller = fixture.Walker.GetComponent<CharacterController>();
            for (int x = -24; x <= 64; x += 2)
                for (int z = 29; z <= 113; z += 2)
                    Assert.That(Physics.Raycast(new Vector3(x,.15f,z), Vector3.down,.7f,~0,QueryTriggerInteraction.Ignore), Is.True, "Ground gap " + x + "," + z);
            var guards = bindings.GeometryRoot.Find("WalkwaySafety");
            Assert.That(guards, Is.Not.Null);
            foreach (Transform guard in guards)
                foreach (float along in new[] { -.35f, 0, .35f })
                    foreach (float diagonal in new[] { -.25f, 0, .25f })
                    {
                        var point = guard.TransformPoint(new Vector3(0,-.5f,along));
                        // Select the side supported by the landing/ramp rather than the drop side.
                        Vector3 start = Vector3.zero; bool supported = false;
                        foreach (int side in new[] { -1, 1 })
                        {
                            var candidate = point + guard.right * side * .6f + Vector3.up * .2f;
                            if (!Physics.Raycast(candidate,Vector3.down,out var hit,.6f,~0,QueryTriggerInteraction.Ignore)) continue;
                            start = hit.point + Vector3.up*.05f; supported = true; break;
                        }
                        Assert.That(supported, Is.True, guard.name + " unsupported test location " + point);
                        controller.enabled = false; controller.transform.position = start; controller.enabled = true;
                        var direction = point-start; direction.y = 0; direction.Normalize();
                        var minY = start.y - .4f;
                        for (int i = 0; i < 35; i++)
                            controller.Move((direction+guard.forward*diagonal)*.05f + Vector3.down*.03f);
                        Assert.That(controller.transform.position.y, Is.GreaterThan(minY), guard.name + " fell at " + point);
                    }
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            yield return null;
            bindings = Object.FindObjectOfType<CombatSceneBindings>();
        }

        [UnityTest]
        public IEnumerator Bdd14_19_StartupAndResetKeepDroneAndPlayerStable()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            var drone = bindings.Drone.position;
            for (int reset = 0; reset < 2; reset++)
            {
                fixture.ResetFixture();
                var until = Time.time + 2;
                while (Time.time < until)
                {
                    fixture.Walker.Move(Vector2.zero, Vector2.zero);
                    Assert.That(Vector3.Distance(bindings.Drone.position, drone), Is.LessThan(.001f), "Inspection must not trigger briefing takeoff");
                    Assert.That(fixture.Walker.transform.position.y, Is.InRange(-.1f, .15f));
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator Bdd19_ExteriorGroundAndWallsPreventLeavingTheTown()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            foreach (var actor in fixture.Actors) actor.gameObject.SetActive(false);
            var controller = fixture.Walker.GetComponent<CharacterController>();
            // Sample open ground between buildings, behind the search building, and beside the street.
            foreach (var position in new[] { new Vector3(-23, 0, 45), new Vector3(62, 0, 60), new Vector3(24, 0, 111), new Vector3(0, 0, 95), new Vector3(40, 0, 47) })
            {
                controller.enabled = false;
                controller.transform.position = position;
                controller.enabled = true;
                for (int i = 0; i < 60; i++) controller.Move(Vector3.down * .05f);
                Assert.That(controller.transform.position.y, Is.GreaterThan(-.25f), "Unsupported exterior ground " + position);
            }
            // Push straight and diagonally against all sides and both sides of the trench opening.
            var starts = new[] { new Vector3(-23, 0, 45), new Vector3(63, 0, 60), new Vector3(24, 0, 112), new Vector3(0, 0, 30), new Vector3(45, 0, 30), new Vector3(-23, 0, 30), new Vector3(63, 0, 30), new Vector3(-23, 0, 112), new Vector3(63, 0, 112) };
            var directions = new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back, Vector3.back, new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1) };
            for (int edge = 0; edge < starts.Length; edge++)
                foreach (float diagonal in new[] { 0f, .25f, -.25f })
                {
                    controller.enabled = false;
                    controller.transform.position = starts[edge];
                    controller.enabled = true;
                    var tangent = Vector3.Cross(directions[edge], Vector3.up);
                    for (int i = 0; i < 120; i++) controller.Move((directions[edge] + tangent * diagonal) * .1f + Vector3.down * .03f);
                    var p = controller.transform.position;
                    Assert.That(p.x, Is.InRange(-25f, 65f), "Town side wall " + edge);
                    Assert.That(p.z, Is.InRange(28f, 114f), "Town end wall " + edge);
                    Assert.That(p.y, Is.GreaterThan(-.25f));
                }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bdd15_19_SpawnTrenchWallsBlockWalkingOut()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            fixture.Walker.InputEnabled = false;
            foreach (var actor in fixture.Actors) actor.gameObject.SetActive(false);
            var controller = fixture.Walker.GetComponent<CharacterController>();
            foreach (var direction in new[] { Vector3.left, Vector3.right, Vector3.back })
            {
                controller.enabled = false;
                controller.transform.position = bindings.PlayerSpawn.position;
                controller.enabled = true;
                for (int i = 0; i < 150; i++) controller.Move(direction * .1f + Vector3.down * .03f);
                var p = controller.transform.position;
                Assert.That(p.x, Is.InRange(-2f, 2f));
                Assert.That(p.z, Is.GreaterThan(-10f));
                Assert.That(p.y, Is.GreaterThan(-.1f));
            }
            yield return null;
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
        public IEnumerator Bdd19_InspectionCanJumpDirectlyToDistrictAndBuildingViews()
        {
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            var productionSpawn = bindings.PlayerSpawn.position;
            fixture.JumpToObservationPoint(0);
            yield return null;
            Assert.That(fixture.Walker.ObservationMode, Is.True);
            Assert.That(fixture.Walker.transform.position.y, Is.GreaterThan(20));
            Assert.That(bindings.PlayerSpawn.position, Is.EqualTo(productionSpawn));
            fixture.JumpToObservationPoint(6);
            Assert.That(fixture.Walker.transform.position.y, Is.GreaterThan(8));
        }

        [UnityTest]
        public IEnumerator Bdd20_DoorOnlyChangesOnOutputAndDuplicateEventsAreIdempotent()
        {
            var door = bindings.Doors[0];
            Assert.That(door.IsOpen, Is.True);
            door.Apply("accepted-1", false);
            door.Apply("accepted-1", true);
            yield return new WaitForSeconds(1);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(door.Obstacle.enabled, Is.True);
            door.ResetView();
            Assert.That(door.IsOpen, Is.True);
            Assert.That(door.Obstacle.enabled, Is.False);
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
            room.Door.Apply("prepare-confirm", false);
            yield return new WaitForSeconds(1);
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
        public IEnumerator Bdd19_20_CharacterColliderWalksThroughEveryRoomAndPermanentPassage()
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

            foreach (var floorY in new[] { 0f, 3.6f })
            {
                var path = new NavMeshPath();
                Assert.That(NavMesh.SamplePosition(new Vector3(31, floorY, 70), out var adjacentRoom, 1, NavMesh.AllAreas), Is.True);
                Assert.That(NavMesh.SamplePosition(new Vector3(31, floorY, 82), out var nextRoom, 1, NavMesh.AllAreas), Is.True);
                Assert.That(NavMesh.CalculatePath(adjacentRoom.position, nextRoom.position, NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), "floor " + floorY + " adjacent rooms");
                Assert.That(path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum(), Is.LessThan(14), "path must use the internal doorway");
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
