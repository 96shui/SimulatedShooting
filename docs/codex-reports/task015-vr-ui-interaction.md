# task015 交付报告：MainScene VR UI 接入

- 日期：2026-07-22
- 工作分支：`feature/task015-vr-ui-interaction`
- 基线：`origin/main` @ `2362d27a3a1695e1a7acc46875b6f951a5ed0e1d`
- 状态：代码与自动化验证已完成；真实 VR 最终复验待用户执行

## 交付结果

- MainScene UI 在 XR Display 运行时自动切换为头显前方约 1.75m 的 World Space Canvas；无 VR 时继续使用 Screen Space Overlay。
- VR 模式使用 `TrackedDeviceGraphicRaycaster`，桌面模式使用 `GraphicRaycaster`；共享 EventSystem 统一使用 `XRUIInputModule`，同时保留鼠标、触摸和手柄输入。
- MainScene 自动在 HMD Camera 与桌面 `PlayerFollowCamera` 之间切换，并保证只有一个活动 Camera 和 AudioListener。
- MainScene 的 XR Origin 与 `Player` 改为场景所有，不再跨场景残留或禁用后续 HMD Camera；场景切换时避免重复 EventSystem 与 XR Interaction Manager。
- UI 与场景测试增加稳定的无设备 VR 替身：验证 tracked ray 命中并点击 `Button_MainMenu_OpenZeroing`、Canvas 模式、XR 输入模块、左右手 Near-Far UI 交互和相机互斥。
- 修正全量测试中的两处隔离问题：HUD 测试限定在自身 UI 根节点查询；抓取测试使用 ManualValue 输入替身，避免无 XR 硬件时读取动作状态。

## 主要文件

- `Assets/VRShooting/Runtime/Unity/UI/TrainingUICanvasAdapter.cs`
- `Assets/VRShooting/Runtime/Unity/UI/MainMenuXRModeController.cs`
- `Assets/VRShooting/Runtime/Unity/UI/TrainingUIHost.cs`
- `Assets/VRShooting/Runtime/Unity/Bootstrap/GameMain.cs`
- `Assets/VRShooting/Runtime/Unity/Player/Player.cs`
- `Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs`
- `Assets/VRShooting/Tests/PlayMode/UI/Screen02_05_SceneOwnedUIFlowTests.cs`

## 自动化验证

- task015 主菜单/UI 定向 PlayMode：7/7 通过。
- MainScene 生命周期与 VR/桌面模式切换 PlayMode：2/2 通过。
- 全量 EditMode：73/73 通过。
- 全量 PlayMode：46/46 通过。
- 上述运行确认 MainScene 稳定态没有重复 EventSystem、XR Interaction Manager 或 AudioListener 警告。
- Unity 2022.3.62f3c1 最终批处理日志无 C# 编译错误/警告、未处理异常、重复 EventSystem 或重复 AudioListener 报告。

## 真实 VR 验收项

1. 从 MainScene 启动，确认菜单在头显前方清晰可读且不会跟随头部每帧漂移。
2. 左右手控制器分别用 UI 射线悬停并点击“100m 射校”。
3. 在任务说明页点击开始与返回，确认 hover、press 和页面切换正常。
4. 确认头显画面没有桌面相机争抢、黑屏、双 AudioListener 或重复 EventSystem 警告。

## 评审重点

- 检查 XR Display 热切换时 Canvas、HMD Camera 和桌面相机的互斥生命周期。
- 检查 `TrainingUIHost` 采用单一 `XRUIInputModule` 后桌面鼠标路径没有回归。
- 检查 World Space Canvas 的距离、比例与位置是否满足目标头显的舒适度；这部分必须以真实 VR 为最终证据。
