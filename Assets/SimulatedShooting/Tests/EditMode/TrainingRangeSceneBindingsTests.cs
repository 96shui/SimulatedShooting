using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class TrainingRangeSceneBindingsTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FiringStation");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Task003_CompleteUniqueBindingsValidate()
        {
            LogAssert.Expect(LogType.Error,
                new Regex("PlayerRootAnchor is missing.*TargetRootAnchor is missing"));
            var bindings = root.AddComponent<TrainingRangeSceneBindings>();
            bindings.Configure(
                Child("PlayerRoot"),
                Child("ProneHead"),
                Child("AimForward"),
                Child("LargeUi"),
                Child("MinimalHud"),
                Child("WeaponRack"),
                Child("TargetRoot"));

            Assert.That(bindings.ValidateBindings(out var error), Is.True, error);
        }

        [Test]
        public void Task003_MissingOrDuplicateBindingsReturnLocatableErrors()
        {
            LogAssert.Expect(LogType.Error,
                new Regex("PlayerRootAnchor is missing.*TargetRootAnchor is missing"));
            var bindings = root.AddComponent<TrainingRangeSceneBindings>();
            var shared = Child("Shared");
            bindings.Configure(shared, shared, Child("Aim"), Child("LargeUi"), Child("MinimalHud"),
                Child("WeaponRack"), null);

            Assert.That(bindings.ValidateBindings(out var error), Is.False);
            StringAssert.Contains("TargetRootAnchor", error);
            StringAssert.Contains("duplicate", error.ToLowerInvariant());
        }

        [TestCase("Assets/Scenes/ZeroingRangeScene.unity")]
        [TestCase("Assets/Scenes/MovingTargetRangeScene.unity")]
        public void Task003_006_MigratedScenesHaveNoMissingScripts(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var missingCount = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));

            Assert.That(missingCount, Is.Zero, $"Missing scripts found in {scenePath}");
        }

        [Test]
        public void Task006_RenamedSceneKeepsOriginalGuidAndBuildPath()
        {
            var movingMeta = File.ReadAllText("Assets/Scenes/MovingTargetRangeScene.unity.meta");
            var buildPaths = EditorBuildSettings.scenes.Select(item => item.path).ToArray();

            StringAssert.Contains("guid: 6e4b13515eb5cfe4f95f0ddfc95cdabd", movingMeta);
            Assert.That(File.Exists("Assets/Scenes/MovingargetScene.unity"), Is.False);
            CollectionAssert.Contains(buildPaths, "Assets/Scenes/MovingTargetRangeScene.unity");
            CollectionAssert.DoesNotContain(buildPaths, "Assets/Scenes/MovingargetScene.unity");
        }

        [TestCase("Assets/Scenes/MovingTargetRangeScene.unity")]
        [TestCase("Assets/Scenes/ZeroingRangeScene.unity")]
        public void Bdd00_AuthoredRangeEnablesAutomaticHeadsetDetection(string scenePath)
        {
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            var mode=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ZeroingRangeXRModeController>(true)).Single();
            Assert.That(new SerializedObject(mode).FindProperty("autoDetectVrDisplayInEditor").boolValue,Is.True,
                "A real headset must work on ordinary Play without a test override");
        }

        [TestCase("Assets/Scenes/ZeroingRangeScene.unity", "ZeroingRange", 25f)]
        [TestCase("Assets/Scenes/MovingTargetRangeScene.unity", "MovingTargetRange", 50f)]
        public void Bdd29_SavedSceneHasShorterPhysicalDistanceWithReachableWeaponAndUi(string path, string prefix, float metres)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var nodes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneTestId>(true)).ToArray();
            Transform At(string id) => nodes.Single(n => n.Id == id).transform;
            var station = At(prefix + ".FiringStation.Root").GetComponent<TrainingRangeSceneBindings>();
            var player = station.PlayerRootAnchor;
            var target = prefix == "ZeroingRange" ? At(prefix + ".Target.Center").position : At(prefix + ".Route.Root").position;
            Assert.That(Vector3.Dot(target - player.position, player.forward), Is.EqualTo(metres).Within(.001f), "Actual saved world geometry, not a label or DTO distance.");
            Assert.That(Vector3.Dot(station.TargetRootAnchor.position - player.position, player.forward), Is.EqualTo(metres).Within(.001f));
            Assert.That(Vector3.Distance(At(prefix + ".Origin.VR").position, player.position), Is.LessThan(.001f));
            Assert.That(Mathf.Abs(At(prefix + ".Camera.NoVR").position.z - player.position.z), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(station.WeaponRackAnchor.position, player.position), Is.LessThan(1.5f));
            Assert.That(Vector3.Distance(station.LargeUiAnchor.position, player.position), Is.LessThan(3f));
            var rifle = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<VRShooting.Unity.Weapons.WeaponPrefabBinding>(true)).Single();
            Assert.That(Vector3.Distance(rifle.RearHandGrip.position, player.position), Is.LessThan(2f));
            if (prefix == "MovingTargetRange")
                Assert.That(Vector3.Distance(At(prefix + ".Route.RightEndpoint").position, At(prefix + ".Route.LeftEndpoint").position), Is.EqualTo(40f).Within(.001f));
        }

        [Test]
        public void Bdd29_PhysicalTargetDrawsAnIdempotentTwentyCentimetreAssistedTenRing()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZeroingRangeScene.unity", OpenSceneMode.Single);
            var surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneTestId>(true))
                .Single(n => n.Id == "ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            ChestTargetArtwork.Apply(surface); ChestTargetArtwork.Apply(surface);
            var circles = surface.TargetCenter.GetComponentsInChildren<LineRenderer>(true);
            Assert.That(circles.Length, Is.EqualTo(1), "The saved physical target must show its actual assisted scoring boundary.");
            Assert.That(circles[0].loop, Is.True);
            // Compare the intended 8-bit color, not a native shader-uniform float round-trip.
            Assert.That((Color32)circles[0].sharedMaterial.color, Is.EqualTo(new Color32(247, 185, 85, 255)), "The production Unlit shader uses its material tint, not LineRenderer vertex color.");
            for (var i = 0; i < circles[0].positionCount; i++)
            {
                var point = circles[0].GetPosition(i);
                Assert.That(new Vector2(point.x, point.y).magnitude, Is.EqualTo(.10f).Within(.00001f));
            }
            Assert.That(surface.TenRingRadiusCm, Is.EqualTo(10f));
        }

        Transform Child(string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(root.transform, false);
            return child;
        }
    }
}
