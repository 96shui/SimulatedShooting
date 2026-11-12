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
            root.GetComponent<TrainingUICanvasAdapter>().SetVrModeForTests(false);
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
            vrCamera.transform.SetPositionAndRotation(new Vector3(0f, 1.62f, 0f), Quaternion.identity);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.worldCamera, Is.SameAs(vrCamera));
            Assert.That(adapter.DesktopRaycaster.enabled, Is.False);
            Assert.That(adapter.TrackedRaycaster.enabled, Is.True);
            var canvasRect = root.transform as RectTransform;
            var canvasBottom = canvasRect.position.y -
                canvasRect.rect.height * Mathf.Abs(canvasRect.lossyScale.y) * 0.5f;
            var horizontalDistance = Vector3.ProjectOnPlane(
                canvasRect.position - vrCamera.transform.position, Vector3.up).magnitude;
            var worldWidth = canvasRect.rect.width * Mathf.Abs(canvasRect.lossyScale.x);
            var angularWidth = 2f * Mathf.Atan(worldWidth * 0.5f / horizontalDistance) * Mathf.Rad2Deg;
            Assert.That(canvasBottom, Is.GreaterThanOrEqualTo(0.10f),
                "A low or temporarily invalid HMD pose must not place the menu below the floor.");
            Assert.That(horizontalDistance, Is.InRange(1.2f, 1.5f));
            Assert.That(angularWidth, Is.GreaterThanOrEqualTo(65f),
                "The VR menu should occupy a large readable portion of the headset view.");
            Assert.That(canvasRect.position.y - vrCamera.transform.position.y, Is.InRange(-0.15f, -0.04f),
                "The menu should follow the stabilized HMD pose and sit only slightly below eye level.");

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
        public IEnumerator Bdd04_BriefingPickupPromptStaysBelowFrameAndAboveStationStatus()
        {
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            var prompt = (RectTransform)FindById("Training.Shared.PickupPrompt").transform;
            var station = (RectTransform)FindById("Training.Shared.FiringStationState").transform;
            var frame = (RectTransform)FindById("Panel_ZeroingBriefing_Frame").transform;
            var back = (RectTransform)FindButton("Button_ZeroingBriefing_Back").transform;
            var start = (RectTransform)FindButton("Button_ZeroingBriefing_Start").transform;
            Assert.That(prompt.offsetMin, Is.EqualTo(new Vector2(480, 46)));
            Assert.That(prompt.offsetMax, Is.EqualTo(new Vector2(1440, 84)));
            Assert.That(prompt.offsetMin.y, Is.GreaterThan(station.offsetMax.y));
            Assert.That(prompt.offsetMax.y, Is.LessThan(frame.offsetMin.y));
            Assert.That(prompt.offsetMax.y, Is.LessThan(back.offsetMin.y));
            Assert.That(prompt.offsetMax.y, Is.LessThan(start.offsetMin.y));
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
        public IEnumerator Screen04_ZeroingBriefingStartButton_CreatesSessionAndWaitsForWeaponPickup()
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
            Assert.IsFalse(services.TrainingSessions.Current.Seed.IsFixed);
            Assert.AreEqual(ScreenId.ZeroingBriefing, services.Router.Current);
            var presentation = services.Presentation.Get(services.TrainingSessions.Current.SessionId);
            Assert.IsTrue(presentation.Success, presentation.Message);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingWeaponPickup, presentation.Data.Phase);
            Assert.IsTrue(presentation.Data.LargePanelVisible);
            Assert.IsFalse(presentation.Data.MinimalHudVisible);
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
            Assert.AreEqual(ScreenId.ZeroingBriefing, services.Router.Current);
            var presentation = services.Presentation.Get(firstSessionId);
            Assert.IsTrue(presentation.Success, presentation.Message);
            Assert.AreEqual(TrainingPresentationPhase.AwaitingWeaponPickup, presentation.Data.Phase);
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
            Assert.That(rules.text, Does.Contain("每轮弹数：4发"));
            Assert.That(rules.text, Does.Contain("50cm x 50cm"));
            Assert.That(rules.text, Does.Contain("10环直径：10cm"));
            Assert.That(rules.text, Does.Contain("通过条件"));
            Assert.That(FindById("Text_ZeroingBriefing_Header").GetComponent<TMP_Text>().text, Is.EqualTo("任务简报"));
        }

        [UnityTest]
        public IEnumerator Bdd02_ControlsPanelOpensAndClosesWithoutLeavingMenu()
        {
            Assert.That(FindById("Text_MainMenu_Subtitle").GetComponent<TMP_Text>().text, Is.EqualTo("战术训练中心"));
            Assert.That(FindById("Text_MainMenu_BottomStatus").GetComponent<TMP_Text>().text,
                Is.EqualTo("单机训练" + new string(' ', 37) + "选择科目后查看任务简报"));
            var panel = FindById("Panel_MainMenu_Controls");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.activeSelf, Is.False);
            FindButton("Button_MainMenu_Controls").onClick.Invoke();
            yield return null;
            Assert.That(panel.activeInHierarchy, Is.True);
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
            var copy = string.Join("\n", panel.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text));
            Assert.That(copy, Does.Contain("右手侧握键抓枪 / 主手握持；左手侧握键副手握持。"));
            Assert.That(copy, Does.Not.Contain("Grip"));
            CaptureControlsPanel();
            FindButton("Button_MainMenu_Controls_Back").onClick.Invoke();
            yield return null;
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
        }

        void CaptureControlsPanel()
        {
            var canvas = root.GetComponent<Canvas>();
            var mode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            var cameraRoot = new GameObject("ControlsEvidenceCamera");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black; camera.cullingMask = 1 << 31;
            var parts = root.GetComponentsInChildren<Transform>(true);
            var layers = parts.Select(part => part.gameObject.layer).ToArray();
            var target = new RenderTexture(1920, 1080, 24);
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var part in parts) part.gameObject.layer = 31;
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                foreach (var label in root.GetComponentsInChildren<TMP_Text>()) label.ForceMeshUpdate();
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
                var folder = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Logs/CodexZeroingControls");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "controls.png"), texture.EncodeToPNG());
            }
            finally
            {
                for (var i = 0; i < parts.Length; i++) parts[i].gameObject.layer = layers[i];
                canvas.renderMode = mode; canvas.worldCamera = previousCamera; RenderTexture.active = previousActive;
                target.Release(); Object.Destroy(target); Object.Destroy(texture); Object.Destroy(cameraRoot);
            }
        }

        [UnityTest]
        public IEnumerator Screen02_CreatorsEntryAndPanelAreAbsentAndNavigationRemainsUsable()
        {
            // BDD02：主菜单不再提供制作人入口；task008。
            string[] removed = { "Button_MainMenu_AboutCreators", "Panel_MainMenu_AboutCreators",
                "Panel_MainMenu_CreatorsFrame", "Text_MainMenu_AboutCreatorsTitle", "Text_MainMenu_Creators",
                "Button_MainMenu_CloseAboutCreators" };
            void AssertRemoved()
            {
                foreach (var id in removed)
                {
                    Assert.That(FindById(id), Is.Null, id);
                    Assert.That(root.GetComponentsInChildren<Transform>(true).Any(node => node.name == id), Is.False, id);
                }
            }
            yield return null;
            AssertRemoved();
            var buttons = new[] { "Button_MainMenu_OpenZeroing", "Button_MainMenu_OpenMovingTarget",
                "Button_MainMenu_Trench", "Button_MainMenu_Urban" }.Select(FindButton).ToArray();
            Canvas.ForceUpdateCanvases();
            var centers = buttons.Select(button => ((RectTransform)button.transform).position.y).ToArray();
            Assert.That(centers[0] - centers[1], Is.EqualTo(centers[1] - centers[2]).Within(.1f));
            Assert.That(centers[1] - centers[2], Is.EqualTo(centers[2] - centers[3]).Within(.1f));
            var buttonCorners = new Vector3[4]; var frameCorners = new Vector3[4];
            ((RectTransform)buttons[3].transform).GetWorldCorners(buttonCorners);
            ((RectTransform)FindById("Panel_MainMenu_Frame").transform).GetWorldCorners(frameCorners);
            var controlsCorners = new Vector3[4];
            ((RectTransform)FindButton("Button_MainMenu_Controls").transform).GetWorldCorners(controlsCorners);
            Assert.That(controlsCorners[1].y, Is.LessThan(buttonCorners[0].y));
            Assert.That(controlsCorners[0].y, Is.GreaterThanOrEqualTo(frameCorners[0].y));
            Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
            for (var i = 0; i < 2; i++)
            {
                FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
                Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingBriefing));
                Assert.That(FindById("Screen_ZeroingBriefing").activeSelf, Is.True);
                FindButton("Button_ZeroingBriefing_Back").onClick.Invoke();
                Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
                Assert.That(FindById("Screen_MainMenu").activeSelf, Is.True);
                AssertRemoved();
            }
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
