# task007 交付记录

2026-09-14；基线7c2f0b2，本文件所属Git提交为交付版本。工程实现与无VR自测完成，按[~]保留独立A/B审核、真实关卡集成和VR待验。

## 实现与契约

- `Assets/VRShooting/Runtime/Application/Combat/TrenchService.cs`：完整ITrenchService、HUD/小地图与ICombatStateService聚合、搜索/死亡/结果、取消重试；地图/简报不创建角色。
- 同目录`SquadFormationService.cs`：可复用三人纵队，沿玩家折线回溯1.5/3m，二号朝前三号朝后；导航成功仅表示请求接受，匹配到达反馈才改位置；失败保持并按1秒重试。
- 同目录`SeededCombatRandom.cs`：固定xorshift，按PointId排序再无放回选3–5人；实际偏移只暴露给视觉适配，HUD预估/死亡叉号使用预估中心。
- task004核心新增HasPendingInputs与TrackingValid，供任务层批次和跟踪门禁使用。伤害仍由核心唯一计算。
- [接口16](../../../接口文档/16-P3堑壕任务与三人纵队.md)、[BDD24](../../../BDD/screens/24-P3堑壕任务与队形.feature.md)定义批次、错误和联调方式。

## 自动化与BDD追溯

Unity2022.3.62f3c1 / Test Framework1.1.33，Windows，无VR。原始XML归档于[证据目录](../../../codex-reports/evidence/p3-task007)。

| 结果 | 通过/总数 | 失败/跳过 |
|---|---:|---:|
| 全量EditMode，含最后两个错误/生命周期用例 | 218/218 | 0/0 |
| 全量PlayMode | 128/128 | 0/0 |

新增EditMode14例、PlayMode2例，分别在`Assets/VRShooting/Tests/EditMode/Infrastructure/Screen24_TrenchTests.cs`与`Assets/VRShooting/Tests/PlayMode/Infrastructure/Screen24_TrenchPlayModeTests.cs`。

- BDD12/14：PreviewDoesNotCreateActors、FixedSeed、EnemyCountBoundaries、MapWeaponAndSceneErrors；覆盖地图A、简报无副作用、3/5边界、种子与候选顺序一致性。
- BDD15/17：SearchOnlyAndKillOnly、ValidationAndIdempotence、SameBatchDeath、HudDoesNotLeakOffsets、CancelRetryAndPause；覆盖仅搜索/仅击杀不足胜利、重复命令、同批最后击杀/节点与死亡竞争、部分统计、只结算一次、只读旧结果与新局隔离。
- BDD24：FormationFollowsPolyline、NavigationFailure、CrossDomainEventIdCollision、FailedNavigationCallback；覆盖折线朝向、实际到达、失败延迟、旧请求、跨输入类型ID冲突和回调内释放。
- PlayMode：FakeScene_BriefingCombatVictoryRetryFailure与FakeNavigation；空场景Text Probe消费真实服务事件，胜利/失败/取消重试、HUD与结果一致、卸载退订、导航反馈位置和终局停调度。
- 当前P1/P2与task001/004自动化全部同时回归。

复现：沿task004交付记录的Unity批处理命令，将结果/日志名换为`p3-task007-editmode-final`和`p3-task007-playmode`；依次执行EditMode（-nographics）和PlayMode。定向筛选可使用`-testFilter Screen24`，也可在已打开Editor使用task001请求桥。完整日志在本机Logs，提交原始测试XML。

## 接入与已知边界

场景先提交本Tick感知/命中/范围/导航事实，再TrenchService.Advance；武器输入从Combat接入。CompleteIfReady/登记命令不能越过未处理队列。查询与事件发布前更新为同一Revision；每个TrenchService只订阅一次核心，重试不增加订阅。导航Fact的TargetId必须为原RequestId。

使用P3Fixtures.TrenchDefinition、FakeCombatClock/FakeCombatWorld及定向数量随机替身；没有把它们注册到生产场景。代码不依赖UI或具体NavMesh对象，真实场景/UI绑定在task013汇合。摘要保存、返回主菜单路由也由task013接入。

无设备OpenXR Display初始化和许可证访问令牌更新提示属于环境信息，未阻止套件通过；不据此声称VR启动或性能通过。未测实机：登记头显后验证拐角/楼梯导航、队友朝向遮挡与尸体、移动舒适度、HUD可读性及72Hz/GC预算。独立A/B/C/D复选框未代签。
