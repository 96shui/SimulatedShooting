using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Unity.Bootstrap;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Scene-owned Task 5 view. It only renders presenter view models and exposes UI commands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovingTargetRangeUI : MonoBehaviour
    {
        readonly Dictionary<float, Button> speedButtons = new Dictionary<float, Button>();
        RectTransform largePanelRoot;
        RectTransform minimalHudRoot;
        RectTransform settingsScreen;
        RectTransform hudScreen;
        RectTransform resultsScreen;
        TMP_Text selectedSpeedText;
        TMP_Text settingsStatusText;
        TMP_Text errorText;
        TMP_Text hudModeText;
        TMP_Text hudAmmoText;
        TMP_Text hudHitsText;
        TMP_Text hudProgressText;
        TMP_Text hudSpeedText;
        TMP_Text hudDirectionText;
        TMP_Text hudCountdownText;
        TMP_Text hudFireStateText;
        TMP_Text hudPromptText;
        TMP_Text resultSummaryText;
        TMP_Text resultSequencesText;
        Button startButton;
        Button settingsBackButton;
        Button retryButton;
        Button resultsBackButton;
        MovingTargetUIPresenter presenter;
        bool built;

        public static MovingTargetRangeUI EnsureExistsInScene(ApplicationServices services = null)
        {
            var ui = FindObjectOfType<MovingTargetRangeUI>(true);
            if (ui == null)
            {
                ui = new GameObject(nameof(MovingTargetRangeUI), typeof(RectTransform))
                    .AddComponent<MovingTargetRangeUI>();
            }

            var resolved = services ?? GameMain.Instance?.Services;
            if (resolved != null && !ui.IsInitialized)
            {
                ui.Initialize(new MovingTargetUICommandAdapter(resolved));
            }

            return ui;
        }

        public bool IsInitialized => presenter != null && presenter.IsInitialized;

        public MovingTargetUIPresenter Presenter => presenter;

        public GameObject LargePanelRoot => largePanelRoot != null ? largePanelRoot.gameObject : null;

        public GameObject MinimalHudRoot => minimalHudRoot != null ? minimalHudRoot.gameObject : null;

        /// <summary>
        /// Composition-root entry point. The scene supplies poses while the UI owns canvas size and scale.
        /// </summary>
        public bool BindToSceneAnchors(
            Transform largePanelAnchor,
            Transform minimalHudAnchor,
            Camera eventCamera = null)
        {
            if (!built)
            {
                Build();
            }

            var binder = GetComponent<TrainingUIAnchorBinder>();
            if (binder == null)
            {
                binder = gameObject.AddComponent<TrainingUIAnchorBinder>();
            }

            binder.Configure(largePanelRoot, minimalHudRoot);
            var large = EnsureAnchor(largePanelAnchor, TrainingUIAnchorSlot.LargePanel);
            var minimal = EnsureAnchor(minimalHudAnchor, TrainingUIAnchorSlot.MinimalHud);
            if (!binder.Bind(large, minimal))
            {
                return false;
            }

            ConfigureWorldSpaceRoot(largePanelRoot, eventCamera, 10);
            ConfigureWorldSpaceRoot(minimalHudRoot, eventCamera, 11);
            return true;
        }

        public void Initialize(IMovingTargetUIPort port)
        {
            if (!built)
            {
                Build();
            }

            presenter = GetComponent<MovingTargetUIPresenter>();
            if (presenter == null)
            {
                presenter = gameObject.AddComponent<MovingTargetUIPresenter>();
            }
            presenter.Initialize(this, port);
        }

        void Awake()
        {
            TrainingUIHost.EnsureExists();
            if (GameMain.Instance?.Services != null)
            {
                Initialize(new MovingTargetUICommandAdapter(GameMain.Instance.Services));
            }
        }

        void Build()
        {
            built = true;
            gameObject.name = nameof(MovingTargetRangeUI);
            AddTestId(gameObject, nameof(MovingTargetRangeUI));

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var adapter = GetComponent<TrainingUICanvasAdapter>();
            if (adapter == null)
            {
                adapter = gameObject.AddComponent<TrainingUICanvasAdapter>();
            }
            adapter.Configure(canvas);

            largePanelRoot = CreateRect("LargePanelRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddTestId(largePanelRoot.gameObject, "Training.Shared.LargePanelRoot");
            minimalHudRoot = CreateRect("MinimalHudRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddTestId(minimalHudRoot.gameObject, "Training.Shared.MinimalHudRoot");

            settingsScreen = CreateScreen(largePanelRoot, "Screen_MovingTargetSetup", true);
            var settingsAlias = CreateRect("Screen_MovingTargetSettings", settingsScreen, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            AddTestId(settingsAlias.gameObject, "Screen_MovingTargetSettings");
            BuildSettings(settingsScreen);

            hudScreen = CreateScreen(minimalHudRoot, "Screen_MovingTargetHud", false);
            BuildHud(hudScreen);

            resultsScreen = CreateScreen(largePanelRoot, "Screen_MovingTargetResults", true);
            BuildResults(resultsScreen);

            largePanelRoot.gameObject.SetActive(false);
            minimalHudRoot.gameObject.SetActive(false);
        }

        void BuildSettings(RectTransform parent)
        {
            AddPanel(parent, "Panel_MovingTargetSetup_Content", new Vector2(290, 175), new Vector2(1630, 900));
            AddLabel(parent, "Text_MovingTargetSetup_Title", "移动目标射击", 52, FontStyles.Bold,
                new Vector2(500, 225), new Vector2(1420, 315), new Color32(231, 242, 235, 255));
            AddLabel(parent, "Text_MovingTargetSetup_Rules",
                "卧姿固定射击位  ·  两发起射 / 长按连射  ·  左端停留禁射",
                25, FontStyles.Bold, new Vector2(430, 330), new Vector2(1490, 380), new Color32(143, 217, 255, 255));

            var speeds = new[] { 3f, 4f, 5f };
            for (var index = 0; index < speeds.Length; index++)
            {
                var speed = speeds[index];
                var button = AddButton(parent, "Button_MovingTargetSetup_Speed" + speed.ToString("0"),
                    speed.ToString("0") + " m/s", new Vector2(555 + index * 285, 440), new Vector2(795 + index * 285, 530), false);
                speedButtons[speed] = button;
            }

            selectedSpeedText = AddLabel(parent, "Text_MovingTargetSetup_SelectedSpeed", "当前选择：4 m/s", 28,
                FontStyles.Bold, new Vector2(630, 565), new Vector2(1290, 625), new Color32(200, 255, 106, 255));
            settingsStatusText = AddLabel(parent, "Text_MovingTargetSetup_Status", "选择速度后开始", 26,
                FontStyles.Bold, new Vector2(540, 640), new Vector2(1380, 700), new Color32(247, 185, 85, 255));
            errorText = AddLabel(parent, "Text_MovingTargetSetup_Error", string.Empty, 20,
                FontStyles.Bold, new Vector2(500, 705), new Vector2(1420, 750), new Color32(255, 120, 92, 255));
            settingsBackButton = AddButton(parent, "Button_MovingTargetSetup_Back", "返回", new Vector2(525, 785), new Vector2(805, 855), false);
            startButton = AddButton(parent, "Button_MovingTargetSetup_Start", "开始训练", new Vector2(1115, 785), new Vector2(1395, 855), true);
        }

        void BuildHud(RectTransform parent)
        {
            AddPanel(parent, "Panel_MovingTargetHud_Left", new Vector2(35, 45), new Vector2(455, 310));
            AddPanel(parent, "Panel_MovingTargetHud_Right", new Vector2(1465, 45), new Vector2(1885, 310));
            hudModeText = AddLabel(parent, "Hud_MovingTarget_FireMode", "两发起射 / 长按连射", 24, FontStyles.Bold,
                new Vector2(65, 70), new Vector2(425, 110), new Color32(143, 217, 255, 255));
            hudAmmoText = AddLabel(parent, "Hud_MovingTarget_Ammo", "弹药 --", 31, FontStyles.Bold,
                new Vector2(65, 125), new Vector2(425, 175), new Color32(231, 242, 235, 255));
            hudHitsText = AddLabel(parent, "Hud_MovingTarget_Hits", "命中 --", 24, FontStyles.Normal,
                new Vector2(65, 185), new Vector2(425, 225), new Color32(231, 242, 235, 255));
            hudProgressText = AddLabel(parent, "Hud_MovingTarget_Progress", "进度 --", 24, FontStyles.Normal,
                new Vector2(65, 240), new Vector2(425, 280), new Color32(231, 242, 235, 255));
            hudSpeedText = AddLabel(parent, "Hud_MovingTarget_Speed", "速度 --", 24, FontStyles.Bold,
                new Vector2(1495, 70), new Vector2(1855, 110), new Color32(143, 217, 255, 255));
            hudDirectionText = AddLabel(parent, "Hud_MovingTarget_Direction", "方向 --", 28, FontStyles.Bold,
                new Vector2(1495, 125), new Vector2(1855, 175), new Color32(231, 242, 235, 255));
            hudFireStateText = AddLabel(parent, "Hud_MovingTarget_FireSequence", "待扣动", 24, FontStyles.Normal,
                new Vector2(1495, 185), new Vector2(1855, 225), new Color32(231, 242, 235, 255));
            hudPromptText = AddLabel(parent, "Hud_MovingTarget_NoFirePrompt", "等待开始", 30, FontStyles.Bold,
                new Vector2(680, 815), new Vector2(1240, 885), new Color32(247, 185, 85, 255));
            hudCountdownText = AddLabel(parent, "Hud_MovingTarget_Countdown", string.Empty, 92, FontStyles.Bold,
                new Vector2(805, 95), new Vector2(1115, 235), new Color32(247, 185, 85, 255));
        }

        void BuildResults(RectTransform parent)
        {
            AddPanel(parent, "Panel_MovingTargetResults_Content", new Vector2(245, 105), new Vector2(1675, 955));
            AddLabel(parent, "Text_MovingTargetResults_Title", "移动靶训练结算", 48, FontStyles.Bold,
                new Vector2(500, 145), new Vector2(1420, 225), new Color32(231, 242, 235, 255));
            resultSummaryText = AddLabel(parent, "Text_MovingTargetResults_Summary", string.Empty, 27, FontStyles.Bold,
                new Vector2(350, 250), new Vector2(1570, 430), new Color32(143, 217, 255, 255));
            resultSequencesText = AddLabel(parent, "Text_MovingTargetResults_Sequences", string.Empty, 20, FontStyles.Normal,
                new Vector2(350, 450), new Vector2(1570, 760), new Color32(231, 242, 235, 255), TextAlignmentOptions.TopLeft);
            resultsBackButton = AddButton(parent, "Button_MovingTargetResults_BackToModeSelection", "返回模式选择",
                new Vector2(485, 815), new Vector2(835, 885), false);
            retryButton = AddButton(parent, "Button_MovingTargetResults_Retry", "重新训练",
                new Vector2(1085, 815), new Vector2(1435, 885), true);
        }

        internal IReadOnlyDictionary<float, Button> SpeedButtons => speedButtons;
        internal Button StartButton => startButton;
        internal Button SettingsBackButton => settingsBackButton;
        internal Button RetryButton => retryButton;
        internal Button ResultsBackButton => resultsBackButton;

        internal void RenderSettings(float selectedSpeed, IReadOnlyList<float> availableSpeeds)
        {
            selectedSpeedText.text = "当前选择：" + selectedSpeed.ToString("0") + " m/s";
            foreach (var pair in speedButtons)
            {
                var available = ContainsSpeed(availableSpeeds, pair.Key);
                pair.Value.gameObject.SetActive(available);
                var image = pair.Value.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Math.Abs(pair.Key - selectedSpeed) < 0.001f
                        ? new Color32(45, 156, 255, 255)
                        : new Color32(24, 45, 39, 255);
                }
            }
        }

        internal void RenderPresentation(TrainingPresentationDto dto, bool busy)
        {
            largePanelRoot.gameObject.SetActive(dto.LargePanelVisible);
            minimalHudRoot.gameObject.SetActive(dto.MinimalHudVisible);
            settingsScreen.gameObject.SetActive(dto.LargePanelVisible && dto.ActiveScreen == ScreenId.MovingTargetSettings);
            resultsScreen.gameObject.SetActive(dto.LargePanelVisible && dto.ActiveScreen == ScreenId.MovingTargetResults);
            hudScreen.gameObject.SetActive(dto.MinimalHudVisible && dto.ActiveScreen == ScreenId.MovingTargetHud);
            var canStart = dto.Phase == TrainingPresentationPhase.AwaitingStartConfirmation
                           || dto.Phase == TrainingPresentationPhase.ModeEntry;
            startButton.interactable = canStart && !busy;
            foreach (var button in speedButtons.Values)
            {
                button.interactable = canStart && !busy;
            }

            retryButton.interactable = dto.Phase == TrainingPresentationPhase.SessionResults && !busy;
            resultsBackButton.interactable = dto.Phase == TrainingPresentationPhase.SessionResults && !busy;
            settingsBackButton.interactable = !busy;
            settingsStatusText.text = dto.AwaitingWeaponPickup
                ? "请拿起正前方训练枪"
                : (canStart ? "选择速度后开始" : string.Empty);
            if (dto.ActiveScreen == ScreenId.MovingTargetResults && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        internal void RenderHud(MovingTargetHudViewModel model)
        {
            hudModeText.text = model.FireMode;
            hudAmmoText.text = model.Ammo;
            hudHitsText.text = model.Hits;
            hudProgressText.text = model.Progress;
            hudSpeedText.text = model.Speed;
            hudDirectionText.text = model.Direction;
            hudFireStateText.text = model.FireState;
            hudPromptText.text = model.Prompt;
            hudPromptText.color = model.CanShoot ? new Color32(200, 255, 106, 255) : new Color32(247, 185, 85, 255);
            hudCountdownText.text = model.Countdown;
        }

        internal void ClearHud()
        {
            hudAmmoText.text = "弹药 --";
            hudHitsText.text = "命中 --";
            hudProgressText.text = "进度 --";
            hudFireStateText.text = "待扣动";
            hudPromptText.text = string.Empty;
            hudCountdownText.text = string.Empty;
        }

        internal void RenderResult(MovingTargetResultViewModel model)
        {
            resultSummaryText.text = model.Summary;
            resultSequencesText.text = model.Sequences;
        }

        internal void RenderError(string message)
        {
            errorText.text = message ?? string.Empty;
        }

        internal void SetBusy(bool busy)
        {
            startButton.interactable &= !busy;
            retryButton.interactable &= !busy;
            resultsBackButton.interactable &= !busy;
            settingsBackButton.interactable &= !busy;
        }

        static bool ContainsSpeed(IReadOnlyList<float> speeds, float value)
        {
            if (speeds == null)
            {
                return false;
            }

            for (var i = 0; i < speeds.Count; i++)
            {
                if (Math.Abs(speeds[i] - value) < 0.001f)
                {
                    return true;
                }
            }

            return false;
        }

        static RectTransform CreateScreen(Transform parent, string id, bool opaque)
        {
            var rect = CreateRect(id, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (opaque)
            {
                AddImage(rect.gameObject, new Color32(7, 16, 13, 242));
            }
            AddTestId(rect.gameObject, id);
            return rect;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        static void AddPanel(RectTransform parent, string id, Vector2 min, Vector2 max)
        {
            var rect = CreateRect(id, parent, Vector2.zero, Vector2.zero, min, max);
            AddImage(rect.gameObject, new Color32(11, 25, 20, 235));
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(45, 156, 255, 220);
            outline.effectDistance = new Vector2(2f, 2f);
            AddTestId(rect.gameObject, id);
        }

        static TMP_Text AddLabel(RectTransform parent, string id, string value, float size, FontStyles style,
            Vector2 min, Vector2 max, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rect = CreateRect(id, parent, Vector2.zero, Vector2.zero, min, max);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            AddTestId(rect.gameObject, id);
            return label;
        }

        static Button AddButton(RectTransform parent, string id, string label, Vector2 min, Vector2 max, bool primary)
        {
            var rect = CreateRect(id, parent, Vector2.zero, Vector2.zero, min, max);
            var image = AddImage(rect.gameObject, primary ? new Color32(45, 156, 255, 255) : new Color32(24, 45, 39, 255));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            AddLabel(rect, id + "_Label", label, 24, FontStyles.Bold, Vector2.zero, max - min,
                new Color32(240, 247, 242, 255));
            AddTestId(rect.gameObject, id);
            return button;
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

        static TrainingUIAnchor EnsureAnchor(Transform anchorTransform, TrainingUIAnchorSlot slot)
        {
            if (anchorTransform == null)
            {
                return null;
            }

            var anchor = anchorTransform.GetComponent<TrainingUIAnchor>();
            if (anchor == null)
            {
                anchor = anchorTransform.gameObject.AddComponent<TrainingUIAnchor>();
            }

            anchor.Configure(slot);
            return anchor;
        }

        static void ConfigureWorldSpaceRoot(RectTransform root, Camera eventCamera, int sortingOrder)
        {
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1920f, 1080f);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * 0.00105f;

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = root.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 1f;

            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.gameObject.AddComponent<GraphicRaycaster>();
            }
            if (root.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }

        static void AddTestId(GameObject go, string id)
        {
            var testId = go.GetComponent<VRShooting.Unity.UITestId>();
            if (testId == null)
            {
                testId = go.AddComponent<VRShooting.Unity.UITestId>();
            }
            testId.SetId(id);
        }
    }

    public readonly struct MovingTargetHudViewModel
    {
        public string FireMode { get; init; }
        public string Ammo { get; init; }
        public string Hits { get; init; }
        public string Progress { get; init; }
        public string Speed { get; init; }
        public string Direction { get; init; }
        public string Countdown { get; init; }
        public string FireState { get; init; }
        public string Prompt { get; init; }
        public bool CanShoot { get; init; }
    }

    public readonly struct MovingTargetResultViewModel
    {
        public string Summary { get; init; }
        public string Sequences { get; init; }
    }
}
