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
    /// <summary>
    /// Runtime-authored P3 UI root. The prefab contains only this composition
    /// component; page objects are generated with stable names/IDs so an empty
    /// scene can exercise the UI without hand-editing a Unity hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class P3CombatUIRoot : MonoBehaviour
    {
        [SerializeField] bool buildOnAwake = true;
        [SerializeField] bool showOnAwake;

        readonly Dictionary<ScreenId, GameObject> pages = new Dictionary<ScreenId, GameObject>();
        readonly P3MapSelectionState selection = new P3MapSelectionState();

        Canvas canvas;
        bool built;
        IP3UiNavigationPort navigation;
        P3TrenchMapSelectionPresenter trenchMapPresenter;
        P3TrenchBriefingPresenter trenchBriefingPresenter;
        P3TrenchHudPresenter trenchHudPresenter;
        P3TrenchResultsPresenter trenchResultsPresenter;
        P3UrbanMapSelectionPresenter urbanMapPresenter;
        P3UrbanStreetHudPresenter urbanStreetPresenter;
        P3UrbanBuildingHudPresenter urbanBuildingPresenter;
        P3UrbanResultsPresenter urbanResultsPresenter;

        P3TrenchMapSelectionView trenchMapView;
        P3TrenchBriefingView trenchBriefingView;
        P3TrenchHudView trenchHudView;
        P3TrenchResultsView trenchResultsView;
        P3UrbanMapSelectionView urbanMapView;
        P3UrbanStreetHudView urbanStreetView;
        P3UrbanBuildingHudView urbanBuildingView;
        P3UrbanResultsView urbanResultsView;

        public bool IsBuilt => built;
        public ScreenId VisibleScreen { get; private set; } = ScreenId.TrenchMapSelection;
        public P3MapSelectionState Selection => selection;
        public P3TrenchMapSelectionView TrenchMapView => trenchMapView;
        public P3TrenchBriefingView TrenchBriefingView => trenchBriefingView;
        public P3TrenchHudView TrenchHudView => trenchHudView;
        public P3TrenchResultsView TrenchResultsView => trenchResultsView;
        public P3UrbanMapSelectionView UrbanMapView => urbanMapView;
        public P3UrbanStreetHudView UrbanStreetView => urbanStreetView;
        public P3UrbanBuildingHudView UrbanBuildingView => urbanBuildingView;
        public P3UrbanResultsView UrbanResultsView => urbanResultsView;

        public static P3CombatUIRoot EnsureExistsInScene(ApplicationServices services = null)
        {
            var existing = FindObjectOfType<P3CombatUIRoot>(true);
            if (existing == null)
            {
                var root = new GameObject(nameof(P3CombatUIRoot), typeof(RectTransform));
                existing = root.AddComponent<P3CombatUIRoot>();
            }

            existing.BuildIfNeeded();
            // Application composition is deliberately not inferred here. The
            // formal scene/service lease is task013's boundary; tests and tools
            // inject ITrenchService/IUrbanService explicitly through Initialize.
            return existing;
        }

        void Awake()
        {
            TrainingUIHost.EnsureExists();
            if (buildOnAwake) BuildIfNeeded();
        }

        void OnDestroy()
        {
            DisposePresenters();
        }

        public void BuildIfNeeded()
        {
            if (!built) Build();
        }

        public void Build()
        {
            if (built) return;
            built = true;
            gameObject.name = nameof(P3CombatUIRoot);
            AddTestId(gameObject, nameof(P3CombatUIRoot));
            BuildCanvas();

            BuildTrenchMapSelection();
            BuildTrenchBriefing();
            BuildTrenchHud();
            BuildTrenchResults();
            BuildUrbanMapSelection();
            BuildUrbanStreetHud();
            BuildUrbanBuildingHud();
            BuildUrbanResults();
            TacticalUIStyle.Apply(transform);
            SetAllPagesActive(false);
            if (showOnAwake) Show(VisibleScreen);
        }

        /// <summary>
        /// Binds the eight pages to the frozen P3 service contracts. This is the
        /// Fake-service entry used by UI tests and the seam consumed by task013.
        /// </summary>
        public void Initialize(ITrenchService trenchService, IUrbanService urbanService,
            IP3UiNavigationPort navigationPort, P3MapSelectionState selectionState = null,
            string weaponId = P3ContractIds.TrainingWeapon, RandomSeed seed = default,
            IHUDService trenchHudService = null, IHUDService urbanHudService = null,
            IP3BriefingVisualPort briefingVisual = null,
            IP3TrenchResultsActions trenchResultsActions = null,
            IP3UrbanResultsActions urbanResultsActions = null)
        {
            BuildIfNeeded();
            DisposePresenters();
            if (selectionState != null)
            {
                selection.TrenchMapId = selectionState.TrenchMapId;
                selection.UrbanMapId = selectionState.UrbanMapId;
                selection.TrenchSessionId = selectionState.TrenchSessionId;
                selection.UrbanSessionId = selectionState.UrbanSessionId;
            }

            navigation = new RootNavigation(this, navigationPort ?? throw new ArgumentNullException(nameof(navigationPort)));
            if (trenchService != null)
            {
                trenchMapPresenter = new P3TrenchMapSelectionPresenter();
                trenchMapPresenter.Initialize(trenchService, trenchMapView, navigation, selection);
                trenchBriefingPresenter = new P3TrenchBriefingPresenter();
                trenchBriefingPresenter.Initialize(trenchService, trenchBriefingView, navigation, selection,
                    weaponId, seed, briefingVisual ?? trenchBriefingView.GetComponent<P3BriefingDroneVisual>());

                if (!string.IsNullOrWhiteSpace(selection.TrenchSessionId))
                {
                    trenchHudPresenter = new P3TrenchHudPresenter();
                    trenchHudPresenter.Initialize(trenchService, trenchHudView, navigation, selection.TrenchSessionId, trenchHudService);
                    trenchResultsPresenter = new P3TrenchResultsPresenter();
                    trenchResultsPresenter.Initialize(trenchService, trenchResultsView, navigation, selection.TrenchSessionId, trenchResultsActions);
                }
            }

            if (urbanService != null)
            {
                urbanMapPresenter = new P3UrbanMapSelectionPresenter();
                urbanMapPresenter.Initialize(urbanService, urbanMapView, navigation, selection, weaponId, seed);
                if (!string.IsNullOrWhiteSpace(selection.UrbanSessionId))
                {
                    urbanStreetPresenter = new P3UrbanStreetHudPresenter();
                    urbanStreetPresenter.Initialize(urbanService, urbanStreetView, navigation, selection.UrbanSessionId, urbanHudService);
                    urbanBuildingPresenter = new P3UrbanBuildingHudPresenter();
                    urbanBuildingPresenter.Initialize(urbanService, urbanBuildingView, navigation, selection.UrbanSessionId, urbanHudService);
                    urbanResultsPresenter = new P3UrbanResultsPresenter();
                    urbanResultsPresenter.Initialize(urbanService, urbanResultsView, navigation, selection.UrbanSessionId, urbanResultsActions);
                }
            }

            Show(navigation.Current);
        }

        public void InitializeTrench(ITrenchService service, IP3UiNavigationPort navigationPort,
            P3MapSelectionState selectionState = null, string weaponId = P3ContractIds.TrainingWeapon,
            RandomSeed seed = default, IHUDService hud = null, IP3BriefingVisualPort visual = null,
            IP3TrenchResultsActions resultsActions = null)
        {
            Initialize(service, null, navigationPort, selectionState, weaponId, seed, hud, null, visual, resultsActions, null);
        }

        public void InitializeUrban(IUrbanService service, IP3UiNavigationPort navigationPort,
            P3MapSelectionState selectionState = null, string weaponId = P3ContractIds.TrainingWeapon,
            RandomSeed seed = default, IHUDService hud = null, IP3UrbanResultsActions resultsActions = null)
        {
            Initialize(null, service, navigationPort, selectionState, weaponId, seed, null, hud, null, null, resultsActions);
        }

        public bool Show(ScreenId screen)
        {
            BuildIfNeeded();
            if (!pages.ContainsKey(screen)) return false;
            SetAllPagesActive(false);
            pages[screen].SetActive(true);
            VisibleScreen = screen;
            return true;
        }

        void DisposePresenters()
        {
            trenchMapPresenter?.Dispose();
            trenchBriefingPresenter?.Dispose();
            trenchHudPresenter?.Dispose();
            trenchResultsPresenter?.Dispose();
            urbanMapPresenter?.Dispose();
            urbanStreetPresenter?.Dispose();
            urbanBuildingPresenter?.Dispose();
            urbanResultsPresenter?.Dispose();
            trenchMapPresenter = null;
            trenchBriefingPresenter = null;
            trenchHudPresenter = null;
            trenchResultsPresenter = null;
            urbanMapPresenter = null;
            urbanStreetPresenter = null;
            urbanBuildingPresenter = null;
            urbanResultsPresenter = null;
        }

        void BuildCanvas()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("P3CombatUIRoot requires a Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;
            var adapter = GetComponent<TrainingUICanvasAdapter>();
            if (adapter == null) adapter = gameObject.AddComponent<TrainingUICanvasAdapter>();
            adapter.Configure(canvas);
        }

        void BuildTrenchMapSelection()
        {
            var page = CreatePage(ScreenId.TrenchMapSelection, "堑壕射击 · 地图选择");
            var left = AddPanel(page, "Panel_TrenchMapSelection_Cards", new Vector2(80, 150), new Vector2(980, 930), new Color32(11, 25, 20, 240));
            var right = AddPanel(page, "Panel_TrenchMapSelection_Conditions", new Vector2(1040, 150), new Vector2(1840, 930), new Color32(11, 25, 20, 240));
            var cards = new List<P3MapCardView>();
            cards.Add(CreateMapCard(left, "TrenchA", "堑壕地图 A", new Vector2(55, 430), new Vector2(810, 680)));
            cards.Add(CreateUnavailableCard(left, "TrenchB", "堑壕地图 B（后续扩展）", new Vector2(55, 180), new Vector2(810, 385)));
            var selected = AddLabel(left, "Text_TrenchMapSelection_Selected", "当前选择：--", 28, new Vector2(55, 90), new Vector2(810, 150), TextAlignmentOptions.Center);
            var title = FindText(page, "Text_TrenchMapSelection_Title");
            var difficulty = AddLabel(right, "Text_TrenchMapSelection_Difficulty", "复杂度：--", 30, new Vector2(55, 585), new Vector2(745, 650), TextAlignmentOptions.Left);
            var enemies = AddLabel(right, "Text_TrenchMapSelection_Enemies", "敌人数量：--", 30, new Vector2(55, 510), new Vector2(745, 575), TextAlignmentOptions.Left);
            var conditions = AddLabel(right, "Text_TrenchMapSelection_Conditions", string.Empty, 25, new Vector2(55, 150), new Vector2(745, 475), TextAlignmentOptions.TopLeft);
            var error = AddLabel(right, "Text_TrenchMapSelection_Error", string.Empty, 20, new Vector2(55, 85), new Vector2(745, 140), TextAlignmentOptions.Left, new Color32(255, 120, 92, 255));
            var select = AddButton(page, "Button_TrenchMapSelection_SelectMap", "选择地图", new Vector2(1200, 60), new Vector2(1510, 125), true);
            var back = AddButton(page, "Button_TrenchMapSelection_Back", "返回", new Vector2(850, 60), new Vector2(1160, 125), false);
            trenchMapView = page.gameObject.AddComponent<P3TrenchMapSelectionView>();
            trenchMapView.Configure(title, selected, difficulty, enemies, conditions, error, select, back, cards);
        }

        void BuildTrenchBriefing()
        {
            var page = CreatePage(ScreenId.TrenchBriefing, "堑壕射击 · 开场任务");
            var projection = CreateMiniMap(page, "Hud_TrenchBriefing_Projection", new Vector2(80, 185), new Vector2(930, 875));
            var right = AddPanel(page, "Panel_TrenchBriefing_Information", new Vector2(990, 185), new Vector2(1840, 875), new Color32(11, 25, 20, 240));
            var map = AddLabel(right, "Text_TrenchBriefing_Map", "地图：--", 28, new Vector2(50, 665), new Vector2(800, 730), TextAlignmentOptions.Left);
            var enemy = AddLabel(right, "Text_TrenchBriefing_EnemyEstimate", "敌情预估：--", 28, new Vector2(50, 595), new Vector2(800, 655), TextAlignmentOptions.Left);
            var squad = AddLabel(right, "Text_TrenchBriefing_Squad", string.Empty, 23, new Vector2(50, 335), new Vector2(800, 570), TextAlignmentOptions.TopLeft);
            var objectives = AddLabel(right, "Text_TrenchBriefing_Objectives", string.Empty, 23, new Vector2(50, 95), new Vector2(800, 315), TextAlignmentOptions.TopLeft);
            var projectionState = AddLabel(page, "Text_TrenchBriefing_ProjectionState", "投影：--", 22, new Vector2(120, 135), new Vector2(890, 180), TextAlignmentOptions.Center, new Color32(247, 185, 85, 255));
            var error = AddLabel(page, "Text_TrenchBriefing_Error", string.Empty, 20, new Vector2(1000, 135), new Vector2(1800, 180), TextAlignmentOptions.Left, new Color32(255, 120, 92, 255));
            var back = AddButton(page, "Button_TrenchBriefing_Back", "返回地图选择", new Vector2(250, 60), new Vector2(590, 125), false);
            var view = AddButton(page, "Button_TrenchBriefing_ViewMap", "查看地图", new Vector2(710, 60), new Vector2(1040, 125), false);
            var start = AddButton(page, "Button_TrenchBriefing_Start", "开始进入", new Vector2(1160, 60), new Vector2(1510, 125), true);
            trenchBriefingView = page.gameObject.AddComponent<P3TrenchBriefingView>();
            trenchBriefingView.Configure(FindText(page, "Text_TrenchBriefing_Title"), map, enemy, squad, objectives,
                projectionState, error, start, view, back, projection);
            var droneBadge = Rect("Image_TrenchBriefing_ReconDrone", page, Vector2.zero, Vector2.zero, new Vector2(105, 900), new Vector2(170, 955));
            AddTestId(droneBadge.gameObject, droneBadge.name);
            AddLabel(page, "Text_TrenchBriefing_ReconLabel", "无人机侦察  /  战术投影", 22, new Vector2(195, 900), new Vector2(850, 955), TextAlignmentOptions.Left, new Color32(143, 217, 255, 255));
            page.gameObject.AddComponent<P3BriefingDroneVisual>().Configure(droneBadge);
        }

        void BuildTrenchHud()
        {
            var page = CreatePage(ScreenId.TrenchHud, "堑壕作战");
            var map = CreateMiniMap(page, "Hud_Trench_MiniMap", new Vector2(50, 625), new Vector2(560, 990));
            AddPanel(page, "Panel_TrenchHud_Left", new Vector2(50, 395), new Vector2(560, 605), new Color32(11, 25, 20, 225));
            var enemy = AddLabel(page, "Hud_Trench_EnemyProgress", "敌情：--", 25, new Vector2(80, 515), new Vector2(530, 565), TextAlignmentOptions.Left);
            var search = AddLabel(page, "Hud_Trench_SearchProgress", "搜索进度：--", 25, new Vector2(80, 450), new Vector2(530, 500), TextAlignmentOptions.Left);
            AddPanel(page, "Panel_TrenchHud_Right", new Vector2(1360, 585), new Vector2(1870, 990), new Color32(11, 25, 20, 225));
            var health = AddLabel(page, "Hud_Trench_Health", "生命：--", 26, new Vector2(1400, 915), new Vector2(1830, 965), TextAlignmentOptions.Left);
            var ammo = AddLabel(page, "Hud_Trench_Ammo", "弹药：--", 28, new Vector2(1400, 820), new Vector2(1830, 900), TextAlignmentOptions.Left);
            var posture = AddLabel(page, "Hud_Trench_Posture", "姿态：--", 24, new Vector2(1400, 765), new Vector2(1830, 810), TextAlignmentOptions.Left);
            var shoulder = AddLabel(page, "Hud_Trench_Shoulder", "射击肩：--", 24, new Vector2(1400, 710), new Vector2(1830, 755), TextAlignmentOptions.Left);
            var corner = AddLabel(page, "Hud_Trench_Corner", "拐角射击：--", 24, new Vector2(1400, 655), new Vector2(1830, 700), TextAlignmentOptions.Left);
            AddPanel(page, "Panel_TrenchHud_Squad", new Vector2(610, 25), new Vector2(1310, 190), new Color32(11, 25, 20, 225));
            var squad = AddLabel(page, "Hud_Trench_Squad", "小队：--", 22, new Vector2(600, 55), new Vector2(1260, 220), TextAlignmentOptions.Center);
            var prompt = AddLabel(page, "Hud_Trench_Prompt", string.Empty, 28, new Vector2(650, 260), new Vector2(1270, 325), TextAlignmentOptions.Center, new Color32(247, 185, 85, 255));
            var state = AddLabel(page, "Hud_Trench_State", "任务状态：--", 22, new Vector2(650, 220), new Vector2(1270, 255), TextAlignmentOptions.Center);
            trenchHudView = page.gameObject.AddComponent<P3TrenchHudView>();
            trenchHudView.Configure(health, ammo, posture, shoulder, corner, squad, prompt, state, map);
            trenchHudView.ConfigureTrench(enemy, search);
        }

        void BuildTrenchResults()
        {
            var page = CreatePage(ScreenId.TrenchResults, "堑壕射击 · 任务结算");
            var left = AddPanel(page, "Panel_TrenchResults_Statistics", new Vector2(80, 165), new Vector2(950, 900), new Color32(11, 25, 20, 240));
            var map = CreateMiniMap(page, "Hud_TrenchResults_ResultMap", new Vector2(1030, 400), new Vector2(1830, 900));
            var outcome = AddLabel(left, "Text_TrenchResults_Outcome", "--", 58, new Vector2(50, 735), new Vector2(800, 830), TextAlignmentOptions.Center, new Color32(200, 255, 106, 255));
            var summary = AddLabel(left, "Text_TrenchResults_Summary", string.Empty, 28, new Vector2(50, 640), new Vector2(800, 720), TextAlignmentOptions.Center);
            var stats = AddLabel(left, "Text_TrenchResults_Stats", string.Empty, 24, new Vector2(65, 90), new Vector2(785, 610), TextAlignmentOptions.TopLeft);
            var error = AddLabel(page, "Text_TrenchResults_Error", string.Empty, 20, new Vector2(100, 105), new Vector2(900, 150), TextAlignmentOptions.Left, new Color32(255, 120, 92, 255));
            var back = AddButton(page, "Button_TrenchResults_BackToMainMenu", "返回主菜单", new Vector2(1040, 230), new Vector2(1390, 300), false);
            var retry = AddButton(page, "Button_TrenchResults_Retry", "重新开始", new Vector2(1470, 230), new Vector2(1820, 300), true);
            trenchResultsView = page.gameObject.AddComponent<P3TrenchResultsView>();
            trenchResultsView.Configure(outcome, summary, stats, error, map, retry, back);
        }

        void BuildUrbanMapSelection()
        {
            var page = CreatePage(ScreenId.UrbanMapSelection, "城镇攻防 · 地图选择");
            var left = AddPanel(page, "Panel_UrbanMapSelection_Cards", new Vector2(80, 150), new Vector2(980, 930), new Color32(11, 25, 20, 240));
            var right = AddPanel(page, "Panel_UrbanMapSelection_Conditions", new Vector2(1040, 150), new Vector2(1840, 930), new Color32(11, 25, 20, 240));
            var cards = new List<P3MapCardView>();
            cards.Add(CreateUrbanMapCard(left, "UrbanA", new Vector2(55, 430), new Vector2(810, 680)));
            cards.Add(CreateUnavailableCard(left, "UrbanB", "城镇地图 B（后续扩展）", new Vector2(55, 180), new Vector2(810, 385)));
            var selected = AddLabel(left, "Text_UrbanMapSelection_Selected", "当前选择：--", 28, new Vector2(55, 90), new Vector2(810, 150), TextAlignmentOptions.Center);
            var enemies = AddLabel(right, "Text_UrbanMapSelection_Enemies", "敌情：--", 28, new Vector2(55, 590), new Vector2(745, 680), TextAlignmentOptions.Left);
            var floors = AddLabel(right, "Text_UrbanMapSelection_Floors", "建筑楼层：--", 28, new Vector2(55, 520), new Vector2(745, 580), TextAlignmentOptions.Left);
            var conditions = AddLabel(right, "Text_UrbanMapSelection_Conditions", string.Empty, 25, new Vector2(55, 170), new Vector2(745, 480), TextAlignmentOptions.TopLeft);
            var error = AddLabel(right, "Text_UrbanMapSelection_Error", string.Empty, 20, new Vector2(55, 90), new Vector2(745, 145), TextAlignmentOptions.Left, new Color32(255, 120, 92, 255));
            var select = AddButton(page, "Button_UrbanMapSelection_SelectMap", "选择地图", new Vector2(1200, 60), new Vector2(1510, 125), true);
            var back = AddButton(page, "Button_UrbanMapSelection_Back", "返回", new Vector2(850, 60), new Vector2(1160, 125), false);
            urbanMapView = page.gameObject.AddComponent<P3UrbanMapSelectionView>();
            urbanMapView.Configure(FindText(page, "Text_UrbanMapSelection_Title"), selected, enemies, floors, conditions, error, select, back, cards);
        }

        void BuildUrbanStreetHud()
        {
            var page = CreatePage(ScreenId.UrbanStreetHud, "城镇攻防 · 街道行动");
            AddPanel(page, "Panel_UrbanStreetHud_Right", new Vector2(1360, 630), new Vector2(1870, 990), new Color32(11, 25, 20, 225));
            var map = CreateMiniMap(page, "Hud_UrbanStreet_MiniMap", new Vector2(50, 625), new Vector2(560, 990));
            AddPanel(page, "Panel_UrbanStreetHud_Left", new Vector2(50, 365), new Vector2(560, 605), new Color32(11, 25, 20, 225));
            var streetState = AddLabel(page, "Hud_UrbanStreet_State", "街道状态：--", 24, new Vector2(80, 520), new Vector2(530, 570), TextAlignmentOptions.Left);
            var streetEnemies = AddLabel(page, "Hud_UrbanStreet_Enemies", "街道敌人：--", 24, new Vector2(80, 430), new Vector2(530, 510), TextAlignmentOptions.Left);
            var health = AddLabel(page, "Hud_UrbanStreet_Health", "生命：--", 26, new Vector2(1400, 915), new Vector2(1830, 965), TextAlignmentOptions.Left);
            var ammo = AddLabel(page, "Hud_UrbanStreet_Ammo", "弹药：--", 28, new Vector2(1400, 820), new Vector2(1830, 900), TextAlignmentOptions.Left);
            var posture = AddLabel(page, "Hud_UrbanStreet_Posture", "姿态：--", 24, new Vector2(1400, 765), new Vector2(1830, 810), TextAlignmentOptions.Left);
            var shoulder = AddLabel(page, "Hud_UrbanStreet_Shoulder", "射击肩：--", 24, new Vector2(1400, 710), new Vector2(1830, 755), TextAlignmentOptions.Left);
            var corner = AddLabel(page, "Hud_UrbanStreet_Corner", "拐角射击：--", 24, new Vector2(1400, 655), new Vector2(1830, 700), TextAlignmentOptions.Left);
            var squad = AddLabel(page, "Hud_UrbanStreet_Squad", "小队：--", 22, new Vector2(600, 30), new Vector2(1260, 105), TextAlignmentOptions.Center);
            var prompt = AddLabel(page, "Hud_UrbanStreet_Prompt", string.Empty, 28, new Vector2(650, 260), new Vector2(1270, 325), TextAlignmentOptions.Center, new Color32(247, 185, 85, 255));
            var state = AddLabel(page, "Hud_UrbanStreet_TaskState", "任务状态：--", 22, new Vector2(650, 220), new Vector2(1270, 255), TextAlignmentOptions.Center);
            var enter = AddButton(page, "Button_UrbanStreetHud_EnterBuilding", "进入建筑", new Vector2(760, 120), new Vector2(1160, 195), true);
            urbanStreetView = page.gameObject.AddComponent<P3UrbanStreetHudView>();
            urbanStreetView.Configure(health, ammo, posture, shoulder, corner, squad, prompt, state, map);
            urbanStreetView.ConfigureStreet(streetState, streetEnemies, enter);
        }

        void BuildUrbanBuildingHud()
        {
            var page = CreatePage(ScreenId.UrbanBuildingHud, "城镇攻防 · 建筑搜索");
            AddPanel(page, "Panel_UrbanBuildingHud_Right", new Vector2(1360, 630), new Vector2(1870, 990), new Color32(11, 25, 20, 225));
            var map = CreateMiniMap(page, "Hud_UrbanBuilding_MiniMap", new Vector2(50, 625), new Vector2(560, 990));
            AddPanel(page, "Panel_UrbanBuildingHud_Rooms", new Vector2(50, 275), new Vector2(560, 605), new Color32(11, 25, 20, 225));
            var floor = AddLabel(page, "Hud_UrbanBuilding_Floor", "当前楼层：--", 24, new Vector2(80, 530), new Vector2(530, 575), TextAlignmentOptions.Left);
            var rooms = AddLabel(page, "Hud_UrbanBuilding_Rooms", "房间：--", 21, new Vector2(80, 315), new Vector2(530, 515), TextAlignmentOptions.TopLeft);
            var progress = AddLabel(page, "Hud_UrbanBuilding_SearchProgress", "建筑搜索：--", 24, new Vector2(80, 215), new Vector2(530, 265), TextAlignmentOptions.Left);
            var health = AddLabel(page, "Hud_UrbanBuilding_Health", "生命：--", 26, new Vector2(1400, 915), new Vector2(1830, 965), TextAlignmentOptions.Left);
            var ammo = AddLabel(page, "Hud_UrbanBuilding_Ammo", "弹药：--", 28, new Vector2(1400, 820), new Vector2(1830, 900), TextAlignmentOptions.Left);
            var posture = AddLabel(page, "Hud_UrbanBuilding_Posture", "姿态：--", 24, new Vector2(1400, 765), new Vector2(1830, 810), TextAlignmentOptions.Left);
            var shoulder = AddLabel(page, "Hud_UrbanBuilding_Shoulder", "射击肩：--", 24, new Vector2(1400, 710), new Vector2(1830, 755), TextAlignmentOptions.Left);
            var corner = AddLabel(page, "Hud_UrbanBuilding_Corner", "拐角射击：--", 24, new Vector2(1400, 655), new Vector2(1830, 700), TextAlignmentOptions.Left);
            var squad = AddLabel(page, "Hud_UrbanBuilding_Squad", "小队：--", 22, new Vector2(600, 30), new Vector2(1260, 105), TextAlignmentOptions.Center);
            var prompt = AddLabel(page, "Hud_UrbanBuilding_Prompt", string.Empty, 28, new Vector2(650, 260), new Vector2(1270, 325), TextAlignmentOptions.Center, new Color32(247, 185, 85, 255));
            var state = AddLabel(page, "Hud_UrbanBuilding_TaskState", "任务状态：--", 22, new Vector2(650, 220), new Vector2(1270, 255), TextAlignmentOptions.Center);
            var open = AddButton(page, "Button_UrbanBuildingHud_OpenDoor", "开门", new Vector2(700, 120), new Vector2(990, 195), true);
            var check = AddButton(page, "Button_UrbanBuildingHud_CheckRoom", "检查房间", new Vector2(1010, 120), new Vector2(1300, 195), true);
            var exit = AddButton(page, "Button_UrbanBuildingHud_ExitBuilding", "返回街道", new Vector2(1350, 120), new Vector2(1640, 195), false);
            urbanBuildingView = page.gameObject.AddComponent<P3UrbanBuildingHudView>();
            urbanBuildingView.Configure(health, ammo, posture, shoulder, corner, squad, prompt, state, map);
            urbanBuildingView.ConfigureBuilding(floor, rooms, progress, open, check, exit);
        }

        void BuildUrbanResults()
        {
            var page = CreatePage(ScreenId.UrbanResults, "城镇攻防 · 任务结算");
            var left = AddPanel(page, "Panel_UrbanResults_Statistics", new Vector2(80, 155), new Vector2(950, 900), new Color32(11, 25, 20, 240));
            var maps = new List<P3MiniMapView>();
            maps.Add(CreateMiniMap(page, "Hud_UrbanResults_1F", new Vector2(1020, 610), new Vector2(1280, 880)));
            maps.Add(CreateMiniMap(page, "Hud_UrbanResults_2F", new Vector2(1305, 610), new Vector2(1565, 880)));
            maps.Add(CreateMiniMap(page, "Hud_UrbanResults_3F", new Vector2(1590, 610), new Vector2(1850, 880)));
            var outcome = AddLabel(left, "Text_UrbanResults_Outcome", "--", 58, new Vector2(50, 735), new Vector2(800, 830), TextAlignmentOptions.Center, new Color32(200, 255, 106, 255));
            var summary = AddLabel(left, "Text_UrbanResults_Summary", string.Empty, 28, new Vector2(50, 665), new Vector2(800, 725), TextAlignmentOptions.Center);
            var stats = AddLabel(left, "Text_UrbanResults_Stats", string.Empty, 23, new Vector2(65, 90), new Vector2(785, 630), TextAlignmentOptions.TopLeft);
            var error = AddLabel(page, "Text_UrbanResults_Error", string.Empty, 20, new Vector2(100, 100), new Vector2(900, 145), TextAlignmentOptions.Left, new Color32(255, 120, 92, 255));
            var labels = new[] { "Text_UrbanResults_1F", "Text_UrbanResults_2F", "Text_UrbanResults_3F" };
            for (var index = 0; index < labels.Length; index++) AddLabel(page, labels[index], (index + 1) + "F", 18, new Vector2(1020 + index * 285, 570), new Vector2(1280 + index * 285, 605), TextAlignmentOptions.Center);
            var back = AddButton(page, "Button_UrbanResults_BackToMainMenu", "返回主菜单", new Vector2(1040, 230), new Vector2(1390, 300), false);
            var retry = AddButton(page, "Button_UrbanResults_Retry", "重新开始", new Vector2(1470, 230), new Vector2(1820, 300), true);
            urbanResultsView = page.gameObject.AddComponent<P3UrbanResultsView>();
            urbanResultsView.Configure(outcome, summary, stats, error, maps, retry, back);
        }

        RectTransform CreatePage(ScreenId screen, string title)
        {
            var page = Rect("Screen_" + screen, transform as RectTransform, Vector2.one*.5f, Vector2.one*.5f, new Vector2(-960,-540), new Vector2(960,540));
            bool hud=screen==ScreenId.TrenchHud||screen==ScreenId.UrbanStreetHud||screen==ScreenId.UrbanBuildingHud;
            var frame=AddPanel(page, "Panel_" + screen + "_Frame", new Vector2(30, 30), new Vector2(1890, 1050), new Color32(7, 16, 13, (byte)(hud?0:242)));
            if(hud)frame.GetComponentInChildren<Image>().raycastTarget=false;
            var titleId = "Text_" + screen + "_Title";
            AddLabel(page, titleId, title, 43, new Vector2(80, 965), new Vector2(1840, 1035), TextAlignmentOptions.Center);
            AddTestId(page.gameObject, "Screen_" + screen);
            pages[screen] = page.gameObject;
            return page;
        }

        P3MapCardView CreateMapCard(RectTransform parent, string suffix, string name, Vector2 min, Vector2 max)
        {
            var card = AddPanel(parent, "Panel_TrenchMapSelection_" + suffix, min, max, new Color32(17, 36, 29, 245));
            var border = card.gameObject.AddComponent<Image>();
            border.color = new Color32(69, 224, 215, 255);
            border.sprite = TacticalUIStyle.Sprite("panel_holo_medium");
            border.type = Image.Type.Sliced;
            border.fillCenter = false;
            border.raycastTarget = false;
            border.enabled = false;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.Find(card.name + "_Image").GetComponent<Image>();
            TacticalUIStyle.Artwork(card, "Image_TrenchMapSelection_Preview", TacticalUITheme.Current != null ? TacticalUITheme.Current.Trench : null, new Vector2(25, 25), new Vector2(310, max.y - min.y - 25));
            var nameText = AddLabel(card, "Text_TrenchMapSelection_" + suffix + "_Name", name, 32, new Vector2(335, 115), new Vector2(max.x - min.x - 35, max.y - min.y - 30), TextAlignmentOptions.Center);
            var condition = AddLabel(card, "Text_TrenchMapSelection_" + suffix + "_Condition", string.Empty, 22, new Vector2(335, 30), new Vector2(max.x - min.x - 35, 105), TextAlignmentOptions.Center, new Color32(143, 217, 255, 255));
            var view = card.gameObject.AddComponent<P3MapCardView>();
            view.Configure(button, nameText, condition, border);
            view.SetMapId(P3ContractIds.TrenchMap);
            return view;
        }

        P3MapCardView CreateUrbanMapCard(RectTransform parent, string suffix, Vector2 min, Vector2 max)
        {
            var card = AddPanel(parent, "Panel_UrbanMapSelection_" + suffix, min, max, new Color32(17, 36, 29, 245));
            var border = card.gameObject.AddComponent<Image>();
            border.color = new Color32(69, 224, 215, 255);
            border.sprite = TacticalUIStyle.Sprite("panel_holo_medium");
            border.type = Image.Type.Sliced;
            border.fillCenter = false;
            border.raycastTarget = false;
            border.enabled = false;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.Find(card.name + "_Image").GetComponent<Image>();
            TacticalUIStyle.Artwork(card, "Image_UrbanMapSelection_Preview", TacticalUITheme.Current != null ? TacticalUITheme.Current.Urban : null, new Vector2(25, 25), new Vector2(310, max.y - min.y - 25));
            var nameText = AddLabel(card, "Text_UrbanMapSelection_" + suffix + "_Name", "城镇地图 A", 32, new Vector2(335, 115), new Vector2(max.x - min.x - 35, max.y - min.y - 30), TextAlignmentOptions.Center);
            var condition = AddLabel(card, "Text_UrbanMapSelection_" + suffix + "_Condition", string.Empty, 22, new Vector2(335, 30), new Vector2(max.x - min.x - 35, 105), TextAlignmentOptions.Center, new Color32(143, 217, 255, 255));
            var view = card.gameObject.AddComponent<P3MapCardView>();
            view.Configure(button, nameText, condition, border);
            view.SetMapId(P3ContractIds.UrbanMap);
            return view;
        }

        P3MapCardView CreateUnavailableCard(RectTransform parent, string suffix, string name, Vector2 min, Vector2 max)
        {
            var card = AddPanel(parent, "Panel_P3MapSelection_" + suffix, min, max, new Color32(15, 24, 21, 180));
            var border = card.gameObject.AddComponent<Image>();
            border.color = new Color32(58, 85, 75, 255);
            border.raycastTarget = false;
            border.enabled = false;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.Find(card.name + "_Image").GetComponent<Image>();
            var nameText = AddLabel(card, "Text_P3MapSelection_" + suffix + "_Name", name, 28, new Vector2(30, 80), new Vector2(max.x - min.x - 30, max.y - min.y - 30), TextAlignmentOptions.Center, new Color32(140, 155, 148, 255));
            var condition = AddLabel(card, "Text_P3MapSelection_" + suffix + "_Condition", "当前版本未开放", 20, new Vector2(30, 30), new Vector2(max.x - min.x - 30, 75), TextAlignmentOptions.Center, new Color32(140, 155, 148, 255));
            var view = card.gameObject.AddComponent<P3MapCardView>();
            view.Configure(button, nameText, condition, border);
            view.ApplyUnavailable(name);
            return view;
        }

        P3MiniMapView CreateMiniMap(RectTransform parent, string id, Vector2 min, Vector2 max)
        {
            var panel = AddPanel(parent, id, min, max, new Color32(18, 35, 31, 240));
            var background = AddImage(panel, id + "_Background", new Vector2(28, 58), new Vector2(max.x - min.x - 28, max.y - min.y - 35), new Color32(22, 45, 41, 235));
            background.raycastTarget = false;
            var label = AddLabel(panel, id + "_Label", "平面图", 18, new Vector2(28, 20), new Vector2(max.x - min.x - 28, 52), TextAlignmentOptions.Left, new Color32(143, 217, 255, 255));
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = 18;
            label.enableWordWrapping = false;
            var areaLayer = Rect("Areas", background.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var markerLayer = Rect("Markers", background.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = panel.gameObject.AddComponent<P3MiniMapView>();
            view.Configure(background, label, areaLayer, markerLayer);
            return view;
        }

        TMP_Text FindText(RectTransform parent, string id)
        {
            var all = parent.GetComponentsInChildren<UITestId>(true);
            for (var index = 0; index < all.Length; index++)
            {
                if (all[index].Id == id) return all[index].GetComponent<TMP_Text>();
            }
            return null;
        }

        void SetAllPagesActive(bool active)
        {
            foreach (var pair in pages) pair.Value.SetActive(active);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
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

        static RectTransform AddPanel(RectTransform parent, string id, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(id, parent, Vector2.zero, Vector2.zero, min, max);
            AddImage(rect, id + "_Image", Vector2.zero, max - min, color);
            AddTestId(rect.gameObject, id);
            return rect;
        }

        static Image AddImage(RectTransform parent, string id, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(id, parent, Vector2.zero, Vector2.zero, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            AddTestId(rect.gameObject, id);
            return image;
        }

        static TMP_Text AddLabel(RectTransform parent, string id, string value, float size, Vector2 min, Vector2 max,
            TextAlignmentOptions alignment)
        {
            return AddLabel(parent, id, value, size, min, max, alignment, new Color32(231, 242, 235, 255));
        }

        static TMP_Text AddLabel(RectTransform parent, string id, string value, float size, Vector2 min, Vector2 max,
            TextAlignmentOptions alignment, Color color)
        {
            var rect = Rect(id, parent, Vector2.zero, Vector2.zero, min, max);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            AddTestId(rect.gameObject, id);
            return text;
        }

        static Button AddButton(RectTransform parent, string id, string label, Vector2 min, Vector2 max, bool primary)
        {
            var rect = Rect(id, parent, Vector2.zero, Vector2.zero, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = primary ? new Color32(45, 156, 255, 255) : new Color32(24, 45, 39, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            AddLabel(rect, id + "_Label", label, 23, Vector2.zero, max - min, TextAlignmentOptions.Center, new Color32(240, 247, 242, 255));
            AddTestId(rect.gameObject, id);
            return button;
        }

        static void AddTestId(GameObject target, string id)
        {
            var testId = target.GetComponent<VRShooting.Unity.UITestId>() ?? target.AddComponent<VRShooting.Unity.UITestId>();
            testId.SetId(id);
        }

        sealed class RootNavigation : IP3UiNavigationPort
        {
            readonly P3CombatUIRoot root;
            readonly IP3UiNavigationPort inner;

            public RootNavigation(P3CombatUIRoot owner, IP3UiNavigationPort wrapped)
            {
                root = owner;
                inner = wrapped;
            }

            public ScreenId Current => inner.Current;

            public ServiceResult<ScreenId> Open(ScreenId screen, NavigationArgs args = default)
            {
                var result = inner.Open(screen, args);
                if (result.Success) root.Show(screen);
                return result;
            }

            public ServiceResult<ScreenId> Back()
            {
                var result = inner.Back();
                if (result.Success) root.Show(result.Data);
                return result;
            }
        }
    }
}
