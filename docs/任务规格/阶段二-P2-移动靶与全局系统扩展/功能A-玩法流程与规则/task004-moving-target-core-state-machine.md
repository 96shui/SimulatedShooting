# task004 移动靶核心状态机、路线与评级

## 负责人

功能A-玩法流程与规则

## 目标

实现可在 EditMode 独立运行的移动目标训练核心：昼夜速度校验、3 秒等待、右到左移动、左端 2 秒停留、左到右返回、端点禁射、最多 5 次两发点射记录、逐发命中统计、自动完成和命中评级。

状态机只负责规则、确定性时间与路线进度，不直接移动 Unity Transform，不读取 XR/键鼠输入，不生成后坐力、弹道、VFX 或音效，也不维护独立于弹药服务的第二套可变弹药库存。

## 参考资料

- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `docs/任务规格/阶段二-P2-移动靶与全局系统扩展/功能A-玩法流程与规则/task001-p2-contracts-regression-entry-gate.md`

## BDD 场景追溯

| BDD 文件 | 场景 | 本任务责任 |
|---|---|---|
| `08-移动靶设置.feature.md` | `默认选择白天模式` | 返回白天 3/4/5m/s 及配置默认速度 |
| `08-移动靶设置.feature.md` | `切换到夜晚模式` | 返回夜晚 2/2.5/3m/s，并拒绝跨模式速度 |
| `08-移动靶设置.feature.md` | `选择目标速度` | 将合法昼夜/速度组合冻结进 Session |
| `08-移动靶设置.feature.md` | `开始训练` | 创建唯一移动靶 Session 并进入 3 秒等待 |
| `08-移动靶设置.feature.md` | `返回上一级` | 未开始时不得创建移动靶 Session；由路由任务验证 |
| `09-移动靶白天HUD.feature.md` | `训练开始前等待 3 秒` | 倒计时期间目标进度不变、CanShoot=false |
| `09-移动靶白天HUD.feature.md` | `HUD 显示白天训练状态` | 提供速度、方向、阶段、命中和可射击权威状态 |
| `09-移动靶白天HUD.feature.md` | `每次点射消耗两发弹` | 只接受 task001 定义的完整两发有效点射记录，最多 5 次 |
| `09-移动靶白天HUD.feature.md` | `命中目标后更新命中数` | 汇总每次点射中 0-2 发命中 |
| `09-移动靶白天HUD.feature.md` | `目标到达左端后禁止射击` | 左端停留整段 CanShoot=false，拒绝新增记录 |
| `09-移动靶白天HUD.feature.md` | `目标抵达右侧终点后进入结算` | 路线完成后状态进入 Completed 并产生唯一结果 |
| `10-移动靶夜晚HUD.feature.md` | `显示夜晚移动状态` | 夜间速度、方向和路线进度规则与白天一致 |
| `10-移动靶夜晚HUD.feature.md` | `端点停留期间禁止射击` | 禁射拒绝不改变点射数或命中数 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚模式命中统计` | 逐发命中与点射记录规则不因夜间视觉改变 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚训练结束` | 结果保留 Night 和选择速度 |
| `11-移动靶结算.feature.md` | `根据命中发数显示评级` | 0-2 不及格、3 及格、4 良好、5 及以上优秀 |
| `11-移动靶结算.feature.md` | `显示训练摘要` | 按 task001 修订口径返回命中数、实际消耗 0-10 发、昼夜和速度；完整五次点射时为 10 发 |
| `11-移动靶结算.feature.md` | `显示 5 次点射记录` | 结果只保留实际发生的 0-5 条有序记录；UI 的其余槽位显示“未使用”，不得伪造时间或位置 |

## 交付内容

