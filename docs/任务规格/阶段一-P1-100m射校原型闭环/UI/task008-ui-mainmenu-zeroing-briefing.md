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

2026-10-03 用户追加 SteamVR 后扳机 UI 排查：保持 VR UI 射线、XRUIInputModule 及 UI Press 动作启用，UI 点击兼容数字扳机与模拟 trigger 阈值，不因物体选择而阻断菜单。BDD02「SteamVR 手柄后扳机点击主菜单」与接口11约定同步；更新 SceneOwnedUIFlowTests 的射线恢复检查。本轮按用户要求不运行测试，VR 实机点击仍待确认。

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

2026-09-29 主菜单 VR 输入回归：左摇杆只移动、右摇杆只转向，禁止双手同时驱动移动/转向或平滑与离散转向叠加。对应 BDD 02「主菜单双摇杆各司其职」，通过无设备 XR 输入替身验证。

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

2026-10-03 继续排查：确认 XRI 3.1.2 在远距离投射关闭时会重置 UI 模型，补充恢复 enableFarCasting 和射线父层级激活；左手 NearFar Prefab 关闭物体选择阻断 UI（右手继承该 Prefab）。补充既有输入替身恢复断言，未运行测试、未验证 VR 实机。
