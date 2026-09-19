using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    /// <summary>Common HUD surface for all P3 combat phases.</summary>
    [DisallowMultipleComponent]
    public class P3CombatHudView : MonoBehaviour
    {
        [SerializeField] protected TMP_Text healthText;
        [SerializeField] protected TMP_Text ammoText;
        [SerializeField] protected TMP_Text postureText;
        [SerializeField] protected TMP_Text shoulderText;
        [SerializeField] protected TMP_Text cornerText;
        [SerializeField] protected TMP_Text squadText;
        [SerializeField] protected TMP_Text promptText;
        [SerializeField] protected TMP_Text stateText;
        [SerializeField] protected P3MiniMapView miniMap;
        RectTransform healthFill;

        public HudDto LastHud { get; private set; }
        public SquadStatusDto LastSquad { get; private set; }
        public string LastPromptId { get; private set; } = string.Empty;
        public string LastPromptText { get; private set; } = string.Empty;
        public P3MiniMapView MiniMap => miniMap;

        public virtual void Configure(TMP_Text health, TMP_Text ammo, TMP_Text posture, TMP_Text shoulder,
            TMP_Text corner, TMP_Text squad, TMP_Text prompt, TMP_Text state, P3MiniMapView map)
        {
            healthText = health;
            ammoText = ammo;
            postureText = posture;
            shoulderText = shoulder;
            cornerText = corner;
            squadText = squad;
            promptText = prompt;
            stateText = state;
            miniMap = map;
            if (healthText != null)
            {
                healthText.enableAutoSizing = true;
                healthText.fontSizeMin = 18;
                healthText.fontSizeMax = 26;
                healthText.enableWordWrapping = false;
            }
            if (health != null && healthFill == null)
            {
                var background = TacticalUIStyle.Artwork(health.transform, "Hud_Combat_HealthTrack", null, Vector2.zero, Vector2.one);
                var track = background.rectTransform;
                track.anchorMin = Vector2.zero; track.anchorMax = new Vector2(1, 0);
                track.offsetMin = new Vector2(0, -9); track.offsetMax = new Vector2(0, -3);
                background.color = new Color32(27, 49, 60, 255);
                var fill = TacticalUIStyle.Artwork(track, "Hud_Combat_HealthFill", null, Vector2.zero, Vector2.one);
                healthFill = fill.rectTransform;
                healthFill.anchorMin = Vector2.zero; healthFill.anchorMax = Vector2.one;
                healthFill.offsetMin = healthFill.offsetMax = Vector2.zero;
                fill.color = new Color32(109, 223, 187, 255);
            }
        }

        public void ApplyHud(HudDto hud, SquadStatusDto squad, MiniMapDto map, string progressText = "")
        {
            LastHud = hud;
            LastSquad = squad;
            var normal = new Color32(218, 241, 249, 255);
            var warning = new Color32(255, 114, 90, 255);
            if (healthText != null) healthText.color = hud.Player.Health <= 25 || !hud.Player.IsAlive ? warning : normal;
            if (ammoText != null) ammoText.color = hud.Ammo.IsReloading
                ? new Color32(247, 185, 85, 255)
                : hud.Ammo.CurrentMagazine <= 0 ? warning : normal;
            if (healthFill != null)
            {
                healthFill.anchorMax = new Vector2(Mathf.Clamp01(hud.Player.Health / 100f), 1);
                healthFill.GetComponent<Image>().color = hud.Player.Health <= 25 ? new Color32(255, 114, 90, 255) : new Color32(109, 223, 187, 255);
            }
            P3UiText.SetText(healthText, "生命：" + Mathf.RoundToInt(hud.Player.Health) + " / 100" +
                (hud.Player.IsAlive ? "" : " · 已失去战斗能力"));
            P3UiText.SetText(ammoText, "弹药：" + P3UiText.Ammo(hud.Ammo) +
                (hud.Ammo.IsReloading ? " · 换弹中" : "") +
                "\n当前弹匣 " + hud.Ammo.CurrentMagazine + " · 备用 " + hud.Ammo.ReserveAmmo);
            P3UiText.SetText(postureText, "姿态：" + P3UiText.Posture(hud.Player.Posture));
            P3UiText.SetText(shoulderText, "射击肩：" + P3UiText.Shoulder(hud.Player.Shoulder));
            P3UiText.SetText(cornerText, "拐角射击：" + (hud.Player.CornerShootingAvailable ? "可用" : "不可用"));
            P3UiText.SetText(squadText, P3UiText.Squad(squad));
            P3UiText.SetText(stateText, string.IsNullOrWhiteSpace(progressText) ? "任务状态：运行中" : progressText);
            ApplyPrompt(hud);
            miniMap?.Apply(map);
        }

        public void ApplyError(ErrorCode code, string message)
        {
            P3UiText.SetText(stateText, P3UiText.Error(code, message));
        }

        public void ShowError(ErrorCode code, string message)
        {
            ApplyError(code, message);
        }

        protected void ApplyPrompt(HudDto hud)
        {
            LastPromptId = string.Empty;
            LastPromptText = string.Empty;
            var prompts = hud.Prompts ?? Array.Empty<HudPromptDto>();
            for (var index = 0; index < prompts.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(prompts[index].Text))
                {
                    LastPromptId = prompts[index].PromptId ?? string.Empty;
                    LastPromptText = prompts[index].Text;
                    break;
                }
            }

            P3UiText.SetText(promptText, LastPromptText);
        }

        protected static HudDto NormalizeHud(HudDto hud, string sessionId, TrainingMode mode,
            HudType hudType, AmmoDto fallbackAmmo, PlayerStatusDto fallbackPlayer, MiniMapDto fallbackMap)
        {
            var ammo = hud.Ammo.MagazineCapacity > 0 ? hud.Ammo : fallbackAmmo;
            var player = string.Equals(hud.SessionId, sessionId, StringComparison.Ordinal) ? hud.Player : fallbackPlayer;
            var map = hud.MiniMap.Visible ? hud.MiniMap : fallbackMap;
            return new HudDto
            {
                SessionId = string.IsNullOrWhiteSpace(hud.SessionId) ? sessionId : hud.SessionId,
                Mode = mode,
                HudType = hudType,
                Ammo = ammo,
                Player = player,
                MiniMap = map,
                TextLines = hud.TextLines ?? Array.Empty<HudTextLineDto>(),
                Prompts = hud.Prompts ?? Array.Empty<HudPromptDto>(),
                CanShoot = hud.CanShoot,
                FireSequence = hud.FireSequence
            };
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
    }

    [DisallowMultipleComponent]
    public sealed class P3TrenchHudView : P3CombatHudView
    {
        [SerializeField] TMP_Text enemyProgressText;
        [SerializeField] TMP_Text searchProgressText;

        public TrenchSessionDto LastSession { get; private set; }
        RectTransform searchFill;

        public void ConfigureTrench(TMP_Text enemyProgress, TMP_Text searchProgress)
        {
            enemyProgressText = enemyProgress;
            searchProgressText = searchProgress;
            if (searchProgress != null && searchFill == null)
            {
                var background = TacticalUIStyle.Artwork(searchProgress.transform, "Hud_Trench_SearchTrack", null, Vector2.zero, Vector2.one);
                var rect = background.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0);
                rect.offsetMin = new Vector2(0, -15); rect.offsetMax = new Vector2(0, -7);
                background.color = new Color32(27, 49, 60, 255);
                var fill = TacticalUIStyle.Artwork(rect, "Hud_Trench_SearchFill", null, Vector2.zero, Vector2.one);
                searchFill = fill.rectTransform;
                searchFill.anchorMin = Vector2.zero; searchFill.anchorMax = Vector2.zero;
                searchFill.offsetMin = searchFill.offsetMax = Vector2.zero;
                fill.color = new Color32(90, 204, 240, 255);
            }
        }

        public void Apply(TrenchSessionDto session, HudDto hud)
        {
            LastSession = session;
            if (searchFill != null) searchFill.anchorMax = new Vector2(Mathf.Clamp01(session.SearchProgress01), 1);
            hud = NormalizeHud(hud, session.SessionId, TrainingMode.Trench, HudType.Trench,
                session.Ammo, session.Player, session.MiniMap);
            var map = hud.MiniMap;
            ApplyHud(hud, session.Squad, map, "任务状态：" + State(session.State));
            P3UiText.SetText(enemyProgressText, "敌情：" + session.EnemyKilled + " / " + session.EnemyTotal);
            P3UiText.SetText(searchProgressText, "搜索进度：" + P3UiText.Percent(session.SearchProgress01));
        }

        static string State(SessionState state)
        {
            switch (state)
            {
                case SessionState.Completed: return "已完成";
                case SessionState.Failed: return "已失败";
                case SessionState.Paused: return "已暂停";
                default: return "运行中";
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class P3TrenchResultsView : MonoBehaviour
    {
        [SerializeField] TMP_Text outcomeText;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text errorText;
        [SerializeField] P3MiniMapView resultMap;
        [SerializeField] Button retryButton;
        [SerializeField] Button backButton;

        public event Action RetryRequested;
        public event Action BackToMainMenuRequested;
        public TrenchResultDto LastResult { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public Button RetryButton => retryButton;
        public Button BackButton => backButton;
        public P3MiniMapView ResultMap => resultMap;

        public void Configure(TMP_Text outcome, TMP_Text summary, TMP_Text stats, TMP_Text error,
            P3MiniMapView map, Button retry, Button back)
        {
            outcomeText = outcome;
            summaryText = summary;
            statsText = stats;
            errorText = error;
            resultMap = map;
            retryButton = retry;
            backButton = back;
            BindButtons();
        }

        public void Apply(TrenchResultDto result)
        {
            LastResult = result;
            LastError = string.Empty;
            P3UiText.SetText(outcomeText, result.Victory ? "胜利" : "失败");
            if (outcomeText != null) outcomeText.color = result.Victory ? new Color32(125, 232, 189, 255) : new Color32(255, 130, 100, 255);
            P3UiText.SetText(summaryText, "堑壕射击 · 任务结算\n地图：" + result.MapName);
            P3UiText.SetText(statsText,
                "消灭敌人：" + result.EnemyKilled + " / " + result.EnemyTotal +
                "\n搜索完成度：" + P3UiText.Percent(result.SearchProgress01) +
                "\n剩余弹药：" + result.RemainingAmmo +
                "\n队友状态：\n" + P3UiText.Squad(result.Squad) +
                "\n用时：" + P3UiText.Time(result.ElapsedSeconds));
            resultMap?.Apply(result.ResultMap);
            if (retryButton != null) retryButton.interactable = true;
            if (backButton != null) backButton.interactable = true;
        }

        public void ShowError(ErrorCode code, string message)
        {
            LastError = P3UiText.Error(code, message);
            P3UiText.SetText(errorText, LastError);
        }

        public void SetBusy(bool busy)
        {
            if (retryButton != null) retryButton.interactable = !busy;
            if (backButton != null) backButton.interactable = !busy;
        }

        void BindButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        void OnRetryClicked() => RetryRequested?.Invoke();
        void OnBackClicked() => BackToMainMenuRequested?.Invoke();
    }

    public interface IP3TrenchResultsActions
    {
        ServiceResult<Unit> Retry(string sessionId);
        ServiceResult<Unit> BackToMainMenu(string sessionId);
    }

    /// <summary>
    /// Fake-friendly fallback. Production composition can replace this with the
    /// application coordinator, which owns scene reload and result persistence.
    /// </summary>
    public sealed class P3DirectTrenchResultsActions : IP3TrenchResultsActions
    {
        readonly ITrenchService service;
        readonly IP3UiNavigationPort navigation;

        public P3DirectTrenchResultsActions(ITrenchService trenchService, IP3UiNavigationPort navigationPort)
        {
            service = trenchService ?? throw new ArgumentNullException(nameof(trenchService));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
        }

        public ServiceResult<Unit> Retry(string sessionId)
        {
            var cancelled = service.Cancel(sessionId);
            if (!cancelled.Success) return cancelled;
            var routed = navigation.Open(ScreenId.TrenchBriefing, new NavigationArgs
            {
                Mode = TrainingMode.Trench,
                ReturnToScreen = ScreenId.MainMenu.ToString()
            });
            return routed.Success ? ServiceResult<Unit>.Ok(Unit.Value) : ServiceResult<Unit>.Fail(routed.ErrorCode, routed.Message);
        }

        public ServiceResult<Unit> BackToMainMenu(string sessionId)
        {
            var cancelled = service.Cancel(sessionId);
            if (!cancelled.Success) return cancelled;
            var routed = navigation.Open(ScreenId.MainMenu, new NavigationArgs { Mode = TrainingMode.Trench });
            return routed.Success ? ServiceResult<Unit>.Ok(Unit.Value) : ServiceResult<Unit>.Fail(routed.ErrorCode, routed.Message);
        }
    }

    public sealed class P3TrenchHudPresenter : IDisposable
    {
        ITrenchService service;
        IHUDService hudService;
        P3TrenchHudView view;
        IP3UiNavigationPort navigation;
        string sessionId = string.Empty;
        long revision = -1;
        bool disposed;

        public void Initialize(ITrenchService trenchService, P3TrenchHudView hudView,
            IP3UiNavigationPort navigationPort, string currentSessionId, IHUDService p3HudService = null)
        {
            Dispose();
            service = trenchService ?? throw new ArgumentNullException(nameof(trenchService));
            view = hudView ?? throw new ArgumentNullException(nameof(hudView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            hudService = p3HudService;
            sessionId = currentSessionId ?? string.Empty;
            disposed = false;
            service.SessionChanged += OnSessionChanged;
            service.ResultReady += OnResultReady;
            Refresh();
        }

        public void Refresh()
        {
            if (disposed || service == null || view == null || string.IsNullOrWhiteSpace(sessionId)) return;
            var current = service.GetSession(sessionId);
            if (!current.Success)
            {
                view.ApplyError(current.ErrorCode, current.Message);
                return;
            }

            Apply(current.Data);
        }

        void OnSessionChanged(TrenchSessionDto snapshot)
        {
            if (disposed || snapshot.SessionId != sessionId || snapshot.Revision <= revision) return;
            Apply(snapshot);
        }

        void Apply(TrenchSessionDto snapshot)
        {
            revision = snapshot.Revision;
            var hud = HudDto.Empty;
            if (hudService != null)
            {
                var currentHud = hudService.GetHud(sessionId);
                if (currentHud.Success) hud = currentHud.Data;
            }

            view.Apply(snapshot, hud);
        }

        void OnResultReady(TrenchResultDto result)
        {
            if (disposed || result.SessionId != sessionId || result.Revision <= revision) return;
            revision = result.Revision;
            navigation.Open(ScreenId.TrenchResults, new NavigationArgs
            {
                Mode = TrainingMode.Trench,
                SessionId = result.SessionId,
                ReturnToScreen = ScreenId.TrenchHud.ToString()
            });
        }

        public void Dispose()
        {
            if (service != null)
            {
                service.SessionChanged -= OnSessionChanged;
                service.ResultReady -= OnResultReady;
            }

            service = null;
            hudService = null;
            view = null;
            navigation = null;
            sessionId = string.Empty;
            revision = -1;
            disposed = true;
        }
    }

    public sealed class P3TrenchResultsPresenter : IDisposable
    {
        ITrenchService service;
        P3TrenchResultsView view;
        IP3UiNavigationPort navigation;
        IP3TrenchResultsActions actions;
        string sessionId = string.Empty;
        long revision = -1;
        bool busy;
        bool commandCompleted;

        public string LastError { get; private set; } = string.Empty;

        public void Initialize(ITrenchService trenchService, P3TrenchResultsView resultView,
            IP3UiNavigationPort navigationPort, string currentSessionId, IP3TrenchResultsActions resultActions = null)
        {
            Dispose();
            service = trenchService ?? throw new ArgumentNullException(nameof(trenchService));
            view = resultView ?? throw new ArgumentNullException(nameof(resultView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            sessionId = currentSessionId ?? string.Empty;
            actions = resultActions ?? new P3DirectTrenchResultsActions(service, navigation);
            service.ResultReady += OnResultReady;
            view.RetryRequested += OnRetryRequested;
            view.BackToMainMenuRequested += OnBackToMainMenuRequested;
            Refresh();
        }

        public void Refresh()
        {
            if (service == null || view == null || string.IsNullOrWhiteSpace(sessionId)) return;
            var result = service.GetResult(sessionId);
            if (result.Success)
            {
                Apply(result.Data);
            }
            else
            {
                LastError = P3UiText.Error(result.ErrorCode, result.Message);
                view.ShowError(result.ErrorCode, result.Message);
            }
        }

        void OnResultReady(TrenchResultDto result)
        {
            if (result.SessionId != sessionId || result.Revision <= revision) return;
            Apply(result);
        }

        void Apply(TrenchResultDto result)
        {
            revision = result.Revision;
            view.Apply(result);
        }

        void OnRetryRequested() => Execute(actions.Retry, ScreenId.TrenchBriefing);
        void OnBackToMainMenuRequested() => Execute(actions.BackToMainMenu, ScreenId.MainMenu);

        void Execute(Func<string, ServiceResult<Unit>> command, ScreenId fallbackScreen)
        {
            if (busy || commandCompleted || string.IsNullOrWhiteSpace(sessionId)) return;
            busy = true;
            view.SetBusy(true);
            var result = command(sessionId);
            if (!result.Success)
            {
                busy = false;
                LastError = P3UiText.Error(result.ErrorCode, result.Message);
                view.ShowError(result.ErrorCode, result.Message);
                view.SetBusy(false);
                return;
            }

            commandCompleted = true;
            busy = false;
            // The action owns navigation. fallbackScreen is retained as a diagnostic
            // contract for custom actions and is intentionally not used to reset state.
            view.SetBusy(true);
        }

        public void Dispose()
        {
            if (service != null) service.ResultReady -= OnResultReady;
            if (view != null)
            {
                view.RetryRequested -= OnRetryRequested;
                view.BackToMainMenuRequested -= OnBackToMainMenuRequested;
            }

            service = null;
            view = null;
            navigation = null;
            actions = null;
            sessionId = string.Empty;
            revision = -1;
            busy = false;
            commandCompleted = false;
        }
    }
}
