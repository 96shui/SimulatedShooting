using NUnit.Framework;
using SimulatedShooting.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class TrainingSceneEnvironmentRefreshTests
    {
        [Test]
        public void SceneSwitch_RestoresMovingRangeEnvironmentWithoutDirtyingScene()
        {
            var movingScene = EditorSceneManager.OpenScene(
                "Assets/Scenes/MovingTargetRangeScene.unity", OpenSceneMode.Single);
            var expectedSkybox = RenderSettings.skybox;
            var expectedFogColor = RenderSettings.fogColor;
            var expectedAmbientSkyColor = RenderSettings.ambientSkyColor;

            EditorSceneManager.OpenScene(
                "Assets/Scenes/ZeroingRangeScene.unity", OpenSceneMode.Single);
            movingScene = EditorSceneManager.OpenScene(
                "Assets/Scenes/MovingTargetRangeScene.unity", OpenSceneMode.Single);

            TrainingSceneEnvironmentRefresh.RefreshActiveTrainingSceneEnvironment();

            Assert.That(SceneManager.GetActiveScene().path,
                Is.EqualTo("Assets/Scenes/MovingTargetRangeScene.unity"));
            Assert.That(RenderSettings.skybox, Is.SameAs(expectedSkybox));
            Assert.That(RenderSettings.fogColor, Is.EqualTo(expectedFogColor));
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(expectedAmbientSkyColor));
            Assert.That(movingScene.isDirty, Is.False);
        }
    }
}
