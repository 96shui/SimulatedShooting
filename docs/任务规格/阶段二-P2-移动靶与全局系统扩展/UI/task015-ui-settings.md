# task015 设置界面 UI

## 负责人

UI

## 目标

实现当前 `22-设置界面.feature.md` 定义的完整 P2 设置页：VR 舒适度、转向、移动、亮度、HUD 透明度、音效音量、瞄准辅助和语言；支持 Pending 临时副本、实时预览、应用、恢复默认和返回丢弃。UI 不直接写本地文件，也不直接启停 XRI Provider、AudioMixer 或后处理组件。

## 参考资料

- `UI/Sample/vr-shooting-settings-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/22-设置界面.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/10-设置服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

| Feature | 精确场景名 | 本任务验收范围 |
|---|---|---|
| `02-游戏主界面.feature.md` | `打开设置界面` | 进入设置并加载当前保存配置 |
| `22-设置界面.feature.md` | `显示当前设置` | 完整显示八个设置字段及保存值 |
| `22-设置界面.feature.md` | `修改二选一或多选设置` | 覆盖舒适度、转向、移动、瞄准辅助、语言示例并更新 Pending/预览 |
| `22-设置界面.feature.md` | `修改滑条设置` | 覆盖亮度 70、HUD 透明度 60、音效音量 50 示例 |
| `22-设置界面.feature.md` | `应用设置` | 保存 Pending，收到结果后保持页面并显示已应用状态 |
| `22-设置界面.feature.md` | `恢复默认设置` | 只恢复 Pending 和预览，未应用前不持久化 |
| `22-设置界面.feature.md` | `返回不保存临时修改` | 丢弃 Pending 后返回，持久化值不变 |
| `22-设置界面.feature.md` | `设置界面在训练中打开` | 返回暂停菜单且不销毁当前 Session |

## 交付内容

- `Screen_Settings`
- 训练中打开设置所需的最小暂停叠层（其 ScreenId/路由先由 task001 写入接口 01）：
  - `Screen_TrainingPause`
  - `Button_TrainingPause_OpenSettings`
  - `Button_TrainingPause_Resume`
- 枚举/开关设置：
  - `Button_Settings_ComfortLow`
  - `Button_Settings_ComfortMedium`
  - `Button_Settings_ComfortHigh`
  - `Button_Settings_TurnSmooth`
  - `Button_Settings_TurnSnap`
  - `Button_Settings_MoveTeleport`
  - `Button_Settings_MoveContinuous`
  - `Button_Settings_AimAssistOn`
  - `Button_Settings_AimAssistOff`
  - `Button_Settings_LanguageChinese`
- 滑条及数值文本：
  - `Slider_Settings_Brightness`、`Text_Settings_BrightnessValue`
  - `Slider_Settings_HudOpacity`、`Text_Settings_HudOpacityValue`
  - `Slider_Settings_SfxVolume`、`Text_Settings_SfxVolumeValue`
- 预览和操作：
  - `Panel_Settings_HudPreview`
  - `Text_Settings_PendingState`
  - `Text_Settings_Error`
  - `Button_Settings_Apply`
  - `Button_Settings_ResetDefault`
  - `Button_Settings_Back`
- Settings Presenter/绑定脚本：
  - 打开页面时调用 `Load()` 并以服务返回值初始化 Pending 显示。
  - 每次交互构造新的 `GameSettingsDto` 调用 `SetPending()`，只以成功返回 DTO 刷新控件和预览。
  - 应用调用 `SavePending()`；恢复默认调用 `ResetPendingToDefault()`；返回未应用状态调用 `DiscardPending()`。
  - `PersistenceFailed`、`InvalidInput` 等失败保留当前页面，显示错误且不伪造保存成功状态。
  - 使用 `NavigationArgs.ReturnToScreen`/路由历史返回，不把训练中返回目标固定为主菜单。

## 视觉与交互要求

- 八个字段按舒适度、操控、画面/HUD、音频、辅助/语言分组，当前值与 Pending 修改状态清晰可见。
- 滑条应有连续视觉反馈和明确数值，不使用文字输入代替 VR 可操作控件。
- Pending 预览与已保存状态要可区分；点击恢复默认后必须显示“尚未应用”或等效状态。
- XR 射线拖动滑条时不得误触页面返回或相邻选项；所有控件具有 Hover、Pressed、Selected、Disabled 状态。

## 不包含

- 不实现设置持久化、默认值、数值合法性或损坏文件回退，task002 负责。
- 不直接切换连续/分段转向、瞬移/连续移动 Provider，不直接改 AudioMixer、HUD CanvasGroup 或曝光，task014 负责。
- 不实现第二种语言内容；当前接口只定义中文，但保留枚举默认显示策略。
- 不实现完整暂停菜单、训练暂停状态机或 P3 页面。
- 不扩展设置字段或引入账号云同步。

## 依赖关系

- 前置依赖：task001、task002、task014。
- 可并行：task013。
- 后续依赖：task016、task017。

## 联调说明

- 与 task002 联调：`Load/GetPending/SetPending/ResetPendingToDefault/SavePending/DiscardPending` 的成功和错误结果。
- 与 task014 联调：Pending 只更新设置页 Preview，保存成功后才应用全局运行时；返回丢弃恢复 Preview，训练 HUD、全局 AudioMixer/曝光和 XRI Provider 在未保存期间保持不变。
- 与 task008 联调：主菜单入口和 `ReturnToScreen`。
- 与 task016 联调：训练中打开/返回设置时当前移动靶或 P1 Session 不丢失。
- task001 未完成接口 01 的暂停 ScreenId、受约束 `ReturnToScreen` 和稳定 ID 更新前，本任务不得实现或验收训练中设置往返。

## 测试要求

### 无 VR PlayMode 自动化

- 用非默认保存 DTO 打开页面，八个字段均显示保存值而非静态示例。
- 对 BDD `修改二选一或多选设置` 的所有示例逐项验证选中态、Pending DTO 和预览刷新。
- 对亮度 70、HUD 透明度 60、音效音量 50 验证滑条、数值文本和 Pending DTO 一致。
- 点击应用后重新打开设置，显示已保存值；`SavePending()` 失败时不显示成功且页面不关闭。
- 点击恢复默认只改变 Pending/预览；未点击应用直接返回并重新进入后仍显示之前保存值。
- 修改但未应用后返回，验证调用 `DiscardPending()` 且持久化值不变。
- 从训练暂停上下文进入并返回，路由回暂停页面且 SessionId/状态保持不变。
- 滑条和按钮均可通过稳定测试 ID 驱动，不依赖真实 XR 设备。

### VR 实机手工验收

- 手柄射线可稳定选择枚举项、开关并拖动三条滑条，焦点离开时不误提交。
- 所有分组、数值、Pending 状态和错误提示在头显中清晰可读。
- 切换移动/转向的实时预览不得在设置页面突然移动玩家或造成明显不适；实际启用时按 task014 的安全策略验证。
- 训练中进入和返回设置不会丢失当前训练状态。

## 验收标准

- 当前 BDD 的全部设置字段、临时修改、应用、恢复默认和返回行为均有 UI 覆盖。
- UI 只消费设置服务 DTO/结果，不直接持久化或控制 Unity 运行时组件。
- 所有字段、预览、错误和按钮有稳定测试 ID。
- 无 VR PlayMode 测试通过，VR 控件可操作性已完成手工记录。
