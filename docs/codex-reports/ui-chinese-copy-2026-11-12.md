# 玩家可见界面文案中文化交付报告（2026-11-12）

本次中文化已完成。定向 EditMode 11/11、PlayMode 38/38 通过；全量 EditMode 385 条（365通过、20失败）、PlayMode 315 条（292通过、23失败）。对指定基线按完整 fullname 逐条比较，两套新增失败均为0，基线用例缺失为0。新增1条 EditMode、2条 PlayMode，全部通过。

交付仅修改显示文字、显示名来源、相应断言和规格示例，以及现有画廊的预览数据绑定。没有修改 UI 坐标、字号、颜色、面板尺寸、武器/弹药/训练数值、输入或结算规则；没有新增语言框架。所有 ID 保持不变。未执行 git commit、push、切分支或 stash。

开工时工作区已有62条状态记录（包含先前任务的目录记录）。本报告的文件清单与补丁均相对于开工备份，不将先前未提交改动计作本次成果；先前改动已保留。

## 文案前后对照

| 位置 | 修改前 | 修改后 |
|---|---|---|
| 主菜单副标题 | `战术训练中心  /  TACTICAL TRAINING` | `战术训练中心` |
| 主菜单底部 | `单机训练  /  LOCAL SIMULATION〔37个空格〕选择科目后查看任务简报` | `单机训练〔原有37个空格〕选择科目后查看任务简报` |
| 操作介绍 | `右手 Grip 抓枪 / 主手握持；左手 Grip 副手握持。` | `右手侧握键抓枪 / 主手握持；左手侧握键副手握持。` |
| 模式一简报标题 | `任务简报  MISSION BRIEFING` | `任务简报` |
| 无人机视频状态 | `LIVE · 实时视频` | `实时视频` |
| 楼层定义/HUD | `floor-1` | `一层` |
| 楼层定义/HUD | `floor-2` | `二层` |
| 楼层定义/HUD | `floor-3` | `三层` |
| 地图目录及结算页楼层 | `1F` | `一层` |
| 地图目录及结算页楼层 | `2F` | `二层` |
| 地图目录及结算页楼层 | `3F` | `三层` |
| 房间定义/列表 | `room-11` | `一层1号` |
| 房间定义/列表 | `room-12` | `一层2号` |
| 房间定义/列表 | `room-21` | `二层1号` |
| 房间定义/列表 | `room-22` | `二层2号` |
| 房间定义/列表 | `room-31` | `三层1号` |
| 无法映射的房间 | `直接显示未知 RoomId` | `房间 + 该层列表序号（从1开始）` |
| 堑壕地图显示名 | `堑壕地图 A / 堑壕地图 B（后续扩展）` | `堑壕地图甲 / 堑壕地图乙（后续扩展）` |
| 城镇地图显示名 | `城镇地图 A / 城镇地图 B（后续扩展）` | `城镇地图甲 / 城镇地图乙（后续扩展）` |
| 备用地图卡 | `地图 B（后续扩展）` | `地图乙（后续扩展）` |
| 堑壕简报缺少显示名时 | `地图：trench-a（或其他 MapId）` | `地图：堑壕训练场` |
| 训练步枪名称 | `P1/P2 training rifle` | `训练步枪` |
| 武器名称 | `19-1 automatic rifle` | `19-1式自动步枪` |
| 武器名称 | `95-1 automatic rifle` | `95-1式自动步枪` |
| 武器名称 | `QBS-09 shotgun` | `09式霰弹枪` |
| 武器名称 | `QJB-201 light machine gun` | `201式轻机枪` |
| 两个靶场左端牌 | `LEFT  -20 m  <` | `左端  -20 m  <` |
| 两个靶场右端牌 | `>  +20 m  RIGHT` | `>  +20 m  右端` |
| 射校射击位状态 | `射击位：ZeroingRange.FiringStation.Root 等内部键` | `射击位：100m射校` |
| 移动靶射击位状态 | `射击位：moving-target-prone-station 等内部键` | `射击位：移动靶` |
| 其他已绑定射击位 | `射击位：内部键` | `射击位：已就位` |
| 弹着坐标说明 | `红：弹着点（X右，Y上）` | `红：弹着点（横向向右，纵向向上）` |
| 无人机异常示例 | `开场未完成：ResourceUnavailable，请返回或重新开始。` | `开场未完成：资源不可用，请返回或重新开始。` |
| 通用及移动靶错误提示 | `英文 ErrorCode / 英文服务诊断` | `对应中文错误原因；原有中文详情继续显示` |
| 第一人称调试标题 | `TASK005 FIRST PERSON WEAPON` | `第一人称武器调试` |
| 第一人称调试状态 | `MAG / RES / READY True或False` | `弹匣 / 备用 / 可射击或不可射击` |
| 第一人称调试姿态 | `MODE HipFire或AimDownSights / SHOULDER Left或Right / STABILITY` | `腰射或瞄准 / 左肩或右肩 / 稳定度` |
| 第一人称调试操作 | `Mouse/Arrows look, WASD front hand, RMB/Shift ADS, LMB/F fire, R reload, Q shoulder` | `鼠标/方向键观察，WASD副手，RMB/Shift瞄准，LMB/F射击，R换弹，Q换肩` |
| 第一人称调试末发 | `LAST SHOT HIT/MISS + HitObjectId / LAST SHOT none` | `最近一发：命中/未命中 / 最近一发：暂无；内部命中键不再显示` |

底部删除英文片段后保留后半句前原有的37个空格；字幕和简报标题没有遗留斜杠或多余空格。既有两条键位提示逐字保留：` · R / 左手柄X键换弹`、`交互：右摇杆按压/Enter/空格 · 手雷：右A/H`。所有原有单位保持原样，包括100m、50cm、2cm、0.32cm、10cm、5°、m/s、50cm x 50cm及遥测m/s/s。

