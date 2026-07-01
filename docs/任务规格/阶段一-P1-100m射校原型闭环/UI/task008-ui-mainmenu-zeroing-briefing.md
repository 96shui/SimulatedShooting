# task008 主菜单 100m 入口与任务说明 UI

## 负责人

UI

## 目标

实现 P1 主菜单到 100m 任务说明的 UI 流程，尽量还原参考图视觉风格，并通过路由服务进入 100m 射校准备状态。

## 参考资料

- `UI/Sample/vr-shooting-main-menu-ui.png`
- `UI/Sample/vr-shooting-training-ui-main.png`
- `UI/Sample/vr-shooting-100m-zeroing-briefing-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/接口文档/01-页面导航与UI事件.md`

## 交付内容

- `Screen_MainMenu`
- `Button_MainMenu_OpenZeroing`
- `Screen_ZeroingBriefing`
- `Button_ZeroingBriefing_Start`
- `Button_ZeroingBriefing_Back`
- 任务说明文本：
  - 射击距离 100m。
  - 单发射。
  - 每轮 3 发，共 3 轮。
  - 50cm x 50cm 胸靶。
  - 10 环直径 10cm。
  - 通过条件。
- 胸靶示意区域。
- 对应 Presenter 或绑定脚本，只转发 UI 事件，不计算训练规则。

## 视觉要求

- 参考主菜单和 100m 任务说明参考图。
- 写实军事训练系统风格，深色半透明面板、清晰信息层级。
- 文本可读，不遮挡主要视觉区域。

## 不包含

- 不实现 100m HUD。
- 不实现弹着分析或评级页。
- 不直接创建 Session 以外的场景对象。

## 依赖关系

- 前置依赖：task001、task002。
- 可并行：task004。
- 后续依赖：task009、task014、task015。

## 联调说明

- 与 功能A 联调：按钮调用 `IUIRouter` 和 `ITrainingSessionService`。
- 与 场景 联调：确认任务说明进入后场景/相机状态。

## 测试要求

- PlayMode 测试：
  - 通过测试 ID 找到 100m 入口。
  - 点击后进入任务说明。
  - 点击开始后创建 Session 并进入 HUD 状态。
  - 快速重复点击不会重复创建 Session。

## 验收标准

- UI 布局接近参考图，核心文本完整。
- 所有按钮和关键文本有稳定 `UITestId`。
- UI 不包含评级、偏移、弹药计算逻辑。
- PlayMode 测试通过。