- 实现 task001 和接口 05 冻结后的契约，至少包括：
  - `MovingTargetLightMode`
  - `TargetMovePhase`
  - `MovingTargetSettingsDto`
  - `MovingTargetRouteConfigDto` 或等效不可变路线配置
  - `MovingTargetSessionDto`
  - 原子两发点射记录输入 DTO
  - `BurstRecordDto`
  - `MovingTargetResultDto`
  - `IMovingTargetService`
- 路线配置至少具有：
  - 稳定 `RouteId`。
  - 左右端点间有效路线长度，必须大于 0 且为有限数。
  - 默认等待 3 秒和左端停留 2 秒；若支持配置，P2 生产配置必须仍使用该值。
  - 明确右端与左端的规范化进度约定，建议右端 `1`、左端 `0`。
- Session 状态至少提供：
  - SessionId、LightMode、SpeedMetersPerSecond、Phase。
  - 倒计时/当前阶段剩余或已用时间。
  - `RouteProgress01` 或 task001 冻结的等效确定性进度。
  - BurstCountUsed、HitCount、CanShoot 和可本地化的方向枚举/稳定 Key。
  - 来自统一弹药快照的只读显示数据；不得与 `IAmmoService` 独立增减。
- 白天合法速度只允许 3、4、5m/s；默认 4m/s，除非权威项目配置明确覆盖。
- 夜晚合法速度只允许 2、2.5、3m/s；夜间默认值由 task001 冻结，测试不得依赖 UI 文本猜测。
- `StartSession` 与 `ITrainingSessionService` 共用唯一 SessionId，并按冻结生命周期创建/开始共享 Session；重复开始不得创建两个活动 Session。
- 状态机顺序固定为：
  1. `WaitingCountdown`：3 秒，目标停在右端，`CanShoot=false`。
  2. `MovingRightToLeft`：进度从右到左，`CanShoot=true`。
  3. `LeftEndpointHold`：2 秒，进度固定在左端，`CanShoot=false`。
  4. `MovingLeftToRight`：进度从左到右，`CanShoot=true`。
  5. `Completed`：进度固定在右端，`CanShoot=false`。
- 移动耗时由路线长度除以选择速度得到；服务不得依赖实际帧率。
- `Tick` 必须处理一个大 `deltaTime` 跨越多个阶段，且每段剩余时间正确结转。
- 零 `deltaTime` 幂等；负值、NaN、Infinity 返回 `InvalidInput`，状态不变。
- 状态机只接受由 task006 点射协调服务确认的完整两发点射结果；每次记录包含 0-2 发命中，累计命中范围为 0-10。
- 等待、左端停留、完成状态或超过 5 次时拒绝记录，且不改变 BurstCount、HitCount 或结果。
- 抵达最终右端时生成唯一、可重复查询的 `MovingTargetResultDto`；结果使用统一弹药快照计算实际消耗，并只包含实际发生的 0-5 条点射记录；重复完成/查询不得重复发布 SessionEnded 或重复保存摘要。
- 评级规则：0-2 `Fail`、3 `Pass`、4 `Good`、5-10 `Excellent`。

## 事件

- Session 创建/开始继续使用共享 `SessionStartedEvent`。
- 阶段或路线进度变化发布 task001 冻结的移动靶状态事件；高频进度可限频，但最终边界状态不得丢失。
- 每次有效点射记录成功后发布 `MovingTargetBurstRecordedEvent`。
- 阶段切换发布 `MovingTargetPhaseChangedEvent` 或等效事件，包含前后阶段和状态快照。
- 完成时发布一次 `MovingTargetCompletedEvent`，并协调共享 `SessionEndedEvent`。
- 无效命令不得发布伪成功状态、点射或完成事件。

## 错误处理

| 条件 | 错误码 |
|---|---|
| 昼夜模式与速度不匹配、路线非法、时间步非法、点射输入不是恰好两发 | `InvalidInput` |
| Session 不存在 | `NotFound` |
| 当前存在不可替换的活动 Session、等待/端点/完成时记录点射、超过 5 次、路线未完成请求结果 | `InvalidState` |
| 场景路线配置尚未提供或共享依赖未准备 | `ResourceUnavailable` |

