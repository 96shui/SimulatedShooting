# 02 训练 Session 数据模型

## 目标

统一 100m 射校、移动靶、堑壕、城镇四类训练的 Session 生命周期和共享字段。

## 枚举

```csharp
public enum TrainingMode
{
    None,
    Zeroing100m,
    MovingTarget,
    Trench,
    Urban
}

public enum SessionState
{
    NotStarted,
    Preparing,
    Countdown,
    Running,
    Paused,
    Analysis,
    Completed,
    Failed,
    Cancelled
}

public enum ResultGrade
{
    None,
    Excellent,
    Good,
    Pass,
    Fail
}
```

## 通用 Session DTO

```csharp
public readonly struct TrainingSessionDto
{
    public string SessionId { get; init; }
    public TrainingMode Mode { get; init; }
    public SessionState State { get; init; }
    public string MapId { get; init; }
    public string WeaponId { get; init; }
    public float ElapsedSeconds { get; init; }
    public AmmoDto Ammo { get; init; }
    public PlayerStatusDto Player { get; init; }
    public SquadStatusDto Squad { get; init; }
    public ResultGrade CurrentGrade { get; init; }
    public string FailureReason { get; init; }
}
```

## 玩家状态

```csharp
public enum PlayerPosture
{
    Standing,
    Crouching,
    Prone
}

public enum ShoulderSide
{
    Left,
    Right
}

public readonly struct PlayerStatusDto
{
    public float Health { get; init; }
    public bool IsAlive { get; init; }
    public PlayerPosture Posture { get; init; }
    public ShoulderSide Shoulder { get; init; }
    public bool CornerShootingAvailable { get; init; }
}
```

## 弹药状态

```csharp
public readonly struct AmmoDto
{
    public int CurrentMagazine { get; init; }
    public int ReserveAmmo { get; init; }
    public int MagazineCapacity { get; init; }
    public bool IsReloading { get; init; }
}
```

## Session 生命周期接口

```csharp
public interface ITrainingSessionService
{
    TrainingSessionDto Current { get; }
    ServiceResult<TrainingSessionDto> Create(TrainingMode mode, string mapId, string weaponId, RandomSeed seed);
    ServiceResult<TrainingSessionDto> Start(string sessionId);
    ServiceResult<TrainingSessionDto> Pause(string sessionId);
    ServiceResult<TrainingSessionDto> Resume(string sessionId);
    ServiceResult<TrainingResultDto> End(string sessionId, SessionEndReason reason);
    ServiceResult<Unit> Cancel(string sessionId);
}
```

## 结算 DTO

```csharp
public enum SessionEndReason
{
    Completed,
    PlayerDead,
    OutOfAmmo,
    Cancelled,
    SystemError
}

public readonly struct TrainingResultDto
{
    public string SessionId { get; init; }
    public TrainingMode Mode { get; init; }
    public bool Victory { get; init; }
    public ResultGrade Grade { get; init; }
    public float ElapsedSeconds { get; init; }
    public int RemainingAmmo { get; init; }
    public string SummaryJson { get; init; }
}
```

## 数据约束

- `SessionId` 创建后不可变。
- `Mode` 创建后不可变。
- `MapId` 对堑壕和城镇必填；100m 和移动靶可为空或使用默认训练场 ID。
- `WeaponId` 对堑壕和城镇必填；100m 和移动靶可使用系统指定训练武器。
- `ElapsedSeconds` 由后端统一计时，UI 不自行累加。
- 所有结算页必须使用 `TrainingResultDto` 或对应模式结果 DTO。

