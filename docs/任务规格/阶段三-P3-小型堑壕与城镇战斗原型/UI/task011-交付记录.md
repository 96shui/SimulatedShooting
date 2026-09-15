# task011 交付记录：建筑搜索 HUD、楼层小地图与城镇结算

日期：2026-09-15。状态：`[~]` 自测完成，独立审核、正式场景装配和 VR 验收待执行。实现提交号在本记录对应的 Git 历史中登记。

## 实际变更

- `Assets/VRShooting/Runtime/Unity/UI/P3UrbanUI.cs`：`Screen_UrbanBuildingHud`、`Screen_UrbanResults` 的 View/Presenter；楼层/房间状态和三层结果小地图均来自 DTO，开门与检查确认分开转发，搜索完成不由 UI 推断。
- `Assets/VRShooting/Runtime/Unity/UI/P3UiCommon.cs`：共享小地图和控件格式化。
- `Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs`、`Assets/VRShooting/Prefabs/UI/P3CombatUI.prefab`：稳定页面根和控件 ID。
- `Assets/VRShooting/Tests/PlayMode/UI/P3UiTask002_005_008_011PlayModeTests.cs`：Screen 20/21 测试。

## BDD、接口与测试映射

| 范围 | 映射 |
|---|---|
| BDD | `docs/BDD/screens/20-建筑搜索HUD.feature.md`、`21-城镇任务结算.feature.md`：楼层搜索、房门提示、开门/房间状态、已搜索标记、搜索完成结算、统计、三层平面图、重试和返回主菜单。 |
| 接口 | `01-页面导航与UI事件.md`、`02-训练Session数据模型.md`、`03-HUD显示数据.md`、`07-队友与战术指令.md`、`09-城镇任务服务.md`、`11-Unity场景与Prefab约定.md`。 |
| 自动化 | `Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands`、`Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce`、`P3UiRoot_BuildsAllPagesAndFrozenTestIds`。 |

定向 PlayMode 结果：`9/9`（`p3-ui-playmode-final.xml`）；其中本任务相关用例通过。全量 EditMode `246/246`、PlayMode `141/141`（`p3-playmode-final.xml`）。原始 XML、日志和复现记录见 `docs/codex-reports/evidence/p3-ui-tasks-002-005-008-011/`。

## 测试命令

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -batchmode -nographics -projectPath 'E:\新建文件夹 (3)\SimulateShooting' -runTests -testPlatform PlayMode -testFilter 'P3UiTask002_005_008_011PlayModeTests' -testResults 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.xml' -logFile 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.log'
```

## 替身、联调与遗留

测试使用 `FakeUrbanService`、`P3Fixtures`、Fake 房间/楼层 DTO 和 `ProbeNavigation`；View 不读取门动画、角色位置或敌人数量来推断房间状态。UI 已提供给 task013 的注入 seam；真实房门/楼层事实由 task012 接入，正式双模式闭环由 task013 负责。未执行真实 VR 验收；待验包括楼层 HUD 可读性、房门交互、队友遮挡、三层地图辨识、结果重试和退出清理。
