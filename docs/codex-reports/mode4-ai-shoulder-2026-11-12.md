# 模式四 AI 对齐、十点生命、队友手雷与换肩修复

日期：2026-11-12。Unity 2022.3.62f3c1。基线 HEAD：`6642b40d`。阶段 P3，玩法 task004/010/015/019/020，兼顾 P1/P2 既有换肩入口。没有 commit、push 或切换分支。

本次只处理用户指定四项；同步修订 BDD23/27/29、接口15/19/21和对应任务规格。旧文档要求整枪镜像及Urban五点生命的条款，由文件开头的2026-11-12修订覆盖。页面布局、UI美术风格、射校算法、地图几何和敌人模型不改。

## 实现与根因

1. **共用AI**：没有新增Urban AI。`CombatSceneRuntime.CollectPerception`仍通过同一Perception事实提交角色位置、实际水平朝向与视线；`UrbanService.GetVisualSnapshot/VisualChanged`输出`CombatCoreService`的动态Forward和状态，`GetEnemyAssignments`只负责稳定的房间/生成点归属。保留Urban关闭房门遮挡。共用核心现在将“发现/追踪中”与“允许攻击”区分：30m/半视角60°内发现进入Attacking，运行时调用既有`CombatActorView.TurnTowards`按180°/s水平转体；水平误差≤12°才开始一秒射击计时。失去视线、离开射程、暂停、死亡或失踪沿现有状态机停止和重新计时；开火动画、枪声、VFX、受伤、死亡及队友目标选择共用原路径。
2. **十点生命**：`CombatConfigDto.ForMode`对Trench/Urban均返回10。Urban构造器原先直接注入Default（5），也改为ForMode(Urban)，避免只改ForMode却被Urban显式传入旧默认值覆盖。EnemyDamage仍1，Validate通过；显式配置仍由调用者决定。HUD数值使用Health/MaxHealth，共用血条原来固定五格，现改为十格；结算沿Player.IsAlive/核心失败状态。服务回归逐次验证第1至10枪、HUD最大血量、死亡失败和重试恢复；预览Urban改为8/10。
3. **队友手雷**：UrbanService持有与Trench完全相同的`SquadGrenadeTacticService`，Mission.Grenades由原null接入它。Start/Advance/Dispose以及核心状态回调负责生命周期，右A按下沿/桌面H沿`P3XRTrainingInput`与共用运行时请求第一名队友投雷；18m限制、碰撞飞行、无遮挡5m范围击杀、1.25s引信、3s冷却均沿用现有配置。真实场景回归发现原运行时在核心未排空本帧感知事实前推进引信，会使爆炸返回Busy；现运行时只提交请求，Trench/Urban任务服务均在核心Advance之后推进手雷，避免重复调度和过早结算。该修复也通过模式三生产投雷回归。
4. **资源引用**：运行仓库已有`GrenadePrefabBuilder.Build`生成M67预制体；将过期源路径改为已有Resources模型。预制体只包含可渲染模型，投射物驱动由运行时唯一拥有，避免模型内和外层出现两份驱动。`CombatScene.unity`显式绑定M67与已有`GrenadeExplosion_Heavy.wav`；新增可序列化GrenadeExplosionClip供共用VFX消费。激活时校验可用模型/音效，缺失返回ResourceUnavailable；Trench继续可使用相同Resources回退，不复制VFX或音效。
5. **换肩**：删除`TrainingRifleGrabInteractable.SetShoulder`整枪VisualRoot负X缩放。右肩保持右后/左前，左肩为左后/右前；取消原选择后按新后手优先重建XRI选择。枪身、瞄具、枪口和两处握把保留原坐标系。`VRControllerHandVisual`只反射手模型及手部偏移：在握把坐标系内以M×offset×M变换手姿，正缩放骨架不再依赖整枪负缩放。当前后手扳机及前手锁定继续随角色切换；换弹的侧倾、横移和弹匣倾斜按肩侧调整，当前支撑手执行动作。P1/P2将既有服务肩侧同步至共用抓取器/可选输入适配器，保留原有换肩入口与默认B键禁用约定。
6. **HUD提示**：显示右后/左前或左后/右前；双模式提示右A/H队友手雷，主菜单操作说明同步。只改语义提示与用户要求的血格数，不移动面板或替换视觉素材。

## 测试与证据方法

