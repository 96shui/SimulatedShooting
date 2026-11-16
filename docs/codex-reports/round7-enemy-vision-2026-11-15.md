# 模式四跨房间感知与圆形视野修复（2026-11-15）

本轮负责 P3 敌人感知、转向、门阻挡与对应契约/测试。使用 Unity 2022.3.62f3c1；生产 `CombatScene.unity`、真实 UrbanService/CombatCoreService、真实人物 Prefab 和 Physics；仅替换 VR 跟踪/输入并移动玩家位置。未连接 PICO，不将此次无设备场景运行写成头显实机验收。

## 结论

已实测复现并修复：玩家已经从 room-11 到 room-12、敌我视线实际通畅时，旧代码仍把房门登记状态当作可见性前置，且 UrbanService 再次拒绝 `Perception=true`；敌人因此停在 Idle，而转向又只允许 Attacking，持续不转向、不攻击。修复后进入 Attacking、转向收敛并造成伤害。

证据指向**错误的门登记感知门控 + 转向依赖 Attacking**。不能把本次未观测到的“射线命中自身/门框”或“actor 未实例化”写成已确认根因。所有生产种子1的敌人都在进入房间前实例化。另补了自身过滤、多个门视图同步及缺失actor恢复回归。

全量新增失败为0，新增23条全部通过。严格基线门禁不通过：本轮同时修复了3条基线失败（EditMode 1条、PlayMode 2条），失败集合缩小；不能把“新增失败为0”写成“失败集合完全相同”。未修改旧测试来维持失败，也未重建或改写基线。

## 复现与根因证据

先运行未修复实现的 `Round7EnemyVisionTests`。第一轮只验证“显式开门命令后移到房间中心”，1/1通过；因此没有把这一条顺利路径当作故障复现。第二轮加入“玩家已在同房间，登记门状态关闭”的真实场景路径，2条中1通过、1失败。失败断言为 Expected Attacking / But was Idle。

复现命令（仓库根目录 PowerShell）：

```powershell
& 'docs/codex-reports/evidence/round7-enemy-vision-2026-11-15/run-unity.ps1' -Kind PlayMode -Name repro-gates-before -Filter Round7EnemyVisionTests
```

固定种子1选择 building-spawn-2/3/6/5；room-12敌人位于 `(23.2, 0.05, 86.8)`，玩家先到room-11 `(25,0,68)`，再到room-12 `(25,0,80)`。旧代码关键帧如下，日志完整路径为 `evidence/round7-enemy-vision-2026-11-15/repro-gates-before.log`：

```text
[R7-Setup] room-12:actor=True;room-21:actor=True;room-31:actor=True;room-31:actor=True
[R7-Boundary] clear=True registeredDoorOpen=False perceptionAccepted=False error=InvalidState
phase=registered-closed-room-12-0  tick=2  oldRay=clear state=Idle angle=25.26 health=10
phase=registered-closed-room-12-29 tick=31 oldRay=clear state=Idle angle=25.26 health=10
```

最后两行从原始 `[R7-Path]` 行省略坐标与门列表；完整原文保存在证据文件中。测试对服务边界另提交一次与几何结果一致的感知探针，明确证实第二道拒绝；最终正常开门/背后转向回归不依赖这个探针。

| 环节 | 旧代码与实测 | 处理 |
|---|---|---|
| actor实例化 | ApplyVisual在Activate时为全部敌人生成actor；room-12进入前actor=True | 不是本次主因；新增actor丢失恢复及失效NavMeshAgent防护 |
| 单点射线 | 故障房内oldRay=clear；room-11到room-12敌人的隔房射线命中DoorLeaf属正常遮挡 | 未证实本路径有自身/门框误命中；升级为多点RaycastAll、自身/玩家过滤和起点环境重叠检查 |
| 场景门登记门控 | 旧CollectPerception在射线clear之后仍执行visible &= 所属房门.IsOpen | 删除该间接前置，真实几何决定Visible |
| 服务门登记门控 | 旧UrbanService.Submit对所属房间登记未开的Perception=true返回InvalidState | 删除该拒绝；开门交互范围与房间业务状态继续由服务管理 |
| 转向门控 | 旧代码只有entity.State==Attacking才TurnTowards；复现3秒夹角始终25.26° | 圆内且无遮挡当帧开始转向，再提交真实Forward |
| 门碰撞 | 旧Apply只改NavMeshObstacle，门扇Collider始终存在 | 开门同命令禁用门扇非Trigger Collider；关闭/复位恢复原启用状态，门框保持实体 |
| 同房间多个门 | 旧ApplyVisual只同步FirstOrDefault命中的门 | 同RoomId所有门视图均消费快照；新增生产场景回归 |

