# task004 共用战斗核心交付记录

日期：2026-09-14。执行前已fetch并pull main，远程基线为`5605031`；先收口task001为`ce2841b`，本任务交付版本由本文件所在Git提交追溯。状态`[~]`：生产核心、适配器与自动化自测已完成；独立A/B及VR待验，不宣称双模式真实关卡或阶段收口完成。

## 交付内容

| 路径 | 行为 |
|---|---|
| `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs` | 玩家生命、存活、姿态/换肩/拐角状态；共用武器协调；敌人出现/发现/攻击/死亡；会话隔离、批次输入、计时、事件去重与尸体快照 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatCoreContracts.cs` | ICombatCoreService、完整快照、射击/反馈、输入帧、移动/枪线端口；不依赖UI、XR和具体场景 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs` | training-rifle增加P3适用范围，P3会话30/120单发；复用原开火/握持/弹药规则，增加内部换弹取消入口，P1/P2参数保持原值 |
| `Assets/VRShooting/Runtime/Input/CombatInputController.cs` | IXRTrainingInput与近距握持/跟踪事实接入；Trigger上升沿单发，换弹/换肩/姿态；中断后持按不补射 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatLocomotionPolicy.cs` | P3站蹲卧步速、30度离散转向、P1/P2禁人工移动；VR不输出强制眼高 |
| `Assets/VRShooting/Runtime/Unity/Combat/` | CharacterController地面移动适配、第一碰撞枪线、墙内枪口阻挡、稳定实体ID绑定 |
| `Assets/VRShooting/Tests/EditMode/Infrastructure/Screen23_CombatCoreTests.cs` | 19个规则测试用例（含参数化），生命/弹药/计时/事件/生命周期及错误边界 |
| `Assets/VRShooting/Tests/PlayMode/Infrastructure/Screen23_CombatInputPlayModeTests.cs` | 4个无VR输入与真实基础Physics/CharacterController适配测试 |

接口依据：[接口15](../../../接口文档/15-P3共用战斗核心.md)，沿用[接口14](../../../接口文档/14-P3战斗契约与独立测试基础.md)与[BDD23](../../../BDD/screens/23-P3共用战斗核心.feature.md)。候选契约细化不表示独立签核已完成。

## BDD—测试映射

| BDD场景 | 验证 |
|---|---|
| BDD15弹药、BDD23双手单发/换弹 | EditMode Ammo_DualGrip/EmptyAmmo；PlayMode ManualInput：30/120、单发持按不连射、双手门禁、2秒换弹守恒 |
| BDD15/19姿态、换肩、拐角 | PostureShoulderAndCorner；ManualInput及Locomotion：服务状态、0.9m/s蹲姿、1.1m无VR眼高、VR不改眼高、一次30度转向 |
| BDD15/21玩家死亡、BDD23攻击与去重 | VisibleRangeAngle、LargeTick、HitAndShotIds、DeathIsClamped、DamageEventDuplicate：遮挡/范围/朝向、10点/秒、死亡幂等、同批死亡优先、尸体保留 |
| BDD23暂停/跟踪/终局 | LifecycleCancelsReloadAndAttacks、TrackingLoss、TerminalSnapshot、TrackingAndPause：停止调度、取消换弹不丢弹、恢复后重新扣动 |
| BDD23错误/重复/旧会话 | UnknownShot、NewSession、DuplicateId、OldShot、Unrepresentable、DisposeDuringFeedback：无伪造子弹、无重复穿透计杀、旧局拒绝、极端值拒绝、回调内卸载 |
| BDD23基础墙体碰撞 | PhysicsFirstHit：墙在目标前先阻挡，枪口在墙内不穿墙，无遮挡时返回稳定敌人ID |
| P1/P2回归 | 当前全量套件；Locomotion额外验证P1/P2禁止人工移动/转向。现有单发/三发轮次、P2两发起射/持续连射/禁射测试一起运行 |

## 实际测试结果

Unity 2022.3.62f3c1，Windows，Unity Test Framework 1.1.33。最终生产代码与测试一起执行全量：

| 模式 | 总数 | 通过 | 失败 | 跳过 | 原始结果 |
|---|---:|---:|---:|---:|---|
| EditMode | 204 | 204 | 0 | 0 | [XML](../../../codex-reports/evidence/p3-task004/p3-task004-editmode-final.xml) |
| PlayMode | 126 | 126 | 0 | 0 | [XML](../../../codex-reports/evidence/p3-task004/p3-task004-playmode-final.xml) |

相对task001的185/122，新增19/4。首轮199/126通过后补充5个边界用例并重跑全量，以上是最终结果。未将配置预算当作性能测量。

复现命令（关闭同一项目的交互Editor后执行；若Editor已打开，可用task001请求桥）：

```powershell
& 'D:/Unity Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProject/VR' -runTests -testPlatform EditMode -testResults 'D:/UnityProject/VR/Logs/p3-task004-editmode-final.xml' -logFile 'D:/UnityProject/VR/Logs/p3-task004-editmode-final.log'
# 等待上一次Unity进程退出后运行：
& 'D:/Unity Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -projectPath 'D:/UnityProject/VR' -runTests -testPlatform PlayMode -testResults 'D:/UnityProject/VR/Logs/p3-task004-playmode-final.xml' -logFile 'D:/UnityProject/VR/Logs/p3-task004-playmode-final.log'
```

日志无C#编译错误或编译警告。环境日志存在许可证访问令牌更新不可用及无设备OpenXR Display初始化失败信息；没有阻止本次无VR套件执行，测试全部通过。这不证明VR启动通过。完整Editor日志留在本机Logs（不提交其中许可证相关信息），提交原始测试XML。

## 独立使用与联调入口

1. 构造可注入ICombatClock、CombatCoreService和新SessionId；Start接收由任务层选好的敌人稳定ID/位置/朝向。核心无随机，固定输入/位置/时钟序列可复现；地图候选抽样属于task007/010。
2. 订阅Changed取得同一个完整快照；Feedback的EnemyHit供音效/命中确认、EnemyDied/PlayerDied供任务层统计与判定，禁止再次扣血。查询不从HUD或场景对象反推。
3. 无VR直接运行上述PlayMode测试，无需手动改Hierarchy。ManualXRTrainingInput、FakeCombatClock、测试内RayFake/MoveFake是替身；服务和Physics/CharacterController适配器是生产实现。
4. 场景适配器采集PlayerPose/Perception/Hit，同批Submit后Advance；使用控制器时先采集感知，最后Step，一Tick一次。绑定提供玩家根姿态、跟踪、握把范围与当前肩位枪口。实体绑定必须使用本Session稳定ID，碰撞层须包含环境并排除枪与玩家自身。
5. task007/010消费死亡事件、查询血量与敌人快照，实现队伍/搜索/胜负；task013装配ICombatStateService/HUD与真实关卡。核心未伪造队伍或搜索完成状态。导航端口/替身沿用task001，三人路径跟随由task007实现。

## 待验与范围边界

- 当前完成本线规则与独立适配验证，UI/场景生产绑定、真实TrenchScene/UrbanScene闭环尚未联调，归对应场景/UI任务及task013。
- VR设备未连接，型号未登记，实机结果为待验。接入后需在登记设备上验证：近距双手拾枪、单发/换弹与失踪恢复、左右肩绑定、站蹲卧/30度转向舒适度、枪线与墙体、角色尸体碰撞及命中音效；预期分别对应接口15与BDD15/19/23。
- CPU/GPU、稳态GC、加载和真实设备72Hz预算未测，节点D执行；不声称0B/frame已达标。
- 未引入高级AI、队友战术、医疗、任务结算页面或第二张地图。下一项可沿本线推进task007。
