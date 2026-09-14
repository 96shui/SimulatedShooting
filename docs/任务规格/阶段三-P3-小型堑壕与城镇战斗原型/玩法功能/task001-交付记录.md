# task001 工程交付记录

交付整理：2026-09-14；基线 `5605031`，交付版本由本文件所在 Git 提交追溯。状态 `[~]`：工程交付和自动化自测通过；节点 A 独立审核、UI/场景负责人签核仍待执行，不把自测当作外部签核。

## 实际交付

- `Assets/VRShooting/Runtime/Contracts/P3/`：堑壕、城镇、玩家、队伍、视觉、地图、结果 DTO，查询/命令/事件及世界输入、时钟、随机、导航、保存端口。
- `VRShooting.Contracts` 独立程序集：共享 Contracts/Dto/Enums/Compat，保留既有文件 GUID、命名空间与枚举编号；使用方增加显式引用。
- `CombatConfigDto`、`CombatSceneDefinitionDto`：默认参数与非法数值、ID、楼层房间、生成容量及可导航候选配置校验。
- `Assets/VRShooting/Tests/Support/`：脚本化 Trench/Urban/CombatState 服务、时钟/随机/世界/保存替身、只读快照与统一 Fixture。仅测试程序集可用，不装配到生产场景。
- `Assets/VRShooting/Tests/Editor/P3ContractTestRunner.cs`：显式请求文件驱动现有 Unity TestRunner，输出原始 XML，支持域重载。
- 接口 14、BDD 22、既有 BDD/接口及阶段任务进度同步。C01–C16 决定见 [冻结清单](../契约冻结清单.md)。

## 自动化证据

Unity 2022.3.62f3c1 / Test Framework 1.1.33，在现有 Editor、无 VR 设备条件执行；原始结果位于 [证据目录](../../../codex-reports/evidence/p3-task001)。

| 验证 | 结果 | 原始 XML |
|---|---|---|
| 最终全量 EditMode | 185 通过、0 失败、0 跳过 | `editmode-reviewed.xml` |
| 全量 PlayMode，含 P1/P2 回归 | 122 通过、0 失败、0 跳过 | `playmode-full.xml` |
| 最后目录/预估点 Fixture 修正后的定向 PlayMode | 1 通过、0 失败、0 跳过 | `playmode-reviewed.xml` |

BDD 22 的默认值/不可变快照/依赖隔离/配置错误/固定随机/旧会话与重复事件/失败恢复对应 `Screen22_P3ContractTests`；空白场景显示快照和卸载退订对应 `Screen22_P3FixturePlayModeTests`。全量结果同时覆盖既有 P1/P2 自动化用例。

运行请求格式、脚本快照用法及 UI/场景独立接入步骤见 [Fixture 使用说明](task001-Fixture使用说明.md)。保持原始 XML 的执行时间；后续整理日期不代表重新执行。

## 范围与联调

本任务交付契约、校验、替身，不实现敌人攻击、血量、搜索或胜负生产状态机。生产规则由 task004/007/010 实现，task013 装配。UI 与场景可引用契约并独立使用 Fixture；无真实 TrenchScene/UrbanScene 联调或负责人签核记录。

节点 A 的独立复验需确认程序集无 UI/XR 依赖、接口签名与 C01–C16、两条消费线可独立编译。纯 DTO/替身不涉及 VR 体验；移动、枪线、舒适度、尸体遮挡与性能预算由后续实现/节点 D 在登记设备后验证。72Hz/11ms/0B 仅为目标值，未测量。
