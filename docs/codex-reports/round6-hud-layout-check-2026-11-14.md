# round6 模式三/四 HUD 布局复核

2026-11-14，Unity 2022.3.62f3c1。**判定：所述建筑 HUD 黄字重叠、堑壕左面板越界均不是真实 UI 布局问题；问题来自旧证据的截图渲染路径和场景背景干扰。本次没有修改 UI 坐标、尺寸、文案、功能或交互逻辑，新增失败为 0。**

正式布局证据已统一为 1920×1080 纯 UI 画廊，并更新原 [round6 报告](round6-2026-11-14.md) 的证据索引。旧图保留原字节，作废其布局验收用途。

本工作属于 P3 审核测试与优化，依据阶段审核清单、UI task005/008/011、BDD15/19/20/35、接口03/11/27及 draw.io 的页面结构执行。没有新增功能任务或改变契约。

正式图片：

| 页面 | 1920×1080 正式证据 |
|---|---|
| 模式三堑壕 HUD | [gallery-fix/Screen_TrenchHud.png](evidence/round6-2026-11-14/gallery-fix/Screen_TrenchHud.png) |
| 模式四街道 HUD | [gallery-fix/Screen_UrbanStreetHud.png](evidence/round6-2026-11-14/gallery-fix/Screen_UrbanStreetHud.png) |
| 模式四建筑 HUD | [gallery-fix/Screen_UrbanBuildingHud.png](evidence/round6-2026-11-14/gallery-fix/Screen_UrbanBuildingHud.png) |

这些图通过既有 `VRShooting.Editor.TacticalUIGallery.CaptureBatch` 构建真实 UI。沿用 11 月 12 日的画廊取景：正交相机位于 `(960,540,-1000)`，`orthographicSize=540`，1920×1080 RenderTexture；Canvas 设为 WorldSpace、尺寸1920×1080、中心 `(960,540,0)`、缩放1，临时禁用 CanvasScaler 和 TrainingUICanvasAdapter。背景纯色，无场景几何。画廊仍使用其原有预览 DTO，截图中的预览血量/任务进度不代表一局生产任务的实时状态。当前弹药30/120、备用4匣及拐角文案来自当前代码。

坐标原点统一为**图片左上角**，单位像素，矩形记为 `(x,y,宽,高)`。原 Unity UI 构建代码的左下角坐标已换算；原始 RectTransform 与 TMP 每行字形网格边界保存在各图同名 `.layout.json`。

| 页面 | 本次实测关键区域 | 对照 11 月 12 日旧画廊 |
|---|---|---|
| 堑壕 | 左侧面板 `(50,475,510,210)`；提示框 `(650,755,620,65)`；任务状态 `(650,825,620,35)` | 左侧框青色像素差异 **0**；提示文字黄色像素差异 **0** |
| 街道 | 左侧面板 `(50,475,510,240)`；提示框 `(650,755,620,65)`；任务状态 `(650,825,620,35)`；进入建筑按钮 `(760,885,400,75)` | 左侧框青色像素差异 **0**；两行提示黄色像素差异 **0** |
| 建筑 | 房间面板 `(50,475,510,330)`；提示框 `(650,755,620,65)`；任务状态 `(650,825,620,35)`；开门按钮 `(700,885,290,75)` | 左侧框青色像素差异 **0**；两行提示黄色像素差异 **0** |

对照文件为 `evidence/ui-chinese-copy-2026-11-12/gallery/Screen_{TrenchHud,UrbanStreetHud,UrbanBuildingHud}.png`。分别提取各左面板范围内的青色边框像素，以及 `(650,750)–(1270,825)` 内的黄色文字像素进行逐像素比较；没有缩放、裁图后重排或重绘证据。脚本 [verify-layout.py](evidence/round6-2026-11-14/gallery-fix/verify-layout.py) 及 [layout-verification.json](evidence/round6-2026-11-14/gallery-fix/layout-verification.json) 可复算：**32/32 检查通过**。

建筑底部进一步实测如下。第一行与第二行属于同一个 TMP 提示组件，所以同时记录每行字形边界和相邻控件的 RectTransform，避免只凭文本框估计。

| 元素 | 当前画廊实测 | 间距/可见性 |
|---|---|---|
| 提示第一行字形 | `(679.93,755.99,558.78,28.95)` | 到第二行字形顶边 **7.98px** |
| 交互提示第二行字形 | `(678.81,792.92,560.31,28.76)` | 到任务状态字形顶边 **10.48px** |
| 任务状态控件 | `(650,825,620,35)` | 控件底边860，到按钮顶边885有 **25px** |
| 开门按钮 | `(700,885,290,75)` | 当前显示；与检查房间预留框水平间隔 **20px** |
| 检查房间按钮预留框 | `(1010,885,290,75)` | **当前隐藏，activeInHierarchy=false**；保留既有显隐逻辑 |

堑壕面板左边界为50、右边界560、上边界475、下边界685，全部位于1920×1080内。敌情文本框 `(80,515,450,50)`、搜索文本框 `(80,580,450,50)` 均完整落在面板区域。本次没有 UI 修改，因此不存在修改前后布局差异。