本次直接证明的是“门登记状态不应代表视线”，并未测得某个异步回调延迟值，所以不把“登记更新滞后”作为已经测量确认的时序问题。即使入口门真正关闭，玩家与敌人都已在同一个房间内时，该门也不位于他们之间。

## 新感知模型与调参

以敌人PerceptionOrigin为圆心，先判断XZ水平半径和独立垂直容差，再从同一原点向玩家脚底加头/胸/脚高度发出三条RaycastAll。任意一条没有有效遮挡就发现玩家；发现不依赖初始朝向。过滤敌人自身层级、玩家层级、玩家步枪和Trigger，不按layer2无条件忽略环境。起点若嵌入真实环境碰撞体则阻挡，避免Physics从内部发射而漏掉墙。

核心继续负责存活、运行/追踪门禁、水平范围、垂直差、Attacking、水平12°开火门槛和EnemyAttackInterval。场景立即平滑转向，并把实际Forward送回核心；无瞬移转向、无UI伤害计算。场景精确垂直差为玩家胸部到PerceptionOrigin；核心用脚底位置等量加胸高估算垂直差作第二道范围复核。

| CombatConfigDto参数 | 默认值 | 调整建议 |
|---|---:|---|
| EnemyRange | 30m | 水平发现半径；希望更近才发现可试20–25m，跨房间仍受墙和门阻挡 |
| EnemyVerticalTolerance | 2.5m | 敌人感知原点与玩家胸部的高差上限；建议1.8–2.5m，过小会漏掉楼梯上的同区域目标 |
| EnemyHeadSampleHeight | 1.65m | 玩家脚底以上；随人物目标高度调整，头>胸>脚 |
| EnemyChestSampleHeight | 1.2m | 胸部采样及垂直差参考；与玩家模型身高一起调整 |
| EnemyFootSampleHeight | 0.15m | 避开地面共面误差；建议0.1–0.25m |
| EnemyTurnSpeedDegrees | 180°/s | 180°转身约1秒；希望更慢试120，更快试240 |
| EnemyAttackInterval | 1s | 对准≤12°后开始原攻击间隔；本轮不修改伤害与射速 |
| EnemyHalfViewAngle | 60°（兼容旧配置） | 不参与发现或开火；当前唯一开火角门槛是12° |

通过 `CombatConfigDto.Default.WithEnemyVision(range, tolerance, head, chest, foot, speed)` 创建配置，并注入UrbanService/TrenchService/CombatCoreService。`ICombatCoreService.Config`向场景只读提供实际生效配置；WithPlayerHealth保留所有感知参数。Validate拒绝非正、NaN/Infinity及头胸脚顺序错误。

诊断默认关闭：运行时组件Inspector的Enemy Vision Diagnostics，或 `runtime.EnemyVisionDiagnostics=true` 可开启。逐帧每个存活敌人的 `[EnemyVision]` 包含tick、实体ID、actor/restored、水平距离、垂直差、inCircle、三条采样最近阻挡物、Visible、Submit返回、核心State和当前水平夹角。复验后关闭开关即可停止日志。

## 修复后的生产场景证据

最终全量PlayMode日志、XML及提取后的逐帧日志在 `docs/codex-reports/evidence/round7-enemy-vision-2026-11-15/`。这些测试加载实际生产地图/人物/服务并运行实际遮挡查询，不是仅注入Visible的单元测试。

正常开门路径还把room-12敌人朝向设为背对玩家，用于验证完整180°转身以及“Idle首帧立即转向”。每步0.1秒，最终全量运行关键帧：

| 路径阶段 | Tick | State | 水平夹角 | 玩家生命 |
|---|---:|---|---:|---:|
| room-11 | 2 | Idle（room-12门扇遮挡） | 15.91° | 10 |
| room-12首帧，原本背对180° | 4 | Attacking | 162.00° | 10 |
| room-12第10帧，约1秒 | 13 | Attacking | 0.02° | 10 |
| 对准后满原1秒开火间隔 | 23 | Attacking | 0.02° | 9 |
| room-12第30帧 | 33 | Attacking | 0.02° | 8 |

