# task014 设置运行时适配与训练 Session 保活

## 负责人

功能B-武器输入与AI基础

## 目标

把设置服务发布的已应用配置映射到 XRI 移动/转向、HUD、音频和画面运行时对象，同时确保从训练中进入设置再返回不会销毁当前训练 Session。

## 参考资料

- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/screens/22-设置界面.feature.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/10-设置服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

- `22-设置界面.feature.md`：`修改二选一或多选设置`、`修改滑条设置`、`应用设置`、`恢复默认设置`、`返回不保存临时修改`、`设置界面在训练中打开`。

## 交付内容

- 可替换的设置运行时适配层，不把 Unity 组件引用放进 `ISettingsService`：
  - `TurnMode.Smooth` 与 `TurnMode.Snap` 只启用对应的连续/分段转向 Provider。
  - `MoveMode.Teleport` 与 `MoveMode.Continuous` 只启用对应的传送/连续移动 Provider。
  - `HudOpacity` 映射到 HUD 根 `CanvasGroup.alpha`，并在各模式 HUD 间保持一致。
  - `SfxVolume` 映射到 AudioMixer 音效组，使用可测试的数值转换。
  - `Brightness` 映射到 task001 约定的全局曝光或后处理适配入口。
  - `ComfortLevel`、`AimAssistEnabled` 和 `Language` 按 task001 冻结的 P2 口径接入；没有契约时不得在本任务自行发明玩法算法或语言包。
- Pending 设置只驱动设置页内隔离的 Preview 适配器，不得直接修改训练 HUD、全局 XRI Provider、全局 AudioMixer 或全局曝光；只有 `SavePending()` 成功并发布 `SettingsChangedEvent` 后才改变全局运行时配置。
- `DiscardPending()` 后把 Preview 恢复为已保存值；全局运行时对象因未应用过 Pending，本身应保持不变。
- 训练内设置路由上下文：
  - 只携带并校验 task001 冻结的 `NavigationArgs`/暂停上下文，调用既有路由和 `Pause/Resume` 命令；不得建立第二套路由历史或 Session 快照权威。
  - 返回后由应用服务恢复原暂停/HUD 页面；本任务只验证同一 Session ID、弹药、命中、计时和武器状态仍存在，不复制、序列化、重建或回写 Session。
- 所有适配器支持无 VR 测试替身；业务层不直接查找具体 XRI GameObject。

## 不包含

- 不实现设置持久化、设置 UI 或 Presenter。
- 不实现新的移动、转向、瞄准辅助或本地化业务算法。
- 不把训练暂停系统扩展为 P3 战斗菜单；只提供 P2 设置往返所需的最小路由/保活接入。

## 依赖关系

- 前置依赖：task002、P1 task003、P1 task013。
- 可并行：task006、task012、task013。
- 后续依赖：task015、task016、task017。

## 联调说明

- 与 功能A 联调：订阅预览/正式设置事件，区分 Preview 与全局对象；训练往返只调用冻结的路由和暂停恢复命令，不保存或回写 Session 快照。
- 与 UI 联调：提供可观察预览状态；UI 不直接启停 XRI Provider、AudioMixer 或后处理对象。
- 与 场景 联调：绑定主菜单、移动靶场和 100m 靶场的 HUD/音频/后处理根节点。

## 测试要求

- EditMode 单元测试：
  - 转向和移动枚举映射到唯一正确 Provider 状态。
  - 透明度、音量和亮度边界值按接口范围转换。
  - Pending 预览不会发布全局应用事件；应用成功恰好应用一次。
- PlayMode 测试：
  - 修改并应用移动/转向后，可观察到对应 XRI Provider 启停。
  - 设置页 Preview 的 HUD 透明度、音量和亮度变化可观察，同时训练 HUD、全局 AudioMixer 和全局曝光保持不变；返回不保存时 Preview 恢复已保存值。
  - 保存成功后才观察到全局 XRI Provider、训练 HUD、AudioMixer 和曝光使用新值。
  - 从移动靶训练中打开设置再返回，Session ID、阶段、弹药、点射数和命中数保持不变。
  - 无 VR 环境可以使用适配器替身完成上述测试。
- VR 实机验收：平滑/分段转向、传送/连续移动切换无双 Provider 冲突；HUD 透明度和音量变化可感知且不影响射击视线。

## 验收标准

- UI 和设置服务不直接依赖具体 XRI、AudioMixer 或后处理组件。
- 只有已应用设置改变全局运行时状态，未应用返回不会泄漏临时值。
- 训练内设置往返不丢失或重复创建 Session。
- EditMode/PlayMode 测试通过，VR 手工验收项已记录。
