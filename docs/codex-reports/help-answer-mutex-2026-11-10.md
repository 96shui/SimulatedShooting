# 帮助/答案互斥与测试契约交付报告

日期：2026-11-10。基线：`1d763ec`；Unity `2022.3.62f3c1`，仅使用 batchmode。P1 UI 对应 BDD06、接口01、UI task010；射校测试判断依据为 BDD06、接口04 和功能 task006。没有修改射校玩法服务及其算法。

## 本次交付与证据

- `TrainingUIRoot` 使用 `None / Help / Answer` 单一状态，统一推导两按钮、帮助正文/提示、答案面板、坐标及帮助文案；页面切换时重置，重复刷新同一页面不意外关闭当前面板。按钮和面板原有 RectTransform 不变。
- PlayMode 通过真实生成的分析页、按钮事件和服务替身覆盖三个状态、两种收起、切轮、离页和重入，以及两个按钮和答案面板的精确坐标。原有“帮助和答案同时展开”的测试步骤已改成互斥步骤，并保留答案内容、调整刷新和应用幂等检查。
- 帮助展开的针对性测试先失败（答案按钮实际仍可见），修复后通过：[红色 XML](evidence/help-answer-mutex-2026-11-10/mutex-red.xml)、[绿色 XML](evidence/help-answer-mutex-2026-11-10/mutex-green.xml)。
- [初始收起](evidence/help-answer-mutex-2026-11-10/01-collapsed.png)、[帮助展开](evidence/help-answer-mutex-2026-11-10/02-help-expanded.png)、[答案展开](evidence/help-answer-mutex-2026-11-10/03-answer-expanded.png)、[再次收起](evidence/help-answer-mutex-2026-11-10/04-collapsed-restored.png)。两张收起图的 SHA-256 都是 `a880177465a347d4270723af0847eac240dfbb1be04077b553a9321b37a8c2cd`。已目视检查四图。
- [批处理截图脚本](evidence/help-answer-mutex-2026-11-10/Capture-MutexEvidence.ps1) 调用生产 UI 的 PlayMode 用例，模拟点击并由相机渲染四态；[截图运行 XML](evidence/help-answer-mutex-2026-11-10/capture.xml) 为 1/1 通过。[完整 EditMode XML](evidence/help-answer-mutex-2026-11-10/editmode-full.xml)、[日志](evidence/help-answer-mutex-2026-11-10/editmode-full.log)；[完整 PlayMode XML](evidence/help-answer-mutex-2026-11-10/playmode-full.xml)、[日志](evidence/help-answer-mutex-2026-11-10/playmode-full.log)。两次全量运行均未传 `-testFilter`。

全量 EditMode **361 总数、347 通过、14 失败、0 跳过**；全量 PlayMode **285 总数、262 通过、23 失败、0 跳过**。未达到零失败。上一轮给出的 XML 分别仅筛选了 66 条 EditMode 和 37 条 PlayMode，因此没有包含下列其他失败。

## 指定 12 条历史用例的新旧期望

建议角度与实际待应用补偿是两个不同值：对 12cm，建议按 5° 档向上取整为 `ceil(12 / (0.064×5))×5 = 190°`；本用例的待应用补偿经单精度最近档量化为 `−11.84cm = −185°`，再加顺时针 `+5°` 基准，当前读数为 `190°`。因此不能简单给旧建议加基准。

