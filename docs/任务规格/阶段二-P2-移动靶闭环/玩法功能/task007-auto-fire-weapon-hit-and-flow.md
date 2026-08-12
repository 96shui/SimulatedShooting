# task007：两发起射连射、武器弹药、命中与应用流程

## 负责人

玩法功能。

## 目标

在不破坏 P1 单发射校的前提下，实现 P2“快速扣动固定两发、持续按住继续连射”的完整武器规则、逐发弹药与命中处理，并把 task001 的展示状态和 task004 的移动靶核心串成完整玩法流程。

## 前置条件

仅依赖玩法同线 task001、task004。不得依赖真实 UI、场景、XR Rig 或武器 Prefab。

## 必读资料

- `docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/BDD/screens/09-移动靶HUD.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/04-100m射校服务.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md`

## 实现内容

### 1. Trigger 状态输入

- 扩展 `IXRTrainingInput` 或等效适配接口，统一提供 Trigger 模拟值、`Pressed`、`Held`、`Released`。
- XR、XR Device Simulator、无 VR 调试和自动测试使用同一状态语义；玩法服务不读取厂商 API。
- 迟滞阈值集中配置，保持高位只产生一次 Pressed，降到释放阈值后才重新武装。
- 取枪、双手跟踪和枪线仍经现有抽象输入进入，连射服务不持有 Interactor、Transform 或 Collider。

### 2. 快速扣动固定两发

- 一次合法 `TriggerPressed` 创建唯一 `SequenceId`，并原子预留前两发弹药。
- 快速释放 Trigger 也必须按配置间隔完成已接受的两发；不能退化成单发。
- 新序列开始时弹药不足 2 发，整次拒绝：0 发射、0 扣弹、0 命中记录。
- 前两发分别使用各自射击时刻的最新枪线快照，分别产生后坐力、弹道、命中和 HUD 更新。
- 重复 SequenceId、重复 Tick 或回调重入必须幂等，不能多扣弹或多发射。

### 3. 长按持续连射

- 第二发完成时如果 Trigger 仍为 Held，则进入 `ContinuousFire`。
- 持续段按同一可配置 `ShotIntervalSeconds` 逐发调度，每发独立校验并消耗 1 发。
- 释放 Trigger、弹药耗尽、进入左端禁射、训练完成、退出 Session、武器失效或双手跟踪无效时立即停止后续调度。
- 因禁射/武器失效停止后设置 `TriggerArmedForNewSequence=false`；即使玩家继续长按，也不能在条件恢复时自动续射。必须先收到 Released，再收到新的 Pressed。
- 连射计时使用注入时钟或显式 Tick，不使用帧计数、Coroutine 实时时钟或 UI 动画事件作为规则权威。

### 4. 弹药事务与逐发事件

- 使用 `ReserveAmmo/ConsumeReservedAmmo` 或等效事务保证起射两发的完整性。
- 起射预留只保护前两发；持续段不一次性预扣剩余弹匣。
- 每一发有效射击发布唯一 ShotId 的 `WeaponShotResultEvent`，随后刷新弹药、移动靶统计和 HUD。
- 序列开始、进入持续段、停止都发布 `WeaponFireSequenceChangedEvent`；结果记录停止原因。
- 退出、重试和场景切换必须取消待执行调度并正确释放未消费预留。

### 5. P1 回归隔离

- P1 `FireMode=SingleShot`，一次 Pressed 只产生 1 发，保持 Trigger 不继续射击。
- 武器模式由 Session/服务配置决定，UI、场景和武器 Prefab 不自行判断 P1/P2。
- 共用输入、弹药、后坐力和事件改动必须补 P1 单发、三发一轮和最终评级回归。

### 6. 命中与应用流程

- 场景/物理层只提交标准化命中快照；每个 ShotId 独立验证、记录和计分。
- 结算保留射击序列以及序列内的逐发记录，不能只保存一条合计文本。
- 组合开始、取枪、倒计时、开火、禁射停火、HUD 更新、完成、结果、重试和退出。
- 不从 Collider 名称、UI 文本、枪口焰数量或 Transform 位置推断训练模式、弹药或评级。

## 交付物

- Trigger 按下/保持/释放输入契约和四种输入路径适配。
- `IWeaponAutomaticFireService`、可注入连射配置/时钟、`WeaponFireSequencePhase` 和 `WeaponFireStopReason` 实现。
- 起射两发弹药预留事务、持续段逐发扣弹和幂等记录。
- P1 单发 / P2 两发起射连射模式策略。
- 逐发命中、HUD/结果聚合接入和 P1/P2 应用流程协调器。
- EditMode 测试、无 VR PlayMode 测试和接入说明。

## 测试要求

- 快速 Pressed→Released：恰好 2 发、2 次逐发扣弹、同一 SequenceId、两个不同 ShotId。
- Held 超过第二发：按配置间隔持续发射；释放后不再产生新 Shot。
- 10 发弹药长按到底：恰好 10 发，不出现负弹药或第 11 发。
- 新序列只剩 1 发时整次拒绝；不能射出部分起射。
- 连射进入左端禁射立即停止；离开禁射但未释放 Trigger 时不能自动恢复。
- 武器失效、跟踪丢失、训练完成、重试、退出时取消后续调度并记录正确停止原因。
- 大步长 Tick 能补齐到期射击但不能超出弹药/阶段边界；固定时间线重复运行结果一致。
- 重复 SequenceId、ShotId、Tick 和延迟回调均不会重复耗弹或计分。
- P1 一次 Pressed 仍只产生 1 发，Held 不连续射击；三发一轮与评级回归通过。
- 每发的后坐力、弹道、命中、弹药和 HUD 使用同一个 ShotId/快照。

## 验收标准

- 纯 Fake 输入、Fake 枪线、Fake 命中和固定时钟可完整跑通快速两发与长按连射。
- 核心服务没有 XR、Scene、Prefab、Canvas、Animator、Coroutine 或具体 Collider 依赖。
- UI 不参与连射计时、开火许可、扣弹、命中、评级或完成条件计算。
- 场景只把输入和枪线转换为契约数据，并消费逐发视觉事件，不持有连射规则。
- P1 单发行为没有被 P2 改造破坏。

## 联调契约（非实现依赖）

- UI 消费 `HudDto.FireSequence`、弹药和逐发结果，可使用固定 Fake 序列独立开发。
- 场景/XR 适配器发布 Trigger 状态和最新枪线快照，并按 ShotId 播放逐发反馈，可用 Fake 服务独立开发。
- 三条真实实现仅在阶段集成节点通过 Composition Root 组装，不在本任务建立跨层引用。
