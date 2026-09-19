using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Navigation boundary used by the P3 UI. Views never receive a scene, actor,
    /// collider or service implementation; presenters only depend on this port.
    /// </summary>
    public interface IP3UiNavigationPort
    {
        ScreenId Current { get; }
        ServiceResult<ScreenId> Open(ScreenId screen, NavigationArgs args = default);
        ServiceResult<ScreenId> Back();
    }

    public sealed class P3RouterNavigationPort : IP3UiNavigationPort
    {
        readonly IUIRouter router;

        public P3RouterNavigationPort(IUIRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public ScreenId Current => router.Current;

        public ServiceResult<ScreenId> Open(ScreenId screen, NavigationArgs args = default)
            => router.Open(screen, args);

        public ServiceResult<ScreenId> Back() => router.Back();
    }

    [Serializable]
    public sealed class P3MapResourceBinding
    {
        [SerializeField]
        string mapId = string.Empty;

        [SerializeField]
        Sprite sprite;

        public string MapId => mapId ?? string.Empty;
        public Sprite Sprite => sprite;
        public P3MapResourceBinding(string id,Sprite value){mapId=id;sprite=value;}
    }

    /// <summary>
    /// DTO-only minimap renderer shared by trench, street, building and result UI.
    /// It deliberately renders only normalized estimate/area data from the DTO.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class P3MiniMapView : MonoBehaviour
    {
        [SerializeField]
        Image mapImage;

        [SerializeField]
        TMP_Text mapLabel;

        [SerializeField]
        RectTransform areaLayer;

        [SerializeField]
        RectTransform markerLayer;

        [SerializeField]
        List<P3MapResourceBinding> mapResources = new List<P3MapResourceBinding>();

        readonly List<GameObject> renderedAreas = new List<GameObject>();
        readonly List<GameObject> renderedMarkers = new List<GameObject>();

        public MiniMapDto LastMap { get; private set; }
        public int RenderedAreaCount => renderedAreas.Count;
        public int RenderedMarkerCount => renderedMarkers.Count;
        public void SetMapResource(string id,Sprite sprite)
        {
            mapResources.RemoveAll(b=>b!=null&&b.MapId==id);
            mapResources.Add(new P3MapResourceBinding(id,sprite));
        }

        public void Configure(Image background, TMP_Text label, RectTransform areas, RectTransform markers)
        {
            mapImage = background;
            mapLabel = label;
            areaLayer = areas;
            markerLayer = markers;
        }

        public void Apply(MiniMapDto map)
        {
            LastMap = map;
            Clear(renderedAreas);
            Clear(renderedMarkers);

            if (mapImage != null)
            {
                mapImage.sprite = ResolveSprite(map.MapId);
                mapImage.color = map.Visible && mapImage.sprite != null ? Color.white : new Color32(22, 30, 29, 160);
                mapImage.enabled = true;
            }

            if (mapLabel != null)
            {
                mapLabel.text = map.Visible
                    ? "▲我方  ○队友  ◇预估  ×击杀"
                    : "平面图不可用";
            }

            if (!map.Visible)
            {
                return;
            }

            var areas = map.Areas ?? Array.Empty<MapAreaDto>();
            for (var index = 0; index < areas.Count; index++)
            {
                var area = areas[index];
                var areaObject = CreateMapObject(
                    areaLayer,
                    "MiniMap.Area." + SafeId(area.AreaId, index),
                    area.NormalizedRect,
                    AreaColor(area.Severity),
                    true);
                renderedAreas.Add(areaObject);
            }

            var markers = map.Markers ?? Array.Empty<MapMarkerDto>();
            for (var index = 0; index < markers.Count; index++)
            {
                var marker = markers[index];
                var markerObject = CreateMarker(marker, index);
                renderedMarkers.Add(markerObject);
            }
        }

        public void ClearMap()
        {
            LastMap = MiniMapDto.Hidden;
            Clear(renderedAreas);
            Clear(renderedMarkers);
            if (mapImage != null)
            {
                mapImage.sprite = null;
                mapImage.color = new Color32(22, 30, 29, 160);
            }

            if (mapLabel != null)
            {
                mapLabel.text = "平面图不可用";
            }
        }

        GameObject CreateMarker(MapMarkerDto marker, int index)
        {
            var parent = markerLayer != null ? markerLayer : transform as RectTransform;
            var objectName = "MiniMap.Marker." + SafeId(marker.MarkerId, index);
            var markerObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            markerObject.transform.SetParent(parent, false);
            var rect = markerObject.transform as RectTransform;
            rect.anchorMin = marker.NormalizedPosition;
            rect.anchorMax = marker.NormalizedPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(22f, 22f);

            var image = markerObject.GetComponent<Image>();
            image.color = MarkerColor(marker.Type);
            image.raycastTarget = false;
            AddTestId(markerObject, objectName);

            image.enabled = false;
            var symbolObject = new GameObject("Symbol", typeof(RectTransform), typeof(TacticalMapMarkerGraphic));
            var symbolRect = (RectTransform)symbolObject.transform;
            symbolRect.SetParent(rect, false);
            symbolRect.anchorMin = Vector2.zero; symbolRect.anchorMax = Vector2.one;
            symbolRect.offsetMin = symbolRect.offsetMax = Vector2.zero;
            var symbol = symbolObject.GetComponent<TacticalMapMarkerGraphic>();
            symbol.MarkerType = marker.Type;
            symbol.color = MarkerColor(marker.Type);
            symbol.raycastTarget = false;

            if (marker.Type == MarkerType.EnemyEstimate || marker.Type == MarkerType.EnemyKilled)
            {
                image.color = Color.clear;
                var cross = new GameObject("Cross", typeof(RectTransform), typeof(TextMeshProUGUI));
                cross.transform.SetParent(markerObject.transform, false);
                var crossRect = cross.transform as RectTransform;
                crossRect.anchorMin = Vector2.zero;
                crossRect.anchorMax = Vector2.one;
                crossRect.offsetMin = Vector2.zero;
                crossRect.offsetMax = Vector2.zero;
                var text = cross.GetComponent<TextMeshProUGUI>();
                text.text = "X";
                text.fontSize = 22f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = MarkerColor(marker.Type);
                text.raycastTarget = false;
                // Retain the existing semantic child for tooling; the vector symbol is the visible mark.
                text.enabled = false;
            }

            return markerObject;
        }

        static GameObject CreateMapObject(RectTransform parent, string objectName, Rect normalizedRect, Color color, bool fill)
        {
            var owner = parent != null ? parent : null;
            var mapObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            mapObject.transform.SetParent(owner, false);
            var rect = mapObject.transform as RectTransform;
            rect.anchorMin = new Vector2(normalizedRect.xMin, normalizedRect.yMin);
            rect.anchorMax = new Vector2(normalizedRect.xMax, normalizedRect.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = mapObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            AddTestId(mapObject, objectName);
            return mapObject;
        }

        Sprite ResolveSprite(string mapId)
        {
            for (var index = 0; index < mapResources.Count; index++)
            {
                var binding = mapResources[index];
                if (binding != null && binding.MapId == (mapId ?? string.Empty))
                {
                    return binding.Sprite;
                }
            }

            return TacticalUITheme.Current != null ? TacticalUITheme.Current.Map(mapId) : null;
        }

        static void Clear(List<GameObject> objects)
        {
            for (var index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null)
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(objects[index]);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(objects[index]);
                    }
                }
            }

            objects.Clear();
        }

        static string SafeId(string value, int index)
            => string.IsNullOrWhiteSpace(value) ? index.ToString("000") : value;

        static Color AreaColor(HudSeverity severity)
        {
            switch (severity)
            {
                case HudSeverity.Danger:
                case HudSeverity.Warning:
                    return new Color32(235, 74, 65, 120);
                case HudSeverity.Success:
                    return new Color32(69, 224, 215, 140);
                default:
                    return new Color32(235, 74, 65, 95);
            }
        }

        static Color MarkerColor(MarkerType type)
        {
            switch (type)
            {
                case MarkerType.Player:
                    return new Color32(200, 255, 106, 255);
                case MarkerType.Teammate:
                    return new Color32(81, 203, 255, 255);
                case MarkerType.SearchedRoom:
                    return new Color32(69, 224, 215, 255);
                case MarkerType.UnsearchedRoom:
                    return new Color32(255, 198, 82, 255);
                case MarkerType.BuildingEntrance:
                case MarkerType.Objective:
                    return new Color32(247, 185, 85, 255);
                case MarkerType.EnemyKilled:
                    return new Color32(255, 94, 87, 255);
                default:
                    return new Color32(235, 74, 65, 255);
            }
        }

        static void AddTestId(GameObject target, string id)
        {
            if (target == null)
            {
                return;
            }

            var testId = target.GetComponent<VRShooting.Unity.UITestId>();
            if (testId == null)
            {
                testId = target.AddComponent<VRShooting.Unity.UITestId>();
            }

            testId.SetId(id);
        }
    }

    /// <summary>Reusable data-driven map card. Selecting a card only changes pending UI state.</summary>
    [DisallowMultipleComponent]
    public sealed class P3MapCardView : MonoBehaviour
    {
        [SerializeField]
        Button button;

        [SerializeField]
        TMP_Text nameText;

        [SerializeField]
        TMP_Text conditionText;

        [SerializeField]
        Image selectedBorder;

        UnityAction clickHandler;
        string mapId = string.Empty;

        public string MapId => mapId ?? string.Empty;
        public bool IsAvailable { get; private set; }
        public bool IsSelected { get; private set; }
        public Button Button => button;

        public void Configure(Button targetButton, TMP_Text targetName, TMP_Text targetCondition, Image border)
        {
            button = targetButton;
            nameText = targetName;
            conditionText = targetCondition;
            selectedBorder = border;
            Bind();
        }

        public void SetMapId(string value)
        {
            mapId = value ?? string.Empty;
        }

        public void SetClickHandler(Action<string> handler)
        {
            if (button == null)
            {
                return;
            }

            if (clickHandler != null)
            {
                button.onClick.RemoveListener(clickHandler);
            }

            clickHandler = () => handler?.Invoke(MapId);
            button.onClick.AddListener(clickHandler);
        }

        public void Apply(TrenchMapDto map, bool selected)
        {
            mapId = map.MapId;
            IsAvailable = !string.IsNullOrWhiteSpace(map.MapId);
            IsSelected = selected;
            SetText(nameText, IsAvailable ? map.DisplayName : "地图不可用");
            SetText(conditionText, IsAvailable
                ? "复杂度：" + P3UiText.Difficulty(map.Difficulty) + "\n敌人数量：" + map.MinEnemyCount + "-" + map.MaxEnemyCount
                : "当前版本未开放");
            RefreshVisualState();
        }

        public void Apply(UrbanMapDto map, bool selected)
        {
            mapId = map.MapId;
            IsAvailable = !string.IsNullOrWhiteSpace(map.MapId);
            IsSelected = selected;
            SetText(nameText, IsAvailable ? map.DisplayName : "地图不可用");
            SetText(conditionText, IsAvailable
                ? "街道敌人：" + map.StreetEnemyMin + "-" + map.StreetEnemyMax + "\n建筑敌人：" + map.BuildingEnemyMin + "-" + map.BuildingEnemyMax
                : "当前版本未开放");
            RefreshVisualState();
        }

        public void ApplyUnavailable(string displayName)
        {
            IsAvailable = false;
            IsSelected = false;
            SetText(nameText, string.IsNullOrWhiteSpace(displayName) ? "地图不可用" : displayName);
            SetText(conditionText, "当前版本未开放");
            RefreshVisualState();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected && IsAvailable;
            RefreshVisualState();
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
            {
                button.interactable = value && IsAvailable;
            }
        }

        void Bind()
        {
            if (button != null && clickHandler == null)
            {
                clickHandler = () => { };
                button.onClick.AddListener(clickHandler);
            }
        }

        void RefreshVisualState()
        {
            if (selectedBorder != null)
            {
                selectedBorder.enabled = IsSelected;
                selectedBorder.color = IsSelected
                    ? new Color32(69, 224, 215, 255)
                    : new Color32(58, 85, 75, 255);
            }

            if (button != null)
            {
                button.interactable = IsAvailable;
            }
        }

        static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }

    public static class P3UiText
    {
        public static string Difficulty(DifficultyLevel value)
        {
            switch (value)
            {
                case DifficultyLevel.Low: return "低";
                case DifficultyLevel.High: return "高";
                default: return "中";
            }
        }

        public static string Posture(PlayerPosture value)
        {
            switch (value)
            {
                case PlayerPosture.Prone: return "卧倒";
                case PlayerPosture.Crouching: return "蹲下";
                default: return "起立";
            }
        }

        public static string Shoulder(ShoulderSide value)
            => value == ShoulderSide.Left ? "左肩" : "右肩";

        public static string RoomState(RoomSearchState value)
        {
            switch (value)
            {
                case RoomSearchState.Searching: return "搜索中";
                case RoomSearchState.Searched: return "已搜索";
                default: return "未搜索";
            }
        }

        public static string SquadRole(SquadMemberRole value)
        {
            switch (value)
            {
                case SquadMemberRole.Player: return "玩家";
                case SquadMemberRole.TeammateTwo: return "二号队友";
                case SquadMemberRole.TeammateThree: return "三号队友";
                default: return "队友";
            }
        }

        public static string SquadState(SquadMemberState value)
        {
            switch (value)
            {
                case SquadMemberState.Following: return "跟随";
                case SquadMemberState.CoveringReload: return "掩护";
                case SquadMemberState.MovingForward: return "前进";
                case SquadMemberState.HoldingPosition: return "保持";
                case SquadMemberState.Down: return "失能";
                default: return "正常";
            }
        }

        public static string Percent(float value)
            => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";

        public static string Ammo(AmmoDto ammo)
            => ammo.CurrentMagazine + " / " + ammo.ReserveAmmo;

        public static string Time(float seconds)
        {
            var safeSeconds = Mathf.Max(0f, seconds);
            var minutes = Mathf.FloorToInt(safeSeconds / 60f);
            var remainder = Mathf.FloorToInt(safeSeconds % 60f);
            return minutes.ToString("00") + ":" + remainder.ToString("00");
        }

        public static string Squad(SquadStatusDto squad)
        {
            var members = squad.Members ?? Array.Empty<SquadMemberDto>();
            if (members.Count == 0)
            {
                return "小队：暂无数据";
            }

            var lines = new List<string>();
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                lines.Add(SquadRole(member.Role) + " · " + SquadState(member.State) + " · 生命 " + Mathf.RoundToInt(member.Health));
            }

            return string.Join("\n", lines.ToArray());
        }

        public static string Prompt(HudDto hud, params string[] ids)
        {
            var prompts = hud.Prompts ?? Array.Empty<HudPromptDto>();
            for (var idIndex = 0; idIndex < ids.Length; idIndex++)
            {
                for (var index = 0; index < prompts.Count; index++)
                {
                    if (prompts[index].PromptId == ids[idIndex])
                    {
                        return prompts[index].Text ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        public static bool PromptEnabled(HudDto hud, params string[] ids)
        {
            var prompts = hud.Prompts ?? Array.Empty<HudPromptDto>();
            for (var idIndex = 0; idIndex < ids.Length; idIndex++)
            {
                for (var index = 0; index < prompts.Count; index++)
                {
                    if (prompts[index].PromptId == ids[idIndex])
                    {
                        return prompts[index].IsInteractive && prompts[index].IsEnabled;
                    }
                }
            }

            return false;
        }

        public static string Error(ErrorCode code, string message)
            => string.IsNullOrWhiteSpace(message) ? code.ToString() : code + "：" + message;

        public static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
