# 模式一简报靶图、重训清靶、模式二结算摆位与世界指引隐藏

日期：2026-11-12。Unity 2022.3.62f3c1 / URP / OpenXR。HEAD：`6642b40d`。本轮为P1/P2界面与场景表现修正，以及P1–P3四个模式共用指引隐藏。没有commit、push或切换分支，全程只运行一个Unity实例。

四项已实现。新增9条PlayMode定向用例全部通过；全量EditMode为384/364/20，全量PlayMode为313/290/23（总数/通过/失败），对指定6642b40基线逐条比较，新增失败为0。本轮未改变面板尺寸、字体、配色或整体布局体系。

## 改动与根因

1. **简报靶图**：`MainMenuUI.BuildTargetDiagram()`移除五个等距示意圈和独立的“10环”覆盖字，复用既有`TacticalTargetPlot`，节点ID为`Image_ZeroingBriefing_Target`。其`mainTexture`是`Resources/UI/Tactical/ChestTargetReference.jpg`，与`ChestTargetArtwork.Apply()`为真实`ChestTarget_ReferenceFace`材质提供的是同一Texture对象。保留原图比例、6–10数字、白色靶心、绿色胸靶和“50cm x 50cm”；没有导出或独立重绘一张近似图。
2. **重训清靶**：真实场景的五个靶面位于`ZeroingRange/FixedTargets_100m`下，彼此是同级对象。`TrainingRangeSceneBindings.TargetRootAnchor`是射击位中的独立锚点，并不是这些靶面的父对象；旧清理仅遍历已绑定`targetSurface`及其子对象，遗漏同排其它靶面。控制器现在监听现有`PresentationChanged`，射校SessionId离开已绑定ID时同步清理绑定靶面所属场景根层级下全部`TargetImpactSurface`（含未激活子面）；会话ID轮询与OnEnable补做清理仍保留。`ClearImpacts()`先`SetActive(false)`，再沿既有Destroy帧末销毁，确保重训当帧看不到旧红点。正常同会话刷新不清靶，第四发轮次结算保留原来绑定靶及子面的清理范围；独立场景根下的靶面不受影响，移动靶适配器仍排除在外。
3. **模式二结算摆位**：原结算复用固定入场锚点，训练期间转头后不会重新对齐。`MovingTargetRangeRuntimeAdapter`在首次进入该会话`SessionResults/MovingTargetResults`时，将现有LargeUiAnchor放到当前活动相机水平前向1.1m，中心等于当前眼高，朝向水平视线；同会话重复DTO不跟头/不重复摆放。只改模式二组合适配器；其它模式摆位代码没有改动。测试实测Eye=(0.23,1.67,-0.11)，面板中心=(1.28,1.67,0.21)，前向距离1.1m，侧向与高度误差接近0。
4. **全模式隐藏指引**：共用`TrainingRifleGrabInteractable.SetPromptVisible()`始终关闭完整`Prompt_training-rifle_Pickup`根，并在Configure时立即关闭；悬停、放回和重启不再开启它。VR Template `Callout`的Awake/OnEnable统一关闭整套专用根与序列化引用的Tooltip/Curve，覆盖文字、蓝线、底板及Link端点球；凝视入口对停用组件直接返回。没有删除节点或序列化引用，没有根据蓝色颜色去屏蔽所有LineRenderer。HUD/UI提示、交互射线、弹道、命中反馈和血条仍走原有路径。

## 全仓指引排查清单

按脚本创建入口、LineRenderer/BezierCurve、Prompt/Callout节点和GUID依赖链排查全部Assets内的脚本、Prefab与Scene。原始依赖清单见[guidance-audit.json](evidence/mode1-brief-results-labels-2026-11-12/guidance-audit.json)。

