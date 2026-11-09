# 主菜单制作人入口与 P3 交互按键第二轮修正（2026-11-08）

## 结果与边界

- 删除 `Button_MainMenu_AboutCreators`、整个 `Panel_MainMenu_AboutCreators` 及其中框架、标题、名单、返回，清掉 `aboutCreatorsPanel`、两个点击监听、面板显隐和该面板专用的射线阻挡设置。主菜单四个训练按钮位置和间距不变，外框下沿从 145 上移至 255，以消除末尾空槽；其他遮挡层、路由和按钮逻辑不变。BDD02 PlayMode 同时检查对象名/测试ID均不存在、按钮等距、框底留白和两次任务说明打开/返回。
- 模式三和四统一左 X 换弹、右手柄摇杆按压交互。`P3XRTrainingInput` 合并 Input System 的 `primary2DAxisClick` / PICO 实际控件 `thumbstickClicked` 与右手 Unity XR `CommonUsages.primary2DAxisClick`，再统一检测按下沿；长按不重复，松开后可重按。左摇杆按压不再交互或换弹；左 Y、右 A/B、双握把及左右摇杆方向保持原职责。PICO 4 官方包 `PICO4ControllerProfile.cs` 声明右/左手通用 `/input/thumbstick/click` 和 `thumbstickClicked`，因此采用首选方案，无须 B 双击备选；PICO 4 实机仍待核验。
- 任务原文称桌面交互是 E，但既有 `InputSystemXRTrainingInput.ConfirmPressed` 实际读取 **Enter、空格或游戏手柄南侧按钮**；E 是 `P3XRTrainingInput` 的后手握持切换。遵照“`desktop.ConfirmPressed` 不变”，本次保留实际行为，HUD 显示 Enter/空格，测试验证 E 不触发确认。若需要 E 开门，须另行解决它与现有握持键的冲突。
- 模式三与模式四的输入适配器均能发出同一 Confirm 命令。现有 `CombatSceneRuntime.Interact()` 仅对 `UrbanService` 处理房门，模式三没有独立可验证的房门服务；本次未新增堑壕开门规则。模式四测试使用 PICO 命名的 Input System 控件经 `P3XRTrainingInput` → `CombatSceneRuntime.Step` → 城镇服务打开真实生产房门，非直接调用开门方法。
- 入口和接口行为先更新 BDD、接口和任务规格，再更新测试及实现；原有工作区未提交改动保留。未提交、未推送、未启动 Unity GUI。

## 本轮改动文件

### 实现与测试

- `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`：删除制作人入口、面板及其绑定，并收短外框。
- `Assets/SimulatedShooting/Runtime/Scene/P3XRTrainingInput.cs`：双模式右摇杆交互、左 X 换弹和边沿去重；兼容 PICO 与旧 XR 回退。
- `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs`：移除逐模式输入开关，使用统一映射。
- `Assets/VRShooting/Runtime/Unity/UI/P3TrenchHudAndResultsUI.cs`：共享交互提示显示右摇杆和桌面确认键。
- `Assets/VRShooting/Runtime/Unity/UI/P3UrbanUI.cs`：城镇街道/建筑 HUD 复用新交互提示，保留服务文本和按钮行为。
- `Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs`：验证入口及子项不存在、按钮等距、外框间距和页面往返。
- `Assets/SimulatedShooting/Tests/PlayMode/P3XRTrainingInputTests.cs`：验证双模式左右摇杆/X隔离、边沿、PICO命名控件及桌面原有确认键。
- `Assets/SimulatedShooting/Tests/PlayMode/P3ProductionFlowTests.cs`：验证 PICO 控件驱动城镇生产门及双模式换弹 HUD。

### 文档与线框

- `docs/README.md`、`docs/需求/阶段化整体需求说明书.md`：登记最新修订和权威顺序。
- `docs/BDD/screens/02-游戏主界面.feature.md`：移除制作人入口场景，增加缺席及导航验收。
- `docs/BDD/screens/23-P3共用战斗核心.feature.md`：更新双模式交互键位、边沿和设备隔离行为。
- `docs/接口文档/01-页面导航与UI事件.md`：删除制作人面板契约。
- `docs/接口文档/11-Unity场景与Prefab约定.md`：同步双模式键位。
- `docs/接口文档/14-P3战斗契约与独立测试基础.md`、`docs/接口文档/15-P3共用战斗核心.md`：同步输入、设备回退和 HUD 契约。
- `docs/接口文档/18-P3应用装配与生命周期.md`、`docs/接口文档/21-训练辅助与镜像左肩持枪.md`：消除相关旧键位冲突。
- `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md`：更新删除入口和验收范围。
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task004-combat-input-weapon-damage-and-enemy.md`、`docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task020-combat-magazine-reload.md`：更新输入职责与回归。
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`：移除主菜单制作人入口，保持其他线框元素。
- 本报告及 `docs/codex-reports/evidence/vr-experience-second-2026-11-08/*.xml`：交付记录和批处理测试证据。

