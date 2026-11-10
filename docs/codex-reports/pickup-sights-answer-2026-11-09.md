# 模式一取枪提示、瞄具基准和分析答案

日期：2026-11-09。Unity 2022.3.62f3c1；在上一轮未提交的随机偏移、≤5cm 通过与操作介绍改动上继续，未 commit / push，未清理工作区或删除 Library。以下截图及测试均为无头显的编辑器验证，PICO 4 验收仍待进行。

## 一、取枪提示

世界空间提示位于 `Weapon_training-rifle_Blockout.prefab/Prompt_training-rifle_Pickup`，生成源为 `TrainingRiflePrefabBuilder.CreatePickupPrompt`。Unity batchmode 加载实际 `ZeroingRangeScene`，只在用于截图的临时场景实例上强制显示提示，并从已保存的射击位相机位置朝枪观察；没有改动生产的触发、显隐或文本。修改前可见文字镜像并横穿枪体；修改后文字正读、位于枪身上方，并与瞄具、握把分开。

| 属性 | 修改前 | 修改后 |
|---|---|---|
| 预制体局部位置 | `(0, 0.18, -0.04)` m | `(0, 0.25, -0.04)` m |
| 预制体局部欧拉角 | `(0°, 180°, 0°)` | `(-5°, 67°, 0°)`；Unity 日志也可显示为 `(355°, 67°, 0°)` |
| TextMesh characterSize | `0.012` | `0.004` |
| 文字 | `GRIP TO PICK UP` | 不变 |

已保存场景内枪体实例世界旋转为 `(0°, 0°, 0°)`，位置约 `(0.42, 0.9077, 0.5172)`；桌面相机约 `(0, 1.5, 0.3)`，卧姿头部参考点约 `(0, 0.72, 0.3)`。旧旋转使 TextMesh 可读面背向观察者。新方向同时兼顾两个参考点：可读面朝向视点的点积约为桌面 `0.744`、卧姿 `0.779`。这不是简单假定模型 forward 必为 −Z；选择依据是实际预制体、场景摆放、TextMesh 面向和两次渲染。头显真实头位可变，仍需实机复核。

证据：

- 世界提示：[修改前](evidence/pickup-prompt-2026-11-09/before-world.png)、[修改后桌面相机](evidence/pickup-prompt-2026-11-09/after-final-world.png)、[修改后卧姿参考点](evidence/pickup-prompt-2026-11-09/after-final-world-prone.png)。两张修改后截图仅旋转临时相机以观察提示；实际头显视角没有被修改。
- 主菜单简报 HUD：[修改前](evidence/pickup-prompt-2026-11-09/before-hud.png)、[修改后](evidence/pickup-prompt-2026-11-09/after-final-hud.png)。这是用真实生成 UI 和 `TrainingPresentationView` 的等待取枪快照渲染；提示一直位于 `(480,46)–(1440,84)`，本地旋转 `(0°,0°,0°)`。简报框底部 `y=90`、返回/开始按钮底部 `y=125`，射击位状态位于 `y=9–41`，均不重叠。
- `ZeroingRangeUI` 的取枪页另外生成一个同名文本，并不是上述简报实例。其原始运行时布局为 `(610,615)–(1310,700)`、旋转 `(0°,0°,0°)`、字号 38；现统一为 `(480,46)–(1440,84)`、旋转不变、字号 24，保证实际字形能放入高度 38 的安全区。取枪页的按钮坐标和触发逻辑未改。[修改前编辑器预览](evidence/pickup-prompt-2026-11-09/range-check-range-hud.png)、[修改后](evidence/pickup-prompt-2026-11-09/after-final-range-hud.png)。修改前预览在画布尚未取得尺寸时还触发了通用小面板样式，导致文字被压成无效尺寸；[修改前 PlayMode 红灯](evidence/pickup-prompt-2026-11-09/range-hud-red.xml)在正常运行时则测到上述 `(610,615)` 起点，不能将预览的空白直接当成设备运行画面。`TacticalUIStyle` 现在对该整页绝对定位文字跳过小胶囊的拉伸规则。

