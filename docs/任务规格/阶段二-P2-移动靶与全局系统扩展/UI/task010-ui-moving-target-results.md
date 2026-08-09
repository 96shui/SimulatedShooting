# task010 移动靶结算 UI

## 负责人

UI

## 目标

实现移动目标射击训练结算页面，展示命中数、实际弹药消耗、昼夜模式、目标速度、评级、五次点射记录和路线命中时间点，并通过服务/路由命令支持重新训练和返回模式选择。页面只渲染 `MovingTargetResultDto`，不根据文本或命中数自行计算评级。

## 参考资料

- `UI/Sample/vr-shooting-moving-target-results-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

| Feature | 精确场景名 | 本任务验收范围 |
|---|---|---|
| `11-移动靶结算.feature.md` | `根据命中发数显示评级` | 渲染 DTO 给出的不及格、及格、良好、优秀，不在 UI 重算 |
| `11-移动靶结算.feature.md` | `显示训练摘要` | 显示命中、弹药消耗、昼夜、速度和评级规则 |
| `11-移动靶结算.feature.md` | `显示 5 次点射记录` | 渲染第 1-5 次记录及对应路线时间点/命中结果 |
| `11-移动靶结算.feature.md` | `重新训练` | 发送重训命令，按服务/路由确定的单一路径清理旧 Session |
| `11-移动靶结算.feature.md` | `返回模式选择` | 保存最近评级摘要后返回模式选择，不由 View 写档案 |

## 交付内容

- `Screen_MovingTargetResults`
- 摘要与评级区域：
  - `Text_MovingTargetResults_Grade`
  - `Text_MovingTargetResults_HitCount`
  - `Text_MovingTargetResults_AmmoConsumed`
  - `Text_MovingTargetResults_LightMode`
  - `Text_MovingTargetResults_TargetSpeed`
  - `Text_MovingTargetResults_GradeRule`
- 记录与路线区域：
  - `Panel_MovingTargetResults_BurstRecords`
  - `Hud_MovingTargetResults_RouteTimeline`
  - 第 1-5 条记录使用稳定行 ID，例如 `Text_MovingTargetResults_Burst_1` 至 `Text_MovingTargetResults_Burst_5`。
- 操作与错误状态：
  - `Button_MovingTargetResults_Retry`
  - `Button_MovingTargetResults_BackToModeSelection`
  - `Text_MovingTargetResults_Error`
- 结算 Presenter：
  - 接收 `MovingTargetResultDto`，按 DTO 顺序渲染实际 0-5 条 `BurstRecords`；固定五个视觉槽位中没有记录的槽位显示服务提供的“未使用”展示状态，不创建伪记录。
  - 使用实际 `TotalAmmoConsumed`，不得固定写死“10 发”；完成 5 次点射时应显示服务返回的 10 发，未射满时显示 0-8 发实际值。
  - 评级文字仅映射 `ResultGrade`；未知枚举使用安全默认文本并记录错误。
  - 评级规则文本只渲染 task001/task007 冻结的结构化展示 DTO/Key，不在 View 或 Presenter 中复制 3/4/5 发阈值。
  - 重训和返回按钮只发送命令，并以 task007/路由结果决定目标页面。
  - 路由或摘要保存失败时保留当前结算页，显示错误并允许重试。

## 视觉要求

- 评级是首要视觉信息；命中、弹药、模式和速度形成易扫描摘要。
- 五次点射记录与路线时间点一一对齐，不能只用一张静态示例路线图。
- 不及格使用警示色，优秀/良好/及格仍需保证色弱和夜间环境下可读。
- 按钮与主要文本在 World Space Canvas 舒适视野内，不遮挡结算记录。

## 不包含

- 不计算命中数、评级、弹药消耗或路线时间。
- 不从 HUD 文本、靶标 Transform 或场景对象反推结算数据。
- 不在 View 中直接删除 Session 或写入玩家档案。
- 不实现完整历史训练记录、回放系统或 P3 结算页。

## 依赖关系

- 前置依赖：task007。
- 联调依赖：task004、task008。
- 后续依赖：task016、task017。

## 联调说明

- 与 task007 联调：`MovingTargetResultDto`、五次点射记录、路线时间点、评级和最近评级摘要保存结果。
- 与 task004 联调：重训时旧 Session 清理及上次设置的处理方式由服务决定。
- 与 task008 联调：返回模式选择和返回设置页面的 `ScreenId`/选中状态一致。

## 测试要求

### 无 VR PlayMode 自动化

- 对命中 0、2、3、4、5、8 的 DTO 分别验证不及格、不及格、及格、良好、优秀、优秀的文字映射；测试只验证渲染，不调用 UI 评级算法。
- 分别使用 0 次、部分点射和完整 5 次结果，显示实际 0-10 发消耗、昼夜模式、速度和服务提供的评级规则。
- 始终存在 5 条稳定视觉槽位；实际记录中的命中数、时间和路线位置与 DTO 一致，其余槽位显示“未使用”且不带伪时间/位置。
- DTO 顺序变化、某次点射命中 1 发或 2 发时，UI 不把记录简化为错误的二值结果。
- 重训按钮快速重复点击只提交一次命令；完成后旧结果不残留。
- 返回模式选择时先收到摘要保存成功结果再切页；失败时保留页面和错误提示。

### VR 实机手工验收

- 评级、摘要、五次点射记录和路线时间点在头显中无需贴近面板即可辨认。
- 使用手柄射线可以稳定选择重训和返回，按钮不会被路线图或记录容器遮挡。
- 页面切换期间按钮进入 Busy 状态，不出现双重结算页。

## 验收标准

- 页面全部数据来自 `MovingTargetResultDto` 或路由/保存命令结果。
- 五次记录和路线时间点有稳定测试 ID，标准流程显示正确评级和 10 发消耗。
- 无 VR PlayMode 测试通过，VR 实机可读性和射线点击已记录。