## 不包含

- 不调用 `Transform.Translate`、Animator、Timeline、Collider 或后处理。
- 不读取 `IXRTrainingInput`、Input System 或具体控制器。
- 不实现两发武器触发、弹药扣除、射线命中、后坐力、触觉、弹道、枪口焰或微光镜视觉。
- 不实现 HUD Presenter、结算 UI、场景跳转或武器库。
- 不保存最近评级摘要；由 task007 负责。

## 依赖关系

- 前置依赖：task001。
- 可并行：task002、task003、task005；本任务使用 task001 冻结的弹药/点射端口和假实现测试，不等待具体 MonoBehaviour。
- 后续依赖：task006、task007、task008、task011、task016。

## 联调说明

- 与 场景 联调：场景只提供路线配置并把 `RouteProgress01` 映射到左右端点；场景不得反向决定阶段或 CanShoot。
- 与 功能B 联调：task006 必须先查询本服务禁射状态，再执行原子两发点射；成功后把完整点射结果提交本服务。
- 与 HUD 联调：task007 订阅状态和点射事件，不能每帧自行推算方向、进度或评级。
- 与 Session 联调：共享 Session 和移动靶 Session 使用同一个不可变 SessionId；完成和取消路径各自只能发布一次终态。

## 测试要求

- EditMode 单元测试，测试名包含 BDD 编号和场景名：
  - 白天/夜晚各合法速度列表及跨模式速度拒绝。
  - 默认白天 4m/s。
  - 固定路线和时间源下，3 秒前目标进度不变且不可射击。
  - 3 秒边界准确进入右到左移动。
  - 按路线长度/速度准确抵达左端并进入 2 秒禁射。
  - 左端 2 秒边界准确进入左到右移动。
  - 返回右端进入 Completed，进度为右端且不可射击。
  - 单个大 delta 跨等待、移动和停留的结转结果正确。
  - 负值、NaN、Infinity Tick 无状态变化。
  - 等待和端点状态记录点射返回 `InvalidState`，点射数/命中数不变。
  - 每次只接受两发，分别覆盖 0、1、2 发命中累计。
  - 第 6 次点射被拒绝。
  - 0、2、3、4、5、8、10 命中的评级正确。
  - 分别覆盖 0 次、部分点射和 5 次点射结束；结果保留昼夜、速度、实际 0-10 发消耗及 0-5 条真实记录，时间/进度一致。
  - 完成与结果查询幂等，完成事件只发布一次。
  - Session 不存在、重复开始、取消后继续 Tick 的错误分支。
- 测试使用手动时间源、固定路线和点射输入替身，无需加载 3D 场景。

## 无 VR PlayMode 联调要求

- task005/task006 接入后，使用无 VR 输入替身快进完整路线，场景目标位置与服务进度一致。
- 等待和左端停留时尝试点射，必须在弹药/VFX/命中前被拒绝。
- 返回右端后只触发一次结算跳转。

## VR 实机验收清单

本任务为纯规则服务；以下交由 task011/task016 实机验证：

- 3 秒等待、右到左、左端 2 秒、左到右的体感顺序与服务记录一致。
- 端点停留时扳机不消耗弹药且无真实射击反馈。
- 日夜速度和方向提示与目标实际运动一致。
- VR 帧率波动不会改变路线耗时或产生重复结算。

## 验收标准

- 所有移动规则可在 EditMode 独立、确定性测试。
- BDD08-11 中属于状态机和评级的场景均有精确测试追溯。
- 场景无需自行判断阶段，UI 无需计算评级，功能B 无需维护第二套移动靶规则。
- 无效点射不改变状态；完成结果和事件幂等。
- EditMode 测试全部通过，且 P1 全量测试不因新增枚举或 Session 模式回归。
