# task012 武器库武器墙与展示挂点

## 负责人

场景

## 目标

在 `MainMenuScene` 的武器库区域搭建四武器展示墙，为 `w_191`、`w_951`、`w_qbs09`、`w_qjb201` 提供稳定展示位、模型/占位绑定和当前预览选中表现。武器墙只呈现武器服务状态，不直接装备武器，也不负责 P3 堑壕或城镇中的实际战斗武器生成。

## 参考资料

- `docs/BDD/screens/13-武器库武器墙.feature.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `UI/Sample/vr-shooting-armory-weapon-wall-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`

## BDD 场景追溯

| BDD 文件 | 精确场景 | 本任务责任 |
|---|---|---|
| `13-武器库武器墙.feature.md` | `默认显示当前已装备武器` | 四个展示位均存在；服务给出的当前装备/预览 ID 对应展示位显示选中或已装备标识。 |
| `13-武器库武器墙.feature.md` | `选择武器后刷新属性` | 预览选择变化时仅一个展示位切换高亮；属性面板和按钮由 UI 负责。 |
| `13-武器库武器墙.feature.md` | `装备选中的武器` | 装备成功事件可刷新“已装备”表现，但场景不调用 `Equip`，也不生成训练武器。 |
| `13-武器库武器墙.feature.md` | `返回上一界面` | 武器墙隐藏/显示不修改当前装备状态，未确认的预览不会由场景写入持久状态。 |
| `13-武器库武器墙.feature.md` | `不适用场景的武器提示` | 所有展示位仍可呈现；适用范围提示和装备禁用来自服务/UI，场景不硬编码限制。 |

## 交付内容

- 在 `SceneId=MainMenuScene` 中建立 `Armory_WeaponWall` 场景区域或可由该场景实例化的武器墙 Prefab。
- 提供四个稳定展示位，并以 `WeaponId` 绑定：

| WeaponId | BDD 显示名 | 展示位测试 ID |
|---|---|---|
| `w_191` | `19-1自动步枪` | `Armory.WeaponWall.Display.w_191` |
| `w_951` | `95-1自动步枪` | `Armory.WeaponWall.Display.w_951` |
| `w_qbs09` | `QBS-09霰弹枪` | `Armory.WeaponWall.Display.w_qbs09` |
| `w_qjb201` | `QJB-201班用机枪` | `Armory.WeaponWall.Display.w_qjb201` |

- 每个展示位至少包含稳定模型锚点、名称/交互锚点、预览高亮根节点和已装备标识根节点；正式模型未到位时允许使用清晰可区分且记录替换计划的展示占位，但绑定 ID 不得变化。
- 提供 `ArmoryWeaponWallBinding` 或等效 Presenter/场景适配：
  - 从 task003 的武器定义、当前预览和当前装备状态读取数据。
  - 任何时刻最多一个预览高亮；装备标识可与预览高亮分别表达。
  - 只更新材质参数、轮廓、灯光或标牌，不直接调用 `SelectPreview` / `Equip`，不持久化数据。
- 使用共享材质或 `MaterialPropertyBlock` 实现选中表现，避免每次选择创建材质实例。
- 武器墙与 UI 属性面板、返回按钮和 XR 射线保持清晰视线，不用大型碰撞体阻挡 UI 交互。

## 稳定测试 ID

| 用途 | 稳定测试 ID |
|---|---|
| 武器墙根节点 | `Armory.WeaponWall.Root` |
| 四个展示位 | `Armory.WeaponWall.Display.<WeaponId>` |
| 四个模型锚点 | `Armory.WeaponWall.ModelAnchor.<WeaponId>` |
| 四个预览高亮 | `Armory.WeaponWall.Selection.<WeaponId>` |
| 四个已装备标识 | `Armory.WeaponWall.Equipped.<WeaponId>` |
| 武器墙绑定 | `Armory.WeaponWall.Binding` |
| UI 武器墙占位握手 ID（由 task013 创建，场景不得复制） | `Placeholder_Armory_WeaponWall` |

## 不包含

- 不实现 `IWeaponService`、武器属性、适用模式判断、装备命令或本地持久化。
- 不实现 P3 堑壕/城镇训练中的实际武器生成、拾取、弹药初始化或战斗逻辑。
- 不将武器墙展示模型当作可直接带入战斗的运行时武器实例。
- 不要求 P2 完成全部武器的高精度动画、拆装或真实弹匣交互。

## 依赖关系

- 前置依赖：task003。
- 后续依赖：task018。

## 联调说明

- 与 功能A/武器服务联调：按 `WeaponDefinitionDto.WeaponId` 绑定四个展示位，区分当前预览和当前已装备状态。
- 与 UI 联调：选择、属性刷新、装备和返回命令由 UI Presenter 转发；场景仅呈现结果。
- 与 功能B 联调：确认展示模型锚点、比例和后续 Prefab 替换方式，但不接入 P3 战斗生成。

## 测试要求

- PlayMode 测试：
  - 加载 `MainMenuScene` 并进入武器库状态后，可通过测试 ID 找到武器墙根节点和四个唯一展示位。
  - 每个展示位的序列化 `WeaponId` 与 `w_191`、`w_951`、`w_qbs09`、`w_qjb201` 一一对应，无重复或空绑定。
  - 注入当前装备 `w_951` 时对应展示位显示已装备标识；注入预览 `w_qbs09` 时只有该展示位显示预览高亮。
  - 连续切换预览不会创建重复展示对象或材质实例，也不会调用测试替身的 `Equip`。
  - 隐藏武器库或返回上页后，测试替身中的当前已装备武器不变。
  - 场景/Prefab 中不存在由本任务新增的 P3 战斗武器生成器、敌人、弹药初始化或 AI 依赖。
  - XR UI 射线和无 VR 鼠标测试路径均不被武器墙碰撞体阻挡。
- 测试名称或说明必须引用上表中的 BDD 文件和精确场景名。

## VR 手工验收

- 四个展示位在正常站姿下均位于舒适观察范围，名称、轮廓和武器差异可辨认。
- 左右手 UI 射线选择武器时，高亮反馈明确且不会误选相邻展示位。
- 当前预览和当前已装备表现可区分，UI 属性面板仍清晰可读。
- 展示模型、墙体和碰撞体不会侵入玩家头部、遮挡返回/装备按钮或造成明显近裁剪。

## 验收标准

- 四个 BDD 武器均有稳定展示位、`WeaponId` 绑定和测试 ID。
- 当前预览/已装备表现完全由服务状态驱动，场景不修改装备数据。
- 武器墙同时支持 VR 射线和无 VR 自动化交互路径。
- PlayMode 测试通过；P3 实际战斗生成明确排除在本任务之外。
