# 当前工作区运行时 UI 截图交付记录（2026-11-10）

## 范围与方法

- 基准：`HEAD 1d763ec70a8c90ca661d3e9c074aeb676224d7f7`，包含抓取时尚未提交的工作区改动（包括 `MainMenuUI.cs` 和相关测试、BDD/接口文档）；本次没有提交或暂存。
- 共 **18 个可渲染页面/独立面板、22 张状态截图**：原有 16 个界面，加上运行时实际存在的等待取枪面板与堑壕无人机侦察页；另外包含 4 张展开状态。`Screen_ZeroingAwaitingWeaponPickup.png` 对应实际的 `Panel_Training_AwaitingWeaponPickup`，只是截图文件名，不是新造页面。移动靶的 `Screen_MovingTargetSettings` 是 `Screen_MovingTargetSetup` 内的零尺寸测试 ID 别名，不是另一张页面。
- 使用 `TacticalUIGallery.CaptureBatch` 创建隔离相机与运行时 UI 组件，1920×1080 截图；`RUNTIME_GALLERY_OUT` 指向本目录，`RUNTIME_GALLERY_STATES=1` 打开额外状态。关闭状态开关后另行实跑，输出 18 张基本页面，额外展开图为 0；未设置输出变量时源码保留原默认目录 `docs/codex-reports/evidence/ui-art-completion`，本次没有覆盖旧图。
- 这是 Editor 画廊中的**界面预览**，不是实际训练录像：射校、HUD、结算及战斗状态使用 Editor 示例 DTO；射校计算字段通过现有 `ZeroingRules` 生成。HUD 的深色背景来自隔离画廊，并非游戏中真实场景。截图不证明实际场景导航、设备操作或 VR 体验通过验收。
- 画廊只捕获这些运行时 Canvas 页面和面板，不包含 IMGUI 技术叠层：`FirstPersonTrainingWeaponController.OnGUI` 有初始化后按 `showDebugOverlay` 显示的武器调试框，P1/P2 场景序列化值为开启；`CombatSceneFixture.OnGUI` 则提供场景巡检准星和地图。它们不是独立 Screen，未在此截图中单独捕获，不能据此判断真实 PlayMode 中的叠层外观。
- 无人机截图的监控区取自工作区 `Assets/Scenes/BunkersOriginalMode3CombatScene.unity` 的一帧静态相机渲染；高度、速度、用时是示例值。画面中的“LIVE · 实时视频”是实际 UI 的可用视频状态文案，**本次输入不是实时无人机飞行视频**。
- “主菜单的收起/帮助/答案三态”与代码实际结构不同：主菜单只有操作介绍面板，本次截图主菜单收起与操作介绍展开；帮助和答案位于弹着分析页，另外截图该页收起、帮助展开和答案展开三态。展开由真实按钮 `onClick` 驱动，并在截图前检查目标显隐状态。

## 逐张文件与校验

PNG 均为 **1920×1080**；JPEG 均为 **1440×810**，使用 Pillow LANCZOS 缩放、JPEG 质量 88、4:4:4 色度采样。逐张完整解码；PNG 文件至少 50,000 B、灰度方差大于 10、亮度最大值大于 30 且最大值与最小值不同，JPEG 文件至少 10,000 B，并再次验证 JPEG 格式与尺寸。此检查确认文件非纯色/全黑，不等同于玩法和视觉验收。