测试使用真实生产场景、共享步枪和手部资源、CombatMission/UrbanService及可替换输入；无需VR设备。新增Urban生产用例实际打开目标房门，检查Forward快照、平滑转角、对准后开火、遮挡停止；使用Input System XRController替身按右A，检查唯一投射物、真实爆炸音源、范围击杀与冷却。源代码、服务状态和场景对象均来自本次工作区。

- 换肩红测`shoulder-red.xml`：真实XRI选择切左肩后期望枪体X缩放>0，旧代码实际-1，1条失败。
- 战斗红测`combat-red.xml`：4条中1通过/3失败；Urban十枪预算和双模式未对准禁射失败。
- 第一轮EditMode全量触发两条旧“整枪镜像”断言，证据保留在`editmode-full-before-contract-test-update.xml`。两条分别为`VRControllerHandVisualEditModeTests.Bdd29_LeftShoulderAimFollowsTheMirroredRenderedBarrelWithYawAndRoll`和`Bdd29_LeftShoulderMirrorsEveryArmBoneAndRenderedVertexAndSwitchesBackExactly`。这是本次契约变化引起的断言不适用，不能当作基线失败。修订后逐骨骼/基向量/真实蒙皮顶点仍以0.0001m检查手部在各自握把坐标系中的反射，另以0.00001精度检查枪线及后坐偏航保持不变；往返切肩必须恢复全部顶点。没有删除几何检查或放宽容差。
- 首次PlayMode全量为304/280/24，多出`Screen23_CombatInputPlayModeTests.ManualInput_PickupAutomaticFireReloadShoulderPostureAndCombat`一条：旧5血扣1期望4，实际新规则为9。该条确属本次漏同步断言，证据保留`playmode-full-before-health-test-update.xml`；改为明确期望9后复跑完整输入类及全量，不列为历史债。
- 原CombatFeedbackIntegrationTests两条基线失败来自检索已退役`Tracer_Enemy_`名称，生产实际创建`Tracer_Actor_`。本次更新血量与对准等待时一并修正该观测入口，保留真实弹体、弹道线宽、音源、血雾、十次伤害和致命反馈断言。
- P1/P2回归实际进入对应训练场并使用既有肩侧服务命令；P3使用真实B键输入替身、左右扳机、XRI后手选择顺序、换弹中切肩和新支撑手锁定/恢复。

全量基线XML原始归档位于`Logs/mode1-ui-2026-11-12/pre-capture/prior-clear-impact-evidence/`。本次逐字节复制其中`editmode-post-merge.xml`和`playmode-post-merge.xml`到新证据目录，并以各test-case的完整fullname和失败消息比较，不使用更早的295/378条基线。

| 运行 | 总数 | 通过 | 失败 | 新增失败 |
|---|---:|---:|---:|---:|
| [EditMode定向](evidence/mode4-ai-shoulder-2026-11-12/editmode-targeted-complete.xml) | 37 | 37 | 0 | 0 |
| [PlayMode定向](evidence/mode4-ai-shoulder-2026-11-12/playmode-targeted-final-all.xml) | 46 | 46 | 0 | 0 |
| [EditMode基线](evidence/mode4-ai-shoulder-2026-11-12/editmode-post-merge.xml) | 379 | 359 | 20 | — |
| [EditMode本次全量](evidence/mode4-ai-shoulder-2026-11-12/editmode-full.xml) | 384 | 364 | 20 | 0 |
| [PlayMode基线](evidence/mode4-ai-shoulder-2026-11-12/playmode-post-merge.xml) | 301 | 276 | 25 | — |
| [PlayMode本次全量](evidence/mode4-ai-shoulder-2026-11-12/playmode-full.xml) | 304 | 281 | 23 | 0 |

EditMode新增5条、PlayMode新增3条，均通过。EditMode原20条失败全部仍为同名、同失败消息；PlayMode原25条中23条仍失败，两条敌人反馈用例已通过。最终新增失败为0；全量没有宣称全绿。完整XML、逐条消息与堆栈及新增/移除测试名保留在证据目录。

## 截图

全部位于[evidence/mode4-ai-shoulder-2026-11-12](evidence/mode4-ai-shoulder-2026-11-12/)，由仓库已有`PresentationEvidence.Capture`相机渲染工具生成，没有新建截图系统。只为该工具新增可选实时Overlay Canvas捕获，默认调用保持原行为；临时相机和Canvas设置在finally中恢复。人工复核了敌人开火、十格HUD、爆炸烟尘/碎屑及两种正立握枪姿态。

