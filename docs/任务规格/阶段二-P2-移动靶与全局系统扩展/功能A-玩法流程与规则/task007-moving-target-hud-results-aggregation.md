# task007 跨模式 HUD、移动靶结算与最近评级摘要

## 负责人

功能A-玩法流程与规则

## 目标

把 P1 的 100m HUD 能力扩展为可按训练模式选择 Provider 的聚合服务，为移动靶昼夜 HUD 提供稳定 DTO 和事件，为结算页提供可重复查询的移动靶结果，并实现 BDD 要求的最小最近评级摘要保存。

本任务只聚合已有业务状态，不重新计算移动靶阶段、弹药、逐发命中或评级；P1 100m HUD 和结算行为必须保持兼容。

## 参考资料

- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/BDD/screens/07-射校最终评级.feature.md`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/04-100m射校服务.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/任务规格/阶段二-P2-移动靶与全局系统扩展/功能A-玩法流程与规则/task001-p2-contracts-regression-entry-gate.md`
- `docs/任务规格/阶段二-P2-移动靶与全局系统扩展/功能A-玩法流程与规则/task004-moving-target-core-state-machine.md`

## BDD 场景追溯

| BDD 文件 | 场景 | 本任务责任 |
|---|---|---|
| `09-移动靶白天HUD.feature.md` | `训练开始前等待 3 秒` | 聚合倒计时、禁止射击和目标未移动状态 |
| `09-移动靶白天HUD.feature.md` | `HUD 显示白天训练状态` | 输出 10 发、点射模式、0/10 命中、速度、方向和 CanShoot |
| `09-移动靶白天HUD.feature.md` | `每次点射消耗两发弹` | 订阅统一弹药与点射事件，刷新弹药和 5 段节奏记录 |
| `09-移动靶白天HUD.feature.md` | `命中目标后更新命中数` | 从状态/点射 DTO 刷新命中，不由 HUD 重新判定 |
| `09-移动靶白天HUD.feature.md` | `目标到达左端后禁止射击` | 输出端点停留提示和 CanShoot=false |
| `09-移动靶白天HUD.feature.md` | `目标抵达右侧终点后进入结算` | 消费唯一完成结果并触发一次结算协调 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚 HUD 使用微光镜显示` | 输出 Night HUD 类型和微光镜状态 Key |
| `10-移动靶夜晚HUD.feature.md` | `显示夜晚移动状态` | 输出夜间速度、方向和端点标识所需数据 |
| `10-移动靶夜晚HUD.feature.md` | `端点停留期间禁止射击` | 端点提示、弹药和命中保持一致 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚模式命中统计` | 点射记录和命中事件驱动 HUD 刷新 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚训练结束` | 结算结果保留 Night 与选择速度 |
| `11-移动靶结算.feature.md` | `根据命中发数显示评级` | 原样传递 task004 的 ResultGrade，不在聚合层计算 |
| `11-移动靶结算.feature.md` | `显示训练摘要` | 提供命中、消耗弹药、昼夜、速度和评级规则所需 DTO |
| `11-移动靶结算.feature.md` | `显示 5 次点射记录` | 提供 0-5 条真实记录及五槽展示状态；未使用槽位不伪造业务记录 |
| `11-移动靶结算.feature.md` | `重新训练` | 清理本次结果查询上下文，由路由返回设置页；不残留旧 HUD |
| `11-移动靶结算.feature.md` | `返回模式选择` | 保存最小最近评级摘要，成功或失败结果可被 Presenter 处理 |
| `05-100m射击HUD.feature.md` | `HUD 显示当前训练状态` | P1 Provider 回归，不改变现有稳定 Key 和事件 |
| `07-射校最终评级.feature.md` | `评级来自服务层` | 跨模式聚合不得使 P1 UI 重新计算评级 |

## 交付内容

### 跨模式 HUD Provider

- 保留 `IHUDService.GetHud(string sessionId)` 和 `HudUpdated` 对外契约，内部改为按 `TrainingMode` 选择 Provider 或等效可扩展结构。
- 提供 100m Provider 适配，复用现有 `ZeroingHudService` 逻辑和 P1 稳定 Key；禁止复制一套分叉的 P1 规则。
- 提供移动靶 Provider，依赖：
  - `ITrainingSessionService`
  - `IMovingTargetService`
  - `IAmmoService`
  - task006 冻结的点射/武器状态查询
  - task001 冻结的日间觇孔/夜间微光镜状态查询
- 组合根注册 Provider 时不得让多个 Provider 同时处理同一 Session；未知模式返回明确错误。
- `GetHud` 不允许 UI 传空 SessionId 后猜测错误训练；若接口允许空值代表当前 Session，必须由 task001 文档和测试固定唯一语义。

### 移动靶 HUD 数据

- `HudType`：白天 `MovingTargetDay`，夜晚 `MovingTargetNight`。
- `Ammo`：来自 `IAmmoService` 的同一快照；初始为本模式规定的 10 发，不从移动靶文本反推。
- `CanShoot`：同时满足移动靶规则门禁与武器有效状态；HUD 只显示最终聚合结果。
- `TextLines`/`Prompts` 使用 task001 冻结的稳定 Key/PromptId，至少包括：
  - `countdown`
  - `ammo`
  - `fireMode`
  - `hits`
  - `speed`
  - `direction`
  - `shootState`
  - `burstRhythm`
  - `endpointHold`
  - `optic`
- `hits` 以 `当前命中/10` 表示；字符串由 DTO 提供，UI 不自行拼业务上限。
- `burstRhythm` 能稳定表达第 1 至第 5 次点射的未使用、未命中、部分命中或完全命中状态；如自由文本不足，应使用 task001 冻结的结构化 DTO。
- 等待与端点停留分别提供可区分的 PromptId；不得都退化为无法测试来源的“禁止射击”。
- 夜间微光镜状态来自 task006/场景绑定状态，聚合层不直接开关后处理。

### 刷新与事件

- 订阅且仅在相关 Session 上响应：
  - `SessionStartedEvent`
  - `AmmoChangedEvent`
  - `WeaponStateChangedEvent`
  - `MovingTargetPhaseChangedEvent`
  - `MovingTargetBurstRecordedEvent`
  - 微光镜状态事件
  - `MovingTargetCompletedEvent`
- 每次弹药、命中、阶段、方向、禁射、点射节奏或微光镜状态变化后发布 `HudUpdatedEvent`。
- 高频路线进度可按接口 03 建议限频到 10-20Hz；阶段边界、点射和完成事件必须立即刷新。
- 服务不得要求 UI 每帧轮询全部业务服务。

### 结算查询与最近评级摘要

- 提供可重复查询的移动靶最终结果，数据直接来自 task004 的 `MovingTargetResultDto`。
- 完成结果至少包含 SessionId、LightMode、Speed、实际 `TotalAmmoConsumed`、HitCount、Grade、结构化评级规则展示 DTO/Key 和 0-5 条有序 `BurstRecords`。
- 为结算 Presenter 提供固定五槽的只读展示状态：真实记录按序映射，其余槽位明确为“未使用”；展示状态不得写回或扩充 `BurstRecords`。
- 结果查询在路由进入结算页后仍有效；开始重训或明确清理 Session 时才清除旧上下文。
- 实现 task001 冻结的最小 `ITrainingResultSummaryStore` 或等效接口，只保存最近一次评级摘要：SessionId、TrainingMode、Grade、完成时间以及供模式选择/档案摘要使用的最小字段。
- 提供 `PlayerProfileSummaryDto`/查询接口，把本地档案标识、训练等级和最近评级交给通用状态面板；不返回完整历史、账号或远程数据。
- 返回模式选择时保存摘要；持久化失败返回 `PersistenceFailed`，不得把失败伪装成成功。
- 最近评级摘要不是完整历史列表，不保存逐发回放、玩家账号或远程数据。

## 错误处理

| 条件 | 错误码 |
|---|---|
| Session 或结果不存在 | `NotFound` |
| Provider 不支持当前模式、SessionId 与当前训练不一致 | `InvalidInput` |
| 训练未完成时查询最终结果、重训清理时仍请求旧 HUD | `InvalidState` |
| HUD 依赖尚未注册或场景状态缺失 | `ResourceUnavailable` |
| 最近评级摘要保存失败 | `PersistenceFailed` |

错误结果必须提供安全空 DTO 或明确的 UI 处理策略，不能用写死的“0/10”“10发”伪装成有效业务状态。

## 不包含

- 不计算移动靶路线、CanShoot 原始规则、逐发命中、弹药消耗或 ResultGrade。
- 不实现 HUD View、TMP 文本、点射节奏条视觉、结算页面或按钮路由。
- 不直接移动目标、切换灯光、启用微光镜或读取 XR 输入。
- 不实现武器开火、两发点射、后坐力、弹道或命中检测。
- 不实现完整玩家档案、历史记录浏览、排行榜、回放或网络存储。

## 依赖关系

- 前置依赖：task003、task004、task006；三者均依赖 task001 冻结契约。
- 后续依赖：task008、task009、task010、task016、task017。

## 联调说明

- 与 功能B 联调：AmmoDto、两发点射结果和微光镜状态必须带同一 SessionId；聚合层不根据场景对象激活状态猜测。
- 与 UI 联调：Presenter 按稳定 Key/结构字段绑定；UI 不计算命中、弹药、方向、CanShoot 或评级。
- 与路由联调：完成事件只触发一次结算；重训明确返回移动靶设置页并清除旧 Session/HUD 上下文。
- 与 P1 联调：组合根仍对 Zeroing Session 返回原 P1 HUD；新增 Provider 不改变原事件订阅顺序和字段。

## 测试要求

- EditMode 单元测试：
  - 根据 Session Mode 选择 Zeroing/MovingTarget Provider，未知模式返回正确错误。
  - 白天初始 HUD：10 发、点射模式、0/10、选择速度、右到左、等待禁射。
  - 夜晚 HUD：Night HudType、夜间速度、微光镜状态和可读提示。
  - 弹药事件使 Ammo 和 `ammo` Key 同步更新。
  - 一次点射事件只新增一个节奏记录，命中 0/1/2 分别正确。
  - 端点状态显示专用提示，Ammo/HitCount 不变化。
  - 状态、弹药、点射、微光事件只刷新匹配 Session。
  - 完成事件产生一次最终结果和一次结算协调，重复事件幂等。
  - 最终结果分别覆盖 0、部分和 5 次点射，保留实际 0-5 条有序记录、实际弹药消耗、时间点、昼夜、速度和 task004 评级。
  - 五槽展示状态把缺少的记录标为“未使用”，不生成伪 `BurstRecordDto`。
  - 评级规则展示 DTO/Key 来自服务；Presenter 无需复制 3/4/5 阈值。
  - 最近评级摘要保存成功、失败和重复保存的行为。
  - 结果完成前查询、重训清理后查询和未知 Session 错误。
- P1 回归测试：
  - P1 `round`、`distance`、`ammo`、`stability`、`impactRecord`、`shoulder`、`shootState` Key 不变。
  - P1 Ammo、肩侧、稳定度、弹着和轮次事件仍刷新同一个 `IHUDService`。
  - P1 最终评级仍来自 `ZeroingResultDto`。
- 所有测试使用事件总线、手动时间、内存摘要存储和服务假实现，不加载 UI Prefab。

## 无 VR PlayMode 联调要求

- task009/task010 接入后，使用同一个无 VR 输入替身驱动点射并观察 HUD/结算刷新。
- 测试通过稳定 ID 找到全部必填 HUD 字段；不能从 View 文本反推结果 DTO。
- 在 P1 和 P2 Session 间切换时，聚合服务返回正确 Provider，不残留上一模式数据。

## VR 实机验收清单

本任务不负责视觉，但需向 task009/task011/task016 提供以下可验证状态：

- 日夜 HUD 显示值与服务查询一致。
- 端点提示、点射节奏、弹药和命中在射击后及时刷新。
- 夜晚微光镜状态有独立字段，HUD 文本不依赖后处理颜色判断。
- 100m HUD 在加入 P2 Provider 后仍可读且行为不变。

## 验收标准

- 一个对外 `IHUDService` 可以按 Session 正确服务 100m 和移动靶，且没有 UI 业务计算。
- 移动靶 HUD 的所有必填字段和事件均可追溯到 BDD09/10。
- 移动靶结算与 5 条点射记录可追溯到 BDD11，评级原样来自 task004。
- 最近评级摘要满足 BDD11“返回模式选择”，但未扩大为完整历史系统。
- EditMode 与 P1 HUD 回归测试全部通过。
