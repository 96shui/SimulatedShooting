# P1 UI task017 工程候选记录

2026-10-03，当前状态 `[~]`。未提交，按用户要求未运行测试，不能据此标记验收完成。

## 已实现

`TacticalTargetPlot` 统一绘制实际场景半身胸靶：以保存的 ZeroingRangeScene 中 50cm 靶纸、36×38cm 躯干（中心下移6cm）、直径15cm头部（中心上移17cm）、直径10cm靶心为基准。配色来自 TargetBoard、TargetDark、TargetTenRing 材质。使用代码原生 UI 网格复用场景的几何/配色配置，无外部位图留白和比例变形。

分析页及三轮结算图均绑定同一组件，提供规格所列稳定 ID。弹着和平均点/补偿预览继续读取 DTO，映射 X向右/Y向上，使用50cm正方形命中区域；原始坐标不再被夹到靶纸边界。结算未使用轮次的弹着列表为空，轮次状态继续显示“未使用”。分析页初始化不显示虚构弹着。

## 验证与后续

- 静态 `git diff --check` 通过。
- 新增 BustTargetMappingTests：中心、轴方向、10环半径、不同尺寸、非正方形外框、未夹取原始坐标（BDD06/07）。
- 更新 Screen07_ZeroingFinalRatingUITests：分析/结算复用组件、三輪记录与未使用轮次无示例弹着。
- 测试未执行；Unity 编辑器导入/运行、场景与两个UI页对照截图、重新训练清理和 VR 可读性尚待验证。分析/结算已共享包含四边的 IsOnTarget 边界，越界弹着/平均/补偿标记不显示且不夹到边缘；空轮次隐藏平均/补偿标记。边界与非有限值检查已加入测试候选。Unity 自带 Roslyn 使用项目引用静态编译运行时及测试程序集通过；不能视为视觉或测试通过。
# 2026-10-04 实际场景胸靶导出修订

按用户要求取消手绘近似轮廓。SceneBustTargetExporter 从 ZeroingRangeScene 实际 TargetImpactSurface 对应靶对象复制 Mesh/Material，以50cm命中面和靶心为正交相机基准导出 `Assets/Resources/UI/Tactical/SceneBustTarget.png`（1024×1024）。已在真实 Editor 导出并检查图像；头部/10环的原模型多边形轮廓、原材质颜色及表面阴影均保留。TacticalTargetPlot 的分析/结算页统一加载此纹理，弹着厘米映射不变。以后场景胸靶外观变化，可通过菜单 VR Shooting/UI/Export Actual Scene Chest Target 重新同步，无需重写轮廓。

新增实际纹理绑定候选测试，未运行。UI 在实际头显中的显示仍由用户复测，不把资源导出视为全部验收通过。
