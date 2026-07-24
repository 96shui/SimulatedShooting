# QBZ-191 训练步枪视觉替换交付记录

- 日期：2026-07-23
- 工作分支：`feature/task015-vr-ui-interaction`
- 关联规格：P1 `task005-basic-weapon-control.md`、`task013-shot-result-recoil-zeroing-integration.md`

## 交付内容

- 使用 Brahian SG 发布的 `QBZ-191 - Free` 替换 `Weapon_training-rifle_Blockout` 的程序化方块视觉，保留原 Prefab 路径、`training-rifle` 服务 ID 和场景引用。
- 模型保持真实米制比例，约 37.9k 三角面；枪身与弹匣使用独立 URP Lit 材质。
- 原始 4K PBR 贴图保留为源文件，Unity 运行时导入上限设为 2K；金属度和反相粗糙度已打包为 URP metallic/smoothness 贴图。
- 重新校准后握把、前护木、枪口、机械瞄具、弹匣和肩侧锚点，并按 QBZ-191 外形缩小三个物理碰撞体。
- 根据模型连通组件重新把后手锚点定位到手枪握把中心、前手锚点定位到护木下缘。
- 顶点级对比确认下载素材把弹匣放在脱离机匣的位置，主要间隙并非 Prefab Transform 导致；垂直射线测得首次修正后弹匣平顶距机匣接触面仍有约 30.5–34.1 mm。
- 将独立弹匣顶点组相对原始素材累计上移 44 mm，使平顶进入机匣约 0–3.6 mm；同步弹匣逻辑锚点到 `(-0.008, -0.136, 0.042)`。
- 下载压缩包缺少 OBJ 引用的 MTL，Unity 原先因此把枪身和弹匣合并为单一 submesh；补充最小 `QBZ-191.mtl` 并启用材质描述导入后，枪身与弹匣恢复为两个 submesh，并分别绑定已有 URP 材质。
- 无 VR 枪体姿态改为让 `RearHandGrip` 与模拟后手对齐，与真实 VR 的 XRI Attach 语义保持一致。
- VR 稳定度基准改为读取当前 Prefab 的真实前后握把间距，不再沿用旧占位枪的 `0.78m` 固定值。
- 素材来源、CC BY 授权和署名要求记录在 `Prefabs/Weapons/ASSET_SOURCE.md` 与 `QBC-191/ATTRIBUTION.md`。

## 自动化验证

- Unity 2022.3.62f3c1 无界面 Prefab 重建：成功，退出码 0。
- 首次 QBZ-191 视觉替换后，`SimulatedShooting.Tests.PlayMode.ZeroingRangeSceneTests`：23/23 通过，0 失败，0 跳过。
- 2026-07-23 弹匣二次贴合修正已在当前打开的 Unity Editor 中完成重新导入和 PlayMode 回归：`ZeroingRangeSceneTests` 23/23 通过，0 失败，0 跳过；日志未发现 C# 编译错误。
- 增加子网格 Bounds、弹匣专用材质和逻辑锚点断言，验证弹匣高度、中心和顶面均处于机匣内的目标区间，防止素材再次回到悬空或材质合并状态。
- 覆盖 QBZ-191 模型与材质存在性、三角面精度、旧 blockout 移除、Prefab 绑定、无 VR 命中与弹着、ADS、前手枪线、XRI 双手拾取/释放、后坐力和触觉替身。

## VR 实机复核项

- 检查枪托、后握把和前护木与左右虚拟手的贴合程度。
- 检查贴近机械瞄具时的双眼可读性和近裁剪。
- 检查枪口曳光是否从消焰器前端发出。
- 连续三发时观察枪体与手部是否分离、材质和 2K 贴图是否影响目标帧率。