批处理捕获辅助代码留作 [文本证据](evidence/pickup-prompt-2026-11-09/CodexPickupEvidenceCapture.cs.txt)，没有把临时脚本留在 Unity `Assets` 中。

## 二、模式一瞄具默认基准

- `ZeroingService.EnsureRecord` 新建会话时设置觇孔 `+1` 格和准心柱 `+5°`。`SightAdjustmentDto` 的现有字段保持类型和调用点不变：觇孔正数为向前、负数为向后；准心柱正数为顺时针、负数为逆时针；零表示中位。
- `ZeroingRules.ApplyCorrection` 使用实际 `ProposedCorrectionCm` 已量化的机械格数和度数作有符号累加：X 正向前、负向后；Y 负顺时针、正逆时针。按机械步长取整而非对浮点绝对值再次向上取整，避免 `11.840001cm` 误加一个 5° 步长；`ApplyAdjustment` 的方向分支也按同向加、反向减处理。
- 分析 DTO 携带当前机械位置；帮助中的“准心柱”“觇孔”和射击 HUD 的新瞄具状态均从服务数据刷新。`ZeroingHudService` 在调整应用事件时发布 HUD 更新，下一轮查询也取得累计值。
- 对固定种子初始偏移、`ComputeImpactPoint` 及原始射击记录、量化补偿和应用后偏移模长 `≤5cm` 通过口径均未因新基准作改变。新测试以本轮平均 `(-8,12)cm` 为例，验证实际待应用为向前 4 格、顺时针 185°，加上基准后为向前 5 格、顺时针 190°；反向操作也覆盖跨零、重复应用幂等和 HUD 事件。

## 三、分析页答案

`MainMenuUI.BuildZeroingImpactAnalysis` 在帮助按钮右侧加入同尺寸、同样式的 `Button_ZeroingImpactAnalysis_Answer`，点击独立切换 `Panel_ZeroingImpactAnalysis_Answer`。面板与帮助可同时展开，不覆盖；既有帮助、应用和继续按钮的坐标与导航门禁未移动。离开分析页或进入下一轮自动收起，重新进入时从当前分析 DTO 刷新。

`ZeroingRules.BuildRoundAnalysis` 计算并返回当前固定偏移、实际待应用量对应的有符号机械位置、应用后预计偏移模长及是否满足 5cm；UI 仅格式化展示。未应用时预计偏移为当前固定偏移加 `ProposedCorrectionCm`，应用后则取已更新的固定偏移，避免重复相加。答案显示 X/Y、觇孔方向和格数、准心柱方向和度数、预计模长、通过结论和 1/2/3 轮评级。帮助中的均值建议是估算，界面注明实际待应用值以控件和答案为准；手动改动待应用量后，答案如实反映它，即使该量无法一次通过也不会悄悄改写玩家选择。[帮助与答案同时展开的实际测试截图](evidence/pickup-prompt-2026-11-09/P1-analysis-answer-and-help.png)。

## 测试与旧断言同步

仅运行射校规则/服务/HUD、分析页、主菜单及相关 P1 门禁和一个场景提示测试；没有运行模式二、三、四的战斗测试。以下数值是 Unity 结果文件中的实际计数，历史失败均计入失败，跳过均为 0。

| 测试集 | 通过 | 失败 | 结果 |
|---|---:|---:|---|
| EditMode：射校规则、射校服务、HUD 服务、主菜单操作介绍 | 57 | 9 | [XML](evidence/pickup-prompt-2026-11-09/editmode-final.xml) / [日志](evidence/pickup-prompt-2026-11-09/editmode-final.log) |
| PlayMode：主菜单、射校 HUD、分析页、最终评级、P1 流程、场景提示 | 34 | 3 | [XML](evidence/pickup-prompt-2026-11-09/playmode-final.xml) / [日志](evidence/pickup-prompt-2026-11-09/playmode-final.log) |

