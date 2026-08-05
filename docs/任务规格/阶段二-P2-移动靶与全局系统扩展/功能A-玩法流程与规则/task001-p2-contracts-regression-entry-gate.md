# task001 P2 契约、P1 回归准入与测试基础

## 负责人

功能A-玩法流程与规则

## 目标

在任何 P2 业务代码、UI、场景或武器接入开始前，完成 P1 收口准入审计，解决现有 P2 文档中的接口缺口，并冻结四个负责范围共同使用的 DTO、服务、事件、错误码、稳定测试 ID 和无 VR 测试替身。

本任务是 P2 的硬门禁。P1 task014-task016 未完整收口、13 项接口缺口未在权威文档中解决、P2 测试程序集不能运行或稳定 ID 未冻结时，本任务不得标记完成，task002-task018 只能进行不依赖未冻结契约的文档或视觉预研。

## 参考资料

- `AGENTS.md`
- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/README.md`
- `docs/BDD/测试实现建议-Unity.md`
- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/03-训练模式选择.feature.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/BDD/screens/13-武器库武器墙.feature.md`
- `docs/BDD/screens/22-设置界面.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/10-设置服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `docs/接口文档/12-玩法服务层开发交付清单.md`
- `docs/任务规格/阶段一-P1-100m射校原型闭环/任务进度.md`
- `docs/任务规格/阶段一-P1-100m射校原型闭环/功能A-玩法流程与规则/task014-p1-flow-playmode-integration-tests.md`
- `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task015-ui-reference-polish-and-testids.md`
- `docs/任务规格/阶段一-P1-100m射校原型闭环/场景/task016-scene-visual-performance-integration.md`

## BDD 场景追溯

本任务不实现下列场景的最终表现，但必须保证每个场景都有明确的服务命令、查询 DTO、事件、错误分支、稳定测试 ID 和后续责任任务：

- `02-游戏主界面`：`进入移动目标射击设置`、`打开武器库`、`打开设置界面`、`快速重复点击菜单按钮`。
- `03-训练模式选择`：`选择不同训练卡片`、`确认进入所选模式`、`返回主界面`。
- `08-移动靶设置`：`默认选择白天模式`、`切换到夜晚模式`、`选择目标速度`、`开始训练`、`返回上一级`。
- `09-移动靶白天HUD`：`训练开始前等待 3 秒`、`HUD 显示白天训练状态`、`每次点射消耗两发弹`、`命中目标后更新命中数`、`目标到达左端后禁止射击`、`目标抵达右侧终点后进入结算`。
- `10-移动靶夜晚HUD`：`夜晚 HUD 使用微光镜显示`、`显示夜晚移动状态`、`端点停留期间禁止射击`、`夜晚模式命中统计`、`夜晚训练结束`。
- `11-移动靶结算`：`根据命中发数显示评级`、`显示训练摘要`、`显示 5 次点射记录`、`重新训练`、`返回模式选择`。
- `13-武器库武器墙`：`默认显示当前已装备武器`、`选择武器后刷新属性`、`装备选中的武器`、`返回上一界面`、`不适用场景的武器提示`。
- `22-设置界面`：`显示当前设置`、`修改二选一或多选设置`、`修改滑条设置`、`应用设置`、`恢复默认设置`、`返回不保存临时修改`、`设置界面在训练中打开`。

## P1 回归准入硬门禁

进入 P2 实现前必须形成以下可审计证据：

- P1 task014 已提供一个由无 VR 输入替身贯穿主菜单、场景、武器、3 发射击、弹着分析、应用调整和结算的 PlayMode 夹具。
- P1 task014 至少覆盖第 1 轮通过为优秀和 3 轮未通过为不及格两个可控结果，且测试不依赖真实随机结果。
- P1 task015 的全部 P1 页面、按钮、文本和 HUD 测试 ID 已收口；无 VR 与 VR World Space Canvas、XR UI 射线、唯一 Camera/AudioListener 有自动化或明确实机证据。
- P1 task016 的场景加载、关键绑定、性能基础检查和无 VR/VR 验收说明已完成，并在 task014/task015 最终状态上重新验证。
- `P1/任务进度.md` 将 task014、task015、task016 标记为完整完成，并记录当前基线的 EditMode、PlayMode 和 VR 实机证据；只存在主要实现但缺测试或实机证据时不得视为准入通过。
- P2 契约和实现不得改变 P1 的 3 发一轮、HUD Key、训练武器或射校评级来换取 P2 测试通过。

## 必须先解决的 13 项接口缺口

以下问题必须先同步更新 BDD、接口文档和本阶段任务规格，再允许编写对应实现：

1. **两发点射原子命令**：替换 `RecordBurst(string, ShotInputDto)` 的单发歧义，定义一次点射包含两发射击输入、两个射击序号、逐发命中快照、一次点射记录和最终弹药快照的原子结果；禁止先扣弹后回滚。
2. **弹药唯一权威与部分点射结算**：规定 `IAmmoService/AmmoDto` 是弹药唯一数据源；`MovingTargetSessionDto` 不得维护可独立修改的第二套 `AmmoRemaining`，只允许引用或镜像同一快照。路线结束时 `TotalAmmoConsumed` 必须等于实际消耗的 0-10 发，`BurstRecords` 只保存实际发生的 0-5 条记录；结算 UI 可以把其余固定槽位显示为“未使用”，但不得伪造射击时间、位置或命中记录。该口径必须先同步修正 BDD11 中固定“消耗 10 发/5 次均命中或未命中”的歧义。
3. **禁射前置门禁**：定义移动靶点射协调服务或等效端口，在弹药消耗、后坐力、触觉、枪口焰、弹道和命中反馈前验证等待/端点禁射状态。
4. **路线配置与进度**：为 `Tick` 提供可注入的路线长度、倒计时和端点停留配置，并在状态 DTO 中提供可驱动场景的规范化进度或等效确定性数据。
5. **Session 生命周期**：明确 `ITrainingSessionService`、移动靶服务、武器 Session 和场景加载的创建/开始/完成/取消顺序，以及唯一 SessionId 来源。
6. **P2 UIEvent 与返回路由**：补齐移动靶开始/重训/返回、武器预览/装备、设置与训练暂停事件；解决跳转表引用了枚举中不存在的事件；`ReturnToScreen` 必须采用受约束的 ScreenId 或严格验证策略。P2 将“重新训练”冻结为返回移动靶设置页并保留上次昼夜/速度选择，不允许 Presenter 在“直接重开/返回设置”间自行选择。
7. **HUD/结算稳定字段**：冻结 `countdown`、`ammo`、`fireMode`、`hits`、`speed`、`direction`、`shootState`、`burstRhythm`、`optic`、`endpointHold` 等 Key/PromptId，并为结算冻结结构化评级规则展示 DTO/Key；不允许 UI 从自由文本反推状态或硬编码 3/4/5 发评级阈值。
8. **微光镜状态契约**：增加日间觇孔/夜间微光镜的只读状态 DTO 和事件，明确夜晚自动启用、HUD 只显示、场景只渲染。
9. **设置 Pending 预览与回滚**：增加 Pending 变更/恢复默认/Discard 事件；Pending 只改变设置页内隔离 Preview，保存成功后才改变全局 XRI/HUD/AudioMixer/曝光，Discard 只恢复 Preview 且全局对象保持不变。冻结保存失败和返回不保存的唯一语义。
10. **训练暂停返回路径**：为从训练中打开设置增加最小暂停叠层 ScreenId、路由参数和返回契约，保证返回不销毁当前 Session。
11. **最近评级与档案摘要**：增加最小 `ITrainingResultSummaryStore` 及 `PlayerProfileSummaryDto`/查询接口，满足返回模式选择时保存本次移动靶评级，并向通用状态面板提供档案标识、训练等级和最近评级；不得隐性扩展成完整历史记录或账号系统。
12. **P2 Unity 稳定 ID 与绑定**：在接口 11 中补移动路线、端点、目标、昼夜根、微光镜、武器墙、Slider、Toggle 和 HUD 字段的命名、绑定组件及稳定测试 ID。
13. **P1 实现兼容和组合根扩展**：解决当前训练步枪仅适用 100m、弹药不是 10 发、武器目录会暴露训练专用武器、HUD 只支持 Zeroing、应用服务组合根只注册 P1 服务等兼容问题，且必须保留 P1 行为回归。

## 交付内容

### 契约与文档

- 先修订 BDD 阶段/行为歧义：为 `03-训练模式选择` 的示例行标明 P2/P3；在 `09/10` 补充白天觇孔、100m 射距/40m 路线和无 VR 完整流程的可追溯场景；按实际消耗、实际记录和唯一重训去向修订 `11-移动靶结算`；把 `13-武器库武器墙` 的 P2 Loadout 保存与 P3 场景实际生成拆开；确认 `22-设置界面` 当前全部字段均进入 P2 验收。
- 更新接口 00：P2 新事件及其数据、发布时机、幂等和取消订阅规则。
- 更新接口 01：完整 P2 `UIEventId`、ScreenId、训练暂停叠层、重训和 ReturnTo 路由。
- 更新接口 02：共享 Session、移动靶 Session、武器 Session 和结算的生命周期协调。
- 更新接口 03：跨模式 HUD Provider 约束、移动靶稳定 Key/PromptId、点射节奏和微光镜状态。
- 更新接口 05：路线配置、状态进度、原子点射记录输入、完整错误码、事件和幂等结算。
- 更新接口 06：移动靶 10 发初始化、公开武器目录与训练专用武器区分、两发点射编排边界。
- 更新接口 10：Pending 预览事件、Discard 回滚、保存失败和损坏文件回退语义。
- 更新接口 11：P2 场景绑定和 UI 稳定测试 ID。
- 更新接口 12：18 个 P2 任务与功能级测试门禁映射。
- 如果上述变更改变既有行为，先更新对应 BDD，再更新接口文档和任务规格，最后才能改代码。

### P2 稳定 ID 基线

接口 11 必须发布“语义 → 精确完整 ID → 唯一所有者”表，并至少冻结下列入口；ID 不得依赖本地化显示文本：

| 区域 | 精确 ID 或唯一模式 | 所有者 |
|---|---|---|
| 主入口 | `Button_MainMenu_OpenMovingTarget`、`Button_MainMenu_OpenArmory`、`Button_MainMenu_OpenSettings` | task008 |
| 主菜单档案摘要 | `Panel_MainMenu_Profile`、`Text_MainMenu_ProfileId`、`Text_MainMenu_TrainingLevel`、`Text_MainMenu_RecentGrade` | task007/task017 |
| 移动靶设置 | `Screen_MovingTargetSettings`、`Button_MovingTargetSettings_ModeDay`、`Button_MovingTargetSettings_ModeNight`、`Button_MovingTargetSettings_Day_Speed_3/4/5`、`Button_MovingTargetSettings_Night_Speed_2/2_5/3`、`Button_MovingTargetSettings_Start`、`Button_MovingTargetSettings_Back` | task008 |
| 白天 HUD | `Screen_MovingTargetDayHud`、`Text_MovingTargetDayHud_Countdown/Ammo/FireMode/HitCount/TargetSpeed/Direction/ShootState/EndpointWarning`、`Hud_MovingTargetDay_BurstRhythm/Route` | task009 |
| 夜晚 HUD | `Screen_MovingTargetNightHud`、`Text_MovingTargetNightHud_Countdown/Ammo/FireMode/HitCount/TargetSpeed/Direction/ShootState/EndpointWarning/LowLightSightState`、`Hud_MovingTargetNight_BurstRhythm/Route` | task009 |
| 结算 | `Screen_MovingTargetResults`、`Text_MovingTargetResults_Grade/HitCount/AmmoConsumed/LightMode/TargetSpeed/GradeRule`、`Text_MovingTargetResults_Burst_1` 至 `_5`、`Button_MovingTargetResults_Retry`、`Button_MovingTargetResults_BackToModeSelection` | task010 |
| 武器库 UI | `Screen_Armory`、`Button_Armory_Select_<WeaponId>`、`Panel_Armory_Preview_<WeaponId>`、`Panel_Armory_Equipped_<WeaponId>`、`Panel_Armory_Unavailable_<WeaponId>`、属性文本、`Button_Armory_Equip`、`Button_Armory_Back` | task013 |
| 武器墙场景 | `Armory.WeaponWall.Root`、`Armory.WeaponWall.Display.<WeaponId>`、`Armory.WeaponWall.ModelAnchor.<WeaponId>`、`Armory.WeaponWall.Selection.<WeaponId>`、`Armory.WeaponWall.Equipped.<WeaponId>`、`Armory.WeaponWall.Binding` | task012 |
| 设置与暂停 | task015 列出的 `Screen_Settings`、全部 Button/Slider/Text/Panel ID，以及 `Screen_TrainingPause`、`Button_TrainingPause_OpenSettings`、`Button_TrainingPause_Resume` | task015 |
| 移动靶场 | `MovingTargetRange.PlayerSpawn`、`MovingTargetRange.Target`、`MovingTargetRange.Route.LeftEndpoint`、`MovingTargetRange.Route.RightEndpoint`、`MovingTargetRange.Lighting.Day`、`MovingTargetRange.Lighting.Night`、`MovingTargetRange.Optic.LowLight`、可选 `MovingTargetRange.Optic.LowLight.Camera`、`MovingTargetRange.Hud.Anchor` | task005/task011 |

同一页面中禁止重复 `UITestId`。白天和夜晚的 3m/s 按钮必须使用上述模式限定 ID，不得创建两个同名 `Button_MovingTargetSettings_Speed_3`。

### 测试基础

- 复用既有 Runtime、EditMode、PlayMode 程序集，不创建重复定义的 P2 DTO 或事件总线。
- 可注入的确定性时间源，可快进 3 秒等待、2 秒端点停留和完整路线。
- 固定路线配置和规范化进度替身。
- 可为两发分别指定命中/未命中的射击与目标命中替身。
- 复用 P1 的无 VR 输入、头部/双手姿态和枪线替身。
- 内存设置持久化、损坏数据持久化和保存失败持久化替身。
- 设置运行时应用替身，可观察预览、提交和回滚调用。
- 事件记录器和稳定测试 ID 查找辅助器。
- 测试命名或 `TestCase` 描述必须包含 BDD 文件编号和场景名。

## 不包含

- 不实现移动靶状态机、设置服务、HUD 聚合或结算业务。
- 不制作 P2 UI、场景、武器 Prefab、微光镜视觉或设置运行时效果。
- 不以本任务代替 P1 task014-task016 的实际收口工作。
- 不实现完整玩家档案、历史列表、云存档或网络后端。

## 依赖关系

- 前置依赖：P1 task014、P1 task015、P1 task016 完整收口并有证据。
- 可并行：门禁通过后，task002、task003、task004、task005 可在冻结契约上并行。
- 后续依赖：task002-task018。

## 联调说明

- 与 UI 联调：确认稳定 Key、ScreenId、UIEventId、ReturnTo 路由和全部测试 ID 足够渲染 BDD，不要求 View 计算规则。
- 与 场景联调：确认路线配置、规范化进度、绑定组件和昼夜/微光状态没有场景业务规则。
- 与 功能B 联调：冻结一次两发点射、逐发命中、弹药唯一权威、禁射校验顺序和训练专用武器目录规则。

## 测试要求

- EditMode 契约测试：新增 DTO 默认集合不为 null 或有安全空值策略；枚举新增值有默认处理；事件可订阅、发布和取消订阅。
- PlayMode 基础测试：无 VR 测试输入、稳定时间源、稳定 ID 查找器和测试场景可被后续任务复用。
- P1 准入回归：重新运行 P1 当前全量 EditMode/PlayMode，并保存测试数量、结果和基线；不得仅引用过期结果文件。
- 文档一致性检查：任务清单编号唯一，所有 P2 任务都有 BDD、接口、交付物、测试和联调引用。

## VR 实机验收清单

本任务不新增 VR 表现，但准入记录必须包含：

- P1 主菜单 World Space Canvas 可读，左右手 UI 射线可悬停和点击。
- P1 训练步枪可近距拾取、双手持枪并由右手扳机有效开火。
- 任一时刻只有一个活动 Camera 和一个 AudioListener，HMD/Origin 未被武器逻辑改写。
- 设备型号、OpenXR Runtime、连接方式和验证日期已记录；无法实测的项目必须标为待验收，不能写成通过。

## 验收标准

- P1 task014-task016 完整收口且最新回归通过。
- 13 项接口缺口均已在 BDD、接口文档和任务规格中获得唯一、可实现的答案。
- P2 DTO、服务、事件、错误码、稳定 Key/ID 和测试替身可以被四个负责范围共同引用，无重复定义。
- 后续任务可以只依赖冻结契约并使用假实现并行开发，不再需要彼此读取具体 MonoBehaviour。
- 所有契约和基础测试通过；缺任一硬门禁时本任务不得标记完成。