旧实拍图的具体差异来源已用原生产用例回放，并分别渲染“场景+UI”“仅UI”“仅场景”，全过程不改 UI 坐标：

| 参数/步骤 | 旧实拍路径及回放实测 | 与本次画廊的关系 |
|---|---|---|
| 相机 | `PresentationEvidence.Capture` 复制生产桌面相机；FOV保持 **60°**；输出相机重置投影矩阵，aspect设置为 **16:9** | 画廊为正交；本次没有发现 FOV 数值被意外改变 |
| 原屏幕 | 批处理运行时 `Screen.width/height=640×480`，生产相机原aspect=4:3 | 输出PNG实际为1920×1080，未将640×480位图放大后冒充1080p |
| CanvasScaler | 生产 `ScaleWithScreenSize`、reference=1920×1080、`Expand`；捕获前 scaleFactor=1/3、Canvas逻辑尺寸1920×1440 | 输出到1920×1080后实测 scaleFactor=1；关键矩形与画廊最大差值仅堑壕 **0.0196px**、建筑 **0.00174px**。Scaler没有造成所述越界/挤压 |
| 截图工具临时改写 | 原 `ScreenSpaceOverlay` 在 `includeOverlay=true` 时改成 **ScreenSpaceCamera**，`worldCamera=outputCamera`、`planeDistance=1`；与场景共用深度，再调用 `Camera.Render` | 原本屏幕叠加的 HUD 在这条临时截图路径中受到近处场景/枪模深度遮挡；画廊没有这些几何 |
| 建筑背景 | “仅场景”图完全移除 HUD 后，仍能看到道路双黄线；长黄线所在行 **y=804–809、818–823** | 与第二行文字 y≈793–822 叠在同一区域，产生两行黄字压在一起的观感；文字实际间距仍为7.98px |

回放数据见 [trench-hud-30-120-camera.txt](evidence/round6-2026-11-14/gallery-fix/replay/trench-hud-30-120-camera.txt)、[urban-hud-30-120-camera.txt](evidence/round6-2026-11-14/gallery-fix/replay/urban-hud-30-120-camera.txt) 及 `replay/*-before-*.layout.json`、`replay/*-converted-*.layout.json`。捕获前640×480 Overlay的坐标与输出1080p的坐标按各自尺寸记录，不混作同一像素网格。

在堑壕回放中，旧 `trench-hud-30-120.png` 被**逐字节复现**。固定输出相机、FOV、Canvas、分辨率，只去除场景几何的渲染后，左面板恢复完整；有场景时缺少了1640个青色边框像素，纯UI路径该区域有5637个。缺口沿土坡轮廓，面板x坐标始终约50，属于深度遮挡。枪模同样遮住提示和底部队友面板的局部。

可直接查看 [堑壕仅UI对照](evidence/round6-2026-11-14/gallery-fix/replay/trench-hud-30-120-ui-only.png)、[建筑仅UI对照](evidence/round6-2026-11-14/gallery-fix/replay/urban-hud-30-120-ui-only.png)、[建筑仅场景对照](evidence/round6-2026-11-14/gallery-fix/replay/urban-hud-30-120-scene-only.png)。[verify-capture-path.py](evidence/round6-2026-11-14/gallery-fix/verify-capture-path.py) 与 [capture-path-verification.json](evidence/round6-2026-11-14/gallery-fix/capture-path-verification.json) 记录 **9/9** 路径验证通过。临时探针源码、回放补丁及画廊探针源码作为 `.cs.txt`/`.patch` 留在证据目录，运行后已从 Assets 还原/移除。

旧证据处理：

| 旧图 | 处理与原因 |
|---|---|
| `round6-2026-11-14/trench-hud-30-120.png` | **布局验收作废**；截图工具临时改Canvas模式，导致土坡和枪模遮住HUD。原文件保留；正式图改用 `gallery-fix/Screen_TrenchHud.png` |
| `round6-2026-11-14/urban-hud-30-120.png` | **布局验收作废**；道路双黄线与提示同色同区域、枪模遮挡中间文字和按钮。原文件保留；正式图改用 `gallery-fix/Screen_UrbanBuildingHud.png` |
| 模式四街道旧实拍 | round6归档及原manifest中没有独立的街道HUD实拍文件；本次补齐 `gallery-fix/Screen_UrbanStreetHud.png`，不把建筑HUD归为街道证据 |

房间和楼层只核对显示，没有修改。建筑HUD当前按楼层显示房间，并非同时列出全部五间。一层有“一层1号/一层2号”，二层有“二层1号/二层2号”，三层有“三层1号”。楼层文本框 `(80,505,450,45)`，房间列表框 `(80,565,450,200)`；三层文字均无TMP溢出、没有屏幕裁切。房间字形网格左侧约0.22px的SDF留白按0.5px容差计入，距屏幕左缘仍约80px。补图为 [二层](evidence/round6-2026-11-14/gallery-fix/Screen_UrbanBuildingHud_Floor2.png)、[三层](evidence/round6-2026-11-14/gallery-fix/Screen_UrbanBuildingHud_Floor3.png)。楼层小地图仍沿用原画廊预览数据；这两张补图只用于房间与楼层文字容量复核。

