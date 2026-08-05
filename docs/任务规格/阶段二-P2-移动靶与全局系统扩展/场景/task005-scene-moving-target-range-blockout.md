# task005 移动靶靶场 Blockout 与路线绑定

## 负责人

场景

## 目标

搭建 P2 `MovingTargetRangeScene` 的可测试 Blockout：射击位到移动路线中心为 100m，目标在左右相距 40m 的锚点之间横向运动，并提供侧身跑步靶、命中面、HUD/武器联调锚点和可替换时间驱动的场景绑定。移动阶段、3 秒等待、2 秒端点停留、禁射和结算均以功能A服务状态为权威，场景不得建立第二套玩法状态机。

## 参考资料

- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `UI/Sample/vr-shooting-moving-target-daytime-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-moving-target-night-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`

## BDD 场景追溯

| BDD 文件 | 精确场景 | 本任务责任 |
|---|---|---|
| `08-移动靶设置.feature.md` | `开始训练` | `MovingTargetRangeScene` 可被加载，场景绑定能呈现服务返回的等待状态，且不自行创建 Session。 |
| `09-移动靶白天HUD.feature.md` | `训练开始前等待 3 秒` | 服务处于等待阶段时目标稳定在右侧起点；测试时间由可替换时间源推进，不依赖真实等待。 |
| `09-移动靶白天HUD.feature.md` | `HUD 显示白天训练状态` | 提供 100m 射距、40m 横向路线和从右向左运动所需的稳定锚点；HUD 内容不属于本任务。 |
| `09-移动靶白天HUD.feature.md` | `目标到达左端后禁止射击` | 服务返回左端停留阶段时目标稳定在左锚点；是否禁射只读取服务状态。 |
| `09-移动靶白天HUD.feature.md` | `目标抵达右侧终点后进入结算` | 服务返回完成阶段时目标位于右锚点；页面跳转由功能A/UI 完成。 |
| `10-移动靶夜晚HUD.feature.md` | `端点停留期间禁止射击` | 夜晚复用同一路线与锚点，场景只呈现端点停留位置，不处理弹药或命中。 |

## 交付内容

- 创建 `MovingTargetRangeScene`，并加入 Build Settings 或项目等效场景注册表。
- 建立射击位、玩家出生点、Floor Tracking 的 VR Origin、无 VR 测试相机和 HUD 锚点。
- 建立与射击位前向垂直的移动路线：
  - 射击位到路线中心的水平距离为 `100m`。
  - 左右端点锚点中心距为 `40m`，默认相对路线中心各 `20m`。
  - 右锚点是等待与路线开始位置，左锚点支持停留，完成位置回到右锚点。
- 提供侧身跑步靶原型，至少包含可见靶体、统一命中碰撞面、中心参考点和弹着/命中反馈挂点；目标沿路线移动时对射击位保持可识别的侧身轮廓。
- 提供 `MovingTargetRouteBinding` 或等效场景适配组件：
  - 消费 task001 定义的移动靶权威状态、路线进度或等效查询结果。
  - 通过可注入时间源或显式 `Tick(deltaTime)` 驱动服务，PlayMode Test 可逐步推进时间。
  - 只把权威状态映射到目标 Transform、动画和场景提示，不自行计算阶段切换、3 秒/2 秒时长、`CanShoot` 或结算条件。
  - 不直接读取硬件输入，也不依赖 `Time.time` 才能测试。
- 保留武器枪口、命中射线、HUD Canvas 和昼夜表现的联调挂点。

## 场景结构与测试 ID

关键对象使用稳定名称，并挂载 `SceneTestId`、`UITestId` 或项目等效测试标识：

