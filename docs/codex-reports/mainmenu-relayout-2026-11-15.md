# 主菜单按钮重排与标题条高亮（B 批，2026-11-15）

本次按用户给定坐标调整主菜单右侧入口，最终顺序为操作介绍 → 选择训练科目 → 01 → 02 → 03 → 04。只有“选择训练科目”标题条高亮；01、02、03、04 和操作介绍均使用次要按钮样式。标题条保留稳定 ID，不包含 Button/Selectable，所有子 Graphic 均不接收射线。

起始 HEAD：`ef5a00e4228dde18d01e672937205ef66e4fecb4`。开工工作区仅有未跟踪的 `docs/codex-reports/pico-apk-2026-11-14.md`。本任务归属 P1/UI task008，修订依据为 BDD02 的“2026-11-15 主菜单入口重排”及接口01同日节。

## 改动文件

| 文件 | 变化 |
|---|---|
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs` | 六行坐标及创建顺序；01 传 `primary: false`；标题条内部亮色填充 |
| `Assets/VRShooting/Runtime/Unity/UI/TacticalUIStyle.cs` | 最终样式步骤保留标题条 primary 九宫格、橙描边、深色28号粗体文字与非交互属性；移除按 OpenZeroing 名称强制高亮的规则 |
| `Assets/VRShooting/Tests/EditMode/Application/MainMenuControlsTests.cs` | 四组精确坐标更新；新增完整边界/间距、标题视觉/非交互和所有次要入口视觉一致性测试 |
| `Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs` | 世界坐标中操作介绍位于标题上方、Frame上下界、128px中心距；保留原点击/导航/射线覆盖 |
| `docs/BDD/screens/02-游戏主界面.feature.md` | 替换过期布局语句，增加同日行为规格、坐标表与验收引用 |
| `docs/接口文档/01-页面导航与UI事件.md` | 更新布局引用并记录标题条的纯展示契约；服务/DTO/事件不变 |
| `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md` | 追加本批交付边界、测试与出图要求，覆盖历史“科目坐标不变”条款 |
| `docs/codex-reports/evidence/mainmenu-relayout-2026-11-15/` | 两张截图、原始测试XML、逐例比较脚本/JSON/CSV及资源清理记录 |
| `docs/codex-reports/mainmenu-relayout-2026-11-15.md` | 本报告 |

## 坐标前后对照

参考1920×1080，y向上，所有六行的 x 始终为1360–1750，宽390px。

| 元素 / TestId | 旧 offsetMin | 旧 offsetMax | 新 offsetMin | 新 offsetMax |
|---|---|---|---|---|
| 操作介绍 / `Button_MainMenu_Controls` | (1360,155) | (1750,225) | (1360,780) | (1750,865) |
| 标题 / `Panel_MainMenu_Menu` | (1360,805) | (1750,865) | (1360,667) | (1750,737) |
| 01 / `Button_MainMenu_OpenZeroing` | (1360,665) | (1750,750) | (1360,539) | (1750,624) |
| 02 / `Button_MainMenu_OpenMovingTarget` | (1360,535) | (1750,620) | (1360,411) | (1750,496) |
| 03 / `Button_MainMenu_Trench` | (1360,405) | (1750,490) | (1360,283) | (1750,368) |
| 04 / `Button_MainMenu_Urban` | (1360,275) | (1750,360) | (1360,155) | (1750,240) |

总纵向范围仍为155–865；相邻六行间隙均为43px，01–04中心距为128px。Frame仍为(1300,130)–(1810,920)，底部状态条仍为(120,55)–(1800,115)。自动断言检查所有行处于Frame内、相互不重叠且完全位于状态条上方。

## 高亮实现与边界

最终样式系统会重设短面板贴图、去掉粗体，并根据 OpenZeroing 名称强制设置 primary，因此不能只修改 AddPanel 的初始填充。本次在最终样式步骤对精确名称 `Panel_MainMenu_Menu` / `Text_MainMenu_Menu` 应用标题样式，同时移除 OpenZeroing 的强制 primary 条件；工程内该规则只命中本次01入口。

标题外框使用现有 `button_primary_orange`、Image.Type.Sliced，保持白色贴图乘色以保留橙色边框，并增加 Color32(247,185,85,255) 描边。原贴图中心像素为不透明深色，单纯乘亮色无法形成亮底，因此在边框内新增非交互 Image `Image_MainMenu_MenuFill`，颜色严格为 Color32(200,255,106,235)，置于文字之前。文字仍由 AddPanel 的 Panel_→Text_ 规则命名为 `Text_MainMenu_Menu`，内容“选择训练科目”，28号、Bold、深色 Color32(7,16,13,255)。没有编辑或重生成贴图资源。

左侧档案、主副标题、底部状态条和操作介绍面板内部均未调整；原有回调、路由、TrainingMode 与 `trenchButton.interactable = urbanButton.interactable = services.CombatScenesAvailable` 保持不变。没有单独运行敌人视野或其他模式的专项工作；按要求执行的全量测试包含其既有用例。

## 有意更新的既有断言

以下是精确规格更新，未删除或 Ignore 用例，也未扩大容差。EditMode方法沿用历史名 `Bdd02_TrainingButtonGeometryIsUnchanged`；参数变化会改变NUnit fullname，比较脚本显式映射下列四组旧参数到新参数，不将它们当作删例。

| 测试名 | 旧断言 | 新断言 | 需求依据 |
|---|---|---|---|
| `MainMenuControlsTests.Bdd02_TrainingButtonGeometryIsUnchanged("Button_MainMenu_OpenZeroing",...)` | offsetMin=(1360,665)，offsetMax=(1750,750) | offsetMin=(1360,539)，offsetMax=(1750,624) | 01移到高亮标题下方，85px高度不变 |
| `MainMenuControlsTests.Bdd02_TrainingButtonGeometryIsUnchanged("Button_MainMenu_OpenMovingTarget",...)` | (1360,535)–(1750,620) | (1360,411)–(1750,496) | 02新精确坐标，距01中心128px |
| `MainMenuControlsTests.Bdd02_TrainingButtonGeometryIsUnchanged("Button_MainMenu_Trench",...)` | (1360,405)–(1750,490) | (1360,283)–(1750,368) | 03新精确坐标，距02中心128px |
| `MainMenuControlsTests.Bdd02_TrainingButtonGeometryIsUnchanged("Button_MainMenu_Urban",...)` | (1360,275)–(1750,360) | (1360,155)–(1750,240) | 04移到入口列最下方，底界155 |
| `Screen02_04_MainMenuZeroingBriefingUITests.Screen02_CreatorsEntryAndPanelAreAbsentAndNavigationRemainsUsable` | `controlsCorners[1].y < buttonCorners[0].y`（操作介绍顶端低于04底端） | `controlsCorners[0].y > panelCorners[1].y`（操作介绍底端高于标题顶端） | 操作介绍从底部移到标题上方 |
| 同一PlayMode测试 | 仅检查 `controlsCorners[0].y >= frameCorners[0].y` | 保留原下界，并新增 `controlsCorners[1].y <= frameCorners[1].y` | 顶部操作介绍仍必须完全位于Frame内 |
| 同一PlayMode测试 | 相邻科目中心差相等，未指定数值 | 保留原等距断言，再逐对检查 `(centers[i]-centers[i+1])/canvasScaleY == 128f`，`Within(.1f)` | 新布局明确要求128px，按画布世界缩放还原设计像素 |

`GetWorldCorners` 的0点为左下、1点为左上，实际世界y轴向上；PlayMode使用这些方向验证上下关系。其他既有断言保持原语义。

新增两个EditMode测试，不替代原用例：

- `Bdd02_ControlsAboveHeadingAndAllMenuRowsStayInsideFrame`：操作介绍/标题精确坐标、顺序、六行43px间隙、Frame四边与状态条边界。
- `Bdd02_HeadingUsesPrimaryVisualWithoutInteractionAndEntriesUseSecondaryVisual`：primary贴图、Sliced、指定亮色填充、橙描边、28号粗体深字、无Selectable/射线；01/03/04/操作介绍与02贴图、颜色、悬停贴图和文字颜色一致。

## 测试结果与基线逐例比较

按要求先跑两个指定类，再跑EditMode、PlayMode全量；每次等待前一个Unity进程退出后启动下一个。使用本机Unity 2022.3.62f3c1，测试参数为 `-batchmode -projectPath <repo> -runTests -testPlatform <EditMode|PlayMode> -testResults <xml> -logFile <log>`；定向额外传入完整类名的 `-testFilter`，测试不传 `-quit` 或 `-nographics`。

| 范围 | 总数 | 通过 | 失败 | 跳过 / 不确定 |
|---|---:|---:|---:|---:|
| EditMode定向：MainMenuControlsTests | 7 | 7 | 0 | 0 / 0 |
| PlayMode定向：Screen02_04_MainMenuZeroingBriefingUITests | 10 | 10 | 0 | 0 / 0 |

| 平台 | 基线 总数 / 通过 / 失败 | 本次 总数 / 通过 / 失败 | 新增测试 | 新增失败 | 缺失 / 跳过 |
|---|---|---|---:|---:|---|
| editmode | 389 / 369 / 20 | 391 / 371 / 20 | 2 | **0** | 0 / 0 |
| playmode | 322 / 299 / 23 | 322 / 299 / 23 | 0 | **0** | 0 / 0 |

两套全量均完整运行一遍。基线取自上一轮 `round6-2026-11-14/editmode-full.xml` 和 `playmode-full.xml`，分别为389/369/20及322/299/23，逐字节复制为本目录的 `baseline-editmode.xml` / `baseline-playmode.xml`。没有使用该轮更早的385/315条基线文件。

[compare-tests.py](evidence/mainmenu-relayout-2026-11-15/compare-tests.py) 对711条基线用例逐条比较完整fullname、状态、原始失败消息和堆栈，仅对上表四组坐标参数作显式旧→新映射；新增2条均通过。没有丢失用例，没有测试状态变化，跳过与不确定均为0。完整713行结果见 [fullname-comparison.csv](evidence/mainmenu-relayout-2026-11-15/fullname-comparison.csv) 与 [baseline-comparison.json](evidence/mainmenu-relayout-2026-11-15/baseline-comparison.json)，原始XML也位于同一目录。

43条失败均继承自基线。EditMode20条失败消息全部相同；PlayMode21条消息相同，另外两条差异如下，原始堆栈均相同，测试源码和断言没有修改：

| 历史失败 | 基线消息与本次差异 | 判定 |
|---|---|---|
| `BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring` | 只有随机Session ID变化；两次范围均为2.018377–3.428377，实际均为1.92627943 | 同一敌人落地高度历史失败 |
| `BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry` | 期望均为2.17788005±0.0799999982；实际1.92736256→1.92739356，差0.000031m | 同一约0.25m高度偏差历史失败 |

以下逐条列出基线43条失败，其他全部基线通过项保持通过：

| 平台 | 完整测试名 | 基线 → 本次 |
|---|---|---|
| editmode | `SimulatedShooting.Tests.EditMode.CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/MovingTargetRangeScene.unity","MovingTargetRange")` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/ZeroingRangeScene.unity")` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/MovingTargetRangeScene.unity")` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SharedPrefabMuzzleMatchesActualMeshOpening` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.VrMechanicalSightCalibrationTests.Bdd33_ImportedMeshHasAnOpenRearApertureAndTheMeasuredFrontPost` | Failed → Failed |
| editmode | `SimulatedShooting.Tests.EditMode.VrMechanicalSightCalibrationTests.Bdd33_PrefabAimLineMatchesVisibleApertureAndFrontPostAtOneHundredMetres` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Trench)` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Urban)` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(False)` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(True)` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban` | Failed → Failed |
| editmode | `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.CombatScenePlayModeTests.Bdd15_17_21_HitDedupCorpseRetentionAndRetryCleanup` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.CombatScenePlayModeTests.Bdd15_20_WallsBlockPerceptionAndSceneUnloadRemovesActors` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.MovingTargetRangeIntegrationTests.P2_PhysicalImpactUsesCollisionPoint_EnvironmentDoesNotScore` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.MovingTargetRangeSceneTests.Bdd31_DayHud_RestoresTheOriginalRearRowAndFortyMetreRoute` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.MovingTargetRangeSceneTests.Task003_StartAreaUsesTheExactZeroingRangeForegroundCopy` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_DeathThreeRetriesTrackingAndMovement` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_ProductionVictoryRetryAndReturn` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.VrZeroingMechanicalSightTests.Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.XRSceneHandoffTests.Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests.Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd04_PickupPromptFacesShooterAboveRifleWithoutChangingText` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd31_TargetKeepsItsDimensionsInTheOriginalRearRow` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task005_TrainingRifleUsesLicensedQbz191Visual` | Failed → Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease` | Failed → Failed |
| playmode | `VRShooting.Tests.PlayMode.Screen25_UrbanPlayModeTests.BuildingFailureKeepsCountsAndOldSessionInputsAreRejected` | Failed → Failed |
| playmode | `VRShooting.Tests.PlayMode.Screen26_CombatCompositionPlayModeTests.UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry` | Failed → Failed |
| playmode | `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen18_UrbanMapSelection_RendersRangesAndStartsStreet` | Failed → Failed |
| playmode | `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands` | Failed → Failed |
| playmode | `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce` | Failed → Failed |

## 截图说明

- [Screen_MainMenu.png](evidence/mainmenu-relayout-2026-11-15/Screen_MainMenu.png)：1920×1080主菜单全景，使用现有 `TacticalUIGallery` 的 `Create<MainMenuUI>` 和 `CaptureFrame`，相同预览场景、正交相机及RenderTexture口径；只构建并显示主菜单，不进入其他训练模式。
- [Screen_MainMenu_Closeup.png](evidence/mainmenu-relayout-2026-11-15/Screen_MainMenu_Closeup.png)：同一全景中Frame区域的裁切放大，原图左上原点裁切框(1300,160)–(1810,950)，510×790等比放大至697×1080，居中置于1920×1080深色底；未重新绘制文字或修改UI内容。
- 全景与放大图用于确认操作介绍在最上、标题亮底深字、01与02–04相同次要样式，以及边界和间距。纯UI证据来自Editor实际资产渲染，不作为头显实机体验结论。VR待人工核验标题可读性、交互舒适度和射线点击。

截图入口源归档为 [MainMenuRelayoutEvidence.cs.txt](evidence/mainmenu-relayout-2026-11-15/MainMenuRelayoutEvidence.cs.txt)，通过反射复用现有画廊的私有构建/捕获函数；临时放入Editor目录运行，完成后移除该临时源码和.meta。初次截图入口遗漏 `SimulatedShooting.Scene` 命名空间导致编译失败，补齐引用后重新采集；这发生在全部正式测试完成之后，未修改业务源码或测试结果。重现入口为 `capture-gallery.ps1`，放大脚本为 `make-closeup.py`。Unity原始日志位于忽略目录 `Logs/mainmenu-relayout/`。

## 资源清理与最终工作区

- 已还原24项受跟踪的跑批改写，包括 TrainingChinese.asset、OpenXR设置和旧证据PNG。Prefab、ProjectSettings、其他中文UI资源均无遗留改动。
- 已移除2项本次新增的临时文件。4个被跟踪的历史InitTestScene场景及其4个.meta共8项全部按开工SHA-256逐字节一致保留。
- `docs/codex-reports/pico-apk-2026-11-14.md` 保持未跟踪，SHA-256仍为 `0a737caec9b5543a1367935a2d4781fa30c7e77473ccced17dc6afd4afdd5e0c`，内容未改。
- `git diff --check` 通过，最终Unity编辑器进程数为0。未执行git commit、git push或切分支；HEAD仍为 `ef5a00e`。
- `git status` 仅含上述7个源码/测试/规格文件、新证据目录、本报告，以及开工已有的未跟踪PICO打包报告。

完整恢复路径、临时文件删除记录、历史InitTestScene逐字节核验和最终改动清单见 [cleanup.json](evidence/mainmenu-relayout-2026-11-15/cleanup.json)。