手雷补图为 [grenade-player-heading-and-landing.png](evidence/round6-2026-11-14/gallery-fix/replay/grenade-player-heading-and-landing.png)。采用同一空敌人核心夹具和生产投雷服务、真实世界Sweep碰撞及M67投射物，改用外部俯视相机，同时显示玩家位置/朝向和实际落点。青色箭头标示服务快照的玩家 `(24,0,52)`、朝向 `+X`；黄色圈标示实际落点 `(42,0.08,52)`，与计划目标一致；紫线后半段为服务逐步采样的轨迹，起点到首次采样点为连线。箭头、圈、文字及轨迹线是临时证据标记，没有写进产品场景。原近景图保留；其相机是在手雷旁近距离取景，只能证实投射物可见，无法从图中判断玩家朝向。[数值记录](evidence/round6-2026-11-14/gallery-fix/replay/grenade-direction.txt)与补图配套。

本次没有改 UI，因此按要求仅跑定向回归，没有重新执行全量711条。基线采用 round6 的 `editmode-full.xml` 与 `playmode-full.xml`，分别389（369通过/20失败）、322（299通过/23失败），按**每条完整fullname**比对本次已执行用例。

| 验证 | 本次总数 | 通过 | 失败 | 新增失败 |
|---|---:|---:|---:|---:|
| EditMode：BDD20中文楼层、五房和ID保留 | 1 | 1 | 0 | **0** |
| PlayMode：P3 UI任务夹具全类、三HUD文案、两模式生产HUD/换弹、无敌人手雷 | 13 | 10 | 3 | **0** |
| 画廊布局/历史像素/三层文字容量脚本 | 32 | 32 | 0 | 0 |
| 截图路径对照脚本 | 9 | 9 | 0 | 0 |

PlayMode三条旧失败均位于 `VRShooting.Tests.PlayMode.UI.P3UiTask002_005_008_011PlayModeTests`，状态与失败消息逐字匹配round6基线：

| 测试方法 | 原有失败断言 |
|---|---|
| `Screen18_UrbanMapSelection_RendersRangesAndStartsStreet` | 期待`1-2`，实际`建筑内部敌人：3-6` |
| `Screen20_UrbanBuildingHud_SeparatesDoorAndCheckCommands` | 期待`未搜索`，实际`房间1 · 靠近后开门` |
| `Screen21_UrbanResults_RendersThreeFloorMapsAndRetryOnce` | 期待`街道清除：已清除`，实际为既有建筑清敌结果文案 |

没有为这些旧失败修改断言、跳过用例或改功能。原始结果：[editmode-targeted.xml](evidence/round6-2026-11-14/gallery-fix/editmode-targeted.xml)、[playmode-targeted.xml](evidence/round6-2026-11-14/gallery-fix/playmode-targeted.xml)。逐条结果：[targeted-fullname-comparison.csv](evidence/round6-2026-11-14/gallery-fix/targeted-fullname-comparison.csv)、[targeted-baseline-comparison.json](evidence/round6-2026-11-14/gallery-fix/targeted-baseline-comparison.json)。复算命令：`python docs/codex-reports/evidence/round6-2026-11-14/gallery-fix/compare-targeted.py`。

Unity命令使用隐藏窗口、`-batchmode -projectPath <repo> -runTests -testPlatform <EditMode|PlayMode> -testFilter <filter> -testResults <xml> -logFile <log>`；测试未加`-quit/-nographics`。EditMode filter为`Bdd20_ChineseDisplayNamesPreserveSceneAndMapIdentifiers`；PlayMode filter为`P3UiTask002_005_008_011PlayModeTests;Bdd35_AllThreeHudLabelsAreAvailableWithRealFalseCapabilityAndFourSpares;Bdd23_Trench_ProductionMagazineReload;Bdd23_Urban_ProductionMagazineReload;Bdd35_NoEnemyThrowUsesCurrentPlayerHeadingAndRealProjectile`。回放时临时设置`HUD_LAYOUT_AUDIT=1`，将测试截图全部重定向至本次`gallery-fix/replay`。运行日志保存在忽略目录`Logs/hud-layout-check-2026-11-14/`。

所有Unity启动前检查已有进程，始终串行，完成后无Unity实例。批处理改写的字体、XR设置/Prefab和临时测试源文件均恢复开工字节；旧证据图保持原字节，被跟踪的`Assets/InitTestScene*.unity`及meta完整保留。资源还原与哈希核对见 [cleanup.json](evidence/round6-2026-11-14/gallery-fix/cleanup.json)。没有执行git commit、push、切分支或stash。当前结论覆盖1920×1080桌面纯UI布局与已复现的截图工具路径，未把画廊图作为VR头显舒适度验收。