| 序号 / 界面 | 源 PNG（1920×1080；B） | JPEG（1440×810；B） | 灰度方差 | 结论 |
|---|---|---|---:|---|
| 01 主菜单（收起） | [Screen_MainMenu.png](evidence/current-runtime-2026-11-10/Screen_MainMenu.png) · 2,402,648 | [01-MainMenu.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/01-MainMenu.jpg>) · 305,042 | 1699.19 | 通过 |
| 02 主菜单（操作介绍展开） | [Screen_MainMenu_ControlsOpen.png](evidence/current-runtime-2026-11-10/Screen_MainMenu_ControlsOpen.png) · 1,928,830 | [02-MainMenu-ControlsOpen.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/02-MainMenu-ControlsOpen.jpg>) · 270,709 | 1801.02 | 通过 |
| 03 100m 任务说明 | [Screen_ZeroingBriefing.png](evidence/current-runtime-2026-11-10/Screen_ZeroingBriefing.png) · 1,506,505 | [03-ZeroingBriefing.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/03-ZeroingBriefing.jpg>) · 255,049 | 1559.20 | 通过 |
| 04 100m 等待取枪 | [Screen_ZeroingAwaitingWeaponPickup.png](evidence/current-runtime-2026-11-10/Screen_ZeroingAwaitingWeaponPickup.png) · 1,100,350 | [04-ZeroingAwaitingWeaponPickup.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/04-ZeroingAwaitingWeaponPickup.jpg>) · 163,395 | 851.38 | 通过 |
| 05 100m 射击 HUD | [Screen_ZeroingHud.png](evidence/current-runtime-2026-11-10/Screen_ZeroingHud.png) · 561,689 | [05-ZeroingHud.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/05-ZeroingHud.jpg>) · 128,186 | 1621.59 | 通过 |
| 06 弹着分析（收起） | [Screen_ZeroingImpactAnalysis.png](evidence/current-runtime-2026-11-10/Screen_ZeroingImpactAnalysis.png) · 1,565,235 | [06-ZeroingImpactAnalysis.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/06-ZeroingImpactAnalysis.jpg>) · 251,874 | 1891.92 | 通过 |
| 07 弹着分析（帮助展开） | [Screen_ZeroingImpactAnalysis_HelpOpen.png](evidence/current-runtime-2026-11-10/Screen_ZeroingImpactAnalysis_HelpOpen.png) · 1,590,837 | [07-ZeroingImpactAnalysis-HelpOpen.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/07-ZeroingImpactAnalysis-HelpOpen.jpg>) · 263,738 | 1939.91 | 通过 |
| 08 弹着分析（答案展开） | [Screen_ZeroingImpactAnalysis_AnswerOpen.png](evidence/current-runtime-2026-11-10/Screen_ZeroingImpactAnalysis_AnswerOpen.png) · 1,671,261 | [08-ZeroingImpactAnalysis-AnswerOpen.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/08-ZeroingImpactAnalysis-AnswerOpen.jpg>) · 280,780 | 2125.88 | 通过 |
| 09 射校最终评级（及格） | [Screen_ZeroingFinalRating.png](evidence/current-runtime-2026-11-10/Screen_ZeroingFinalRating.png) · 1,580,562 | [09-ZeroingFinalRating.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/09-ZeroingFinalRating.jpg>) · 245,167 | 1453.47 | 通过 |
| 10 移动靶设置 | [Screen_MovingTargetSetup.png](evidence/current-runtime-2026-11-10/Screen_MovingTargetSetup.png) · 1,018,834 | [10-MovingTargetSetup.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/10-MovingTargetSetup.jpg>) · 165,363 | 860.51 | 通过 |
| 11 移动靶 HUD | [Screen_MovingTargetHud.png](evidence/current-runtime-2026-11-10/Screen_MovingTargetHud.png) · 226,228 | [11-MovingTargetHud.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/11-MovingTargetHud.jpg>) · 66,689 | 297.75 | 通过 |
| 12 移动靶结算 | [Screen_MovingTargetResults.png](evidence/current-runtime-2026-11-10/Screen_MovingTargetResults.png) · 1,019,006 | [12-MovingTargetResults.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/12-MovingTargetResults.jpg>) · 164,844 | 1064.39 | 通过 |
| 13 移动靶结算（帮助展开） | [Screen_MovingTargetResults_HelpOpen.png](evidence/current-runtime-2026-11-10/Screen_MovingTargetResults_HelpOpen.png) · 1,197,657 | [13-MovingTargetResults-HelpOpen.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/13-MovingTargetResults-HelpOpen.jpg>) · 202,699 | 1105.69 | 通过 |
| 14 堑壕地图选择 | [Screen_TrenchMapSelection.png](evidence/current-runtime-2026-11-10/Screen_TrenchMapSelection.png) · 1,290,207 | [14-TrenchMapSelection.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/14-TrenchMapSelection.jpg>) · 204,570 | 1038.70 | 通过 |
| 15 堑壕开场简报 | [Screen_TrenchBriefing.png](evidence/current-runtime-2026-11-10/Screen_TrenchBriefing.png) · 755,644 | [15-TrenchBriefing.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/15-TrenchBriefing.jpg>) · 178,846 | 1210.83 | 通过 |
| 16 堑壕无人机侦察 | [Screen_TrenchDroneRecon.png](evidence/current-runtime-2026-11-10/Screen_TrenchDroneRecon.png) · 1,585,951 | [16-TrenchDroneRecon.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/16-TrenchDroneRecon.jpg>) · 144,938 | 973.95 | 通过 |
| 17 堑壕作战 HUD | [Screen_TrenchHud.png](evidence/current-runtime-2026-11-10/Screen_TrenchHud.png) · 674,419 | [17-TrenchHud.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/17-TrenchHud.jpg>) · 143,102 | 830.99 | 通过 |
| 18 堑壕任务结算 | [Screen_TrenchResults.png](evidence/current-runtime-2026-11-10/Screen_TrenchResults.png) · 838,543 | [18-TrenchResults.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/18-TrenchResults.jpg>) · 160,781 | 1058.77 | 通过 |
| 19 城镇地图选择 | [Screen_UrbanMapSelection.png](evidence/current-runtime-2026-11-10/Screen_UrbanMapSelection.png) · 1,298,765 | [19-UrbanMapSelection.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/19-UrbanMapSelection.jpg>) · 206,872 | 1111.03 | 通过 |
| 20 城镇街道 HUD | [Screen_UrbanStreetHud.png](evidence/current-runtime-2026-11-10/Screen_UrbanStreetHud.png) · 581,008 | [20-UrbanStreetHud.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/20-UrbanStreetHud.jpg>) · 137,993 | 1037.83 | 通过 |
| 21 城镇建筑 HUD | [Screen_UrbanBuildingHud.png](evidence/current-runtime-2026-11-10/Screen_UrbanBuildingHud.png) · 622,546 | [21-UrbanBuildingHud.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/21-UrbanBuildingHud.jpg>) · 146,646 | 1081.78 | 通过 |
| 22 城镇任务结算 | [Screen_UrbanResults.png](evidence/current-runtime-2026-11-10/Screen_UrbanResults.png) · 774,953 | [22-UrbanResults.jpg](<C:/Users/10532/WorkBuddy/Claw/send/current/22-UrbanResults.jpg>) · 162,974 | 1008.68 | 通过 |

