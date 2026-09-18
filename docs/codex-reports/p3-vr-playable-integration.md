# P3 双模式可测试候选交付

日期：2026-09-17（合并复验更新）。执行：Codex；性质：实现方自测，不代替独立审核签核。

后续实机反馈暴露了本文原验证未覆盖的头手动作资产切换、交互组注册和近距抓枪问题。最新修复与 XR 输入链路证据以 [P2/P3 XR 输入修复报告](xr-input-handoff-fix.md) 为准；下文早先的 VR 模式识别不代表实际头手/UI 操作已经通过。

## 候选范围

工作分支 `fix/p3-vr-playable-integration`，已快进合入 main `ae49519`（PR #25，包含最新场景 `d6fd8dc`），并在新场景上恢复本地 VR 生产装配。Unity `2022.3.62f3c1`，Windows Editor、OpenXR、XRI 3.1.2。本地 VR 接入修改尚未提交或推送到 main。

此前 main 有玩法服务和独立 UI，但缺少正式场景装配，不能从菜单完整测试 P3。本次接入 `CombatScene.unity`，两种模式各自拥有独占场景租约，逻辑 SceneId 仍为 TrenchScene/UrbanScene。训练从正式菜单进入，不依赖场景巡检 Fixture。

## 2026-09-17 最新 main 合并复验

- `origin/main` 的 `ae49519` 已快进合入当前分支。CombatScene 按 Unity 对象 ID 合并，保留新主线布局、资源和本地生产绑定；原工作区另有 stash 与 `.git/p3-main-merge-ae49519/` 备份。
- 修复合并后测试程序集缺少 `VRShooting.Contracts` 引用的问题；主线移动敌人出生点后，原预估锚点距离超出契约范围，已重新定位生成锚点，并让安装器重复执行时同步更新。
- 最新代码全量 **EditMode 264/264、PlayMode 165/165** 通过，失败/跳过均为 0，覆盖 P1/P2 回归、新场景通行、双模式胜利、死亡重试及返回。证据：[EditMode](evidence/p3-vr-playable-integration/editmode-main-ae49519.json)、[PlayMode](evidence/p3-vr-playable-integration/playmode-main-ae49519.json)。
- 另行从 MainScene 启动城镇再返回，实际检测到 `IsVr=True`、`UrbanStreetHud`、`Error=None`，有效相机与 AudioListener 各 1。见 [运行状态](evidence/p3-vr-playable-integration/runtime-main-ae49519.json)。已停止 Play，MainScene 为活动场景且无未保存修改。
- 此次 VR 模式冒烟有 1 条 XRI `GazeInputManager` 的“未找到眼动设备”提示；源码第 55 行为 `Debug.LogWarning`（MCP 快照将其标为 Error）。该组件等待可选眼动设备，手柄交互不依赖眼动；未压制日志，见 [Console](evidence/p3-vr-playable-integration/console-main-ae49519.json)。这次不能沿用下文历史桌面冒烟的 0 warning 结论。
- 当前已达到进入 VR 实机测试的候选状态；自动化与运行模式识别不代替真实头手操作、舒适度或设备性能验收。本地 VR 装配修改仍未提交/推送。
- Unity 刷新后自动重写了 OpenXR Standalone 的部分 feature 对象引用；已核对引用存在、所指 feature 名称和启用状态与备份一致。保留该序列化更新，未改变用户的设备配置；原材质内容与备份一致。

下文 257/163 测试结果、截图和桌面 Console 记录为 2026-09-16 合并前证据；最新合并验证以上述记录为准。

## 如何开始测试

1. 打开 `Assets/Scenes/MainScene.unity`，连接已配置的 OpenXR 头显与左右手柄，确保设备运行后再按 Play。
2. **堑壕**：主菜单 → 堑壕 → 选择地图 → 简报 → 开始。拾枪，沿堑壕路线搜索节点并清除敌人，搜索与清敌均完成才胜利；死亡则显示失败。结果页可重试或返回主菜单。
3. **城镇**：主菜单 → 城镇 → 选择地图。清除街道敌人，到建筑入口确认进入；逐层开门、清敌、进入房间确认检查。全部房间检查且全部敌人清除才胜利。结果页可重试或返回。
4. 实机至少分别完成一次胜利、一次死亡、连续三次重试、返回再进入；同时检查下列待验项目。

