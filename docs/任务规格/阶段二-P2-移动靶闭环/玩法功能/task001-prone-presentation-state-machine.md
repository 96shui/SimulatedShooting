# task001：P1/P2 共用卧姿、禁移动与界面显隐状态机

## 负责人

玩法功能。

## 目标

实现 P1 与 P2 共用的纯 C# 展示/训练阶段状态机和 Locomotion 策略，使“进入模式—确认开始—等待取枪—隐藏大型 UI—实射—轮次复盘/结算”具有唯一权威来源，并把 P1 从旧站姿/常驻界面逻辑迁移到固定卧姿规则。

## 前置条件

需求、BDD、接口契约和阶段审核清单已经确认。不得依赖 task002、task003 或任何真实 UI/场景实现。

## 必读资料

- `docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/BDD/screens/06-弹着分析.feature.md`
- `docs/BDD/screens/07-射校最终评级.feature.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶HUD.feature.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md`

## 实现内容

### 1. 共用展示状态

- 实现并序列化 `TrainingPresentationPhase`、`TrainingPostureMode` 和 `TrainingPresentationDto`。
- 实现 `ITrainingPresentationService` 的命令、查询与事件；状态变更必须发布完整快照。
- `LargeUiVisible`、`MinimalHudVisible`、`ShootingAllowed` 必须由阶段推导，不能由 UI 自行组合布尔值。
- 非法命令返回稳定错误码且不改变状态；重复事件必须幂等。

### 2. 固定卧姿与移动策略

- P1、P2 创建 Session 时固定 `PostureMode = ProneFixed`、稳定 `FiringStationId`、`ArtificialLocomotionAllowed = false`。
- 输出 `TrainingLocomotionPolicyDto`：连续移动、传送、人工转向均为禁用，物理头手追踪为允许。
- 玩法层只输出策略，不访问或关闭 XR 组件。

### 3. 统一入口和取枪时序

- 进入 P1/P2：阶段为模式入口/等待开始，大型 UI 可见，禁止射击。
- `StartTraining`：创建/更新 Session，阶段进入 `AwaitingWeaponPickup`；UI 仍可见。
- 只接受当前 Session、当前训练枪、有效后握把拿起的 `TrainingWeaponPickupEvent`。
- 有效取枪后：P1 进入实射；P2 进入倒计时；大型 UI 隐藏。
- 未点击开始、拿错枪、Session 不匹配或重复拿起都不得开始实射。

### 4. P1 轮次和结算改造

- 每轮第三发完成后进入 `RoundReview`，大型分析 UI 自动可见并禁射。
- `NextRound` 成功后无条件隐藏大型 UI并进入下一轮；若枪已释放，仅在最小 HUD 提示重新取枪并保持禁射，重新有效持枪后恢复射击。
- 最后一轮完成进入 `Results`，评级 UI 可见并禁射。
- 退出、重试和切换模式必须释放旧订阅和旧 Session 状态。

### 5. P2 对接点

- 暴露 P2 在有效取枪后进入 3 秒倒计时的稳定转换点。
- P2 训练完成时统一进入 `Results`；重试行为按接口文档回到设置或等待取枪状态。
- 此任务不实现移动靶运动、计分、两发起射或长按连射。

## 交付物

- 展示状态、卧姿模式、移动策略 DTO 和服务实现。
- Session 创建/恢复/退出时的固定卧姿字段处理。
- P1 轮次复盘、下一轮、最终评级的状态迁移改造。
- 供 UI 订阅的完整快照事件；供组合层应用策略的查询接口。
- EditMode 单元测试和无 VR 输入替身说明。

## 测试要求

- 参数化覆盖 P1/P2 的全部合法状态转换和非法转换。
- 验证开始前取枪不隐藏 UI；开始后有效取枪只触发一次隐藏。
- 验证 P1 第三发显示分析、下一轮隐藏、最终轮显示评级。
- 验证 NextRound 在“仍持枪 / 已放枪”两种情况下大型 UI 都隐藏；后者只显示最小取枪提示且保持禁射。
- 验证 P2 有效取枪进入倒计时而不是直接实射。
- 验证所有 P1/P2 Session 的人工移动策略恒为禁用，物理追踪恒为允许。
- 验证退出/重试后旧 Session 的取枪事件不能污染新 Session。

## 验收标准

- 相关 BDD 场景均可用纯 C# 测试重放。
- UI 只需渲染 DTO，场景只需应用移动策略；两者均不需要了解内部状态机。
- 核心程序集不引用 `UnityEngine.SceneManagement`、UGUI、XR Interaction Toolkit、Prefab 或具体场景类。
- P1 原有射击次数和评级规则未被改变。

## 联调契约（非实现依赖）

- UI 消费 `TrainingPresentationDto` 并发送开始、下一轮、重试命令。
- 场景组合适配器消费 `TrainingLocomotionPolicyDto`，但场景负责人可先用常量 Fake 开发。
- 武器适配器只发布 `TrainingWeaponPickupEvent`，玩法不持有武器组件引用。
