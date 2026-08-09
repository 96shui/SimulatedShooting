# task002 设置服务、Pending 语义与本地持久化

## 负责人

功能A-玩法流程与规则

## 目标

实现完整的本地设置服务，统一已保存设置、Pending 临时设置、恢复默认、实时预览通知、应用持久化和返回不保存的语义，使 UI 与 Unity 运行时适配只消费 DTO 和事件，不直接读写本地文件。

本任务按 `22-设置界面.feature.md` 和接口 10 的完整字段交付，不把阶段需求中的简化列表解释为可以省略 VR 舒适度、亮度、瞄准辅助或语言。

## 参考资料

- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/screens/22-设置界面.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/10-设置服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `docs/任务规格/阶段二-P2-移动靶与全局系统扩展/功能A-玩法流程与规则/task001-p2-contracts-regression-entry-gate.md`

## BDD 场景追溯

| BDD 文件 | 场景 | 本任务责任 |
|---|---|---|
| `22-设置界面.feature.md` | `显示当前设置` | 从持久化加载全部字段，并建立一致的已保存/Pending 快照 |
| `22-设置界面.feature.md` | `修改二选一或多选设置` | 校验枚举并更新 Pending，发布预览事件 |
| `22-设置界面.feature.md` | `修改滑条设置` | 校验 0-100 范围并更新 Pending，发布预览事件 |
| `22-设置界面.feature.md` | `应用设置` | 原子保存 Pending，成功后更新已保存值并发布正式变更事件 |
| `22-设置界面.feature.md` | `恢复默认设置` | 只重置 Pending，不立即持久化，并发布默认预览 |
| `22-设置界面.feature.md` | `返回不保存临时修改` | 丢弃 Pending，恢复为最近一次成功保存值并发布回滚预览 |
| `22-设置界面.feature.md` | `设置界面在训练中打开` | 服务不得创建、替换、结束或取消当前训练 Session |

## 交付内容

- 实现接口 10 冻结的完整 DTO 和枚举：
  - `GameSettingsDto`
  - `ComfortLevel`
  - `TurnMode`
  - `MoveMode`
  - `Language`
- 实现 `ISettingsService`：
  - `Load()`
  - `GetPending()`
  - `SetPending(GameSettingsDto settings)`
  - `ResetPendingToDefault()`
  - `SavePending()`
  - `DiscardPending()`
  - `GetDefault()`
- 提供可注入的本地持久化抽象，例如 `ISettingsPersistence`；生产实现可以使用 JSON、PlayerPrefs 或项目统一方案，业务服务不得依赖具体文件 API。
- 默认值严格使用接口 10：Comfort Medium、Turn Smooth、Move Teleport、Brightness 70、HudOpacity 80、SfxVolume 80、AimAssist true、Language Chinese。
- `Brightness`、`HudOpacity`、`SfxVolume` 的合法范围均为 0-100，包含端点。
- 对所有枚举值做显式合法性校验；反序列化出的未知值不得静默进入运行时。
- `Load()` 成功时同时建立已保存快照和 Pending 快照。
- 首次无配置文件时返回默认值并建立内存快照；是否立即写入默认文件必须由接口文档给出唯一策略，测试与实现保持一致。
- 配置损坏时返回 `PersistenceFailed` 和可安全渲染的默认 DTO，在内存中回退默认值，但在玩家点击应用前不得静默覆盖损坏文件。
- `SetPending()` 只在完整 DTO 通过校验后原子替换 Pending；失败时不得部分修改或发布成功预览事件。
- `ResetPendingToDefault()` 只修改 Pending，不修改最近一次成功保存值。
- `SavePending()` 成功后才更新已保存快照；保存失败时保留原已保存快照和当前 Pending，允许玩家重试。
- `SavePending()` 保存相同 DTO 必须幂等，不重复写入、不重复制造副作用；是否仍发布一次确认事件应在 task001 冻结并由测试固定。
- `DiscardPending()` 将 Pending 恢复为最近一次成功保存值，不访问或改变训练 Session。

## 事件契约

- `SettingsPreviewChangedEvent` 或 task001 冻结的等效事件：
  - `SetPending()` 成功后发布。
  - `ResetPendingToDefault()` 成功后发布。
  - `DiscardPending()` 成功后发布恢复后的已保存设置，只用于设置页 Preview 回滚；全局运行时对象在未保存期间本就不应被 Pending 修改。
  - 数据必须包含完整 `GameSettingsDto` 和预览来源，不能让订阅方再查询 UI 控件。