## 验证

Unity 2022.3.62f3c1，运行前确认项目未被 Unity 占用。仅 `-batchmode`，PlayMode 因上一轮无图形设备引擎崩溃而保留图形设备，未打开 GUI。Unity 改写的 OpenXR 设置由运行前副本恢复，SHA-256 前后均为 `45B9F322917B20C7377AC477EB5A31EFBCC38051786A76602AC5F8259F4D220F`；测试生成的原本干净字体和 Prefab 变更也已恢复。

| 验证 | 通过/总数 | 证据 |
|---|---:|---|
| 全量 EditMode | 323/345 | [edit-full.xml](evidence/vr-experience-second-2026-11-08/edit-full.xml) |
| 全量中的战斗弹匣规则子集 | 18/18 | 同上 |
| 最终针对性 PlayMode | 40/40 | [play-targeted.xml](evidence/vr-experience-second-2026-11-08/play-targeted.xml) |
| 桌面确认焦点修复单项 | 1/1 | [play-desktop.xml](evidence/vr-experience-second-2026-11-08/play-desktop.xml) |

PlayMode 40 项由主菜单8、P3输入10、换弹表现9、战斗输入8及生产流程5组成；生产流程含模式三/四左右肩换弹和模式四右摇杆开门。首次 PlayMode 38/40：测试夹具不能直接发送键盘位字段且测试中右手设备选取有误；修复后第二次为39/40，余下一项是 batchmode 键盘焦点设置；按照项目既有输入测试设置焦点后单项及完整重跑通过。首次和中间结果分别保留为 `play-initial.xml`、`play-intermediate.xml`。

全量 EditMode 的 22 个失败与 [上一轮全量结果](evidence/vr-experience-2026-11-07/edit-full.xml) 按完整用例名逐项比对完全一致，无新增失败。覆盖靶场位置/场景数量、射校数值、城镇旧敌人/入口/胜负等历史项。上一轮另有 6 个 PlayMode 历史失败及 P1 两项结算文字失败，本次针对性回归未重跑，仍不能视作通过；详见 [上一轮报告](vr-experience-update-2026-11-07.md)。

### 22 个历史 EditMode 失败

- `SimulatedShooting.Tests.EditMode.CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms`：Expected: 13
- `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")`：Expected: 100.0f +/- 0.0199999996f
- `VRShooting.Tests.EditMode.Application.ZeroingRulesTests.ComputeFrontSightDegrees_UsesCeilingAnd064CmPerDegree`：Expected: 188.0d +/- 0.0099999997764825821d
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.ApplyAdjustment_IsIdempotentForSameRound`：Expected: 188.0d +/- 0.0099999997764825821d
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(-1)`：Expected: -114.240005f +/- 0.00999999978f
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(1)`：Expected: 90.5600052f +/- 0.00999999978f
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd06_VerticalAdjustmentDoesNotTruncateALargeSuggestedAngle(64.0f,-1)`：Expected: greater than 780.0f
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd06_VerticalAdjustmentDoesNotTruncateALargeSuggestedAngle(-64.0f,1)`：Expected: greater than 780.0f
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd30_ApplyingSuggestedCorrectionActuallyZerosTheSameAimAndIsIdempotent`：Expected: less than 0.00100000005f
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd30_HelpRecommendationStaysBasedOnRoundImpactsWhilePendingCorrectionChanges`：Expected: 188
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.Bdd30_ManualPreviewCommitsSignedCorrectionWithoutChangingHistory`：Expected: -12.0d +/- 0.0010000000474974513d
- `VRShooting.Tests.EditMode.Application.ZeroingServiceTests.CompleteRound_CalculatesOffsetDirectionsAndAdjustmentAmounts`：Expected: 188.0d +/- 0.0099999997764825821d
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins`：Expected: True
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(False)`：Expected: 1
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(True)`：Expected: 2
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps`：Expected: True
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult`：Expected: True
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies`：Expected: InvalidState
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime`：Expected: True
- `VRShooting.Tests.EditMode.Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds`：Expected: in range (1,2)
- `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban`：Expected: UrbanStreetHud
- `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)`：Expected: True

## VR 实机待验

在 PICO 4 分别进入模式三/四：验证左 X 换弹、右摇杆按下/长按/松开重按、左右摇杆方向、右 A 手雷、右 B 切肩和左 Y 姿态互不串键；模式四靠近房门验证右摇杆开门，确认 HUD 可读、双肩换弹及跟踪恢复。无 VR 替身和 PICO 配置代码验证不替代设备实测。
