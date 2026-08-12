# task016 P1 场景验收说明

## 1. 交付状态

task016 的场景侧视觉、性能和基础联调收口已实现。task014 输入替身端到端门禁已于 2026-08-12 通过（3/3），task015 的 P1 UI、`UITestId`、分页面测试和 VR/无 VR UI 切换也已合入当前基线。

2026-08-12，项目负责人接受现有自动化、历史场景结果和既有 VR 核心射击证据，确认 task016 与 P1 阶段完成。本次状态更新没有新增真实 VR 菜单或目标设备帧率测试记录；这些体验项保留为后续回归观察项。

## 2. Scene 位置

```text
Assets/Scenes/ZeroingRangeScene.unity
```

场景生成器：

```text
Assets/SimulatedShooting/Editor/ZeroingRangeSceneBuilder.cs
```

Unity 菜单：

- 重新生成：`Tools/Simulated Shooting/Build Zeroing Range Scene`
- 无 VR 运行：`Tools/Simulated Shooting/Run Zeroing Range Scene`

## 3. 视觉收口

- 保留 task004 的 100m 射击通道、距离标记、靶后挡土、射击台和远景；移除通道两侧旧护坡，避免与建筑重复占位。
- 保留 task012 的 50cm 靶面、十环、靶心和弹着表现。
- 通道与射击台改用带漫反射、法线和 AO 的平铺混凝土 PBR 表面。
- 左右通道外各布置 3 栋低成本模块化厂房，使用 1K 厂房贴图与 2K 混凝土贴图；厂房包含门窗、装卸口、附属体和轻量战损轮廓。
- 每栋厂房包含塌落屋面和烟熏破损表现，但战损件不带 Collider 或 Rigidbody，不改变射击规则。
- 树线与对应侧厂房处在同一条侧向布置带，树根统一位于 `Y=0`；树木使用不规则间距并保持在 100m 射界之外。
- 新增射击区入口框架和识别色条，强化军事训练场入口层次。
- 新增主靶标识别框，使 100m 目标在远景中更易定位。
- 靶后远景使用无雪写实岩壁素材 `Coastal Cliff 01`，替换原有球体山体占位；前层和两个抬高、后移、错角度的后层共同形成更高的山体轮廓，整体位于靶后 10m 以外且不参与碰撞。
- 新增射击位安全边界和射向引导线。
- 新增射击位左前方低面数武器箱，在无 VR 第一人称视野中位于左下区域。
- 新增贴图最高为 2K；厂房贴图为 1K，树木继续使用 Billboard，未引入高面数建筑模型。
- 岩壁保留原模型四级 LOD，运行时贴图限制为 1K，并关闭远景投影、碰撞、光照探针和反射探针。
- 保持单一方向光、线性雾和低/中精度几何体，不启用后处理 Volume。

新增稳定测试 ID：

| 用途 | 测试 ID |
|---|---|
| 视觉收口根节点 | `ZeroingRange.Visual.Root` |
| 射击区入口框架 | `ZeroingRange.Visual.RangeGate` |
| 主靶识别框 | `ZeroingRange.Visual.TargetFrame` |
| 安全边界 | `ZeroingRange.Visual.SafetyBoundary` |
| 左侧武器箱 | `ZeroingRange.Visual.WeaponCrate.Left` |
| 主方向光 | `ZeroingRange.Lighting.Sun` |
| 左侧厂房组 | `ZeroingRange.Environment.Buildings.Left` |
| 右侧厂房组 | `ZeroingRange.Environment.Buildings.Right` |
| 左侧树线 | `ZeroingRange.Environment.TreeLine.Left` |
| 右侧树线 | `ZeroingRange.Environment.TreeLine.Right` |

## 4. 性能收口

