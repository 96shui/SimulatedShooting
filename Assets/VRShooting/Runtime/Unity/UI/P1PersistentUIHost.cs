using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VRShooting.Unity.Bootstrap;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// P1 UI 持久化根节点。切场景时不销毁 Canvas 与 EventSystem。
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class P1PersistentUIHost : MonoBehaviour
    {
        const string RootName = "PersistentUI";

        static P1PersistentUIHost instance;

        P1MainMenuZeroingBriefingUI ui;
        bool uiInitialized;

        public static P1PersistentUIHost Instance => instance;

        public static P1MainMenuZeroingBriefingUI Ui => instance != null ? instance.ui : null;

        public static P1PersistentUIHost EnsureExists()
        {
            if (instance != null)
            {
                instance.EnsureUiInitialized();
                return instance;
            }

            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null && existingRoot.TryGetComponent(out P1PersistentUIHost existingHost))
            {
                instance = existingHost;
                instance.EnsureUiInitialized();
                return instance;
            }

            var root = existingRoot != null ? existingRoot : new GameObject(RootName);
            if (existingRoot == null)
            {
                DontDestroyOnLoad(root);
            }

            instance = root.GetComponent<P1PersistentUIHost>() ?? root.AddComponent<P1PersistentUIHost>();
            instance.AdoptOrCreateUi();
            instance.AdoptOrCreateEventSystem();
            instance.EnsureUiInitialized();
            return instance;
        }

        public static bool TryAdoptUi(P1MainMenuZeroingBriefingUI candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            EnsureExists();

            if (instance.ui != null && instance.ui != candidate)
            {
                Destroy(candidate.gameObject);
                return false;
            }

            instance.ui = candidate;
            candidate.transform.SetParent(instance.transform, false);
            return true;
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
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        void AdoptOrCreateUi()
        {
            if (ui != null)
            {
                return;
            }

            ui = GetComponentInChildren<P1MainMenuZeroingBriefingUI>(true);
            if (ui == null)
            {
                ui = FindObjectOfType<P1MainMenuZeroingBriefingUI>(true);
            }

            if (ui != null)
            {
                ui.transform.SetParent(transform, false);
                return;
            }

            var uiRoot = new GameObject("P1_MainMenuZeroingBriefingUI", typeof(RectTransform));
            uiRoot.transform.SetParent(transform, false);
            ui = uiRoot.AddComponent<P1MainMenuZeroingBriefingUI>();
        }

        void AdoptOrCreateEventSystem()
        {
            if (GetComponentInChildren<EventSystem>(true) != null)
            {
                return;
            }

            var eventSystem = FindObjectOfType<EventSystem>(true);
            if (eventSystem != null)
            {
                eventSystem.transform.SetParent(transform, false);
                return;
            }

            var eventSystemRoot = new GameObject("EventSystem");
            eventSystemRoot.transform.SetParent(transform, false);
            eventSystemRoot.AddComponent<EventSystem>();
            eventSystemRoot.AddComponent<InputSystemUIInputModule>();
        }

        void EnsureUiInitialized()
        {
            if (ui == null || uiInitialized)
            {
                return;
            }

            var services = GameMain.Instance != null ? GameMain.Instance.Services : null;
            if (services == null)
            {
                return;
            }

            if (!ui.IsInitialized)
            {
                ui.Initialize(services);
            }

            uiInitialized = true;
        }
    }
}
