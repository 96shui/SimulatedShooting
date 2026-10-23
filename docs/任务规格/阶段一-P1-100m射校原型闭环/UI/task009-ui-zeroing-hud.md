# task009 100m 射击 HUD

## 负责人

UI

## 目标

实现 100m 第一人称射击 HUD，显示轮次、距离、弹数、稳定度、弹着记录、当前肩侧和射击/换弹状态。

## 参考资料

- `UI/Sample/vr-shooting-100m-zeroing-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/接口文档/03-HUD显示数据.md`

## 交付内容

- `Screen_ZeroingHud` 或场景内 HUD 根节点。
- HUD 元素：
  - `Text_ZeroingHud_Round`
  - `Text_ZeroingHud_Distance`
  - `Text_ZeroingHud_Ammo`
  - `Hud_Zeroing_Stability`
  - `Hud_Zeroing_ImpactRecord`
  - `Text_ZeroingHud_Shoulder`
  - `Text_ZeroingHud_Prompt`
- HUD Presenter：
  - 订阅 `HudUpdatedEvent`。
  - 从 `HudDto` 渲染，不访问服务内部状态。

## 视觉要求

- 按参考图呈现第一人称 HUD：轻量、清晰、贴近军事训练界面。
- HUD 不阻挡靶标和瞄准区域。
- 文本在无 VR 和 VR 视角都可读。

## 不包含

- 不计算稳定度。
- 不计算弹着点。
- 不执行换弹或开火。

## 依赖关系

- 前置依赖：task005、task007。
- 联调依赖：task004、task012。
- 后续依赖：task014、task015。

## 联调说明

- 与 功能A 联调：字段由 `HudDto` 提供。
- 与 功能B 联调：弹药、换弹、肩侧、开火禁用提示。
- 与 场景 联调：HUD 空间位置和靶标视野。

## 测试要求

- PlayMode 测试：
  - HUD 加载后能显示初始 1/3、100m、3/3。
  - 开火后弹数和弹着记录刷新。
  - 切肩后肩侧提示刷新。
  - 弹数为 0 时显示禁射提示。

## 验收标准

- HUD 数据全部来自 DTO 或事件。
- 参考图中的核心信息布局已还原。
- 所有关键元素有稳定测试 ID。
- PlayMode 测试通过。
