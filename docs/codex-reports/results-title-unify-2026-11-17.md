# 四模式结算页标题统一与模式一布局验证（2026-11-17）

模式一运行时及共享世界空间 UI 预制体构建器统一使用 `100m精度射校靶 · 训练结算`；模式二统一使用 `移动目标射击 · 训练结算`。模式一标题框对称加宽，两模式标题关闭换行，保留原字号。1920×1080真实 Unity 渲染、TMP 文本测量及定向测试均确认标题完整、单行、有左右留白。全量748个用例按完整fullname与指定基线逐条比较，新增失败为0。

**文案与文件改动**

| 位置 | 旧文案 | 新文案 |
|---|---|---|
| `Assets/VRShooting/Runtime/Unity/UI/MainMenuUI.cs` | `100m射校 训练结算` | `100m精度射校靶 · 训练结算` |
| `Assets/VRShooting/Editor/Task002SharedUiPrefabBuilder.cs` | `100m射校 训练结算` | `100m精度射校靶 · 训练结算` |
| `Assets/VRShooting/Runtime/Unity/UI/MovingTargetRangeUI.cs` | `移动靶训练结算` | `移动目标射击 · 训练结算` |
| `docs/BDD/screens/07-射校最终评级.feature.md` | `100m射校 训练结算` | `100m精度射校靶 · 训练结算` |
| `docs/BDD/screens/09-移动靶HUD.feature.md`、`11-移动靶结算.feature.md` | `移动目标射击 训练结算` | `移动目标射击 · 训练结算` |

另改 `Assets/VRShooting/Tests/PlayMode/UI/Screen07_ZeroingFinalRatingUITests.cs`、`Screen08_09_11_MovingTargetUIFlowTests.cs`，在原结算用例内增加精确文案及布局断言。同步 `docs/任务规格/阶段一-P1-100m射校原型闭环/UI/task011-ui-final-rating.md`、`阶段二-P2-移动靶闭环/UI/task005-moving-target-ui-flow.md` 的交付与验证约束。接口文档未发现需要替换的结算标题原文，本次没有DTO、命令、事件或接口变更。

模式三、四继续为 `堑壕射击 · 任务结算` / `城镇攻防 · 任务结算`。主菜单入口、简报与介绍页文案保持原样。历史报告及既有证据在跑批后按开工快照还原。

**最终尺寸及实测宽度**

模式一的Drawio参数会经横向×2.4、纵向×1.8转换到1920×1080设计画布：原参数宽390实际为936px。修复前仅将新文案注入原组件的探针已实测宽476.13px、单行且未溢出，因此不需要降低字号。此次在允许范围内对称扩展标题框，增加容量；视觉中心仍在x=960。模式二的构建坐标随后被TacticalUILayout重排，下表列最终生效的组件坐标。

| 组件 | 最终文本框（画布左下原点） | 实际宽×高 | 字号 | TMP首选宽 | 实际字形网格宽 | 可用宽度 | 文本框距面板左右边缘 |
|---|---|---:|---:|---:|---:|---:|---:|
| 模式一运行时 | (369.60,817.20)–(1550.40,903.60) | 1180.80×86.40 | 38.00 | 476.13px | 473.58px | 1180.80px | 57.60 / 57.60px |
| 模式二运行时 | (500.00,845.00)–(1420.00,920.00) | 920.00×75.00 | 48.00 | 519.97px | 521.34px | 920.00px | 255.00 / 255.00px |
| 模式一共享构建器 | (510.00,190.00)–(1410.00,280.00) | 900.00×90.00 | 46.00 | 576.33px | 573.72px | 900.00px | 510.00 / 510.00px |

模式一最终调用 `DrawioMin/Max(154,98,492,48)`，面板仍为 `DrawioMin/Max(130,85,540,390)`，文本框左右各留24个Drawio单位，即57.6px，满足≥24px。模式二构建坐标保留(500,145)–(1420,225)，最终由TacticalUILayout定位到(500,845)–(1420,920)，居中不偏移。共享构建器保留(510,190)–(1410,280)、46号，真实调用Build后从新生成Prefab实例测量，结束后恢复生成的Prefab。

运行时字体仍沿用TrainingChinese；原AddLabel/TacticalUIStyle会去除合成Bold，因此最终实测fontStyle为Normal，未改变共用字体风格。没有使用自动缩字号。三个标题均为lineCount=1、isTextOverflowing=false、isTextTruncated=false、全部非空白字符可见；模式一为16字符/14可见字形，模式二为13字符/11可见字形。首选宽使用TMP.GetPreferredValues(text, Infinity, Infinity)，网格宽直接取textInfo.characterInfo可见字形边界，未以字数估算。

[完整TMP组件测量](evidence/results-title-unify-2026-11-17/final/title-measurements.json)；[修改前原尺寸探针](evidence/results-title-unify-2026-11-17/before-layout-measured/title-measurements.json)。

**1920×1080截图及像素验证**

沿用仓库TacticalUIGallery.Create、FillRangePreview、PreviewScene、WorldSpace Canvas与Camera.Render流程，RGB24，整页1设计单位=1像素。纯UI画面不包含游戏场景。标题特写为同一真实组件的另一次1920×1080相机渲染，通过改变正交相机取景生成，未重绘文字。

