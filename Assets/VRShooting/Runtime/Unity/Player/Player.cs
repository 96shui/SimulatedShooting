using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityScene = UnityEngine.SceneManagement.Scene;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;

namespace VRShooting.Unity.Player
{
    /// <summary>
    /// 无 VR 设备时的玩家移动替身，移动输入来自 task003 的 <see cref="IXRTrainingInput.MoveAxis"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public class Player : MonoBehaviour
    {
        const string ZeroingRangeSceneName = "ZeroingRangeScene";
        const string ShootingPositionName = "ShootingPosition";

        static Player instance;

        [SerializeField] float moveSpeed = 2f;
        [SerializeField] Transform headTransform;

        IXRTrainingInput trainingInput;

        CharacterController characterController;
        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

        public static Player Instance => instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            characterController = GetComponent<CharacterController>();

            ResolveTrainingInput();
            BindFollowCamera();

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

        void OnSceneLoaded(UnityScene scene, LoadSceneMode mode)
        {
            if (instance != this)
            {
                return;
            }

            ResolveTrainingInput();
            BindFollowCamera();
            AlignToSceneSpawn(scene);
        }

        void Update()
        {
            ResolveTrainingInput();
            var input = trainingInput.MoveAxis;
            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            var viewTransform = ResolveHeadTransform();
            var forward = viewTransform != null ? viewTransform.forward : transform.forward;
            var right = viewTransform != null ? viewTransform.right : transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            var moveDirection = forward * input.y + right * input.x;
            var motion = moveDirection * (moveSpeed * Time.deltaTime);

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(motion);
                return;
            }

            transform.position += motion;
        }

        void ResolveTrainingInput()
        {
            if (GameMain.Instance?.Services?.TrainingInput != null)
            {
                trainingInput = GameMain.Instance.Services.TrainingInput;
                return;
            }

            trainingInput ??= new InputSystemXRTrainingInput();
        }

        void BindFollowCamera()
        {
            if (IsVrDisplayRunning())
            {
                var hmdCamera = GetComponentInChildren<Camera>(true);
                if (hmdCamera != null)
                {
                    headTransform = hmdCamera.transform;
                }

                return;
            }

            var followCamera = PlayerFollowCamera.EnsureExists();
            followCamera.FollowTarget = transform;
            headTransform = followCamera.transform;
        }

        Transform ResolveHeadTransform()
        {
            if (headTransform != null)
            {
                return headTransform;
            }

            return PlayerFollowCamera.Instance != null
                ? PlayerFollowCamera.Instance.transform
                : transform;
        }

        bool IsVrDisplayRunning()
        {
            displays.Clear();
            SubsystemManager.GetInstances(displays);
            for (var index = 0; index < displays.Count; index++)
            {
                if (displays[index] != null && displays[index].running)
                {
                    return true;
                }
            }

            return false;
        }

        void AlignToSceneSpawn(UnityScene scene)
        {
            if (scene.name != ZeroingRangeSceneName)
            {
                return;
            }

            var spawn = FindTransformInScene(scene, ShootingPositionName);
            if (spawn == null)
            {
                return;
            }

            TeleportTo(spawn.position, spawn.rotation);
        }

        void TeleportTo(Vector3 position, Quaternion rotation)
        {
            var hadController = characterController != null && characterController.enabled;
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(position, rotation);

            if (characterController != null && hadController)
            {
                characterController.enabled = true;
            }
        }

        static Transform FindTransformInScene(UnityScene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = FindTransformRecursive(root.transform, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        static Transform FindTransformRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var match = FindTransformRecursive(parent.GetChild(i), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