| 文件 | 证明与限制 |
|---|---|
| `urban-enemy-aim-and-fire.png` | 已打开房门的真实Urban敌人转向后开火，确认枪口VFX和受击血雾；在攻击事件帧采集。HUD按10Hz刷新，事件帧可能仍显示上一帧生命。 |
| `urban-health-10.png` | 生产服务和实际建筑HUD初始10/10、十格生命。 |
| `urban-health-after-hit-9.png` | 等待生产Presenter刷新后，实际HUD为9/10、九格；测试检查文本和Image.enabled数量。 |
| `urban-teammate-grenade-explosion.png` | 实际右A投雷、世界碰撞与伤害确认后采集的爆炸烟尘/碎屑；从场景内无遮挡相机位置观察，未叠加伪造爆炸。 |
| `Urban-B-left-shoulder-left-rear-right-front.png` | 按B后左手后握把、右手在护木，枪身和瞄具正立；对应RearHandedness.Left/FrontHandedness.Right。 |
| `Urban-B-right-shoulder-right-rear-left-front.png` | 再按B恢复右后/左前，枪身正立。 |
| `Trench-B-left-shoulder-left-rear-right-front.png` / `Trench-B-right-shoulder-right-rear-left-front.png` | 模式三相同两种姿态。 |

截图使用当前修改后Unity TestRunner编译程序集与生产资源，**不是重新打包的PICO APK，也不是真实头显录屏**。测试只替换硬件输入和时钟；投雷证据选择无遮挡观察位以避免被墙或队友遮住，位置信息记录在测试日志`Mode4Evidence`。默认无火焰爆炸素材呈现烟尘而不是火球。构建源码与截图SHA-256记录在`artifact-manifest.json`，采集用例及时间可在最终PlayMode XML核对。

## 文件清单

- `Assets/Scenes/CombatScene.unity`
- `Assets/SimulatedShooting/Editor/GrenadePrefabBuilder.cs`
- `Assets/SimulatedShooting/Runtime/Scene/CombatExplosionVfx.cs`
- `Assets/SimulatedShooting/Runtime/Scene/CombatRifleReloadPresentation.cs`
- `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs`
- `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs`
- `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs`
- `Assets/SimulatedShooting/Runtime/Scene/TrainingRifleGrabInteractable.cs`
- `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs`
- `Assets/SimulatedShooting/Tests/EditMode/CombatSceneTests.cs`
- `Assets/SimulatedShooting/Tests/EditMode/VRControllerHandVisualEditModeTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/AmericanSoldierPlayModeTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/CombatFeedbackIntegrationTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/CombatMagazineReloadPlayModeTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/MovingTargetRangeIntegrationTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/P3XRTrainingInputTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/ZeroingRangeSceneTests.cs`
- `Assets/VRShooting/Editor/TacticalUIGallery.cs`
- `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs`
- `Assets/VRShooting/Runtime/Application/Combat/CombatMission.cs`
- `Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs`
- `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs`
- `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`
- `Assets/VRShooting/Runtime/Unity/UI/P3TrenchHudAndResultsUI.cs`
- `Assets/VRShooting/Tests/EditMode/Infrastructure/Screen23_CombatCoreTests.cs`
- `Assets/VRShooting/Tests/EditMode/Infrastructure/Screen25_UrbanTests.cs`
- `Assets/VRShooting/Tests/PlayMode/Infrastructure/Screen23_CombatInputPlayModeTests.cs`
- `Assets/VRShooting/Tests/PlayMode/TacticalUIArtTests.cs`
- `docs/BDD/screens/23-P3共用战斗核心.feature.md`
- `docs/BDD/screens/27-模式三队友自动手雷.feature.md`
- `docs/BDD/screens/29-训练辅助与镜像左肩持枪.feature.md`
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task004-combat-input-weapon-damage-and-enemy.md`
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task010-urban-phases-room-search-and-results.md`
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task015-mode3-squad-grenade.md`
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task019-combat-feedback-health-and-vr-hands.md`
- `docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task020-combat-magazine-reload.md`
- `docs/接口文档/15-P3共用战斗核心.md`
- `docs/接口文档/19-模式三队友自动手雷.md`
- `docs/接口文档/21-训练辅助与镜像左肩持枪.md`
- `Assets/Resources/Combat/Grenade_M67.prefab`
- `Assets/Resources/Combat/Grenade_M67.prefab.meta`
- `Assets/SimulatedShooting/Tests/PlayMode/Mode4AlignmentPlayModeTests.cs`
- `Assets/SimulatedShooting/Tests/PlayMode/Mode4AlignmentPlayModeTests.cs.meta`

