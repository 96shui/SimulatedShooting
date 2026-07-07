# task016 P1 场景验收说明

## 1. 交付状态

task016 的场景侧视觉、性能和基础联调收口已实现。

完整 P1 流程与关键 UI 联调暂不能标记为完成：当前 `Assets/` 中未发现 task014 的端到端流程实现，也未发现 task015 要求的 P1 UI、`UITestId` 和关键控件。因此本文不以场景占位 UI 代替这两个前置任务。

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

- 保留 task004 的 100m 射击通道、护坡、距离标记、靶后挡土、射击台和远景。
- 保留 task012 的 50cm 靶面、十环、靶心和弹着表现。
- 新增射击区入口框架和识别色条，强化军事训练场入口层次。
- 新增主靶标识别框，使 100m 目标在远景中更易定位。
- 新增射击位安全边界和射向引导线。
- 新增射击位左前方低面数武器箱，在无 VR 第一人称视野中位于左下区域。
- 使用现有低成本材质系统，不引入新的高分辨率贴图。
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

## 4. 性能收口

- Environment 和视觉收口几何体标记为静态批处理、Occludee 和 Reflection Probe Static。
- 无 VR 相机关闭 HDR，保留 MSAA 和 Occlusion Culling。
- 仅保留一个启用的实时方向光。
- 方向光使用中等阴影分辨率和软阴影。
- 场景不使用后处理 Volume。
- 自动化预算：
  - Renderer 数量不超过 256。
  - 独立材质数量不超过 20（包含停用状态下的 XR Origin 示例资源）。
  - 启用的 Light 数量为 1。

上述预算是 P1 原型门禁，不等同于目标 VR 设备上的最终帧率结论。

## 5. 联调检查

### 已覆盖

- `ZeroingRangeScene` 可由 Build Settings 加载。
- 射击位、主靶标、武器出生点、HUD 锚点、无 VR 相机和 VR Origin 均保留稳定测试 ID。
- 无 VR 相机到主靶标的射线无遮挡。
- task012 靶面射线命中和厘米偏差转换保持有效。
- 除武器箱主体外，新增视觉元素不带 Collider，不干扰射线和武器命中。
- 武器箱主体保留 Collider，但位于射线轴线左侧，不遮挡主靶。
- 无 VR 相机的目标构图保持在画面中心。

### 待前置依赖

- task014：输入替身驱动的完整 P1 训练流程。
- task015：P1 UI 页面、HUD、按钮、文本及稳定 `UITestId`。
- UI/HUD 字体可读性、文本溢出、Busy 状态和 DTO 刷新。
- 完整流程从任务说明、HUD、弹着分析到最终评级的场景内运行。

## 6. 自动化测试

测试文件：

```text
Assets/SimulatedShooting/Tests/PlayMode/ZeroingRangeSceneTests.cs
```

task016 场景侧测试：

- 可查找视觉收口根节点、入口框架、主靶识别框和方向光。
- Renderer、材质、Light 和后处理数量满足 P1 原型预算。
- 无 VR 相机启用 MSAA 和遮挡剔除并关闭 HDR。
- 无 VR 相机到主靶标视线无遮挡。
- HUD 锚点继续存在。

## 7. 手工验收

### 无 VR Editor

1. 打开 `Assets/Scenes/ZeroingRangeScene.unity`。
2. 执行 `Tools/Simulated Shooting/Run Zeroing Range Scene`。
3. 确认入口框架和安全边界不会遮挡主靶。
4. 确认主靶识别框能提高远距离识别度。
5. 确认视野中心、武器参考模型和主靶在同一射击轴线上。
6. 触发靶面命中，确认弹着标记与厘米偏差数据一致。

### VR 实机待验收

- HUD 字体和信息层级可读。
- HUD 不遮挡瞄准点和主靶。
- 射击位站姿、头部高度和武器操作空间舒适。
- 单方向光、阴影和场景几何体在目标设备上达到预期帧率。

## 8. 完成判定

当前判定：**场景侧完成，P1 总联调部分完成**。

task014 和 task015 接入并通过各自 PlayMode 测试后，需要补跑完整 P1 流程并完成 VR 实机手工验收，才能把 task016 标记为全部完成。
