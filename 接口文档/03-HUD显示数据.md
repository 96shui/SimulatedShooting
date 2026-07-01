# 03 HUD 显示数据

## 目标

约束 100m 射击 HUD、移动靶 HUD、堑壕 HUD、城镇 HUD 的显示数据来源，禁止 UI 直接计算业务状态。

## 通用 HUD DTO

```csharp
public readonly struct HudDto
{
    public string SessionId { get; init; }
    public TrainingMode Mode { get; init; }
    public HudType HudType { get; init; }
    public AmmoDto Ammo { get; init; }
    public PlayerStatusDto Player { get; init; }
    public MiniMapDto MiniMap { get; init; }
    public IReadOnlyList<HudTextLineDto> TextLines { get; init; }
    public IReadOnlyList<HudPromptDto> Prompts { get; init; }
    public bool CanShoot { get; init; }
}

public enum HudType
{
    Zeroing,
    MovingTargetDay,
    MovingTargetNight,
    Trench,
    UrbanStreet,
    UrbanBuilding
}
```

## 文本与提示

```csharp
public readonly struct HudTextLineDto
{
    public string Key { get; init; }
    public string Label { get; init; }
    public string Value { get; init; }
    public HudSeverity Severity { get; init; }
}

public readonly struct HudPromptDto
{
    public string PromptId { get; init; }
    public string Text { get; init; }
    public bool IsInteractive { get; init; }
    public bool IsEnabled { get; init; }
}

public enum HudSeverity
{
    Normal,
    Info,
    Warning,
    Danger,
    Success
}
```

## 小地图 DTO

```csharp
public readonly struct MiniMapDto
{
    public bool Visible { get; init; }
    public string MapId { get; init; }
    public IReadOnlyList<MapMarkerDto> Markers { get; init; }
    public IReadOnlyList<MapAreaDto> Areas { get; init; }
}

public readonly struct MapMarkerDto
{
    public string MarkerId { get; init; }
    public MarkerType Type { get; init; }
    public Vector2 NormalizedPosition { get; init; }
    public string Label { get; init; }
}

public enum MarkerType
{
    Player,
    Teammate,
    EnemyEstimate,
    EnemyKilled,
    BuildingEntrance,
    Objective,
    SearchedRoom,
    UnsearchedRoom
}
```

## HUD 服务接口

```csharp
public interface IHUDService
{
    ServiceResult<HudDto> GetHud(string sessionId);
    event Action<HudDto> HudUpdated;
}
```

## 各 HUD 必填字段

| HUD | 必填显示 |
|---|---|
| Zeroing | 轮次、距离 100m、弹数、稳定度、弹着记录、CanShoot |
| MovingTargetDay | 弹药 10 发、点射模式、命中数、速度、方向、可射击状态 |
| MovingTargetNight | 弹药 10 发、点射模式、命中数、速度、方向、端点禁射提示、微光镜状态 |
| Trench | 小地图、弹药、队友状态、姿态、换肩、拐角射击 |
| UrbanStreet | 小地图、任务提示、弹药、姿态、换肩、建筑入口提示 |
| UrbanBuilding | 楼层小地图、房间状态、危险区域、交互提示、弹药、任务进度 |

## 刷新规则

- 弹药变化、姿态变化、命中变化、任务状态变化必须发布 `HudUpdatedEvent`。
- 稳定度和移动靶方向可按固定频率刷新，建议 10-20Hz。
- UI 不应每帧主动轮询全部服务；高频数据由 HUD 服务聚合后推送。
- HUD 文本由 DTO 提供 `Label` 和 `Value`，UI 只负责排版。

