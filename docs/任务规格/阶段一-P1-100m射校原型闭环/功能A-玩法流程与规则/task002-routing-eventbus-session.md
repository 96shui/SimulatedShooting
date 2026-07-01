# task002 路由、事件总线与训练 Session 生命周期

## 负责人

功能A-玩法流程与规则

## 目标

实现 P1 页面跳转、事件发布订阅和 100m 训练 Session 生命周期骨架，使 UI 能从主菜单进入 100m 任务说明、开始训练、进入 HUD、分析和结算。

## 参考资料

- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/00-前后端交互总约束.md`

## 交付内容

- `IGameEventBus` 及基础实现。
- `IUIRouter` 及 P1 页面路由状态。
- `ITrainingSessionService` 的 P1 生命周期：
  - 创建 Session
  - 开始 100m 射校
  - 暂停/恢复预留
  - 结束 Session
- P1 `ScreenId` 和 `UIEventId` 最小集。
- 事件：
  - `ScreenChangedEvent`
  - `SessionStartedEvent`
  - `SessionEndedEvent`

## 不包含

- 不计算弹着偏差。
- 不生成武器或场景对象。
- 不直接控制 UI GameObject。

## 依赖关系

- 前置依赖：task001。
- 后续依赖：task006、task007、task008、task014。

## 联调说明

- 与 UI 联调：按钮点击只调用路由/服务命令，不直接创建训练逻辑。
- 与 场景 联调：确认 `SceneId` 和当前是否合并场景的策略。

## 测试要求

- EditMode 单元测试：
  - 事件订阅、发布、取消订阅。
  - 从 MainMenu 到 ZeroingBriefing 的路由状态。
  - 创建 100m Session 后状态为进行中。
- PlayMode 测试预留：
  - 可由 task014 使用测试 ID 点击主菜单入口。

## 验收标准

- UI 可以通过服务接口完成 P1 页面状态切换。
- 重复点击开始按钮不会重复创建 Session。
- 所有状态变化有事件或可查询 DTO。
- EditMode 测试通过。
