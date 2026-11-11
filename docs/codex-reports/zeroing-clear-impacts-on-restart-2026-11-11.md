# 模式一重新进入时清除靶面残留弹着点

日期：2026-11-11。阶段：P1。归属：场景 task012、功能B task013，联调与测试追溯 task014、BDD05/07、接口04。Unity：2022.3.62f3c1。

本次生产代码仅在 `ResetSessionState()` 中新增 6 行。新增三条 PlayMode 回归全部通过，原有第四发清靶用例通过；定向 PlayMode 和一次 EditMode 全量的新增失败均为 **0**。没有提交或推送 Git。

## 根因复核

1. 修改前，`TargetImpactSurface.ClearImpacts()` 唯一的生产调用点位于 `FirstPersonTrainingWeaponController.HandleZeroingRoundCompleted()`。它只处理 `evt.SessionId == boundSessionId` 的轮次完成事件，因此未满四发便退出时，没有物理弹孔清理入口。
2. `EnsureInitialized()` 在 `boundSessionId != SessionId` 时调用 `ResetSessionState()`。旧实现只重置状态、输入订阅、握持与后坐力等字段，没有销毁靶面标记，因此服务会话释放后，保留的靶面对象仍持有旧红点。
3. `SessionId` 在 `TrainingSessions.HasActiveSession == false` 时返回空字符串。活动控制器下一次 `Update()` 会走旧 ID → 空 ID 的重置；重置将 `boundSessionId` 也设为空，后续无会话帧不会重复重置。新会话成功绑定后，相同 ID 的初始化直接返回，因此正常会话内不会每帧清靶。
4. `OnDisable()` 不清靶，并保留已绑定 ID；若会话在禁用期间结束或更换，`OnEnable()` 调用 `EnsureInitialized()` 时仍能识别 ID 变化并重置。相同会话的禁用/重新启用不会清靶。因此本次**不增加 OnDisable 清理点**。
5. 返回主菜单/模式选择走现有 `EndCurrentSession()`、`Presentation.Exit()` 路径。最终评级页“重新训练”由 `TrainingPresentationPresenter.RequestRetry()` 调用 `Presentation.Retry()`，释放当前 Session；该路径同样由上述会话重置覆盖。

## 文件与行号

行号为本次最终工作区版本。

