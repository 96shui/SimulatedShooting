# task006 100m 射校核心规则服务

## 负责人

功能A-玩法流程与规则

## 目标

实现 100m 射校的核心规则：固定偏移、3 发一轮、最多 3 轮、弹着记录、偏差分析、应用调整、通过判定和最终评级。

## 参考资料

- `docs/需求/VR射击游戏需求文档_汇总版.md`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/BDD/screens/06-弹着分析.feature.md`
- `docs/BDD/screens/07-射校最终评级.feature.md`
- `docs/接口文档/04-100m射校服务.md`

## 交付内容

- `IZeroingService` 或等效服务。
- `ZeroingSessionDto`
- `ZeroingShotDto`
- `ShotInputDto`
- `ZeroingRoundAnalysisDto`
- `SightAdjustmentDto`
- `ZeroingResultDto`
- 固定随机种子生成初始偏移。
- 轮次和每轮 3 发限制。
- 垂直偏差和水平偏差计算。
- 准星柱调整：
  - 偏上：逆时针。
  - 偏下：顺时针。
  - 23cm 一周，0.064cm 约 1°。
- 觇孔调整：
  - 偏左：向前。
  - 偏右：向后。
  - 2cm 一格，向上取整。
- 调整应用幂等。
- 评级：
  - 第 1 轮通过：优秀。
  - 第 2 轮：良好。
  - 第 3 轮：及格。
  - 未通过：不及格。

## 不包含

- 不控制 UI 显示。
- 不直接读取武器或 XR 输入。
- 不直接创建靶标 GameObject。

## 依赖关系

- 前置依赖：task001、task002、task003。
- 后续依赖：task007、task010、task011、task013、task014。

## 联调说明

- 与 功能B 联调：接收射击时刻的瞄准点、稳定度、当前偏移和命中数据。
- 与 UI 联调：提供弹着分析和结算 DTO。

## 测试要求

- EditMode 单元测试：
  - 固定随机种子产生可复现偏移。
  - 每轮最多记录 3 发。
  - 偏差方向和调整方向正确。
  - 水平格数向上取整。
  - 应用调整幂等。
  - 各轮通过对应正确评级。
  - 3 轮未通过输出不及格。

## 验收标准

- 所有规则可在不加载 3D 场景时单独测试。
- UI 不需要自行计算评级或调整值。
- 服务返回错误码覆盖非法状态、重复操作、弹数耗尽。
- EditMode 测试通过。
