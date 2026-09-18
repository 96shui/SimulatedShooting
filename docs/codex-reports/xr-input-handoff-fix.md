# P2/P3 真实 XR 输入接入修复

日期：2026-09-17。分支：`fix/p3-vr-playable-integration`，基于 main `ae49519`。本次为用户实机反馈后的修复，尚未提交或推送。

## 问题与修复

1. P1/P2 场景的 `autoDetectVrDisplayInEditor` 被序列化为 false，普通 Play 即使存在运行中的 XR Display 也选择桌面相机。两场景改为 true，组件新建默认值也改为 true。保留显式无 VR 测试入口。
2. P3 加载时先启用新 XR Rig，再关闭 MainScene。两者共用 XRI InputActionAsset，旧 Rig 的 `OnDisable` 会关闭新 Rig 正在使用的头手姿态、Grip、UI Press 等动作。改为先停旧 Rig、绑定新 Rig，再启用新 Rig；返回时先停 P3 Rig，再恢复 MainScene，避免异步卸载再次关闭主菜单输入。
3. P3 原来只重新绑定 Interactor，没有绑定 XRInteractionGroup。手柄追踪激活后会报“交互组尚未注册”。现在先绑定组，再绑定组内交互器；共用靶场管理器切换也同步绑定组。
4. P3 枪体使用 IgnoreRaycast 层以排除自身弹道，但 Near-Far Interactor 的近距 Sphere Caster 只检测 Default 层。现在近距检测包含枪体层；弹道仍忽略自身枪体，远距抓枪规则未扩大。

## 测试与追溯

| 规格 / 任务 | 新增验证 |
|---|---|
| BDD00 进入靶场保留真实头手追踪；P2 场景 task003 | 两个场景的序列化自动检测开关必须开启；普通入口模式必须匹配运行中的 XR Display |
| BDD00/05 右后左前双手持枪；P2 场景 task003 | 合成 XRHMD/左右 XRController 的设备状态驱动正式 TrackedPoseDriver，检查相机旋转、双手可见与位置；用实际 Grip 输入拾取后手、前手 |
| BDD14/18 地图选择与简报开始；P3 task013 | 合成手柄姿态瞄准正式按钮，检查 Near-Far Interactor 的 UI 命中，再发送扳机按下/释放，经过 XRUIInputModule 完成点击；覆盖两个地图页及切场景后的简报 |
| BDD21 返回重进；P3 task013 | 两次进入城镇、双手实物抓枪、返回并等旧场景卸载完成，再检查主菜单动作与 HMD 旋转 |

新增 PlayMode 用例位于 `XRSceneHandoffTests`；静态场景验收加入 `TrainingRangeSceneBindingsTests`。原桌面几何/非 VR 集成测试明确选择桌面替身，避免测试机器是否接着头显改变测试目标。未删除或放宽原行为断言。

设备替身只替换显示可用性与硬件状态；使用原场景、XRI 动作引用、追踪驱动、手柄交互器、枪体碰撞、XR UI 模块和正式应用服务。点击验证未直接调用 `Button.onClick` 或 `ExecuteEvents`，抓枪验证未手动调用 `SelectEnter`。测试期间隔离真实输入设备并限定动作资产的测试设备，结束时先停止消费者，再恢复设备筛选与真实设备。

专项 **4/4**、全量 EditMode **266/266**、全量 PlayMode **169/169** 已通过，失败/跳过均为 0。证据：[专项 PlayMode](evidence/xr-input-handoff-fix/focused-playmode.json)、[EditMode](evidence/xr-input-handoff-fix/editmode.json)、[PlayMode](evidence/xr-input-handoff-fix/playmode.json)。

全量测试后另起普通 Play，不注入设备或强制 VR 开关，执行 MainScene → 移动靶 → MainScene → 城镇 → MainScene：两个模式均自动识别 VR，头部与左右控制器的 position/rotation 动作全部启用；城镇两个交互组均绑定 CombatScene 的管理器，返回主菜单后头部旋转动作仍启用。见 [移动靶运行状态](evidence/xr-input-handoff-fix/xr-range-runtime.json)、[城镇运行状态](evidence/xr-input-handoff-fix/xr-urban-runtime.json)。已退出 Play，MainScene 为活动场景且无未保存修改。

普通 Play 冒烟仅记录一条可选眼动设备缺失提示，无交互组注册错误或异常。该提示来自 XRI 示例 `GazeInputManager.cs:55` 的 `Debug.LogWarning`；MCP 将其类型标为 Error，保留原快照：[Console](evidence/xr-input-handoff-fix/xr-console.json)。不将此记录表述为 Console 0 warning，也未修改包代码压制提示。

之前 264/165 的回归只证明服务/桌面闭环与部分输入替身，未覆盖真实动作资产的跨 Rig 生命周期，不能作为上述真实 XR 操作通过的证据。本次补测覆盖这条遗漏路径，但仍不代替用户佩戴具体头显的实机复验。

## 用户复验

从 `Assets/Scenes/MainScene.unity` 开始，先连接头显与左右手柄，再按 Play：

1. 移动目标射击：转头确认视角跟随，移动左右手确认显示，右 Grip 抓后握把，再左 Grip 抓前握把；确认开始与拾枪后应进入倒计时。测试射击和返回。
2. 堑壕/城镇：用手柄射线与扳机点击地图页；堑壕还需点击简报“开始”。进场后测试转头、抓枪、移动/转向及场景内 UI，返回主菜单再进入一次。
3. P1/P2 按既有规格为固定卧姿：禁用摇杆人工移动、摇杆转向和传送，但真实转头必须有效。本次没有改变这一规则；P3 保留摇杆移动与离散转向。

真实设备的握把触达、自然持枪手感、HUD 可读性、舒适度和性能仍需复验。测试中的合成设备不提供触觉硬件；触觉能力提示及无眼动设备提示不作为实机触觉通过证据。