| 类型 | 创建/控制脚本 | 资源与引用 | 本次处理 |
|---|---|---|---|
| 枪上世界空间取枪提示 | `TrainingRiflePrefabBuilder.CreatePickupPrompt`、`TrainingRifleGrabInteractable.ConfigurePickupPrompt/SetPromptVisible` | `Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab`中的`Prompt_training-rifle_Pickup`；P1/P2场景内枪和P3运行时步枪共用 | 关闭完整提示根及所有子件；保留引用。该提示生成器自身只创建TextMesh/TMP，未创建蓝线。 |
| 蓝线+标签+端点装饰 | `Assets/VRTemplateAssets/Scripts/Callout.cs`、`BezierCurve.cs`，凝视由`CalloutGazeController`驱动 | `Assets/VRTemplateAssets/Prefabs/Affordance/Affordance Callout.prefab`，含`Lazy Tooltip/Tooltip Canvas/Tooltip Text`、`Curve`、`Link Start/End/Sphere` | 共用Callout入口关闭整套专用根及Tooltip/Curve，重新激活也立即关闭。 |
| 左右控制器提示集合 | 同上 | `Assets/VRTemplateAssets/Prefabs/Setup/Affordance Callouts Left.prefab`、`Affordance Callouts Right.prefab`，经`Complete XR Origin Set Up Variant.prefab`复用 | 由同一Callout处理；不关闭控制器/手部跟踪或UI交互射线。集合中的Callout可不绑定Curve或Tooltip，独立Callout预制体测试则明确断言两者引用都存在。 |
| 生产场景继承实例 | 上述共用Prefab与脚本 | `ZeroingRangeScene`、`MovingTargetRangeScene`、`BunkersOriginalMode3CombatScene`、`BunkersTrenchCombatScene`、`CombatScene` | 全部覆盖，实际四模式持枪用例分别验证隐藏。 |
| 历史/模板场景继承实例 | 同上 | `GameScene`、`ZeroingRangeScene_Backup_20260816` | 同一运行时入口覆盖，未手工修改场景。 |
| 靶位、门、其它交互物专属蓝线标签 | 全仓脚本/序列化资源排查 | 未发现这些对象另有独立蓝线标签创建器或Callout装配；距离牌、路线标识等为普通场景文字，无这套连线结构 | 不按名字误关普通标识；现存Callout实例不论挂在哪个对象都由共用脚本关闭。不能把排查结论写成“只存在枪上”，因为控制器Callout确实存在。 |

## 本轮文件

以下列出相对于开始前工作区的本轮修改，不把既有P3、换肩和TacticalUIGallery改动计作本轮成果：

- `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs`
- `Assets/SimulatedShooting/Runtime/Scene/MovingTargetRangeRuntimeAdapter.cs`
- `Assets/SimulatedShooting/Runtime/Scene/TargetImpactSurface.cs`
- `Assets/SimulatedShooting/Runtime/Scene/TrainingRifleGrabInteractable.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/BriefResultsGuidanceRegressionTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/BriefResultsGuidanceRegressionTests.cs.meta`
- `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`
- `Assets/VRTemplateAssets/Scripts/Callout.cs`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/BDD/screens/07-射校最终评级.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/BDD/screens/29-训练辅助与镜像左肩持枪.feature.md`
- `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task017-ui-bust-target-consistency.md`
- `docs/任务规格/阶段一-P1-100m射校原型闭环/场景/task012-scene-target-impact-support.md`
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task019-combat-feedback-health-and-vr-hands.md`
- `docs/任务规格/阶段二-P2-移动靶闭环/UI/task005-moving-target-ui-flow.md`
- `docs/接口文档/04-100m射校服务.md`
- `docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md`


另新增本报告及`docs/codex-reports/evidence/mode1-brief-results-labels-2026-11-12/`内的XML、比较表、审计记录和截图。BDD04/07/11/29、接口04/13和对应P1 task012/017、P2 task005、P3 task019先补充本次修订；不增加服务方法或DTO，不修改玩法规则。

## 测试路径与结果

测试追溯：BDD04简报与真实靶纸、BDD05/07重新训练/退出重入、BDD11结算正前方、BDD29四模式整套指引隐藏；对应任务与接口见上一节。

| 验证 | 总数 | 通过 | 失败 | 对指定基线新增失败 |
|---|---:|---:|---:|---:|
| 本轮新增PlayMode定向 | 9 | 9 | 0 | 0 |
| EditMode基线6642b40 | 379 | 359 | 20 | — |
| EditMode当前全量 | 384 | 364 | 20 | 0 |
| PlayMode基线6642b40 | 301 | 276 | 25 | — |
| PlayMode当前全量 | 313 | 290 | 23 | 0 |

定向通过后才启动全量，依次EditMode、PlayMode、截图画廊，进程之间等待退出。定向Unity退出0，两次全量退出2分别对应20/23条失败；画廊退出0。没有把全量描述成全部通过。

