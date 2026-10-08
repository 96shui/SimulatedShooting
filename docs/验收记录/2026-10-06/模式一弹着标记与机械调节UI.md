# 模式一评级弹着点与机械调节 UI 修订（2026-10-06）

依据：BDD06/07、接口04、UI task010/task011，用户最新要求。

## 实现
- 修复 TacticalTargetPlot 标记取样透明贴图角落的问题，标记取样胸靶白色背景，显示为红色。
- 修复缩略图二次 HitRegion 换算。最终评价按 RoundIndex 显示 Rounds/Shots；提前通过后的未使用轮次无 Shots，不生成弹着标记。未修改射线命中/场景弹孔。
- 分析页不显示水平、垂直厘米偏差。准心柱/觇孔建议和调节量用方向、角度、格数；一格=2cm、一度=0.064cm。顺时针向下、逆时针向上，水平向前向右/向后向左。
- 帮助默认关闭，可展开/收起；显示用户指定完整帮助。保留已有分析页靶图、均值和调整预览。
- 保留已有自动初始补偿和应用流程；本次改变调节步长及其显示单位。

## 直接检查证据（不是测试用例执行）
- Unity 编译后 read_console(error) 返回 0 条。
- 编辑器临时生成实际 ZeroingRangeUI，调用其 RenderFinalRating，三个靶图 ImpactCount 分别为 3/3/3。
- 实际相机渲染截图 Temp/UnityMcpTools/P1-three-round-markers.png，三个胸靶各自有三个可见红点。
- 实际 UI 帮助按钮：hidden=True，opens=True，closes=True，overflow=False；截图 P1-analysis-help.png。
- 临时预览场景均关闭，主场景未被预览修改。
- 更新现有方向/步长回归断言，增加缩略图标记 UV 与单次映射回归断言。按用户要求没有运行 EditMode/PlayMode 测试，未提交、未推送。

VR实机验收：查看已射击轮次红点，确认提前结束的后续轮次无点，手柄打开/收起帮助，以及方向按钮点击后的格数/角度变化。