## 未截图：运行时不存在的独立界面

- **训练模式选择**：`ScreenId.TrainingModeSelection` 和 BDD03 存在，但实际主菜单直接提供四个训练入口；当前 UI 代码没有构建独立 `Screen_TrainingModeSelection`。
- **小队指挥轮盘**：`ScreenId.SquadCommandRadial` 和 BDD16 存在，但 BDD16 将其列为 Later、P3 UI 任务明确不制作；运行时没有对应界面，当前 P3 输入实现的 `CommandMenuHeld` 为 `false`。
- **视觉风格参考页**：`ScreenId.VisualStyleReference` 存在，BDD01 是通用视觉风格规范；当前运行时没有构建独立 `Screen_VisualStyleReference`。
- **堑壕无人机侦察不属于缺失项**：`P3CombatUIRoot.BuildDroneRecon` 构建 `Screen_TrenchDroneRecon`，`P3DroneReconView` 和运行时控制器负责显示，见上表第 16 项。

依据：`Assets/VRShooting/Runtime/Enums/ScreenId.cs`、`Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`、`Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs`、`Assets/VRShooting/Runtime/Unity/UI/P3LiveUIController.cs`、`Assets/SimulatedShooting/Runtime/Scene/P3XRTrainingInput.cs`、BDD01/03/16/28 及 P3 UI task002/task005/task018。

## 编译与渲染记录

- 最终 Unity 2022.3.62f3c1 batchmode 进程退出码 **0**，[最终 capture.log](evidence/current-runtime-2026-11-10/capture.log) 包含 `Batchmode quit successfully invoked`，没有 C# 编译错误；22 张图片通过上述文件检查，人工查看了主菜单、操作介绍、分析答案、最终评级、无人机页及全套缩略图。
- 开发中的一次中间编译失败，原始错误为 `Assets\VRShooting\Editor\TacticalUIGallery.cs(123,44): error CS0117: 'EditorSceneManager' does not contain a definition for 'OpenPreviewScene'`。原因是 Unity 2022 没有该 API；已改为该版本支持的附加场景读取并重新成功编译、截图。原始过程保存在 [中间编译错误日志](evidence/current-runtime-2026-11-10/capture-intermediate-compile-error.log)。
- 最终日志仍有授权客户端 `LicensingClient has failed validation; ignoring` / `Access token is unavailable; failed to update` 消息，以及退出时 `Call to StopSubsystems without an initialized manager` 的 XR 警告；授权 entitlement 随后成功，进程退出码为 0，未阻止本次渲染。没有据此声称 VR 实机已验证。
- Unity 抓取期间动态字体资源曾被编辑器写回；该文件在任务开始时是干净状态，任务结束前已恢复本次生成的改动。未修改 Runtime 生产代码、依赖、ProjectSettings 或 XR 配置；未清理 Library，未删除 Exports。
