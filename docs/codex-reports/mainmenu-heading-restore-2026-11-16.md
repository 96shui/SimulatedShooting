# 主菜单标题条蓝色发光边框恢复（2026-11-16）

已恢复主菜单“选择训练科目”的 hud_capsule 九宫格贴图原色显示。标题条使用贴图自身的亮蓝发光胶囊边框和深色内芯，Image.color 为 Color.white，没有新增描边 Graphic 或 Outline。

## 实现范围

- 删除 MainMenuUI.ResolvePanelSprite 中 Panel_MainMenu_Menu → status_strip_long 的特例；390×60 标题条重新走通用 hud_capsule 选择逻辑。
- 删除 AddPanel 中该标题条的 image.color = fill 特例。ResolvePanelSprite 和 AddPanel 两个方法已与只读 git show ef5a00e 的完整方法内容逐段核对，完全一致，详见[历史方法比较](evidence/mainmenu-heading-restore-2026-11-16/historical-method-comparison.json)。
- 标题条坐标保持 (1360,805)–(1750,865)。文字保持“选择训练科目”、20号、FontStyles.Bold、左对齐、内边距 (20,18)–(-20,-18)、Color32(231,242,235,255)。共用文字工厂和 TacticalUIStyle 会剥除 Bold，且后者会改写小面板内边距，因此在现有共用样式执行后，仅恢复此标题的 Bold 与原内边距。原有 Ellipsis 模式在24px高文字框内会将整行裁空，故仅将此标题设为单行 Overflow，确保六个汉字实际可见；未修改共用样式或其他页面。
- 更新已有 EditMode 样式断言，并在已有 PlayMode 主菜单用例内补入相同展示契约，保留既有覆盖和方法 fullname。BDD02 与 P1/UI task008 仅修正标题条的旧错误描述，服务接口无变化。
- 本次增量仅涉及 MainMenuUI.cs、MainMenuControlsTests.cs、Screen02_04_MainMenuZeroingBriefingUITests.cs、BDD02 和 task008 五个既有文件；[本轮差异](evidence/mainmenu-heading-restore-2026-11-16/task-changes.diff)相对于开始时的未提交工作区生成。介绍页、按钮悬停及敌人逻辑没有本轮增量修改。

## 测试与逐条基线比较

定向 EditMode **16/16** 通过；定向 PlayMode **63/63** 通过，覆盖主菜单、真实 XR/输入替身入口、页面归属、P1 训练闭环及相关 HUD/分析/评级回归。

| 平台 | 指定基线 | 本次全量 | 新增失败 | fullname 缺失/新增 | 状态变化 |
|---|---|---|---:|---:|---:|
| EditMode | 412：393通过 / 19失败 | 412：393通过 / 19失败 | 0 | 0 / 0 | 0 |
| PlayMode | 335：314通过 / 21失败 | 335：314通过 / 21失败 | 0 | 0 / 0 | 0 |

以 mode-intro-pages-2026-11-16/editmode-full.xml 和 playmode-full-final.xml 为基线，比较全部 **747 个完整 fullname**；没有别名、重命名、缺失豁免或基线改写。每条结果、失败消息和堆栈均写入 CSV，两平台失败集合完全相同。全量套件仍含上述原有失败，不能表述为全量全部通过。

- [最终 EditMode 定向 XML](evidence/mainmenu-heading-restore-2026-11-16/editmode-targeted-pass.xml)
- [最终 PlayMode 定向 XML](evidence/mainmenu-heading-restore-2026-11-16/playmode-targeted-final.xml)
- [EditMode 全量 XML](evidence/mainmenu-heading-restore-2026-11-16/editmode-full.xml)
- [PlayMode 全量 XML](evidence/mainmenu-heading-restore-2026-11-16/playmode-full.xml)
- [逐条比较 JSON，含 XML SHA256、失败全集及差异集合](evidence/mainmenu-heading-restore-2026-11-16/baseline-comparison.json)
- [747 条完整 fullname 比较 CSV](evidence/mainmenu-heading-restore-2026-11-16/fullname-comparison.csv)

保留修复前的 editmode-heading-before.xml：期望 hud_capsule，实际 status_strip_long。中间定向运行还捕获到共用样式将 Bold 及内边距改写的两个真实偏离，修正后通过。首次出图发现原标题的省略模式在指定内边距下会裁空文字，新增真实字形可见性断言并先复现失败（editmode-heading-visibility-before.xml），再修正并重新完成定向和全量回归。首次图像及先前回归归档在 before-text-visibility/。截图辅助脚本首次缺少现有场景加载器的命名空间，修正后编译渲染退出码0，编译日志另存 gallery-compile-before.log。扩展到完整 P3ProductionFlowTests/XRSceneHandoffTests 类的 85 条预检为81通过/4失败，这4条全部属于指定全量基线的既有失败；最终63条定向包含前批62条用例及主菜单无VR输入替身入口，用例无删除、无放宽断言。中间记录均保留供追溯。

## 截图证据

两张均为 **1920×1080**，在隔离 Unity PreviewScene 中直接 Camera.Render，包含真实运行时 UI 与原 UI 背景；全景为原画幅，入口列特写由相机放大，未进行图像重绘、拼接或后期改色。逐张人工目检标题条亮蓝发光边、深色内芯、白色中文以及与01–04的蓝色边框统一性。

- [主菜单全景](evidence/mainmenu-heading-restore-2026-11-16/Screen_MainMenu.png)
- [右侧入口列特写](evidence/mainmenu-heading-restore-2026-11-16/Screen_MainMenu_Closeup.png)
- [真实组件检查：贴图、tint、坐标、文字和无描边](evidence/mainmenu-heading-restore-2026-11-16/gallery-inspection.txt)
- [PNG 尺寸与 SHA256](evidence/mainmenu-heading-restore-2026-11-16/screenshot-manifest.json)
- [渲染源存档](evidence/mainmenu-heading-restore-2026-11-16/MainMenuHeadingEvidence.cs.txt)与[完整渲染日志](evidence/mainmenu-heading-restore-2026-11-16/gallery.log)

视觉参照为 ui-chinese-copy-2026-11-12/gallery/Screen_MainMenu.png；本轮没有恢复参照图中的旧按钮布局或操作介绍入口。

## 工作区保护

开始时快照保护 4,390 个文件。批跑与渲染结束后，从本轮快照恢复资源和历史证据，并逐字节 SHA256 核对其余 4,385 个受保护文件全部一致。被跟踪 Assets/InitTestScene*.unity 及其 meta 均保留；只清理本轮临时生成、且不在快照或 Git 跟踪列表中的测试场景与批跑证据。临时 Editor 截图脚本及 meta 已删除，源代码存档在本证据目录。

同一时间只有一个 Unity 实例，所有测试与截图串行运行。没有执行 git commit、push、切分支、stash、checkout 或 reset，HEAD保持 9a1c01224a7e87ed84c6d54fe47cec4e46d5422a。原有全部未提交改动和历史证据均已保留。[清理与哈希核对记录](evidence/mainmenu-heading-restore-2026-11-16/cleanup.json)、[最终验证](evidence/mainmenu-heading-restore-2026-11-16/final-validation.json)、[初始状态](evidence/mainmenu-heading-restore-2026-11-16/initial-git-status.txt)、[最终状态](evidence/mainmenu-heading-restore-2026-11-16/final-git-status.txt)可供核查。
