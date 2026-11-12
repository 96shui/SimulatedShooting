# 模式一 UI 截图交付（2026-11-12）

本目录交付 7 张 1920×1080 PNG，覆盖模式选择、任务简报、第一轮 HUD、集中分析、分散分析、最终评级和操作介绍。交付副本位于 `C:/Users/10532/WorkBuddy/Claw/send/mode1-ui/`，该目录只放这 7 张 PNG。

这是 `TacticalUIGallery` 在隔离预览场景中渲染的真实 UI，分析和评级使用既有编辑器预览样本及生产 `ZeroingRules` 生成的 DTO；不是头显实射录像。本次只补编辑器导出能力和交付说明，不改运行时玩法、BDD、接口或清靶代码。

## 截图、规则与旧版可见差异

旧版基准目录为 `../ui-art-completion/`。本次使用编号文件名，下面按原来的同页面 `Screen_*.png` 名称对应比较；旧目录没有独立的操作介绍或分散态截图。

| 本次截图 | 页面 / 状态与可证明内容 | 旧版对应截图与可见差异 |
|---|---|---|
| [01-mode-select.png](01-mode-select.png) | `Screen_MainMenu`，操作介绍关闭。四个训练入口与下方“操作介绍”按钮可见，模式一入口高亮；对应 BDD02“主菜单打开并关闭操作介绍”的默认状态。 | `Screen_MainMenu.png`：旧图科目下方为空，新图增加“操作介绍”按钮；保留训练中心背景和四个科目布局。 |
| [02-briefing.png](02-briefing.png) | `Screen_ZeroingBriefing`。可见每轮 **4 发**、共 3 轮、100m 单发；正文列出集中门槛（距均值 <10cm、两两间距 <15cm）及两条通过路径。对应 BDD04“任务说明显示完整规则”和 BDD06 的集中及通过规则。 | `Screen_ZeroingBriefing.png`：旧图为每轮 3 发、“3发全部进入10环”；新图改为四发，并展示集中条件以及直接判定 / 应用调整后判定。 |
| [03-hud-round1.png](03-hud-round1.png) | `Screen_ZeroingHud`，第一轮未开枪。可见轮次 **1/3**、距离 **100m**、弹数 **4/4**、“待记录 4 发”、右肩、稳定度 82%、觇孔“向前 1 格”、准心柱“顺时针 5°”。预览 `AmmoDto` 为弹匣 4、容量 4、备用 8，即 **4+8**。对应 BDD05 的四发 HUD 与初始瞄具规则。 | `Screen_ZeroingHud.png`：旧图轮次、距离、弹数、肩侧与稳定度均是占位值，靶区为灰底同心圆；新图显示完整状态、初始瞄具位置和绿色胸靶，未射击时靶上无弹着点。 |
| [04-analysis-pass.png](04-analysis-pass.png) | `Screen_ZeroingImpactAnalysis`，**集中判定成立、尚未通过训练、未应用调整**。四发坐标为 (9,6)、(15,6)、(12,12)、(22,18)cm；前三发构成计算集，黄点 (12,8)cm、蓝点 (0,0)cm。四发同样绘制，不额外标记舍弃的一发。帮助、答案及四个手动调整按钮可见，“应用调整”可用。垂直按钮明确显示“顺时针 5° / 逆时针 5°”。对应 BDD06“严格的集中门槛”“显示四发及补偿”“手动量化调整”。 | `Screen_ZeroingImpactAnalysis.png`：旧图只有三点同心圆、偏差和“调整已应用”；新图为绿色胸靶上的四点、黄蓝点、四发坐标、帮助/答案、水平向前/向后与垂直顺时针/逆时针调整，底部提示“应用后均值距靶心小于5cm可通过”。 |
| [05-analysis-spread.png](05-analysis-spread.png) | 同一分析页的**分散拒绝态**，四发为 (−20,−20)、(20,−20)、(−20,20)、(20,20)cm。可见“弹着点过于分散，本次射击无参考价值，不可用于调整，请进行下一轮。”；水平减/加、垂直减/加和应用调整 **五个按钮均置灰禁用**，帮助与答案隐藏，“进入下一轮”可用。对应 BDD06“分散处置”。 | 旧目录无分散态同名图；相对 `Screen_ZeroingImpactAnalysis.png`，新增明确的分散拒绝文案、四个离散红点和五个禁用按钮，不能误读为“调整已应用”。 |
| [06-final-rating.png](06-final-rating.png) | `Screen_ZeroingFinalRating`。评级为**及格**；三轮均显示 **4 发**，第 1、2 轮未通过，第 3 轮通过。三轮记录列出全部弹着坐标、计算集黄点与实际补偿，并显示三张胸靶缩略图。评级由 `ResolvePassedRoundIndex` 和 `ComputeFinalGrade` 计算。对应 BDD07 的最终评级与三轮记录、BDD06 的应用后通过路径。 | `Screen_ZeroingFinalRating.png`：旧图每轮 3 发，只显示平均偏差与同心圆缩略图；新图每轮 4 发，增加逐发记录、黄点/补偿及绿色胸靶缩略图。新旧示例均为第三轮通过的“及格”。 |
| [07-controls.png](07-controls.png) | `Screen_MainMenu` 下 `Panel_MainMenu_Controls` 打开。可见“操作介绍”标题、通用 VR / 模式一 / 模式二 / 模式三四分组说明和“返回”按钮；模式一正文列出每轮 4 发、共 3 轮、水平 1 格=2cm、垂直 5 度=0.32cm及两条通过路径。对应 BDD02“主菜单打开并关闭操作介绍”和 P1 UI task008。 | 旧目录没有 `Screen_MainMenu_ControlsOpen.png` 或操作介绍独立图；相对 `Screen_MainMenu.png`，新图展示主菜单上的两栏说明面板及返回按钮。 |

