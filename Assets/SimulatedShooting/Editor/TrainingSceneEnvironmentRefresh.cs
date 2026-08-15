using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace SimulatedShooting.Editor
{
    [InitializeOnLoad]
    public static class TrainingSceneEnvironmentRefresh
    {
        const string ZeroingScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        const string MovingScenePath = "Assets/Scenes/MovingTargetRangeScene.unity";

        static int pendingSceneHandle;

        static TrainingSceneEnvironmentRefresh()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        static void OnSceneOpened(UnityScene scene, OpenSceneMode mode)
        {
            QueueRefresh(scene);
        }

        static void OnActiveSceneChanged(UnityScene previousScene, UnityScene nextScene)
        {
            QueueRefresh(nextScene);
        }

        static void QueueRefresh(UnityScene scene)
        {
            if (!IsTrainingScene(scene))
                return;

            pendingSceneHandle = scene.handle;
            EditorApplication.delayCall -= RefreshPendingEnvironment;
            EditorApplication.delayCall += RefreshPendingEnvironment;
        }

        static void RefreshPendingEnvironment()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.handle != pendingSceneHandle)
                return;

            pendingSceneHandle = 0;
            RefreshActiveTrainingSceneEnvironment();
        }

        public static void RefreshActiveTrainingSceneEnvironment()
        {
            if (!IsTrainingScene(SceneManager.GetActiveScene()))
                return;

            DynamicGI.UpdateEnvironment();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        static bool IsTrainingScene(UnityScene scene)
        {
            return scene.IsValid() &&
                   (scene.path == ZeroingScenePath || scene.path == MovingScenePath);
        }
    }
}
