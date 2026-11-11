# 模式一四发与弹着分析验收材料

- `01-briefing.png`：原四发任务简报。
- `02-analysis-four-shots.png`：原始四发分析截图，保留当时的舍弃标注作为历史对照。
- `02-analysis-four-shots-no-label.png`：相同的分析页和四发数据，四个有效弹着均为相同红点，不对舍弃弹额外标注；黄点为计算集均值，蓝点为当前补偿。
- `03-analysis-spread.png`：原分散状态分析截图。
- `04-analysis-clockwise-blue-up.png`：原顺时针调整后蓝点上移截图。
- `editmode-final.xml`、`playmode-final.xml`：移除标注前的全量测试基线；`red.xml`、`green.xml` 是此前定向测试记录。

无标注截图由 `Screen06_ZeroingImpactAnalysisUITests.Bdd06_FourImpactsShowIdenticalMarkersAndAverage` 在 `P1_FOUR_CAPTURE=1` 时生成；四发坐标与原截图一致，分别为 `(5, 7)`、`(13, 7)`、`(9, 14)`、`(22, 20)` cm。两张 1920×1080 截图的像素差异仅位于第四发及原标签附近（x=792–843，y=395–416，以左上角为原点）；黄点与蓝点区域逐像素一致。原截图未覆盖。

移除标注后重新运行全量测试：EditMode 378 项、失败 12 项，PlayMode 292 项、失败 22 项；两套失败集合均与本目录的原始 XML 逐条一致，新增失败为 0。四点样式和无标签测试、脱靶标签测试均通过。
