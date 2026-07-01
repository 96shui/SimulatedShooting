# task007 100m HUD 与结算 DTO 聚合

## 负责人

功能A-玩法流程与规则

## 目标

把 100m 射校 Session、武器弹药、稳定度、弹着记录和评级结果聚合为 UI 可直接渲染的 DTO，保证 HUD 和结算页不写业务规则。

## 参考资料

- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/BDD/screens/06-弹着分析.feature.md`
- `docs/BDD/screens/07-射校最终评级.feature.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/04-100m射校服务.md`

## 交付内容

- `IHUDService` 的 P1 实现。
- `HudDto` 中 `Zeroing` 最小字段集：
  - 轮次。
  - 距离 100m。
  - 弹数。
  - 稳定度或稳定提示。
  - 弹着记录。
  - `CanShoot`。
  - 当前肩侧。
  - 换弹/弹药状态。
- `HudUpdatedEvent`
- 弹着分析 DTO 查询入口。
- 最终评级/结算 DTO 查询入口。

## 不包含

- 不负责 UI 排版。
- 不负责武器动画。
- 不负责场景靶标显示。

## 依赖关系

- 前置依赖：task006。
- 后续依赖：task009、task010、task011、task014、task015。

## 联调说明

- 与 UI 联调：确认每个 HUD 文本和面板字段从 DTO 映射。
- 与 功能B 联调：接收弹药变化、换弹状态、肩侧状态和稳定度。

## 测试要求

- EditMode 单元测试：
  - Session 初始 HUD 字段。
  - 开火后弹数和弹着记录刷新。
  - 换弹/切肩后 HUD 字段刷新。
  - 结算 DTO 不从 UI 文本反推。
- PlayMode 测试由 task014 覆盖 UI 订阅和页面刷新。

## 验收标准

- HUD 和结算页所需数据均可通过 DTO 获取。
- `HudUpdatedEvent` 在弹药、弹着、轮次、结算状态变化时发布。
- UI 不需要访问射校服务内部状态。
- EditMode 测试通过。
