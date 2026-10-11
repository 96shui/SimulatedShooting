# P1 水平整格调整修复

追溯：BDD06「水平调整只允许整格」、接口04「2026-10-11 水平整格约束」、P1 功能 task006 与 UI task010。

默认水平补偿原先直接取三发平均偏差的相反数，除以2后出现小数格。现在服务按偏差绝对值除以2cm向上取整，并保留补偿方向；默认、手动点击、长按步进、预览与应用均使用2cm整数倍。UI 当前调节量显示整数格。垂直补偿保持既有规则。

验证使用 Unity 2022.3.62f3c1 在临时独立项目中执行当前源码，保留正在打开的生产项目编辑器状态。复制业务代码、完整 UI 源码、相关包和字体，未加载生产靶场场景。

- EditMode：ZeroingRulesTests、ZeroingServiceTests，42/42通过。包括正负小数偏差、零偏差、整格默认值、±2cm步进、±50cm边界、应用幂等和下一轮弹着。
- PlayMode：Bdd06_HorizontalFractionalMean_ShowsWholeClicksAcrossZero，1/1通过。验证非整数均值生成整格补偿、无小数格文本、连续向后调整跨零及垂直量保持。
- `git diff --check` 通过。

测试证据：[EditMode](evidence/p1-horizontal-whole-clicks/editmode.xml)、[PlayMode](evidence/p1-horizontal-whole-clicks/playmode.xml)。

VR 实机待验：分析页觇孔格数可读，向前/向后短按每次一格，长按逐格变化；蓝点、应用后下一轮弹着与实际整格补偿一致。本轮未执行 VR 实机验收或生产场景端到端回归。
