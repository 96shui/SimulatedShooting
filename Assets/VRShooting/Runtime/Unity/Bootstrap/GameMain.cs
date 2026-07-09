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

        public static GameMain Instance { get; private set; }

        public ApplicationServices Services { get; private set; }

        public GameStateManager GameState { get; private set; }

        void Awake()
        {
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
            PersistSceneObject("XR Interaction Manager");
            PlayerFollowCamera.EnsureExists();
            TrainingUIHost.EnsureExists();
        }

        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            EnsureSceneUi(scene);
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

        static void PersistSceneObject(string objectName)
        {
            var sceneObject = GameObject.Find(objectName);
            if (sceneObject == null)
            {
                return;
            }

            DontDestroyOnLoad(sceneObject);
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
