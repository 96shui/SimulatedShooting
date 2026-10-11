# 模式三/四弹匣换弹交付（P3 task020）

基线：main `a3f4bda3eee12b4bbedbba3d6b06567614feb397`。Unity 2022.3.62f3c1；2026-10-11。

## 实现行为

- 每轮总共三个30发弹匣：枪内一个、备用两个，开局30/60。重试恢复库存，模式四街道/建筑共用本轮库存。
- 有效双手持枪后按桌面R或VR左摇杆按压。两秒内禁止射击，左人物手离开护木握紧弹匣，向下拔出、移到玩家视野下方，取新弹匣上推插入，再回到护木。枪体倾斜便于观察动作，真实HMD与控制器不移动。
- 弹数在服务完成计时后才装满，每次消耗一个备用弹匣；提前换弹会丢弃旧弹匣余弹。满弹、无备用、无效持枪、开场锁定与重复请求不会扣备用或重复播放。
- 暂停、追踪失效、终局和退出取消未完成动作、恢复弹匣与人物手，保留原弹数和备用库存。换弹期间扣住扳机不会在结束时自动补射。
- HUD从AmmoDto显示弹数、备用弹匣数量、换弹中与操作提示。

## 分层与资产

`WeaponControlService`负责P3整匣库存；`CombatCoreService`负责启动、完成及取消时序，快照提供开始时间和时长。`CombatRifleReloadPresentation`只采样服务时钟并移动渲染网格/人物手，不修改弹药、伤害或真实输入。

从原QBZ191 OBJ分离3,006个弹匣面与34,301个枪身面，保留原顶点、UV、法线与各自原材质；Prefab绑定MagazineVisual。`VRControllerHandVisual`临时表现锚点与手指弯曲不会改动控制器或XRI交互器；清除时恢复原握姿。桌面复用已有两只人物手资源，VR复用已安装的左人物手。资产来源见[说明](../../Assets/Resources/Combat/Weapons/ATTRIBUTION.md)。

场景安装入口：`Tools/Simulated Shooting/Scene 3/Install Magazine Reload`。Prefab已安装并提交，无需用户手动执行菜单。

P1/P2弹药、单发/两发起射、固定卧姿规则保持。回归期间发现P2既有场景缺少P1的非交互WeaponReference_Blockout参考锚点，按原样补齐；同时将已有契约测试中过时的玩家2格生命预期对齐当前5格规则。

## 自动化验证与追溯

| 验证 | 结果 | 覆盖 |
|---|---|---|
| 全量EditMode | 325/325通过，0失败、0跳过 | 新增10个整匣库存/边界/取消/新局/P1/P2用例，已有契约、规则、UI和资产回归 |
| 全量PlayMode | 218/218通过，0失败、0跳过 | 新增4个真实枪/手动作与VR输入替身用例、2个双模式生产流程用例，既有P1/P2/P3回归 |

新增EditMode：`CombatMagazineReloadTests`；PlayMode：`CombatMagazineReloadPlayModeTests`与`P3ProductionFlowTests.Bdd23_*_ProductionMagazineReload`。生产流程覆盖输入命令、真实服务、动作、HUD刷新、期间禁射和退出；VR替身验证控制器/握把/HMD不受动作驱动、追踪取消恢复。已有P3XRTrainingInputTests验证VR左摇杆按压映射。

BDD：[23共用战斗核心](../BDD/screens/23-P3共用战斗核心.feature.md)「三个弹匣与动作结束装填」「提前换弹与无效请求」「取消换弹与新局复位」，HUD参考15/19/20。接口02/06/11/14/15和[task020](../任务规格/阶段三-P3-小型堑壕与城镇战斗原型/玩法功能/task020-combat-magazine-reload.md)已同步。

Unity Test Framework通过MCP启动；测试结果与帧证据保存在[证据目录](evidence/combat-reload-2026-10-11)。同一真实枪械动作关键帧：[拔出](evidence/combat-reload-2026-10-11/01-pull.png)、[移出视野](evidence/combat-reload-2026-10-11/02-below-view.png)、[插入](evidence/combat-reload-2026-10-11/03-insert.png)、[完成](evidence/combat-reload-2026-10-11/04-complete.png)。图像来自Unity Camera.Render，已人工查看；TXT记录对应相机、枪体和渲染范围。

最终编译完成且无编译错误。Console记录164条诊断，其中157条为XR样例的无眼动设备提示（GazeInputManager）；其余涉及输入替身不支持触觉、测试场景切换中的双AudioListener/EventSystem、未初始化XR及EditMode最后场景卸载。全量测试仍全部通过，但不能宣称Console为0；汇总见[日志观察](evidence/combat-reload-2026-10-11/console-observations.json)。这些环境/旧fixture诊断不作为本次换弹实机验收通过依据。测试期间自动生成的临时桥接脚本、手雷截图、字体图集及XR设置重排序已清理或恢复，不纳入功能提交。

## 实机待验

真实VR设备的双眼观感、抓握自然度、换弹时移动、左右手与UI射线可用性、暂停/追踪恢复和72Hz性能需要实机复验；自动化输入替身不等于实机通过。步骤：分别进入模式三/四，双手持枪，射一发后左摇杆按压换弹，观察握匣→下拔→视野外→装回；确认两秒后30/30、备用减少一匣，期间无法开火；用尽三个弹匣后不能再换弹；动作中暂停/追踪失效应恢复原位并保留库存，重试恢复30/60。

本报告仅交付该功能实现与实际自动化验证，不签核整个P3阶段或真实VR体验。
