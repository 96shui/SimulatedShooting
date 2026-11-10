# 模式一随机偏移、调整后通过与主菜单操作介绍

日期：2026-11-09。基线：`feea04c`；Unity 2022.3.62f3c1。改动保留在工作区，未 commit / push。任务开始时已有未跟踪的 `Exports/`，本次未处理。

## 实现与验证范围

### 1. 四象限随机偏移

- `Assets/VRShooting/Runtime/Application/ZeroingRules.cs`：X、Y 分别用独立的随机符号和整数采样得到绝对值 `[5, 20]cm`，按 0.01cm 取样，5 和 20 均可取到。固定种子仍可重现。
- `Assets/VRShooting/Runtime/Contracts/RandomSeed.cs`：`CreateRandom()` 使用系统随机数产生非固定种子，并在当前进程内避免重复种子；`Fixed` 和原有 `Unfixed` 保持可用。`MainMenuUI.OnStartZeroingClicked` 和 `ZeroingRangeSessionBootstrap.EnsureZeroingSession` 改用该来源；后者的 `ConfigureForTests(..., seed)` 允许注入固定种子。
- `ComputeImpactPoint` 的原有靶面夹取没有更改。规则测试检查四象限、两轴上下端点及中心瞄准坐标；PlayMode 用真实场景 `TargetImpactSurface` 核对四个 `(±20, ±20)cm` 点都在靶面内，坐标没有被压到边缘，四个标记均生成。

### 2. 应用调整后偏移 ≤5cm 通过

- `ZeroingRules.BuildRoundAnalysis` 接收本轮是否应用调整和当前 `FixedImpactOffsetCm`。本轮完成三发后，原“三发全进十环”与“已应用量化调整且固定偏移模长 ≤5cm”任一路径成立即通过；边界含等号。
- `ZeroingService.ZeroingSessionRecord.BuildAnalysis` 传入当前偏移；`ApplyAdjustment` 写入量化补偿后立即更新分析及 `FinalResultAvailable`，保留应用幂等和历史弹着点。点击下一步时可直接进入评级，无需再次射击。
- `MainMenuUI` 更新简报和分析页的通过说明、应用状态和主按钮文案；`FinalResultAvailable` 或已通过时主按钮为“查看评级”。评级仍按第 1/2/3 轮通过分别为优秀/良好/及格，三轮未通过为不及格；第三轮原有直接结算流程保持不变。
- 新增测试覆盖 `(3,4)cm` 恰好 5cm、略大于 5cm、尚未应用、第一轮直接优秀、第二轮调整后良好、幂等和原始弹着不变；PlayMode 覆盖点击应用后直接查看评级。

### 3. 主菜单操作介绍

- `MainMenuUI.BuildMainMenu` 新增 `Button_MainMenu_Controls`，在四个训练入口下方；只延长 `Panel_MainMenu_Frame` 下沿。四个训练按钮的坐标和尺寸均由 EditMode 逐一断言保持原值。
- 新增默认隐藏的 `Panel_MainMenu_Controls`、双栏文字和 `Button_MainMenu_Controls_Back`，复用项目的面板、按钮与既有 XR UI 射线。EditMode 检查稳定 ID、默认隐藏、开关、关键字、字号和文本溢出；PlayMode 检查按钮开关、路由不变及原主菜单门禁。
- [操作面板无头渲染截图](evidence/zeroing-controls-2026-11-09/controls.png)经检查，双栏完整、返回按钮可见；这不等同于 PICO 4 实机可读性验收。

### 手柄绑定逐项源码核对

- 通用：`InputSystemXRTrainingInput` 的右/左手 `gripPressed`、射击扳机及 `MainMenuXRModeController` / 现有 XR UI 射线；主菜单与 P3 左摇杆移动、右摇杆转向。P1/P2 固定射击点，不允许人工移动。
- 模式一：`ZeroingRules` / `ZeroingService` 为三发、最多三轮，水平 2cm / 格、垂直 0.064cm / 度 × 5 度；分析动作通过现有 UI 按钮。
- 模式二：`FirstPersonTrainingWeaponController` 在 VR 姿态路径直接使用真实头手和枪体姿态，自然据枪对齐瞄具；`InputSystemXRTrainingInput.AimHeld` 的可用项为桌面/普通手柄，并非额外的 VR 瞄准键。P2 发射行为为两发起射与继续按住逐发连射。
- 模式三 / 四：`P3XRTrainingInput` 将左手 primary / secondary 映射为 X 换弹 / Y 姿态，右手 secondary 映射为 B 切肩；`CombatInputController` 和 `InputSystemXRTrainingInput` 在左肩时切换主副手和左扳机。
- 与题目列举不同的两点，以实现为准：`CombatSceneRuntime.AdvanceGrenadeCommand` 限定模式三，右手 A 请求**队友投掷手雷**；模式四不执行该命令。`CombatSceneRuntime.Interact` 只接受 `UrbanService`，右摇杆按压交互 / 开门因此只写在模式四。未修改 P2/P3/P4 战斗逻辑。

## 测试结果

仅运行相关规则/服务、主菜单及射校 UI / 场景、P3 输入定向集。Unity 以批处理方式运行，未启动 Unity GUI、未删除 `Library`；启动前无 Unity 进程占用。测试过程中系统未连接 VR 头显。

| 定向测试 | 通过 | 失败 | 跳过 | 结果文件 |
|---|---:|---:|---:|---|
| EditMode：射校规则/服务/HUD、主菜单操作介绍 | 52 | 9 | 0 | [EditMode XML](evidence/zeroing-controls-2026-11-09/editmode.xml) |
| PlayMode：主菜单、射校 UI/流程/标记、P3 输入 | 53 | 3 | 0 | [PlayMode XML](evidence/zeroing-controls-2026-11-09/playmode.xml) |

