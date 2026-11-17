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

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Scene-flow fixtures can leave a persistent GameMain. This fixture supplies its own
            // services and must not let the mode-two callback start a real scene load through it.
            if (VRShooting.Unity.Bootstrap.GameMain.Instance != null)
            {
                Object.Destroy(VRShooting.Unity.Bootstrap.GameMain.Instance.gameObject);
                yield return null;
            }
            services = ApplicationServices.CreateDefault(combatScenes: new VRShooting.P3.TestSupport.CombatApplicationFixture());
            root = new GameObject("Test_MainMenuUI", typeof(RectTransform));
            root.SetActive(false);
            ui = root.AddComponent<MainMenuUI>();
            typeof(TrainingUIRoot)
                .GetField("buildOnAwake", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ui, false);
            root.SetActive(true);
            ui.Initialize(services);
            root.GetComponent<TrainingUICanvasAdapter>().SetVrModeForTests(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            services.Combat.Dispose();
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

            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
            Assert.That(button.image.overrideSprite, Is.SameAs(button.spriteState.highlightedSprite));
            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerExitHandler);
            Assert.That(button.image.overrideSprite, Is.SameAs(button.image.sprite));
            ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
            yield return null;
            Canvas.ForceUpdateCanvases();
            ModeIntroTestActions.ConfirmIntroduction(FindButton, click: enter =>
            {
                var center = ((RectTransform)enter.transform).TransformPoint(((RectTransform)enter.transform).rect.center);
                eventData.rayPoints = new List<Vector3> { vrCamera.transform.position,
                    vrCamera.transform.position + (center - vrCamera.transform.position).normalized * 5f };
                results.Clear();
                adapter.TrackedRaycaster.Raycast(eventData, results);
                Assert.That(results.Any(hit => hit.gameObject == enter.gameObject), Is.True);
                ExecuteEvents.Execute(enter.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            });
            yield return null;

            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingBriefing));
            Assert.That(FindById("Screen_ZeroingBriefing").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Screen02_MainMenuOpenZeroingButton_RoutesToZeroingBriefing()
        {
            var button = FindButton("Button_MainMenu_OpenZeroing");

            ModeIntroTestActions.EnterTraining(FindButton);
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingBriefing, services.Router.Current);
            Assert.AreEqual(ScreenId.ZeroingBriefing, ui.ActiveScreen);
            Assert.IsTrue(FindById("Screen_ZeroingBriefing").activeSelf);
            Assert.IsFalse(FindById("Screen_MainMenu").activeSelf);
        }

        [UnityTest]
        public IEnumerator Bdd04_BriefingPickupPromptStaysBelowFrameAndAboveStationStatus()
        {
            ModeIntroTestActions.EnterTraining(FindButton);
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
            Assert.That(FindById("Panel_MainMenu_Controls"), Is.Null);
            Assert.That(FindById("Button_MainMenu_Controls"), Is.Null);
            var entries = new[] { "OpenZeroing", "OpenMovingTarget", "Trench", "Urban" };
            var modes = new[] { "Zeroing", "MovingTarget", "Trench", "Urban" };
            var titles = new[] { "模式一 · 100m 精度射校", "模式二 · 移动目标射击", "模式三 · 堑壕射击", "模式四 · 城镇攻防" };
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                var panel = FindById("Screen_ModeIntro_" + mode);
                Assert.That(panel.activeSelf, Is.False);
                for (var repeat = 0; repeat < 3; repeat++)
                {
                    FindButton("Button_MainMenu_" + entries[i]).onClick.Invoke();
                    yield return null;
                    Assert.That(panel.activeInHierarchy, Is.True);
                    Assert.That(FindById("Text_ModeIntro_" + mode + "_Title").GetComponent<TMP_Text>().text, Is.EqualTo(titles[i]));
                    Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
                    Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
                    Assert.That(services.Presentation.Get(string.Empty).Success, Is.False);
                    Assert.That(services.Combat.Snapshot.Mode, Is.Null);
                    foreach (var side in new[] { "Left", "Right" })
                    {
                        var label = FindById("Text_ModeIntro_" + mode + "_" + side).GetComponent<TMP_Text>();
                        Assert.That(label.text, Does.Not.Contain("Grip").And.Not.Contain("UI").And.Not.Contain("HUD"));
                        label.ForceMeshUpdate();
                        Assert.That(label.isTextOverflowing, Is.False);
                    }
                    FindButton("Button_ModeIntro_" + mode + "_Back").onClick.Invoke();
                    yield return null;
                    Assert.That(panel.activeSelf, Is.False);
                    Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
                    Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
                    Assert.That(services.Combat.Snapshot.Mode, Is.Null);
                }
            }
        }

        [UnityTest]
        public IEnumerator Bdd37_FourConfirmationsPreserveOriginalRoutesAndDoNotReplay()
        {
            var entries = new[] { "OpenZeroing", "OpenMovingTarget", "Trench", "Urban" };
            var targets = new[] { ScreenId.ZeroingBriefing, ScreenId.MovingTargetSettings, ScreenId.TrenchMapSelection, ScreenId.UrbanMapSelection };
            for (var i = 0; i < entries.Length; i++)
            {
                ModeIntroTestActions.EnterTraining(FindButton, entries[i]);
                yield return null;
                Assert.That(services.Router.Current, Is.EqualTo(targets[i]));
                var mode = i == 0 ? "Zeroing" : i == 1 ? "MovingTarget" : entries[i];
                FindButton("Button_ModeIntro_" + mode + "_Enter").onClick.Invoke();
                Assert.That(services.Router.Current, Is.EqualTo(targets[i]));
                Assert.That(FindById("Screen_ModeIntro_" + mode).activeSelf, Is.False);
                Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
                services.Router.Open(ScreenId.MainMenu);
            }
        }

        [UnityTest]
        public IEnumerator Bdd37_FourIntroPanelsPreserveLegacyFrameAndPrimaryEnterStyle()
        {
            // BDD37 正文自适应并可滚动且不阻断按钮；P1 UI task008。
            foreach (var entry in new[] { "OpenZeroing", "OpenMovingTarget", "Trench", "Urban" })
            {
                var mode = entry == "OpenZeroing" ? "Zeroing" : entry == "OpenMovingTarget" ? "MovingTarget" : entry;
                FindButton("Button_MainMenu_" + entry).onClick.Invoke();
                yield return null;
                Canvas.ForceUpdateCanvases();

                var panel = FindById("Panel_ModeIntro_" + mode);
                var panelRect = (RectTransform)panel.transform;
                Assert.That(panelRect.offsetMin, Is.EqualTo(new Vector2(130, 120)), mode);
                Assert.That(panelRect.offsetMax, Is.EqualTo(new Vector2(1790, 960)), mode);
                Assert.That(panelRect.rect.size, Is.EqualTo(new Vector2(1660, 840)), mode);
                var panelImage = panel.GetComponent<Image>();
                Assert.That(panelImage.sprite, Is.Not.Null, mode);
                Assert.That(panelImage.sprite.name, Is.EqualTo("panel_holo_large"), mode);
                Assert.That(panelImage.type, Is.EqualTo(Image.Type.Sliced), mode);
                Assert.That(panelImage.color, Is.EqualTo(Color.white), mode);
                Assert.That(panel.GetComponent<Outline>(), Is.Null, mode);

                var title = FindById("Text_ModeIntro_" + mode + "_Title").GetComponent<TMP_Text>();
                Assert.That(title.rectTransform.offsetMin, Is.EqualTo(new Vector2(80, 750)), mode);
                Assert.That(title.rectTransform.offsetMax, Is.EqualTo(new Vector2(1580, 820)), mode);
                Assert.That(title.fontSize, Is.EqualTo(38), mode);
                var scroll = FindById("Scroll_ModeIntro_" + mode).GetComponent<ScrollRect>();
                var scrollRect = (RectTransform)scroll.transform;
                Assert.That(scrollRect.offsetMin, Is.EqualTo(new Vector2(75, 150)), mode);
                Assert.That(scrollRect.offsetMax, Is.EqualTo(new Vector2(1585, 720)), mode);
                Assert.That(scrollRect.rect.size, Is.EqualTo(new Vector2(1510, 570)), mode);
                Assert.That(scroll.content.GetComponent<HorizontalLayoutGroup>().spacing, Is.EqualTo(60), mode);
                foreach (var side in new[] { "Left", "Right" })
                {
                    var body = FindById("Text_ModeIntro_" + mode + "_" + side).GetComponent<TMP_Text>();
                    Assert.That(body.fontSize, Is.EqualTo(26), mode + side);
                    Assert.That(body.lineSpacing, Is.EqualTo(10), mode + side);
                }

                var enter = FindButton("Button_ModeIntro_" + mode + "_Enter");
                var back = FindButton("Button_ModeIntro_" + mode + "_Back");
                Assert.That(enter.image.sprite, Is.Not.Null, mode);
                Assert.That(enter.image.sprite.name, Is.EqualTo("button_primary_orange"), mode);
                Assert.That(back.image.sprite, Is.Not.Null, mode);
                Assert.That(back.image.sprite.name, Is.EqualTo("button_secondary_cyan"), mode);
                Assert.That(((RectTransform)back.transform).offsetMin, Is.EqualTo(new Vector2(460, 35)), mode);
                Assert.That(((RectTransform)back.transform).offsetMax, Is.EqualTo(new Vector2(760, 105)), mode);
                Assert.That(((RectTransform)enter.transform).offsetMin, Is.EqualTo(new Vector2(900, 35)), mode);
                Assert.That(((RectTransform)enter.transform).offsetMax, Is.EqualTo(new Vector2(1200, 105)), mode);
                back.onClick.Invoke();
            }
        }

        [UnityTest]
        public IEnumerator Bdd37_BodyFitsCommonResolutionsAndOverflowScrollDoesNotCoverButtons()
        {
            root.GetComponent<CanvasScaler>().enabled = false;
            root.GetComponent<TrainingUICanvasAdapter>().enabled = false;
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)root.transform;
            foreach (var resolution in new[] { new Vector2(1920,1080), new Vector2(1280,720), new Vector2(2560,1440) })
            {
                canvasRect.sizeDelta = new Vector2(1920,1080);
                canvasRect.localScale = Vector3.one * (resolution.x / 1920f);
                foreach (var entry in new[] { "OpenZeroing", "OpenMovingTarget", "Trench", "Urban" })
                {
                    var mode = entry == "OpenZeroing" ? "Zeroing" : entry == "OpenMovingTarget" ? "MovingTarget" : entry;
                    FindButton("Button_MainMenu_" + entry).onClick.Invoke();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var scroll = FindById("Scroll_ModeIntro_" + mode).GetComponent<ScrollRect>();
                    Assert.That(scroll.vertical, Is.True);
                    Assert.That(scroll.horizontal, Is.False);
                    Assert.That(scroll.content.rect.height, Is.LessThanOrEqualTo(scroll.viewport.rect.height), mode + resolution);
                    foreach (var side in new[] { "Left", "Right" })
                    {
                        var text = FindById("Text_ModeIntro_" + mode + "_" + side).GetComponent<TMP_Text>();
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False);
                        Assert.That(text.textInfo.characterCount, Is.EqualTo(text.text.Length));
                        var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                        var viewportCorners = new Vector3[4]; scroll.viewport.GetWorldCorners(viewportCorners);
                        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(viewportCorners[0].x - .1f), mode + side);
                        Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(viewportCorners[0].y - .1f), mode + side);
                        Assert.That(corners[2].x, Is.LessThanOrEqualTo(viewportCorners[2].x + .1f), mode + side);
                        Assert.That(corners[2].y, Is.LessThanOrEqualTo(viewportCorners[2].y + .1f), mode + side);
                        foreach (var action in new[] { "Back", "Enter" })
                        {
                            var buttonCorners = new Vector3[4];
                            ((RectTransform)FindButton("Button_ModeIntro_" + mode + "_" + action).transform).GetWorldCorners(buttonCorners);
                            Assert.That(corners[0].y, Is.GreaterThan(buttonCorners[2].y), mode + side + action);
                        }
                    }
                    var enter = FindButton("Button_ModeIntro_" + mode + "_Enter");
                    var back = FindButton("Button_ModeIntro_" + mode + "_Back");
                    Assert.That(enter.transform.IsChildOf(scroll.transform), Is.False);
                    Assert.That(back.transform.IsChildOf(scroll.transform), Is.False);
                    var left = FindById("Text_ModeIntro_" + mode + "_Left").GetComponent<TMP_Text>();
                    var originalSize = left.fontSize;
                    left.fontSize = 60;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                    scroll.verticalNormalizedPosition = 0;
                    Canvas.ForceUpdateCanvases();
                    Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f));
                    left.ForceMeshUpdate();
                    var last = left.textInfo.characterInfo.Last(c => c.isVisible);
                    var lastWorld = left.rectTransform.TransformPoint(last.bottomLeft);
                    var visibleBottom = scroll.viewport.TransformPoint(scroll.viewport.rect.min).y;
                    Assert.That(lastWorld.y, Is.GreaterThanOrEqualTo(visibleBottom - .1f));
                    left.fontSize = originalSize;
                    ExecuteEvents.Execute(back.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                    Assert.That(FindById("Screen_ModeIntro_" + mode).activeSelf, Is.False);
                }
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
            var canvasScaleY = Mathf.Abs(root.transform.lossyScale.y);
            for (var i = 0; i < centers.Length - 1; i++)
                Assert.That((centers[i] - centers[i + 1]) / canvasScaleY, Is.EqualTo(130f).Within(.1f));
            var panelCorners = new Vector3[4]; var frameCorners = new Vector3[4];
            // BDD02 / P1 UI task008: preserve the texture's blue glow without tint or extra borders.
            var heading = FindById("Panel_MainMenu_Menu");
            var headingRect = (RectTransform)heading.transform;
            Assert.That(headingRect.offsetMin, Is.EqualTo(new Vector2(1360, 805)));
            Assert.That(headingRect.offsetMax, Is.EqualTo(new Vector2(1750, 865)));
            Assert.That(heading.GetComponent<Image>().sprite.name, Is.EqualTo("hud_capsule"));
            Assert.That(heading.GetComponent<Image>().type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(heading.GetComponent<Image>().color, Is.EqualTo(Color.white));
            Assert.That(heading.GetComponentsInChildren<Outline>(true), Is.Empty);
            Assert.That(heading.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
            Assert.That(heading.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(heading.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "MainMenuHeadingBorderGraphic"), Is.False);
            var headingLabel = FindById("Text_MainMenu_Menu").GetComponent<TMP_Text>();
            Assert.That(headingLabel.text, Is.EqualTo("选择训练科目"));
            Assert.That(headingLabel.fontSize, Is.EqualTo(20f));
            Assert.That(headingLabel.fontStyle, Is.EqualTo(FontStyles.Normal));
            Assert.That(headingLabel.fontWeight, Is.EqualTo(FontWeight.Regular));
            Assert.That(headingLabel.extraPadding, Is.False);
            Assert.That(headingLabel.alignment, Is.EqualTo(TextAlignmentOptions.Left));
            Assert.That(headingLabel.rectTransform.offsetMin, Is.EqualTo(new Vector2(30, 4)));
            Assert.That(headingLabel.rectTransform.offsetMax, Is.EqualTo(new Vector2(-30, -4)));
            Assert.That((Color32)headingLabel.color, Is.EqualTo(new Color32(231, 242, 235, 255)));
            headingLabel.ForceMeshUpdate();
            Assert.That(headingLabel.textInfo.characterInfo.Take(headingLabel.textInfo.characterCount).Count(c => c.isVisible), Is.EqualTo(6),
                "The title must display all six glyphs with the 9a1c012 text container.");
            Assert.That(headingLabel.isTextOverflowing, Is.False, "No ellipsis may replace the title");
            headingRect.GetWorldCorners(panelCorners);
            ((RectTransform)FindById("Panel_MainMenu_Frame").transform).GetWorldCorners(frameCorners);
            Assert.That(panelCorners[1].y, Is.LessThanOrEqualTo(frameCorners[1].y));
            Assert.That(panelCorners[0].y, Is.GreaterThanOrEqualTo(frameCorners[0].y));
            var buttonCorners = new Vector3[4];
            ((RectTransform)buttons[0].transform).GetWorldCorners(buttonCorners);
            Assert.That(panelCorners[0].y, Is.GreaterThan(buttonCorners[1].y));
            Assert.That(FindById("Button_MainMenu_Controls"), Is.Null);
            Assert.That(FindById("Panel_MainMenu_Controls"), Is.Null);
            Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
            for (var i = 0; i < 2; i++)
            {
                ModeIntroTestActions.EnterTraining(FindButton);
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