逐帧诊断摘录（原样，完整行见diagnostic-keyframes.log）：

```text
[EnemyVision] tick=4 id=c4430163000746749a81dee61ac7f5d0.enemy-001 actor=True restored=False distance=7.334 vertical=0.500 inCircle=True head=clear chest=clear foot=clear Visible=True submit=None State=Attacking angle=162.00
[EnemyVision] tick=13 id=c4430163000746749a81dee61ac7f5d0.enemy-001 actor=True restored=False distance=6.750 vertical=0.500 inCircle=True head=clear chest=clear foot=clear Visible=True submit=None State=Attacking angle=0.02
[EnemyVision] tick=1 id=3047dd0fec834d6191bcb9426447e64f.enemy-001 actor=True restored=False distance=7.620 vertical=0.500 inCircle=True head=room-12/DoorLeaf chest=room-12/DoorLeaf foot=room-12/DoorLeaf Visible=False submit=None State=Idle angle=8.94
[R7-Floor] head=Floor_2 chest=Floor_2 foot=Floor_2
```

原故障条件“房内通畅但登记未开”的复验：`[R7-Boundary] clear=True registeredDoorOpen=False perceptionAccepted=True error=None`；首帧Attacking、7.25°，最后0.02°、生命8。闭门反例所有三条采样真实命中room-12/DoorLeaf，持续Idle、无开火。跨楼层回归另外把诊断采样容差放宽到10m，单独证明三条射线都真实命中Floor_2；正常运行仍使用默认2.5m。

证据入口（相对本报告）：

- [原始复现XML](evidence/round7-enemy-vision-2026-11-15/repro-gates-before.xml) / [修复前路径日志](evidence/round7-enemy-vision-2026-11-15/room11-room12-before.log)。
- [修复后全敌人逐帧日志](evidence/round7-enemy-vision-2026-11-15/room11-room12-after.log) / [关键帧原文](evidence/round7-enemy-vision-2026-11-15/diagnostic-keyframes.log)。
- [EditMode完整XML](evidence/round7-enemy-vision-2026-11-15/editmode-full.xml) / [PlayMode完整XML](evidence/round7-enemy-vision-2026-11-15/playmode-full.xml)。
- [机器对比](evidence/round7-enemy-vision-2026-11-15/baseline-comparison.json) / [736条完整fullname对比CSV](evidence/round7-enemy-vision-2026-11-15/fullname-comparison.csv)。
- [资源恢复和主菜单保护校验](evidence/round7-enemy-vision-2026-11-15/cleanup.json)。

## 测试与完整fullname对比

权威基线直接读取 `evidence/mainmenu-relayout-2026-11-15/editmode-full.xml` 和 `playmode-full.xml`，分别391与322条，包含应保留的主菜单成果。没有使用更早的baseline-*.xml替代本轮工作区基线，也没有别名映射、删用例或改写原始XML。

| 平台 | 基线总数/通过/失败 | 本轮总数/通过/失败 | 新增 | 新增失败 |
|---|---|---|---|---|
| EditMode | 391 / 371 / 20 | 403 / 384 / 19 | 12，全过 | 0 |
| PlayMode | 322 / 299 / 23 | 333 / 312 / 21 | 11，全过 | 0 |

EditMode基线失败转通过的完整fullname：

```text
VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)
```

旧失败位于该测试Die辅助方法45行，对Urban闭门登记敌人提交Perception=true时断言Success；本轮移除错误门登记拒绝后通过。未修改这个旧测试。

PlayMode基线失败转通过的完整fullname：

```text
SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_DeathThreeRetriesTrackingAndMovement
VRShooting.Tests.PlayMode.Screen26_CombatCompositionPlayModeTests.UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry
```

第一条原来在P3ProductionFlowTests.cs:363失败（Real line-of-sight enemy attacks must reach failure）；现在实际感知/转向/攻击使死亡、三次重试、跟踪与移动流程跑通。第二条原来在Screen26_CombatCompositionPlayModeTests.cs:69读不到死亡Summary；该用例重试后直接提交Perception=true，原门登记拒绝导致未发生攻击，本轮正确通过。两个旧测试均未修改。