不要直接运行 `CombatScene` 作为正式训练入口：直接打开它会运行保留的场景巡检工具。正式加载器会禁用该 Fixture，创建真实任务服务与角色。

### 操作

| 操作 | VR（常见双摇杆手柄） | 无 VR Editor |
|---|---|---|
| 移动 / 转向 | 左摇杆 / 右摇杆水平离散转向 | WASD / 左右方向键；握枪后鼠标瞄准 |
| 持枪 | 右 Grip 近距握后握把，再左 Grip 握前握把 | E 切换后手，G 切换前手 |
| 射击 | 右扳机单发 | 鼠标左键单发 |
| 换弹 / 换肩 | 右 primary（A） / secondary（B） | R / Q |
| 入口、开门、房间检查 | 左 primary（X），在有效范围内 | Enter |
| 站 / 蹲 / 卧 | 左 secondary（Y）循环 | C |
| 大型页面按钮 | XR 射线选择 | 鼠标点击 |

VR 姿态命令只调整玩法姿态与角色碰撞体，不强行移动真实头部。键鼠输入也通过同一应用/玩法服务端口，枪线与区域判定由真实场景采集。

## 本次实现与修复

- 接入 task003/006/009/012 场景、NavMesh、角色、门、楼层地图和反馈资源；补齐正式步枪与音效绑定、Build Settings。
- task013：注册正式加载器，开放主菜单两入口，将 P3 UI 接到唯一应用协调器；真实 Session、HUD、结算、重试、返回与摘要保存沿用服务契约。
- P3 专用输入适配、角色移动、跟踪暂停、物理命中、区域事实、感知遮挡、队友导航回执和视觉反馈进入生产端口。
- 修复主菜单相机重新启用、旧交互管理器失效、展示步枪挡路、子弹视觉 Collider 拦截枪线、入口范围改变但 HUD 未刷新的问题。
- 画面复查后修复宽屏 HUD 裁切、城镇队友状态与按钮重叠、范围连接符/地图叉号缺字；进入训练关闭常驻桌面相机，退出时先关闭旧场景相机与交互管理器，避免双音频监听器及重复管理器。
- 出生点初始化同步到玩法服务与队伍；敌情预估使用独立锚点；结果 UI 避免逐帧重复生成地图。
- 取消加载后等待自己的场景卸载，旧操作不隐藏菜单或抢占下一模式的场景。

## 自动化验证与证据边界

全量 EditMode **257/257**、PlayMode **163/163** 通过，失败和跳过均为 0。证据：[EditMode 明细](evidence/p3-vr-playable-integration/editmode.json)、[PlayMode 明细](evidence/p3-vr-playable-integration/playmode.json)。最终 PlayMode 结果包含画面复查后的字体、布局、单音频输出与退出管理器修复，以及 P1/P2 回归。

复现：Unity Test Runner 分别选择全部 EditMode、全部 PlayMode；MCP 使用 `run_tests(mode="EditMode")` 与 `run_tests(mode="PlayMode")`，通过 `get_test_job(include_details=true)` 导出明细。物理路线测试明确使用非 VR 巡检角色，避免连接中的 XR 胶囊体干扰另一个受测角色；正式运行只启用所选模式的角色碰撞体。

