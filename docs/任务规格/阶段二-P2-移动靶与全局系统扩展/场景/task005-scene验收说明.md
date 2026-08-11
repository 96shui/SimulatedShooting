# task005 移动靶靶场场景验收说明

## 当前交付

- 场景：`Assets/Scenes/MovingTargetRangeScene.unity`
- 场景生成器：`Assets/SimulatedShooting/Editor/MovingTargetRangeSceneBuilder.cs`
- 路线绑定：`Assets/SimulatedShooting/Runtime/Scene/MovingTargetRouteBinding.cs`
- PlayMode 测试：`Assets/SimulatedShooting/Tests/PlayMode/MovingTargetRangeSceneTests.cs`

## 场景尺度

- 射击位位于路线中心正前方，水平距离为 100m。
- 路线沿世界 X 轴横向布置，右端点为 `X=20m`，左端点为 `X=-20m`，中心距为 40m。
- 目标默认位于右端点；外部权威状态通过 `ApplyNormalizedProgress` 提供从右端 0 到左端 1 的规范化进度。
- 返回阶段由功能A传入递减的权威进度，场景绑定不计算方向、阶段和时长。

## 场景内容

- Floor Tracking 的 XR Origin、无 VR 相机、射击位、玩家出生点和 HUD 锚点。
- 横向轨道、左右端点标识、射击台、距离标记和基础挡弹背景。
- 侧身跑步靶 Blockout，包含统一 BoxCollider 命中面、目标中心和命中反馈挂点。
- 预留武器枪口、命中射线、昼夜根节点和微光镜挂点。
- 目标及其绑定未标记为 Static；静态批处理仅用于环境和路线设施。

## 稳定测试 ID

| 用途 | 测试 ID |
|---|---|
| 场景根 | `MovingTargetRange.Root` |
| 射击位 | `MovingTargetRange.ShootingPosition` |
| 玩家出生点 | `MovingTargetRange.PlayerSpawn` |
| VR Origin | `MovingTargetRange.Origin.VR` |
| 无 VR 相机 | `MovingTargetRange.Camera.NoVR` |
| HUD 锚点 | `MovingTargetRange.Hud.Anchor` |
| 路线根 | `MovingTargetRange.Route.Root` |
| 右端点 | `MovingTargetRange.Route.RightEndpoint` |
| 左端点 | `MovingTargetRange.Route.LeftEndpoint` |
| 目标根 | `MovingTargetRange.Target` |
| 命中面 | `MovingTargetRange.Target.HitSurface` |
| 路线绑定 | `MovingTargetRange.Target.Binding` |

## 自动化验收

PlayMode 测试覆盖：

- `BDD08 开始训练`：场景和关键 ID 可定位。
- `BDD09 HUD 显示白天训练状态`：100m 射距、40m 路线以及中心关系。
- `BDD09 训练开始前等待 3 秒`：目标默认停在右端，真实帧时间不会自行移动目标。
- `BDD09 目标到达左端后禁止射击`、`目标抵达右侧终点后进入结算`的场景责任：外部进度可确定性映射到右端、路线中点和左端，绑定不修改玩法状态。
- 射击位朝路线中心和两个端点的测试射线均可命中同一稳定命中面。
- XR/无 VR 模式分别只保留一个活动玩家 Camera 和 AudioListener。

## 任务边界

本任务没有实现移动靶状态机、Session、3 秒等待、2 秒停留、禁射、点射、弹药、命中评级、结算或 HUD。当前 task001 权威 DTO 尚未落地，因此 `MovingTargetRouteBinding` 只暴露无时间依赖的规范化进度入口；功能A契约冻结后再由适配器接入，不得在场景层增加第二套状态机。

## VR 手工验收

- [ ] 真实 HMD 中确认 100m 距离和 40m 横向路线关系清晰。
- [ ] 中心与左右端点的侧身靶均可识别，HUD/武器不遮挡路线。
- [ ] 玩家不离开安全射击位即可观察完整路线。
- [ ] HMD 与无 VR 相机的射击轴线、目标高度和 HUD 锚点一致。

