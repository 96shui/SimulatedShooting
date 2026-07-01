# task011 射校最终评级 UI

## 负责人

UI

## 目标

实现 100m 射校结算页面，展示最终评级、三轮记录、弹着缩略图，并支持重新训练和返回模式选择。

## 参考资料

- `UI/Sample/vr-shooting-100m-final-rating-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/07-射校最终评级.feature.md`
- `docs/接口文档/04-100m射校服务.md`

## 交付内容

- `Screen_ZeroingFinalRating`
- 最终评级文本。
- 三轮射击记录列表。
- 弹着缩略图区域。
- `Button_ZeroingFinalRating_Retry`
- `Button_ZeroingFinalRating_BackToModeSelection`
- 结算 Presenter，从结算 DTO 渲染。

## 视觉要求

- 尽量还原最终评级参考图。
- 评级要醒目，记录列表易扫描。
- 缩略图来自训练数据，不使用静态示例。

## 不包含

- 不计算评级。
- 不保存完整档案系统；P1 只需要保留最近评级摘要或预留接口。

## 依赖关系

- 前置依赖：task006、task007。
- 后续依赖：task014、task015。

## 联调说明

- 与 功能A 联调：获取 `ZeroingResultDto` 和返回/重训命令。
- 与 UI task008 联调：返回模式选择或任务说明的目标页面一致。

## 测试要求

- PlayMode 测试：
  - 不同通过轮次显示不同评级。
  - 未通过显示不及格。
  - 三轮记录和未使用轮次显示正确。
  - 重训会清理旧 Session。
  - 返回按钮路由正确。

## 验收标准

- 评级和记录全部来自服务 DTO。
- 页面视觉接近参考图。
- 所有按钮和关键文本有稳定测试 ID。
- PlayMode 测试通过。
