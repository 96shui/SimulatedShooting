using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Application;
using VRShooting.Unity.Player;
using VRShooting.Unity.UI;

namespace VRShooting.Unity.Bootstrap
{
    /// <summary>
    /// 全局启动入口，负责初始化应用层服务。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameMain : MonoBehaviour
    {
        const string MainSceneName = "MainScene";
        const string ZeroingRangeSceneName = "ZeroingRangeScene";
        const string MovingTargetSceneName = GameStateManager.MovingTargetSceneName;

        public static GameMain Instance { get; private set; }

        public ApplicationServices Services { get; private set; }

        public GameStateManager GameState { get; private set; }

        MainMenuXRModeController mainMenuXrModeController;

        void Awake()
        {
            ActivateSceneObject("XR Interaction Manager", gameObject.scene);

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitManagers();
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureSceneUi(SceneManager.GetActiveScene());
        }

        void InitManagers()
        {
            Services = ApplicationServices.CreateDefault();
            GameState = GameStateManager.Instance;
            mainMenuXrModeController = MainMenuXRModeController.EnsureExists(gameObject);

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == MainSceneName)
            {
                mainMenuXrModeController.RefreshForScene(activeScene);
            }
            else if (activeScene.name != ZeroingRangeSceneName && activeScene.name != MovingTargetSceneName)
            {
                PlayerFollowCamera.EnsureExists();
            }

            TrainingUIHost.EnsureExists();
        }

        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            EnsureSceneUi(scene);
            mainMenuXrModeController?.RefreshForScene(scene);
        }

        void EnsureSceneUi(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.name == MainSceneName && Services != null)
            {
                MainMenuUI.EnsureExistsInScene(Services);
            }

            if (scene.name == ZeroingRangeSceneName && Services != null)
            {
                ZeroingRangeUI.EnsureExistsInScene(Services);
            }
        }

        static void ActivateSceneObject(string objectName, UnityEngine.SceneManagement.Scene scene)
        {
            var candidates = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var index = 0; index < candidates.Length; index++)
            {
                var candidate = candidates[index];
                if (candidate == null || candidate.name != objectName || candidate.scene != scene)
                {
                    continue;
                }

                candidate.SetActive(true);
                return;
            }
        }

        void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            GameStateManager.DestroyInstance();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Services = null;
            GameState = null;
            Instance = null;
        }
    }
}
