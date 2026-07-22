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
        AudioListener audioListener;
        bool outputEnabled = true;

        public static PlayerFollowCamera Instance => instance;

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public bool OutputEnabled => outputEnabled;

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

            if (!TryGetComponent(out audioListener))
            {
                audioListener = gameObject.AddComponent<AudioListener>();
            }

            SetOutputEnabled(true);
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
            if (!outputEnabled || followTarget == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                followTarget.TransformPoint(eyeOffset),
                followTarget.rotation);
        }

        void OnSceneLoaded(UnityScene scene, LoadSceneMode mode)
        {
            if (!outputEnabled)
            {
                return;
            }

            DisableSceneCameras(scene);
        }

        public void SetOutputEnabled(bool enabled)
        {
            outputEnabled = enabled;
            if (cameraComponent == null)
            {
                cameraComponent = GetComponent<Camera>();
            }

            if (audioListener == null)
            {
                audioListener = GetComponent<AudioListener>();
            }

            cameraComponent.enabled = enabled;
            if (audioListener != null)
            {
                audioListener.enabled = enabled;
            }

            gameObject.tag = enabled ? "MainCamera" : "Untagged";
        }

        void DisableSceneCameras(UnityScene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || cameraComponent == null)
            {
                return;
            }

            cameraComponent.enabled = outputEnabled;

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
