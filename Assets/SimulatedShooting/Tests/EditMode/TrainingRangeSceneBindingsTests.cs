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

        Transform Child(string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(root.transform, false);
            return child;
        }
    }
}
