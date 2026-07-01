# task015 P1 UI 参考图还原与测试 ID 收口

## 负责人

UI

## 目标

对 P1 所有 UI 进行统一检查，确保视觉尽量贴近参考图，所有关键控件具备测试 ID，且无 VR/VR 两种视角下信息可读。

## 参考资料

- `UI/Sample/vr-shooting-main-menu-ui.png`
- `UI/Sample/vr-shooting-training-ui-main.png`
- `UI/Sample/vr-shooting-100m-zeroing-briefing-ui.png`
- `UI/Sample/vr-shooting-100m-zeroing-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-100m-impact-analysis-ui.png`
- `UI/Sample/vr-shooting-100m-final-rating-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## 交付内容

- P1 UI 视觉检查和调整。
- 所有 P1 页面根节点、按钮、文本、HUD 元素具备稳定测试 ID。
- 所有按钮禁用/Busy 状态检查。
- HUD 不遮挡靶标和瞄准点。
- UI 文本不溢出、不重叠。
- 提供 P1 UI 验收截图或说明。

## 不包含

- 不新增 P2/P3 页面。
- 不改变服务接口。

## 依赖关系

- 前置依赖：task008-task011、task014。
- 后续依赖：task016。

## 联调说明

- 与 功能A 联调：确认所有 DTO 字段都有 UI 映射。
- 与 功能B 联调：确认换弹、肩侧、禁射提示可见。
- 与 场景 联调：确认 HUD 空间位置和靶场视线。

## 测试要求

- PlayMode 测试：
  - 通过测试 ID 找到所有 P1 关键控件。
  - Busy 状态下重复点击无效。
  - HUD 字段随 DTO 更新。

## 验收标准

- P1 UI 与参考图主要结构和风格一致。
- 所有 P1 BDD 中出现的按钮和关键文本可由测试 ID 定位。
- 无 VR 测试和 VR 视角下核心信息可读。
- PlayMode UI 测试通过。