- [模式一训练结算整页](evidence/results-title-unify-2026-11-17/final/Screen_ZeroingFinalRating.png)
- [模式一标题特写](evidence/results-title-unify-2026-11-17/final/Screen_ZeroingFinalRating_Title_Closeup.png)
- [模式二训练结算整页](evidence/results-title-unify-2026-11-17/final/Screen_MovingTargetResults.png)
- [共享构建器模式一结算补充截图](evidence/results-title-unify-2026-11-17/final/SharedWorldSpace_ZeroingFinalRating.png)

| 整页截图 | 白色标题墨迹宽×高 | 距文本框左 / 右留白 | 距面板左 / 右留白 |
|---|---:|---:|---:|
| 模式一 | 471×37px | 356.40 / 353.40px | 414.00 / 411.00px |
| 模式二 | 518×46px | 201.00 / 201.00px | 456.00 / 456.00px |

墨迹测量在实际标题RectTransform区域内筛选min(R,G,B)≥140且max−min≤40的像素，排除青蓝面板；所有原始截图保持不变。[像素结果与PNG SHA256](evidence/results-title-unify-2026-11-17/pixel-verification.json)；[可复算脚本](evidence/results-title-unify-2026-11-17/verify-pixels.py)。人工查看整页与特写，标题左右均有可见留白，开头与结尾字形完整。

**断言新旧对照**

未发现依赖旧结算标题的既有测试断言。本次只在下列两个原用例中增加检查，原有评级、记录、重试和路由断言全部保留，没有删除或放宽断言，也没有改用例fullname。

| 完整fullname | 修改前 | 修改后 |
|---|---|---|
| `VRShooting.Tests.PlayMode.UI.Screen07_ZeroingFinalRatingUITests.Screen07_FirstRoundPassShowsExcellentFinalRating` | 无标题精确断言 | 标题严格等于`100m精度射校靶 · 训练结算`，并检查字号≥28、居中、左右内边距≥24且相等、关闭换行、实际首选宽≤可用宽、无溢出、1行、字符数与首尾可见 |
| `VRShooting.Tests.PlayMode.UI.Screen08_09_11_MovingTargetUIFlowTests.Screen11_CompletionShowsVariableSequencesAndRetrySendsOneCommand` | 无标题精确断言 | 标题严格等于`移动目标射击 · 训练结算`，并检查同等布局及完整显示要求 |

测试先于实现加入。[修复前红灯XML](evidence/results-title-unify-2026-11-17/playmode-titles-red.xml)为2条/0通过/2失败，两条均精确报出旧标题与新标题不一致；修复后通过。初次测量脚本缺少Bootstrap命名空间导致编译失败，已补齐后重跑，成功证据位于before-layout-measured和final；失败的探针日志保留，没有以其作为验收结果。

**测试与基线逐条比较**

| 验证 | 总数 | 通过 | 失败 | 新增失败 |
|---|---:|---:|---:|---:|
| 定向PlayMode | 27 | 27 | 0 | 0 |
| editmode 指定基线 | 412 | 393 | 19 | — |
| editmode 本次全量 | 412 | 393 | 19 | 0 |
| playmode 指定基线 | 336 | 315 | 21 | — |
| playmode 本次全量 | 336 | 315 | 21 | 0 |

定向过滤器为`Screen07_ZeroingFinalRatingUITests;Screen08_09_11_MovingTargetUIFlowTests;P1FlowPlayModeGateTests;BriefResultsGuidanceRegressionTests`，覆盖最终评级/结算页及相关完整流程。[定向XML](evidence/results-title-unify-2026-11-17/playmode-targeted.xml)。

基线严格使用`docs/codex-reports/evidence/mainmenu-heading-font-2026-11-17/editmode-full.xml`与`playmode-full.xml`。按完整fullname原样比较748条：EditMode412条、PlayMode336条。两平台均无缺失fullname、无新增fullname、无共同项状态变化；失败集合分别19与21条，逐条完全相同。40个既有失败仍为Failed，本次新增失败0。失败消息及堆栈差异另存JSON，不按相似名字映射或豁免。

[本次EditMode全量XML](evidence/results-title-unify-2026-11-17/editmode-full.xml)；[本次PlayMode全量XML](evidence/results-title-unify-2026-11-17/playmode-full.xml)；[748条完整fullname对比CSV](evidence/results-title-unify-2026-11-17/fullname-comparison.csv)；[失败集合、计数与XML SHA256](evidence/results-title-unify-2026-11-17/baseline-comparison.json)；[比较脚本](evidence/results-title-unify-2026-11-17/compare-tests.py)。

**工作区恢复**

开工HEAD为c492f6a1，工作区干净。Unity实例全程串行，启动脚本每次检查没有其它Unity进程。结束后按开工逐文件快照恢复4551个受保护文件的内容；中文UI字体资源、XR设置、生成Prefab、跑批覆盖的旧证据PNG均恢复，4个被跟踪的InitTestScene场景及其meta保留，新增临时场景与临时编辑器脚本清除。[恢复明细与哈希检查](evidence/results-title-unify-2026-11-17/cleanup.json)。

未执行git commit、push、切分支、stash或reset；未手工修改或清理Library，未删除Build/PICO下任何APK。保留的改动为上述10个实现/测试/规格文件、本报告及本任务证据目录。
