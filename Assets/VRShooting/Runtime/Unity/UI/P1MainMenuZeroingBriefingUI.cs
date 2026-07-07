using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
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

        [SerializeField]
        TMP_FontAsset fontAsset;

        ApplicationServices services;
        IDisposable screenSubscription;
        RectTransform mainMenuScreen;
        RectTransform zeroingBriefingScreen;
        RectTransform zeroingHudScreen;
        RectTransform zeroingStabilityFill;
        Button openZeroingButton;
        Button startButton;
        Button backButton;
        TextMeshProUGUI zeroingRoundText;
        TextMeshProUGUI zeroingDistanceText;
        TextMeshProUGUI zeroingAmmoText;
        TextMeshProUGUI zeroingStabilityText;
        TextMeshProUGUI zeroingImpactRecordText;
        TextMeshProUGUI zeroingShoulderText;
        TextMeshProUGUI zeroingPromptText;
        Image zeroingPromptBackground;
#if UNITY_EDITOR
        static TMP_FontAsset generatedEditorFontAsset;
#endif

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
            if (services?.Hud != null)
            {
                services.Hud.HudUpdated -= RenderHud;
            }
        }

        public void Initialize(ApplicationServices applicationServices)
        {
            services = applicationServices ?? ApplicationServices.CreateDefault();
            LastError = string.Empty;

            ClearChildren(transform);
            BuildCanvas();
            SubscribeRouter();
            SubscribeHud();
            ShowScreen(services.Router.Current);
        }

        void SubscribeRouter()
        {
            screenSubscription?.Dispose();
            screenSubscription = services.EventBus.Subscribe<ScreenChangedEvent>(evt => ShowScreen(evt.CurrentScreen));
        }

        void SubscribeHud()
        {
            services.Hud.HudUpdated -= RenderHud;
            services.Hud.HudUpdated += RenderHud;
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

            zeroingHudScreen = CreateHudScreen("Screen_ZeroingHud");
            BuildZeroingHud(zeroingHudScreen);
        }

        RectTransform CreateScreen(string name)
        {
            var screen = CreateRect(name, transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(screen.gameObject, new Color32(7, 16, 13, 242));
            AddTestId(screen.gameObject, name);
            return screen;
        }

        RectTransform CreateHudScreen(string name)
        {
            var screen = CreateRect(name, transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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

        void BuildZeroingHud(RectTransform parent)
        {
            AddPanel(parent, "Panel_ZeroingHud_Round", new Vector2(60, 60), new Vector2(350, 175), new Color32(12, 28, 36, 205), new Color32(45, 156, 255, 255));
            AddLabel(parent, "Text_ZeroingHud_RoundIcon", "◎", 46, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(85, 82), new Vector2(150, 150), new Color32(143, 217, 255, 255));
            zeroingRoundText = AddLabel(parent, "Text_ZeroingHud_Round", "轮次 1/3", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(150, 78), new Vector2(325, 155), new Color32(231, 242, 235, 255));

            AddPanel(parent, "Panel_ZeroingHud_Distance", new Vector2(810, 75), new Vector2(1110, 155), new Color32(12, 28, 36, 205), new Color32(45, 156, 255, 255));
            zeroingDistanceText = AddLabel(parent, "Text_ZeroingHud_Distance", "距离 100m", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(835, 88), new Vector2(1085, 145), new Color32(143, 217, 255, 255));

            AddPanel(parent, "Panel_ZeroingHud_Ammo", new Vector2(1530, 60), new Vector2(1810, 175), new Color32(12, 28, 36, 205), new Color32(45, 156, 255, 255));
            zeroingAmmoText = AddLabel(parent, "Text_ZeroingHud_Ammo", "弹数 3/3", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(1555, 78), new Vector2(1730, 155), new Color32(231, 242, 235, 255));
            AddLabel(parent, "Text_ZeroingHud_AmmoIcon", "|||", 36, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(1725, 82), new Vector2(1792, 150), new Color32(143, 217, 255, 255));

            var stabilityPanel = AddPanel(parent, "Hud_Zeroing_Stability", new Vector2(80, 785), new Vector2(555, 905), new Color32(12, 28, 36, 200), new Color32(45, 156, 255, 255));
            AddLabel(stabilityPanel, "Text_ZeroingHud_StabilityLabel", "稳定度", 24, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(28, 14), new Vector2(-28, -70), new Color32(231, 242, 235, 255), true);
            zeroingStabilityText = AddLabel(stabilityPanel, "Text_ZeroingHud_StabilityValue", "100%", 20, FontStyles.Bold, TextAlignmentOptions.Right, new Vector2(28, 14), new Vector2(-28, -70), new Color32(200, 255, 106, 255), true);
            var stabilityBar = CreateRect("Hud_Zeroing_StabilityBar", stabilityPanel, Vector2.zero, Vector2.zero, new Vector2(28, 70), new Vector2(420, 92));
            AddTestId(stabilityBar.gameObject, "Hud_Zeroing_StabilityBar");
            AddImage(stabilityBar.gameObject, new Color32(33, 64, 75, 230));
            zeroingStabilityFill = CreateRect("Hud_Zeroing_StabilityFill", stabilityBar, Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            AddTestId(zeroingStabilityFill.gameObject, "Hud_Zeroing_StabilityFill");
            AddImage(zeroingStabilityFill.gameObject, new Color32(89, 224, 255, 245));

            var recordPanel = AddPanel(parent, "Hud_Zeroing_ImpactRecord", new Vector2(1430, 300), new Vector2(1840, 845), new Color32(12, 28, 36, 190), new Color32(45, 156, 255, 255));
            AddLabel(recordPanel, "Text_ZeroingHud_ImpactTitle", "弹着记录", 30, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(35, 25), new Vector2(-35, -465), new Color32(143, 217, 255, 255), true);
            BuildHudTargetPlaceholder(recordPanel);
            zeroingImpactRecordText = AddLabel(recordPanel, "Text_ZeroingHud_ImpactRecord", "待记录 3 发", 26, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(35, 430), new Vector2(-35, -28), new Color32(77, 213, 255, 255), true);

            AddPanel(parent, "Panel_ZeroingHud_Shoulder", new Vector2(80, 925), new Vector2(390, 995), new Color32(12, 28, 36, 190), new Color32(56, 84, 71, 255));
            zeroingShoulderText = AddLabel(parent, "Text_ZeroingHud_Shoulder", "肩侧 右肩", 24, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(95, 936), new Vector2(375, 985), new Color32(231, 242, 235, 255));

            var promptPanel = AddPanel(parent, "Panel_ZeroingHud_Prompt", new Vector2(760, 880), new Vector2(1160, 960), new Color32(200, 255, 106, 230), new Color32(247, 185, 85, 255));
            zeroingPromptBackground = promptPanel.GetComponent<Image>();
            zeroingPromptText = AddLabel(promptPanel, "Text_ZeroingHud_Prompt", "稳定据枪", 42, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(18, 10), new Vector2(-18, -10), new Color32(7, 16, 13, 255), true);
        }

        void BuildHudTargetPlaceholder(RectTransform parent)
        {
            var target = CreateRect("Placeholder_ZeroingHud_TargetPaper", parent, Vector2.zero, Vector2.zero, new Vector2(65, 105), new Vector2(345, 395));
            AddTestId(target.gameObject, "Placeholder_ZeroingHud_TargetPaper");
            AddImage(target.gameObject, new Color32(7, 16, 13, 145));
            var outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(231, 242, 235, 180);
            outline.effectDistance = new Vector2(2f, -2f);

            var center = new Vector2(140f, 145f);
            var sizes = new[] { 210f, 165f, 120f, 78f, 38f };
            for (var i = 0; i < sizes.Length; i++)
            {
                var ring = CreateRect("Image_ZeroingHud_TargetRing_" + i, target, Vector2.zero, Vector2.zero, center - new Vector2(sizes[i] * 0.5f, sizes[i] * 0.5f), center + new Vector2(sizes[i] * 0.5f, sizes[i] * 0.5f));
                AddImage(ring.gameObject, i == sizes.Length - 1 ? new Color32(231, 242, 235, 230) : new Color32(0, 0, 0, 0));
                var ringOutline = ring.gameObject.AddComponent<Outline>();
                ringOutline.effectColor = new Color32(231, 242, 235, 180);
                ringOutline.effectDistance = new Vector2(1.5f, 1.5f);
            }
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

            var weapon = services.WeaponControl.StartSession(start.Data.SessionId, start.Data.WeaponId, start.Data.Mode);
            if (!weapon.Success)
            {
                LastError = weapon.Message;
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

            if (zeroingHudScreen != null)
            {
                zeroingHudScreen.gameObject.SetActive(screen == ScreenId.ZeroingHud);
            }

            if (screen == ScreenId.ZeroingHud)
            {
                RefreshHud();
            }
        }

        void RefreshHud()
        {
            if (services == null || !services.TrainingSessions.HasActiveSession)
            {
                return;
            }

            var result = services.Hud.GetHud(services.TrainingSessions.Current.SessionId);
            if (result.Success)
            {
                RenderHud(result.Data);
            }
        }

        void RenderHud(HudDto hud)
        {
            if (hud.HudType != HudType.Zeroing)
            {
                return;
            }

            var round = FindLine(hud, "round");
            var distance = FindLine(hud, "distance");
            var ammo = FindLine(hud, "ammo");
            var stability = FindLine(hud, "stability");
            var impactRecord = FindLine(hud, "impactRecord");
            var shoulder = FindLine(hud, "shoulder");

            SetLabel(zeroingRoundText, round, "轮次 1/3");
            SetLabel(zeroingDistanceText, distance, "距离 100m");
            SetLabel(zeroingAmmoText, ammo, "弹数 3/3");
            SetLabel(zeroingStabilityText, stability, "100%", false);
            SetLabel(zeroingImpactRecordText, impactRecord, "待记录 3 发", false);
            SetLabel(zeroingShoulderText, shoulder, "肩侧 右肩");

            var ratio = ParsePercent01(stability.Value);
            if (zeroingStabilityFill != null)
            {
                zeroingStabilityFill.anchorMax = new Vector2(ratio, 1f);
            }

            var prompt = hud.Prompts != null && hud.Prompts.Count > 0 ? hud.Prompts[0].Text : (hud.CanShoot ? "稳定据枪" : "禁止射击");
            if (zeroingPromptText != null)
            {
                zeroingPromptText.text = prompt;
                zeroingPromptText.color = hud.CanShoot ? new Color32(7, 16, 13, 255) : new Color32(255, 225, 180, 255);
            }

            if (zeroingPromptBackground != null)
            {
                zeroingPromptBackground.color = hud.CanShoot
                    ? new Color32(200, 255, 106, 230)
                    : new Color32(50, 28, 24, 230);
            }
        }

        static HudTextLineDto FindLine(HudDto hud, string key)
        {
            if (hud.TextLines == null)
            {
                return default;
            }

            for (var i = 0; i < hud.TextLines.Count; i++)
            {
                if (hud.TextLines[i].Key == key)
                {
                    return hud.TextLines[i];
                }
            }

            return default;
        }

        static void SetLabel(TextMeshProUGUI label, HudTextLineDto line, string fallback, bool includeLabel = true)
        {
            if (label == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(line.Key))
            {
                label.text = fallback;
                return;
            }

            label.text = includeLabel ? line.Label + " " + line.Value : line.Value;
        }

        static float ParsePercent01(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 1f;
            }

            var normalized = value.Trim().TrimEnd('%');
            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                return 1f;
            }

            return Mathf.Clamp01(percent / 100f);
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

        RectTransform AddPanel(RectTransform parent, string id, Vector2 min, Vector2 max, Color fill, Color stroke, string text = "")
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

        TextMeshProUGUI AddLabel(RectTransform parent, string id, string text, float size, FontStyles style, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color color, bool stretchOffsets = false)
        {
            var label = CreateRect(id, parent, stretchOffsets ? Vector2.zero : Vector2.zero, stretchOffsets ? Vector2.one : Vector2.zero, min, max);
            AddTestId(label.gameObject, id);
            var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            var resolvedFont = ResolveFontAsset();
            if (resolvedFont != null)
            {
                tmp.font = resolvedFont;
            }

            tmp.text = text;
            tmp.fontSize = size;
            // Synthetic bold on dense CJK SDF atlases can create blocky white halos.
            tmp.fontStyle = style & ~FontStyles.Bold;
            tmp.fontWeight = FontWeight.Regular;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.extraPadding = true;
            ResetTextMaterialEffects(tmp);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        static void ResetTextMaterialEffects(TextMeshProUGUI tmp)
        {
            var sourceMaterial = tmp.fontSharedMaterial;
            if (sourceMaterial == null)
            {
                return;
            }

            var material = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + " P1Clean"
            };

            if (material.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlayDilate))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            }

            material.DisableKeyword(ShaderUtilities.Keyword_Outline);
            material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            tmp.fontSharedMaterial = material;
            tmp.outlineWidth = 0f;
        }

        TMP_FontAsset ResolveFontAsset()
        {
#if UNITY_EDITOR
            if (fontAsset == null)
            {
                fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/VRShooting/Art/Fonts/MSYH SDF.asset");
            }

            if (fontAsset == null || fontAsset.atlasPadding < 4)
            {
                var generatedFont = ResolveGeneratedEditorFontAsset();
                if (generatedFont != null)
                {
                    return generatedFont;
                }
            }
#endif

            if (fontAsset != null)
            {
                return fontAsset;
            }

            return null;
        }

#if UNITY_EDITOR
        static TMP_FontAsset ResolveGeneratedEditorFontAsset()
        {
            if (generatedEditorFontAsset != null)
            {
                return generatedEditorFontAsset;
            }

            var sourceFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/VRShooting/Art/Fonts/MSYH.TTC");
            if (sourceFont == null)
            {
                sourceFont = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "SimHei",
                    "Noto Sans CJK SC",
                    "Arial Unicode MS"
                }, 90);
            }

            if (sourceFont == null)
            {
                return null;
            }

            generatedEditorFontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                4096,
                4096,
                AtlasPopulationMode.Dynamic,
                true);
            generatedEditorFontAsset.name = "MSYH Runtime SDF";
            generatedEditorFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            return generatedEditorFontAsset;
        }
#endif

        Button AddButton(RectTransform parent, string id, string label, Vector2 min, Vector2 max, bool primary)
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