| 原用例 | 旧期望 → 本次期望 | 判断依据和结果 |
|---|---|---|
| `ComputeFrontSightDegrees_UsesCeilingAnd064CmPerDegree` | `188° / 1°` → `190° / 5°` | 建议向上取完整 5° 档，已更名 `ComputeFrontSightDegrees_RoundsSuggestionUpToFiveDegreeSteps`；通过。 |
| `ApplyAdjustment_IsIdempotentForSameRound` | `193°` → `190°` | 实际量化修正 `185°` 加 `+5°` 基准，觇孔 `1+4=5` 格；仍验证重复应用幂等，通过。 |
| `Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(-1)` | `−114.24cm` → `−114.24cm` | 原 1600 次单度改为 320 次 5°，仍为累计 1600°；BDD06 明确不应在 ±50cm 截断。实际 `−49.92cm`，失败。 |
| `Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(1)` | `90.56cm` → `90.56cm` | 同上，保留跨零和多圈含义；实际 `49.92cm`，失败。 |
| `Bdd06_VerticalAdjustmentDoesNotTruncateALargeSuggestedAngle(64.0f,-1)` | `+64cm`、建议 `>780°`、每步 `−0.064cm` → `+24cm`、建议 `375°`、每步 `−0.32cm` | `±64cm` 位于有效靶面外，不再计入平均；改为靶面内的大角度并更名 `Bdd06_LargeOnTargetSuggestedAngleRemainsAdjustable`；通过。不截断多圈要求仍由上两条保留。 |
| `Bdd06_VerticalAdjustmentDoesNotTruncateALargeSuggestedAngle(-64.0f,1)` | `−64cm`、建议 `>780°`、每步 `+0.064cm` → `−24cm`、建议 `375°`、每步 `+0.32cm` | 同上，反向通过。 |
| `Bdd30_HelpRecommendationStaysBasedOnRoundImpactsWhilePendingCorrectionChanges` | `188°` → `190°` | 本轮建议取 5° 档向上取整，手动改待应用水平量不得改建议或历史；通过。 |
| `Bdd30_ManualPreviewCommitsSignedCorrectionWithoutChangingHistory` | 初始 Y `−12cm`、预览 `0`；一步后 `−12.064cm / −0.064cm` → 初始 Y `−11.84cm`、预览 `+0.16cm`；一步后 `−12.16cm / −0.16cm` | 待应用量最近档量化、手动每步 5° 即 `0.32cm`；保留两轴、历史、应用幂等及下一发预览一致性，通过。 |
| `CompleteRound_CalculatesOffsetDirectionsAndAdjustmentAmounts` | `188°` → `190°` | 建议取完整 5° 档，水平 `4` 格和方向不变；通过。 |
| `P1Flow_InputStub_Round1Pass_ShowsExcellentFinalRating` | `第1轮：通过` → `第1轮：3发 · 通过` | 改查实际承载通过状态的 `Text_ZeroingFinalRating_ImpactThumbnails`；原 `Rounds` 文本展示逐发坐标。优秀评级和服务 DTO 断言保留，通过。 |
| `P1Flow_InputStub_ThreeRoundsFail_ShowsFailFinalRating` | `第3轮：未通过` → 三条 `第N轮：3发 · 未通过` | 同上改查状态摘要，额外验证三轮均失败；不及格和服务 DTO 断言保留，通过。 |
| `Screen07_FirstRoundPassShowsExcellentFinalRating` | `第1轮：通过` → `第1轮：3发 · 通过` | 改查状态摘要；保留优秀、第二轮未使用和靶图弹着断言，通过。 |

上述 12 条中 **10 条通过、2 条失败**。这两条不是靠修改期望即可满足的历史格式问题：`ZeroingRules.QuantizeCorrection` 仍对垂直分量 `Mathf.Clamp(..., -50, 50)`，再按 `0.32cm` 量化，饱和于 `±49.92cm`。BDD06 的 2026-10-21 明确写有垂直可多圈且不受 ±50cm 截断；后续 5° 修订改变步长，未明确撤销这一要求。由于本任务禁止为测试修改玩法生产逻辑，保留失败，不能声称 12 条全过。

## 全量 EditMode 失败明细（14）

下表的期望和实际值来自本次 XML。其他 12 条均位于本任务未修改的场景或 P3 代码/测试；其中“旧断言”表示与当前实现不一致，并不凭此断定该实现符合所有权威规格。

