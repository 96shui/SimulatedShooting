using UnityEngine;
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
        }

        void InitManagers()
        {
            Services = ApplicationServices.CreateDefault();
            GameState = GameStateManager.Instance;
            PersistSceneObject("XR Interaction Manager");
            PlayerFollowCamera.EnsureExists();
            P1PersistentUIHost.EnsureExists();
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
            Services = null;
            GameState = null;
            Instance = null;
        }
    }
}
