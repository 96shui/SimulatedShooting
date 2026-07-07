using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace VRShooting.Unity.Player
{
    /// <summary>
    /// 无 VR 调试用的持久化主摄像机，切场景后不销毁，并跟随玩家。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class PlayerFollowCamera : MonoBehaviour
    {
        static PlayerFollowCamera instance;

        [SerializeField] Transform followTarget;
        [SerializeField] Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);

        Camera cameraComponent;

        public static PlayerFollowCamera Instance => instance;

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public static PlayerFollowCamera EnsureExists()
        {
            if (instance != null)
            {
                return instance;
            }

            var existing = FindObjectOfType<PlayerFollowCamera>();
            if (existing != null)
            {
                return existing;
            }

            var cameraObject = new GameObject("PlayerFollowCamera");
            return cameraObject.AddComponent<PlayerFollowCamera>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            cameraComponent = GetComponent<Camera>();
            cameraComponent.tag = "MainCamera";

            if (!TryGetComponent<AudioListener>(out _))
            {
                gameObject.AddComponent<AudioListener>();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (instance == this)
            {
                instance = null;
            }
        }

        void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                followTarget.TransformPoint(eyeOffset),
                followTarget.rotation);
        }

        void OnSceneLoaded(UnityScene scene, LoadSceneMode mode)
        {
            DisableSceneCameras(scene);
        }

        void DisableSceneCameras(UnityScene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || cameraComponent == null)
            {
                return;
            }

            cameraComponent.enabled = true;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var sceneCamera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (sceneCamera == cameraComponent)
                    {
                        continue;
                    }

                    sceneCamera.enabled = false;

                    if (sceneCamera.TryGetComponent<AudioListener>(out var listener))
                    {
                        listener.enabled = false;
                    }
                }
            }
        }
    }
}