全部原391/322个fullname仍在，无别名映射、无缺失，无Passed→Failed，无Skipped/Inconclusive，新增23条全Passed。其余19个EditMode和21个PlayMode失败均与基线逐条完整fullname一致。严格“失败集合逐条完全同名”不成立，**compare-tests.py按严格条件返回exit 1，未通过该严格门禁**，原因只有上述3条Failed→Passed；新增失败为0。机器对比详见 `baseline-comparison.json` 和逐条 `fullname-comparison.csv`。

定向EditMode为41/41通过；首轮修复后定向PlayMode为13/14通过，发现销毁actor后提前访问NavMeshAgent的MissingReferenceException，随后修复并在最终全量中验证对应新增用例通过。最终接受依据为完整403/333条结果，不隐藏早期红灯记录。模式三既有 `SimulatedShooting.Tests.PlayMode.CombatFeedbackIntegrationTests.TrenchEnemyAttacksShowShotTracerAudioAndPlayerBlood` 及双模式转向/规则回归通过。

新增规则测试覆盖双模式背后发现但未对准不伤害、水平半径边界、垂直容差边界、配置传播及非法配置。新增PlayMode覆盖room-11→room-12发现/转向/伤害、门登记不替代实际视线、闭门、真实楼板、actor丢失恢复、同RoomId多门同步、自身/玩家碰撞过滤、局部遮挡、layer2墙阻挡、门碰撞开关复位、转向速度。既有CombatFeedbackIntegrationTests覆盖Trench和Urban真实枪声/弹道/玩家伤害及生命周期。

复跑全量：

```powershell
& 'docs/codex-reports/evidence/round7-enemy-vision-2026-11-15/run-unity.ps1' -Kind EditMode -Name editmode-full
# 等该Unity完全退出，再启动下一条，脚本也检查是否已有Unity进程。
& 'docs/codex-reports/evidence/round7-enemy-vision-2026-11-15/run-unity.ps1' -Kind PlayMode -Name playmode-full
python docs/codex-reports/evidence/round7-enemy-vision-2026-11-15/compare-tests.py
```

## 本轮文件与工作区保护

生产代码：

- `Assets/SimulatedShooting/Runtime/Scene/CombatEnemySight.cs`（新增）：圆形范围、多点真实遮挡及过滤。
- `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs`：发现即转向、感知提交、可开关诊断、actor恢复及多门视图同步。
- `Assets/SimulatedShooting/Runtime/Scene/CombatActorView.cs`：转向速度接受实际配置。
- `Assets/SimulatedShooting/Runtime/Scene/CombatDoorView.cs`：门扇Collider随开关与复位同步。
- `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs`：全向水平范围发现，保留开火对准与节奏。
- `Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs`：移除门登记对真实感知的拒绝。
- `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs`、`CombatCoreContracts.cs`：参数、校验、复制和只读Config。

测试：新增 `Assets/VRShooting/Tests/EditMode/Infrastructure/Round7EnemyVisionRuleTests.cs`、`Assets/SimulatedShooting/Tests/PlayMode/Round7EnemyVisionTests.cs`、`Round7EnemySightPhysicsTests.cs`；新增脚本对应Unity .meta。

契约：新增BDD36及接口28，更新各自README和P3玩法task004/010、场景task012追加条款；不创建新任务编号。另新增本报告、round7证据目录内运行/比较/恢复脚本及输出。

未执行git commit/push/切分支/stash/checkout；所有Unity运行串行，结束时无Unity进程。主菜单任务的7个原有修改文件不属于本轮变更，最终与运行前备份逐字节一致且git status仍为Modified；旧证据全部逐字节恢复。恢复了字体、OpenXR设置及3份被跑批序列化的XR Prefab，另恢复原有截图。根目录Evidence/P3-production-M67-in-flight.png在开工时干净，按HEAD原始blob字节恢复，没有对用户原有修改使用HEAD恢复。四份被跟踪的Assets/InitTestScene*.unity及各自meta全部保留并逐字节核对。最终保护结果见 `evidence/round7-enemy-vision-2026-11-15/cleanup.json`。

## VR待验

PICO头显内复走一层一号→一层二号：确认打开门、从侧后方进入，观察敌人转身、枪声、玩家受击；再关门隔墙验证无攻击，上下楼验证楼板遮挡。检查不同身高、蹲姿以及遮挡边缘的观感。此次未连接设备，不用桌面场景证据替代这些实机项目。