`04-analysis-pass` 中的 pass 指集中规则通过；黄点距靶心约 14.42cm，不能把这张图解释为训练已经通过。第三轮结算样本的黄点为 (4,4.8)cm，实际补偿为 (−2,−1.92)cm，应用后为 (2,2.88)cm、模长约 3.51cm，满足集中且严格小于 5cm的通过路径。

**截图证据边界：** HUD 图直接证明弹匣 `4/4`，备用弹药 **8 未在该画面单独绘制**；4+8 的完整配置可核对 `TacticalUIGallery.FillRangePreview` 中的 `AmmoDto`。顺/逆时针的按钮文字可以由分析图确认，正负补偿的数值语义以接口04为准（顺时针 +0.32cm，逆时针 −0.32cm），静态截图不证明实际输入步进。未扩展此次范围去修改 HUD 或补拍额外状态。

## 导出方式与验证

本次只启动一次 Unity 2022.3.62f3c1。启动前无 Unity 进程；使用 `-batchmode -quit` 并保留图形设备，没有 `-nographics`；进程 PID 为 41064，退出码为 **0**。环境变量为 `RUNTIME_GALLERY_MODE1=1`、`RUNTIME_GALLERY_STATES=0`。本次增加的操作介绍导出不依赖额外状态开关。

```powershell
$env:RUNTIME_GALLERY_OUT = 'E:/新建文件夹 (3)/SimulateShooting/docs/codex-reports/evidence/mode1-ui-2026-11-12'
$env:RUNTIME_GALLERY_MODE1 = '1'
$env:RUNTIME_GALLERY_STATES = '0'
& 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -quit `
  -projectPath 'E:/新建文件夹 (3)/SimulateShooting' `
  -executeMethod VRShooting.Editor.TacticalUIGallery.CaptureBatch `
  -logFile 'E:/新建文件夹 (3)/SimulateShooting/Logs/mode1-ui-2026-11-12/capture-controls.log'
