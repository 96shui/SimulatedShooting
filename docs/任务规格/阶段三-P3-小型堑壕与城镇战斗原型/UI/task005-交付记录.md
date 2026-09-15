# task005 交付记录：堑壕战斗 HUD、小地图与任务结算

日期：2026-09-15。状态：`[~]` 自测完成，独立审核、正式场景装配和 VR 验收待执行。实现提交：`f77273b`。

## 实际变更

- `Assets/VRShooting/Runtime/Unity/UI/P3UiCommon.cs`：DTO 驱动小地图和通用格式化组件。
- `Assets/VRShooting/Runtime/Unity/UI/P3TrenchHudAndResultsUI.cs`：`Screen_TrenchHud`、`Screen_TrenchResults` 的 View/Presenter；HUD 消费 Session/HUD 快照，结果消费 `TrenchResultDto`，重试/返回只转发应用命令并防重复提交。
- `Assets/VRShooting/Runtime/Unity/UI/P3CombatUIRoot.cs`、`Assets/VRShooting/Prefabs/UI/P3CombatUI.prefab`：稳定 HUD/结算根和控件 ID。
- `Assets/VRShooting/Tests/PlayMode/UI/P3UiTask002_005_008_011PlayModeTests.cs`：Screen 15/17 测试。

## BDD、接口与测试映射

| 范围 | 映射 |
|---|---|
| BDD | `docs/BDD/screens/15-堑壕战斗HUD.feature.md`、`17-堑壕任务结算.feature.md`：血量/弹药/姿态/肩侧/拐角/队友/搜索进度、胜负统计、平面图、重试和返回主菜单。 |
| 接口 | `01-页面导航与UI事件.md`、`02-训练Session数据模型.md`、`03-HUD显示数据.md`、`07-队友与战术指令.md`、`08-堑壕任务服务.md`、`11-Unity场景与Prefab约定.md`。 |
| 自动化 | `Screen15_TrenchHud_RefreshesDtoAndRejectsOldSessionEvents`、`Screen17_TrenchResults_RendersPartialDtoAndRetryIsIdempotent`、`P3UiRoot_BuildsAllPagesAndFrozenTestIds`。 |

定向 PlayMode 结果：`9/9`（`p3-ui-playmode-final.xml`）；其中本任务相关用例通过。全量 EditMode `246/246`、PlayMode `141/141`（`p3-playmode-final.xml`）。原始 XML、日志和复现记录见 `docs/codex-reports/evidence/p3-ui-tasks-002-005-008-011/`。

## 测试命令

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -batchmode -nographics -projectPath 'E:\新建文件夹 (3)\SimulateShooting' -runTests -testPlatform PlayMode -testFilter 'P3UiTask002_005_008_011PlayModeTests' -testResults 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.xml' -logFile 'E:\新建文件夹 (3)\SimulateShooting\docs\codex-reports\evidence\p3-ui-tasks-002-005-008-011\p3-ui-playmode-final.log'
```

## 替身、联调与遗留

测试使用 `FakeTrenchService`、`P3Fixtures`、Fake Session/HUD/Result DTO 和 `ProbeNavigation`；HUD 在无独立 HUD 服务的替身路径回退到 Session 快照，但不从文本反算结算。UI 已提供给 task013 的注入 seam；正式场景事实、真实输入和应用闭环接线仍待 task013/C。未执行真实 VR 验收；待验包括 HUD 可读性、队友遮挡、姿态/机瞄反馈、结果返回后的输入和场景清理。
