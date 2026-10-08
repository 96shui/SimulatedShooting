# 两格生命、战斗反馈与 VR 持枪修复

日期：2026-10-09。实现基线：`7361aba`，工作分支：`fix/p3-vr-playable-integration`，交付目标：远程 `main`（用户已授权提交与推送）。工程修复与自动化自测完成，VR 实机复验及阶段独立审核待执行。

## 用户需求与实现

| 需求 | 实现与追溯 | 当前验证 |
|---|---|---|
| P3 玩家和敌人均两格血，两次有效子弹命中死亡 | BDD23/task019；CombatConfigDto 默认双方2、单发伤害1；CombatCoreService 去重、死亡和结算；PlayerStatusDto.MaxHealth；HUD 两个独立刻度 | 全量 EditMode 301/301；实际场景/输入流程专项通过 |
| 敌人射击动作、子弹、声音及玩家受击血雾 | 恢复 D3 步枪网格；服务事件驱动 Shot 动画、枪口、短弹道、空间音源与玩家血雾；包括致命攻击 | 两张生产地图的 CombatFeedbackIntegrationTests 通过；受击截图已检查 |
| 自然倒地，不高举双手 | 保留原倒地躯干/腿部运动，重烘焙 D3/D4 双臂和手腕，枪随身体放下；不驱动导航根 | 两模型连续姿态、倒地和复位测试通过；逐帧截图已检查 |
| 移动时枪稳定、人物双手替代手柄 | 共用训练枪按 XR 当前姿态更新；渲染前刷新近手抓握锚点，枪更新后双手视觉再对齐；资源手模型用于所有 XR Rig；隐藏硬件网格并保留射线 | 设备输入替身驱动物理拾枪、45帧移动和渲染前30mm移动；主菜单/P2/城镇/堑壕路径通过 |
| P1 所有分析帮助仅点击帮助时出现 | BDD06/task010；建议、准心柱/觇孔推荐、图例/长按说明统一显隐；每轮默认收起 | 7项 UI 测试通过；展开/收起截图已检查 |

## 一并修复的场景问题

- P3 地图概览相机与玩家相机同时有音频监听器：运行时仅启用选定玩家输出；无人机拍摄相机保持纹理输出。
- UI 选择旧场景相机使卸载中的 XR Rig 重新启用，随后关闭共享输入：只选当前活动场景的活动相机。
- UI 跟随组件无有效相机引用：场景交接时明确绑定玩家相机。
- 未激活 XR Rig 安装手模型时，父节点查询漏掉手模型组件：包含未激活父节点，防止把人物双手当成硬件隐藏；实际网格启用与渲染状态进入验收。
- 全场景替换时销毁回调重新激活已在卸载的旧主菜单：恢复主菜单由 lease 在卸载前完成，销毁回调只清理，避免生成孤立 XR 管理器。
- 生产闭环测试同步最新 main 的无人机起飞、侦察、返回、落地和开战流程，不直接绕过开场。
- P1/P2 近手抓取曾只绑定数字抓握信号，模拟 Value 是空动作；XRI 读取交互强度时可访问无效输入状态。两张场景补齐左右手模拟 `/grip` 绑定，场景构建器同步；输入替身测试确认实际绑定存在。

## 最终验证

| 验证 | Job | 结果与证据 |
|---|---|---|
| 全量 EditMode | `53207fb4c8be432aba1da329d7f047ec` | [301/301，0失败、0跳过](evidence/combat-feedback-vr-2026-10-09/editmode.json) |
| 全量 PlayMode | `20689f75ec4d4bddaf640253b2433cca` | [206/206，0失败、0跳过](evidence/combat-feedback-vr-2026-10-09/playmode.json) |
| 强化头显视野中的双手检查 | `e1fca452ca6f473f9c0fe05a3e6dd815` | [6/6，0失败、0跳过](evidence/combat-feedback-vr-2026-10-09/xr-viewport.json) |

全量通过后，运行时代码和场景资源未再改动；补充测试中的抬枪到模拟头显视野和双手位置断言，XR 6项重新通过。Editor安装工具补充保护：复用已打开的源场景，不关闭用户原先打开的场景；已确认编译通过。左右手资源各有一个有效的骨骼网格、根骨及模型引用。

设备输入替身驱动实际 XRI 拾枪，不直接调用 SelectEnter；检查45帧步行、普通更新后30mm移动的渲染前同步、双手网格启用、手柄网格隐藏、VR射线点击、开场和重进。P1/P2、手雷、无人机及训练结算一并回归。

回归同步了 main 已变更的 P1 终局直接评级和战壕可站立表面/无人机开场断言。菜单高度测试在同一时刻放置和测量，原可读性范围不变。测试环境有缺少眼动设备、虚拟手柄无硬件震动能力的警告，未声称真实设备或 Console 0 warning 验收通过。

## 渲染证据

- 帮助：[默认收起](evidence/combat-feedback-vr-2026-10-09/P1-analysis-help-closed.png)、[点击展开](evidence/combat-feedback-vr-2026-10-09/P1-analysis-help-open.png)。
- 双手持枪：[堑壕](evidence/combat-feedback-vr-2026-10-09/VR-human-hands-BunkersOriginalMode3CombatScene.png)、[城镇](evidence/combat-feedback-vr-2026-10-09/VR-human-hands-CombatScene.png)，均为模拟头显视角。
- 玩家受击：[堑壕](evidence/combat-feedback-vr-2026-10-09/Trench-enemy-shot-player-blood.png)、[城镇](evidence/combat-feedback-vr-2026-10-09/Urban-enemy-shot-player-blood.png)。
- 敌人倒地动作：[约0.18秒](evidence/combat-feedback-vr-2026-10-09/DetailedCharacterD3-death-5.png)、[0.54秒](evidence/combat-feedback-vr-2026-10-09/DetailedCharacterD3-death-17.png)、[1.05秒](evidence/combat-feedback-vr-2026-10-09/DetailedCharacterD3-death-34.png)、[2.10秒](evidence/combat-feedback-vr-2026-10-09/DetailedCharacterD3-death-69.png)；角色隔离观察视角用于检查骨骼姿态，不是第一人称截图。友军[最终姿态](evidence/combat-feedback-vr-2026-10-09/DetailedCharacterD4-death-69.png)同样验证。

## VR 实机复验步骤（待用户执行，尚未签核）

1. 连接头显和左右手柄，从 MainScene 进入 Play Mode，确认只显示人物双手，射线能点选训练模式和地图。
2. 堑壕进入简报后点击开始，观察完整无人机开场；落地恢复后先松开按键，再用右手握枪柄、左手托护木。
3. 左摇杆移动，保持双手相对身体稳定，观察枪是否跟随平顺；右摇杆转向后再次检查。松开右手后枪应自然掉落。
4. 让敌人看见玩家：确认举枪射击、枪口/子弹、方向枪声和视野下方血雾；第一次受击生命1/2，第二次0/2并失败。重试恢复2/2。
5. 向同一敌人分别射两发：第一发仍存活，第二发倒地，双臂自然放下；尸体5秒后隐藏。城镇重复第3–5步，并验证返回后重新进入可拾枪。
6. P1 完成一轮三发，确认分析页没有自动建议和操作说明；点击帮助显示全部，再点击收起全部。下一轮再次默认收起。

自动输入和渲染验证不替代真实双眼可读性、声音听感和设备追踪抖动复验。阶段独立审核与 VR 签核保持待验。
