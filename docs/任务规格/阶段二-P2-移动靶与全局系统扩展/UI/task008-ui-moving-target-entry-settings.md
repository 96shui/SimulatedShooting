# task008 P2 入口、模式选择与移动靶设置 UI

## 负责人

UI

## 目标

在 P1 主菜单和训练模式选择基础上开放移动目标射击、武器库和设置入口，并实现移动靶昼夜模式、合法速度选择、开始训练和返回的设置页面。所有页面跳转和 Session 创建均由路由及移动靶服务完成，UI 不直接加载训练场景或创建目标。

## 参考资料

- `UI/Sample/vr-shooting-main-menu-ui.png`
- `UI/Sample/vr-shooting-training-mode-selection-ui.png`
- `UI/Sample/vr-shooting-moving-target-mode-settings-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/03-训练模式选择.feature.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

| Feature | 精确场景名 | 本任务验收范围 |
|---|---|---|
| `02-游戏主界面.feature.md` | `进入移动目标射击设置` | 主菜单进入 `MovingTargetSettings` 并记录移动目标模式 |
| `02-游戏主界面.feature.md` | `打开武器库` | 打开 `Armory` 并保留返回页面 |
| `02-游戏主界面.feature.md` | `打开设置界面` | 打开 `Settings` 并加载当前设置入口状态 |
| `02-游戏主界面.feature.md` | `快速重复点击菜单按钮` | Busy/切页期间入口禁用，不重复发出命令 |
| `03-训练模式选择.feature.md` | `选择不同训练卡片` | P2 覆盖 `100m精度射校靶`、`移动目标射击` 示例行的单选状态 |
| `03-训练模式选择.feature.md` | `确认进入所选模式` | P2 覆盖上述两个示例行及移动靶设置路由 |
| `03-训练模式选择.feature.md` | `返回主界面` | 返回时不改变已装备武器和已保存设置 |
| `08-移动靶设置.feature.md` | `默认选择白天模式` | 渲染服务提供的默认模式、合法速度和默认速度 |
| `08-移动靶设置.feature.md` | `切换到夜晚模式` | 刷新夜晚速度列表、选中状态和微光镜预览提示 |
| `08-移动靶设置.feature.md` | `选择目标速度` | 六个示例组合均能把选择提交给 Presenter/服务 |
| `08-移动靶设置.feature.md` | `开始训练` | 单次创建 Session，并按昼夜进入对应 HUD |
| `08-移动靶设置.feature.md` | `返回上一级` | 不创建 Session，按路由历史返回 |

## 交付内容

- 在既有 `Screen_MainMenu` 中接入：
  - `Button_MainMenu_OpenMovingTarget`
  - `Button_MainMenu_OpenArmory`
  - `Button_MainMenu_OpenSettings`
- `Screen_TrainingModeSelection` 的 P2 可用状态：
  - `Button_TrainingModeSelection_SelectZeroing`
  - `Button_TrainingModeSelection_SelectMovingTarget`
  - `Button_TrainingModeSelection_Confirm`
  - `Button_TrainingModeSelection_Back`
  - 当前待确认模式的可见选中态。
- `Screen_MovingTargetSettings`：
  - `Button_MovingTargetSettings_ModeDay`
  - `Button_MovingTargetSettings_ModeNight`
  - 白天速度按钮：`Button_MovingTargetSettings_Day_Speed_3`、`Button_MovingTargetSettings_Day_Speed_4`、`Button_MovingTargetSettings_Day_Speed_5`
  - 夜晚速度按钮：`Button_MovingTargetSettings_Night_Speed_2`、`Button_MovingTargetSettings_Night_Speed_2_5`、`Button_MovingTargetSettings_Night_Speed_3`
  - `Text_MovingTargetSettings_SelectedMode`
  - `Text_MovingTargetSettings_SelectedSpeed`
  - `Panel_MovingTargetSettings_Preview`
  - `Text_MovingTargetSettings_Error`
  - `Button_MovingTargetSettings_Start`
  - `Button_MovingTargetSettings_Back`
- Presenter/绑定脚本：
  - 主菜单和模式卡片只向 `IUIRouter`/对应命令入口转发事件。
  - 使用 `IMovingTargetService.GetAvailableSpeeds` 的结果生成或启用速度选项，不在 View 内保存第二套合法速度表。
  - 开始按钮提交当前 `MovingTargetSettingsDto`，根据服务和路由结果进入 `MovingTargetDayHud` 或 `MovingTargetNightHud`。
  - `Busy`、`InvalidInput`、`ResourceUnavailable` 等失败只更新提示和按钮状态，不先行切页。

## 视觉与交互要求

- 延续 P1 深色半透明军事训练面板、青蓝 HUD 线框和橙色警示色。
- 昼夜与速度必须有明确、互斥的选中状态，确认逻辑读取真实选择值而不是高亮物体本身。
- 切换昼夜后，上一模式专属速度不得继续显示为可选；如果服务修正默认速度，UI 立即以返回 DTO 为准。
- 素材预览区不是按钮，不得拦截开始、返回或速度选择的 XR 射线。
- 页面切换期间所有会创建 Session 或切页的按钮进入 Busy/不可用状态。

## 不包含

- 不实现移动靶状态机、倒计时、端点停留或命中评级。
- 不直接创建 `MovingTargetSession`、加载场景对象或移动目标 Transform。
- 不开放堑壕、城镇完整流程；`03` 中对应 P3 示例行不作为 task008 验收内容。
- 不实现武器库和设置页面内部内容，分别由 task013、task015 负责。
- 不新增独立于 P1 的 XR/无 VR 输入系统。

## 依赖关系

- 前置依赖：task001、task004。
- 可并行：task005、task006、task007。
- 后续依赖：task009、task010、task016、task017。

## 联调说明

- 与 task001 联调：`ScreenId`、UI 命令、路由历史、Busy 和错误码显示策略一致。
- 与 task004 联调：默认昼夜、速度列表、设置提交、Session 单次创建和目标 HUD 路由。
- 与场景 task005 联调：开始训练后的 `MovingTargetRangeScene` 加载与首屏 HUD 状态。
- 与 task013/task015 联调：主菜单武器库、设置入口的 `ReturnToScreen` 保持一致。

## 测试要求

### 无 VR PlayMode 自动化

- 通过稳定测试 ID 定位移动靶、武器库、设置入口；测试点击和输入替身均走正式 UI 事件路径。
- 主菜单移动靶入口与模式选择中的移动靶卡片均进入 `Screen_MovingTargetSettings`。
- 默认白天显示 3m/s、4m/s、5m/s 且选中服务默认值；夜晚显示 2m/s、2.5m/s、3m/s。
- 对 `选择目标速度` 的六个示例逐项验证 Presenter 提交的 `MovingTargetSettingsDto`。
- 开始按钮快速重复触发只创建一个 Session；Busy 时入口保持禁用。
- 白天/夜晚设置分别进入对应 HUD；返回不创建 Session。
- 服务返回 `InvalidInput`、`Busy` 或资源错误时页面不切换并显示可定位提示。

### VR 实机手工验收

- 使用左右任一手柄射线可悬停并确认 P2 入口、昼夜、速度、开始和返回按钮。
- 按钮、模式选中态和速度文本在舒适视野内可读，预览区域不误拦截射线。
- 快速连续点击不会出现重复场景加载、重复 Session 或页面叠层。

## 验收标准

- 三个 P2 主菜单入口和移动靶设置页均能通过正式路由/服务运行。
- 移动靶设置 UI 不包含速度合法性、Session 或状态机业务规则。
- 所有按钮、关键文本和选中态均有稳定 `UITestId`。
- 无 VR PlayMode 测试通过，VR 实机验收项已记录结果。
