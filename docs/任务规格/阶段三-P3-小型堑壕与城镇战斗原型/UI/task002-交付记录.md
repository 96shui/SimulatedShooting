# task002 交付记录：堑壕地图选择与开场简报 UI

日期：2026-09-15。状态：`[~]` 自测完成，独立审核、正式场景装配和 VR 验收待执行。实现提交：`f77273b`。

## 实际变更

- `Assets/VRShooting/Runtime/Unity/UI/P3UiCommon.cs`：地图卡片、小地图、导航端口和 DTO 文本格式化。
- `Assets/VRShooting/Runtime/Unity/UI/P3TrenchUI.cs`：`Screen_TrenchMapSelection`、`Screen_TrenchBriefing` 的 View/Presenter；地图选择只提交 `SelectMap`，开始按钮才提交 `StartSession`；简报使用可替换无人机表现端口。
- `Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs`、`Assets/VRShooting/Prefabs/UI/P3CombatUI.prefab`：稳定页面根、按钮/文本测试 ID 和运行时 UI 组合根。
- `Assets/VRShooting/Tests/PlayMode/UI/P3UiTask002_005_008_011PlayModeTests.cs`：Screen 12/14 和根页面测试。

## BDD、接口与测试映射

| 范围 | 映射 |
|---|---|
| BDD | `docs/BDD/screens/12-堑壕地图选择.feature.md`、`14-堑壕开场简报.feature.md`：默认地图、任务条件、返回、简报投影/小队/目标、查看地图、开始进入。 |
| 接口 | `01-页面导航与UI事件.md`、`02-训练Session数据模型.md`、`03-HUD显示数据.md`、`08-堑壕任务服务.md`、`11-Unity场景与Prefab约定.md`。 |
| 自动化 | `Screen12_TrenchMapSelection_DefaultDtoAndSelectOnlyRoutesBriefing`、`Screen14_TrenchBriefing_PlaysPreviewAndStartsOnlyOnCommand`、`P3UiRoot_BuildsAllPagesAndFrozenTestIds`。 |

定向 PlayMode 结果：`9/9`（`p3-ui-playmode-final.xml`）；其中本任务相关用例通过。全量 EditMode `246/246`、PlayMode `141/141`（`p3-playmode-final.xml`）。原始 XML、日志和复现记录见 `docs/codex-reports/evidence/p3-ui-tasks-002-005-008-011/`。

## 测试命令

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -batchmode -nographics -projectPath 'E:\新建文件夹 (3)\SimulateShooting' -runTests -testPlatform PlayMode -testFilter 'P3UiTask002_005_008_011PlayModeTests' -testResults 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.xml' -logFile 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.log'
```

## 替身、联调与遗留

测试使用 `FakeTrenchService`、`P3Fixtures`、`ProbeNavigation` 和 `P3BriefingDroneVisual`，生产代码没有注册 Fake，也没有让 View 持有玩法/场景引用。当前 UI 已提供给 task013 的注入 seam；正式应用 Presenter/场景 loader 接线仍由 task013 负责。未执行真实 VR 验收；待验包括头显可读性、按钮可达性、简报视线和进入 HUD 后输入/场景清理。
