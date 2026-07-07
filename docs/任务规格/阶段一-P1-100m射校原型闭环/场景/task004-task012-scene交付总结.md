# task004 与 task012 Scene 交付总结

## 1. 文档范围

本文总结 P1 100m 射校场景的两个任务：

- `task004`：100m 靶场 Blockout 与基础场景结构。
- `task012`：靶标命中、弹着坐标和弹着点可视化支持。

权威任务规格仍为：

- `task004-scene-zeroing-range-blockout.md`
- `task012-scene-target-impact-support.md`

## 2. Scene 及相关文件位置

生成后的 Unity Scene：

```text
Assets/Scenes/ZeroingRangeScene.unity
```

| 内容 | 位置 |
|---|---|
| Scene | `Assets/Scenes/ZeroingRangeScene.unity` |
| 场景生成器 | `Assets/SimulatedShooting/Editor/ZeroingRangeSceneBuilder.cs` |
| 命中组件 | `Assets/SimulatedShooting/Runtime/Scene/TargetImpactSurface.cs` |
| 弹着材质 | `Assets/SimulatedShooting/Art/Materials/TargetImpactMarker.mat` |
| PlayMode 测试 | `Assets/SimulatedShooting/Tests/PlayMode/ZeroingRangeSceneTests.cs` |
| Build Settings | `ProjectSettings/EditorBuildSettings.asset` |

Unity 菜单入口：

- 重新生成：`Tools/Simulated Shooting/Build Zeroing Range Scene`
- 打开并运行：`Tools/Simulated Shooting/Run Zeroing Range Scene`

> 重新生成会按 Builder 当前定义重建 `ZeroingRangeScene`。需要永久保留的场景修改应同步写入 Builder。

## 3. task004：100m 靶场 Blockout

### 3.1 目标

task004 提供 P1 射校闭环需要的基础空间、靶标和联调锚点。重点是比例正确、靶标可识别、射线可达以及支持无 VR 测试，不包含最终美术精修。

### 3.2 已实现内容

- 射击位与 100m 主靶标。
- 50cm × 50cm 胸靶靶面。
- 直径 10cm 的十环显示。
- 射击通道、距离线、左右护坡和靶后挡土区域。
- 基础地面、远景、植被、沙袋、射击台和方向光。
- 武器 Blockout 参考模型。
- 武器出生点和 HUD 锚点。
- 无 VR 测试相机。
- VR Origin 接入点，默认关闭以避免无设备测试受阻。
- 场景关键对象的稳定测试 ID。

### 3.3 关键空间数据

| 项目 | 配置 |
|---|---:|
| 射击位到主靶标距离 | 100m |
| 靶面尺寸 | 0.5m × 0.5m |
| 十环直径 | 0.1m |
| 无 VR 相机高度 | 约 1.5m |

### 3.4 主要场景结构

```text
ZeroingRange
├─ Environment
│  ├─ Ground
│  ├─ RangeLane_100m
│  ├─ Berm_Left
│  ├─ Berm_Right
│  └─ TargetBackstop
├─ TrainingAnchors
│  ├─ ShootingPosition
│  ├─ WeaponSpawnPoint
│  ├─ HudAnchor
│  ├─ Camera_NoVR
│  ├─ WeaponReference_Blockout
│  └─ XR Origin (VR)
└─ Target_Primary_100m
   ├─ TargetBacker
   ├─ TargetFace_50cm
   ├─ TargetSilhouette_Torso
   ├─ TargetSilhouette_Head
   ├─ TenRing_10cm
   ├─ TargetPost_Left
   └─ TargetPost_Right
```

### 3.5 task004 稳定测试 ID

| 用途 | 测试 ID |
|---|---|
| 场景根节点 | `ZeroingRange.Root` |
| 射击位 | `ZeroingRange.ShootingPosition` |
| 武器出生点 | `ZeroingRange.WeaponSpawn` |
| HUD 锚点 | `ZeroingRange.HudAnchor` |
| 无 VR 相机 | `ZeroingRange.Camera.NoVR` |
| VR Origin | `ZeroingRange.Origin.VR` |
| 武器参考模型 | `ZeroingRange.Weapon.Reference` |
| 主靶标 | `ZeroingRange.Target.Primary` |
| 靶面 | `ZeroingRange.Target.Face` |
| 十环 | `ZeroingRange.Target.TenRing` |
| 射击通道 | `ZeroingRange.Environment.Lane` |
| 左右护坡 | `ZeroingRange.Environment.Berm.Left` / `ZeroingRange.Environment.Berm.Right` |