- Environment 和视觉收口几何体标记为静态批处理、Occludee 和 Reflection Probe Static。
- 无 VR 相机关闭 HDR，保留 MSAA 和 Occlusion Culling。
- 仅保留一个启用的实时方向光。
- 方向光使用中等阴影分辨率和软阴影。
- 场景不使用后处理 Volume。
- 自动化预算：
  - Renderer 数量不超过 256。
  - 独立材质数量不超过 24（包含停用状态下的 XR Origin 示例资源和新增厂房材质）。
  - 启用的 Light 数量为 1。

上述预算是 P1 原型门禁，不等同于目标 VR 设备上的最终帧率结论。

## 5. 联调检查

### 已覆盖

- `ZeroingRangeScene` 可由 Build Settings 加载。
- 射击位、主靶标、武器出生点、HUD 锚点、无 VR 相机和 VR Origin 均保留稳定测试 ID。
- 无 VR 相机到主靶标的射线无遮挡。
- 左右厂房和对应树线均位于同一侧向布置带，并处于 14m 射击通道之外。
- 混凝土地面和厂房材质均绑定实际贴图、法线；混凝土额外绑定 AO。
- task012 靶面射线命中和厘米偏差转换保持有效。
- 厂房主体保留静态 Collider，但不进入射击通道；战损装饰、门窗表面和屋顶装饰不带 Collider。
- 武器箱主体保留 Collider，但位于射线轴线左侧，不遮挡主靶。
- 无 VR 相机的目标构图保持在画面中心。

### 已完成的前置与总联调

- task014 已提供输入替身驱动的完整 P1 训练流程，并通过首轮优秀、三轮不及格和场景联调 3 个门禁用例。
- task015 已提供 P1 UI 页面、HUD、按钮、文本、稳定 `UITestId` 和 VR/无 VR UI 切换测试替身。
- 完整流程已经覆盖任务说明、HUD、弹着分析和最终评级。

## 6. 自动化测试

测试文件：

```text
Assets/SimulatedShooting/Tests/PlayMode/ZeroingRangeSceneTests.cs
```

task016 场景侧测试：

- 可查找视觉收口根节点、入口框架、主靶识别框和方向光。
- 可查找左右厂房组和树线；验证各 3 栋厂房、实际 PBR 贴图、战损件和射界边界。
- 验证地面贴图平铺、树线不规则分布以及旧侧护坡已移除。
- Renderer、材质、Light 和后处理数量满足 P1 原型预算。
- 无 VR 相机启用 MSAA 和遮挡剔除并关闭 HDR。
- 无 VR 相机到主靶标视线无遮挡。
- HUD 锚点继续存在。

## 7. 手工验收

### 无 VR Editor

1. 打开 `Assets/Scenes/ZeroingRangeScene.unity`。
2. 执行 `Tools/Simulated Shooting/Run Zeroing Range Scene`。
3. 确认左右各 3 栋厂房形成战损工业训练场轮廓，且树线与厂房位于同一侧向布置带、树根贴合地面。
4. 确认混凝土地面没有明显拉伸，厂房门窗、装卸口、烟熏和塌顶可辨认。
5. 确认入口框架、安全边界和所有厂房均不会遮挡主靶或侵入 14m 射击通道。
6. 确认主靶识别框能提高远距离识别度。
7. 确认视野中心、武器参考模型和主靶在同一射击轴线上。
8. 触发靶面命中，确认弹着标记与厘米偏差数据一致。

### VR 实机持续回归项

- HUD 字体和信息层级可读。
- HUD 不遮挡瞄准点和主靶。
- 射击位站姿、头部高度和武器操作空间舒适。
- 单方向光、阴影和场景几何体在目标设备上达到预期帧率。

## 8. 完成判定

当前判定：**task016 已完成，P1 总联调已按项目负责人决定关闭。**

后续 P2 共用输入、HUD、武器和全局服务时，仍需抽测本说明第 7 节的 VR 体验项；发现问题按回归缺陷处理，不再把它们作为 P1 阶段状态的阻塞项。
