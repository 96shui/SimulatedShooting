# task010 城镇任务交付记录

2026-09-14，基线91139e5；本文件所在Git提交为交付版本。工程与自动化自测完成，按[~]保留独立审核及真实UI/场景/VR待验。

## 交付

- `Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs`：完整IUrbanService、Street/Building/Results、入口/房门/检查范围、稳定敌人归属、分项计数、HUD/三层地图/门视觉、结果和取消重试。
- 复用task004的CombatCoreService及task007的SquadFormationService/SeededCombatRandom，伤害与队形规则无第二套实现。
- 公共DTO补充CombatEnemyAssignmentDto、RoomDto.MapPosition、UrbanResultDto.ElapsedSeconds与可选EntranceWorldPosition，配置及Fixture同步；缺失地图位置不伪造。见[接口17](../../../接口文档/17-P3城镇阶段与房间检查.md)、[BDD25](../../../BDD/screens/25-P3城镇阶段与检查.feature.md)。

## 结果与追溯

Unity2022.3.62f3c1 / Test Framework1.1.33，无VR，Windows。

| 最终全量 | 通过/总数 | 失败/跳过 |
|---|---:|---:|
| EditMode | 229/229 | 0/0 |
| PlayMode | 130/130 | 0/0 |

新增EditMode11例、PlayMode2例，原始XML见[证据目录](../../../codex-reports/evidence/p3-task010)。命令沿用task004批处理模板，结果/日志改为`p3-task010-editmode-final`、`p3-task010-playmode`，依次执行EditMode和PlayMode；定向筛选`-testFilter Screen25`。

`Screen25_UrbanTests`覆盖：BDD18地图/数量3层约束与固定种子；BDD19范围外拒绝、街道残敌风险进入和同局返回；BDD20门与检查分离、有活敌禁止、空房仍需开门检查、重复命令；BDD21建筑完成但街道残敌阻止胜利、仅击杀不胜利、最后检查/击杀与死亡同批优先失败、三层结果与重试只读旧结果。`Screen25_UrbanPlayModeTests`以Fake范围事实驱动生产服务Street→Building→Results、门快照、楼层状态、胜利/失败/重试及旧会话拒绝。

## 接入与边界

使用P3Fixtures.UrbanDefinition、FakeCombatClock/FakeCombatWorld及数量端点随机替身。场景收集入口、FloorId、`<RoomId>.door`和`<RoomId>.check`范围后Submit/Advance，再转发交互命令；同批结果只在Advance或无待处理事实的CompleteIfReady评估。场景读取GetEnemyAssignments和视觉快照，UI读取HUD/Session/Result；不根据GameObject数目统计击杀。

当前全量包含P1/P2与001/004/007回归。无C#编译错误/警告；无设备OpenXR初始化及许可证访问令牌更新为环境提示，不影响这些无VR测试，也不表示实机通过。完整Editor日志留本机Logs，避免提交许可证相关日志。

task013负责真实地图/门/输入/UI装配与摘要保存；此处不把替身流程记为真实集成。实机待验：登记头显后验证入口/楼层切换、门口检查可达性、队友楼梯路径、UI/小地图可读性、死亡反馈及性能预算；未代签A/B/C/D。
