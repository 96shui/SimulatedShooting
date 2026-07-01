# task010 弹着分析弹窗 UI

## 负责人

UI

## 目标

实现每轮 3 发后的弹着分析弹窗，展示靶图、3 个弹着点、垂直/水平偏差、准星柱和觇孔调整建议，并支持应用调整和进入下一步。

## 参考资料

- `UI/Sample/vr-shooting-100m-impact-analysis-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/06-弹着分析.feature.md`
- `docs/接口文档/04-100m射校服务.md`

## 交付内容

- `Screen_ZeroingImpactAnalysis`
- 靶图显示区域。
- 3 个弹着点显示。
- 偏差数据文本：
  - 垂直偏差。
  - 水平偏差。
  - 准星柱方向和角度。
  - 觇孔方向和格数。
- `Button_ZeroingImpactAnalysis_ApplyAdjustment`
- `Button_ZeroingImpactAnalysis_NextRound`
- 已应用状态显示。

## 视觉要求

- 尽量还原弹着分析参考图的信息密度和布局。
- 靶图和数据面板并列，重点突出调整建议。
- 不使用固定示例数据。

## 不包含

- 不计算偏差或调整量。
- 不直接改变武器归零状态，只调用服务命令。

## 依赖关系

- 前置依赖：task006、task007。
- 后续依赖：task011、task014、task015。

## 联调说明

- 与 功能A 联调：获取 `ZeroingRoundAnalysisDto`，调用应用调整和继续命令。
- 与 功能B 联调：确认应用调整后后续射击偏移来源更新。

## 测试要求

- PlayMode 测试：
  - 3 发完成后弹窗打开。
  - 显示 3 个弹着点。
  - 点击应用调整后按钮状态/文本变化。
  - 重复点击不会重复应用。
  - 点击下一轮进入 HUD 或第三轮后进入结算。

## 验收标准

- 弹着分析页面所有数据来自服务 DTO。
- 应用调整操作幂等。
- 页面视觉接近参考图。
- PlayMode 测试通过。