| 文件 | 行号 | 变更 |
|---|---|---|
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs` | 389–394，方法起于 387 | `targetSurface != null && movingTargetHitAdapter == null` 时，遍历 `targetSurface.GetComponentsInChildren<TargetImpactSurface>(true)` 并逐一 `ClearImpacts()`。 |
| `Assets/SimulatedShooting/Tests/PlayMode/ZeroingOctober07RegressionTests.cs` | 13、216–295 | 引入无 VR 输入替身，增加活动状态退出重进、禁用期间换会话两条回归。 |
| `Assets/SimulatedShooting/Tests/PlayMode/ZeroingRangeSceneTests.cs` | 635–666 | 整合本轮并发出现的重训回归，修正控制器查找并命名为 `Bdd05_RetrainingClearsPreviousSessionImpacts`。 |
| `docs/BDD/screens/05-100m射击HUD.feature.md` | 246–252 | 明确退出后重进、未激活子靶面、同会话保留弹孔与模式隔离的验收场景。 |
| `docs/BDD/screens/07-射校最终评级.feature.md` | 64 | 保留并发补充的重新训练时清空靶面标记要求。 |
| `docs/接口文档/04-100m射校服务.md` | 60 | 补充场景端会话切换清靶、禁用重进与帧末实际对象断言约定；不改变 DTO、方法或事件。 |
| `docs/任务规格/阶段一-P1-100m射校原型闭环/场景/task012-scene-target-impact-support.md` | 55 | 补充 BDD、接口与回归用例追溯。 |

`HandleZeroingRoundCompleted()`（现第 975–979 行）保持原文，每轮第四发的清靶条件与遍历方式均未变化。没有修改四发规则、集中规则、垂直方向、通过判定，也没有修改模式二、三、四的玩法代码。新增清理严格使用指定的已绑定对象判断，不做全局查找或扩大清理范围。

## 新增 PlayMode 测试

测试加载真实 `ZeroingRangeScene`，使用现有枪线/快照/服务开火与 `TargetImpactSurface` 标记创建逻辑，无需 VR 设备。

| 测试名 | 覆盖内容 | 结果 |
|---|---|---|
| `ZeroingOctober07RegressionTests.Bdd05_ExitAndReenterClearsPhysicalImpactMarkers` | 打两发；确认同会话多帧保留两个实际标记；另造一个未激活子靶面的标记；退出释放 Session；重启新会话，验证主靶与子靶的 `ImpactMarkerRoot.childCount == 0`。 | 通过 |
| `ZeroingOctober07RegressionTests.Bdd07_SessionChangedWhileDisabledClearsPhysicalMarkersOnEnable` | 先验证同会话禁用/启用不删标记；再在禁用期间释放旧会话、启动新会话；保持同一靶对象并重新启用，断言子对象数为零且旧标记对象已销毁。 | 通过 |
| `ZeroingRangeSceneTests.Bdd05_RetrainingClearsPreviousSessionImpacts` | 命中后取消会话，通过现有 bootstrap 启动新会话，断言实际标记根节点为空。 | 通过 |

原有 `ZeroingOctober07RegressionTests.Bdd05_FourthShotImmediatelyClearsOnlyCurrentTrainingTarget` 同次通过，确认第四发清理与其他独立靶面保留行为继续成立。新增用例的关键断言直接检查层级中的对象，不只检查内部 `Impacts` 集合；在 `ClearImpacts()` 使用的 Unity `Destroy()` 完成帧末销毁后断言。

## 红测与定向 PlayMode

先临时关闭会话重置的清理入口，以复现旧行为，运行上述前两条新测试：**2 条、0 通过、2 失败**。

- `Bdd05_ExitAndReenterClearsPhysicalImpactMarkers`：期望实际标记数 `0`，实际为 `2`。
- `Bdd07_SessionChangedWhileDisabledClearsPhysicalMarkersOnEnable`：期望实际标记数 `0`，实际为 `1`。

实现最终的 6 行修复后，仅运行两个相关测试类：

```text
SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests
SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests
```

结果：**70 条，66 通过，4 失败**。与给定 `p1-four-shots-2026-11-11/playmode-final.xml` 中这两个类的失败集合按完整 `fullname` 比较，新增失败 **0**、消失失败 **0**。Unity 退出码为 `2`，对应以下已有失败，不能表述为整套定向测试全部通过：

```text
SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests.Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles
SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests.Bdd30_OffFaceUsesSameTargetPlaneCentimetres(0.0f,-40.0f)
SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd31_TargetKeepsItsDimensionsInTheOriginalRearRow
SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease
```

本轮启动的定向验证均带 `-testFilter`。执行期间另一轮 Unity 跑批曾占用同一项目，首次启动因项目锁未进入测试；随后使用带图形设备的 batchmode 完成上述有效运行，并发运行的结果不计入这里的验收数据。全量 PlayMode 在并发跑批结束后单独补跑，结果见下一节。

## 全量 PlayMode 复核

并发跑批结束后补跑一次不带 `-testFilter` 的全量 PlayMode，作为最终验收数据：

| 运行 | 总数 | 通过 | 失败 | 新增失败 |
|---|---:|---:|---:|---:|
| 基线 `p1-four-shots-2026-11-11/playmode-final.xml` | 292 | 270 | 22 | — |
| 本次 `evidence/.../playmode-full.xml` | 295 | 273 | 22 | **0** |

总数多出 3 条即本次新增的三条回归，且三条均为 `Passed`；失败集合与基线按完整 `fullname` 比较完全一致，消失失败 0。

## EditMode 全量与基线比较

只运行一次 EditMode 全量，不传 `-testFilter`。结果与指定基线一致：

| 运行 | 总数 | 通过 | 失败 | 跳过 | 新增失败 |
|---|---:|---:|---:|---:|---:|
| 基线 `p1-four-shots-2026-11-11/editmode-final.xml` | 378 | 366 | 12 | 0 | — |
| 本次 `editmode-full.xml` | 378 | 366 | 12 | 0 | **0** |

两者失败集合完全相同，消失失败也为 **0**；Unity 退出码为 `2`。完整失败名如下（不是只比较失败数量）：

```text
SimulatedShooting.Tests.EditMode.CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms
SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")
VRShooting.Tests.EditMode.Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins
VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(False)
VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(True)
VRShooting.Tests.EditMode.Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps
VRShooting.Tests.EditMode.Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult
VRShooting.Tests.EditMode.Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies
VRShooting.Tests.EditMode.Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime
VRShooting.Tests.EditMode.Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds
VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban
VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)
```

## 证据与复现

- [红测 XML](evidence/zeroing-clear-impacts-on-restart-2026-11-11/playmode-red.xml)
- [定向 PlayMode XML](evidence/zeroing-clear-impacts-on-restart-2026-11-11/playmode-targeted.xml)
- [EditMode 全量 XML](evidence/zeroing-clear-impacts-on-restart-2026-11-11/editmode-full.xml)
- [完整失败集合比较及新增用例结果](evidence/zeroing-clear-impacts-on-restart-2026-11-11/baseline-comparison.json)

原始日志位于本地忽略目录 `Logs/zeroing-clear-impacts-on-restart-2026-11-11/` 中的 `playmode-red.log`、`playmode-targeted.log`、`editmode-full.log`。

有效验证命令（PowerShell，Unity 路径和项目路径均与本机一致；不传 `-quit`、`-nographics`）：

```powershell
& 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -projectPath 'E:/新建文件夹 (3)/SimulateShooting' -runTests -testPlatform PlayMode -testFilter 'SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests;SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests' -testResults 'E:/新建文件夹 (3)/SimulateShooting/docs/codex-reports/evidence/zeroing-clear-impacts-on-restart-2026-11-11/playmode-targeted.xml' -logFile 'E:/新建文件夹 (3)/SimulateShooting/Logs/zeroing-clear-impacts-on-restart-2026-11-11/playmode-targeted.log'
& 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -projectPath 'E:/新建文件夹 (3)/SimulateShooting' -runTests -testPlatform EditMode -testResults 'E:/新建文件夹 (3)/SimulateShooting/docs/codex-reports/evidence/zeroing-clear-impacts-on-restart-2026-11-11/editmode-full.xml' -logFile 'E:/新建文件夹 (3)/SimulateShooting/Logs/zeroing-clear-impacts-on-restart-2026-11-11/editmode-full.log'
```

## 生成物清理与工作区

验证结束后已还原跑批自动改写的 `TrainingChinese.asset`、`Open XR Package Settings.asset`、`Teleport Interactor.prefab`、`XR Origin (XR Rig).prefab`、`Complete XR Origin Set Up Variant.prefab`，并删除本轮/并发跑批残留的未跟踪临时 `InitTestScene639300321205456514.unity` 及其 `.meta`。`ProjectSettings/` 无变更，`git diff --check` 通过。

开始工作前已存在的 `Assets/Editor/BuildPicoApk.cs`、`docs/构建/打包PICO-APK.md`、`docs/codex-reports/apk-v1-signing-2026-11-11.md` 已保留，且与本次整合前备份逐字节 SHA-256 核对相同。因此最终 `git status` 除这三项既有改动外，仅保留本次代码、测试、契约说明、报告及测试证据，不含 Unity 自动生成改动。

## 尚未验证

本轮验证为无 VR 设备的 Unity 自动化测试；头显内红点消失的实际观感仍需用户真机确认。建议在 PICO 上分别验证“打一至三发后退出并重进”以及“最终评级页点击重新训练”，观察同一靶面没有上一会话红点，并确认当前轮次内已有弹着不会提前消失。

模式二、三、四未单独运行完整 PlayMode 或真机验收；本次仅通过指定模式判断和生产差异审查确认新增清理入口没有扩大到这些模式。最终评级页按钮链路已复核，新增对象级回归覆盖的是会话释放/重新创建与控制器生命周期，未另加最终评级页按钮点击测试。
