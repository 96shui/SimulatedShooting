# task015 P1 UI 参考图还原与测试 ID 收口

## 负责人

UI

## 目标

对 P1 所有 UI 进行统一检查，确保视觉尽量贴近参考图，所有关键控件具备测试 ID，且无 VR/VR 两种视角下信息可读。

## 参考资料

- `UI/Sample/vr-shooting-main-menu-ui.png`
- `UI/Sample/vr-shooting-training-ui-main.png`
- `UI/Sample/vr-shooting-100m-zeroing-briefing-ui.png`
- `UI/Sample/vr-shooting-100m-zeroing-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-100m-impact-analysis-ui.png`
- `UI/Sample/vr-shooting-100m-final-rating-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## 交付内容

- P1 UI 视觉检查和调整。
- 所有 P1 页面根节点、按钮、文本、HUD 元素具备稳定测试 ID。
- 所有按钮禁用/Busy 状态检查。
- HUD 不遮挡靶标和瞄准点。
- UI 文本不溢出、不重叠。
- MainScene 在无 VR 时保留屏幕空间 UI 和鼠标/测试替身交互，在 OpenXR 运行时自动切换为头显前方的 World Space Canvas。
- VR Canvas 使用 `TrackedDeviceGraphicRaycaster`，全局 EventSystem 使用支持桌面与 XR 的 `XRUIInputModule`；左右手 Near-Far/Ray Interactor 的 UI 射线可悬停并确认按钮。
- MainScene 根据 XR Display 运行状态在桌面跟随相机和 HMD Camera 间自动切换，任一时刻只保留一个活动 Camera 和一个 AudioListener。
- VR World Space Canvas 在跟踪姿态稳定窗口内随最新 HMD 水平方向重新摆放，默认距离 1.2m 至 1.5m、中心略低于水平视线，水平可读视角不小于 65°。
- 场景地面只作为 UI 下沿安全约束；画布下沿不得低于地面 0.10m，低位跟踪姿态不得让主菜单或 HUD 陷入地面。
- 提供 P1 UI 验收截图或说明。

## 不包含

- 不新增 P2/P3 页面。
- 不改变服务接口。

## 依赖关系

- 前置依赖：task008-task011、task014。
- 后续依赖：task016。

## 联调说明

- 与 功能A 联调：确认所有 DTO 字段都有 UI 映射。
- 与 功能B 联调：确认换弹、肩侧、禁射提示可见。
- 与 场景 联调：确认 HUD 空间位置和靶场视线。

## 测试要求

- PlayMode 测试：
  - 通过测试 ID 找到所有 P1 关键控件。
  - Busy 状态下重复点击无效。
  - HUD 字段随 DTO 更新。
  - 无 VR 模式下 Canvas 为 `ScreenSpaceOverlay`，桌面 Graphic Raycaster 可用。
  - VR 输入替身模式下 Canvas 为 World Space，绑定 HMD Camera，且 Tracked Device Graphic Raycaster 能命中 `Button_MainMenu_OpenZeroing`。
  - MainScene 的 VR/无 VR 模式切换后分别只有一个活动 Camera 和一个 AudioListener。
  - VR 相机使用显式 Floor Tracking Origin，Camera Y Offset 与 Camera Floor Offset Object 初始 Y 均为 0m，不叠加固定站立眼高。
  - HMD 输入替身从启动低位姿态移动到站立姿态后，主菜单和靶场 UI 会在稳定窗口内重新摆放；画布距离为 1.2m 至 1.5m、水平可读视角不小于 65°、下沿不低于地面 0.10m。
  - EventSystem 只有一个活动输入模块，并使用 `XRUIInputModule` 同时承接桌面和 XR 输入。

## 验收标准

- P1 UI 与参考图主要结构和风格一致。
- 所有 P1 BDD 中出现的按钮和关键文本可由测试 ID 定位。
- 无 VR 测试和 VR 视角下核心信息可读；真实 VR 中可用任一手柄射线悬停并点击主菜单与任务说明按钮。
- PlayMode UI 测试通过。
