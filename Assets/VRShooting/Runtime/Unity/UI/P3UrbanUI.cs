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
    [DisallowMultipleComponent]
    public sealed class P3UrbanMapSelectionView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text selectedMapText;
        [SerializeField] TMP_Text enemyText;
        [SerializeField] TMP_Text floorText;
        [SerializeField] TMP_Text conditionsText;
        [SerializeField] TMP_Text errorText;
        [SerializeField] Button selectButton;
        [SerializeField] Button backButton;
        [SerializeField] List<P3MapCardView> mapCards = new List<P3MapCardView>();

        public event Action<string> MapSelectionChanged;
        public event Action SelectRequested;
        public event Action BackRequested;

        public string SelectedMapId { get; private set; } = P3ContractIds.UrbanMap;
        public string LastError { get; private set; } = string.Empty;
        public IReadOnlyList<UrbanMapDto> LastMaps { get; private set; } = Array.Empty<UrbanMapDto>();
        public Button SelectButton => selectButton;
        public Button BackButton => backButton;

        public void Configure(TMP_Text title, TMP_Text selected, TMP_Text enemies, TMP_Text floors,
            TMP_Text conditions, TMP_Text error, Button select, Button back, IReadOnlyList<P3MapCardView> cards)
        {
            titleText = title;
            selectedMapText = selected;
            enemyText = enemies;
            floorText = floors;
            conditionsText = conditions;
            errorText = error;
            selectButton = select;
            backButton = back;
            mapCards = cards == null ? new List<P3MapCardView>() : new List<P3MapCardView>(cards);
            BindButtons();
            BindCards();
        }

        public void Apply(IReadOnlyList<UrbanMapDto> maps, string selectedMapId)
        {
            LastMaps = maps ?? Array.Empty<UrbanMapDto>();
            SelectedMapId = string.IsNullOrWhiteSpace(selectedMapId) ? P3ContractIds.UrbanMap : selectedMapId;
            LastError = string.Empty;
            P3UiText.SetText(titleText, "城镇攻防 · 地图选择");

            var selectedMap = UrbanMapDto.Empty;
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

                if (index < mapCards.Count) mapCards[index]?.Apply(map, selected);
            }

            for (var index = LastMaps.Count; index < mapCards.Count; index++)
                mapCards[index]?.ApplyUnavailable("城镇地图 B（后续扩展）");

            if (!found && LastMaps.Count > 0)
            {
                selectedMap = LastMaps[0];
                SelectedMapId = selectedMap.MapId;
                if (mapCards.Count > 0) mapCards[0]?.SetSelected(true);
            }

            P3UiText.SetText(selectedMapText, LastMaps.Count == 0
                ? "当前选择：暂无地图"
                : "当前选择：" + selectedMap.DisplayName);
            P3UiText.SetText(enemyText, found || LastMaps.Count > 0
                ? "街道敌人：" + selectedMap.StreetEnemyMin + "-" + selectedMap.StreetEnemyMax +
                  "\n建筑敌人：" + selectedMap.BuildingEnemyMin + "-" + selectedMap.BuildingEnemyMax
                : "敌情：--");
            P3UiText.SetText(floorText, found || LastMaps.Count > 0
                ? "建筑楼层：" + selectedMap.Floors.Count + " 层"
                : "建筑楼层：--");
            P3UiText.SetText(conditionsText,
                "任务信息\n搜索楼房\n消灭所有敌人\n队友保持三人纵队\n失败条件：玩家死亡");
            if (selectButton != null) selectButton.interactable = found || LastMaps.Count > 0;
        }

        public void SelectMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId)) return;
            UrbanMapDto selected = UrbanMapDto.Empty;
            var found = false;
            for (var index = 0; index < LastMaps.Count; index++)
            {
                if (LastMaps[index].MapId == mapId)
                {
                    selected = LastMaps[index];
                    found = true;
                    break;
                }
            }

            if (!found) return;
            SelectedMapId = mapId;
            for (var index = 0; index < mapCards.Count; index++) mapCards[index]?.SetSelected(mapCards[index].MapId == mapId);
            P3UiText.SetText(selectedMapText, "当前选择：" + selected.DisplayName);
            P3UiText.SetText(enemyText, "街道敌人：" + selected.StreetEnemyMin + "-" + selected.StreetEnemyMax +
                "\n建筑敌人：" + selected.BuildingEnemyMin + "-" + selected.BuildingEnemyMax);
            P3UiText.SetText(floorText, "建筑楼层：" + selected.Floors.Count + " 层");
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
            for (var index = 0; index < mapCards.Count; index++) mapCards[index]?.SetClickHandler(SelectMap);
        }

        void OnSelectClicked() => SelectRequested?.Invoke();
        void OnBackClicked() => BackRequested?.Invoke();
        bool HasSelectedMap() => !string.IsNullOrWhiteSpace(SelectedMapId) && FindMap(SelectedMapId).MapId.Length > 0;

        UrbanMapDto FindMap(string id)
        {
            for (var index = 0; index < LastMaps.Count; index++) if (LastMaps[index].MapId == id) return LastMaps[index];
            return UrbanMapDto.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class P3UrbanStreetHudView : P3CombatHudView
    {
        [SerializeField] TMP_Text streetStateText;
        [SerializeField] TMP_Text streetEnemyText;
        [SerializeField] Button enterBuildingButton;

        public event Action EnterBuildingRequested;
        public UrbanSessionDto LastSession { get; private set; }
        public bool EnterBuildingInteractive { get; private set; }
        public Button EnterBuildingButton => enterBuildingButton;

        public void ConfigureStreet(TMP_Text streetState, TMP_Text streetEnemies, Button enter)
        {
            streetStateText = streetState;
            streetEnemyText = streetEnemies;
            enterBuildingButton = enter;
            BindEnterButton();
        }

        public void Apply(UrbanSessionDto session, HudDto hud)
        {
            LastSession = session;
            hud = NormalizeHud(hud, session.SessionId, TrainingMode.Urban, HudType.UrbanStreet,
                session.Ammo, session.Player, session.MiniMap);
            var map = hud.MiniMap;
            ApplyHud(hud, session.Squad, map, "任务状态：街道行动");
            P3UiText.SetText(streetStateText, session.StreetCleared ? "街道状态：已清除" : "街道状态：尚有敌人");
            P3UiText.SetText(streetEnemyText, "街道敌人：" + session.StreetEnemyKilled + " / " + session.StreetEnemyTotal +
                (session.StreetCleared ? "" : "\n进入建筑仍有风险，需继续清除敌人"));
            var enterText = P3UiText.Prompt(hud, "EnterBuilding");
            if (string.IsNullOrWhiteSpace(enterText)) enterText = "进入建筑";
            P3UiText.SetText(promptText, enterText);
            EnterBuildingInteractive = P3UiText.PromptEnabled(hud, "EnterBuilding");
            if (enterBuildingButton != null)
            {
                enterBuildingButton.GetComponentInChildren<TMP_Text>()?.SetText(enterText);
                enterBuildingButton.interactable = EnterBuildingInteractive;
            }
        }

        void BindEnterButton()
        {
            if (enterBuildingButton == null) return;
            enterBuildingButton.onClick.RemoveListener(OnEnterBuildingClicked);
            enterBuildingButton.onClick.AddListener(OnEnterBuildingClicked);
        }

        void OnEnterBuildingClicked()
        {
            if (EnterBuildingInteractive) EnterBuildingRequested?.Invoke();
        }
    }

    [DisallowMultipleComponent]
    public sealed class P3UrbanBuildingHudView : P3CombatHudView
    {
        [SerializeField] TMP_Text floorText;
        [SerializeField] TMP_Text roomsText;
        [SerializeField] TMP_Text buildingProgressText;
        [SerializeField] Button openDoorButton;
        [SerializeField] Button checkRoomButton;
        [SerializeField] Button exitButton;

        public event Action<string> OpenRoomDoorRequested;
        public event Action<string> MarkRoomSearchedRequested;
        public event Action ExitBuildingRequested;

        public UrbanSessionDto LastSession { get; private set; }
        public string CurrentRoomId { get; private set; } = string.Empty;
        public bool OpenDoorInteractive { get; private set; }
        public bool CheckRoomInteractive { get; private set; }
        public Button OpenDoorButton => openDoorButton;
        public Button CheckRoomButton => checkRoomButton;
        public Button ExitButton => exitButton;

        public void ConfigureBuilding(TMP_Text floor, TMP_Text rooms, TMP_Text progress,
            Button openDoor, Button checkRoom, Button exit)
        {
            floorText = floor;
            roomsText = rooms;
            buildingProgressText = progress;
            openDoorButton = openDoor;
            checkRoomButton = checkRoom;
            exitButton = exit;
            BindInteractionButtons();
        }

        public void Apply(UrbanSessionDto session, HudDto hud)
        {
            LastSession = session;
            hud = NormalizeHud(hud, session.SessionId, TrainingMode.Urban, HudType.UrbanBuilding,
                session.Ammo, session.Player, session.MiniMap);
            var map = hud.MiniMap;
            ApplyHud(hud, session.Squad, map, "任务状态：建筑搜索");
            P3UiText.SetText(floorText, "当前楼层：" + (string.IsNullOrWhiteSpace(session.CurrentFloorId) ? "--" : session.CurrentFloorId));
            P3UiText.SetText(buildingProgressText, "建筑搜索：" + session.RoomsSearched + " / " + session.RoomsTotal +
                " · " + (session.RoomsTotal == 0 ? "0%" : P3UiText.Percent((float)session.RoomsSearched / session.RoomsTotal)));

            var floor = FindFloor(session, session.CurrentFloorId);
            var rooms = floor.Rooms ?? Array.Empty<RoomDto>();
            var lines = new List<string>();
            CurrentRoomId = string.Empty;
            for (var index = 0; index < rooms.Count; index++)
            {
                var room = rooms[index];
                if (string.IsNullOrWhiteSpace(CurrentRoomId) && room.SearchState != RoomSearchState.Searched)
                    CurrentRoomId = room.RoomId;
                lines.Add(room.DisplayName + " · " + P3UiText.RoomState(room.SearchState) +
                    (room.DoorOpen ? " · 门已开" : " · 门关闭") +
                    (room.HasPossibleEnemyArea ? " · 敌情预估" : ""));
            }
            P3UiText.SetText(roomsText, lines.Count == 0 ? "房间：暂无数据" : string.Join("\n", lines.ToArray()));

            var prompts = hud.Prompts ?? Array.Empty<HudPromptDto>();
            var openPrompt = FindRoomPrompt(prompts, ".OpenDoor");
            var checkPrompt = FindRoomPrompt(prompts, ".CheckRoom");
            OpenDoorInteractive = openPrompt.IsInteractive && openPrompt.IsEnabled;
            CheckRoomInteractive = checkPrompt.IsInteractive && checkPrompt.IsEnabled;
            if (!string.IsNullOrWhiteSpace(openPrompt.PromptId)) CurrentRoomId = RoomIdFromPrompt(openPrompt.PromptId);
            if (!string.IsNullOrWhiteSpace(checkPrompt.PromptId)) CurrentRoomId = RoomIdFromPrompt(checkPrompt.PromptId);

            var prompt = !string.IsNullOrWhiteSpace(openPrompt.Text) ? openPrompt.Text : checkPrompt.Text;
            if (string.IsNullOrWhiteSpace(prompt)) prompt = "观察房间状态";
            P3UiText.SetText(promptText, prompt);
            if (openDoorButton != null) openDoorButton.interactable = OpenDoorInteractive;
            if (checkRoomButton != null) checkRoomButton.interactable = CheckRoomInteractive;
            if (exitButton != null) exitButton.interactable = true;
        }

        public void SetBusy(bool busy)
        {
            if (openDoorButton != null) openDoorButton.interactable = !busy && OpenDoorInteractive;
            if (checkRoomButton != null) checkRoomButton.interactable = !busy && CheckRoomInteractive;
            if (exitButton != null) exitButton.interactable = !busy;
        }

        void BindInteractionButtons()
        {
            if (openDoorButton != null)
            {
                openDoorButton.onClick.RemoveListener(OnOpenDoorClicked);
                openDoorButton.onClick.AddListener(OnOpenDoorClicked);
            }
            if (checkRoomButton != null)
            {
                checkRoomButton.onClick.RemoveListener(OnCheckRoomClicked);
                checkRoomButton.onClick.AddListener(OnCheckRoomClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        void OnOpenDoorClicked()
        {
            if (OpenDoorInteractive && !string.IsNullOrWhiteSpace(CurrentRoomId)) OpenRoomDoorRequested?.Invoke(CurrentRoomId);
        }

        void OnCheckRoomClicked()
        {
            if (CheckRoomInteractive && !string.IsNullOrWhiteSpace(CurrentRoomId)) MarkRoomSearchedRequested?.Invoke(CurrentRoomId);
        }

        void OnExitClicked() => ExitBuildingRequested?.Invoke();

        static FloorDto FindFloor(UrbanSessionDto session, string floorId)
        {
            var floors = session.Floors ?? Array.Empty<FloorDto>();
            for (var index = 0; index < floors.Count; index++) if (floors[index].FloorId == floorId) return floors[index];
            return floors.Count > 0 ? floors[0] : FloorDto.Empty;
        }

        static HudPromptDto FindRoomPrompt(IReadOnlyList<HudPromptDto> prompts, string suffix)
        {
            for (var index = 0; index < prompts.Count; index++)
                if ((prompts[index].PromptId ?? string.Empty).EndsWith(suffix, StringComparison.Ordinal)) return prompts[index];
            return default;
        }

        static string RoomIdFromPrompt(string promptId)
        {
            var suffix = promptId.LastIndexOf('.');
            return suffix > 0 ? promptId.Substring(0, suffix) : promptId;
        }
    }

    [DisallowMultipleComponent]
    public sealed class P3UrbanResultsView : MonoBehaviour
    {
        [SerializeField] TMP_Text outcomeText;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text errorText;
        [SerializeField] List<P3MiniMapView> floorMaps = new List<P3MiniMapView>();
        [SerializeField] Button retryButton;
        [SerializeField] Button backButton;

        public event Action RetryRequested;
        public event Action BackToMainMenuRequested;
        public UrbanResultDto LastResult { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public int RenderedFloorMapCount { get; private set; }
        public Button RetryButton => retryButton;
        public Button BackButton => backButton;

        public void Configure(TMP_Text outcome, TMP_Text summary, TMP_Text stats, TMP_Text error,
            IReadOnlyList<P3MiniMapView> maps, Button retry, Button back)
        {
            outcomeText = outcome;
            summaryText = summary;
            statsText = stats;
            errorText = error;
            floorMaps = maps == null ? new List<P3MiniMapView>() : new List<P3MiniMapView>(maps);
            retryButton = retry;
            backButton = back;
            BindButtons();
        }

        public void Apply(UrbanResultDto result)
        {
            LastResult = result;
            LastError = string.Empty;
            P3UiText.SetText(outcomeText, result.Victory ? "胜利" : "失败");
            P3UiText.SetText(summaryText, "城镇攻防 · 任务结算");
            P3UiText.SetText(statsText,
                "街道清除：" + (result.StreetCleared ? "已清除" : "未清除") +
                "\n建筑搜索完成度：" + P3UiText.Percent(result.BuildingSearchProgress01) +
                "\n房间搜索：" + result.RoomsSearched + " / " + result.RoomsTotal +
                "\n消灭敌人：" + result.EnemyKilled + " / " + result.EnemyTotal +
                "\n剩余弹药：" + result.RemainingAmmo +
                "\n队友状态：\n" + P3UiText.Squad(result.Squad) +
                "\n用时：" + P3UiText.Time(result.ElapsedSeconds));

            var maps = result.FloorMaps ?? Array.Empty<MiniMapDto>();
            RenderedFloorMapCount = Mathf.Min(maps.Count, floorMaps.Count);
            for (var index = 0; index < floorMaps.Count; index++)
            {
                if (index < maps.Count) floorMaps[index]?.Apply(maps[index]);
                else floorMaps[index]?.ClearMap();
            }
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

    public interface IP3UrbanResultsActions
    {
        ServiceResult<Unit> Retry(string sessionId);
        ServiceResult<Unit> BackToMainMenu(string sessionId);
    }

    public sealed class P3DirectUrbanResultsActions : IP3UrbanResultsActions
    {
        readonly IUrbanService service;
        readonly IP3UiNavigationPort navigation;

        public P3DirectUrbanResultsActions(IUrbanService urbanService, IP3UiNavigationPort navigationPort)
        {
            service = urbanService ?? throw new ArgumentNullException(nameof(urbanService));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
        }

        public ServiceResult<Unit> Retry(string sessionId)
        {
            var cancelled = service.Cancel(sessionId);
            if (!cancelled.Success) return cancelled;
            var routed = navigation.Open(ScreenId.UrbanMapSelection, new NavigationArgs
            {
                Mode = TrainingMode.Urban,
                ReturnToScreen = ScreenId.MainMenu.ToString()
            });
            return routed.Success ? ServiceResult<Unit>.Ok(Unit.Value) : ServiceResult<Unit>.Fail(routed.ErrorCode, routed.Message);
        }

        public ServiceResult<Unit> BackToMainMenu(string sessionId)
        {
            var cancelled = service.Cancel(sessionId);
            if (!cancelled.Success) return cancelled;
            var routed = navigation.Open(ScreenId.MainMenu, new NavigationArgs { Mode = TrainingMode.Urban });
            return routed.Success ? ServiceResult<Unit>.Ok(Unit.Value) : ServiceResult<Unit>.Fail(routed.ErrorCode, routed.Message);
        }
    }

    public sealed class P3UrbanMapSelectionPresenter : IDisposable
    {
        IUrbanService service;
        P3UrbanMapSelectionView view;
        IP3UiNavigationPort navigation;
        P3MapSelectionState state;
        string weaponId;
        RandomSeed seed;
        bool busy;
        bool commandCompleted;

        public string LastError { get; private set; } = string.Empty;

        public void Initialize(IUrbanService urbanService, P3UrbanMapSelectionView mapView,
            IP3UiNavigationPort navigationPort, P3MapSelectionState selectionState,
            string trainingWeaponId, RandomSeed randomSeed)
        {
            Dispose();
            service = urbanService ?? throw new ArgumentNullException(nameof(urbanService));
            view = mapView ?? throw new ArgumentNullException(nameof(mapView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            state = selectionState ?? new P3MapSelectionState();
            weaponId = string.IsNullOrWhiteSpace(trainingWeaponId) ? P3ContractIds.TrainingWeapon : trainingWeaponId;
            seed = randomSeed;
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
            view.Apply(maps.Data, state.UrbanMapId);
        }

        void OnMapSelectionChanged(string mapId) { if (!string.IsNullOrWhiteSpace(mapId)) state.UrbanMapId = mapId; }

        void OnSelectRequested()
        {
            if (busy || commandCompleted || service == null) return;
            busy = true;
            view.SetBusy(true);
            var selected = service.SelectMap(view.SelectedMapId);
            if (!selected.Success) { Complete(selected.ErrorCode, selected.Message, false); return; }
            state.UrbanMapId = selected.Data.MapId;
            var started = service.StartSession(state.UrbanMapId, weaponId, seed);
            if (!started.Success) { Complete(started.ErrorCode, started.Message, false); return; }
            state.UrbanSessionId = started.Data.SessionId;
            var routed = navigation.Open(ScreenId.UrbanStreetHud, new NavigationArgs
            {
                Mode = TrainingMode.Urban,
                SessionId = started.Data.SessionId,
                ReturnToScreen = ScreenId.UrbanMapSelection.ToString()
            });
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Message, routed.Success);
        }

        void OnBackRequested()
        {
            if (busy || commandCompleted) return;
            busy = true;
            view.SetBusy(true);
            var routed = navigation.Back();
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Message, routed.Success);
        }

        void Complete(ErrorCode error, string message, bool success)
        {
            busy = false;
            if (success) commandCompleted = true;
            LastError = error == ErrorCode.None ? string.Empty : P3UiText.Error(error, message);
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
            service = null; view = null; navigation = null; state = null;
            busy = false; commandCompleted = false;
        }
    }

    public sealed class P3UrbanStreetHudPresenter : IDisposable
    {
        IUrbanService service;
        IHUDService hudService;
        P3UrbanStreetHudView view;
        IP3UiNavigationPort navigation;
        string sessionId = string.Empty;
        long revision = -1;
        bool busy;
        bool disposed;

        public void Initialize(IUrbanService urbanService, P3UrbanStreetHudView streetView,
            IP3UiNavigationPort navigationPort, string currentSessionId, IHUDService p3HudService = null)
        {
            Dispose();
            service = urbanService ?? throw new ArgumentNullException(nameof(urbanService));
            view = streetView ?? throw new ArgumentNullException(nameof(streetView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            hudService = p3HudService;
            sessionId = currentSessionId ?? string.Empty;
            disposed = false;
            service.SessionChanged += OnSessionChanged;
            service.ResultReady += OnResultReady;
            view.EnterBuildingRequested += OnEnterBuildingRequested;
            Refresh();
        }

        public void Refresh()
        {
            if (disposed || service == null || view == null || string.IsNullOrWhiteSpace(sessionId)) return;
            var current = service.GetSession(sessionId);
            if (!current.Success) { view.ApplyError(current.ErrorCode, current.Message); return; }
            Apply(current.Data);
        }

        void OnSessionChanged(UrbanSessionDto snapshot)
        {
            if (disposed || snapshot.SessionId != sessionId || snapshot.Revision <= revision) return;
            Apply(snapshot);
        }

        void Apply(UrbanSessionDto snapshot)
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

        void OnEnterBuildingRequested()
        {
            if (busy || !view.EnterBuildingInteractive) return;
            busy = true;
            var maps = service.GetMaps();
            var entranceId = maps.Success && maps.Data.Count > 0 ? maps.Data[0].BuildingEntranceId : string.Empty;
            if (string.IsNullOrWhiteSpace(entranceId)) { Complete(ErrorCode.NotFound, "建筑入口不存在", false); return; }
            var entered = service.EnterBuilding(sessionId, entranceId);
            if (!entered.Success) { Complete(entered.ErrorCode, entered.Message, false); return; }
            Apply(entered.Data);
            var routed = navigation.Open(ScreenId.UrbanBuildingHud, new NavigationArgs
            {
                Mode = TrainingMode.Urban, SessionId = sessionId, ReturnToScreen = ScreenId.UrbanStreetHud.ToString()
            });
            Complete(routed.Success ? ErrorCode.None : routed.ErrorCode, routed.Message, routed.Success);
        }

        void Complete(ErrorCode error, string message, bool success)
        {
            busy = false;
            if (error != ErrorCode.None) view.ShowError(error, message);
        }

        void OnResultReady(UrbanResultDto result)
        {
            if (disposed || result.SessionId != sessionId || result.Revision <= revision) return;
            revision = result.Revision;
            navigation.Open(ScreenId.UrbanResults, new NavigationArgs
            {
                Mode = TrainingMode.Urban, SessionId = sessionId, ReturnToScreen = ScreenId.UrbanStreetHud.ToString()
            });
        }

        public void Dispose()
        {
            if (service != null)
            {
                service.SessionChanged -= OnSessionChanged;
                service.ResultReady -= OnResultReady;
            }
            if (view != null) view.EnterBuildingRequested -= OnEnterBuildingRequested;
            service = null; hudService = null; view = null; navigation = null;
            sessionId = string.Empty; revision = -1; busy = false; disposed = true;
        }
    }

    public sealed class P3UrbanBuildingHudPresenter : IDisposable
    {
        IUrbanService service;
        IHUDService hudService;
        P3UrbanBuildingHudView view;
        IP3UiNavigationPort navigation;
        string sessionId = string.Empty;
        long revision = -1;
        string lastCommandKey = string.Empty;
        long lastCommandRevision = -1;
        bool busy;
        bool disposed;

        public void Initialize(IUrbanService urbanService, P3UrbanBuildingHudView buildingView,
            IP3UiNavigationPort navigationPort, string currentSessionId, IHUDService p3HudService = null)
        {
            Dispose();
            service = urbanService ?? throw new ArgumentNullException(nameof(urbanService));
            view = buildingView ?? throw new ArgumentNullException(nameof(buildingView));
            navigation = navigationPort ?? throw new ArgumentNullException(nameof(navigationPort));
            hudService = p3HudService;
            sessionId = currentSessionId ?? string.Empty;
            disposed = false;
            service.SessionChanged += OnSessionChanged;
            service.ResultReady += OnResultReady;
            view.OpenRoomDoorRequested += OnOpenRoomDoorRequested;
            view.MarkRoomSearchedRequested += OnMarkRoomSearchedRequested;
            view.ExitBuildingRequested += OnExitBuildingRequested;
            Refresh();
        }

        public void Refresh()
        {
            if (disposed || service == null || view == null || string.IsNullOrWhiteSpace(sessionId)) return;
            var current = service.GetSession(sessionId);
            if (!current.Success) { view.ApplyError(current.ErrorCode, current.Message); return; }
            Apply(current.Data);
        }

        void OnSessionChanged(UrbanSessionDto snapshot)
        {
            if (disposed || snapshot.SessionId != sessionId || snapshot.Revision <= revision) return;
            Apply(snapshot);
        }

        void Apply(UrbanSessionDto snapshot)
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

        void OnOpenRoomDoorRequested(string roomId)
            => ExecuteRoomCommand("OpenDoor:" + roomId, roomId, service.OpenRoomDoor);

        void OnMarkRoomSearchedRequested(string roomId)
            => ExecuteRoomCommand("CheckRoom:" + roomId, roomId, service.MarkRoomSearched);

        void ExecuteRoomCommand(string key, string roomId, Func<string, string, ServiceResult<UrbanSessionDto>> command)
        {
            if (busy || string.IsNullOrWhiteSpace(roomId) ||
                (key == lastCommandKey && revision <= lastCommandRevision)) return;
            busy = true;
            var result = command(sessionId, roomId);
            if (!result.Success)
            {
                busy = false;
                view.ShowError(result.ErrorCode, result.Message);
                view.SetBusy(false);
                return;
            }

            Apply(result.Data);
            lastCommandKey = key;
            lastCommandRevision = revision;
            busy = false;
        }

        void OnExitBuildingRequested()
        {
            if (busy) return;
            var maps = service.GetMaps();
            var entranceId = maps.Success && maps.Data.Count > 0 ? maps.Data[0].BuildingEntranceId : string.Empty;
            if (string.IsNullOrWhiteSpace(entranceId)) { view.ApplyError(ErrorCode.NotFound, "建筑入口不存在"); return; }
            busy = true;
            var result = service.ExitBuilding(sessionId, entranceId);
            if (!result.Success)
            {
                busy = false;
                view.ShowError(result.ErrorCode, result.Message);
                view.SetBusy(false);
                return;
            }
            Apply(result.Data);
            var routed = navigation.Open(ScreenId.UrbanStreetHud, new NavigationArgs
            {
                Mode = TrainingMode.Urban, SessionId = sessionId, ReturnToScreen = ScreenId.UrbanBuildingHud.ToString()
            });
            busy = false;
            if (!routed.Success) view.ShowError(routed.ErrorCode, routed.Message);
        }

        void OnResultReady(UrbanResultDto result)
        {
            if (disposed || result.SessionId != sessionId || result.Revision <= revision) return;
            revision = result.Revision;
            navigation.Open(ScreenId.UrbanResults, new NavigationArgs
            {
                Mode = TrainingMode.Urban, SessionId = sessionId, ReturnToScreen = ScreenId.UrbanBuildingHud.ToString()
            });
        }

        public void Dispose()
        {
            if (service != null)
            {
                service.SessionChanged -= OnSessionChanged;
                service.ResultReady -= OnResultReady;
            }
            if (view != null)
            {
                view.OpenRoomDoorRequested -= OnOpenRoomDoorRequested;
                view.MarkRoomSearchedRequested -= OnMarkRoomSearchedRequested;
                view.ExitBuildingRequested -= OnExitBuildingRequested;
            }
            service = null; hudService = null; view = null; navigation = null;
            sessionId = string.Empty; revision = -1; lastCommandKey = string.Empty; lastCommandRevision = -1;
            busy = false; disposed = true;
        }
    }

    public sealed class P3UrbanResultsPresenter : IDisposable
    {
        IUrbanService service;
        P3UrbanResultsView view;
        IP3UrbanResultsActions actions;
        string sessionId = string.Empty;
        long revision = -1;
        bool busy;
        bool commandCompleted;

        public string LastError { get; private set; } = string.Empty;

        public void Initialize(IUrbanService urbanService, P3UrbanResultsView resultView,
            IP3UiNavigationPort navigation, string currentSessionId, IP3UrbanResultsActions resultActions = null)
        {
            Dispose();
            service = urbanService ?? throw new ArgumentNullException(nameof(urbanService));
            view = resultView ?? throw new ArgumentNullException(nameof(resultView));
            sessionId = currentSessionId ?? string.Empty;
            actions = resultActions ?? new P3DirectUrbanResultsActions(service, navigation);
            service.ResultReady += OnResultReady;
            view.RetryRequested += OnRetryRequested;
            view.BackToMainMenuRequested += OnBackToMainMenuRequested;
            Refresh();
        }

        public void Refresh()
        {
            if (service == null || view == null || string.IsNullOrWhiteSpace(sessionId)) return;
            var result = service.GetResult(sessionId);
            if (result.Success) Apply(result.Data);
            else { LastError = P3UiText.Error(result.ErrorCode, result.Message); view.ShowError(result.ErrorCode, result.Message); }
        }

        void OnResultReady(UrbanResultDto result)
        {
            if (result.SessionId != sessionId || result.Revision <= revision) return;
            Apply(result);
        }

        void Apply(UrbanResultDto result) { revision = result.Revision; view.Apply(result); }
        void OnRetryRequested() => Execute(actions.Retry);
        void OnBackToMainMenuRequested() => Execute(actions.BackToMainMenu);

        void Execute(Func<string, ServiceResult<Unit>> command)
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
            service = null; view = null; actions = null; sessionId = string.Empty;
            revision = -1; busy = false; commandCompleted = false;
        }
    }
}