| 规格 / 任务 | 实现证据 | 自动化路径 |
|---|---|---|
| BDD14/17/18/21、task013 | 正式菜单、UnityCombatSceneLoader、CombatSceneRuntime、P3LiveUIController | `P3ProductionFlowTests`：双模式胜利/重试/返回、双模式死亡三局与跟踪/移动、取消加载切换模式 |
| BDD23、task004/013 | P3XRTrainingInput、CombatInputController | `P3XRTrainingInputTests`：合成 XR 设备摇杆、ABXY、双 Grip 边沿；既有核心输入测试 |
| BDD24/25、task003/009/013、C05 | CombatSceneDefinitionBuilder 与场景序列化锚点 | `P3ProductionSceneDefinitionTests`：实际候选、导航、楼层、房间、预估偏移和展示步枪碰撞 |
| BDD19/20、task009/012 | 实际几何、CharacterController、NavMesh、房门 | `CombatScenePlayModeTests`：穿越堑壕与每个房间、楼梯、街道、地面、门与反馈 |
| BDD19、task010 | UrbanService 范围事实发布 | `Screen19_EntryRangePublishesPromptWithoutMovementOrElapsedTime` |
| BDD14/18/19/20、task013 | Canvas 边界、桌面输出、模式退出 | `Screen14_18_DesktopSceneHasSingleOutputAndVisiblePageBounds`：分别加载双模式，检查页面边界、单相机/音频输出、城市按钮与队友信息无重叠，返回当帧只剩主菜单交互管理器 |

生产闭环测试加载真实 UI、场景和服务，命中通过真实物理查询，不直接改击杀/搜索计数。为稳定复现，测试会替换输入、时钟与玩家姿态，并把玩家放到作者定义的测试位置；它证明服务和场景装配闭环，**不等于人戴头显连续走完全图**。连续物理通行由场景 CharacterController 测试单独覆盖。真实手柄抓取、机瞄与舒适度仍须实机验收。

### 画面证据

本机 OpenXR 有运行中的 Head Tracking 设备，但检查时头部位置停在地面原点。下列截图在该次 Play 中临时停止 XR 子系统，使用无 VR 相机采集，**不是头显实拍**；未改项目 XR 设置，退出 Play 后下一次运行正常初始化 XR。

- [主菜单两种模式入口](evidence/p3-vr-playable-integration/main-menu.png)
- [堑壕简报](evidence/p3-vr-playable-integration/trench-briefing-final.png)
- [堑壕 HUD 与中央视野](evidence/p3-vr-playable-integration/trench-hud-final.png)
- [城镇街道 HUD](evidence/p3-vr-playable-integration/urban-hud-final.png)

清空 Console 后执行一次桌面视角“MainScene → 城镇街道 → 返回主菜单”，记录为 **0 error / 0 warning**，见 [Console 快照](evidence/p3-vr-playable-integration/console.json)。[运行状态](evidence/p3-vr-playable-integration/runtime-state.json) 确认街道 HUD 激活、单相机、单 AudioListener、EventSystem 正常。这个结论只覆盖该次复验，不代表全部历史日志或真实 VR 设备运行均无警告。测试和截图完成后已退出 Play，保留 `MainScene` 为活动场景。

本次保留工作区原有 OpenXR 设置、材质与 npm 文件修改，没有将它们重置，也没有提交或推送这些文件。

## 第三阶段仍未交付的项目

严格按项目“实现 + 测试 + 独立审核”的完成口径，13 项仍为部分完成，不能把自测通过写成阶段验收通过。

1. **A/B 独立审核**：契约 C01–C16、各任务 BDD 追溯、消费端确认与独立复验签核。
2. **C 集成签核**：审核人员对当前候选复跑双模式，确认摘要保存失败、旧会话隔离及 P1/P2 回归；本报告提供实现方证据，不替审核人勾选。
3. **D VR 实机**：自然头手跟踪、双手持枪/松手/重拾、机瞄、换弹换肩、跟踪丢失恢复、楼梯/拐角、队友遮挡、尸体、HUD 清晰可达及眩晕体验。
4. **D 性能和稳定性**：指定设备/刷新率下 CPU/GPU、GC、Canvas、NavMesh、最大尸体视角、加载卸载、连续多局资源增长测量及优化前后对照。
5. **正式提交与阶段归档**：当前候选尚未合入 main；完成审核后更新对应交付和阶段记录。

第二张地图、多楼建筑、高级 AI、环形队友菜单、复杂装填等仍属于 Later，不是本次完整试跑的前置条件。