| 失败用例 | 期望 → 实际 | 原因或已定位的失败点 |
|---|---|---|
| `CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms` | 敌人生成点 `13` → `11` | 保存的 `CombatScene` 绑定实际只有 11 个 EnemySpawn。 |
| `TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")` | 靶 Z `100±0.02` → `79.678` | 场景中绑定的中央靶位置与 BDD31 旧位置断言不符。 |
| `ZeroingServiceTests.Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(-1)` | `−114.24±0.01cm` → `−49.92cm` | 垂直量化先按 ±50cm 截断。 |
| `ZeroingServiceTests.Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(1)` | `90.56±0.01cm` → `49.92cm` | 同上，正向截断。 |
| `Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins` | 后续范围输入成功 `true` → `false` | 当前只生成建筑敌人，杀完建筑组已触发全歼结算，旧用例随后还试图提交房间区域输入。 |
| `Screen25_UrbanTests.RandomCountEndpoints(False)` | 街道敌人 `1` → `0` | 当前城镇生成只选建筑组。 |
| `Screen25_UrbanTests.RandomCountEndpoints(True)` | 街道敌人 `2` → `0` | 同上。 |
| `Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps` | 后续范围输入成功 `true` → `false` | 无街道组，建筑组全歼已结算；旧测试仍假定有街道敌人等待击杀。 |
| `Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult` | 后续范围输入成功 `true` → `false` | 建筑组全歼后已结算，旧测试随后执行房间检查。 |
| `Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies` | 未到入口 `InvalidState` → `None` | 会话初始即 `Building`，再次请求进入当前阶段幂等成功。 |
| `Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` | 启用入楼提示 `true` → `false` | 当前从建筑内开始，HUD 不再提供旧街道入楼提示。 |
| `Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds` | 街道敌人数 `1–2` → `0` | 当前只生成建筑敌人。 |
| `Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban` | `UrbanStreetHud` → `UrbanBuildingHud` | 当前城镇选择后导航到建筑 HUD。 |
| `Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)` | 感知输入成功 `true` → `false` | 测试选中的建筑敌人属于未开门房间，`UrbanService.Submit` 拒绝其感知输入。 |

## 全量 PlayMode 失败明细（23）

“尚需定位”表示 XML 只能证明失败发生在哪里，不能可靠确定更深一层的原因；不将猜测当成已验证根因。