楼层和房间中文名由 CombatSceneDefinitionBuilder 提供，CombatMapCatalog 同步提供一层/二层/三层。HUD 按 CurrentFloorId 查找 FloorDto.DisplayName，缺少显示名才回退 Id；没有当前楼层则显示 --。当前生产场景的3层5房全部可映射，未遇到无法映射的实际房间；定向测试用 custom-room 验证“房间1”回退。开门测试继续验证提交 room-11 等原始键。

## 本次修改文件（43个原有文件）

下列清单的机器可读版本及前后 SHA-256 见 [changed-files.json](evidence/ui-chinese-copy-2026-11-12/changed-files.json)，相对于开工状态的完整补丁见 [task-changes.patch](evidence/ui-chinese-copy-2026-11-12/task-changes.patch)。

- [`Assets/Scenes/MovingTargetRangeScene.unity`](../../Assets/Scenes/MovingTargetRangeScene.unity)
- [`Assets/Scenes/ZeroingRangeScene.unity`](../../Assets/Scenes/ZeroingRangeScene.unity)
- [`Assets/SimulatedShooting/Editor/ProneRangeSceneMigration.cs`](../../Assets/SimulatedShooting/Editor/ProneRangeSceneMigration.cs)
- [`Assets/SimulatedShooting/Runtime/Scene/CombatSceneDefinitionBuilder.cs`](../../Assets/SimulatedShooting/Runtime/Scene/CombatSceneDefinitionBuilder.cs)
- [`Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs`](../../Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs)
- [`Assets/SimulatedShooting/Tests/EditMode/CombatSceneTests.cs`](../../Assets/SimulatedShooting/Tests/EditMode/CombatSceneTests.cs)
- [`Assets/VRShooting/Editor/TacticalUIGallery.cs`](../../Assets/VRShooting/Editor/TacticalUIGallery.cs)
- [`Assets/VRShooting/Runtime/Application/Combat/CombatMapCatalog.cs`](../../Assets/VRShooting/Runtime/Application/Combat/CombatMapCatalog.cs)
- [`Assets/VRShooting/Runtime/Application/Combat/TrenchService.cs`](../../Assets/VRShooting/Runtime/Application/Combat/TrenchService.cs)
- [`Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs`](../../Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs)
- [`Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs`](../../Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs`](../../Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/MovingTargetUIPresenter.cs`](../../Assets/VRShooting/Runtime/Unity/UI/MovingTargetUIPresenter.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs`](../../Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/P3DroneReconView.cs`](../../Assets/VRShooting/Runtime/Unity/UI/P3DroneReconView.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/P3TrenchUI.cs`](../../Assets/VRShooting/Runtime/Unity/UI/P3TrenchUI.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/P3UiCommon.cs`](../../Assets/VRShooting/Runtime/Unity/UI/P3UiCommon.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/P3UrbanUI.cs`](../../Assets/VRShooting/Runtime/Unity/UI/P3UrbanUI.cs)
- [`Assets/VRShooting/Runtime/Unity/UI/TrainingPresentationView.cs`](../../Assets/VRShooting/Runtime/Unity/UI/TrainingPresentationView.cs)
- [`Assets/VRShooting/Tests/EditMode/Application/WeaponControlServiceTests.cs`](../../Assets/VRShooting/Tests/EditMode/Application/WeaponControlServiceTests.cs)
- [`Assets/VRShooting/Tests/PlayMode/TacticalUIArtTests.cs`](../../Assets/VRShooting/Tests/PlayMode/TacticalUIArtTests.cs)
- [`Assets/VRShooting/Tests/PlayMode/UI/P3UiTask002_005_008_011PlayModeTests.cs`](../../Assets/VRShooting/Tests/PlayMode/UI/P3UiTask002_005_008_011PlayModeTests.cs)
- [`Assets/VRShooting/Tests/PlayMode/UI/Screen00_TrainingPresentationPresenterPlayModeTests.cs`](../../Assets/VRShooting/Tests/PlayMode/UI/Screen00_TrainingPresentationPresenterPlayModeTests.cs)
- [`Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs`](../../Assets/VRShooting/Tests/PlayMode/UI/Screen02_04_MainMenuZeroingBriefingUITests.cs)
- [`Assets/VRShooting/Tests/PlayMode/UI/Screen28_DroneMonitorUiTests.cs`](../../Assets/VRShooting/Tests/PlayMode/UI/Screen28_DroneMonitorUiTests.cs)
- [`Assets/VRShooting/Tests/Support/P3Fixtures.cs`](../../Assets/VRShooting/Tests/Support/P3Fixtures.cs)
- [`docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md`](../../docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md)
- [`docs/BDD/screens/02-游戏主界面.feature.md`](../../docs/BDD/screens/02-游戏主界面.feature.md)
- [`docs/BDD/screens/04-100m任务说明.feature.md`](../../docs/BDD/screens/04-100m任务说明.feature.md)
- [`docs/BDD/screens/12-堑壕地图选择.feature.md`](../../docs/BDD/screens/12-堑壕地图选择.feature.md)
- [`docs/BDD/screens/18-城镇地图选择.feature.md`](../../docs/BDD/screens/18-城镇地图选择.feature.md)
- [`docs/BDD/screens/20-建筑搜索HUD.feature.md`](../../docs/BDD/screens/20-建筑搜索HUD.feature.md)
- [`docs/BDD/screens/21-城镇任务结算.feature.md`](../../docs/BDD/screens/21-城镇任务结算.feature.md)
- [`docs/BDD/screens/28-模式三无人机开场侦察.feature.md`](../../docs/BDD/screens/28-模式三无人机开场侦察.feature.md)
- [`docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md`](../../docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md)
- [`docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/UI/task011-urban-building-hud-and-results.md`](../../docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/UI/task011-urban-building-hud-and-results.md)
- [`docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/UI/task018-mode3-drone-monitor-ui.md`](../../docs/任务规格/阶段三-P3-小型堑壕与城镇战斗原型/UI/task018-mode3-drone-monitor-ui.md)
- [`docs/接口文档/00-UI与玩法服务层交互总约束.md`](../../docs/接口文档/00-UI与玩法服务层交互总约束.md)
- [`docs/接口文档/01-页面导航与UI事件.md`](../../docs/接口文档/01-页面导航与UI事件.md)
- [`docs/接口文档/06-武器与弹药服务.md`](../../docs/接口文档/06-武器与弹药服务.md)
- [`docs/接口文档/09-城镇任务服务.md`](../../docs/接口文档/09-城镇任务服务.md)
- [`docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md`](../../docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md)
- [`docs/接口文档/20-模式三无人机开场侦察.md`](../../docs/接口文档/20-模式三无人机开场侦察.md)

另新增本报告与 `docs/codex-reports/evidence/ui-chinese-copy-2026-11-12/` 下的测试结果、逐项对照、审计记录和真实截图；没有新增生产资源或第三方依赖。

## 测试与完整 fullname 基线比对

基线来源为仓库现存 `mode1-brief-results-labels-2026-11-12/editmode-full.xml` 和 `playmode-full.xml`，数量与本次指定的384/364/20及313/290/23完全一致。基线文件路径与SHA-256均记录在比较JSON中。比较采用完整 fullname（包括参数），不按短方法名或总失败数量归类。

| 运行 | 总数 | 通过 | 失败 | 跳过 | 新增失败 |
|---|---:|---:|---:|---:|---:|
| 定向EditMode | 11 | 11 | 0 | 0 | 0 |
| 定向PlayMode | 38 | 38 | 0 | 0 | 0 |
| EditMode基线 | 384 | 364 | 20 | 0 | — |
| EditMode本次全量 | 385 | 365 | 20 | 0 | 0 |
| PlayMode基线 | 313 | 290 | 23 | 0 | — |
| PlayMode本次全量 | 315 | 292 | 23 | 0 | 0 |

原始结果：[EditMode定向](evidence/ui-chinese-copy-2026-11-12/editmode-targeted.xml)、[PlayMode定向](evidence/ui-chinese-copy-2026-11-12/playmode-targeted.xml)、[EditMode全量](evidence/ui-chinese-copy-2026-11-12/editmode-full.xml)、[PlayMode全量](evidence/ui-chinese-copy-2026-11-12/playmode-full.xml)。

逐条对照：[baseline-comparison.json](evidence/ui-chinese-copy-2026-11-12/baseline-comparison.json)、[fullname-comparison.csv](evidence/ui-chinese-copy-2026-11-12/fullname-comparison.csv)。CSV覆盖全部700个测试 fullname，包含基线/本次状态及两侧原始失败消息。现存20+23项失败均为同名基线失败，本任务未将它们删除、跳过或放宽断言。

先增加定义层断言并执行红灯验证，结果为1条预期失败（floor-1尚未中文化），见 [editmode-red.xml](evidence/ui-chinese-copy-2026-11-12/editmode-red.xml)。一次中间定向尝试暴露测试取值对象与画廊场景常量名称的编译错误，已纠正；中间无效尝试不计入最终测试数据。

新增测试：

- `SimulatedShooting.Tests.EditMode.CombatSceneTests.Bdd20_ChineseDisplayNamesPreserveSceneAndMapIdentifiers`：通过。
- `VRShooting.Tests.PlayMode.TacticalUIArtTests.ChineseErrorCopyCoversEveryCodeAndPreservesChineseDetails`：通过。
- `VRShooting.Tests.PlayMode.TacticalUIArtTests.Screen20_ChineseFloorAndRoomCopyPreservesDoorCommandIdentifiers`：通过。

原有用例同步增加/更新严格断言：菜单副标题与37空格底部文案、侧握键、简报标题、射击位显示与原Id不变、无人机实时视频及中文异常、武器显示名。新增HUD用例覆盖三层五房、开门命令原Id、未知楼层及空层回退；错误用例枚举覆盖全部现存ErrorCode。

## 真实渲染截图

复用现有 `VRShooting.Editor.TacticalUIGallery.CaptureBatch`，串行启动单个Unity 2022.3.62f3c1，用Camera.Render和RenderTexture直接生成1920×1080 PNG。没有P图、裁切、重画文字或修改像素。画廊建筑HUD改用实际CombatSceneBindings经生产CombatSceneDefinitionBuilder输出的楼层/房间DTO；无人机背景由现有工具渲染当前堑壕场景静帧，UI使用既有预览DTO，属于画廊预览截图，不冒充正在运行的完整飞行录像。

| 要求画面 | 图片 | 已核对内容 |
|---|---|---|
| 主菜单 | [Screen_MainMenu.png](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_MainMenu.png) | 副标题仅战术训练中心，底部无英文副标题 |
| 操作介绍 | [Screen_MainMenu_ControlsOpen.png](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_MainMenu_ControlsOpen.png) | 侧握键，键位及单位保留 |
| 模式一任务简报 | [Screen_ZeroingBriefing.png](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_ZeroingBriefing.png) | 标题仅任务简报 |
| 城镇建筑HUD | [Screen_UrbanBuildingHud.png](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_UrbanBuildingHud.png) | 当前楼层：一层，一层1号/一层2号；原键位提示保留 |
| 无人机侦察 | [Screen_TrenchDroneRecon.png](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_TrenchDroneRecon.png) | 实时视频，无LIVE；遥测单位保留 |
| 补充：城镇结算 | [Screen_UrbanResults.png](evidence/ui-chinese-copy-2026-11-12/gallery/Screen_UrbanResults.png) | 三层缩略图标题中文化 |

完整画廊保留22张真实渲染截图；尺寸及SHA-256见 [screenshot-manifest.json](evidence/ui-chinese-copy-2026-11-12/screenshot-manifest.json)，画廊实际渲染文字见 [gallery-rendered-text.txt](evidence/ui-chinese-copy-2026-11-12/gallery-rendered-text.txt)。

## 全仓英文残留复查与保留理由

核查仓库6107个跟踪文件清单中的1596个受支持文本文件，按用户要求排除79个Samples文本文件，实际读取/检索1517个非Samples文本文件；同时检查两套Runtime实际源码与场景/Prefab序列化文字。复查代码中的动态拼接、DisplayName、UI错误显示、调试GUI与生成器。扫描详情见 [english-audit-summary.json](evidence/ui-chinese-copy-2026-11-12/english-audit-summary.json)。

当前训练界面中未发现需要继续中文化的英文文案。下列为玩家可见的明确保留项；内部英文与模板资源的逐项明细继续列在后面。

| 保留项 | 原因 |
|---|---|
| VR | 用户明确要求保留的通用缩写 |
| UI | 用户明确要求保留的通用缩写 |
| HUD | 与VR/UI同类的通用界面缩写，不属于英文副标题 |
| X | 手柄键位，原样保留 |
| Y | 手柄键位，原样保留；弹着坐标说明中原X/Y已中文化 |
| A | 手柄键位，原样保留；地图A已改甲 |
| B | 手柄键位，原样保留；地图B已改乙 |
| R | 桌面换弹键，原样保留 |
| H | 桌面手雷键，原样保留 |
| Enter | 交互键，完整键位提示逐字保留 |
| WASD | 调试操作中的键位组，原样保留 |
| RMB | 调试操作中的鼠标右键标识，原样保留 |
| Shift | 调试瞄准键，原样保留 |
| LMB | 调试操作中的鼠标左键标识，原样保留 |
| F | 调试击发键，原样保留 |
| Q | 调试换肩键，原样保留 |
| 100m | 用户明确要求不统一单位 |
| 50cm | 用户明确要求不统一单位 |
| 2cm | 用户明确要求不统一单位 |
| 0.32cm | 用户明确要求不统一单位 |
| 10cm | 用户明确要求不统一单位 |
| 5° | 用户明确要求不统一单位 |
| m/s | 速度单位，按用户口径保留 |
| 50cm x 50cm | 尺寸及小写x按用户口径保留 |
| 0/25/50/75/100 m，-20/+20 m | 场景距离单位保留，仅LEFT/RIGHT中文化 |
| m、s | 无人机高度/用时单位保留，不统一成中文 |

内部键保留：FloorId/RoomId/PointId、floor-1/2/3、room-11/12/21/22/31、urban-a、trench-a、urban-1f/2f/3f及测试ID/资源键均保持原样。`Hud_UrbanResults_1F`、`Text_UrbanResults_1F`等是节点/测试标识，玩家看到的标题已改为一层。未知楼层回退原Id是用户明确要求，生产已知三层均有中文显示名。未知房间回退“房间+序号”，不会显示英文房间键。

以下逐项列出序列化资源中的英文保留项（含重复出现的位置）。GameScene/MainScene内XR模板教学文字和VRTemplateAssets模板控件均按用户明确豁免保留，Samples未修改；其余为单位。

| 文件:行 | 保留原文 | 保留理由 |
|---|---|---|
| `Assets/Scenes/GameScene.unity:730` | ray | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:1365` | gaze | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:2082` | poke | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:9990` | There are two core components that are critical to building XR interactions; | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:10991` | The <b>Complete XR Origin Setup</b> contains everything needed for a fully | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:12300` | <b>Interactable</b> components define how the user can interact with objects | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:13334` | This includes head-mounted device tracking, controller presence, interactors, | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:13639` | That’s it! <br>We can’t wait to see what XR experiences you build!  | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:14400` | An <b>interactor</b> component controls how a GameObject interacts with | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:17038` | Continue | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/GameScene.unity:17538` | Welcome to the VR Template! <br>Let’s learn about some of the features. | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/MainScene.unity:300` | Press K to enter the game | 用户明确排除的 XR 模板及模板控件 |
| `Assets/Scenes/MovingTargetRangeScene.unity:21517` | 100 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:34854` | 50 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:37396` | 左端  -20 m  < | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:40644` | 75 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:41109` | 0 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:57690` | >  +20 m  右端 | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:57788` | 75 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:59387` | 100 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:65042` | 25 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:66031` | 0 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:69674` | 50 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/MovingTargetRangeScene.unity:70313` | 25 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:21256` | 100 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:34381` | 50 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:37006` | 左端  -20 m  < | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:40254` | 75 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:40719` | 0 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:57563` | >  +20 m  右端 | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:57661` | 75 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:59343` | 100 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:65252` | 25 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:66076` | 0 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:69720` | 50 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene.unity:70359` | 25 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:1683` | 25 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:2336` | 25 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:11489` | 50 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:16795` | 100 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:19218` | 0 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:20336` | 50 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:26842` | 100 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:27687` | 0 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:30975` | 75 m | 用户要求保留的单位/尺寸 |
| `Assets/Scenes/ZeroingRangeScene_Backup_20260816.unity:32028` | 75 m | 用户要求保留的单位/尺寸 |
| `Assets/VRShooting/Prefabs/UI/Training_SharedWorldSpaceUI.prefab:69` | 100m射校 训练结算 | 用户要求保留的单位/尺寸 |
| `Assets/VRTemplateAssets/Prefabs/Affordance/Affordance Callout.prefab:424` | Callout | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/TutorialPlayer/Tutorial Player.prefab:314` | Default Input Controls | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/Dropdown.prefab:185` | Option A | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/Dropdown.prefab:355` | Option A | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/Dropdown.prefab:357` | Option B | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/Dropdown.prefab:359` | Option C | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/Dropdown.prefab:702` | Option A | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Button.prefab:69` | Text Button | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Button.prefab:342` | Button | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Dropdown.prefab:116` | Dropdown | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Slider.prefab:116` | Slider | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Slider.prefab:694` | Value | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Slider.prefab:906` | type | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/List Item Toggle.prefab:581` | Toggle | 用户明确排除的 XR 模板及模板控件 |
| `Assets/VRTemplateAssets/Prefabs/UI/Spatial Panel Scroll.prefab:1557` | Spatial Panel | 用户明确排除的 XR 模板及模板控件 |

运行时源码的英文/混合语言字符串候选逐项复核如下。候选包含单位、键位、内部错误、资源绑定、Inspector名和字符串插值表达式；\n等转义已解码，避免将换行转义误认为英文。完整机器可读记录见 [runtime-english-candidates.json](evidence/ui-chinese-copy-2026-11-12/runtime-english-candidates.json)。

| 文件:行 | 保留字符串/候选 | 保留理由 |
|---|---|---|
| `Assets/VRShooting/Runtime/Application/GameStateManager.cs:42` | [{nameof(GameStateManager)}] 状态初始化为: {CurrentState} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/GameStateManager.cs:65` | [{nameof(GameStateManager)}] 状态改变为: {newState} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetHudService.cs:53` | session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetHudService.cs:58` | hud only supports moving target mode | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetProgressCoordinator.cs:33` | an active moving target session is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:35` | speed must be 3, 4 or 5 m/s | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:46` | active session is not a moving target session | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:55` | moving target session already started | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:56` | cannot change speed or restart during an active moving target session | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:118` | deltaTime must be a finite non-negative value | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:131` | session has been cancelled | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:185` | sequenceId and shotIndexInSequence are required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:198` | shot is not valid | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:206` | shots are not scored during countdown, endpoint hold or results | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:214` | fire sequence already stopped | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:258` | fire sequence not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:290` | route is not complete | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:315` | moving target session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/MovingTargetService.cs:320` | sessionId is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:40` | presentation only supports P1/P2 modes | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:66` | presentation not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:73` | presentation not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:83` | enter a P1/P2 range before confirming start | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:95` | start is only valid while awaiting confirmation | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:103` | session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:162` | next round is only valid for 100m zeroing | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:167` | next round is only valid during round review | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:201` | countdown is not pending | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:219` | training is not in a completable phase | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:237` | retry is only valid from session results | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:256` | presentation not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:263` | presentation not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:300` | locomotion policy not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:338` | presentation not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:348` | confirm start before picking up the weapon | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:354` | weapon pickup session does not match | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:362` | weapon pickup does not match the training rifle | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:369` | rear-hand pickup is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:399` | weapon pickup is not valid in the current phase | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:407` | presentation not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:413` | session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingPresentationService.cs:419` | session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:30` | mode is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:37` | active session already exists | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:89` | cannot start from state {session.State} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:117` | cannot pause from state {session.State} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:144` | cannot resume from state {session.State} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:165` | session already ended | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:209` | sessionId is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingSessionService.cs:215` | session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/TrainingWeaponFireCoordinator.cs:51` | session id is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/UIRouter.cs:30` | router is transitioning | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/UIRouter.cs:69` | router is transitioning | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/UIRouter.cs:74` | no previous screen | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/UIRouter.cs:136` | apply adjustment is only valid on impact analysis | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/UIRouter.cs:163` | unsupported ui event: {eventId} from {sourceScreen} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingHudService.cs:58` | session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingHudService.cs:63` | hud only supports zeroing mode | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingHudService.cs:280` | 已记录 {Math.Min(shots.Count, ZeroingRules.ShotsPerRound)}/{ZeroingRules.ShotsPerRound} | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:57` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:67` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:77` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:82` | round requires 4 shots | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:92` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:94` | round cannot be adjusted | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:97` | round cannot be adjusted | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:100` | invalid adjustment request | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:115` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:120` | round requires 4 shots | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:126` | round index mismatch | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:130` | round cannot be adjusted | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:150` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:154` | round requires 4 shots | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:160` | adjustment must be applied first | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:182` | zeroing session not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:267` | shooting is not allowed in the current presentation phase | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/ZeroingService.cs:272` | round is already complete | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Input/CombatInputController.cs:27` | Session required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Input/CombatInputController.cs:28` | P3 mode required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Input/CombatInputController.cs:36` | Invalid combat configuration | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Input/CombatInputController.cs:145` | Invalid combat input:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Input/ManualXRTrainingInput.cs:111` | unsupported input button | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Unity/Bootstrap/GameMain.cs:29` | XR Interaction Manager | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/Combat/CombatCharacterLocomotion.cs:18` | Character controller unavailable | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/VRShooting/Runtime/Unity/Combat/CombatPhysicsShotQuery.cs:16` | Include environment layers and a finite positive range | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/VRShooting/Runtime/Unity/Scene/GameStartTrigger.cs:23` | [{nameof(GameStartTrigger)}] 未找到名为 {CanvasChildName} 的子物体。 | 开发日志，非玩家界面 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:364` | 100m精度射校靶 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:527` | VR 射击训练系统 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:534` | 01   100m 精度射校靶 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:562` | 通用 VR 操作<br>右手侧握键抓枪 / 主手握持；左手侧握键副手握持。<br>右扳机射击；用 UI 射线选择按钮。<br>主菜单及模式三 / 四：左摇杆移动，右摇杆转向。<br>模式一 / 二为固定射击点，不可移动。<br><br>模式一  ·  100m 精度射校<br>每轮 4 发，共 3 轮。<br>分析页使用“应用调整”“进入下一轮”或“查看评级”。<br>水平 1 格 = 2cm；垂直 5 度 = 0.32cm。<br>集中且计算集各点距靶心≤10cm、均值距靶心≤5cm且至少一发距靶心≤5cm，或应用调整后均值距靶心<5cm即可通过。 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:565` | 模式二  ·  移动目标<br>双手据枪，抬枪对齐瞄具并跟随目标。<br>右扳机快速扣动起射两发，继续按住可逐发连射。<br><br>模式三 / 四  ·  堑壕与城镇<br>左手柄 X：换弹；左手柄 Y：切换姿态。<br>右手柄 B：切换左右肩。<br>模式四：右摇杆按压交互 / 开门。<br>模式三 / 四：右手柄 A 请求队友投掷手雷。<br>切换左肩后，左手为主手并使用左扳机射击，右手为副手。 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:579` | 100m精度射校靶 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:582` | 射击距离：100m<br>射击模式：单发射<br>每轮弹数：4发<br>训练轮次：共3轮<br>目标规格：50cm x 50cm 胸靶<br>10环直径：10cm<br>集中：各点距均值<10cm，两两间距<15cm<br>通过条件：集中且各点距靶心≤10cm、均值距靶心≤5cm、至少一发距靶心≤5cm；或集中且调整后均值距靶心<5cm | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:609` | 50cm x 50cm | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:658` | 50cm x 50cm 胸靶 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:672` | 弹着点偏左时觇孔向前调整，弹着点偏右时觇孔向后调整，100m射击距离时觇孔调整一格为两厘米。<br><br>弹着点偏上时逆时针调整准心柱，弹着点偏下时顺时针调整准心柱，100m射击距离时准心柱调整一圈约为23厘米，每次调整5度约0.32厘米。 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:747` | 100m射校 训练结算 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1097` | 应用后均值距靶心小于5cm可通过 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1504` | 居中 0cm | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1514` | 居中 0cm | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1535` | 小于5cm，应用后通过 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1535` | 不小于5cm，尚未通过 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1772` | Assets/VRShooting/Art/Fonts/MSYH SDF.asset | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1806` | Microsoft YaHei UI | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1807` | Microsoft YaHei | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1809` | Noto Sans CJK SC | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1810` | Arial Unicode MS | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs:1828` | MSYH Runtime SDF | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuXRModeController.cs:330` | XR Origin | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuXRModeController.cs:348` | XR Origin | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuXRModeController.cs:375` | XR Origin | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Unity/UI/MovingTargetRangeUI.cs:188` | 射击距离100m  ·  卧姿固定射击  ·  两发起射 / 长按连射  ·  左端禁射 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MovingTargetRangeUI.cs:205` | 当前选择：4 m/s | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/MovingTargetUIPresenter.cs:320` | 评级：{0}<br>总射击：{1}    命中：{2}    命中率：{3:0.#}%<br>速度：{4:0.#} m/s    用时：{5:0.0} 秒 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs:224` | P3CombatUIRoot requires a Canvas | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/VRShooting/Runtime/Unity/UI/P3DroneReconView.cs:51` |  m   速度  | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/P3TrenchHudAndResultsUI.cs:97` |  · R / 左手柄X键换弹 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/P3TrenchHudAndResultsUI.cs:139` | 交互：右摇杆按压/Enter/空格 · 手雷：右A/H | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/P3TrenchUI.cs:556` | 无法打开堑壕 HUD | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/TrainingPresentationView.cs:128` | 射击位：100m射校 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/VRShooting/Runtime/Unity/UI/TrainingUIAnchorBinding.cs:95` | UI root references are incomplete. | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/VRShooting/Runtime/Unity/UI/TrainingUIAnchorBinding.cs:100` | LargePanel and MinimalHud anchors must be independent. | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/VRShooting/Runtime/Unity/UI/TrainingUIAnchorBinding.cs:118` |  anchor has the wrong slot. | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/VRShooting/Runtime/Unity/UI/TrainingUICanvasAdapter.cs:293` | XR Origin | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs:81` | bullet damage must reduce representable enemy health | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs:83` | damage must reduce representable health | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs:108` | posture speeds | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs:109` | posture heights | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs:111` | frame budgets | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatConfigDto.cs:152` | Invalid combat configuration:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:38` | Player spawn | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:45` | Projection bounds/key/floor | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:52` | Trench search nodes/floors | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:58` | Entrance/three floors | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:59` | Entrance position | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:68` | Room entry position | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:72` | Room map position | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:77` | Total rooms must be 3-5 | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:81` | Spawn ID/estimate | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:86` | Spawn group/mode | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:89` | Spawn FloorId | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:90` | Spawn RoomId | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:92` | Outdoor spawn floor/room | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:93` | Spawn position | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:96` | Invalid spawn candidate:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:100` | Insufficient spawn candidates for maximum configured encounter | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Contracts/P3/CombatSceneDefinitionDto.cs:119` | Invalid scene configuration:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatApplicationCoordinator.cs:228` | Combat application:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatApplicationCoordinator.cs:236` | P3 production scene loader is not registered | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs:53` | Invalid combat configuration | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs:235` | Invalid grenade explosion | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs:236` | Resolve grenade at an Advance boundary | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatCoreService.cs:507` | Combat request rejected:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatMission.cs:43` | Only P3 modes can be composed | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatMission.cs:69` | Trench drone scene port is missing | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatSummaryFileStore.cs:40` | Unable to save latest combat result | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatSummaryFileStore.cs:54` | Invalid combat summary format | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/CombatSummaryFileStore.cs:61` | Unable to read latest combat result | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/SquadFormationService.cs:32` | Invalid configuration | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/SquadFormationService.cs:194` | Squad request rejected:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/SquadGrenadeTacticService.cs:35` | Invalid configuration | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/SquadGrenadeTacticService.cs:118` | Grenade tactic rejected:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/TrenchService.cs:355` | Trench request rejected:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Combat/UrbanService.cs:315` | Urban request rejected:  | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponAutomaticFireService.cs:49` | session id is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponAutomaticFireService.cs:57` | shot interval must be a finite positive value | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponAutomaticFireService.cs:65` | P2 initial shot count must be 2 | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponAutomaticFireService.cs:136` | deltaTime must be a finite non-negative value | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponAutomaticFireService.cs:423` | weapon auto-fire session is not initialized | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:37` | weapon not found | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:70` | weapon is not applicable to mode | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:83` | session id is required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:95` | weapon is not applicable to mode | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:174` | invalid muzzle or aim direction | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:181` | weapon cannot shoot | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:189` | not enough ammo | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:345` | amount must be positive | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:350` | not enough ammo | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:371` | reservation id and a positive amount are required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:390` | not enough ammo to reserve initial shots | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:413` | reservation id and a positive amount are required | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:418` | reservation cannot consume that amount | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:462` | reload already started | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:472` | no reserve ammo | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:575` | weapon session is not initialized | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:716` | an unheld weapon cannot report tracked grip hands | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:724` | rear-hand hold requires only the rear hand | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:732` | two-hand hold requires both tracked hands | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/VRShooting/Runtime/Application/Weapons/WeaponControlService.cs:738` | unsupported weapon hold state | 内部契约校验/服务诊断；玩家错误提示已在 UI 显示边界改为中文，键和数据不改 |
| `Assets/SimulatedShooting/Runtime/Scene/BallisticTracerVisual.cs:232` | Universal Render Pipeline/Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/ChestTargetArtwork.cs:18` | Universal Render Pipeline/Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/ChestTargetArtwork.cs:62` | Universal Render Pipeline/Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatExplosionVfx.cs:43` | Particles/Standard Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatExplosionVfx.cs:43` | Universal Render Pipeline/Particles/Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatExplosionVfx.cs:54` | Missing heavy grenade explosion audio:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatRifleReloadPresentation.cs:42` | Missing removable magazine on combat rifle | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:77` | Missing point | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:78` | Empty Id:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:79` | Duplicate Id:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:80` | Missing volume:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:81` | Missing door:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:82` | Missing region:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:84` | Missing map:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:88` | Outside map:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:91` | Expected two teammate spawns | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:95` | Missing door | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:96` | Door hinge:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:97` | Door obstacle:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:101` | Missing map | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:102` | Invalid map:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneBindings.cs:103` | Duplicate map Id:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneDefinitionBuilder.cs:34` | Missing estimate anchor:  | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:218` | Training rifle binding missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:220` | M67 grenade model missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:222` | Grenade explosion audio missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:646` | Grenade model resource is missing. | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:655` | Grenade prefab has no usable mesh bounds:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:657` | Grenade prefab has no renderers:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/CombatSceneRuntime.cs:662` | Universal Render Pipeline/Lit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs:367` | Failed to start weapon session: {start.Message} | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs:1229` | Universal Render Pipeline/Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs:1246` | 弹匣 {currentState.CurrentMagazine}/3  备用 {currentState.ReserveAmmo}  {(currentState.CanShoot ?  | 字符串插值中的 C# 字段/表达式，字段名不作为文字输出 |
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs:1248` | )}  稳定度 {currentState.Stability01:0.00} | 字符串插值中的 C# 字段/表达式，字段名不作为文字输出 |
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs:1250` | 鼠标/方向键观察，WASD副手，RMB/Shift瞄准，LMB/F射击，R换弹，Q换肩 | 用户要求保留的键位、VR/UI 缩写、单位或字符串格式/插值代码；逐项人工复核 |
| `Assets/SimulatedShooting/Runtime/Scene/FirstPersonTrainingWeaponController.cs:1252` | 最近一发：{(lastShot.Hit ?  | 字符串插值中的 C# 字段/表达式，字段名不作为文字输出 |
| `Assets/SimulatedShooting/Runtime/Scene/FixedProneLocomotionGuard.cs:32` | Enforce Fixed Prone Locomotion Policy | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:76` | Missing drone, geometry or current session | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:78` | Drone route/feed binding does not match this scene | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:80` | Invalid drone flight/video configuration | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:82` | Invalid authored recon waypoint | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:89` | No navigable launch point near the squad | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:100` | Full trench survey route is empty | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:113` | Drone RenderTexture allocation failed | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:115` | Recorded DJI rotor audio is missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:138` | Empty flight step | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:147` | Invalid flight clock delta | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:148` | Live drone feed became unavailable | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:161` | Drone route blocked by scene geometry | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:172` | Drone route left the configured map bounds | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:173` | Drone route blocked by scene geometry | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:197` | Scene runtime missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/Mode3DroneReconScene.cs:208` | Player camera missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/MovingTargetFakeTimeline.cs:44` | Preview Next Fake Timeline Frame | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/MuzzleBlastVfx.cs:94` | Particles/Standard Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/PicoStandaloneRenderPolicy.cs:69` | [PICO Render] eye= | 开发日志，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/RealisticProjectileVisual.cs:82` | Universal Render Pipeline/Lit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/TrainingRangeSceneBindings.cs:75` | {names[index]} is missing | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/TrainingRangeSceneBindings.cs:79` | {names[index]} has a duplicate Transform binding ({anchor.name}) | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/TrainingRangeSceneBindings.cs:89` | Validate Training Range Bindings | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/UnityCombatSceneLoader.cs:32` | Combat scenes require Play Mode. | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/UnityCombatSceneLoader.cs:62` | P3 scene load:  | 内部异常/配置校验诊断，非玩家文案；原始诊断保留 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:571` | 掌骨 Metacarpal | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:573` | 近端 Proximal | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:575` | 中段 Intermediate | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:577` | 远端 Distal | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:617` | 拇指 Thumb | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:619` | 食指 Index | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:621` | 中指 Middle | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:623` | 无名指 Ring | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRControllerHandVisual.cs:625` | 小指 Little | 仅 Unity Inspector/编辑器菜单可见，非玩家界面 |
| `Assets/SimulatedShooting/Runtime/Scene/VRHandPresentation.cs:36` | Left Controller | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/VRHandPresentation.cs:36` | Right Controller | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/WeaponFeedbackController.cs:193` | ActionVFX Blood Mist material is missing. | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/WeaponFeedbackController.cs:397` | Universal Render Pipeline/Unlit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/WeaponFeedbackController.cs:398` | Universal Render Pipeline/Lit | 资源路径/Shader、字体或场景绑定名称，非玩家文字 |
| `Assets/SimulatedShooting/Runtime/Scene/ZeroingRangeSessionBootstrap.cs:83` | [{nameof(ZeroingRangeSessionBootstrap)}] failed to start zeroing session: {zeroing.Message} | 异常/日志/配置诊断，不直接显示给玩家 |
| `Assets/SimulatedShooting/Runtime/Scene/ZeroingRangeSessionBootstrap.cs:110` | [{nameof(ZeroingRangeSessionBootstrap)}] failed to start weapon session: {weapon.Message} | 异常/日志/配置诊断，不直接显示给玩家 |

仓库文档中的C#类型名、接口/枚举/方法名、字段名、路径、测试fullname、工具命令与历史报告/历史截图仍有英文：这些是开发契约或历史证据，不是当前玩家文案。没有为了扫描清零而重写技术契约、ID或历史记录。UI线框/参考图仍作为历史布局依据保留，实际文字以本次同步BDD和接口约定为准。

## 运行隔离与工作区清理

所有测试和画廊均在检查无Unity进程后串行运行，使用隐藏窗口；测试参数为 `-batchmode -projectPath <repo> -runTests -testPlatform EditMode/PlayMode -testResults <xml> -logFile <log>`，定向另加testFilter；测试未使用-quit或-nographics。画廊使用 `-executeMethod VRShooting.Editor.TacticalUIGallery.CaptureBatch -quit`，`RUNTIME_GALLERY_STATES=1`且输出到本次证据目录。日志与运行脚本保存在本地忽略目录 `Logs/ui-chinese-copy-2026-11-12/`。

结束前已恢复本次跑批改写的TrainingChinese.asset、Open XR Package Settings.asset及7张旧证据PNG。3个容易被跑批修改的XR Prefab经核查与开工备份一致；ProjectSettings没有本次改动。4个跟踪历史InitTestScene及其4个meta逐个SHA-256与开工一致，没有删除历史场景，也没有遗留新生成的InitTestScene。清理明细见 [cleanup-audit.json](evidence/ui-chinese-copy-2026-11-12/cleanup-audit.json)。

两个靶场场景相对开工状态分别仅有2处m_Text替换；其余序列化布局、资源引用和场景数据保持不变。源文件保留各自开工时的BOM/换行风格。git diff --check通过；结束时无Unity实例。最终工作区审计见 [workspace-audit.json](evidence/ui-chinese-copy-2026-11-12/workspace-audit.json)，unexpected_changes为空；除开工已有内容外，仅保留本次43个文件修改、新证据目录和本报告。

## 保留的基线失败 fullname

下列43条本次仍失败，均可在完整CSV中核对同名基线失败及双方原始消息；未出现额外失败或基线用例缺失。

| 平台 | 完整fullname | 基线 | 本次 |
|---|---|---|---|
| editmode | `SimulatedShooting.Tests.EditMode.CombatSceneTests.Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/MovingTargetRangeScene.unity","MovingTargetRange")` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRangeSceneBindingsTests.Bdd31_RangeKeepsTheRearTargetRowAndBackground("Assets/Scenes/ZeroingRangeScene.unity","ZeroingRange")` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/MovingTargetRangeScene.unity")` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget("Assets/Scenes/ZeroingRangeScene.unity")` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.TrainingRifleMuzzleAlignmentTests.SharedPrefabMuzzleMatchesActualMeshOpening` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.VrMechanicalSightCalibrationTests.Bdd33_ImportedMeshHasAnOpenRearApertureAndTheMeasuredFrontPost` | Failed | Failed |
| editmode | `SimulatedShooting.Tests.EditMode.VrMechanicalSightCalibrationTests.Bdd33_PrefabAimLineMatchesVisibleApertureAndFrontPostAtOneHundredMetres` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Trench)` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.CombatMagazineReloadTests.AutomaticFireRequiresTwoRoundsAndReloadCancelsHeldSequence(Urban)` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.DeathCompetesWithLastCheckAndKill_AndWins` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(False)` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RandomCountEndpoints(True)` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RemainingStreetEnemyBlocksVictory_ThenResultHasThreeFloorMaps` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RetryResetsDoorsAndSnapshotsWithoutChangingOldResult` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.RiskyEntryAndReturnPreserveSessionAmmoAndStreetEnemies` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen25_UrbanTests.SeedAndOwnershipAreStableAndCountsWithinBounds` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.QuickSwitchDisposesOnlyStaleLease_AndCannotOverwriteUrban` | Failed | Failed |
| editmode | `VRShooting.Tests.EditMode.Screen26_CombatApplicationTests.ThreeRetriesUseNewSessions_RejectOldFacts_AndSaveOnlyLatest(Urban)` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.BunkersTrenchPlayModeTests.Bdd14_NewTrenchAllowsContinuousMovementAndFiring` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.BunkersTrenchPlayModeTests.NewSceneInspectionWalkerCanLeaveEntry` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.CombatScenePlayModeTests.Bdd15_17_21_HitDedupCorpseRetentionAndRetryCleanup` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.CombatScenePlayModeTests.Bdd15_20_WallsBlockPerceptionAndSceneUnloadRemovesActors` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.MovingTargetRangeIntegrationTests.P2_PhysicalImpactUsesCollisionPoint_EnvironmentDoesNotScore` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.MovingTargetRangeSceneTests.Bdd31_DayHud_RestoresTheOriginalRearRowAndFortyMetreRoute` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.MovingTargetRangeSceneTests.Task003_StartAreaUsesTheExactZeroingRangeForegroundCopy` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen14_18_CancelLoadingThenSwitchModeKeepsExclusiveScene` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_DeathThreeRetriesTrackingAndMovement` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.P3ProductionFlowTests.Screen21_Urban_ProductionVictoryRetryAndReturn` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.VrZeroingMechanicalSightTests.Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.XRSceneHandoffTests.Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingOctober07RegressionTests.Bdd30_CentralSightActuallyZerosOnTheNextRoundWithMatchingVisibleHoles` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd04_PickupPromptFacesShooterAboveRifleWithoutChangingText` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Bdd31_TargetKeepsItsDimensionsInTheOriginalRearRow` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task005_TrainingRifleUsesLicensedQbz191Visual` | Failed | Failed |
| playmode | `SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests.Task013_DirectGrabRequiresRightRearThenLeftFrontAndDropsOnRearRelease` | Failed | Failed |
| playmode | `VRShooting.Tests.PlayMode.Screen25_UrbanPlayModeTests.BuildingFailureKeepsCountsAndOldSessionInputsAreRejected` | Failed | Failed |
| playmode | `VRShooting.Tests.PlayMode.Screen26_CombatCompositionPlayModeTests.UrbanButtonsPreserveStreetSessionThroughRoomsAndRetry` | Failed | Failed |
| playmode | `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen18_UrbanMapSelection_RendersRangesAndStartsStreet` | Failed | Failed |
| playmode | `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands` | Failed | Failed |
| playmode | `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests.Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce` | Failed | Failed |