| 用途 | 建议对象名 | 稳定测试 ID |
|---|---|---|
| 场景根节点 | `MovingTargetRange` | `MovingTargetRange.Root` |
| 射击位 | `ShootingPosition` | `MovingTargetRange.ShootingPosition` |
| 玩家出生点 | `PlayerSpawn` | `MovingTargetRange.PlayerSpawn` |
| VR Origin | `XR Origin (VR)` | `MovingTargetRange.Origin.VR` |
| 无 VR 相机 | `Camera_NoVR` | `MovingTargetRange.Camera.NoVR` |
| HUD 锚点 | `HudAnchor` | `MovingTargetRange.Hud.Anchor` |
| 路线根节点 | `MovingTargetRoute_40m` | `MovingTargetRange.Route.Root` |
| 右端锚点 | `Anchor_MovingTarget_Right` | `MovingTargetRange.Route.RightEndpoint` |
| 左端锚点 | `Anchor_MovingTarget_Left` | `MovingTargetRange.Route.LeftEndpoint` |
| 侧身靶根节点 | `Target_Moving_SideProfile` | `MovingTargetRange.Target` |
| 靶体命中面 | `TargetHitSurface` | `MovingTargetRange.Target.HitSurface` |
| 路线绑定组件 | `MovingTargetRouteBinding` | `MovingTargetRange.Target.Binding` |

## 不包含

- 不实现移动靶状态机、速度合法性、3 秒倒计时、2 秒停留、禁射、点射、弹药、命中评级或结算。
- 不在场景脚本中复制 `TargetMovePhase` 或用 Transform 位置反推玩法阶段。
- 不实现昼夜美术和微光镜最终效果，由 task011 负责。
- 不实现最终场景美术、目标高级动画或真实人体伤害区域。

## 依赖关系

- 前置依赖：task001。
- 后续依赖：task011、task018。

## 联调说明

- 与 功能A 联调：确认 task001 输出的阶段/路线进度、可替换时间入口和场景加载命令；若现有 DTO 缺少驱动 Transform 所需的权威路线进度，应先更新接口文档，不得由场景私自补状态。
- 与 功能B 联调：确认侧身靶命中面、枪线、命中对象 ID 和视觉反馈挂点。
- 与 UI 联调：确认日/夜 HUD 锚点不遮挡目标路线和瞄准区域。

## 测试要求

- PlayMode 测试：
  - 通过 `SceneId=MovingTargetRangeScene` 加载场景，并找到全部关键测试 ID。
  - 射击位到路线中心的水平距离为 `100m`，允许几何搭建误差不超过 `0.10m`。
  - 左右锚点中心距为 `40m`，允许误差不超过 `0.05m`。
  - 从射击位朝路线中心、左右端点发出的测试射线均可命中侧身靶命中面，命中对象 ID 稳定。
  - 注入假时间源和 task001 测试替身后，等待阶段目标保持右端、右到左阶段连续移动、左端停留阶段保持左端、返回阶段连续移动并最终回到右端。
  - 假时间源不推进时目标不得因真实帧时间自行移动；绑定不得改写服务阶段或 `CanShoot`。
  - XR/无 VR 模式切换后分别只保留一个活动玩家相机和一个 `AudioListener`。
- 测试名称或说明必须引用上表中的 BDD 文件和精确场景名。

## VR 手工验收

- 从 Floor Tracking 的射击位观察时，100m 距离感、40m 横向路线和左右端点关系清晰。
- 侧身靶在中心及两个端点均可识别，不被环境物体、HUD 或武器遮挡。
- 玩家无需离开安全射击位即可观察完整路线；目标移动不会引发明显视觉跳变、穿模或不适。
- 无 VR 测试相机与真实 HMD 的射击轴线、目标高度和 HUD 锚点保持一致。

## 验收标准

- `MovingTargetRangeScene` 同时支持无 VR 自动化路径和真实 VR 场景验收。
- 100m 射距、40m 路线、左右锚点、侧身靶和稳定测试 ID 均满足规格。
- 场景绑定可由替换时间源确定性驱动，并只消费玩法服务权威状态。
- PlayMode 测试通过；真实 VR 手工验收项有记录，未执行项必须明确标记。
