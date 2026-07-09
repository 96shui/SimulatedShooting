using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
                eventSystemRoot.AddComponent<InputSystemUIInputModule>();
            }

            var allEventSystems = FindObjectsOfType<EventSystem>(true);
            for (var i = 0; i < allEventSystems.Length; i++)
            {
                if (allEventSystems[i] == ownedEventSystem)
                {
                    continue;
                }

                DestroyObject(allEventSystems[i].gameObject);
            }
        }

        static void DestroyObject(GameObject target)
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
