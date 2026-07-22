using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    /// <summary>
    /// task008 UI 流程测试。追溯 docs/BDD/screens/02-游戏主界面.feature.md 与 04-100m任务说明.feature.md。
    /// </summary>
    [TestFixture]
    public class Screen02_04_MainMenuZeroingBriefingUITests
    {
        GameObject root;
        GameObject vrCameraRoot;
        ApplicationServices services;
        MainMenuUI ui;

        [SetUp]
        public void SetUp()
        {
            services = ApplicationServices.CreateDefault();
            root = new GameObject("Test_MainMenuUI", typeof(RectTransform));
            root.SetActive(false);
            ui = root.AddComponent<MainMenuUI>();
            typeof(TrainingUIRoot)
                .GetField("buildOnAwake", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ui, false);
            root.SetActive(true);
            ui.Initialize(services);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }

            if (vrCameraRoot != null)
            {
                Object.Destroy(vrCameraRoot);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen02_InputInfrastructure_SupportsDesktopAndXrPointers()
        {
            yield return null;

            var canvas = root.GetComponent<Canvas>();
            var adapter = root.GetComponent<TrainingUICanvasAdapter>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(adapter, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(adapter.DesktopRaycaster, Is.Not.Null);
            Assert.That(adapter.DesktopRaycaster.enabled, Is.True);
            Assert.That(adapter.TrackedRaycaster, Is.Not.Null);
            Assert.That(adapter.TrackedRaycaster.enabled, Is.False);

            var activeEventSystems = Object.FindObjectsOfType<EventSystem>(true)
                .Where(candidate => candidate.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(activeEventSystems, Has.Length.EqualTo(1));
            Assert.That(activeEventSystems[0].GetComponent<XRUIInputModule>(), Is.Not.Null);
            Assert.That(activeEventSystems[0].GetComponents<BaseInputModule>().Count(module => module.enabled), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Screen02_VrTrackedRay_HitsAndClicksOpenZeroingButton()
        {
            vrCameraRoot = new GameObject("XR Origin (Test)");
            var cameraObject = new GameObject("Main Camera (Test)");
            cameraObject.transform.SetParent(vrCameraRoot.transform, false);
            var vrCamera = cameraObject.AddComponent<Camera>();
            vrCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var canvas = root.GetComponent<Canvas>();
            var adapter = root.GetComponent<TrainingUICanvasAdapter>();
            adapter.SetVrModeForTests(true, vrCamera);
            adapter.ForcePlacementForTests();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.worldCamera, Is.SameAs(vrCamera));
            Assert.That(adapter.DesktopRaycaster.enabled, Is.False);
            Assert.That(adapter.TrackedRaycaster.enabled, Is.True);

            var button = FindButton("Button_MainMenu_OpenZeroing");
            var buttonRect = button.transform as RectTransform;
            var buttonCenter = buttonRect.TransformPoint(buttonRect.rect.center);
            var rayDirection = (buttonCenter - vrCamera.transform.position).normalized;
            var eventSystem = Object.FindObjectsOfType<EventSystem>(true)
                .First(candidate => candidate.gameObject.activeInHierarchy);
            var eventData = new TrackedDeviceEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                layerMask = ~0,
                rayPoints = new List<Vector3>
                {
                    vrCamera.transform.position,
                    vrCamera.transform.position + rayDirection * 5f
                }
            };
            var results = new List<RaycastResult>();

            adapter.TrackedRaycaster.Raycast(eventData, results);

            Assert.That(results.Any(result =>
                result.gameObject == button.gameObject || result.gameObject.transform.IsChildOf(button.transform)), Is.True,
                "The tracked XR ray should hit the 100m main-menu button or one of its graphics.");

            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            yield return null;

            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingBriefing));
            Assert.That(FindById("Screen_ZeroingBriefing").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Screen02_MainMenuOpenZeroingButton_RoutesToZeroingBriefing()
        {
            var button = FindButton("Button_MainMenu_OpenZeroing");

            button.onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingBriefing, services.Router.Current);
            Assert.AreEqual(ScreenId.ZeroingBriefing, ui.ActiveScreen);
            Assert.IsTrue(FindById("Screen_ZeroingBriefing").activeSelf);
            Assert.IsFalse(FindById("Screen_MainMenu").activeSelf);
        }

        [UnityTest]
        public IEnumerator Screen04_ZeroingBriefingBackButton_ReturnsToMainMenu()
        {
            services.Router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);
            yield return null;

            FindButton("Button_ZeroingBriefing_Back").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.MainMenu, services.Router.Current);
            Assert.IsTrue(FindById("Screen_MainMenu").activeSelf);
        }

        [UnityTest]
        public IEnumerator Screen04_ZeroingBriefingStartButton_CreatesSessionAndRoutesToHud()
        {
            services.Router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);
            TrainingSessionDto? started = null;
            services.EventBus.Subscribe<SessionStartedEvent>(evt => started = evt.Session);
            yield return null;

            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return null;

            Assert.IsTrue(services.TrainingSessions.HasActiveSession);
            Assert.AreEqual(TrainingMode.Zeroing100m, services.TrainingSessions.Current.Mode);
            Assert.AreEqual(SessionState.Running, services.TrainingSessions.Current.State);
            Assert.AreEqual(ScreenId.ZeroingHud, services.Router.Current);
            Assert.IsTrue(started.HasValue);
            Assert.AreEqual(services.TrainingSessions.Current.SessionId, started.Value.SessionId);
        }

        [UnityTest]
        public IEnumerator Screen04_ZeroingBriefingStartButton_IsIdempotentForRepeatedClicks()
        {
            services.Router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);
            var startEvents = 0;
            services.EventBus.Subscribe<SessionStartedEvent>(_ => startEvents++);
            yield return null;

            var start = FindButton("Button_ZeroingBriefing_Start");
            start.onClick.Invoke();
            var firstSessionId = services.TrainingSessions.Current.SessionId;
            start.onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, startEvents);
            Assert.AreEqual(firstSessionId, services.TrainingSessions.Current.SessionId);
            Assert.AreEqual(ScreenId.ZeroingHud, services.Router.Current);
        }

        [UnityTest]
        public IEnumerator Screen04_ZeroingBriefing_RequiredTextAndTestIdsExist()
        {
            services.Router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);
            yield return null;

            Assert.IsNotNull(FindById("Text_ZeroingBriefing_Title"));
            Assert.IsNotNull(FindById("Text_ZeroingBriefing_Rules"));
            Assert.IsNotNull(FindById("Panel_ZeroingBriefing_Target"));
            Assert.IsNotNull(FindById("Button_ZeroingBriefing_Start"));
            Assert.IsNotNull(FindById("Button_ZeroingBriefing_Back"));

            var title = FindById("Text_ZeroingBriefing_Title").GetComponent<TextMeshProUGUI>();
            var rules = FindById("Text_ZeroingBriefing_Rules").GetComponent<TextMeshProUGUI>();

            Assert.That(title.text, Does.Contain("100m"));
            Assert.That(rules.text, Does.Contain("射击距离：100m"));
            Assert.That(rules.text, Does.Contain("射击模式：单发射"));
            Assert.That(rules.text, Does.Contain("每轮弹数：3发"));
            Assert.That(rules.text, Does.Contain("50cm x 50cm"));
            Assert.That(rules.text, Does.Contain("10环直径：10cm"));
            Assert.That(rules.text, Does.Contain("通过条件"));
        }

        Button FindButton(string id)
        {
            var go = FindById(id);
            Assert.IsNotNull(go, id);
            var button = go.GetComponent<Button>();
            Assert.IsNotNull(button, id);
            return button;
        }

        GameObject FindById(string id)
        {
            var allIds = root.GetComponentsInChildren<UITestId>(true);
            for (var i = 0; i < allIds.Length; i++)
            {
                if (allIds[i].Id == id)
                {
                    return allIds[i].gameObject;
                }
            }

            return null;
        }
    }
}
