# task011 昼夜环境、目标运动与微光镜表现接入

## 负责人

场景

## 目标

在 task005 的移动靶靶场中接入白天/夜晚环境表现、左右端点视觉标识和夜晚微光瞄准镜可用效果。两种模式必须复用同一条 100m/40m 路线，并保证目标、HUD 和禁射提示在无 VR 与真实 VR 中可读；昼夜选择、速度、阶段和禁射规则仍由服务层决定。

## 参考资料

- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `UI/Sample/vr-shooting-moving-target-daytime-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-moving-target-night-first-person-hud-ui.png`

## BDD 场景追溯

| BDD 文件 | 精确场景 | 本任务责任 |
|---|---|---|
| `08-移动靶设置.feature.md` | `切换到夜晚模式` | 提供可由 `MovingTargetLightMode.Night` 激活的夜间预览/环境和微光镜视觉入口。 |
| `09-移动靶白天HUD.feature.md` | `HUD 显示白天训练状态` | 白天环境下目标、路线和 HUD 锚点清晰可读；HUD 数据由 UI/服务负责。 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚 HUD 使用微光镜显示` | 中心瞄准区域提供绿色微光可用效果，同时不污染或遮挡固定 HUD 文本。 |
| `10-移动靶夜晚HUD.feature.md` | `显示夜晚移动状态` | 左右端点在夜晚使用发光插旗、标牌或等效低成本标识，并与 task005 锚点一致。 |
| `10-移动靶夜晚HUD.feature.md` | `端点停留期间禁止射击` | 场景可呈现端点停留视觉反馈，但禁止射击状态只消费服务输出，不由灯光或位置推导。 |

## 交付内容

- 在 `MovingTargetRangeScene` 中建立可切换的 Day/Night 视觉配置，复用路线、目标、碰撞体和测试锚点，不复制第二套玩法场景。
- 白天模式提供清晰的天空、主光、地面/靶场材质和足够的远距离目标对比度。
- 夜晚模式降低环境照度，并通过材质、灯光或轻量后处理建立夜间训练氛围；不得让玩家必须关闭 HUD 才能看清目标。
- 左右端点标识与 task005 的路线锚点绑定：
  - 白天可通过插旗、标牌或高对比色识别。
  - 夜晚使用自发光材质或受控低成本灯光识别。
  - 视觉标识不得成为端点状态、路线完成或禁射的权威来源。
- 与 task006 联调微光镜：
  - 提供中心绿色微光视野、遮罩、输出面和武器瞄具挂点。
  - 优先采用瞄具局部 RenderTexture、材质或镜头 UI；不得用强制改写 HMD FOV、姿态或全屏高亮模拟瞄准。
  - HUD Canvas 保持在不受微光色调污染的可读层，弹药、点射、命中和提示位置稳定。
- 提供消费 `MovingTargetLightMode` 的纯视觉绑定；视觉切换不得改变速度列表、`TargetMovePhase`、`CanShoot`、弹药或结算数据。

## 稳定测试 ID

| 用途 | 稳定测试 ID |
|---|---|
| 白天视觉根节点 | `MovingTargetRange.Lighting.Day` |
| 夜晚视觉根节点 | `MovingTargetRange.Lighting.Night` |
| 左端点标识 | `MovingTargetRange.EndpointMarker.Left` |
| 右端点标识 | `MovingTargetRange.EndpointMarker.Right` |
| 微光镜根节点 | `MovingTargetRange.Optic.LowLight` |
| 微光镜遮罩 | `MovingTargetRange.Optic.LowLight.Mask` |
| 微光镜输出 | `MovingTargetRange.Optic.LowLight.Output` |
| 微光辅助相机（仅 RenderTexture 方案需要） | `MovingTargetRange.Optic.LowLight.Camera` |
| 昼夜视觉绑定 | `MovingTargetRange.Lighting.ModeBinding` |

## 不包含

- 不实现昼夜模式选择、可用速度校验、目标路线状态机、端点禁射或结算。
- 不实现真实光电倍增、热成像、自动增益、炫光损伤或高拟真夜视噪声。
- 不通过灯光开关、端点发光或目标位置反推玩法状态。
- 不做 task018 的最终视觉和性能收口。

## 依赖关系

- 前置依赖：task005、task006。
- 后续依赖：task018。

## 联调说明

- 与 功能A 联调：从当前 Session/设置 DTO 读取 `MovingTargetLightMode` 和端点阶段，仅渲染对应视觉。
- 与 功能B 联调：确认夜晚瞄具挂点、微光输出、枪线和真实 HMD 自然机瞄路径，不修改武器服务状态。
- 与 UI 联调：确认日/夜 HUD、禁射提示和微光镜遮罩分层，避免文字被染色、裁切或遮挡。

## 测试要求

- PlayMode 测试：
  - 通过测试 ID 找到 Day/Night 根节点、两个端点标识和微光镜绑定。
  - 注入 Day DTO 时只启用白天视觉；注入 Night DTO 时只启用夜晚视觉和微光镜可用输出。
  - 重复切换昼夜不会复制路线、目标、玩家相机、`AudioListener` 或后处理对象。
  - 左右端点标识分别与 task005 左右锚点对齐，允许位置误差不超过 `0.05m`。
  - 微光镜输出限制在瞄具/中心遮罩内，HUD 根节点不位于被染色或裁切的渲染层。
  - 视觉模式切换前后，测试替身中的 `TargetMovePhase`、`CanShoot`、弹药和目标路线进度保持不变。
  - 无 VR 测试相机和 XR 相机路径均能激活相同的 Day/Night 配置，不要求真实等待。
- 测试名称或说明必须引用上表中的 BDD 文件和精确场景名。

## VR 手工验收

- 白天在 100m 距离能识别侧身靶、左右端点和路线方向，HUD 不遮挡瞄准区域。
- 夜晚裸眼环境保持基本方位可辨，使用微光镜时中心目标可识别、绿色效果稳定且 HUD 清晰。
- 左右端点发光标识不会造成过曝、双眼冲突或明显闪烁。
- 微光镜不吸附或强制移动 HMD，不发生 FOV 跳变、单眼黑屏、明显延迟或眩晕。

## 验收标准

- Day/Night 表现由服务模式确定，且不改变任何玩法状态。
- 夜晚微光镜达到“可用且可读”的 P2 原型标准，HUD 与端点标识在 VR/无 VR 中均可识别。
- 所有视觉根节点、端点标识和瞄具输出具有稳定测试 ID。
- PlayMode 测试通过；真实 VR 可读性与舒适度验收有记录。
