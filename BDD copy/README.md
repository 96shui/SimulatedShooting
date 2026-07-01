# VR射击训练游戏 BDD 文档

本目录根据 `UI/Sample/vr-shooting-ui-reference-wireframes.drawio` 中的 22 个 UI 页面、`VR射击游戏需求文档_汇总版.md` 以及当前 Unity 项目结构整理。文档面向策划、Unity 开发、QA 和自动化测试团队，用行为示例描述系统应如何响应玩家操作。

## 使用方式

- 每个 `BDD/screens/*.feature.md` 文件对应 draw.io 的一个页面。
- 场景使用中文 BDD 写法：`假如 / 当 / 那么 / 而且 / 但是`。
- “玩家”默认指佩戴 VR 头显并使用左右手柄或手部射线交互的训练者。
- “选择”“点击”“确认”在 Unity 中默认通过 XR Ray Interactor 或等效 UI 输入模块触发。
- UI 文本、按钮名、状态名应尽量与 draw.io 页面保持一致，开发时如需改名，需要同步更新 BDD。

## 文档索引

| draw.io 页面 | BDD 文档 | 覆盖范围 |
|---|---|---|
| 01 通用视觉风格 | [01-通用视觉风格.feature.md](screens/01-通用视觉风格.feature.md) | UI 风格、HUD 容器、非交互文本框 |
| 02 游戏主界面 | [02-游戏主界面.feature.md](screens/02-游戏主界面.feature.md) | 主菜单入口、档案、状态栏 |
| 03 训练模式选择 | [03-训练模式选择.feature.md](screens/03-训练模式选择.feature.md) | 模式卡片选择、确认、返回 |
| 04 100m任务说明 | [04-100m任务说明.feature.md](screens/04-100m任务说明.feature.md) | 射校任务参数、开始射击、返回 |
| 05 100m射击HUD | [05-100m射击HUD.feature.md](screens/05-100m射击HUD.feature.md) | 据枪稳定、弹数、轮次、弹着记录 |
| 06 弹着分析 | [06-弹着分析.feature.md](screens/06-弹着分析.feature.md) | 偏差计算、应用调整、下一轮 |
| 07 射校最终评级 | [07-射校最终评级.feature.md](screens/07-射校最终评级.feature.md) | 最终评级、三轮记录、重训 |
| 08 移动靶设置 | [08-移动靶设置.feature.md](screens/08-移动靶设置.feature.md) | 昼夜模式、速度选择、开始训练 |
| 09 移动靶白天HUD | [09-移动靶白天HUD.feature.md](screens/09-移动靶白天HUD.feature.md) | 白天移动靶、点射、命中统计 |
| 10 移动靶夜晚HUD | [10-移动靶夜晚HUD.feature.md](screens/10-移动靶夜晚HUD.feature.md) | 夜晚微光镜、端点禁射、命中统计 |
| 11 移动靶结算 | [11-移动靶结算.feature.md](screens/11-移动靶结算.feature.md) | 命中评级、路线记录、重训 |
| 12 堑壕地图选择 | [12-堑壕地图选择.feature.md](screens/12-堑壕地图选择.feature.md) | 地图卡片、任务条件、确认 |
| 13 武器库武器墙 | [13-武器库武器墙.feature.md](screens/13-武器库武器墙.feature.md) | 武器选择、属性、装备 |
| 14 堑壕开场简报 | [14-堑壕开场简报.feature.md](screens/14-堑壕开场简报.feature.md) | 无人机简报、小队配置、进入堑壕 |
| 15 堑壕第一人称HUD | [15-堑壕第一人称HUD.feature.md](screens/15-堑壕第一人称HUD.feature.md) | 堑壕战斗 HUD、姿态、弹药、队友 |
| 16 队友指令环形菜单 | [16-队友指令环形菜单.feature.md](screens/16-队友指令环形菜单.feature.md) | 指令菜单、手榴弹、火力支援、掩护 |
| 17 堑壕任务结算 | [17-堑壕任务结算.feature.md](screens/17-堑壕任务结算.feature.md) | 胜负结果、搜索进度、重开 |
| 18 城镇地图选择 | [18-城镇地图选择.feature.md](screens/18-城镇地图选择.feature.md) | 城镇地图、任务信息、确认 |
| 19 城镇街道HUD | [19-城镇街道HUD.feature.md](screens/19-城镇街道HUD.feature.md) | 街道进入、建筑入口、HUD 状态 |
| 20 建筑搜索HUD | [20-建筑搜索HUD.feature.md](screens/20-建筑搜索HUD.feature.md) | 开门、观察、拐角伸枪、房间搜索 |
| 21 城镇任务结算 | [21-城镇任务结算.feature.md](screens/21-城镇任务结算.feature.md) | 城镇胜负、房间搜索、重开 |
| 22 设置界面 | [22-设置界面.feature.md](screens/22-设置界面.feature.md) | 舒适度、转向、移动、HUD、音量、应用 |

## 统一验收口径

- UI 应使用 Unity `Canvas` 或等效 UI 系统组织，所有可交互元素应有稳定的对象名或测试 ID。
- 文本建议使用 TextMeshPro，非交互显示内容应位于明确边框容器内。
- 交互按钮应响应悬停、按下、取消和确认状态，VR 射线焦点离开时不应误触。
- 场景切换、模式选择和设置应用应通过可测试的状态机或服务暴露当前状态。
- HUD 数据必须来自游戏状态，不应只写死在 UI 文本中。
- 每个结算页的数据必须来自本局训练记录，不能使用 UI 示例值作为最终逻辑。

## 自动化建议

- 关键流程使用 Unity PlayMode Test 覆盖：菜单导航、训练开始、HUD 状态、结算跳转。
- 纯 UI 组件使用 EditMode Test 覆盖：按钮绑定、文本刷新、设置持久化、布局对象存在。
- XR 输入可用测试替身模拟：射线命中 UI、按下确认、取消、扳机、换弹、姿态切换。
- 对涉及随机的敌人、弹着偏移、地图选择，应注入可控随机种子，保证 BDD 示例可重复执行。