分析页 15/15、主菜单 10/10、射校 HUD 5/5、场景取枪提示 1/1。新增用例覆盖两种 HUD 的安全区、实际文字字形、默认瞄具、双向跨零、真实应用及幂等、HUD 事件、答案按钮/内容/独立显隐/不重叠/调整和应用刷新/下一轮收起。新会话默认值的先行测试在旧实现上为 0/1，结果见 [TDD 红灯](evidence/pickup-prompt-2026-11-09/tdd-red.xml)。

旧断言同步：`ZeroingServiceTests.ApplyAdjustment_IsIdempotentForSameRound` 的基准从原来的 0/0 更新为 `+5°/+1格`，即后照门旧期望由 4 改为 5，前照门历史 188° 期望加上基准变为 193°。它仍与当前实际量化的 `185° + 5° = 190°` 不一致，并按用户要求继续计为历史失败；新测试独立验证实际 190°。其余旧的 188° 建议值断言不改成通过。

9 项 EditMode 历史失败具体为：

1. `ZeroingRulesTests.ComputeFrontSightDegrees_UsesCeilingAnd064CmPerDegree`：旧期望 188°，现行 5° 量化为 190°。
2. `ZeroingServiceTests.ApplyAdjustment_IsIdempotentForSameRound`：原旧期望 188°，加新 5° 基准后仍期望 193°；现行实际是 190°，如上所述仍计失败。
3. `Bdd06_VerticalAdjustmentCanContinueForMultipleTurnsAndApplyOnce(-1)` 和 `(1)`：旧测试按 0.064cm 单步及无截断假设，现行步长为 0.32cm，量化范围为 ±50cm。
4. `Bdd06_VerticalAdjustmentDoesNotTruncateALargeSuggestedAngle(64,-1)` 和 `(-64,1)`：旧测试依赖靶外 64cm 弹着参与建议；现行平均值排除靶外点。
5. `Bdd30_HelpRecommendationStaysBasedOnRoundImpactsWhilePendingCorrectionChanges`、`CompleteRound_CalculatesOffsetDirectionsAndAdjustmentAmounts`：旧期望 188°，现行建议为 190°。
6. `Bdd30_ManualPreviewCommitsSignedCorrectionWithoutChangingHistory`：旧期望 −12cm，现行量化值为 −11.84cm。

3 项 PlayMode 历史失败为 `P1Flow_InputStub_Round1Pass_ShowsExcellentFinalRating`、`P1Flow_InputStub_ThreeRoundsFail_ShowsFailFinalRating` 和 `Screen07_FirstRoundPassShowsExcellentFinalRating`：它们要求逐发坐标文本包含“第1轮：通过”或“第3轮：未通过”；当前状态文字在另一摘要处。没有为消除这些失败而改变结算文案或玩法范围。

## 文档、线框与遗留验收

- 已同步 `docs/README.md`，BDD 04/05/06，接口 01/03/04，P1 task004/006/007/008/009/010，以及 draw.io `p06` 的帮助、答案和答案面板示意。
- 未连接 PICO 4，未做打包或 VR 真机测试。需在头显中检查真实卧姿及头部移动时世界提示是否始终正读、与枪不遮挡，简报和取枪页底部提示是否在安全视野，HUD 瞄具状态可读性，手柄分别开关帮助/答案，以及量化调整、应用后立即评级和进入下一轮。
- 训练步枪预制体为共享资源；修改只针对提示视觉，但其他使用该预制体的场景仍应实机确认视角，不声称已验证模式二或战斗模式。
- Unity 本轮自动改写过在任务开始时干净的动态字体缓存和 OpenXR 设置，测试结束后仅将这两个自动生成改动恢复到任务开始前的内容；此前所有未提交改动和 `Exports/` 均保留。
