# 当前 UI 美术完善（重点：堑壕作战）

日期：2026-09-20。范围为现有 P1/P2/P3 页面表现，不扩展地图 B、夜间模式、武器库、设置或玩法规则。未更改 DTO、任务胜负、敌人位置获取方式或训练命令。

## 已实现

| 范围 / BDD | 本次完善 |
| --- | --- |
| 主菜单 01/02 | imagegen 生成的无文字训练大厅背景；正式科目入口排版；移除示例等级/评级、占位状态文字和已撤销的武器库/设置入口。 |
| P1 04–07 / UI task015 | 简报上标题下操作布局；圆形靶环按 50cm 靶面、10cm 十环比例绘制；三轮缩略图直接使用结果 DTO 的弹着坐标；修正实射提示对比度。 |
| P2 08/09/11 / UI task005 | 往返路径示意、速度选中样式、统一 HUD/结算；逐发记录可滚动，不依赖固定行数。 |
| P3 12/14 / UI task002 | 地图卡片加载现有关卡底图，选中框不遮住预览，未开放地图仍禁用；任务说明重新排版；侦察无人机徽标及短入场动效。 |
| P3 15/17 / UI task005 | 战壕 HUD 生命条、搜索条均由 DTO 驱动；小地图恢复原图亮度，用矢量符号区分我方、队友、敌情预估和击杀；保持中央瞄准区域透明；结算页胜负配色和数据排版。 |
| P3 18–21 / UI task008/011 | 城镇卡片、街道/楼内 HUD 与三层地图结算统一样式、补右侧底板；同用状态条与地图符号。 |
| 共用 | Resources 主题引用与带 9px 留白的动态中文 SDF 字体，保留源字体供构建加载；原生 UGUI 九宫格及按钮高亮/按下/禁用状态；装饰不拦截射线，地图卡片仍可点击。 |

## 验证

本轮继续细化：战壕标题降低视觉权重，小队状态添加底部衬板且不进入中央瞄准区；生命低于等于 25 / 失去战斗能力显示警示色，空弹匣显示警示色，换弹显示琥珀色，恢复正常 DTO 后恢复正常文字色。失能长文字自适应字号。移动靶速度按钮的选中、悬停、按下样式使用同一配色组，避免选中时悬停跳回青色。以上均为显示规则，不改变任何玩法命令或状态。

- Unity C# 编译检查：无编译错误。
- EditMode：本轮命令行复跑 **268/268 通过、0 跳过**，结果 `Temp/ui-refinement-edit.xml`，日志 `Temp/ui-refinement-edit.log`。
- 独立 UI PlayMode：本轮 Unity 2022.3.62f3c1 命令行复跑 **51/51 通过**，结果 `Temp/ui-refinement-play.xml`，日志 `Temp/ui-refinement-play.log`。覆盖 `VRShooting.Tests.PlayMode.UI`（排除依赖真实场景的 `Screen02_05_SceneOwnedUIFlowTests`）及 `TacticalUIArtTests`。新增颜色恢复、弹药/换弹配色、选中按钮指针状态、底板非交互性检查。
- 新增测试核对：构建可用主题/中文源字体、地图资源、非交互装饰、DTO 生命/搜索条、地图亮度和符号、HUD 中心透明、卡片射线与边框、靶图几何组件。
- 16 个页面的 1920×1080 原生 UGUI 独立预览保存在 `evidence/ui-art-completion/`，本轮已重新渲染并目视复核战壕简报/HUD/结算。`TacticalUIGallery.Capture()` 可复现；命令行使用 `-batchmode -quit -executeMethod VRShooting.Editor.TacticalUIGallery.CaptureBatch`。它使用临时 PreviewScene，关闭时释放对象，不保存/替换项目场景。P1/P2/P3 动态内容使用 **Editor 专用布局样例 DTO**，不是实机游玩或实际成绩证据；这些样例不进入玩家构建。
- 已目视检查主菜单、简报、HUD、地图卡、结果、三轮靶图和逐发记录；修正状态文字裁切、地图过暗、靶环不可见、正文溢出和字体边缘杂线。

## 尚需环境 / 实机验收

前轮编辑器内全量 PlayMode 曾遇到 `Burst failed to compile ... StabilizePosition$BurstManaged`。本轮重新启动 Unity，以独立命令行进程复跑全量 PlayMode，**176/176 通过、0 跳过**；结果 `Temp/ui-refinement-all-play.xml`，日志 `Temp/ui-refinement-all-play.log`。本次未复现先前错误，但不将新进程通过等同于已诊断其根因；没有修改包版本、玩法代码或测试断言来绕过。

后续仍需用 VR 设备检查：战壕中央视野不遮挡、生命/搜索/小队文字可读性、地图卡 XR 射线、近距观看与头动稳定性、P2 长记录滚动。当前不声称 VR 实机验收或阶段审核完成。

提交范围仅包含 UI 实现、对应主题资源、地图 Sprite 导入设置、UI 测试和本报告/预览。用户已有的 `Packages/`、`Assets/Plugins/`、ProBuilder 配置和导入日志保留且不纳入提交。Unity 重写的 OpenXR/VFX 序列化配置同样排除，未擅自覆盖回退。本轮不执行推送。

## 素材与维护

- 主题：`Assets/Resources/UI/Tactical/Theme.asset`。
- 背景：`Assets/Resources/UI/Tactical/hall-background.png`。本次使用 imagegen 技能生成纯场景素材，再以原生 UGUI 叠加真实文字/交互；没有把整页截图当 UI。
- 生成提示摘要：16:9 写实现代训练指挥大厅，左侧低矮青色全息地形台，石墨色混凝土与黑色金属，右侧暗色留白，无人物、文字、标志、水印或 UI 按钮。
- 复用原有 `UI/TrainingGenerated/Sprites` 九宫格/图标和 `SimulatedShooting/Art/Combat` 关卡底图；只调整后者 Sprite 导入设置，未改变地图内容。
- `Tools/VR Shooting/Rebuild Tactical UI Art` 重建资源绑定。布局在 `TacticalUILayout`，统一样式在 `TacticalUIStyle`；靶环、弹着和地图标记为可缩放矢量 UGUI 几何。