`TacticalUIGallery.cs`开工前已有用户的模式一截图工具改动；本轮相对开工备份只改变Urban预览的Health=4→8一行。新增M67.prefab/.meta为本次必需资源，不属于应还原的跑批污染。测试工具仅扩展已有Capture，不新增第二套截图实现。

## 全量失败逐条对照

下面列出指定基线的全部45条；每条均附本次结果和归因。原始失败消息及堆栈在[baseline-comparison.json](evidence/mode4-ai-shoulder-2026-11-12/baseline-comparison.json)与前后XML中保留。

### EditMode 基线失败逐条复核

| 完整测试名 | 本次结果 | 归因 |
|---|---|---|
| `SimulatedShooting.Tests.EditMode.CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms` | Failed | 旧测试仍要求街道候选/实体数量13或15；现行城镇只生成建筑敌人，基线同名同原因。 |
| `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/MovingTargetRangeScene.unity","MovingTargetRange")` | Failed | 现有远端场景靶排距离38.908m或40.710m，与测试100m要求不一致；未改场景靶位。 |
| `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")` | Failed | 现有远端场景靶排距离38.908m或40.710m，与测试100m要求不一致；未改场景靶位。 |
| `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/MovingTargetRangeScene.unity")` | Failed | 既有新步枪网格枪口边缘与枪口锚点位置不同；未改步枪网格或枪口锚点。 |
| `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/ZeroingRangeScene.unity")` | Failed | 既有新步枪网格枪口边缘与枪口锚点位置不同；未改步枪网格或枪口锚点。 |
| `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SharedPrefabMuzzleMatchesActualMeshOpening` | Failed | 既有新步枪网格枪口边缘与枪口锚点位置不同；未改步枪网格或枪口锚点。 |
| `SimulatedShooting.Tests.EditMode.VrMechanicalSightCalibrationTests.Bdd33_ImportedMeshHasAnOpenRearApertureAndTheMeasuredFrontPost` | Failed | 既有步枪觇孔/准星与AimLine标定不匹配；本轮只去掉换肩反射，不修改网格/瞄具标定。 |
| `SimulatedShooting.Tests.EditMode.VrMechanicalSightCalibrationTests.Bdd33_PrefabAimLineMatchesVisibleApertureAndFrontPostAtOneHundredMetres` | Failed | 既有步枪觇孔/准星与AimLine标定不匹配；本轮只去掉换肩反射，不修改网格/瞄具标定。 |
| `VRShooting.Tests.EditMode.CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Trench)` | Failed | 既有单余弹仍起射与旧两发门槛断言冲突；未改自动连射或弹药规则。 |
| `VRShooting.Tests.EditMode.CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Urban)` | Failed | 既有单余弹仍起射与旧两发门槛断言冲突；未改自动连射或弹药规则。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins` | Failed | 旧用例沿街道→建筑→搜索/检查闭环，与现行直接建筑清剿冲突；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(False)` | Failed | 旧服务用例要求街道1–2人，现行建筑清剿街道固定0；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(True)` | Failed | 旧服务用例要求街道1–2人，现行建筑清剿街道固定0；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps` | Failed | 旧用例沿街道→建筑→搜索/检查闭环，与现行直接建筑清剿冲突；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult` | Failed | 旧用例沿街道→建筑→搜索/检查闭环，与现行直接建筑清剿冲突；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies` | Failed | 旧用例要求街道残敌阻止进入，现行直接建筑清剿允许；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` | Failed | 旧用例期待EnterBuilding提示，现行开局建筑HUD；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds` | Failed | 旧服务用例要求街道1–2人，现行建筑清剿街道固定0；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban` | Failed | 旧装配断言期待UrbanStreetHud/街道流程，现行UrbanBuildingHud；基线同名失败。 |
| `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)` | Failed | 旧装配断言期待UrbanStreetHud/街道流程，现行UrbanBuildingHud；基线同名失败。 |

### PlayMode 基线失败逐条复核

| 完整测试名 | 本次结果 | 归因 |
|---|---|---|
| `SimulatedShooting.Tests.PlayMode.BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring` | Failed | 模式三原资源鞋底/地形高度不匹配；同名基线已失败，本次未修改人物或地形资源。 |
| `SimulatedShooting.Tests.PlayMode.BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry` | Failed | 模式三原资源鞋底/地形高度不匹配；同名基线已失败，本次未修改人物或地形资源。 |
| `SimulatedShooting.Tests.PlayMode.CombatFeedbackIntegrationTests.TrenchEnemyAttacksShowShotTracerAudioAndPlayerBlood` | Passed | 原断言检索已退役Tracer_Enemy_名称；改为检索生产Tracer_Actor_，仍检查真实弹道网格、线宽、枪声、血雾、十次伤害和死亡；本轮通过。 |
| `SimulatedShooting.Tests.PlayMode.CombatFeedbackIntegrationTests.UrbanEnemyAttacksShowShotTracerAudioAndPlayerBlood` | Passed | 原断言检索已退役Tracer_Enemy_名称；改为检索生产Tracer_Actor_，仍检查真实弹道网格、线宽、枪声、血雾、十次伤害和死亡；本轮通过。 |
| `SimulatedShooting.Tests.PlayMode.CombatScenePlayModeTests.Bdd15_17_21_HitDedupCorpseRetentionAndRetryCleanup` | Failed | 旧测试仍要求街道候选/实体数量13或15；现行城镇只生成建筑敌人，基线同名同原因。 |
| `SimulatedShooting.Tests.PlayMode.CombatScenePlayModeTests.Bdd15_20_WallsBlockPerceptionAndSceneUnloadRemovesActors` | Failed | 旧测试仍要求街道候选/实体数量13或15；现行城镇只生成建筑敌人，基线同名同原因。 |
| `SimulatedShooting.Tests.PlayMode.MovingTargetRangeIntegrationTests.P2_PhysicalImpactUsesCollisionPoint_EnvironmentDoesNotScore` | Failed | 现有新步枪/物理命中点偏差约9.3cm，超出旧2cm断言；未改靶命中规则或锚点。 |
| `SimulatedShooting.Tests.PlayMode.MovingTargetRangeSceneTests.Bdd31_DayHud_RestoresTheOriginalRearRowAndFortyMetreRoute` | Failed | 现有远端场景靶排距离38.908m或40.710m，与测试100m要求不一致；未改场景靶位。 |
| `SimulatedShooting.Tests.PlayMode.MovingTargetRangeSceneTests.Task003_StartAreaUsesTheExactZeroingRangeForegroundCopy` | Failed | 远端移动靶场缺ShootingBench前景副本；本次未改该场景。 |
| `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene` | Failed | 旧装配断言期待UrbanStreetHud/街道流程，现行UrbanBuildingHud；基线同名失败。 |
| `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds` | Failed | 旧用例查找街道页面控件，现行默认建筑HUD导致空引用；基线同名失败。 |
| `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_DeathThreeRetriesTrackingAndMovement` | Failed | 旧生产死亡夹具的敌人/视线位置不满足当前房门及感知条件；基线同名失败，新生产Urban十枪与转向回归通过。 |
| `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_ProductionVictoryRetryAndReturn` | Failed | 旧生产闭环仍要求街道入口提示/进入建筑；现行直接建筑清剿，基线同名失败。 |
| `SimulatedShooting.Tests.PlayMode.VrZeroingMechanicalSightTests.Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound` | Failed | 既有步枪觇孔/准星与AimLine标定不匹配；本轮只去掉换肩反射，不修改网格/瞄具标定。 |
| `SimulatedShooting.Tests.PlayMode.XRSceneHandoffTests.Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands` | Failed | 既有训练手模型可见数量为0而非2；新P3换肩手模型测试通过，未修改该交接可见性模块。 |
| `SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests.Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles` | Failed | 既有射校补偿后物理弹着仍偏约22.9cm；本次未修改射校算法或瞄具资产。 |
| `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd04_PickupPromptFacesShooterAboveRifleWithoutChangingText` | Failed | 旧断言期待英文GRIP TO PICK UP，现生产中文两个手柄抓握持枪；基线同名失败。 |
| `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd31_TargetKeepsItsDimensionsInTheOriginalRearRow` | Failed | 现有远端场景靶排距离38.908m或40.710m，与测试100m要求不一致；未改场景靶位。 |
| `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task005_TrainingRifleUsesLicensedQbz191Visual` | Failed | 既有新步枪模型尺寸/位置偏差约2.35cm，超出旧1mm断言；未改模型资产。 |
| `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease` | Failed | 旧断言支撑手半径12cm，现行22cm；本次保留既有抓取半径。 |
| `VRShooting.Tests.PlayMode.Screen25_UrbanPlayModeTests.BuildingFailureKeepsCountsAndOldSessionInputsAreRejected` | Failed | 旧街道残敌查询找不到元素，现行街道0敌人；基线同名失败。 |
| `VRShooting.Tests.PlayMode.Screen26_CombatCompositionPlayModeTests.UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry` | Failed | 旧街道阶段/房间流程对现行建筑清剿取到空值；基线同名失败。 |
| `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen18_UrbanMapSelection_RendersRangesAndStartsStreet` | Failed | 旧文本断言街道1–2人，现行建筑内部3–6人；基线同名失败。 |
| `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands` | Failed | 旧房间文本未搜索，现行靠近后开门；基线同名失败。 |
| `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce` | Failed | 旧结算文本街道清除，现行消灭建筑敌人；基线同名失败。 |

PlayMode两条鞋底失败的原始文本有运行差异：第一条只有新会话GUID不同，第二条鞋底高度仅有物理浮点尾数差别，仍为约1.927m对2.178m的既有失配。不是只凭数量归类，前后原始消息均保留。

## 复现与运行隔离

所有Unity运行串行，启动前检查无Unity进程；未使用`-nographics`。测试调用形式：

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe'
$project = 'E:/新建文件夹 (3)/SimulateShooting'
$evidence = "$project/docs/codex-reports/evidence/mode4-ai-shoulder-2026-11-12"
foreach ($platform in @('EditMode', 'PlayMode')) {
    if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity实例仍在运行' }
    $name = $platform.ToLower() + '-full'
    $arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'),
        '-runTests', '-testPlatform', $platform,
        '-testResults', ('"' + $evidence + '/' + $name + '.xml"'),
        '-logFile', ('"' + $project + '/Logs/mode4-ai-shoulder/' + $name + '.log"'))
    Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -Wait
}

```

