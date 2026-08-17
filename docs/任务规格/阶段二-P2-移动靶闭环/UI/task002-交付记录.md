# task002 交付记录：正前方 UI、最小 HUD 与显隐框架

## 交付状态

- 工程实现与无设备自动化测试已完成，日期：2026-08-14。
- 阶段审核节点 B 仍需与 task003 的两个场景 Anchor 组合验证。
- VR 字号、视距、射线热区和舒适度按任务规格保留到阶段审核节点 D 实机复验。

## 实现清单

- `Training_SharedWorldSpaceUI.prefab`：World Space Canvas，以及相互独立的 `LargePanelRoot`、`MinimalHudRoot`。
- `TrainingPresentationView`：一次性应用完整展示快照，控制根节点、当前面板、按钮状态和输入焦点。
- `TrainingPresentationPresenter`：查询权威快照、去重、抵抗乱序事件、转发下一轮/重试命令，并在禁用或销毁时退订。
- `ITrainingUIAnchor`、`TrainingUIAnchorBinder`：只通过通用 Anchor 挂接两个 UI 根；缺失、槽位错误或两个根共用 Anchor 时输出明确错误。
- P1 任务说明、等待取枪、实射 HUD、弹着分析和最终评级已接入统一显隐流程。
- HUD 初始文本使用 `--` 或“待同步”，不再预填示例训练数字。

## 稳定测试 ID

| 用途 | 稳定 ID |
|---|---|
| 共用 World Space UI | `Training.Shared.WorldSpaceUI` |
| 大型交互 UI 根 | `Training.Shared.LargePanelRoot` |
| 最小只读 HUD 根 | `Training.Shared.MinimalHudRoot` |
| 取枪提示 | `Training.Shared.PickupPrompt` |
| 射击位状态 | `Training.Shared.FiringStationState` |
| P1 任务说明 | `Screen_ZeroingBriefing` |
| P1 实射 HUD | `Screen_ZeroingHud` |
| P1 弹着分析 | `Screen_ZeroingImpactAnalysis` |
| P1 最终评级 | `Screen_ZeroingFinalRating` |
| 开始训练 | `Button_ZeroingBriefing_Start` |
| 应用调整 | `Button_ZeroingImpactAnalysis_ApplyAdjustment` |
| 下一轮 | `Button_ZeroingImpactAnalysis_NextRound` |
| 重新训练 | `Button_ZeroingFinalRating_Retry` |

P1 HUD 继续沿用接口文档定义的 `Hud_Zeroing_*` 和 `Text_ZeroingHud_*` ID；P2 移动靶页面与按钮由 task005 在本框架上补齐。

## DTO 到控件映射

| 来源字段 | UI 行为或控件 |
|---|---|
| `TrainingPresentationDto.ActiveScreen` | 激活对应 `Screen_*` 面板，其余同根面板关闭 |
| `LargePanelVisible` | `Training.Shared.LargePanelRoot` 的可见、交互和射线阻挡状态 |
| `MinimalHudVisible` | `Training.Shared.MinimalHudRoot` 的可见状态；始终只读且不拦截射线 |
| `Phase` | 开始、下一轮、重试按钮可用状态及主操作焦点 |
| `AwaitingWeaponPickup`、`VisibilityReason` | `Training.Shared.PickupPrompt` |
| `FiringStationId` | `Training.Shared.FiringStationState`；空值显示“等待绑定” |
| `Mode`、`SessionId` | 页面导航参数以及 Presenter 命令参数 |
| `HudDto.Lines[round/distance/ammo/stability/impactRecord/shoulder]` | P1 HUD 对应 `Text_ZeroingHud_*` 文本和稳定度条 |
| `HudDto.Prompts`、`CanShoot` | P1 HUD 提示及允许/禁止射击视觉状态 |
| `ZeroingRoundAnalysisDto` | 偏差、瞄具调整、建议、弹着点、按钮状态 |
| `ZeroingResultDto` | 最终评级、轮次记录和弹着摘要 |

`ShootingAllowed`、`ArtificialLocomotionAllowed` 和 `Posture` 随完整快照保留在 Presenter 当前状态中；UI 不据此推导玩法规则，射击与移动许可仍由玩法/输入层执行。

## 自动化证据

- 空白场景 Fake DTO/命令 Spy PlayMode：6/6 通过。
- `VRShooting.Tests.PlayMode`：34/34 通过，包含 P1 输入替身与真实场景回归。
- `VRShooting.Tests.EditMode`：93/93 通过。
- Prefab 通过批处理构建并保存，无场景对象、武器对象或具体玩法服务的序列化引用。

覆盖模式入口、等待取枪、有效取枪、实射、轮次复盘、命令仅发送一次、收到新状态后隐藏、最终结果、重复/乱序事件、禁用/启用、销毁退订、Fake Anchor、稳定 ID 和中心瞄准保留区。

## 阶段审核节点 D 实机项

- 卧姿常见头显高度与视距下的大型面板字号、对比度和舒适度。
- XR Ray Interactor 对开始、应用调整、下一轮、重试按钮的命中热区与焦点反馈。
- 实射最小 HUD 不遮挡实际枪械瞄线和中心保留区。
- 取枪后大型 UI 隐藏、脱手提示和复盘/结算自动出现的真实设备时序。
