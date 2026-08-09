# task017 P2 UI 参考图、测试 ID 与 VR 可读性收口

## 负责人

UI

## 目标

对 task008-task010、task013、task015 的全部 P2 页面进行统一视觉、命名、测试 ID、Busy/错误状态、无 VR 与 VR UI 交互收口，保证参考图主要结构一致、端到端测试可稳定定位控件，并在白天、夜晚和武器墙背景下保持舒适可读。

## 参考资料

- `UI/Sample/vr-shooting-main-menu-ui.png`
- `UI/Sample/vr-shooting-training-mode-selection-ui.png`
- `UI/Sample/vr-shooting-moving-target-mode-settings-ui.png`
- `UI/Sample/vr-shooting-moving-target-daytime-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-moving-target-night-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-moving-target-results-ui.png`
- `UI/Sample/vr-shooting-armory-weapon-wall-ui.png`
- `UI/Sample/vr-shooting-settings-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/01-通用视觉风格.feature.md`
- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶白天HUD.feature.md`
- `docs/BDD/screens/10-移动靶夜晚HUD.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`
- `docs/BDD/screens/13-武器库武器墙.feature.md`
- `docs/BDD/screens/22-设置界面.feature.md`
- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

| Feature | 精确场景名 | 本任务验收范围 |
|---|---|---|
| `01-通用视觉风格.feature.md` | `页面加载时显示统一的边框布局` | 所有 P2 页面边框、文本容器和素材占位语义一致 |
| `01-通用视觉风格.feature.md` | `左下状态面板展示玩家档案摘要` | 有状态面板的页面使用档案 DTO，最近移动靶评级可刷新 |
| `01-通用视觉风格.feature.md` | `右侧信息栏展示当前页面提示` | 信息分组、警示色和提示测试 ID 统一 |
| `01-通用视觉风格.feature.md` | `VR 视野中 UI 不遮挡核心内容` | HUD、操作按钮和关键文本处于舒适视野且不挡瞄准中心 |
| `02-游戏主界面.feature.md` | `主界面显示玩家档案和底部状态栏` | P2 入口加入后不破坏既有档案和状态栏布局 |
| `02-游戏主界面.feature.md` | `快速重复点击菜单按钮` | 全部 P2 切页按钮具有统一 Busy 状态 |
| `08-移动靶设置.feature.md` | `默认选择白天模式` | 默认选中、速度和预览控件可定位且可读 |
| `08-移动靶设置.feature.md` | `切换到夜晚模式` | 夜晚选中态、速度和微光提示视觉一致 |
| `09-移动靶白天HUD.feature.md` | `HUD 显示白天训练状态` | 白天 HUD 必填字段测试 ID 齐全且不遮挡目标 |
| `09-移动靶白天HUD.feature.md` | `目标到达左端后禁止射击` | 禁射警示的颜色、位置和测试 ID 可验证 |
| `10-移动靶夜晚HUD.feature.md` | `夜晚 HUD 使用微光镜显示` | 微光效果下 HUD 文字和瞄准区域可读 |
| `10-移动靶夜晚HUD.feature.md` | `端点停留期间禁止射击` | 夜晚禁射警示与白天语义一致 |
| `11-移动靶结算.feature.md` | `显示训练摘要` | 评级和摘要布局可扫描，字段均可定位 |
| `11-移动靶结算.feature.md` | `显示 5 次点射记录` | 五条记录及路线时间点均有稳定 ID |
| `13-武器库武器墙.feature.md` | `默认显示当前已装备武器` | 当前装备、预览和属性面板状态可辨识 |
| `13-武器库武器墙.feature.md` | `选择武器后刷新属性` | 四个目录项和属性字段 ID 完整，墙面素材不抢交互 |
| `22-设置界面.feature.md` | `显示当前设置` | 八个字段、三条滑条、预览和操作按钮 ID 完整 |
| `22-设置界面.feature.md` | `修改滑条设置` | 滑条焦点、拖动和数值反馈在桌面/XR 路径一致 |
| `22-设置界面.feature.md` | `应用设置` | 应用 Busy、成功和失败状态统一 |
| `22-设置界面.feature.md` | `恢复默认设置` | Pending/未应用视觉状态明确 |
| `22-设置界面.feature.md` | `返回不保存临时修改` | 返回按钮和丢弃结果的视觉/交互状态完整 |

## 交付内容

- P2 UI 视觉审查与调整：
  - 主菜单 P2 入口、模式选择、移动靶设置。
  - 白天/夜晚 HUD。
  - 移动靶结算。
  - 武器库。
  - 设置页面。
- 统一命名与测试 ID 清单：
  - 页面根节点全部使用 `Screen_<ScreenId>`。
  - 按钮、滑条、文本、面板、HUD 和素材占位符合接口文档命名。
  - 每个 BDD 可交互项、关键文本、提示、记录行均带唯一稳定 `UITestId`。
  - 输出 P2 UI 测试 ID 对照表或可由自动化扫描导出的清单。
- 通用档案摘要绑定：
  - 左下状态面板和主菜单只读取 task001/task007 冻结的 `PlayerProfileSummaryDto` 或等效查询结果。
  - 主菜单使用 `Panel_MainMenu_Profile`、`Text_MainMenu_ProfileId`、`Text_MainMenu_TrainingLevel`、`Text_MainMenu_RecentGrade`；其他页面若复用档案摘要，按 `Text_<ScreenId>_<Data>` 生成页面限定唯一 ID。
  - 移除 P2 可见页面中的档案标识、训练等级和最近评级硬编码；移动靶结算保存后刷新最近评级，但不扩展完整历史系统。
- 统一交互状态：
  - Hover、Pressed、Selected、Disabled、Busy、Success、Warning、Error。
  - 过渡期间按钮不可重复提交；失败时页面保留并提供可定位错误。
- 无 VR/VR Canvas 收口：
  - 复用 P1 的桌面屏幕空间和 OpenXR World Space Canvas 切换。
  - 复用唯一 `EventSystem + XRUIInputModule` 和支持桌面/XR 的射线模块。
  - World Space Canvas 默认位于头显前方 1.2m 至 1.5m、中心略低于水平视线、下沿不低于地面 0.10m。
  - 画布稳定窗口、唯一活动玩家相机/AudioListener 继续遵循 P1 基线；若 task011 采用 RenderTexture 微光镜，允许零或一个带固定测试 ID 的辅助瞄具 Camera，但它不得成为玩家视角或携带 `AudioListener`。
- 可读性与证据：
  - 检查夜晚微光背景、白天靶场、武器墙和设置预览下的对比度。
  - 检查文本溢出、遮挡、射线命中层级和瞄准中心净空。
  - 输出 P2 UI 无 VR 截图/验收说明以及 VR 实机验收记录模板。

## 不包含

- 不新增 P2 玩法、DTO、错误码或服务方法，不借收口任务改变既有业务接口。
- 不修正状态机、评级、弹药、瞄具、武器适用性或设置持久化逻辑。
- 不新增堑壕、城镇、队友指令等 P3/Later 页面。
- 不制作最终美术资产、完整本地化或高级夜视效果。
- 不把参考图示例数字写入运行时 UI。

## 依赖关系

- 前置依赖：task008-task010、task013、task015、task016。
- 联调依赖：task011、task012。
- 后续依赖：task018、P2 总体验收。

## 联调说明

- 与 task016 联调：端到端测试使用的页面状态和测试 ID 在收口后保持稳定。
- 与 task011 联调：夜晚微光和白天环境下 HUD 对比度、画布层级和相机配置。
- 与 task012 联调：武器墙展示、预览镜头与 Armory UI 射线命中层级。
- 与功能A/功能B联调：逐字段确认 DTO 映射完整，UI 不含任何规则补算。

## 测试要求

### 无 VR PlayMode 自动化

- 扫描全部 P2 页面，断言规格列出的测试 ID 存在、唯一、挂载到正确组件且不随显示文本变化。
- 通过测试 ID 完成：移动靶设置、白天/夜晚 HUD 状态刷新、结算、武器选择/装备、设置修改/应用/返回。
- Busy 状态下连续点击所有切页/提交按钮，命令计数均不增加。
- 服务 DTO/错误结果变化后，关键文本、Selected/Disabled/Warning/Error 状态正确刷新。
- 无 VR 模式使用屏幕空间 Canvas 和正式 EventSystem；XR Device Simulator/输入替身使用 World Space Canvas 和同一 Presenter。
- 模式切换后只有一个活动玩家 Camera、一个 `AudioListener` 和一个活动 UI 输入模块；可选微光辅助 Camera 数量不超过 1，具有稳定 ID 且不带监听器。
- 注入不同 `PlayerProfileSummaryDto` 后，档案标识、训练等级和最近评级随 DTO 刷新；UI 不保留静态示例值。
- HMD 姿态稳定后，World Space Canvas 距离、可读视角和地面安全边界符合接口文档；修正 UI 不改写 HMD/XR Origin。
- 白天/夜晚 HUD 中央瞄准净空、文本边界、五次记录、四个武器条目和八个设置字段可由自动化定位。

### VR 实机手工验收

- 从主菜单依次进入移动靶、武器库和设置，左右手柄射线均可悬停、确认、返回和拖动滑条。
- 白天觇孔和夜晚微光镜下 HUD 核心信息可读，不遮挡目标与瞄准线，不引发明显视觉不适。
- 武器墙和 RenderTexture 不抢占按钮射线；设置滑条拖动时不会误触相邻控件。
- 结算页五次点射记录无需过度转头或贴近画布即可阅读。
- 记录 HMD/控制器、OpenXR Runtime、连接方式、刷新率、HUD 可读性、交互命中和不适情况。

## 验收标准

- P2 页面主要结构和风格与参考图/draw.io 一致，且与 P1 UI 保持同一视觉语言。
- 全部 BDD 关键控件和文本具有稳定、唯一、可自动化定位的测试 ID。
- 无 VR PlayMode、XR Device Simulator/输入替身门禁通过，真实 VR 手工项完成记录。
- UI 收口未引入业务规则、第二套路由/输入或 P3/Later 隐性前置。
