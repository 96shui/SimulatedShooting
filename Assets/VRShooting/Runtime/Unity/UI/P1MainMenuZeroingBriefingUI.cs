using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Unity.Bootstrap;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// P1 主菜单与 100m 任务说明 UI。追溯 task008 与 docs/BDD/screens/02、04。
    /// </summary>
    public sealed class P1MainMenuZeroingBriefingUI : MonoBehaviour
    {
        const string ZeroingMapId = "zeroing-range-100m";
        const string ZeroingWeaponId = "training-rifle";

        [SerializeField]
        bool buildOnAwake = true;

        ApplicationServices services;
        IDisposable screenSubscription;
        RectTransform mainMenuScreen;
        RectTransform zeroingBriefingScreen;
        Button openZeroingButton;
        Button startButton;
        Button backButton;
        static TMP_FontAsset uiFont;

        public ScreenId ActiveScreen { get; private set; } = ScreenId.MainMenu;

        public string LastError { get; private set; } = string.Empty;

        public Button OpenZeroingButton => openZeroingButton;

        public Button StartButton => startButton;

        public Button BackButton => backButton;

        void Awake()
        {
            if (!buildOnAwake)
            {
                return;
            }

            Initialize(GameMain.Instance != null ? GameMain.Instance.Services : ApplicationServices.CreateDefault());
        }

        void OnDestroy()
        {
            screenSubscription?.Dispose();
        }

        public void Initialize(ApplicationServices applicationServices)
        {
            services = applicationServices ?? ApplicationServices.CreateDefault();
            LastError = string.Empty;

            ClearChildren(transform);
            BuildCanvas();
            SubscribeRouter();
            ShowScreen(services.Router.Current);
        }

        void SubscribeRouter()
        {
            screenSubscription?.Dispose();
            screenSubscription = services.EventBus.Subscribe<ScreenChangedEvent>(evt => ShowScreen(evt.CurrentScreen));
        }

        void BuildCanvas()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            gameObject.name = "P1_MainMenuZeroingBriefingUI";
            AddTestId(gameObject, "P1_MainMenuZeroingBriefingUI");

            mainMenuScreen = CreateScreen("Screen_MainMenu");
            BuildMainMenu(mainMenuScreen);

            zeroingBriefingScreen = CreateScreen("Screen_ZeroingBriefing");
            BuildZeroingBriefing(zeroingBriefingScreen);
        }

        RectTransform CreateScreen(string name)
        {
            var screen = CreateRect(name, transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(screen.gameObject, new Color32(7, 16, 13, 242));
            AddTestId(screen.gameObject, name);
            return screen;
        }

        void BuildMainMenu(RectTransform parent)
        {
            AddPanel(parent, "Panel_MainMenu_Frame", new Vector2(60, 60), new Vector2(-60, -60), new Color32(11, 19, 16, 220), new Color32(45, 66, 56, 255));
            AddPanel(parent, "Placeholder_MainMenu_BaseHall", new Vector2(130, 170), new Vector2(1220, 690), new Color32(10, 21, 18, 160), new Color32(73, 107, 90, 255), "素材占位：基地大厅 / 全息任务台 / 武器墙");
            AddLabel(parent, "Text_MainMenu_Title", "VR射击训练系统 DEMO", 42, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(360, 360), new Vector2(1120, 440), new Color32(231, 242, 235, 255));
            AddLabel(parent, "Text_MainMenu_Subtitle", "沉浸训练   精准提升   实战为先", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(470, 445), new Vector2(1010, 500), new Color32(143, 217, 255, 255));

            AddPanel(parent, "Panel_MainMenu_Profile", new Vector2(95, 720), new Vector2(450, 940), new Color32(17, 29, 24, 220), new Color32(56, 84, 71, 255),
                "玩家档案\n训练等级：L03\n最近评级：良好");
            AddPanel(parent, "Panel_MainMenu_Menu", new Vector2(1390, 110), new Vector2(1700, 170), new Color32(17, 29, 24, 230), new Color32(56, 84, 71, 255), "主菜单入口");

            openZeroingButton = AddButton(parent, "Button_MainMenu_OpenZeroing", "100m精度射校靶", new Vector2(1390, 205), new Vector2(1700, 275), true);
            openZeroingButton.onClick.AddListener(OnOpenZeroingClicked);

            DisableButton(AddButton(parent, "Button_MainMenu_MovingTarget_Disabled", "移动目标射击", new Vector2(1390, 300), new Vector2(1700, 370), false));
            DisableButton(AddButton(parent, "Button_MainMenu_Trench_Disabled", "堑壕射击", new Vector2(1390, 395), new Vector2(1700, 465), false));
            DisableButton(AddButton(parent, "Button_MainMenu_Urban_Disabled", "城镇攻防", new Vector2(1390, 490), new Vector2(1700, 560), false));
            DisableButton(AddButton(parent, "Button_MainMenu_Armory_Disabled", "武器库", new Vector2(1390, 585), new Vector2(1700, 655), false));
            DisableButton(AddButton(parent, "Button_MainMenu_Settings_Disabled", "设置", new Vector2(1390, 680), new Vector2(1700, 750), false));

            AddPanel(parent, "Panel_MainMenu_BottomStatus", new Vector2(120, 985), new Vector2(1800, 1032), new Color32(17, 29, 24, 210), new Color32(56, 84, 71, 255),
                "底部状态栏：网络 / 音量 / 用户 / 版本 / 提示");
        }

        void BuildZeroingBriefing(RectTransform parent)
        {
            AddPanel(parent, "Panel_ZeroingBriefing_Frame", new Vector2(110, 60), new Vector2(1390, 820), new Color32(11, 19, 16, 215), new Color32(45, 156, 255, 255));
            AddPanel(parent, "Placeholder_ZeroingBriefing_Range", new Vector2(1390, 100), new Vector2(1840, 790), new Color32(10, 21, 18, 120), new Color32(73, 107, 90, 255), "素材占位：100m室外靶场背景");
            AddLabel(parent, "Text_ZeroingBriefing_Header", "任务简报  MISSION BRIEFING", 26, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(180, 95), new Vector2(780, 145), new Color32(231, 242, 235, 255));
            AddLabel(parent, "Text_ZeroingBriefing_Title", "100m精度射校靶", 54, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(190, 190), new Vector2(760, 280), new Color32(231, 242, 235, 255));

            AddPanel(parent, "Panel_ZeroingBriefing_Rules", new Vector2(190, 305), new Vector2(740, 745), new Color32(17, 29, 24, 225), new Color32(56, 84, 71, 255),
                "射击距离：100m\n射击模式：单发射\n每轮弹数：3发\n训练轮次：共3轮\n目标规格：50cm x 50cm 胸靶\n10环直径：10cm\n通过条件：3发全部进入10环");

            AddPanel(parent, "Panel_ZeroingBriefing_Target", new Vector2(840, 210), new Vector2(1325, 745), new Color32(10, 21, 18, 210), new Color32(45, 156, 255, 255), string.Empty);
            AddLabel(parent, "Text_ZeroingBriefing_TargetTitle", "胸靶示意图", 26, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(900, 230), new Vector2(1265, 285), new Color32(77, 213, 255, 255));
            BuildTargetDiagram(parent);

            startButton = AddButton(parent, "Button_ZeroingBriefing_Start", "开始射击", new Vector2(510, 850), new Vector2(820, 925), true);
            startButton.onClick.AddListener(OnStartZeroingClicked);

            backButton = AddButton(parent, "Button_ZeroingBriefing_Back", "返回", new Vector2(870, 850), new Vector2(1180, 925), false);
            backButton.onClick.AddListener(OnBackClicked);

            AddPanel(parent, "Panel_ZeroingBriefing_SafetyNote", new Vector2(105, 980), new Vector2(900, 1030), new Color32(17, 29, 24, 230), new Color32(56, 84, 71, 255),
                "请确认武器处于安全状态，佩戴护具，听从指挥。");
            AddPanel(parent, "Panel_ZeroingBriefing_Status", new Vector2(960, 980), new Vector2(1815, 1030), new Color32(17, 29, 24, 230), new Color32(56, 84, 71, 255),
                "当前轮次  1 / 3        历史最佳  0环        预计用时  00:10:00");
        }

        void BuildTargetDiagram(RectTransform parent)
        {
            var center = new Vector2(1082f, 505f);
            var sizes = new[] { 315f, 250f, 190f, 130f, 70f };
            for (var i = 0; i < sizes.Length; i++)
            {
                var ring = CreateRect("Image_ZeroingBriefing_TargetRing_" + i, parent, Vector2.zero, Vector2.zero, center - new Vector2(sizes[i] * 0.5f, sizes[i] * 0.5f), center + new Vector2(sizes[i] * 0.5f, sizes[i] * 0.5f));
                AddImage(ring.gameObject, i == sizes.Length - 1 ? new Color32(231, 242, 235, 255) : new Color32(0, 0, 0, 0));
                var outline = ring.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(231, 242, 235, 230);
                outline.effectDistance = new Vector2(2f, 2f);
            }

            AddLabel(parent, "Text_ZeroingBriefing_TargetCenter", "10环", 22, FontStyles.Bold, TextAlignmentOptions.Center, center - new Vector2(60, 28), center + new Vector2(60, 28), new Color32(7, 16, 13, 255));
            AddLabel(parent, "Text_ZeroingBriefing_TargetSize", "50cm x 50cm", 20, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(930, 700), new Vector2(1230, 740), new Color32(77, 213, 255, 255));
        }

        void OnOpenZeroingClicked()
        {
            var result = services.Router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);
            if (!result.Success)
            {
                LastError = result.Message;
            }
        }

        void OnStartZeroingClicked()
        {
            LastError = string.Empty;

            var session = services.TrainingSessions.HasActiveSession
                ? ServiceResult<TrainingSessionDto>.Ok(services.TrainingSessions.Current)
                : services.TrainingSessions.Create(
                    TrainingMode.Zeroing100m,
                    ZeroingMapId,
                    ZeroingWeaponId,
                    RandomSeed.Fixed(100));

            if (!session.Success)
            {
                LastError = session.Message;
                return;
            }

            var start = services.TrainingSessions.Start(session.Data.SessionId);
            if (!start.Success)
            {
                LastError = start.Message;
                return;
            }

            var route = services.Router.HandleUIEvent(UIEventId.Zeroing_Start, ScreenId.ZeroingBriefing, new NavigationArgs
            {
                Mode = TrainingMode.Zeroing100m,
                SessionId = start.Data.SessionId,
                ReturnToScreen = ScreenId.ZeroingBriefing.ToString()
            });

            if (!route.Success)
            {
                LastError = route.Message;
            }
        }

        void OnBackClicked()
        {
            var result = services.Router.HandleUIEvent(UIEventId.Common_Back, ScreenId.ZeroingBriefing);
            if (!result.Success)
            {
                LastError = result.Message;
            }
        }

        void ShowScreen(ScreenId screen)
        {
            ActiveScreen = screen;
            if (mainMenuScreen != null)
            {
                mainMenuScreen.gameObject.SetActive(screen == ScreenId.MainMenu);
            }

            if (zeroingBriefingScreen != null)
            {
                zeroingBriefingScreen.gameObject.SetActive(screen == ScreenId.ZeroingBriefing);
            }
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        static Image AddImage(GameObject go, Color color)
        {
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.color = color;
            return image;
        }

        static void AddTestId(GameObject go, string id)
        {
            var testId = go.GetComponent<UITestId>();
            if (testId == null)
            {
                testId = go.AddComponent<UITestId>();
            }

            testId.SetId(id);
        }

        static RectTransform AddPanel(RectTransform parent, string id, Vector2 min, Vector2 max, Color fill, Color stroke, string text = "")
        {
            var panel = CreateRect(id, parent, Vector2.zero, Vector2.zero, min, max);
            AddImage(panel.gameObject, fill);
            AddTestId(panel.gameObject, id);

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = stroke;
            outline.effectDistance = new Vector2(2f, -2f);

            if (!string.IsNullOrEmpty(text))
            {
                var textId = id.StartsWith("Panel_", StringComparison.Ordinal)
                    ? id.Replace("Panel_", "Text_")
                    : "Text_" + id;
                AddLabel(panel, textId, text, 20, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(20, 18), new Vector2(-20, -18), new Color32(231, 242, 235, 255), true);
            }

            return panel;
        }

        static TextMeshProUGUI AddLabel(RectTransform parent, string id, string text, float size, FontStyles style, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color color, bool stretchOffsets = false)
        {
            var label = CreateRect(id, parent, stretchOffsets ? Vector2.zero : Vector2.zero, stretchOffsets ? Vector2.one : Vector2.zero, min, max);
            AddTestId(label.gameObject, id);
            var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            var font = GetUIFont();
            if (font != null)
            {
                tmp.font = font;
            }

            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        static TMP_FontAsset GetUIFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

            var font = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "Noto Sans CJK SC",
                "Arial Unicode MS"
            }, 90);

            if (font == null)
            {
                return null;
            }

            uiFont = TMP_FontAsset.CreateFontAsset(font);
            if (uiFont != null)
            {
                uiFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }

            return uiFont;
        }

        static Button AddButton(RectTransform parent, string id, string label, Vector2 min, Vector2 max, bool primary)
        {
            var rect = CreateRect(id, parent, Vector2.zero, Vector2.zero, min, max);
            AddTestId(rect.gameObject, id);
            var image = AddImage(rect.gameObject, primary ? new Color32(200, 255, 106, 235) : new Color32(20, 37, 31, 235));
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = primary ? new Color32(247, 185, 85, 255) : new Color32(107, 138, 119, 255);
            outline.effectDistance = new Vector2(2f, -2f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = primary ? new Color32(220, 255, 150, 255) : new Color32(35, 62, 54, 255);
            colors.pressedColor = primary ? new Color32(170, 220, 70, 255) : new Color32(10, 24, 20, 255);
            button.colors = colors;

            AddLabel(rect, id.Replace("Button_", "Text_"), label, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(8, 8), new Vector2(-8, -8), primary ? new Color32(7, 16, 13, 255) : new Color32(231, 242, 235, 255), true);
            return button;
        }

        static void DisableButton(Button button)
        {
            button.interactable = false;
            var colors = button.colors;
            colors.disabledColor = new Color32(20, 37, 31, 140);
            button.colors = colors;
        }

        static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
