using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    /// <summary>
    /// P3 UI line tests. They intentionally use the frozen FakeTrench/FakeUrban
    /// services and exercise DTO rendering plus command/event boundaries only.
    /// BDD references are recorded in each test name and the task delivery notes.
    /// </summary>
    [TestFixture]
    public sealed class P3UiTask002_005_008_011PlayModeTests
    {
        readonly List<GameObject> objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null) UnityEngine.Object.Destroy(objects[index]);
            }
            objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen12_TrenchMapSelection_DefaultDtoAndSelectOnlyRoutesBriefing()
        {
            using (var service = new FakeTrenchService())
            {
                var view = CreateTrenchMapView();
                var navigation = new ProbeNavigation(ScreenId.TrenchMapSelection);
                var state = new P3MapSelectionState();
                var presenter = new P3TrenchMapSelectionPresenter();
                presenter.Initialize(service, view, navigation, state);
                yield return null;

                Assert.That(view.SelectedMapId, Is.EqualTo(P3ContractIds.TrenchMap));
                Assert.That(Text(view, "selected").text, Does.Contain("堑壕地图 A"));
                Assert.That(Text(view, "difficulty").text, Does.Contain("中"));
                Assert.That(Text(view, "enemy").text, Does.Contain("3-5"));
                Assert.That(Text(view, "conditions").text, Does.Contain("搜索完整堑壕"));
                Assert.That(Text(view, "conditions").text, Does.Contain("失败条件：玩家死亡"));

                view.SelectButton.onClick.Invoke();
                Assert.That(service.Commands, Does.Contain("SelectMap:" + P3ContractIds.TrenchMap));
                Assert.That(service.Commands.Any(command => command.StartsWith("StartSession:", StringComparison.Ordinal)), Is.False);
                Assert.That(navigation.Current, Is.EqualTo(ScreenId.TrenchBriefing));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Screen14_TrenchBriefing_PlaysPreviewAndStartsOnlyOnCommand()
        {
            using (var service = new FakeTrenchService())
            {
                service.Bind(P3Fixtures.TrenchSession("trench-ui", 1));
                var view = CreateTrenchBriefingView(out var visual);
                var navigation = new ProbeNavigation(ScreenId.TrenchBriefing);
                var presenter = new P3TrenchBriefingPresenter();
                presenter.Initialize(service, view, navigation, new P3MapSelectionState(), P3ContractIds.TrainingWeapon,
                    RandomSeed.Fixed(7), visual);
                yield return null;

                Assert.That(visual.PlayCount, Is.EqualTo(1));
                Assert.That(Text(view, "objectives").text, Does.Contain("保持小队队形"));
                Assert.That(Text(view, "squad").text, Does.Contain("玩家"));
                Assert.That(Text(view, "squad").text, Does.Contain("二号队友"));
                Assert.That(service.Commands.Any(command => command.StartsWith("StartSession:", StringComparison.Ordinal)), Is.False);

                view.ViewMapButton.onClick.Invoke();
                Assert.That(view.MapFocused, Is.True);
                view.ViewMapButton.onClick.Invoke();
                Assert.That(view.MapFocused, Is.False);
                view.StartButton.onClick.Invoke();
                Assert.That(service.Commands, Does.Contain("StartSession:" + P3ContractIds.TrenchMap));
                Assert.That(navigation.Current, Is.EqualTo(ScreenId.TrenchHud));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Screen15_TrenchHud_RefreshesDtoAndRejectsOldSessionEvents()
        {
            using (var service = new FakeTrenchService())
            {
                service.Bind(P3Fixtures.TrenchSession("trench-hud", 1));
                var view = CreateTrenchHudView();
                var navigation = new ProbeNavigation(ScreenId.TrenchHud);
                var presenter = new P3TrenchHudPresenter();
                presenter.Initialize(service, view, navigation, "trench-hud");
                yield return null;

                Assert.That(Text(view, "ammo").text, Does.Contain("30 / 120"));
                Assert.That(Text(view, "health").text, Does.Contain("100"));
                Assert.That(view.MiniMap.RenderedMarkerCount, Is.EqualTo(1));
                Assert.That(Text(view, "posture").text, Does.Contain("起立"));

                var changed = P3Fixtures.TrenchSession("trench-hud", 2);
                changed = new TrenchSessionDto
                {
                    SessionId = changed.SessionId, Revision = changed.Revision, MapId = changed.MapId,
                    State = changed.State, EnemyTotal = changed.EnemyTotal, EnemyKilled = 1,
                    SearchProgress01 = 0.5f,
                    Ammo = new AmmoDto { CurrentMagazine = 12, ReserveAmmo = 80, MagazineCapacity = 30 },
                    Player = new PlayerStatusDto { Health = 42, IsAlive = true, Posture = PlayerPosture.Crouching,
                        Shoulder = ShoulderSide.Left, CornerShootingAvailable = true },
                    Squad = changed.Squad, MiniMap = changed.MiniMap
                };
                Assert.That(service.Publish(changed), Is.True);
                Assert.That(Text(view, "ammo").text, Does.Contain("12 / 80"));
                Assert.That(Text(view, "health").text, Does.Contain("42"));
                Assert.That(Text(view, "posture").text, Does.Contain("蹲下"));
                Assert.That(Text(view, "shoulder").text, Does.Contain("左肩"));
                Assert.That(Text(view, "corner").text, Does.Contain("可用"));

                Assert.That(service.Publish(P3Fixtures.TrenchSession("old", 99)), Is.False);
                presenter.Dispose();
                Assert.That(service.SubscriberCount, Is.EqualTo(0));
            }
        }

        [UnityTest]
        public IEnumerator Screen17_TrenchResults_RendersPartialDtoAndRetryIsIdempotent()
        {
            using (var service = new FakeTrenchService())
            {
                service.Bind(P3Fixtures.TrenchSession("trench-result", 1));
                var view = CreateTrenchResultsView();
                var navigation = new ProbeNavigation(ScreenId.TrenchResults);
                var presenter = new P3TrenchResultsPresenter();
                presenter.Initialize(service, view, navigation, "trench-result");
                yield return null;

                var result = new TrenchResultDto
                {
                    SessionId = "trench-result", Revision = 3, Victory = false, MapName = "堑壕地图 A",
                    EnemyKilled = 1, EnemyTotal = 3, SearchProgress01 = 0.5f, RemainingAmmo = 92,
                    Squad = P3Fixtures.Squad("trench-result"), ElapsedSeconds = 75f,
                    ResultMap = P3Fixtures.MiniMap(P3ContractIds.TrenchMap)
                };
                Assert.That(service.SetResult(result), Is.True);
                Assert.That(Text(view, "outcome").text, Does.Contain("失败"));
                Assert.That(Text(view, "summary").text, Does.Contain("堑壕地图 A"));
                Assert.That(Text(view, "stats").text, Does.Contain("1 / 3"));
                Assert.That(Text(view, "stats").text, Does.Contain("50%"));
                Assert.That(Text(view, "stats").text, Does.Contain("剩余弹药：92"));

                view.RetryButton.onClick.Invoke();
                view.RetryButton.onClick.Invoke();
                Assert.That(service.Commands.Count(command => command == "Cancel:trench-result"), Is.EqualTo(1));
                Assert.That(navigation.Current, Is.EqualTo(ScreenId.TrenchBriefing));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Screen18_UrbanMapSelection_RendersRangesAndStartsStreet()
        {
            using (var service = new FakeUrbanService())
            {
                service.Bind(P3Fixtures.UrbanSession("urban-map", 1));
                var view = CreateUrbanMapView();
                var navigation = new ProbeNavigation(ScreenId.UrbanMapSelection);
                var presenter = new P3UrbanMapSelectionPresenter();
                presenter.Initialize(service, view, navigation, new P3MapSelectionState(), P3ContractIds.TrainingWeapon, RandomSeed.Fixed(9));
                yield return null;

                Assert.That(view.SelectedMapId, Is.EqualTo(P3ContractIds.UrbanMap));
                Assert.That(Text(view, "selected").text, Does.Contain("城镇地图 A"));
                Assert.That(Text(view, "enemy").text, Does.Contain("1-2"));
                Assert.That(Text(view, "enemy").text, Does.Contain("3-6"));
                Assert.That(Text(view, "conditions").text, Does.Contain("搜索楼房"));
                view.SelectButton.onClick.Invoke();
                CollectionAssert.AreEqual(
                    new[] { "SelectMap:" + P3ContractIds.UrbanMap, "StartSession:" + P3ContractIds.UrbanMap },
                    service.Commands.Take(2).ToArray());
                Assert.That(navigation.Current, Is.EqualTo(ScreenId.UrbanStreetHud));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Screen19_UrbanStreetHud_UsesPromptDtoAndKeepsRiskVisible()
        {
            using (var service = new FakeUrbanService())
            {
                service.Bind(P3Fixtures.UrbanSession("urban-street", 1));
                var view = CreateUrbanStreetView();
                var navigation = new ProbeNavigation(ScreenId.UrbanStreetHud);
                var presenter = new P3UrbanStreetHudPresenter();
                presenter.Initialize(service, view, navigation, "urban-street");
                yield return null;

                var session = P3Fixtures.UrbanSession("urban-street", 1);
                var hud = new HudDto
                {
                    SessionId = "urban-street", Mode = TrainingMode.Urban, HudType = HudType.UrbanStreet,
                    Ammo = session.Ammo, Player = session.Player, MiniMap = session.MiniMap,
                    Prompts = new[] { new HudPromptDto { PromptId = "StreetRisk", Text = "街道尚有敌人，任务仍需清除街道" },
                        new HudPromptDto { PromptId = "EnterBuilding", Text = "建筑入口 / 进入建筑", IsInteractive = true, IsEnabled = true } }
                };
                view.Apply(session, hud);
                Assert.That(Text(view, "streetState").text, Does.Contain("尚有敌人"));
                Assert.That(Text(view, "prompt").text, Does.Contain("建筑入口"));
                Assert.That(view.EnterBuildingInteractive, Is.True);
                view.EnterBuildingButton.onClick.Invoke();
                Assert.That(service.Commands, Does.Contain("EnterBuilding:urban-a.entrance"));
                Assert.That(navigation.Current, Is.EqualTo(ScreenId.UrbanBuildingHud));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands()
        {
            using (var service = new FakeUrbanService())
            {
                service.Bind(P3Fixtures.UrbanSession("urban-building", 1, UrbanPhase.Building));
                var view = CreateUrbanBuildingView();
                var navigation = new ProbeNavigation(ScreenId.UrbanBuildingHud);
                var presenter = new P3UrbanBuildingHudPresenter();
                presenter.Initialize(service, view, navigation, "urban-building");
                yield return null;

                var baseSession = P3Fixtures.UrbanSession("urban-building", 1, UrbanPhase.Building);
                var openHud = BuildingHud(baseSession, "urban-a.room-001.OpenDoor", "开门", true);
                view.Apply(baseSession, openHud);
                Assert.That(Text(view, "rooms").text, Does.Contain("未搜索"));
                view.OpenDoorButton.onClick.Invoke();
                Assert.That(service.Commands, Does.Contain("OpenRoomDoor:urban-a.room-001"));
                // The fake returns the unchanged frame. UI must not infer that the
                // door animation/search completed just because the command succeeded.
                Assert.That(view.LastSession.Floors[0].Rooms[0].SearchState, Is.EqualTo(RoomSearchState.Unsearched));

                var changedFloor = new FloorDto
                {
                    FloorId = baseSession.Floors[0].FloorId, DisplayName = baseSession.Floors[0].DisplayName,
                    MiniMap = baseSession.Floors[0].MiniMap,
                    Rooms = new[] { new RoomDto { RoomId = "urban-a.room-001", DisplayName = "房间1",
                        SearchState = RoomSearchState.Searching, DoorOpen = true } }
                };
                var changed = new UrbanSessionDto
                {
                    SessionId = baseSession.SessionId, Revision = 2, MapId = baseSession.MapId, State = baseSession.State,
                    Phase = UrbanPhase.Building, StreetCleared = false, StreetEnemyTotal = 2,
                    BuildingEnemyTotal = 3, RoomsSearched = 0, RoomsTotal = 3, Ammo = baseSession.Ammo,
                    Player = baseSession.Player, Squad = baseSession.Squad, MiniMap = baseSession.MiniMap,
                    CurrentFloorId = baseSession.CurrentFloorId, Floors = new[] { changedFloor }
                };
                var checkHud = BuildingHud(changed, "urban-a.room-001.CheckRoom", "检查房间", true);
                view.Apply(changed, checkHud);
                view.CheckRoomButton.onClick.Invoke();
                Assert.That(service.Commands, Does.Contain("MarkRoomSearched:urban-a.room-001"));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce()
        {
            using (var service = new FakeUrbanService())
            {
                service.Bind(P3Fixtures.UrbanSession("urban-result", 1, UrbanPhase.Building));
                var view = CreateUrbanResultsView();
                var navigation = new ProbeNavigation(ScreenId.UrbanResults);
                var presenter = new P3UrbanResultsPresenter();
                presenter.Initialize(service, view, navigation, "urban-result");
                yield return null;

                var result = new UrbanResultDto
                {
                    SessionId = "urban-result", Revision = 3, Victory = true, StreetCleared = true,
                    BuildingSearchProgress01 = 1f, RoomsSearched = 3, RoomsTotal = 3,
                    EnemyKilled = 5, EnemyTotal = 5, RemainingAmmo = 76,
                    Squad = P3Fixtures.Squad("urban-result"), ElapsedSeconds = 120f,
                    ResultMap = P3Fixtures.MiniMap(P3ContractIds.UrbanMap),
                    FloorMaps = new[] { P3Fixtures.MiniMap("urban-a.floor-1"), P3Fixtures.MiniMap("urban-a.floor-2"), P3Fixtures.MiniMap("urban-a.floor-3") }
                };
                Assert.That(service.SetResult(result), Is.True);
                Assert.That(Text(view, "outcome").text, Does.Contain("胜利"));
                Assert.That(Text(view, "stats").text, Does.Contain("街道清除：已清除"));
                Assert.That(Text(view, "stats").text, Does.Contain("100%"));
                Assert.That(view.RenderedFloorMapCount, Is.EqualTo(3));
                view.RetryButton.onClick.Invoke();
                view.RetryButton.onClick.Invoke();
                Assert.That(service.Commands.Count(command => command == "Cancel:urban-result"), Is.EqualTo(1));
                Assert.That(navigation.Current, Is.EqualTo(ScreenId.UrbanMapSelection));
                presenter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator P3UiRoot_BuildsAllPagesAndFrozenTestIds()
        {
            var root = Track(new GameObject("P3UiRootTest", typeof(RectTransform)));
            root.SetActive(false);
            var ui = root.AddComponent<P3CombatUIRoot>();
            ui.Build();
            var ids = root.GetComponentsInChildren<UITestId>(true).Select(item => item.Id).ToHashSet();
            var required = new[]
            {
                "Screen_TrenchMapSelection", "Screen_TrenchBriefing", "Screen_TrenchHud", "Screen_TrenchResults",
                "Screen_UrbanMapSelection", "Screen_UrbanStreetHud", "Screen_UrbanBuildingHud", "Screen_UrbanResults",
                "Button_TrenchMapSelection_SelectMap", "Button_TrenchBriefing_Start", "Button_TrenchBriefing_ViewMap",
                "Button_TrenchResults_Retry", "Button_TrenchResults_BackToMainMenu",
                "Hud_Trench_Health", "Hud_Trench_Ammo", "Hud_Trench_MiniMap", "Hud_Trench_Squad",
                "Button_UrbanMapSelection_SelectMap", "Button_UrbanStreetHud_EnterBuilding",
                "Button_UrbanBuildingHud_OpenDoor", "Button_UrbanBuildingHud_CheckRoom",
                "Button_UrbanResults_Retry", "Button_UrbanResults_BackToMainMenu",
                "Hud_UrbanBuilding_MiniMap", "Hud_UrbanBuilding_Rooms", "Text_UrbanResults_Summary"
            };
            for (var index = 0; index < required.Length; index++) Assert.That(ids, Does.Contain(required[index]), required[index]);
            Assert.That(ui.IsBuilt, Is.True);
            Assert.That(ui.Show(ScreenId.TrenchHud), Is.True);
            Assert.That(ui.VisibleScreen, Is.EqualTo(ScreenId.TrenchHud));
            yield return null;
        }

        P3TrenchMapSelectionView CreateTrenchMapView()
        {
            var root = Track(new GameObject("TrenchMapView", typeof(RectTransform)));
            var card = CreateCard(root.transform, P3ContractIds.TrenchMap, "堑壕地图 A");
            var selected = Label(root.transform, "selected");
            var difficulty = Label(root.transform, "difficulty");
            var enemy = Label(root.transform, "enemy");
            var conditions = Label(root.transform, "conditions");
            var error = Label(root.transform, "error");
            var select = Button(root.transform, "select");
            var back = Button(root.transform, "back");
            var view = root.AddComponent<P3TrenchMapSelectionView>();
            view.Configure(Label(root.transform, "title"), selected, difficulty, enemy, conditions, error, select, back, new[] { card });
            return view;
        }

        P3TrenchBriefingView CreateTrenchBriefingView(out P3BriefingDroneVisual visual)
        {
            var root = Track(new GameObject("TrenchBriefingView", typeof(RectTransform)));
            var map = CreateMiniMap(root.transform, "briefing-map");
            var view = root.AddComponent<P3TrenchBriefingView>();
            view.Configure(Label(root.transform, "title"), Label(root.transform, "map"), Label(root.transform, "enemy"),
                Label(root.transform, "squad"), Label(root.transform, "objectives"), Label(root.transform, "projection"),
                Label(root.transform, "error"), Button(root.transform, "start"), Button(root.transform, "view"),
                Button(root.transform, "back"), map);
            visual = root.AddComponent<P3BriefingDroneVisual>();
            return view;
        }

        P3TrenchHudView CreateTrenchHudView()
        {
            var root = Track(new GameObject("TrenchHudView", typeof(RectTransform)));
            var view = root.AddComponent<P3TrenchHudView>();
            view.Configure(Label(root.transform, "health"), Label(root.transform, "ammo"), Label(root.transform, "posture"),
                Label(root.transform, "shoulder"), Label(root.transform, "corner"), Label(root.transform, "squad"),
                Label(root.transform, "prompt"), Label(root.transform, "state"), CreateMiniMap(root.transform, "hud-map"));
            view.ConfigureTrench(Label(root.transform, "enemy-progress"), Label(root.transform, "search"));
            return view;
        }

        P3TrenchResultsView CreateTrenchResultsView()
        {
            var root = Track(new GameObject("TrenchResultsView", typeof(RectTransform)));
            var view = root.AddComponent<P3TrenchResultsView>();
            view.Configure(Label(root.transform, "outcome"), Label(root.transform, "summary"), Label(root.transform, "stats"),
                Label(root.transform, "error"), CreateMiniMap(root.transform, "result-map"), Button(root.transform, "retry"), Button(root.transform, "back"));
            return view;
        }

        P3UrbanMapSelectionView CreateUrbanMapView()
        {
            var root = Track(new GameObject("UrbanMapView", typeof(RectTransform)));
            var card = CreateCard(root.transform, P3ContractIds.UrbanMap, "城镇地图 A");
            var view = root.AddComponent<P3UrbanMapSelectionView>();
            view.Configure(Label(root.transform, "title"), Label(root.transform, "selected"), Label(root.transform, "enemy"),
                Label(root.transform, "floors"), Label(root.transform, "conditions"), Label(root.transform, "error"),
                Button(root.transform, "select"), Button(root.transform, "back"), new[] { card });
            return view;
        }

        P3UrbanStreetHudView CreateUrbanStreetView()
        {
            var root = Track(new GameObject("UrbanStreetView", typeof(RectTransform)));
            var view = root.AddComponent<P3UrbanStreetHudView>();
            view.Configure(Label(root.transform, "health"), Label(root.transform, "ammo"), Label(root.transform, "posture"),
                Label(root.transform, "shoulder"), Label(root.transform, "corner"), Label(root.transform, "squad"),
                Label(root.transform, "prompt"), Label(root.transform, "state"), CreateMiniMap(root.transform, "street-map"));
            view.ConfigureStreet(Label(root.transform, "streetState"), Label(root.transform, "streetEnemies"), Button(root.transform, "enter"));
            return view;
        }

        P3UrbanBuildingHudView CreateUrbanBuildingView()
        {
            var root = Track(new GameObject("UrbanBuildingView", typeof(RectTransform)));
            var view = root.AddComponent<P3UrbanBuildingHudView>();
            view.Configure(Label(root.transform, "health"), Label(root.transform, "ammo"), Label(root.transform, "posture"),
                Label(root.transform, "shoulder"), Label(root.transform, "corner"), Label(root.transform, "squad"),
                Label(root.transform, "prompt"), Label(root.transform, "state"), CreateMiniMap(root.transform, "building-map"));
            view.ConfigureBuilding(Label(root.transform, "floor"), Label(root.transform, "rooms"), Label(root.transform, "progress"),
                Button(root.transform, "open"), Button(root.transform, "check"), Button(root.transform, "exit"));
            return view;
        }

        P3UrbanResultsView CreateUrbanResultsView()
        {
            var root = Track(new GameObject("UrbanResultsView", typeof(RectTransform)));
            var view = root.AddComponent<P3UrbanResultsView>();
            view.Configure(Label(root.transform, "outcome"), Label(root.transform, "summary"), Label(root.transform, "stats"),
                Label(root.transform, "error"), new[] { CreateMiniMap(root.transform, "1f"), CreateMiniMap(root.transform, "2f"), CreateMiniMap(root.transform, "3f") },
                Button(root.transform, "retry"), Button(root.transform, "back"));
            return view;
        }

        static HudDto BuildingHud(UrbanSessionDto session, string promptId, string text, bool enabled)
        {
            return new HudDto
            {
                SessionId = session.SessionId, Mode = TrainingMode.Urban, HudType = HudType.UrbanBuilding,
                Ammo = session.Ammo, Player = session.Player, MiniMap = session.MiniMap,
                Prompts = new[] { new HudPromptDto { PromptId = promptId, Text = text, IsInteractive = true, IsEnabled = enabled } }
            };
        }

        GameObject Track(GameObject value) { objects.Add(value); return value; }

        static TMP_Text Label(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            return go.GetComponent<TextMeshProUGUI>();
        }

        static Button Button(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        P3MapCardView CreateCard(Transform parent, string mapId, string name)
        {
            var go = Track(new GameObject(name + "Card", typeof(RectTransform), typeof(Image), typeof(Button)));
            go.transform.SetParent(parent, false);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var border = Track(new GameObject("Border", typeof(RectTransform), typeof(Image))).GetComponent<Image>();
            border.transform.SetParent(go.transform, false);
            var card = go.AddComponent<P3MapCardView>();
            card.Configure(button, Label(go.transform, "name"), Label(go.transform, "condition"), border);
            card.SetMapId(mapId);
            return card;
        }

        static P3MiniMapView CreateMiniMap(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(P3MiniMapView));
            root.transform.SetParent(parent, false);
            var background = root.GetComponent<Image>();
            var label = Label(root.transform, "map-label");
            var areas = new GameObject("Areas", typeof(RectTransform)).GetComponent<RectTransform>();
            areas.transform.SetParent(root.transform, false);
            var markers = new GameObject("Markers", typeof(RectTransform)).GetComponent<RectTransform>();
            markers.transform.SetParent(root.transform, false);
            var view = root.GetComponent<P3MiniMapView>();
            view.Configure(background, label, areas, markers);
            return view;
        }

        static TMP_Text Text(MonoBehaviour owner, string logicalName)
        {
            return owner.GetComponentsInChildren<TMP_Text>(true).First(text => text.gameObject.name == logicalName);
        }

        sealed class ProbeNavigation : IP3UiNavigationPort
        {
            public ProbeNavigation(ScreenId initial) { Current = initial; }
            public ScreenId Current { get; private set; }
            public ServiceResult<ScreenId> Open(ScreenId screen, NavigationArgs args = default)
            {
                Current = screen;
                return ServiceResult<ScreenId>.Ok(screen);
            }
            public ServiceResult<ScreenId> Back()
            {
                Current = ScreenId.MainMenu;
                return ServiceResult<ScreenId>.Ok(Current);
            }
        }
    }
}
