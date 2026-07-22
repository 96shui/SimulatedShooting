using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Persistent UI infrastructure shared by scene-owned UI roots.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class TrainingUIHost : MonoBehaviour
    {
        const string RootName = "TrainingUIHost";

        static TrainingUIHost instance;

        public static TrainingUIHost Instance => instance;

        public static TrainingUIHost EnsureExists()
        {
            if (instance != null)
            {
                instance.EnsureEventSystem();
                return instance;
            }

            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null && existingRoot.TryGetComponent(out TrainingUIHost existingHost))
            {
                instance = existingHost;
                instance.EnsureEventSystem();
                return instance;
            }

            var root = existingRoot != null ? existingRoot : new GameObject(RootName);
            if (existingRoot == null)
            {
                DontDestroyOnLoad(root);
            }

            instance = root.GetComponent<TrainingUIHost>() ?? root.AddComponent<TrainingUIHost>();
            instance.EnsureEventSystem();
            return instance;
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
            EnsureEventSystem();
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        void EnsureEventSystem()
        {
            var ownedEventSystem = GetComponentInChildren<EventSystem>(true);
            if (ownedEventSystem == null)
            {
                var sceneEventSystem = FindObjectOfType<EventSystem>(true);
                if (sceneEventSystem != null)
                {
                    sceneEventSystem.transform.SetParent(transform, false);
                    ownedEventSystem = sceneEventSystem;
                }
            }

            if (ownedEventSystem == null)
            {
                var eventSystemRoot = new GameObject("EventSystem");
                eventSystemRoot.transform.SetParent(transform, false);
                ownedEventSystem = eventSystemRoot.AddComponent<EventSystem>();
            }

            EnsureXrInputModule(ownedEventSystem);

            var allEventSystems = FindObjectsOfType<EventSystem>(true);
            for (var i = 0; i < allEventSystems.Length; i++)
            {
                if (allEventSystems[i] == ownedEventSystem)
                {
                    continue;
                }

                allEventSystems[i].gameObject.SetActive(false);
                DestroyUnityObject(allEventSystems[i].gameObject);
            }

            ownedEventSystem.gameObject.SetActive(true);
        }

        static void EnsureXrInputModule(EventSystem eventSystem)
        {
            var inputModule = eventSystem.GetComponent<XRUIInputModule>();
            var modules = eventSystem.GetComponents<BaseInputModule>();
            for (var index = 0; index < modules.Length; index++)
            {
                if (modules[index] == inputModule)
                {
                    continue;
                }

                modules[index].enabled = false;
                DestroyUnityObject(modules[index]);
            }

            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }

            inputModule.enableXRInput = true;
            inputModule.enableMouseInput = true;
            inputModule.enableTouchInput = true;
            inputModule.enableGamepadInput = true;
            inputModule.enableJoystickInput = true;
            inputModule.enableBuiltinActionsAsFallback = true;
        }

        static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
