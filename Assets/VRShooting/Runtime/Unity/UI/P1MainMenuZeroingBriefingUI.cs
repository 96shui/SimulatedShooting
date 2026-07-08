using System;
using System.Collections.Generic;
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

        readonly Dictionary<string, Sprite> generatedSpriteCache = new Dictionary<string, Sprite>();

        ApplicationServices services;
        IDisposable screenSubscription;
        IDisposable zeroingRoundSubscription;
        RectTransform mainMenuScreen;
        RectTransform zeroingBriefingScreen;
        RectTransform zeroingHudScreen;
        RectTransform zeroingImpactAnalysisScreen;
        RectTransform zeroingStabilityFill;
        readonly List<RectTransform> zeroingImpactDots = new List<RectTransform>();
        Button openZeroingButton;
        Button startButton;
        Button backButton;
        Button applyAdjustmentButton;
        Button nextRoundButton;
        TextMeshProUGUI zeroingRoundText;
        TextMeshProUGUI zeroingDistanceText;
        TextMeshProUGUI zeroingAmmoText;
        TextMeshProUGUI zeroingStabilityText;
        TextMeshProUGUI zeroingImpactRecordText;
        TextMeshProUGUI zeroingShoulderText;
        TextMeshProUGUI zeroingPromptText;
        TextMeshProUGUI zeroingAnalysisVerticalText;
        TextMeshProUGUI zeroingAnalysisHorizontalText;
        TextMeshProUGUI zeroingAnalysisFrontSightText;
        TextMeshProUGUI zeroingAnalysisRearSightText;
        TextMeshProUGUI zeroingAnalysisSuggestionText;
        TextMeshProUGUI zeroingAnalysisAppliedText;
        Image zeroingPromptBackground;
#if UNITY_EDITOR
        static TMP_FontAsset generatedEditorFontAsset;