新增用例均在真实生产场景/应用组合上运行，无VR硬件；最终评级用例通过真实按钮的onClick到Presenter再到Presentation.Retry，不直接调用Retry代替入口。返回重入用例通过现有分析页返回主菜单入口，再从模式选择进入模式一，检查全排与未激活子面，并验证新会话正常红点跨帧保留。模式二用例实际完成服务路线，先改变相机眼位/朝向再断言前向距离1.0–1.2m、侧偏<2cm、高差<2cm和面板朝向。四模式用例均保持HUD提示，模式三/四以ManualXRTrainingInput形成有效双手持枪；独立Callout预制体测试强行重新激活并凝视超过原延迟，检查文字、曲线、装饰及引用。

红测证据中，`playmode-red-confirmed.xml`捕获简报缺少真实靶图、模式二前向距离-0.423152655m，以及四模式取枪提示仍可重新启用；`playmode-clear-red.xml`捕获真实重训按钮返回当帧旧红点仍可见。中间执行也包含测试夹具初始化、停用对象不接受SendMessage、可选Callout引用和P3原有提示文案等夹具修正，不作为产品回归通过数据。最终定向与全量均使用修正后的同一测试文件。

- [定向最终XML](evidence/mode1-brief-results-labels-2026-11-12/playmode-targeted-verified.xml)
- [EditMode全量XML](evidence/mode1-brief-results-labels-2026-11-12/editmode-full.xml)
- [PlayMode全量XML](evidence/mode1-brief-results-labels-2026-11-12/playmode-full.xml)
- [EditMode基线原始副本](evidence/mode1-brief-results-labels-2026-11-12/baseline-editmode-6642b40.xml)
- [PlayMode基线原始副本](evidence/mode1-brief-results-labels-2026-11-12/baseline-playmode-6642b40.xml)
- [逐用例比较JSON](evidence/mode1-brief-results-labels-2026-11-12/baseline-comparison.json)
- [45条基线失败逐条比较CSV，含原始消息](evidence/mode1-brief-results-labels-2026-11-12/failure-comparison.csv)

基线来自上一份报告归档的`mode4-ai-shoulder-2026-11-12/*-post-merge.xml`，与用户给定379/359/20和301/276/25一致。复制SHA-256：EditMode为`831e2c58033f81d224d1297acca026ace3d80b61d219f331cd6d53bafebc758d`，PlayMode为`299e1e699e68414fef95c0b59be10b00ac29865e845ef41c9c1df97c7869c88e`。

### 数量、重命名与失败消息变化归因

- 本轮仅新增9条PlayMode，没有删除或重命名既有测试。EditMode净增加5条、PlayMode另净增加3条，以及4个EditMode/3个PlayMode既有测试重命名，均在开始前的工作区与上一份`mode4-ai-shoulder`报告中已经存在。完整addedTests/missingTests按fullname记录于比较JSON；对该开始前全量报告比较，本轮没有新增失败或丢失用例。
- `CombatFeedbackIntegrationTests.TrenchEnemyAttacksShowShotTracerAudioAndPlayerBlood`和`UrbanEnemyAttacksShowShotTracerAudioAndPlayerBlood`由失败转为通过，来自开始前已有的Tracer_Actor_观测入口/AI等待等修订；本轮保留其代码，不归功于标签修复。
- 仍失败的`BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring`：只有随机Session GUID从`89eb...`变为`ab3559...`；期望高度区间(2.018377,3.428377)和实际1.92627943均相同。
- 仍失败的`BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry`：靴底实际高度由1.92738593变为1.92736709m，相差0.00001884m；期望仍为2.17788005±0.0799999982m。开始前报告实际1.92737138m；属于相同物理落地误差范围，差值远小于约0.25m的原失败量，本轮未改地形、出生或角色落地代码。
- 其余仍失败条目中：EditMode20条消息完全相同；PlayMode其余21条消息完全相同。失败集合无新增。下表逐条列出，不仅比较数量。


### EditMode：20条

