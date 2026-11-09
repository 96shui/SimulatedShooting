# 模式一、主菜单及模式三/四体验修订（2026-11-07）

## 定位与实现

- `Assets/Scenes/ZeroingRangeScene.unity` 的黑色框是 `ZeroingRange/FixedTargets_100m/Target_Primary_100m/TargetBacker` 以及其余四个固定靶同名子物体。它们是使用 `TargetDark` 材质的 1.4m×1.6m 深色背板，各自有 BoxCollider，可能挡住射向靶外的射线；因此仅禁用五个 MeshRenderer，保留碰撞体和原引用。命中由同级 `TargetFace_50cm` 的 `TargetImpactSurface` 和 Collider 承担，胸靶是 `TargetCenter/ChestTarget_ReferenceFace`，弹着位于同级 `ImpactMarkers`。场景生成器的主靶背板也禁用渲染。
- `MainScene` 的主菜单由 `MainMenuUI` 在运行时创建，原名单为 `Screen_MainMenu/Panel_MainMenu_AboutCreators/Text_MainMenu_Creators`。仅移除该标签，保留关于按钮、面板、标题、返回、点击遮挡和导航。对应测试把“内容为两个人名且字号≥60”的断言改为“名单标签不存在、标题仍在”，继续验证打开/关闭及导航。
- 模式三由 `UnityCombatSceneLoader` 加载 `Assets/Scenes/BunkersOriginalMode3CombatScene.unity`；模式四加载 `Assets/Scenes/CombatScene.unity`，`BunkersTrenchCombatScene.unity` 不是当前模式三生产入口。两者的 `CombatSceneRuntime` 实例化同一个已由 `CombatReloadInstaller` 分离弹匣的枪械预制体，挂载现有 `CombatRifleReloadPresentation`，无需改场景或重复安装器。
- 原模式二经 `TrainingWeaponFireCoordinator` 使用 `WeaponAutomaticFireService` 和应用配置 `WeaponAutoFireConfigDto.P2Default`（两发起射、0.1秒间隔、持续按住后逐发连射）。P3 原经 `CombatInputController → CombatCoreService.Fire` 单发；现在战斗核心注入逐发回调，复用同一调度服务和同一 DTO 配置。每发仍创建战斗 ShotId，经原有命中查询、伤害、反馈和弹药状态传递；不足两发拒绝起射；快速松开仍完成已预留的两发，持续连射段松开即停止，换弹、失握、追踪失效、切肩和终止均取消序列。模式一保持单发。
- 模式四沿用已有 30/60（共三个弹匣）、两秒计时、支撑手换弹与握把锁定组件；输入由左摇杆按压改为与模式三相同的左手柄 X（桌面 R），HUD 同步提示。为保留模式四原 X 开门交互，将其移到左摇杆按压；Input System 与 Unity XR 旧输入回退均支持按下沿。

## 变更文件

### 需求一：P1 背板

- `Assets/Scenes/ZeroingRangeScene.unity`：关闭五个背板的 MeshRenderer。
- `Assets/SimulatedShooting/Editor/ZeroingRangeSceneBuilder.cs`：生成主靶时隐藏背板渲染。
- `Assets/SimulatedShooting/Tests/PlayMode/ZeroingRangeSceneTests.cs`：新增背板不可见、碰撞与靶纸保留测试，并沿用靶面命中/弹着回归。

### 需求二：主菜单

- `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`：移除名单标签。
- `Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs`：改为验证名单不存在并保留面板、按钮与路由行为。

### 需求三：模式三/四连发

- `Assets/VRShooting/Runtime/Application/Weapons/WeaponAutomaticFireService.cs`：增加战斗核心的逐发回调接入，沿用原调度算法。
- `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs`：P3 步枪状态公布两发起射模式。
- `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs`：复用调度、模式二配置和 P3 弹药预留，逐发保留核心射击与生命周期门禁。
- `Assets/VRShooting/Runtime/Contracts/P3/CombatCoreContracts.cs`：增加战斗核心逐帧连发输入及逐发结果契约。
- `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs`：P3 默认模式及配置验证改为两发起射。
- `Assets/VRShooting/Runtime/Input/CombatInputController.cs`：提交扳机按下/保持/释放并逐发提交物理命中事实。
- `Assets/VRShooting/Tests/EditMode/Infrastructure/CombatMagazineReloadTests.cs`：双模式节奏、逐发快照、两发预留、弹量不足、换弹取消与扳机重置。
- `Assets/VRShooting/Tests/EditMode/Infrastructure/Screen22_P3ContractTests.cs`、`Screen23_CombatCoreTests.cs`：默认/实际武器模式断言同步。
- `Assets/VRShooting/Tests/PlayMode/Infrastructure/Screen23_CombatInputPlayModeTests.cs`：输入替身验证双模式两发独立命中，更新长按、换弹和肩侧流程。