## 4. task012：靶标命中与弹着可视化

### 4.1 目标

task012 在 task004 场景上提供统一的靶面命中入口，使功能B的射线或基础弹道、功能A的射校记录以及 UI 弹着图能够使用同一份坐标数据。

### 4.2 已实现内容

- 靶面 Collider 和射线命中入口。
- 独立靶心参考点。
- 50cm × 50cm 有效命中范围。
- 5cm 十环半径。
- 世界坐标转换为以靶心为原点的厘米偏差。
- 十环内命中判定。
- 超出靶面范围的命中拒绝。
- 场景靶面弹着标记。
- 可供后续服务或 UI 适配层读取的弹着记录。
- `ImpactRecorded` 新增弹着事件。
- 清除弹着数据和标记的入口。
- 靶面与十环调试 Gizmo。

不包含：

- 准星柱或觇孔调整算法。
- 完整子弹物理和复杂弹道。
- UI 分析图绘制。

### 4.3 核心接口

核心组件：`TargetImpactSurface`。

| 接口 | 用途 |
|---|---|
| `TryRecordRay` | 接收射线并记录有效命中 |
| `TryRecordWorldPoint` | 将世界坐标转换为厘米偏差并记录 |
| `Impacts` | 获取当前弹着记录 |
| `ImpactRecorded` | 订阅新增弹着点 |
| `ClearImpacts` | 清除弹着数据和场景标记 |
| `TargetCenter` | 获取靶心 Transform |
| `TenRingRadiusCm` | 获取十环半径配置 |

`TargetImpactPoint` 数据包含：

- `WorldPoint`：靶面上的世界坐标。
- `OffsetCm`：相对靶心的水平、垂直厘米偏差。
- `InsideTenRing`：是否位于 5cm 十环半径内。

### 4.4 task012 新增场景节点

```text
Target_Primary_100m
├─ TargetCenter
├─ ImpactMarkers
└─ TargetFace_50cm
   └─ TargetImpactSurface
```

| 用途 | 测试 ID |
|---|---|
| 靶心参考点 | `ZeroingRange.Target.Center` |
| 弹着标记容器 | `ZeroingRange.Target.ImpactMarkers` |

## 5. 测试与验收

PlayMode 测试共 7 项，结果为 **7/7 通过**。

### task004 覆盖

- 场景包含射击位、主靶标、VR Origin 和无 VR 相机。
- 射击位与靶标距离为 100m。
- 靶面和十环尺寸正确。
- 射击位射线能够命中靶标。
- 第一人称相机能够在射击通道中央看到主靶标。

### task012 覆盖

- 中心射线命中返回接近 `(0cm, 0cm)` 的偏差。
- 世界坐标正确转换为厘米偏差。
- 场景弹着标记与记录坐标一致。
- 超出 50cm 靶面的坐标被拒绝，不生成记录和标记。

无 VR 设备时可通过 `Camera_NoVR` 和 PlayMode Test 验证核心流程。真实 VR 头显下的可读性、舒适度和射击手感仍需实机验收。

## 6. 查看效果

1. 使用 Unity 打开项目根目录。
2. 打开 `Assets/Scenes/ZeroingRangeScene.unity`。
3. 选择 `Tools/Simulated Shooting/Run Zeroing Range Scene`。
4. Unity 使用无 VR 测试相机进入播放模式。

在 Hierarchy 中展开 `ZeroingRange/Target_Primary_100m`，选择 `TargetFace_50cm` 可检查 `TargetImpactSurface`。选中该对象时，Scene 视图会显示靶面和十环 Gizmo。

## 7. 联调约定

- 功能B将武器射线传入 `TryRecordRay`，不重复计算靶面厘米偏差。
- 功能A读取 `TargetImpactPoint.OffsetCm` 并转换为射校服务记录。
- UI 通过服务 DTO 或适配层使用同一份 `OffsetCm`，保证 HUD、分析图和场景弹着点一致。
- task016进行视觉和性能收口时，应保留现有锚点、测试 ID 和坐标约定。