| 用例（省略测试命名空间，完整名见CSV） | 当前 | 消息/逐条比较结论 |
|---|---|---|
| `CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms` | 失败 | 与基线消息一致；Expected: 13 But was: 11 |
| `TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/MovingTargetRangeScene.unity","MovingTargetRange")` | 失败 | 与基线消息一致；Expected: 100.0f +/- 0.0199999996f But was: 40.7099991f |
| `TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")` | 失败 | 与基线消息一致；Expected: 100.0f +/- 0.0199999996f But was: 38.9079971f |
| `TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/MovingTargetRangeScene.unity")` | 失败 | 与基线消息一致；Muzzle (-0.004500, -0.041000, 0.455000) must sit at the mesh rim centre plus 1mm clearance (-0.004701, -0.007982, 0.456597). Expected: less than 0.000199999995f But was: 0.033056885f |
| `TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/ZeroingRangeScene.unity")` | 失败 | 与基线消息一致；Muzzle (-0.004500, -0.041000, 0.455000) must sit at the mesh rim centre plus 1mm clearance (-0.004701, -0.007982, 0.456597). Expected: less than 0.000199999995f But was: 0.0330568887f |
| `TrainingRifleMuzzleAlignmentTests.SharedPrefabMuzzleMatchesActualMeshOpening` | 失败 | 与基线消息一致；Muzzle (-0.004500, -0.041000, 0.455000) must sit at the mesh rim centre plus 1mm clearance (-0.004701, -0.007982, 0.456597). Expected: less than 0.000199999995f But was: 0.0330568887f |
| `VrMechanicalSightCalibrationTests.Bdd33_ImportedMeshHasAnOpenRearApertureAndTheMeasuredFrontPost` | 失败 | 与基线消息一致；Three millimetres above the aperture must intersect its metal ring Expected: True But was: False |
| `VrMechanicalSightCalibrationTests.Bdd33_PrefabAimLineMatchesVisibleApertureAndFrontPostAtOneHundredMetres` | 失败 | 与基线消息一致；The VR player aligns real model sights, not a virtual +Z anchor Expected: less than 0.00999999978f But was: 2.03472328f |
| `CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Trench)` | 失败 | 与基线消息一致；one remaining round cannot start a P2 sequence Expected: 0 But was: 1 |
| `CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Urban)` | 失败 | 与基线消息一致；one remaining round cannot start a P2 sequence Expected: 0 But was: 1 |
| `Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins` | 失败 | 与基线消息一致；Expected: True But was: False |
| `Screen25_UrbanTests.RandomCountEndpoints(False)` | 失败 | 与基线消息一致；Expected: 1 But was: 0 |
| `Screen25_UrbanTests.RandomCountEndpoints(True)` | 失败 | 与基线消息一致；Expected: 2 But was: 0 |
| `Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps` | 失败 | 与基线消息一致；Expected: True But was: False |
| `Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult` | 失败 | 与基线消息一致；Expected: True But was: False |
| `Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies` | 失败 | 与基线消息一致；Expected: InvalidState But was: None |
| `Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` | 失败 | 与基线消息一致；Expected: True But was: False |
| `Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds` | 失败 | 与基线消息一致；Expected: in range (1,2) But was: 0 |
| `Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban` | 失败 | 与基线消息一致；Expected: UrbanStreetHud But was: UrbanBuildingHud |
| `Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)` | 失败 | 与基线消息一致；Expected: True But was: False |

### PlayMode：25条基线失败

