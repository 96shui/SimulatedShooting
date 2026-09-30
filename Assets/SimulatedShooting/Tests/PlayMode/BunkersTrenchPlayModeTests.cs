using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.TestTools;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class BunkersTrenchPlayModeTests
    {
        // BDD 12 "模式 03 使用新战壕场景"; BDD 14 "新战壕入口与敌人候选点";
        // BDD 15 "小地图与新战壕对应"; P3 scene tasks 003/006.
        [UnityTest]
        public IEnumerator Bdd12_14_15_ModeThreeLoadsOriginalDemoAndUsesNewPlan()
        {
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            var app = GameMain.Instance.Services.Combat;
            Assert.That(app.OpenMode(TrainingMode.Trench).Success, Is.True);
            var load = app.SelectMapAsync("trench-a", RandomSeed.Fixed(20260928));
            while (!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True, load.Result.Message);
            var runtime = Object.FindObjectOfType<CombatSceneRuntime>();
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.gameObject.scene.path, Is.EqualTo(UnityCombatSceneLoader.TrenchScenePath));
            var binding = runtime.GetComponent<CombatSceneBindings>();
            Assert.That(Vector3.Distance(runtime.PlayerRoot.position, binding.TrenchEntry.position), Is.LessThan(.2f));
            Assert.That(binding.WeaponAnchor.gameObject.activeSelf, Is.True, "The briefing needs a visible rifle beside the spawn");
            Assert.That(binding.WeaponAnchor.GetComponentsInChildren<Renderer>().Any(r => r.enabled), Is.True);
            Assert.That(binding.Maps.Single(m => m.Id == "trench-a").Plan.name, Does.Contain("OriginalDemoMap"));
            Assert.That(binding.GeometryRoot.GetComponentsInChildren<Terrain>(true).Length, Is.EqualTo(1));
            var ui = GameMain.Instance.GetComponent<P3LiveUIController>().View;
            var preview = ui.TrenchBriefingView.ProjectedMap.LastMap;
            Assert.That(preview.MapId, Is.EqualTo("trench-a"));
            var playerMarker = preview.Markers.Single(m => m.MarkerId == "preview.player");
            var expected = binding.Maps.Single(m => m.Id == "trench-a").WorldToMap(binding.TrenchEntry.position);
            Assert.That(Vector2.Distance(playerMarker.NormalizedPosition, expected), Is.LessThan(.01f));
            app.ReturnToMainMenu();
            while (SceneManager.GetSceneByPath(UnityCombatSceneLoader.TrenchScenePath).isLoaded) yield return null;
        }

        // BDD 14 "新战壕进入实战后保持完整操作".
        [UnityTest]
        public IEnumerator Bdd14_NewTrenchAllowsContinuousMovementAndFiring()
        {
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
            var app = GameMain.Instance.Services.Combat;
            Assert.That(app.OpenMode(TrainingMode.Trench).Success, Is.True);
            var load = app.SelectMapAsync("trench-a", RandomSeed.Fixed(20260928));
            while (!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True, load.Result.Message);
            var runtime = Object.FindObjectOfType<CombatSceneRuntime>();
            runtime.ManualStepping = true;
            var input = new ManualXRTrainingInput();
            runtime.InputOverride = input;
            Assert.That(app.Start().Success, Is.True);
            var binding = runtime.GetComponent<CombatSceneBindings>();
            var rifle = runtime.GetComponentInChildren<TrainingRifleGrabInteractable>();
            Assert.That(rifle, Is.Not.Null, "A grabbable rifle must replace the briefing preview");
            Assert.That(binding.WeaponAnchor.gameObject.activeSelf, Is.False);
            Assert.That(Vector3.Distance(rifle.RearAttach.position, runtime.PlayerCamera.transform.position), Is.LessThan(1.25f));
            Assert.That(runtime.Actors.Values.Count(a => a.EntityId.Contains(".enemy-")), Is.InRange(3, 5));
            Assert.That(runtime.Actors.Values.Count(a => a.EntityId.Contains(".teammate-")), Is.EqualTo(2));
            var start = runtime.PlayerRoot.position;
            var forward = runtime.PlayerRoot.forward;
            var ground = binding.GeometryRoot.GetComponentInChildren<Terrain>();
            foreach (var actor in runtime.Actors.Values)
            {
                var boots = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(r => r.name.EndsWith("_Boots"));
                Assert.That(boots, Is.Not.Null, actor.EntityId);
                var footGround = ground.SampleHeight(actor.transform.position) + ground.transform.position.y;
                Assert.That(boots.bounds.min.y, Is.InRange(footGround - .06f, footGround + 1.35f), actor.EntityId);
                Assert.That(actor.HitCollider.bounds.min.y, Is.EqualTo(boots.bounds.min.y).Within(.08f), actor.EntityId);
                Assert.That(NavMesh.SamplePosition(actor.transform.position, out _, .8f, NavMesh.AllAreas), Is.True, actor.EntityId);
            }
            Assert.That(start.y, Is.EqualTo(ground.SampleHeight(start) + ground.transform.position.y).Within(.25f),
                "Player feet must start on the imported terrain");
            input.SetMoveAxis(Vector2.up);
            for (var frame = 0; frame < 75; frame++)
            {
                runtime.Step(.02f);
                Assert.That(runtime.LastFrameResult.Success, Is.True, runtime.LastFrameResult.Message);
                input.AdvanceFrame();
            }
            input.SetMoveAxis(Vector2.zero);
            Assert.That(Vector3.Dot(runtime.PlayerRoot.position - start, forward), Is.GreaterThan(2f),
                "Player must move through the authored trench corridor");
            input.Press(XRTrainingInputButton.RightGrip);
            input.Press(XRTrainingInputButton.LeftGrip);
            runtime.Step(.02f);
            Assert.That(runtime.LastFrameResult.Success, Is.True, runtime.LastFrameResult.Message);
            input.AdvanceFrame();
            var before = app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.Ammo.CurrentMagazine;
            input.Press(XRTrainingInputButton.Trigger);
            runtime.Step(.02f);
            Assert.That(runtime.LastFrameResult.Success, Is.True, runtime.LastFrameResult.Message);
            Assert.That(app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.Ammo.CurrentMagazine,
                Is.EqualTo(before - 1), "A normal trigger press must fire in the new scene");
            app.ReturnToMainMenu();
            while (SceneManager.GetSceneByPath(UnityCombatSceneLoader.TrenchScenePath).isLoaded) yield return null;
        }

        [UnityTest]
        public IEnumerator NewSceneInspectionWalkerCanLeaveEntry()
        {
            yield return SceneManager.LoadSceneAsync("BunkersOriginalMode3CombatScene");
            yield return null;
            var fixture = Object.FindObjectOfType<CombatSceneFixture>();
            Assert.That(fixture, Is.Not.Null);
            Assert.That(fixture.enabled, Is.True);
            var expectedFloorY = new[] { 2.08f, 2.64f, 2.32f, 2.35f, 2.19f, 2.14f, 1.24f };
            foreach (var enemy in fixture.Actors.Where(a => a.EntityId.StartsWith("trench-spawn-")))
            {
                var foot = enemy.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .First(r => r.name.EndsWith("_Boots"));
                var pointIndex = int.Parse(enemy.EntityId.Substring("trench-spawn-".Length)) - 1;
                var floorY = expectedFloorY[pointIndex];
                Assert.That(foot.bounds.min.y, Is.EqualTo(floorY).Within(.08f), enemy.EntityId + " boots");
                Assert.That(enemy.HitCollider.bounds.min.y, Is.EqualTo(floorY).Within(.08f), enemy.EntityId + " collision");
            }
            var walker = fixture.Walker;
            var start = walker.transform.position;
            var forward = walker.transform.forward;
            var previousFrameTime = Time.captureDeltaTime;
            Time.captureDeltaTime = .02f;
            try
            {
                for (var frame = 0; frame < 75; frame++)
                {
                    walker.Move(Vector2.up, Vector2.zero);
                    yield return null;
                }
            }
            finally { Time.captureDeltaTime = previousFrameTime; }
            Assert.That(Vector3.Dot(walker.transform.position - start, forward), Is.GreaterThan(2f),
                "The editor inspection rig must not be blocked by the scene rifle or terrain");
        }
    }
}
