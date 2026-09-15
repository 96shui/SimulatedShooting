# task008 交付记录：城镇地图选择、街道 HUD 与建筑入口

日期：2026-09-15。状态：`[~]` 自测完成，独立审核、正式场景装配和 VR 验收待执行。实现提交：`f77273b`。

## 实际变更

- `Assets/VRShooting/Runtime/Unity/UI/P3UiCommon.cs`：共享地图卡片、小地图和 DTO 格式化。
- `Assets/VRShooting/Runtime/Unity/UI/P3UrbanUI.cs`：`Screen_UrbanMapSelection`、`Screen_UrbanStreetHud` 的 View/Presenter；显示街道/建筑敌情范围，入口提示由 `HudPromptDto` 驱动，进入建筑只提交 `EnterBuilding`，保持 Busy、错误和旧 Session 隔离。
- `Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs`、`Assets/VRShooting/Prefabs/UI/P3CombatUI.prefab`：稳定页面根、按钮/文本测试 ID 和运行时 UI 组合根。
- `Assets/VRShooting/Tests/PlayMode/UI/P3UiTask002_005_008_011PlayModeTests.cs`：Screen 18/19 测试。

## BDD、接口与测试映射

| 范围 | 映射 |
|---|---|
| BDD | `docs/BDD/screens/18-城镇地图选择.feature.md`、`19-城镇街道HUD.feature.md`：默认地图、任务信息、返回、街道 HUD、入口提示、风险提示、姿态/换肩/拐角和进入建筑。 |
| 接口 | `01-页面导航与UI事件.md`、`02-训练Session数据模型.md`、`03-HUD显示数据.md`、`07-队友与战术指令.md`、`09-城镇任务服务.md`、`11-Unity场景与Prefab约定.md`。 |
| 自动化 | `Screen18_UrbanMapSelection_RendersRangesAndStartsStreet`、`Screen19_UrbanStreetHud_UsesPromptDtoAndKeepsRiskVisible`、`P3UiRoot_BuildsAllPagesAndFrozenTestIds`。 |

定向 PlayMode 结果：`9/9`（`p3-ui-playmode-final.xml`）；其中本任务相关用例通过。全量 EditMode `246/246`、PlayMode `141/141`（`p3-playmode-final.xml`）。原始 XML、日志和复现记录见 `docs/codex-reports/evidence/p3-ui-tasks-002-005-008-011/`。

## 测试命令

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -batchmode -nographics -projectPath 'E:\新建文件夹 (3)\SimulateShooting' -runTests -testPlatform PlayMode -testFilter 'P3UiTask002_005_008_011PlayModeTests' -testResults 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.xml' -logFile 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.log'
```

## 替身、联调与遗留

测试使用 `FakeUrbanService`、`P3Fixtures` 和 `ProbeNavigation`；没有引用入口 Collider、敌人组件或场景对象，进入许可完全由服务提示/命令结果决定。UI 已提供给 task013 的注入 seam；真实街道/建筑场景和应用 loader 接线仍待 task009/012/013。未执行真实 VR 验收；待验包括街道 HUD 可读性、入口交互可达性、队友遮挡、建筑切换后的状态保持和退出清理。