| 用例（省略测试命名空间，完整名见CSV） | 当前 | 消息/逐条比较结论 |
|---|---|---|
| `BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring` | 失败 | 消息差异已在上一节单列；ab3559b5a9834b859eae43873f5a68bb.enemy-001 Expected: in range (2.018377,3.428377) But was: 1.92627943f |
| `BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry` | 失败 | 消息差异已在上一节单列；trench-spawn-1 boots Expected: 2.17788005f +/- 0.0799999982f But was: 1.92736709f |
| `CombatFeedbackIntegrationTests.TrenchEnemyAttacksShowShotTracerAudioAndPlayerBlood` | 通过；开始前工作区已修复 | 基线为Sequence contains no matching element；当前通过。 |
| `CombatFeedbackIntegrationTests.UrbanEnemyAttacksShowShotTracerAudioAndPlayerBlood` | 通过；开始前工作区已修复 | 基线为Sequence contains no matching element；当前通过。 |
| `CombatScenePlayModeTests.Bdd15_17_21_HitDedupCorpseRetentionAndRetryCleanup` | 失败 | 与基线消息一致；Expected: 15 But was: 13 |
| `CombatScenePlayModeTests.Bdd15_20_WallsBlockPerceptionAndSceneUnloadRemovesActors` | 失败 | 与基线消息一致；Expected: 15 But was: 13 |
| `MovingTargetRangeIntegrationTests.P2_PhysicalImpactUsesCollisionPoint_EnvironmentDoesNotScore` | 失败 | 与基线消息一致；Expected: less than 0.0199999996f But was: 0.0931938887f |
| `MovingTargetRangeSceneTests.Bdd31_DayHud_RestoresTheOriginalRearRowAndFortyMetreRoute` | 失败 | 与基线消息一致；Expected: 100.0f +/- 0.100000001f But was: 40.7099991f |
| `MovingTargetRangeSceneTests.Task003_StartAreaUsesTheExactZeroingRangeForegroundCopy` | 失败 | 与基线消息一致；Missing ZeroingRange start-area copy: ShootingBench Expected: True But was: False |
| `P3ProductionFlowTests.Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene` | 失败 | 与基线消息一致；Expected: UrbanStreetHud But was: UrbanBuildingHud |
| `P3ProductionFlowTests.Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds` | 失败 | 与基线消息一致；System.NullReferenceException : Object reference not set to an instance of an object |
| `P3ProductionFlowTests.Screen21_Urban_DeathThreeRetriesTrackingAndMovement` | 失败 | 与基线消息一致；Real line-of-sight enemy attacks must reach failure Expected: True But was: False |
| `P3ProductionFlowTests.Screen21_Urban_ProductionVictoryRetryAndReturn` | 失败 | 与基线消息一致；Entry prompt should reflect physical range: Expected: True But was: False |
| `VrZeroingMechanicalSightTests.Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound` | 失败 | 与基线消息一致；Real VR sights and sampled gun line must meet within one centimetre Expected: less than 1.0f But was: 77.9776382f |
| `XRSceneHandoffTests.Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands` | 失败 | 与基线消息一致；Expected: 2 But was: 0 |
| `ZeroingOctober07RegressionTests.Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles` | 失败 | 与基线消息一致；Expected: less than 1.01999998f But was: 22.9285927f |
| `ZeroingRangeSceneTests.Bdd04_PickupPromptFacesShooterAboveRifleWithoutChangingText` | 失败 | 与基线消息一致；Expected string length 15 but was 8. Strings differ at index 0. Expected: "GRIP TO PICK UP" But was: "两个手柄抓握持枪" -----------^ |
| `ZeroingRangeSceneTests.Bdd31_TargetKeepsItsDimensionsInTheOriginalRearRow` | 失败 | 与基线消息一致；Expected: 100.0f +/- 0.0199999996f But was: 38.9079971f |
| `ZeroingRangeSceneTests.Task005_TrainingRifleUsesLicensedQbz191Visual` | 失败 | 与基线消息一致；Expected: less than 0.00100000005f But was: 0.0235372111f |
| `ZeroingRangeSceneTests.Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease` | 失败 | 与基线消息一致；Expected: 0.119999997f +/- 0.00100000005f But was: 0.219999999f |
| `Screen25_UrbanPlayModeTests.BuildingFailureKeepsCountsAndOldSessionInputsAreRejected` | 失败 | 与基线消息一致；System.InvalidOperationException : Sequence contains no matching element |
| `Screen26_CombatCompositionPlayModeTests.UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry` | 失败 | 与基线消息一致；System.InvalidOperationException : Nullable object must have a value. |
| `UI.P3UiTask002_005_008_011PlayModeTests.Screen18_UrbanMapSelection_RendersRangesAndStartsStreet` | 失败 | 与基线消息一致；Expected: String containing "1-2" But was: "建筑内部敌人：3-6" |
| `UI.P3UiTask002_005_008_011PlayModeTests.Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands` | 失败 | 与基线消息一致；Expected: String containing "未搜索" But was: "房间1 · 靠近后开门" |
| `UI.P3UiTask002_005_008_011PlayModeTests.Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce` | 失败 | 与基线消息一致；Expected: String containing "街道清除：已清除" But was: "消灭建筑敌人：5 / 5 剩余弹药：76 队友状态： 玩家 · 正常 · 生命 100 二号队友 · 跟随 · 生命 100 三号队友 · 跟随 · 生命 100 用时：02:00" |


## 截图与视觉复核

均使用仓库现有截图工具，没有绘图、拼接或伪造运行画面。