| 失败用例 | 期望 → 实际 | 原因或已定位的失败点 |
|---|---|---|
| `BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring` | 靴底高度 `2.018377–3.428377` → `1.926279` | 敌人靴子渲染包围盒低于地形采样容差；定位/地形哪个应修仍需核查。 |
| `BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry` | 靴底 `2.17788±0.08` → `1.927394` | 靴子包围盒低于几何表面约 0.25m；具体放置链路尚需定位。 |
| `CombatFeedbackIntegrationTests.TrenchEnemyAttacksShowShotTracerAudioAndPlayerBlood` | 找到一个 `Tracer_Enemy_` → `Sequence contains no matching element` | 测试查旧前缀，`CombatActorView` 当前创建 `Tracer_Actor_`。 |
| `CombatFeedbackIntegrationTests.UrbanEnemyAttacksShowShotTracerAudioAndPlayerBlood` | 找到一个 `Tracer_Enemy_` → 同上异常 | 同一名称不匹配。 |
| `CombatScenePlayModeTests.Bdd15_17_21_HitDedupCorpseRetentionAndRetryCleanup` | 场景演员 `15` → `13` | 保存场景中敌人生成点减少，夹具实际创建 13 个演员。 |
| `CombatScenePlayModeTests.Bdd15_20_WallsBlockPerceptionAndSceneUnloadRemovesActors` | 重载后演员 `15` → `13` | 同一保存场景/夹具数量差异。 |
| `MovingTargetRangeSceneTests.Task003_StartAreaUsesTheExactZeroingRangeForegroundCopy` | P2 前景含 `ShootingBench`：`true` → `false` | 复制前景对象下未找到该名称。 |
| `P3ProductionFlowTests.Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene` | `UrbanStreetHud` → `UrbanBuildingHud` | 城镇现从建筑 HUD 开始，旧断言仍要求街道 HUD。 |
| `P3ProductionFlowTests.Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds` | 街道 HUD 子控件存在 → `NullReferenceException` | 测试在当前建筑 HUD 页面下查找 `Hud_UrbanStreet_Squad` 和街道入口按钮。 |
| `P3ProductionFlowTests.Screen21_Urban_DeathThreeRetriesTrackingAndMovement` | 15 次推进后死亡结果 `true` → `false` | 测试选取敌人并推进后未得到失败结果；房门/感知和视线的具体阻断点尚需定位。 |
| `P3ProductionFlowTests.Screen21_Urban_ProductionVictoryRetryAndReturn` | 入口按钮可交互 `true` → `false` | 当前初始已经在建筑阶段，旧测试仍等待街道入楼提示。 |
| `VrZeroingMechanicalSightTests.Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound` | 第四发后记录数 `4` → `3` | 本轮中心射击、应用补偿后立即满足新通过条件，服务直接结算并不启动第二轮，旧测试仍尝试射击。 |
| `XRSceneHandoffTests.Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands` | 活动手模型 `2` → `0` | 失跟后 `FindObjectsOfType` 未找到活动手对象；是对象被停用还是未创建，当前证据不足。 |
| `ZeroingOctober07RegressionTests.Bdd30_BackwardAndParallelShotsCountAsMissesWithoutTargetHoles` | 靶平面射线有效 `false` → `true` | 某个反向/平行夹具得到有效投影；XML 未记录是哪个方向或其浮点射线，具体原因尚需定位。 |
| `ZeroingOctober07RegressionTests.Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles` | 第二轮 Fire 成功 `true` → `false` | 默认补偿后偏移 ≤5cm 即完成训练，旧测试仍请求进入并射击第二轮。 |
| `ZeroingOctober07RegressionTests.Bdd30_OffFaceUsesSameTargetPlaneCentimetres(0.0f,-40.0f)` | 靶面弹孔 `0` → `1` | 随机固定偏移叠加后落点可能回到 ±25cm 靶面，场景按最终服务坐标画点；旧用例无条件按原始瞄点要求零弹孔。 |
| `ZeroingRangeSceneTests.Bdd31_TargetKeepsItsDimensionsInTheOriginalRearRow` | 靶 Z `100±0.02` → `79.678` | 与 EditMode 中的同一保存靶位置不符。 |
| `ZeroingRangeSceneTests.Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease` | 前握半径 `0.12±0.001` → `0.22` | `TrainingRifleGrabInteractable` 当前配置为 `0.22`，旧测试仍要求 `0.12`。 |
| `Screen25_UrbanPlayModeTests.BuildingFailureKeepsCountsAndOldSessionInputsAreRejected` | 找到街道敌人 → `Sequence contains no matching element` | 当前生成只有建筑敌人，`First(e => e.Group == Street)` 找不到对象。 |
| `Screen26_CombatCompositionPlayModeTests.UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry` | 有失败结算 → `Nullable object must have a value` | 重试后测试直接向未开门房间的建筑敌人提交感知且未检查返回值，没有产生预期结算即读取 `Summary.Value`。 |
| `P3UiTask002_005_008_011PlayModeTests.Screen18_UrbanMapSelection_RendersRangesAndStartsStreet` | 敌人数文案含 `1-2` → `建筑内部敌人：3-6` | 当前页面只显示建筑敌人范围。 |
| `P3UiTask002_005_008_011PlayModeTests.Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands` | 房间文案含 `未搜索` → `房间1 · 靠近后开门` | 当前页面文案与旧断言不同。 |
| `P3UiTask002_005_008_011PlayModeTests.Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce` | 统计含 `街道清除：已清除` → 仅建筑敌人、弹药、队友和用时 | 当前城镇结算展示内容与旧街道清除文案不同。 |

## 保留风险与清理

- 尚未在 PICO 4 上实际验证头显内字号/遮挡、手柄射线命中区域、UI 切换及轮间复位；本次 batchmode 截图和替身不能代替实机。
- 未修改其他阶段的失败用例或玩法；场景靶位、角色接地、VR 手模型和射线等失败应分别核对当前 BDD、场景及真机后再修。
- Unity 自动改写的旧截图、字体资源和 XR 设置已恢复至任务开始时的干净状态。`Exports/` 保持原有未跟踪状态，未添加、忽略或删除；未 commit、push、stash，未删除 `Library`。
