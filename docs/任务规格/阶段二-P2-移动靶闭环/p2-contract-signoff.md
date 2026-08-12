# P2 契约签核清单

## 状态

- 产品口径：已按 2026-08-12 最新需求更新。
- 技术审核：按 `阶段审核清单.md` 的审核节点 A 核验，不占 task 编号。
- 本文件不是功能完成证明。

## 产品冻结项

- [x] P1/P2 都是卧姿固定射击。
- [x] 进入场景即在射击点，不提供连续移动、传送、攀爬或无 VR 平移。
- [x] 真实 HMD/双手房间尺度跟踪保留。
- [x] 准备 UI 和枪位于正前方的独立锚点。
- [x] Start 后等待拾枪；有效拾枪隐藏大型 UI，P1 进入实射、P2 进入倒计时。
- [x] P1 每轮结束显示分析，NextRound 后大型 UI 保持隐藏；放枪时仅以最小 HUD 提示，最终显示评级。
- [x] P2 结束显示结算；Retry 返回设置。
- [x] 实弹阶段仅保留不遮挡瞄准线的最小只读 HUD。
- [x] P2 只做单一日间移动靶，不做昼夜/武器库/设置/人工行走。

## 移动靶冻结项

| 项目 | 决定 |
|---|---|
| 距离/路线 | 100m 射击距离；水平 40m，右端起止、左端折返 |
| 速度 | 3/4/5 m/s，默认 4 m/s |
| 阶段 | 等待拾枪 → 3 秒倒计时 → 右到左 → 左端 2 秒禁射 → 左到右 → 结算 |
| 射击模式 | 快速扣动一次固定两发；第二发后仍保持 Trigger 则按配置间隔继续逐发连射 |
| 弹药 | `IAmmoService/AmmoDto` 唯一权威，P2 可用弹药 10 发 |
| 评级 | 0-2 不及格、3 及格、4 良好、5-10 优秀 |
| 停止 | Trigger 释放、无弹、禁射、训练完成或武器/跟踪失效立即停止；禁射恢复后必须释放再扣动 |
| 记录 | 按每次扣动保存 `FireSequenceRecordDto`，序列内保存逐发 ShotId、时间和命中；一个序列可含 2-10 发 |
| 结束 | 到达右端自动结束并终止当前射击序列 |

## 三条实现轨道接口

### UI 输入

- `TrainingPresentationDto`
- `HudDto`
- `MovingTargetSettingsDto`
- `MovingTargetResultDto`
- 命令结果与 Busy/Error 状态

### 场景输入/输出

- `TrainingRangeSceneBindings` 的七个独立锚点。
- 标准化目标视觉输入：Phase、RouteProgress01、LegProgress01、Direction。
- 稳定 TargetId、HitSurface 和场景加载/卸载生命周期。

### 玩法输入/输出

- 假/真实 `IXRTrainingInput`、武器持有事件、时钟、命中快照。
- Session、Presentation、HUD、Trigger/连射序列、逐发记录和结果 DTO/事件。
- 不含 Unity Scene/Transform/Canvas 类型。

## 审核节点 A 技术检查（完成时填写）

- [ ] BDD、接口和任务规格无矛盾。
- [ ] P1/P2 稳定 UI/场景测试 ID 已冻结。
- [ ] UI DTO Fixture 可独立运行。
- [ ] 场景 Visual Driver Fixture 可独立运行。
- [ ] 玩法 Input/Clock/Hit Fixture 可独立运行。
- [ ] P1 基线回归过滤器已记录。
- [ ] 误拼写场景迁移策略已确认。
- [ ] Trigger `Pressed/Held/Released`、连射间隔、停止原因和 P1 单发回归口径已冻结。
- [ ] 三位实现负责人确认无需引用彼此具体实现即可开工。
- 证据路径：待填写。