| 内容 | 截图 | 工具与说明 |
|---|---|---|
| 模式一任务简报 | [01-mode1-briefing.png](evidence/mode1-brief-results-labels-2026-11-12/01-mode1-briefing.png) | `TacticalUIGallery.CaptureBatch`生成真实UI组件预览；已检查绿色胸靶、6–10数字、原图比例及尺寸说明。 |
| 模式二结算正前方 | [02-mode2-results-forward-context.png](evidence/mode1-brief-results-labels-2026-11-12/02-mode2-results-forward-context.png) | 真实移动靶场景、完成路线后的UI，`PresentationEvidence.Capture`；为同时显示面板边界和场景，截图时临时使用85°FOV/水平视线，随后恢复相机。没有改变生产面板尺寸或镜头设置。 |
| 模式二原相机视角 | [02-mode2-results-forward.png](evidence/mode1-brief-results-labels-2026-11-12/02-mode2-results-forward.png) | 原相机参数与8°俯仰，保留原始截图供核对。 |
| 模式一第一人称持枪 | [03-mode1-held-rifle-no-labels.png](evidence/mode1-brief-results-labels-2026-11-12/03-mode1-held-rifle-no-labels.png) | `PresentationEvidence.Capture`，实际持枪与HUD“稳定据枪”，无枪上3D文字或蓝线。 |
| 模式三第一人称持枪 | [03-Trench-held-rifle-no-labels.png](evidence/mode1-brief-results-labels-2026-11-12/03-Trench-held-rifle-no-labels.png) | 实际堑壕场景，人物手套/枪、HUD和交互提示保留。 |
| 模式四第一人称持枪 | [03-Urban-held-rifle-no-labels.png](evidence/mode1-brief-results-labels-2026-11-12/03-Urban-held-rifle-no-labels.png) | 实际城镇场景与共用步枪。 |

## 跑批清理与工作区

开始时工作区已经包含未提交的P3/换肩/截图工具等修改及未跟踪资源。初始快照保存在项目外`%TEMP%/mode1-brief-results-labels-2026-11-12-initial/`。本轮6个运行时文件、测试和契约修改保留；其余77个初始文件逐字节核对并保留。

已还原跑批改写的`TrainingChinese.asset`、`Open XR Package Settings.asset`、`Teleport Interactor.prefab`、`XR Origin (XR Rig).prefab`、`Complete XR Origin Set Up Variant.prefab`、原有跟踪证据PNG，以及开始前未跟踪证据目录内被旧用例覆盖的图片。`Assets/InitTestScene*.unity`没有本轮残留，仓库中原有四组已跟踪场景及meta均保留，没有误删。完整还原清单见[worktree-audit.json](evidence/mode1-brief-results-labels-2026-11-12/worktree-audit.json)。`git diff --check`通过，已无Unity进程。

最终`git status`并非空白：保留开始前改动和本轮尚未提交的代码、文档、报告与证据；没有跑批额外改写。用户同时要求“不提交”和保留实现，因此不通过撤销用户已有改动或撤销本轮成果来制造空工作区。

原始日志位于本地忽略目录`Logs/mode1-brief-results-labels-2026-11-12/`，包含定向、全量、画廊与比较脚本。有效运行使用本机Unity可执行文件，`-batchmode -projectPath <repo> -runTests -testPlatform PlayMode/EditMode -testResults <xml> -logFile <log>`；定向另传`-testFilter SimulatedShooting.Tests.PlayMode.BriefResultsGuidanceRegressionTests`。运行测试未加`-quit`或`-nographics`，截图画廊用`-executeMethod VRShooting.Editor.TacticalUIGallery.CaptureBatch -quit`。

## 遗留风险与实机验收

- 现有全量20条EditMode/23条PlayMode失败仍存在，具体条件逐条见上表和原始XML；本轮没有把这些与四项界面修正无关的地图、枪口、AI、角色落地等问题扩大为修复范围。
- 本轮是Unity无VR输入替身验证及真实组件渲染，尚未在PICO头显上验收。需要实机依次确认：简报靶纸一致；最终评级重训与中途返回重入没有旧红点；转头后模式二结算出现在眼前1.1m且可点；四模式拿枪/靠近交互物/凝视后无世界空间蓝线标签，同时HUD、准心、弹道、命中和血条正常。面板尺寸沿用原值，头显内完整性与舒适度仍需实机确认。

