# task003/task006 场景交付说明

## 改动边界

- `MovingTargetRangeScene` 使用增量迁移补齐固定卧姿射击点，没有重新运行场景 Builder，也没有重建已手工微调的远景、道路、山体、标语、固定靶或 40m 路线。
- 新增近景内容仅位于 `TrainingAnchors/FiringStation_Prone/FiringStationVisual`：0m 安全线、射击框架、卧姿枪托、枪箱、左右沙袋和 0m 标牌。
- 移动目标只补齐缺失的六段侧身轮廓；所有轮廓 Collider 已移除，目标仍只有一个统一命中面。
- `ZeroingRangeScene` 从无冲突的 main 父版本恢复有效 YAML 后，仅增量接入共用卧姿 Anchor 和移动禁用策略；未运行 P1 Builder。

## Anchor 合同

以下为 P2 `FiringStation_Prone` 下的局部坐标（米）：

| Anchor | 局部坐标 | 用途 |
|---|---|---|
| `PlayerRootAnchor` | `(0, 0, 0)` | 唯一固定玩家根 |
| `ProneHeadReference` | `(0, 0.72, 0)` | 卧姿头部参考，不锁定 HMD 追踪 |
| `AimForwardAnchor` | `(0, 0.72, 1)` | 初始正前方 |
| `LargeUiAnchor` | `(-0.72, 0.88, 1.55)` | 大型页面挂点 |
| `MinimalHudAnchor` | `(0.62, 0.82, 1.25)` | 最小 HUD 挂点 |
| `WeaponRackAnchor` | `(0.42, 0.35, 0.75)` | 枪/枪架挂点 |
| `TargetRootAnchor` | 路线根相对位置 | 目标区域语义挂点 |

P1 使用相同合同；`WeaponRackAnchor` 保留原 `WeaponSpawn` 的实际位置，`TargetRootAnchor` 保留原主靶位置。三个 UI/枪 Anchor 是同级独立节点。

## task003

- `TrainingRangeSceneBindings` 在 Inspector 显式绑定七个 Anchor，并在 `OnValidate`、`Awake` 和上下文菜单中报告缺失/重复引用。
- `FixedProneLocomotionGuard` 禁用 Locomotion 根和所有 XRI `LocomotionProvider`，不逐帧写入头手 Transform。
- P1/P2 的 XR Origin 固定到唯一玩家根；无 VR Camera 使用卧姿头部参考。
- P1/P2 不再包含 `PlayerFootstepAudio`。
- Fake 时间线仅在 Editor/Development 生效。

## task006

- 场景路径迁移为 `Assets/Scenes/MovingTargetRangeScene.unity`，保留旧 GUID `6e4b13515eb5cfe4f95f0ddfc95cdabd`；Build Settings 和运行时 SceneName 已同步，旧误拼写场景已移除。
- 左右端点间距 40m，进度 `0/0.5/1` 映射右端/中点/左端；Gizmo 和方向标牌可在 Editor 查看。
- `MovingTargetVisualDriver` 只消费 `RouteProgress01`、方向、端点停留、开火许可和速度视觉状态，不反推玩法阶段。
- Fake 时间线覆盖 3/4/5m/s、左端停留、反向、右端结束和重试复位。
- 目标使用 `TrainingTarget`，环境使用 `TrainingEnvironment`；命中适配器只产生标准化命中输入。
- 确认命中反馈按唯一 `ShotId` 去重；场景不生成弹药、计分、倒计时或结算规则。
- 夜间灯光和低照度资源已移除，场景只保留固定白天。

## 验证与证据

- EditMode：`100/100` 通过，包括 P1/P2 Missing Script 为 0、绑定校验、场景名/GUID/Build Settings 迁移。
- PlayMode：task003/task006 命名测试 `12/12` 通过；全量为 `79/92`。
- 全量剩余 13 个失败属于旧 task018 标语网格/高度断言、P1 task004/task013/task016 和 P1 完整流程；没有通过修改这些旧测试掩盖失败。
- [最终场景截图](../../../codex-reports/evidence/task003-task006-moving-target-range.png)
- [Editor 三状态渲染基线](../../../codex-reports/evidence/task003-task006-performance-baseline.md)

## 仍需 VR 实机验收

- HMD 与双手真实追踪在固定 Player Root 下仍自然可动。
- 卧姿 UI/枪的可读性、可达性和中心瞄准保留区舒适度。
- PICO/目标设备上的 GPU、帧时、阴影和雾性能；Editor batchmode 基线只用于代码回归比较。