其中：主菜单 EditMode 5/5；主菜单与场景归属门禁 PlayMode 13/13；分析页 PlayMode 13/13；20cm 场景标记及场景固定种子注入 2/2；P3 两组输入测试 18/18。新需求专门新增的边界、调整后首轮和第二轮评级、菜单开关与标记用例均通过。上述分类为完整测试集的子集，不另作总数累加。

最初的 TDD 定向红灯为 0/2：旧实现的每轴范围测试和应用后立即通过测试均失败。最终完整定向集存在以下失败，**均计入失败而非通过**。这些用例是任务前已有测试；未另行运行干净基线，但相关旧公式和旧结算文本可在 HEAD 中核对。未为消除失败而扩大本任务的玩法规则范围。

EditMode 9 项：

1. `ZeroingRulesTests.ComputeFrontSightDegrees_UsesCeilingAnd064CmPerDegree`：期望 188，现行 5 度量化给出 190。
2. `ZeroingServiceTests.ApplyAdjustment_IsIdempotentForSameRound`：旧断言期望累计 188 度，现行量化补偿记账为 185 度。
3. `ZeroingServiceTests.Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(-1)` 与 `(1)`：旧断言要求按 0.064cm 单步无限累加，现行规则为 0.32cm 步长并在量化时约束至 ±50cm。
4. `ZeroingServiceTests.Bdd06_VerticalAdjustmentDoesNotTruncateALargeSuggestedAngle(64,-1)` 与 `(-64,1)`：旧断言要求用靶外 64cm 计算建议，现行平均值排除靶外弹着，得到 0。
5. `ZeroingServiceTests.Bdd30_HelpRecommendationStaysBasedOnRoundImpactsWhilePendingCorrectionChanges`、`CompleteRound_CalculatesOffsetDirectionsAndAdjustmentAmounts`：旧断言期望 188 度，现行建议为 190 度。
6. `ZeroingServiceTests.Bdd30_ManualPreviewCommitsSignedCorrectionWithoutChangingHistory`：旧断言期望 -12cm，现行按 0.32cm 量化为 -11.84cm。

PlayMode 3 项：

1. `P1FlowPlayModeGateTests.P1Flow_InputStub_Round1Pass_ShowsExcellentFinalRating` 和 `Screen07_ZeroingFinalRatingUITests.Screen07_FirstRoundPassShowsExcellentFinalRating`：期望 `Text_ZeroingFinalRating_Rounds` 含“第1轮：通过”；现有该文本显示逐发坐标，状态位于另一摘要文本。
2. `P1FlowPlayModeGateTests.P1Flow_InputStub_ThreeRoundsFail_ShowsFailFinalRating`：期望同一坐标文本含“第3轮：未通过”，现有文本未含该句。

## 旧测试与夹具同步清单

- `ZeroingRulesTests.GenerateFixedOffset_IsInsideTargetOutsideTenRing`：原先按半径不超过 25cm 判断，已改为每轴 5–20cm、端点、四象限与中心瞄准不夹边；新的方形范围允许偏移模长大于 25cm。
- `ZeroingServiceTests.Bdd30_ApplyingSuggestedCorrectionActuallyZerosTheSameAimAndIsIdempotent`：原先要求补偿绝对归零并进入下一轮；改为核对水平/垂直量化剩余误差、应用幂等、原始记录与立即通过。
- `Screen02_04_MainMenuZeroingBriefingUITests`：移除“面板下沿必须紧贴第四按钮”的旧布局断言，改为验证操作介绍按钮位于第四项下方且在延长后的边框内；保留原科目导航和制作人入口不存在的门禁。
- `Screen06_ZeroingImpactAnalysisUITests` 与 `P1FlowPlayModeGateTests`：按目标弹着建立测试输入，并在需要继续到下一轮的测试中明确布置调整后仍大于 5cm 的场景，避免随机偏移或新通过路径使测试偶发跳到终局。

## 文档同步

- `docs/README.md`
- `docs/BDD/screens/02-游戏主界面.feature.md`、`04-100m任务说明.feature.md`、`06-弹着分析.feature.md`、`07-射校最终评级.feature.md`
- `docs/接口文档/01-页面导航与UI事件.md`、`04-100m射校服务.md`
- P1 任务规格：`功能A-玩法流程与规则/task006-zeroing-core-rules-service.md`、`UI/task008-ui-mainmenu-zeroing-briefing.md`、`UI/task010-ui-impact-analysis.md`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`：主菜单增加操作介绍入口及独立的双栏面板示意页。

## 遗留风险和实机验收

- 用户已接受：默认调整一次抵消本轮平均弹着；稳定居中瞄准时通常第一轮调整后即达到 ≤5cm 并获得“优秀”。未添加单轮修正上限。
- 随机种子在单次进程内不重复，但随机输出只有有限的离散组合，两局偏移仍可能偶然相同；固定种子行为可复现。
- 9 项 EditMode 和 3 项 PlayMode 旧用例仍失败，按仓库交付门禁，本交付尚不能宣称全量测试通过；需要后续明确历史测试与现行契约的取舍后处理。
- 尚未进行 PICO 4 实机验收：需核对菜单文字可读性、UI 射线打开/返回、四象限最大偏移的场景弹孔、调整后立即评级，以及模式二自然据枪和模式三/四手柄绑定。无 VR 测试替身和编辑器渲染不能代替这些项目。
- Unity 测试自动改写的 OpenXR 设置和动态字体缓存已恢复为任务开始前版本；没有清理其他工作区文件。