### 需求四：模式四换弹

- `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs`：模式四采用模式三的 X 键与支撑手换弹表现；模式四门交互转到左摇杆按压。
- `Assets/SimulatedShooting/Runtime/Scene/P3XRTrainingInput.cs`：支持模式四左摇杆交互按下沿和 XR 回退。
- `Assets/VRShooting/Runtime/Input/CombatInputController.cs`：两模式换弹时均清除并锁定支撑握持。
- `Assets/VRShooting/Runtime/Unity/UI/P3TrenchHudAndResultsUI.cs`：共享 HUD 提示统一为 R / 左手柄 X。
- `Assets/SimulatedShooting/Tests/PlayMode/P3XRTrainingInputTests.cs`：验证双模式 X 换弹与模式四左摇杆交互按下沿。
- `Assets/SimulatedShooting/Tests/PlayMode/P3ProductionFlowTests.cs`：按实际建筑 HUD 与双模式支撑手锁定验证两种肩侧生产换弹；堑壕胜利测试以两发起射完成两次命中。

### 规格与记录

- `docs/README.md`、`docs/需求/阶段化整体需求说明书.md`：新增本次优先修订。
- `docs/BDD/screens/02-游戏主界面.feature.md`、`05-100m射击HUD.feature.md`、`23-P3共用战斗核心.feature.md`：更新名单、背板、连发与换弹验收。
- `docs/接口文档/01-页面导航与UI事件.md`、`06-武器与弹药服务.md`、`11-Unity场景与Prefab约定.md`、`14-P3战斗契约与独立测试基础.md`、`15-P3共用战斗核心.md`、`21-训练辅助与镜像左肩持枪.md`：更新绑定、战斗输入和共用服务契约。
- `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md`、`场景/task012-scene-target-impact-support.md`，`docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task004-combat-input-weapon-damage-and-enemy.md`、`task020-combat-magazine-reload.md` 与 `契约冻结清单.md`：同步负责人、测试与行为边界。
- 本记录及 `docs/codex-reports/evidence/vr-experience-2026-11-07/*.xml`：验证证据。

## 验证结果与未解决项

使用 Unity 2022.3.62f3c1 batchmode；未启动 Unity GUI。首次启动前没有 Unity 进程占用该项目；无图形 PlayMode 运行时 URP 原生渲染 SIGSEGV，未产出结果，之后采用带图形设备的 batchmode。用户原有 OpenXR 设置改动已逐字节恢复。

| 运行 | 结果 | 证据 |
|---|---|---|
| 全量 EditMode，343 项 | 321 通过、22 失败 | [edit-full.xml](evidence/vr-experience-2026-11-07/edit-full.xml) |
| 首轮相关 PlayMode，95 项 | 86 通过、9 失败 | [play-initial.xml](evidence/vr-experience-2026-11-07/play-initial.xml) |
| 最终集中 EditMode | 79/79 通过 | [edit-targeted.xml](evidence/vr-experience-2026-11-07/edit-targeted.xml) |
| 最终集中 PlayMode | 45/45 通过 | [play-targeted.xml](evidence/vr-experience-2026-11-07/play-targeted.xml) |
| 补充 P1 靶面、弹着、评分 PlayMode | 17/19 通过 | [p1-score.xml](evidence/vr-experience-2026-11-07/p1-score.xml) |

首轮 PlayMode 失败中两条模式四换弹用例按当前建筑 HUD 修正、一条旧单发测试按连发契约修正，三者及其关联回归随后通过。其余六条是模式四旧街道入口/HUD/胜负流程四条、模式一旧 100m 世界坐标及 12cm 前握半径两条；这六条没有在最终集中回归中重跑。`HEAD` 的固定靶父节点已有 z=-20.31，实际前握半径已为22cm，本次没有改动这些数值。全量 EditMode 的22条集中于上述靶位、射校调整数值、城镇旧敌人/入口/胜负及城市场景数量；失败用例和堆栈见 XML，本次没有将它们标记为通过。

P1 补充测试中，真实 MainScene → ZeroingRangeScene → 首轮优秀评级通过，16 个物理靶面、遮挡、弹孔与评分一致性回归通过；另外两个内存输入替身流程在结算轮次文字“第1轮：通过”“第3轮：未通过”断言失败，二者到该断言前的评级文字分别已符合“优秀”“不及格”。这些文案与调整服务问题不在本次修改范围，不能据此声称全量 P1 回归通过。

实机仍需分别在模式三/四验证双手持枪、快速扣动及长按的手感、最后一发/换弹门禁、左X与左摇杆开门、左右肩支撑手动作及跟踪恢复；并在模式一确认双眼视图中背板不再可见、靶纸/弹孔和评分表现正常。未进行 VR 设备验收；未提交或推送。
