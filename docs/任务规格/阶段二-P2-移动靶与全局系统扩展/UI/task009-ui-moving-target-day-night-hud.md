# task009 移动靶昼夜 HUD UI

## 负责人

UI

## 目标

实现共享 Presenter 驱动的白天与夜晚移动靶第一人称 HUD，显示倒计时、弹药、点射次数、命中数、速度、方向、可射击状态和端点禁射提示，并在夜晚显示微光镜状态。HUD 只渲染 task007 聚合的 DTO/事件，不决定目标运动、点射是否有效或弹药是否消耗。

## 参考资料

- `UI/Sample/vr-shooting-moving-target-daytime-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-moving-target-night-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

| Feature | 精确场景名 | 本任务验收范围 |
|---|---|---|
| `09-移动靶白天HUD.feature.md` | `训练开始前等待 3 秒` | 显示倒计时/等待提示，禁射状态不显示为可射击 |
| `09-移动靶白天HUD.feature.md` | `HUD 显示白天训练状态` | 显示 10 发、点射模式、0/10、4m/s、右→左和可射击状态 |
| `09-移动靶白天HUD.feature.md` | `每次点射消耗两发弹` | DTO 更新后同步刷新弹药和五段点射节奏条 |
| `09-移动靶白天HUD.feature.md` | `命中目标后更新命中数` | 渲染命中数与本次点射反馈 |
| `09-移动靶白天HUD.feature.md` | `目标到达左端后禁止射击` | 显示端点停留禁射警示，数据保持服务结果 |
| `09-移动靶白天HUD.feature.md` | `目标抵达右侧终点后进入结算` | 收到完成/路由事件后退出 HUD，不由 View 自行结算 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚 HUD 使用微光镜显示` | 显示微光镜状态和夜晚固定 HUD 信息区 |
| `10-移动靶夜晚HUD.feature.md` | `显示夜晚移动状态` | 显示 2.5m/s、左→右及发光端点/路线 UI 状态 |
| `10-移动靶夜晚HUD.feature.md` | `端点停留期间禁止射击` | 显示“禁止射击”或等效警示，按 DTO 保持弹药/命中值 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚模式命中统计` | 更新命中数和点射节奏结果 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚训练结束` | 路由到结算并由结果 DTO 保留夜晚/2.5m/s 信息 |

## 交付内容

- `Screen_MovingTargetDayHud` 或对应场景内 HUD 根节点：
  - `Text_MovingTargetDayHud_Countdown`
  - `Text_MovingTargetDayHud_Ammo`
  - `Text_MovingTargetDayHud_FireMode`
  - `Text_MovingTargetDayHud_HitCount`
  - `Text_MovingTargetDayHud_TargetSpeed`
  - `Text_MovingTargetDayHud_Direction`
  - `Text_MovingTargetDayHud_ShootState`
  - `Text_MovingTargetDayHud_EndpointWarning`
  - `Hud_MovingTargetDay_BurstRhythm`
  - `Hud_MovingTargetDay_Route`
- `Screen_MovingTargetNightHud` 或对应场景内 HUD 根节点：
  - `Text_MovingTargetNightHud_Countdown`
  - `Text_MovingTargetNightHud_Ammo`
  - `Text_MovingTargetNightHud_FireMode`
  - `Text_MovingTargetNightHud_HitCount`
  - `Text_MovingTargetNightHud_TargetSpeed`
  - `Text_MovingTargetNightHud_Direction`
  - `Text_MovingTargetNightHud_ShootState`
  - `Text_MovingTargetNightHud_EndpointWarning`
  - `Text_MovingTargetNightHud_LowLightSightState`
  - `Hud_MovingTargetNight_BurstRhythm`
  - `Hud_MovingTargetNight_Route`
- 共享 HUD Presenter/绑定组件：
  - 订阅 `HudUpdatedEvent`/`IHUDService.HudUpdated` 以及 task007 暴露的完成事件。
  - 根据 `HudType` 选择昼夜 View，不在 View 中根据速度推断模式。
  - 将 DTO 提供的 `Ammo`、文本行、提示、`CanShoot`、方向和点射记录映射到固定控件。
  - 对 DTO 新枚举或缺失可选字段提供安全的默认显示，不写入示例数据。

## 视觉要求

- 昼夜 HUD 共享信息层级和锚点，模式差异只体现在配色、微光镜状态和场景视觉接口。
- HUD 中心必须为觇孔/微光镜和移动目标保留清晰瞄准区域。
- 禁射提示使用橙色或红色，但不得用全屏闪烁造成 VR 不适。
- 微光镜图形只表现 task006/task011 的状态；HUD 不创建第二套相机、后处理或命中判断。
- 倒计时、弹药、命中和方向应在 World Space Canvas 及无 VR 屏幕空间路径中保持可读。

## 不包含

- 不推进时间、不移动目标、不判断左右端点或结束条件。
- 不执行两发点射，不扣弹、不计算命中或评级。
- 不实现微光镜渲染、场景灯光或后处理，分别由 task006、task011 负责。
- 不从 HUD 文本反推结算数据。
- 不包含堑壕、城镇 HUD 或队友/敌人状态。

## 依赖关系

- 前置依赖：task006、task007。
- 联调依赖：task005、task011。
- 后续依赖：task016、task017、task018。

## 联调说明

- 与 task007 联调：`HudDto` 必填字段、点射记录显示键、完成事件和刷新频率。
- 与 task006 联调：弹药、有效点射、命中反馈和昼夜瞄具状态只通过 DTO/事件进入 HUD。
- 与 task005/task011 联调：HUD 锚点、目标可见区域、端点标识和微光效果下的对比度。
- 与 task010 联调：收到 Session 完成事件后只请求路由，不自行拼装结算对象。

## 测试要求

### 无 VR PlayMode 自动化

- 使用测试 DTO 验证白天 HUD 初始显示 10 发、点射模式、0/10、4m/s、右→左和可射击状态。
- 倒计时阶段显示等待且 `CanShoot=false`；切换到运行 DTO 后提示和方向同步更新。
- 每次接收弹药/点射更新后，弹药按 2 发递减且节奏条恰好新增一段，最多显示 5 段。
- 命中 0、1 或 2 发的点射结果均能按 DTO 更新总命中和本次反馈，不由 UI 推算。
- 左端停留 DTO 同时显示禁射提示并保持服务提供的弹药、命中值。
- 夜晚 HUD 显示微光镜状态、2.5m/s、左→右和端点标识；缺少微光状态时安全降级为可定位提示。
- 完成事件只触发一次结算路由，重复事件不生成叠层页面。
- 测试路径使用 P1 输入替身/事件总线，不为 HUD 添加直接硬件读取。

### VR 实机手工验收

- 白天通过觇孔观察移动靶时，弹药、命中和方向可读且不遮挡目标。
- 夜晚微光效果开启后文字对比度足够，中心视野不过曝，禁射提示能及时辨认。
- 头部小幅移动和正常据枪时 HUD 不漂移到舒适视野外，不强制移动 HMD 或 XR Origin。

## 验收标准

- 昼夜 HUD 全部字段来自 DTO/事件，且共享 Presenter 不包含业务规则。
- BDD 中弹药、点射、命中、速度、方向、禁射和微光状态均有稳定测试 ID。
- 无 VR PlayMode 测试通过；VR 下瞄准区域、文字和警示可读。
