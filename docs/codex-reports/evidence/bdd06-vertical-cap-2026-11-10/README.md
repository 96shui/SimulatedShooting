# BDD06 垂直补偿上限：契约与 EditMode 验证

日期：2026-11-10。范围：P1 射校的四份契约文档和 `ZeroingServiceTests.cs`；本次未修改 `Assets/VRShooting/Runtime`。

## 契约摘要

- 准心柱短按和长按均按 5° 步长匀速调整，不切换加速档；1°≈0.064cm，5°=0.32cm。移出、松开、禁用或关页即停止，长按松开不追加点击。
- 实际叠加到落点的补偿不含会话初始随机偏移，按轴限制在 ±50cm；垂直量化为 0.32cm，饱和值为 ±49.92cm。饱和后同向点击不报错、不累加补偿；反向一步立即向零回退至 ±49.60cm，不做超出量抵扣。
- 保留默认补偿预览、方向含义、水平每格 2cm 及其现有范围、同轮应用幂等和原始弹着不变。BDD06 验收保留双向各累计 1600° 步进可跨零、只应用一次，以及长按超过 2 秒仍匀速重复的要求。
- 唯一保留的实现差异：服务只钳制各轮待应用补偿，跨轮补偿相加时没有总量二次钳制，待后续实现。本次未进行跨轮总量钳制测试。

以上条款同步在 BDD06、接口04、P1 玩法 task006 和 UI task010 中；四处原有的 2026-11-10 实现差异注记均已就地改写。

## 测试改动

- 保留上一轮已通过的 `Bdd06_VerticalCorrectionSaturatesAfterMultipleTurnsAndAppliesOnce(-1/1)` 不变。该用例验证双向 320 步、跨零、±49.92cm 饱和、继续同向步进补偿不变、应用一次及同轮幂等。
- 将 `Bdd06_VerticalAngleBeyondSaturationIsRetainedWhenDirectionReverses(-1/1)` 改名为 `Bdd06_VerticalCorrectionExitsSaturationOnFirstReverseStep(-1/1)`。每个方向先执行 320 步并断言待应用垂直补偿为 `direction × 49.92cm`；反向一步后断言为 `direction × 49.60cm`，两处容差均为 0.001cm，并保留操作成功断言。删除超限角度推算及相关断言。
- 其余测试用例未在本次修改。`UnityEngine` 仍用于 `Vector2`，因此保留该引用。

## 运行结果

| 运行 | 总数 | 通过 | 失败 | 跳过 | 证据 |
|---|---:|---:|---:|---:|---|
| 本次全量 EditMode | 363 | 351 | 12 | 0 | [XML](editmode-full.xml)、[日志](editmode-full.log) |
| 上一轮全量 EditMode | 363 | 349 | 14 | 0 | [上一轮 XML](editmode-previous-full.xml) |
| 原始基线全量 EditMode | 361 | 347 | 14 | 0 | [基线 XML](../help-answer-mutex-2026-11-10/editmode-full.xml) |

本次 `Bdd06_VerticalCorrectionSaturatesAfterMultipleTurnsAndAppliesOnce` 和 `Bdd06_VerticalCorrectionExitsSaturationOnFirstReverseStep` 两个方法的 `-1`、`1` 参数实例共 4 条均通过。上一轮多出的 2 条失败是旧的反向角度保持用例；原始基线多出的 2 条失败是更早的 `Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(-1/1)`。本次其余失败与两次比较的共同失败集合完全相同。

本次只运行 EditMode 全量测试；UI 长按的 PlayMode 和 VR 手柄实机验收未在本次执行，不能由 EditMode 结果替代。

## 剩余 12 条既有失败

逐条比对 XML：下表每个测试的完整名称以及预期/实际失败消息在原始基线、上一轮和本次均相同。它们均在本次文档及 BDD06 测试修改之前已失败，与本次改动无关；这里记录断言差异，不额外推断各失败的根因。

| 测试名称 | 三次运行一致的失败信息 |
|---|---|
| `CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms` | 期望 `13`，实际 `11`。 |
| `TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")` | 期望 `100.0 ± 0.02`，实际 `79.678`。 |
| `Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins` | 期望 `True`，实际 `False`。 |
| `Screen25_UrbanTests.RandomCountEndpoints(False)` | 期望 `1`，实际 `0`。 |
| `Screen25_UrbanTests.RandomCountEndpoints(True)` | 期望 `2`，实际 `0`。 |
| `Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps` | 期望 `True`，实际 `False`。 |
| `Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult` | 期望 `True`，实际 `False`。 |
| `Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies` | 期望错误码 `InvalidState`，实际 `None`。 |
| `Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` | 期望 `True`，实际 `False`。 |
| `Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds` | 期望位于 `[1, 2]`，实际 `0`。 |
| `Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban` | 期望 `UrbanStreetHud`，实际 `UrbanBuildingHud`。 |
| `Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)` | 期望 `True`，实际 `False`。 |

## 执行方式

使用 `C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe`，传入 `-batchmode -projectPath "E:/新建文件夹 (3)/SimulateShooting" -runTests -testPlatform EditMode` 和本目录中的 `-testResults`、`-logFile` 路径，未传 `-quit`。旧的提前退出日志已删除；保留的 `editmode-targeted.xml` 和日志来自上一轮定向运行，不作为本次全量结果。

Unity 运行期间自动改写的 `TrainingChinese.asset` 和 `Open XR Package Settings.asset` 在运行前均无工作区改动，运行后均已恢复。`Exports/` 在本任务开始前就未跟踪，本次没有改动。