#endif

        public ScreenId ActiveScreen { get; private set; } = ScreenId.MainMenu;

        public string LastError { get; private set; } = string.Empty;

        public Button OpenZeroingButton => openZeroingButton;

        public Button StartButton => startButton;

        public Button BackButton => backButton;

        public bool IsInitialized => services != null;

        void Awake()
        {
            if (!P1PersistentUIHost.TryAdoptUi(this))
            {
                return;
            }

            if (!buildOnAwake)
            {
                return;
            }

            TryInitializeFromGameMain();
        }

        void Start()
        {
            if (!buildOnAwake || services != null)
            {
                return;
            }

            TryInitializeFromGameMain();
        }

        void TryInitializeFromGameMain()
        {
            if (services != null)
            {
                return;
            }

            var gameMain = GameMain.Instance;
            if (gameMain == null || gameMain.Services == null)
            {
                return;
            }

            Initialize(gameMain.Services);
        }

        void OnDestroy()
        {
            screenSubscription?.Dispose();
            zeroingRoundSubscription?.Dispose();
            if (services?.Hud != null)
            {
                services.Hud.HudUpdated -= RenderHud;
            }
        }

        public void Initialize(ApplicationServices applicationServices)
        {
            services = applicationServices ?? ApplicationServices.CreateDefault();
            LastError = string.Empty;

            if (mainMenuScreen != null)
            {
                ShowScreen(services.Router.Current);
                return;
            }

            ClearChildren(transform);
            BuildCanvas();
            SubscribeRouter();
            SubscribeHud();
            SubscribeZeroing();
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

        void SubscribeZeroing()
        {
            zeroingRoundSubscription?.Dispose();
            zeroingRoundSubscription = services.EventBus.Subscribe<ZeroingRoundCompletedEvent>(OnZeroingRoundCompleted);
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

            zeroingImpactAnalysisScreen = CreateHudScreen("Screen_ZeroingImpactAnalysis");
            BuildZeroingImpactAnalysis(zeroingImpactAnalysisScreen);
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

        Sprite LoadGeneratedSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            if (generatedSpriteCache.TryGetValue(spriteName, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>("UI/P1Generated/Sprites/" + spriteName);
            generatedSpriteCache[spriteName] = sprite;
            return sprite;
        }

        static void ApplySlicedSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
            image.pixelsPerUnitMultiplier = 1f;
        }

        Sprite ResolvePanelSprite(string id, Vector2 min, Vector2 max)
        {
            var width = Mathf.Abs(max.x - min.x);
            var height = Mathf.Abs(max.y - min.y);

            if (id.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Bottom", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LoadGeneratedSprite(width > 700f ? "status_strip_long" : "status_strip_short");
            }

            if (id.IndexOf("Prompt", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LoadGeneratedSprite("button_primary_orange");
            }

            if (height <= 130f)
            {
                return LoadGeneratedSprite(width > 520f ? "status_strip_long" : "hud_capsule");
            }

            return LoadGeneratedSprite(width > 700f || height > 260f ? "panel_holo_large" : "panel_holo_medium");
        }

        Image AddIcon(RectTransform parent, string id, string spriteName, Vector2 min, Vector2 max)
        {
            var rect = CreateRect(id, parent, Vector2.zero, Vector2.zero, min, max);
            AddTestId(rect.gameObject, id);
            var image = AddImage(rect.gameObject, Color.white);
            var sprite = LoadGeneratedSprite(spriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            return image;
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
            AddIcon(parent, "Icon_ZeroingHud_Round", "icon_reticle", new Vector2(82, 80), new Vector2(152, 150));
            zeroingRoundText = AddLabel(parent, "Text_ZeroingHud_Round", "轮次 1/3", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(150, 78), new Vector2(325, 155), new Color32(231, 242, 235, 255));

            AddPanel(parent, "Panel_ZeroingHud_Distance", new Vector2(810, 75), new Vector2(1110, 155), new Color32(12, 28, 36, 205), new Color32(45, 156, 255, 255));
            zeroingDistanceText = AddLabel(parent, "Text_ZeroingHud_Distance", "距离 100m", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(835, 88), new Vector2(1085, 145), new Color32(143, 217, 255, 255));

            AddPanel(parent, "Panel_ZeroingHud_Ammo", new Vector2(1530, 60), new Vector2(1810, 175), new Color32(12, 28, 36, 205), new Color32(45, 156, 255, 255));
            zeroingAmmoText = AddLabel(parent, "Text_ZeroingHud_Ammo", "弹数 3/3", 30, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(1555, 78), new Vector2(1730, 155), new Color32(231, 242, 235, 255));
            AddIcon(parent, "Icon_ZeroingHud_Ammo", "icon_bullets", new Vector2(1725, 82), new Vector2(1792, 150));

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

        void BuildZeroingImpactAnalysis(RectTransform parent)
        {
            AddPanel(parent, "Placeholder_ZeroingImpactAnalysis_BlurredRange", DrawioMin(35, 45, 730, 500), DrawioMax(35, 45, 730, 500), new Color32(10, 21, 18, 165), new Color32(73, 107, 90, 255), "素材占位：虚化靶场射击背景");
            AddPanel(parent, "Panel_ZeroingImpactAnalysis_Modal", DrawioMin(160, 85, 480, 390), DrawioMax(160, 85, 480, 390), new Color32(11, 19, 16, 235), new Color32(45, 156, 255, 255));
            AddLabel(parent, "Text_ZeroingImpactAnalysis_Title", "本轮弹着分析", 40, FontStyles.Bold, TextAlignmentOptions.Center, DrawioMin(210, 100, 380, 45), DrawioMax(210, 100, 380, 45), new Color32(231, 242, 235, 255));

            var target = AddPanel(parent, "Placeholder_ZeroingImpactAnalysis_Target", DrawioMin(205, 160, 170, 210), DrawioMax(205, 160, 170, 210), new Color32(17, 29, 24, 225), new Color32(200, 255, 106, 255));
            AddLabel(target, "Text_ZeroingImpactAnalysis_TargetTitle", "50cm x 50cm 胸靶", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(35, 325), new Vector2(373, 365), new Color32(77, 213, 255, 255));
            BuildAnalysisTargetDiagram(target);

            var data = AddPanel(parent, "Panel_ZeroingImpactAnalysis_Data", DrawioMin(405, 155, 190, 230), DrawioMax(405, 155, 190, 230), new Color32(17, 29, 24, 225), new Color32(56, 84, 71, 255));
            zeroingAnalysisVerticalText = AddLabel(data, "Text_ZeroingImpactAnalysis_VerticalOffset", "垂直偏差：--", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(28, 330), new Vector2(428, 370), new Color32(231, 242, 235, 255));
            zeroingAnalysisHorizontalText = AddLabel(data, "Text_ZeroingImpactAnalysis_HorizontalOffset", "水平偏差：--", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(28, 270), new Vector2(428, 310), new Color32(231, 242, 235, 255));
            zeroingAnalysisFrontSightText = AddLabel(data, "Text_ZeroingImpactAnalysis_FrontSight", "准星柱：--", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(28, 210), new Vector2(428, 250), new Color32(247, 185, 85, 255));
            zeroingAnalysisRearSightText = AddLabel(data, "Text_ZeroingImpactAnalysis_RearSight", "觇孔：--", 22, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(28, 150), new Vector2(428, 190), new Color32(247, 185, 85, 255));
            zeroingAnalysisSuggestionText = AddLabel(data, "Text_ZeroingImpactAnalysis_Suggestion", "说明：应用后进入下一轮", 18, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(28, 45), new Vector2(428, 120), new Color32(143, 217, 255, 255));

            zeroingAnalysisAppliedText = AddLabel(parent, "Text_ZeroingImpactAnalysis_AppliedState", "等待应用调整", 20, FontStyles.Bold, TextAlignmentOptions.Center, DrawioMin(285, 382, 245, 28), DrawioMax(285, 382, 245, 28), new Color32(143, 217, 255, 255));
            applyAdjustmentButton = AddButton(parent, "Button_ZeroingImpactAnalysis_ApplyAdjustment", "应用调整", DrawioMin(280, 415, 110, 38), DrawioMax(280, 415, 110, 38), true);
            applyAdjustmentButton.onClick.AddListener(OnApplyAdjustmentClicked);
            nextRoundButton = AddButton(parent, "Button_ZeroingImpactAnalysis_NextRound", "进入下一轮", DrawioMin(415, 415, 120, 38), DrawioMax(415, 415, 120, 38), false);
            nextRoundButton.onClick.AddListener(OnNextRoundClicked);
        }

        void BuildAnalysisTargetDiagram(RectTransform parent)
        {
            var center = new Vector2(204f, 190f);
            var sizes = new[] { 255f, 205f, 155f, 105f, 56f };
            for (var i = 0; i < sizes.Length; i++)
            {
                var ring = CreateRect("Image_ZeroingImpactAnalysis_TargetRing_" + i, parent, Vector2.zero, Vector2.zero, center - new Vector2(sizes[i] * 0.5f, sizes[i] * 0.5f), center + new Vector2(sizes[i] * 0.5f, sizes[i] * 0.5f));
                AddImage(ring.gameObject, i == sizes.Length - 1 ? new Color32(231, 242, 235, 230) : new Color32(0, 0, 0, 0));
                var outline = ring.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(231, 242, 235, 190);
                outline.effectDistance = new Vector2(1.5f, 1.5f);
            }

            AddLabel(parent, "Text_ZeroingImpactAnalysis_TargetCenter", "10环", 18, FontStyles.Bold, TextAlignmentOptions.Center, center - new Vector2(50, 24), center + new Vector2(50, 24), new Color32(7, 16, 13, 255));
            zeroingImpactDots.Clear();
            for (var i = 0; i < 3; i++)
            {
                var dot = CreateRect("Image_ZeroingImpactAnalysis_Impact_" + (i + 1), parent, Vector2.zero, Vector2.zero, center - new Vector2(9, 9), center + new Vector2(9, 9));
                AddTestId(dot.gameObject, dot.gameObject.name);
                AddImage(dot.gameObject, new Color32(255, 92, 68, 255));
                var outline = dot.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(255, 225, 160, 255);
                outline.effectDistance = new Vector2(2f, -2f);
                zeroingImpactDots.Add(dot);
            }
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

            var zeroingSession = services.Zeroing.StartSession(RandomSeed.Fixed(100), ZeroingWeaponId);
            if (!zeroingSession.Success)
            {
                LastError = zeroingSession.Message;
                return;
            }

            var trainingSession = services.TrainingSessions.Current;
            var weapon = services.WeaponControl.StartSession(trainingSession.SessionId, trainingSession.WeaponId, trainingSession.Mode);
            if (!weapon.Success)
            {
                LastError = weapon.Message;
                return;
            }

            var route = services.Router.HandleUIEvent(UIEventId.Zeroing_Start, ScreenId.ZeroingBriefing, new NavigationArgs
            {
                Mode = TrainingMode.Zeroing100m,
                SessionId = zeroingSession.Data.SessionId,
                ReturnToScreen = ScreenId.ZeroingBriefing.ToString()
            });

            if (!route.Success)
            {
                LastError = route.Message;
                return;
            }

            var gameMain = GameMain.Instance;
            if (gameMain?.GameState != null)
            {
                gameMain.GameState.ChangeState(GameState.InGame);
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

            if (zeroingImpactAnalysisScreen != null)
            {
                zeroingImpactAnalysisScreen.gameObject.SetActive(screen == ScreenId.ZeroingImpactAnalysis);
            }

            if (screen == ScreenId.ZeroingHud)
            {
                RefreshHud();
            }

            if (screen == ScreenId.ZeroingImpactAnalysis)
            {
                RefreshImpactAnalysis();
            }
        }

        void OnZeroingRoundCompleted(ZeroingRoundCompletedEvent evt)
        {
            if (services == null || !services.TrainingSessions.HasActiveSession || services.TrainingSessions.Current.SessionId != evt.SessionId)
            {
                return;
            }

            if (services.Router.Current != ScreenId.ZeroingHud)
            {
                return;
            }

            var route = services.Router.Open(ScreenId.ZeroingImpactAnalysis, new NavigationArgs
            {
                Mode = TrainingMode.Zeroing100m,
                SessionId = evt.SessionId,
                ReturnToScreen = ScreenId.ZeroingHud.ToString()
            });
            if (!route.Success)
            {
                LastError = route.Message;
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
                zeroingPromptBackground.color = zeroingPromptBackground.sprite != null
                    ? (hud.CanShoot ? Color.white : new Color32(255, 120, 92, 255))
                    : (hud.CanShoot ? new Color32(200, 255, 106, 230) : new Color32(50, 28, 24, 230));
            }
        }

        void RefreshImpactAnalysis()
        {
            if (services == null || !services.TrainingSessions.HasActiveSession)
            {
                return;
            }

            var result = services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId);
            if (!result.Success)
            {
                LastError = result.Message;
                return;
            }

            RenderImpactAnalysis(result.Data);
        }

        void RenderImpactAnalysis(ZeroingRoundAnalysisDto analysis)
        {
            SetAnalysisText(zeroingAnalysisVerticalText, "垂直偏差", FormatVerticalOffset(analysis.AverageOffsetCm.y));
            SetAnalysisText(zeroingAnalysisHorizontalText, "水平偏差", FormatHorizontalOffset(analysis.AverageOffsetCm.x));
            SetAnalysisText(zeroingAnalysisFrontSightText, "准星柱", FormatFrontSight(analysis));
            SetAnalysisText(zeroingAnalysisRearSightText, "觇孔", FormatRearSight(analysis));

            if (zeroingAnalysisSuggestionText != null)
            {
                zeroingAnalysisSuggestionText.text = "说明：" + FormatSuggestion(analysis);
            }

            if (zeroingAnalysisAppliedText != null)
            {
                zeroingAnalysisAppliedText.text = analysis.AdjustmentApplied ? "调整已应用" : "等待应用调整";
                zeroingAnalysisAppliedText.color = analysis.AdjustmentApplied
                    ? new Color32(200, 255, 106, 255)
                    : new Color32(143, 217, 255, 255);
            }

            if (applyAdjustmentButton != null)
            {
                applyAdjustmentButton.interactable = !analysis.AdjustmentApplied;
                var label = applyAdjustmentButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = analysis.AdjustmentApplied ? "已应用" : "应用调整";
                }
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.interactable = analysis.AdjustmentApplied;
            }

            RenderImpactDots(analysis);
        }

        void RenderImpactDots(ZeroingRoundAnalysisDto analysis)
        {
            var center = new Vector2(204f, 190f);
            const float pixelsPerCm = 5.1f;
            for (var i = 0; i < zeroingImpactDots.Count; i++)
            {
                var visible = analysis.Shots != null && i < analysis.Shots.Count;
                zeroingImpactDots[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var impact = analysis.Shots[i].ImpactPointCm;
                var clamped = new Vector2(Mathf.Clamp(impact.x, -25f, 25f), Mathf.Clamp(impact.y, -25f, 25f));
                var pos = center + new Vector2(clamped.x * pixelsPerCm, clamped.y * pixelsPerCm);
                zeroingImpactDots[i].offsetMin = pos - new Vector2(9f, 9f);
                zeroingImpactDots[i].offsetMax = pos + new Vector2(9f, 9f);
            }
        }

        void OnApplyAdjustmentClicked()
        {
            if (!services.TrainingSessions.HasActiveSession)
            {
                return;
            }

            var analysis = services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId);
            if (!analysis.Success)
            {
                LastError = analysis.Message;
                return;
            }

            var applied = services.Zeroing.ApplyAdjustment(analysis.Data.SessionId, analysis.Data.RoundIndex);
            if (!applied.Success)
            {
                LastError = applied.Message;
                return;
            }

            services.Router.HandleUIEvent(UIEventId.Zeroing_ApplyAdjustment, ScreenId.ZeroingImpactAnalysis);
            RenderImpactAnalysis(applied.Data);
        }

        void OnNextRoundClicked()
        {
            if (!services.TrainingSessions.HasActiveSession)
            {
                return;
            }

            var analysis = services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId);
            if (!analysis.Success)
            {
                LastError = analysis.Message;
                return;
            }

            var next = services.Zeroing.ContinueAfterAnalysis(analysis.Data.SessionId);
            if (!next.Success)
            {
                LastError = next.Message;
                return;
            }

            var final = analysis.Data.PassedTenRing || analysis.Data.RoundIndex >= 3;
            var route = services.Router.HandleUIEvent(UIEventId.Zeroing_NextRound, ScreenId.ZeroingImpactAnalysis, new NavigationArgs
            {
                Mode = TrainingMode.Zeroing100m,
                SessionId = analysis.Data.SessionId,
                ReturnToScreen = final ? ScreenId.ZeroingFinalRating.ToString() : ScreenId.ZeroingHud.ToString()
            });

            if (!route.Success)
            {
                LastError = route.Message;
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

        static Vector2 DrawioMin(float x, float y, float width, float height)
        {
            return new Vector2(x * 2.4f, 1080f - (y + height) * 1.8f);
        }

        static Vector2 DrawioMax(float x, float y, float width, float height)
        {
            return new Vector2((x + width) * 2.4f, 1080f - y * 1.8f);
        }

        static void SetAnalysisText(TextMeshProUGUI label, string name, string value)
        {
            if (label != null)
            {
                label.text = name + "：" + value;
            }
        }

        static string FormatVerticalOffset(float y)
        {
            if (Mathf.Abs(y) < 0.01f)
            {
                return "居中 0cm";
            }

            return (y > 0f ? "偏上 " : "偏下 ") + FormatCm(y);
        }

        static string FormatHorizontalOffset(float x)
        {
            if (Mathf.Abs(x) < 0.01f)
            {
                return "居中 0cm";
            }

            return (x < 0f ? "偏左 " : "偏右 ") + FormatCm(x);
        }

        static string FormatFrontSight(ZeroingRoundAnalysisDto analysis)
        {
            if (analysis.VerticalDirection == VerticalAdjustmentDirection.None)
            {
                return "无需调整";
            }

            var direction = analysis.VerticalDirection == VerticalAdjustmentDirection.CounterClockwise ? "逆时针" : "顺时针";
            return direction + "调整 " + analysis.FrontSightDegreesToAdjust.ToString("0.#", CultureInfo.InvariantCulture) + "°";
        }

        static string FormatRearSight(ZeroingRoundAnalysisDto analysis)
        {
            if (analysis.HorizontalDirection == HorizontalAdjustmentDirection.None)
            {
                return "无需调整";
            }

            var direction = analysis.HorizontalDirection == HorizontalAdjustmentDirection.Forward ? "向前" : "向后";
            return direction + "调整 " + analysis.RearSightClicksToAdjust + " 格";
        }

        static string FormatSuggestion(ZeroingRoundAnalysisDto analysis)
        {
            if (analysis.VerticalDirection == VerticalAdjustmentDirection.None && analysis.HorizontalDirection == HorizontalAdjustmentDirection.None)
            {
                return "本轮弹着居中，应用确认后进入下一步。";
            }

            return FormatVerticalOffset(analysis.AverageOffsetCm.y) + "，准星柱" + FormatFrontSight(analysis) + "；"
                + FormatHorizontalOffset(analysis.AverageOffsetCm.x) + "，觇孔" + FormatRearSight(analysis) + "。";
        }

        static string FormatCm(float value)
        {
            return Mathf.Abs(value).ToString("0.#", CultureInfo.InvariantCulture) + "cm";
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
            var image = AddImage(panel.gameObject, fill);
            AddTestId(panel.gameObject, id);

            var panelSprite = ResolvePanelSprite(id, min, max);
            if (panelSprite != null)
            {
                ApplySlicedSprite(image, panelSprite);
            }
            else
            {
                var outline = panel.gameObject.AddComponent<Outline>();
                outline.effectColor = stroke;
                outline.effectDistance = new Vector2(2f, -2f);
            }

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
            var normalSprite = LoadGeneratedSprite(primary ? "button_primary_orange" : "button_secondary_cyan");
            if (normalSprite != null)
            {
                ApplySlicedSprite(image, normalSprite);
                image.raycastTarget = true;
            }
            else
            {
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = primary ? new Color32(247, 185, 85, 255) : new Color32(107, 138, 119, 255);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (normalSprite != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = LoadGeneratedSprite(primary ? "button_primary_orange_highlighted" : "button_secondary_cyan_highlighted"),
                    pressedSprite = LoadGeneratedSprite(primary ? "button_primary_orange_pressed" : "button_secondary_cyan_pressed"),
                    selectedSprite = LoadGeneratedSprite(primary ? "button_primary_orange_highlighted" : "button_secondary_cyan_highlighted"),
                    disabledSprite = normalSprite
                };

                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
                button.colors = colors;
            }
            else
            {
                var colors = button.colors;
                colors.normalColor = image.color;
                colors.highlightedColor = primary ? new Color32(220, 255, 150, 255) : new Color32(35, 62, 54, 255);
                colors.pressedColor = primary ? new Color32(170, 220, 70, 255) : new Color32(10, 24, 20, 255);
                button.colors = colors;
            }

            AddLabel(rect, id.Replace("Button_", "Text_"), label, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(8, 8), new Vector2(-8, -8), primary ? new Color32(255, 202, 92, 255) : new Color32(231, 242, 235, 255), true);
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
