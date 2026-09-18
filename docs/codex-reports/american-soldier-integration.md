# P3 AmericanSoldier 敌我角色接入

日期：2026-09-18。对应场景 task014；保持本地工作区交付，未提交/推送。代码接入与自动化自测完成后交由用户 VR 验收，独立审核与实机帧率不记为通过。

## 最终变化

- `Actor_Enemy.prefab`、`Actor_Teammate.prefab` 均替换为用户提供的 `army3.fbx`，继续使用原 Prefab GUID、CombatActorView、命中体、导航和服务实体映射。堑壕/城镇生产加载器都会实例化这两个 Prefab。
- 敌军保留原迷彩；友军使用独立深绿色军服/裤子/帽子材质、蓝色臂带和胸背标记。双方共享基础人物网格，皮肤和头发保留原色；友军不是另一个下载来的角色。
- FBX 有 52 个蒙皮骨骼、约 9710 三角面，但自带片段只有约 0.033 秒，不能当作战斗动作库。复用项目 Quaternius CC0 SWAT 动作，离线生成适用于新骨骼的待机、走路、跑步、开火、受击、倒地六个动画。
- 导航实际速度驱动走跑混合；射击/受击由已有服务反馈驱动。左右手进行握枪对齐，步枪跟随右手，枪口跟随步枪；臂带随手臂、胸背标记随躯干。
- 死亡过渡只启动一次，停止导航和有效命中，尸体持续保留；复位恢复存活动画和命中体。无动画组件的旧角色保留兼容路径。
- `AmericanSoldierInstaller` 提供可重复生成入口：`Tools > Simulated Shooting > Scene 3 > Install American Soldiers`。它更新角色资产，不重建地图或覆盖建筑布局。

## 素材处理

外部 JPEG 存在名称与内容错位；其中 `mott_var01_body_n.tex.jpeg` 实际是 ZIP，已原样移动到 `Source~/AmericanSoldier-original.zip`。正式材质使用 Unity 从 FBX 提取到 `Embedded/` 的正确内嵌贴图，原外部图片保留。衣服法线使用 NormalMap 导入设置，所有人物材质使用 URP/Lit。

原始贴图只有 256/512 级别，实际清晰度受此限制；这次没有生成或伪称 4K 美术。模型与动作来源见角色目录 `ATTRIBUTION.md`。

## 自动化与问题修复

- BDD23 / task014：`AmericanSoldierTests` 检查敌我蒙皮、材质区别、动作和绑定。
- BDD23 / task014：`AmericanSoldierPlayModeTests` 验证射击状态、命中事件去重、死亡不重复播放、尸体保留、导航/命中体关闭与复位、实际位移及腿骨动画变化。
- 原 `CombatSceneTests` 对敌军视觉的断言更新为新模型；仍保留室内布置等原断言。
- 交叉运行测试发现旧 P3 工厂可在退出 Play 后保留。`UnityCombatSceneLoader` 现在在 Edit Mode 直接返回 ResourceUnavailable，不调用仅运行期可用的场景 API；新增 BDD14/18 测试验证无场景变更。这不会改变 Play Mode 下的加载路径。
- 全量 EditMode：268/268，失败/跳过 0。证据：[EditMode](evidence/american-soldier/editmode.json)。
- 全量 PlayMode 最终复验：170/170，失败/跳过 0，约 157.6 秒。证据：[PlayMode](evidence/american-soldier/playmode.json)。包含 P1/P2 回归、P3 双模式闭环、设备状态驱动的头手跟踪、双手抓枪和射线点击 UI。
- 修正测试稳定性：腿部动作在一个步态周期内取最大变化，避免两个采样点恰好落在相似姿势；XR 输入替身修改设备过滤器前先禁用动作，并在结束时停止消费者、禁用动作后恢复过滤器，避免重绑定时残留动作状态。
- 普通 MainScene Play 入口通过应用服务加载（未冒充手动点击）：堑壕进入 TrenchHud，5/5 个角色有新模型及动画组件；城镇进入 UrbanStreetHud，7/7 个角色有新模型及动画组件。两次均自动检测 IsVr=True，并成功返回主菜单。证据：[堑壕](evidence/american-soldier/trench-smoke.json)、[城镇](evidence/american-soldier/urban-smoke.json)。
- 普通入口 Console 未出现异常；两条警告为未发现眼动设备，不影响手柄路径。见 [Console](evidence/american-soldier/smoke-console.json)。最终 Unity 已停止 Play，打开未修改的 MainScene。

## 截图与性能范围

- [正面敌我对比](evidence/american-soldier/Idle.png)
- [侧面持枪与军服对比](evidence/american-soldier/Idle-side.png)
- [倒地姿态](evidence/american-soldier/Death.png)

截图是 Unity 实际渲染，展示用临时相机和角色在捕获后销毁。动作的运行验证另由 PlayMode 用例覆盖。

已有场景压力测试采样为 13 具尸体 + 2 名队友、180 帧，机器 RTX 3060 / i5-12490F、Editor 2040×1015。记录平均约 67.8 ms、P95 81.8 ms、GPU 46.2 ms；见 [原始记录](evidence/american-soldier/editor-performance.txt)。这是整个场景的 Editor 压力采样，未隔离本次角色开销，也不是 VR 设备帧预算验收；不能据此宣称已达到稳定 72/90 FPS。实机帧率与舒适度仍需单独验收。

## 用户 VR 验收步骤

1. 连接头显和左右手柄，打开 `Assets/Scenes/MainScene.unity` 后按 Play。
2. 通过主菜单进入堑壕，选地图并开始简报后的训练。转头查看两名深绿军服、蓝色标记的队友；对比敌军原迷彩。
3. 左摇杆移动、右摇杆离散转向，观察队友跟随时腿部动作、臂带和武器是否稳定，检查近距离穿插与遮挡。
4. 右 Grip 抓步枪后握把，左 Grip 抓前握把，右扳机射击。观察敌人开火、命中后倒地、尸体保留；检查枪口焰的位置。
5. 重试并确认人物恢复，返回主菜单后进入城镇，复验军服材质、敌我区分和战斗表现，再返回重进一次。
6. 单独记录头显型号、连接方式、帧率/卡顿和不自然姿势所在位置。未佩戴真实设备完成上述步骤，因此本报告不将 VR 实机验收填写为通过。

友军仍遵守现有队形与任务服务规则；本次不新增友军自动开火、误伤、复杂掩体或卧姿动画。