```

`MainMenuUI.cs` 实际通过 `Button_MainMenu_Controls.onClick` 调用 `mainMenuControlsPanel.gameObject.SetActive(true)`，通过返回按钮设为 false。画廊先导出关闭状态的 `01-mode-select.png`，再调用同一按钮事件打开并导出 `07-controls.png`，最后在 `finally` 中调用返回事件关闭并断言还原。面板是 `Screen_MainMenu` 的子节点，本次正常显示，无需更改 `LargePanelRoot` 或运行时层级。

画廊本次编译并完成七图导出。既有 `CaptureSpreadState()` 保留，其断言确认分散成立、五个调整/应用按钮不可交互、帮助与答案隐藏；新增操作介绍导出检查默认关闭、打开后父层级有效及最终关闭。日志中的 `GalleryState` 记录每页文本与按钮状态。人工逐张检查本目录七图及旧版五张对应页面；未生成拼图或对比图，未另跑 EditMode / PlayMode 全量测试或 VR 实机验收。

后续 VR 手工验收：主菜单用 UI 射线打开操作介绍，核对头显内两栏文字可读，再点击返回，确认四个训练入口恢复可用；本次桌面预览不替代头显内可读性和真实输入验收。此次没有新增玩法功能或变更其测试契约。

## 资源还原与工作区

上一轮六张 PNG 在启动前备份并以 SHA-256 核对，交付保留其原始字节；本次新增第七张。`TrainingChinese.asset` 还原至 HEAD；检查 OpenXR Settings、所有 Prefab、场景与已跟踪 PNG，均不保留跑批差异。仅按 Git 跟踪清单清理未跟踪的 `InitTestScene*.unity` 及配套 `.meta`，四组已跟踪历史文件全部保留。

开始时另有上一项清靶回归交付的报告追加内容及 4 份 XML、1 份 JSON。本次先将完整文件备份至忽略目录 `Logs/mode1-ui-2026-11-12/pre-capture/`，再还原报告并移出那 5 份未跟踪证据，使工作区只保留本任务要求的画廊代码和本证据目录；未更改清靶实现或测试。备份中的 `prior-clear-impact-evidence/` 保留那 5 份原始证据。

交付核对包括七张 PNG 尺寸、源目录与 send 副本 SHA-256 一致、send 仅七张 PNG、`git diff --check`、已跟踪历史临时场景仍在及无 Unity 残留进程。send 中额外出现的 7 张同名 JPG 副本已移到 `Logs/mode1-ui-2026-11-12/pre-capture/send-jpeg-sidecars/`，不放入交付目录。未执行 `git commit` 或 `git push`。

## 追溯资料

- [BDD02 主菜单](../../../BDD/screens/02-游戏主界面.feature.md)、[BDD04 任务说明](../../../BDD/screens/04-100m任务说明.feature.md)、[BDD05 HUD](../../../BDD/screens/05-100m射击HUD.feature.md)、[BDD06 弹着分析](../../../BDD/screens/06-弹着分析.feature.md)、[BDD07 最终评级](../../../BDD/screens/07-射校最终评级.feature.md)。
- [接口01 页面导航与 UI 事件](../../../接口文档/01-页面导航与UI事件.md)、[接口03 HUD](../../../接口文档/03-HUD显示数据.md)、[接口04 射校服务](../../../接口文档/04-100m射校服务.md)。
- [P1 UI task008](../../../任务规格/阶段一-P1-100m射校原型闭环/UI/task008-ui-mainmenu-zeroing-briefing.md)、[task009](../../../任务规格/阶段一-P1-100m射校原型闭环/UI/task009-ui-zeroing-hud.md)、[task010](../../../任务规格/阶段一-P1-100m射校原型闭环/UI/task010-ui-impact-analysis.md)、[task011](../../../任务规格/阶段一-P1-100m射校原型闭环/UI/task011-ui-final-rating.md)。
