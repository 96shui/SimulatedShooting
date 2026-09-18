using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    /// <summary>Pending configuration shared by the two trench preparation screens.</summary>
    [Serializable]
    public sealed class P3MapSelectionState
    {
        public string TrenchMapId = P3ContractIds.TrenchMap;
        public string UrbanMapId = P3ContractIds.UrbanMap;
        public string TrenchSessionId = string.Empty;
        public string UrbanSessionId = string.Empty;
    }

    [DisallowMultipleComponent]
    public sealed class P3TrenchMapSelectionView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text selectedMapText;
        [SerializeField] TMP_Text difficultyText;
        [SerializeField] TMP_Text enemyText;
        [SerializeField] TMP_Text conditionsText;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button selectButton;
        [SerializeField] Button backButton;
        [SerializeField] List<P3MapCardView> mapCards = new List<P3MapCardView>();

        public event Action<string> MapSelectionChanged;
        public event Action SelectRequested;
        public event Action BackRequested;

        public string SelectedMapId { get; private set; } = P3ContractIds.TrenchMap;
        public string LastError { get; private set; } = string.Empty;
        public IReadOnlyList<TrenchMapDto> LastMaps { get; private set; } = Array.Empty<TrenchMapDto>();
        public Button SelectButton => selectButton;
        public Button BackButton => backButton;

        public void Configure(
            TMP_Text title,
            TMP_Text selected,
            TMP_Text difficulty,
            TMP_Text enemies,
            TMP_Text conditions,
            TMP_Text error,
            Button select,
            Button back,
            IReadOnlyList<P3MapCardView> cards)
        {
            titleText = title;
            selectedMapText = selected;
            difficultyText = difficulty;
            enemyText = enemies;
            conditionsText = conditions;
            errorText = error;
            selectButton = select;
            backButton = back;
            mapCards = cards == null ? new List<P3MapCardView>() : new List<P3MapCardView>(cards);
            BindButtons();
            BindCards();
        }

        public void Apply(IReadOnlyList<TrenchMapDto> maps, string selectedMapId)
        {
            LastMaps = maps ?? Array.Empty<TrenchMapDto>();
            SelectedMapId = string.IsNullOrWhiteSpace(selectedMapId) ? P3ContractIds.TrenchMap : selectedMapId;
            LastError = string.Empty;
            P3UiText.SetText(titleText, "堑壕射击 · 地图选择");

            var selectedMap = TrenchMapDto.Empty;
            var found = false;
            for (var index = 0; index < LastMaps.Count; index++)
            {
                var map = LastMaps[index];
                var selected = map.MapId == SelectedMapId;
                if (selected)
                {
                    selectedMap = map;
                    found = true;
                }

                if (index < mapCards.Count && mapCards[index] != null)
                {
                    mapCards[index].Apply(map, selected);
                }
            }

            for (var index = LastMaps.Count; index < mapCards.Count; index++)
            {
                mapCards[index]?.ApplyUnavailable("地图 B（后续扩展）");
            }

            if (!found && LastMaps.Count > 0)
            {
                selectedMap = LastMaps[0];
                SelectedMapId = selectedMap.MapId;
                if (mapCards.Count > 0)
                {
                    mapCards[0]?.SetSelected(true);
                }
            }

            P3UiText.SetText(selectedMapText, found || LastMaps.Count > 0
                ? "当前选择：" + selectedMap.DisplayName
                : "当前选择：暂无地图");
            P3UiText.SetText(difficultyText, found ? "复杂度：" + P3UiText.Difficulty(selectedMap.Difficulty) : "复杂度：--");
            P3UiText.SetText(enemyText, found
                ? "敌人数量：" + selectedMap.MinEnemyCount + "-" + selectedMap.MaxEnemyCount
                : "敌人数量：--");
            P3UiText.SetText(conditionsText,
                "任务条件\n搜索完整堑壕\n消灭全部敌人\n失败条件：玩家死亡\n红色标记：敌人预估区域");
            if (selectButton != null)
            {
                selectButton.interactable = found;
            }
        }

        public void SelectMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                return;
            }

            var available = false;
            for (var index = 0; index < LastMaps.Count; index++)
            {
                if (LastMaps[index].MapId == mapId)
                {
                    available = true;
                    break;
                }
            }

            if (!available)
            {
                return;
            }

            SelectedMapId = mapId;
            for (var index = 0; index < mapCards.Count; index++)
            {
                mapCards[index]?.SetSelected(mapCards[index].MapId == mapId);
            }

            var selectedMap = FindMap(mapId);
            P3UiText.SetText(selectedMapText, "当前选择：" + selectedMap.DisplayName);
            P3UiText.SetText(difficultyText, "复杂度：" + P3UiText.Difficulty(selectedMap.Difficulty));
            P3UiText.SetText(enemyText, "敌人数量：" + selectedMap.MinEnemyCount + "-" + selectedMap.MaxEnemyCount);
            MapSelectionChanged?.Invoke(mapId);
        }

        public void SetBusy(bool busy)
        {
            if (selectButton != null) selectButton.interactable = !busy && HasSelectedMap();
            if (backButton != null) backButton.interactable = !busy;
            for (var index = 0; index < mapCards.Count; index++) mapCards[index]?.SetInteractable(!busy);
        }

        public void ShowError(ErrorCode code, string message)
        {
            LastError = P3UiText.Error(code, message);
            P3UiText.SetText(errorText, LastError);
            if (selectButton != null) selectButton.interactable = false;
        }

        void BindButtons()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        void BindCards()
        {
            for (var index = 0; index < mapCards.Count; index++)
            {
                var card = mapCards[index];
                if (card == null) continue;
                card.SetClickHandler(SelectMap);
            }
        }

        void OnSelectClicked() => SelectRequested?.Invoke();
        void OnBackClicked() => BackRequested?.Invoke();

        bool HasSelectedMap() => !string.IsNullOrWhiteSpace(SelectedMapId) && FindMap(SelectedMapId).MapId.Length > 0;

        TrenchMapDto FindMap(string id)
        {
            for (var index = 0; index < LastMaps.Count; index++)
                if (LastMaps[index].MapId == id) return LastMaps[index];
            return TrenchMapDto.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class P3TrenchBriefingView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text mapText;
        [SerializeField] TMP_Text enemyText;
        [SerializeField] TMP_Text squadText;
        [SerializeField] TMP_Text objectivesText;
        [SerializeField] TMP_Text projectionStateText;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button startButton;
        [SerializeField] Button viewMapButton;
        [SerializeField] Button backButton;
        [SerializeField] P3MiniMapView projectedMap;

        public event Action StartRequested;
        public event Action ViewMapRequested;
        public event Action BackRequested;

        public bool MapFocused { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public TrenchBriefingDto LastBriefing { get; private set; }
        public Button StartButton => startButton;
        public Button ViewMapButton => viewMapButton;
        public Button BackButton => backButton;
        public P3MiniMapView ProjectedMap => projectedMap;

        public void Configure(
            TMP_Text title,
            TMP_Text map,
            TMP_Text enemies,
            TMP_Text squad,
            TMP_Text objectives,
            TMP_Text projectionState,
            TMP_Text error,
            Button start,
            Button view,
            Button back,
            P3MiniMapView mapView)
        {
            titleText = title;
            mapText = map;
            enemyText = enemies;
            squadText = squad;
            objectivesText = objectives;
            projectionStateText = projectionState;
            errorText = error;
            startButton = start;
            viewMapButton = view;
            backButton = back;
            projectedMap = mapView;
            BindButtons();
        }

        public void Apply(TrenchBriefingDto briefing, TrenchMapDto map)
        {
            LastBriefing = briefing;
            LastError = string.Empty;
            P3UiText.SetText(titleText, "堑壕射击 · 开场任务");
            P3UiText.SetText(mapText, "地图：" + (string.IsNullOrWhiteSpace(map.DisplayName) ? briefing.MapId : map.DisplayName));
            P3UiText.SetText(enemyText, "敌情预估：" + briefing.EnemyEstimateMin + "-" + briefing.EnemyEstimateMax + " 名");
            P3UiText.SetText(squadText, "小队配置\n" + P3UiText.Squad(briefing.PlannedSquad));
            P3UiText.SetText(objectivesText, "任务目标\n进入堑壕\n搜索路线\n消灭敌人\n保持小队队形");
            P3UiText.SetText(projectionStateText, MapFocused ? "投影：已聚焦 · 点击返回简报" : "投影：无人机起飞 · 等待开始");
            projectedMap?.Apply(briefing.ProjectedMap);
            if (startButton != null) startButton.interactable = true;
            if (viewMapButton != null) viewMapButton.interactable = true;
        }

        public void SetMapFocused(bool focused)
        {
            MapFocused = focused;
            P3UiText.SetText(projectionStateText, focused ? "投影：已聚焦 · 点击返回简报" : "投影：无人机起飞 · 等待开始");
            if (projectedMap != null)
            {
                var rect = projectedMap.transform as RectTransform;
                if (rect != null) rect.localScale = focused ? Vector3.one * 1.12f : Vector3.one;
            }
        }

        public void SetBusy(bool busy)
        {
            if (startButton != null) startButton.interactable = !busy;
            if (viewMapButton != null) viewMapButton.interactable = !busy;
            if (backButton != null) backButton.interactable = !busy;
        }

        public void ShowError(ErrorCode code, string message)
        {
            LastError = P3UiText.Error(code, message);
            P3UiText.SetText(errorText, LastError);
        }

        void BindButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (viewMapButton != null)
            {
                viewMapButton.onClick.RemoveListener(OnViewMapClicked);
                viewMapButton.onClick.AddListener(OnViewMapClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        void OnStartClicked() => StartRequested?.Invoke();
        void OnViewMapClicked() => ViewMapRequested?.Invoke();
        void OnBackClicked() => BackRequested?.Invoke();
    }

    public interface IP3BriefingVisualPort
    {
        void PlayDroneTakeoff(TrenchBriefingDto briefing);
    }

    [DisallowMultipleComponent]
    public sealed class P3BriefingDroneVisual : MonoBehaviour, IP3BriefingVisualPort
    {
        public int PlayCount { get; private set; }

        public void PlayDroneTakeoff(TrenchBriefingDto briefing)
        {
            // Presentation-only hook. It never creates or modifies a mission.
            PlayCount++;
        }
    }

    public sealed class P3TrenchMapSelectionPresenter : IDisposable
    {
        ITrenchService service;
        P3TrenchMapSelectionView view;
        IP3UiNavigationPort navigation;
        P3MapSelectionState state;
        bool busy;
        bool commandCompleted;

        public string LastError { get; private set; } = string.Empty;

        public void Initialize(ITrenchService trenchService, P3TrenchMapSelectionView mapView,
            IP3UiNavigationPort navigationPort, P3MapSelectionState selectionState)
        {
            Dispose();
            service = trenchService ?? throw new ArgumentNullException(nameof(trenchService));
            view = mapView ?? throw new ArgumentNullException(nameof(mapView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            state = selectionState ?? new P3MapSelectionState();
            view.MapSelectionChanged += OnMapSelectionChanged;
            view.SelectRequested += OnSelectRequested;
            view.BackRequested += OnBackRequested;
            Refresh();
        }

        public void Refresh()
        {
            if (service == null || view == null) return;
            var maps = service.GetMaps();
            if (!maps.Success)
            {
                LastError = P3UiText.Error(maps.ErrorCode, maps.Message);
                view.ShowError(maps.ErrorCode, maps.Message);
                return;
            }

            view.Apply(maps.Data, state.TrenchMapId);
        }

        void OnMapSelectionChanged(string mapId)
        {
            if (!string.IsNullOrWhiteSpace(mapId)) state.TrenchMapId = mapId;
        }

        void OnSelectRequested()
        {
            if (busy || commandCompleted || service == null) return;
            busy = true;
            view.SetBusy(true);
            var selected = service.SelectMap(view.SelectedMapId);
            if (!selected.Success)
            {
                Complete(selected.ErrorCode, selected.Message, false);
                return;
            }

            state.TrenchMapId = selected.Data.MapId;
            var routed = navigation.Open(ScreenId.TrenchBriefing, new NavigationArgs
            {
                Mode = TrainingMode.Trench,
                ReturnToScreen = ScreenId.TrenchMapSelection.ToString()
            });
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Success ? string.Empty : "无法打开堑壕简报", routed.Success);
        }

        void OnBackRequested()
        {
            if (busy || commandCompleted || navigation == null) return;
            busy = true;
            view.SetBusy(true);
            var routed = navigation.Back();
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Message, routed.Success);
        }

        void Complete(ErrorCode error, string message, bool success)
        {
            busy = false;
            LastError = error == ErrorCode.None ? string.Empty : P3UiText.Error(error, message);
            if (success) commandCompleted = true;
            view.SetBusy(false);
            if (error != ErrorCode.None) view.ShowError(error, message);
        }

        public void Dispose()
        {
            if (view != null)
            {
                view.MapSelectionChanged -= OnMapSelectionChanged;
                view.SelectRequested -= OnSelectRequested;
                view.BackRequested -= OnBackRequested;
            }

            service = null;
            view = null;
            navigation = null;
            state = null;
            busy = false;
            commandCompleted = false;
        }
    }

    public sealed class P3TrenchBriefingPresenter : IDisposable
    {
        ITrenchService service;
        P3TrenchBriefingView view;
        IP3UiNavigationPort navigation;
        P3MapSelectionState state;
        IP3BriefingVisualPort visual;
        string weaponId;
        RandomSeed seed;
        bool busy;
        bool commandCompleted;
        bool visualPlayed;

        public string LastError { get; private set; } = string.Empty;
        public TrenchBriefingDto LastBriefing { get; private set; }

        public void Initialize(ITrenchService trenchService, P3TrenchBriefingView briefingView,
            IP3UiNavigationPort navigationPort, P3MapSelectionState selectionState,
            string trainingWeaponId, RandomSeed randomSeed, IP3BriefingVisualPort briefingVisual = null)
        {
            Dispose();
            service = trenchService ?? throw new ArgumentNullException(nameof(trenchService));
            view = briefingView ?? throw new ArgumentNullException(nameof(briefingView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            state = selectionState ?? new P3MapSelectionState();
            weaponId = string.IsNullOrWhiteSpace(trainingWeaponId) ? P3ContractIds.TrainingWeapon : trainingWeaponId;
            seed = randomSeed;
            visual = briefingVisual;
            view.StartRequested += OnStartRequested;
            view.ViewMapRequested += OnViewMapRequested;
            view.BackRequested += OnBackRequested;
            Refresh();
        }

        public void Refresh()
        {
            if (service == null || view == null) return;
            var selected = service.SelectMap(state.TrenchMapId);
            if (!selected.Success)
            {
                LastError = P3UiText.Error(selected.ErrorCode, selected.Message);
                view.ShowError(selected.ErrorCode, selected.Message);
                return;
            }

            var briefing = service.GetBriefing(selected.Data.MapId, weaponId, seed);
            if (!briefing.Success)
            {
                LastError = P3UiText.Error(briefing.ErrorCode, briefing.Message);
                view.ShowError(briefing.ErrorCode, briefing.Message);
                return;
            }

            LastBriefing = briefing.Data;
            view.Apply(briefing.Data, selected.Data);
            if (!visualPlayed)
            {
                visual?.PlayDroneTakeoff(briefing.Data);
                visualPlayed = true;
            }
        }

        void OnStartRequested()
        {
            if (busy || commandCompleted || service == null) return;
            busy = true;
            view.SetBusy(true);
            var started = service.StartSession(state.TrenchMapId, weaponId, seed);
            if (!started.Success)
            {
                Complete(started.ErrorCode, started.Message, false);
                return;
            }

            state.TrenchSessionId = started.Data.SessionId;
            var routed = navigation.Open(ScreenId.TrenchHud, new NavigationArgs
            {
                Mode = TrainingMode.Trench,
                SessionId = started.Data.SessionId,
                ReturnToScreen = ScreenId.TrenchBriefing.ToString()
            });
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Success ? string.Empty : "无法打开堑壕 HUD", routed.Success);
        }

        void OnViewMapRequested()
        {
            if (busy || view == null) return;
            view.SetMapFocused(!view.MapFocused);
        }

        void OnBackRequested()
        {
            if (busy || commandCompleted || navigation == null) return;
            busy = true;
            view.SetBusy(true);
            var routed = navigation.Open(ScreenId.TrenchMapSelection, new NavigationArgs
            {
                Mode = TrainingMode.Trench,
                ReturnToScreen = ScreenId.MainMenu.ToString()
            });
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Message, routed.Success);
        }

        void Complete(ErrorCode error, string message, bool success)
        {
            busy = false;
            LastError = error == ErrorCode.None ? string.Empty : P3UiText.Error(error, message);
            if (success) commandCompleted = true;
            view.SetBusy(false);
            if (error != ErrorCode.None) view.ShowError(error, message);
        }

        public void Dispose()
        {
            if (view != null)
            {
                view.StartRequested -= OnStartRequested;
                view.ViewMapRequested -= OnViewMapRequested;
                view.BackRequested -= OnBackRequested;
            }

            service = null;
            view = null;
            navigation = null;
            state = null;
            visual = null;
            busy = false;
            commandCompleted = false;
            visualPlayed = false;
        }
    }
}
