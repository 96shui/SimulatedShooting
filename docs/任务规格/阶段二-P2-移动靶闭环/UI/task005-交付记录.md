# task005 交付记录：移动靶设置、连射 HUD 与结算 UI

## 交付状态

- 工程实现与空白场景 Fake DTO 自动化测试已完成，日期：2026-08-18。
- 阶段审核节点 C 仍需与 task006/task007 的场景和玩法实现组合验收。
- World Space Anchor、卧姿视距、XR 射线热区和实机舒适度保留到阶段审核节点 D 验收。

## 实现清单

- `MovingTargetRangeUI`：生成并承载移动靶设置、最小 HUD 和结算三个页面，沿用共用大型面板/最小 HUD 根命名。
- `MovingTargetUIPresenter`：集中处理 DTO 格式化、速度选择、命令防重复、服务错误、HUD/结果渲染和事件退订。
- `IMovingTargetUIPort`：UI 侧 DTO/命令边界，可由空白场景 Fake 实现替换。
- `MovingTargetUICommandAdapter`：组合根内对接移动靶、展示、HUD、结果和页面路由服务；返回主界面时切换场景。
- `GameMain`：移动靶场景加载后创建并注册移动靶 UI。
- `MovingTargetHudService`：提供只读路线进度文本行，UI 不读取目标 Transform。

## DTO 到控件映射

| DTO 来源 | UI 显示或行为 |
|---|---|
| `GetAvailableSpeeds()` | 3/4/5 m/s 速度按钮的显示与可选状态，默认选择 4 m/s |
| `TrainingPresentationDto.LargePanelVisible` | 设置/结算大型 UI 根显隐 |
| `TrainingPresentationDto.MinimalHudVisible` | 倒计时与实射最小 HUD 显隐 |
| `ActiveScreen`、`Phase`、`AwaitingWeaponPickup` | 设置、HUD、结算页面切换，按钮状态与取枪提示 |
| `HudDto.TextLines[ammo/hits/progress/speed/direction/countdown/fireMode]` | 弹药、命中、进度、速度、方向、倒计时和射击模式文本 |
| `HudDto.FireSequence` | 待扣动、两发起射、长按连射、停止原因中文映射 |
| `HudDto.Prompts`、`CanShoot` | 可射击/禁射提示及只读视觉状态 |
| `MovingTargetResultDto` | 总射击、命中、命中率、速度、用时、评级和可变长射击序列 |
| `FireSequenceRecordDto.Shots` | 每个扣动序列的逐发命中与路线进度记录 |

## 命令转发表

| 控件 | UI Port 命令 | 防重复条件 |
|---|---|---|
| `Button_MovingTargetSetup_Start` | `Start(MovingTargetSettingsDto)` | 仅设置阶段、速度有效且无命令执行中 |
| `Button_MovingTargetSetup_Back` | `Exit(sessionId)` | 无退出命令执行中且未处于 `Exiting` |
| `Button_MovingTargetResults_Retry` | `Retry(sessionId)` | 仅结算阶段且无命令执行中 |
| `Button_MovingTargetResults_BackToModeSelection` | `Exit(sessionId)` | 仅一次退出；确认后路由主界面 |

## 稳定测试 ID

| 用途 | 稳定 ID |
|---|---|
| 设置页 | `Screen_MovingTargetSetup`、`Screen_MovingTargetSettings` |
| 速度选择 | `Button_MovingTargetSetup_Speed3/Speed4/Speed5` |
| 开始/返回 | `Button_MovingTargetSetup_Start`、`Button_MovingTargetSetup_Back` |
| 取枪与错误 | `Text_MovingTargetSetup_Status`、`Text_MovingTargetSetup_Error` |
| 最小 HUD | `Screen_MovingTargetHud` |
| HUD 字段 | `Hud_MovingTarget_FireMode/Ammo/Hits/Progress/Speed/Direction/Countdown/FireSequence/NoFirePrompt` |
| 结算页 | `Screen_MovingTargetResults` |
| 结算摘要/序列 | `Text_MovingTargetResults_Summary`、`Text_MovingTargetResults_Sequences` |
| 重试/返回模式 | `Button_MovingTargetResults_Retry`、`Button_MovingTargetResults_BackToModeSelection` |

## 自动化证据

- 空白测试场景使用 `FakeMovingTargetUIPort`，不依赖真实场景、目标 Transform、武器组件、XR 设备或玩法具体服务。
- 覆盖 3/4/5 m/s、缺失速度、等待取枪、倒计时、10→9→8 逐发弹药、快速两发、长按连射、五种停止原因、禁射恢复、可变长序列、0/全命中、四档评级、重试、返回和销毁退订。
- Task 5 空白场景 PlayMode：11/11 通过。

## 阶段审核节点 D 实机项

- 卧姿常见头显高度下的字号、对比度、面板视距和中心瞄准保留区。
- XR Ray Interactor 对速度、开始、重试、返回按钮的命中热区和焦点反馈。
- 取枪后大型 UI 隐藏、倒计时 HUD 出现、训练结束结算回显的真实设备时序。
- 最小 HUD 不遮挡实际枪械瞄线，禁射/恢复提示在运动目标端点清晰可读。