定向运行使用相同命令加`-testFilter`，最终完整过滤条件、退出结果与实际命令参数可在对应日志核对。全量两次不传testFilter。初期无效编译尝试和中间失败结果未冒充最终通过数据；最后定向/全量结果见表。

## 工作区清理与遗留风险

已逐项还原跑批改写的TrainingChinese.asset、OpenXR设置、三个XR示例Prefab与7张旧证据PNG；检查后没有残留本轮新生成的临时InitTestScene文件，无需删除。4个受跟踪历史InitTestScene及其4个.meta均保留并与HEAD一致。

开工前的模式一证据目录和TacticalUIGallery已有改动保留；Gallery相对开工备份只有Urban预览Health=4→8一行变化。当前工作区按用户要求留存本次代码、必要资源、测试、契约文档、报告及新证据，未提交。因此git status包含预期工作区改动，所谓清理完成指无跑批污染，并非把授权的改动还原为空。清理明细见本地Logs/mode4-ai-shoulder/cleanup.json。git diff --check通过，ProjectSettings无改动，结束时无Unity实例。

VR实机待验：PICO上左右手保持各自物理手柄，使用B切换肩侧，再以新后手握把、新前手护木重抓，确认当前后手扳机、自然机瞄、后坐力、X换弹、前手锁定/恢复及A投雷；分别检查模式三/四，并回归模式一/二原入口。未接入真实PICO设备，本轮不把无设备测试记为实机通过。

已知资源风险仍存在：基线新步枪模型的枪口锚点、觇孔/准星和AimLine标定测试仍失败，包括约2.03°的既有瞄具轴偏差。本轮验证换肩不会反射或反转现有枪线，未擅自更改用户资源或把既有绝对标定失败说成已修复。旧街道/搜索验收、场景距离与鞋底等其余失败见逐条清单；它们不纳入本次四项实现范围。