- `SettingsChangedEvent`：仅在持久化成功并提交已保存快照后发布，数据包含完整已保存 DTO。
- 持久化失败、非法输入或无状态变化不得发布伪成功事件。

## 错误处理

| 条件 | 错误码 | 状态要求 |
|---|---|---|
| Slider 数值越界 | `InvalidInput` | Pending 和已保存值不变 |
| 枚举值未定义 | `InvalidInput` | Pending 和已保存值不变 |
| DTO 缺少必须数据或结构非法 | `InvalidInput` | 不发布预览事件 |
| 读取失败或配置损坏 | `PersistenceFailed` | 返回默认 DTO 供渲染，保留可重试/覆盖策略 |
| 保存失败 | `PersistenceFailed` | 已保存值不变，Pending 保留，允许重试 |

## 不包含

- 不实现设置页面、Slider、Toggle、实时预览窗口或 UI 文本。
- 不直接启用/禁用 XRI 移动与转向 Provider。
- 不直接修改 HUD CanvasGroup、AudioMixer、后处理、曝光、瞄准辅助或本地化组件。
- 不实现训练暂停菜单或路由；只保证服务调用不影响当前 Session。
- 不实现云同步、账号设置、网络存档或多语言资源内容。

## 依赖关系

- 前置依赖：task001。
- 可并行：task003、task004、task005。
- 后续依赖：task014、task015、task016。

## 联调说明

- 与 UI 联调：UI 通过 `GetPending` 渲染，通过 `SetPending` 提交完整临时 DTO；View 不直接持久化。
- 与 功能B 联调：task014 只订阅预览/正式事件并应用 Unity 运行时效果，不反向修改设置服务内部状态。
- 与路由联调：从主菜单或训练暂停页进入设置时均使用相同服务；返回动作先 `DiscardPending`，再由路由返回原页。

## 测试要求

- EditMode 单元测试，测试名需包含 `BDD22` 和对应场景名：
  - 默认值和全部字段加载。
  - 0、100 合法，-1、101 非法且无部分修改。
  - 未定义枚举值返回 `InvalidInput`。
  - SetPending 更新 Pending、不更新已保存值，并发布一次完整预览事件。
  - 恢复默认只改 Pending；随后重新 Load 仍得到旧已保存值。
  - SavePending 成功后新服务实例可加载相同值，并发布正式事件。
  - 保存同一 DTO 幂等。
  - DiscardPending 恢复最近成功保存值并发布回滚预览。
  - 保存失败保留 Pending、保持已保存值，并且不发布正式变更事件。
  - 配置损坏返回 `PersistenceFailed` 与安全默认 DTO。
  - 设置操作前后 `ITrainingSessionService.Current.SessionId` 和 State 不变。
- 测试使用内存持久化、损坏数据持久化和保存失败持久化替身，不读写开发机真实配置。
- 本任务测试无需加载 Unity 场景、XR 设备或 UI Prefab。

## 无 VR PlayMode 联调要求

由 task014/task015 接入后补充 PlayMode 证明：Pending 修改会调用运行时预览替身，Discard 会回滚，Save 会提交；测试不得依赖 VR 设备。该联调用例缺失时，task002 服务可以完成，但 P2 设置功能不得整体标记完成。

## VR 实机验收清单

本任务本身是纯服务；以下项目移交 task014/task015 和 task016 实机验收：

- 平滑/分段转向和瞬移/连续移动与保存值一致。
- HUD 透明度、音效音量和亮度预览可观察，返回不保存后恢复。
- 从训练暂停菜单进入设置并返回时，当前移动靶或 100m Session 不丢失。
- 任一设置应用不得改写 HMD 跟踪姿态或创建第二个 Camera/AudioListener。

## 验收标准

- `ISettingsService` 全部命令、查询、默认值、事件和错误分支符合接口 10 及 task001 冻结契约。
- BDD22 的七类场景均有可追溯 EditMode 测试或明确的后续 PlayMode 联调用例。
- Pending、已保存值和运行时预览三者边界清楚；返回不保存不会留下预览副作用。
- 持久化失败或损坏数据不会使 UI 崩溃，也不会误报保存成功。
- 不加载场景的 EditMode 测试全部通过。
