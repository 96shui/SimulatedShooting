# task013 武器库与 Loadout UI

## 负责人

UI

## 目标

实现 P2 武器库/武器墙界面，通过武器服务显示四种武器目录、当前装备、预览选择、属性、适用训练模式和装备结果。UI 必须区分“当前预览”和“当前已装备”，不在 View 内判断武器适用性或修改 Loadout。

## 参考资料

- `UI/Sample/vr-shooting-armory-weapon-wall-ui.png`
- `UI/Sample/vr-shooting-ui-reference-wireframes.drawio`
- `docs/BDD/screens/02-游戏主界面.feature.md`
- `docs/BDD/screens/13-武器库武器墙.feature.md`
- `docs/接口文档/00-UI与玩法服务层交互总约束.md`
- `docs/接口文档/01-页面导航与UI事件.md`
- `docs/接口文档/06-武器与弹药服务.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## BDD 场景追溯

| Feature | 精确场景名 | 本任务验收范围 |
|---|---|---|
| `02-游戏主界面.feature.md` | `打开武器库` | 从主菜单进入武器库并高亮当前装备 |
| `13-武器库武器墙.feature.md` | `默认显示当前已装备武器` | 加载装备状态并显示名称、类型、容量、后坐力、适用场景 |
| `13-武器库武器墙.feature.md` | `选择武器后刷新属性` | 四个示例武器均能预览和刷新属性，尚不改变装备 |
| `13-武器库武器墙.feature.md` | `装备选中的武器` | P2 只验收成功命令后更新 Loadout/标记并响应 `WeaponChangedEvent`；堑壕/城镇实际生成由 task001 拆为 P3 验收 |
| `13-武器库武器墙.feature.md` | `返回上一界面` | 未装备的预览不改变当前 Loadout，按路由历史返回 |
| `13-武器库武器墙.feature.md` | `不适用场景的武器提示` | 显示服务返回的适用限制并禁用/确认装备按钮 |

## 交付内容

- `Screen_Armory`
- 武器目录和选择：
  - `Button_Armory_Select_w_191`
  - `Button_Armory_Select_w_951`
  - `Button_Armory_Select_w_qbs09`
  - `Button_Armory_Select_w_qjb201`
  - 每把武器使用唯一 `Panel_Armory_Preview_<WeaponId>`、`Panel_Armory_Equipped_<WeaponId>`、`Panel_Armory_Unavailable_<WeaponId>` 状态标记；当前预览、当前装备和不可用状态不得只依赖材质颜色或按钮显示文本。
- 武器属性面板：
  - `Text_Armory_WeaponName`
  - `Text_Armory_WeaponType`
  - `Text_Armory_MagazineCapacity`
  - `Text_Armory_ReserveAmmo`
  - `Text_Armory_Recoil`
  - `Text_Armory_ApplicableModes`
  - `Text_Armory_Restriction`
- 操作与展示接口：
  - `Placeholder_Armory_WeaponWall`
  - `Button_Armory_Equip`
  - `Button_Armory_Back`
  - `Text_Armory_Error`
- Armory Presenter：
  - 调用 `GetWeapons()`、`GetEquippedWeapon()` 初始化目录与装备状态。
  - 选择卡片只调用 `SelectPreview()` 并渲染返回的 `WeaponDefinitionDto`。
  - 点击装备才调用 `Equip()`；只在成功或 `WeaponChangedEvent` 后更新已装备标记。
  - `InvalidInput`、`NotFound`、`Busy` 等结果按契约显示限制或错误，不在 UI 复制适用模式矩阵。

## 视觉要求

- 目录与场景武器墙保持对应关系，选择高亮不能依赖模型材质变化作为唯一提示。
- 属性文本采用一致单位和枚举本地化显示，未知枚举安全降级。
- 当前装备、当前预览、不可用三种状态必须视觉可区分。
- 武器墙 RenderTexture/模型/素材区域不作为按钮；交互命中由稳定卡片和装备按钮承担。

## 不包含

- 不生成堑壕/城镇中的实际武器，不验证 P3 战斗场景使用效果。
- 不实现武器弹道、后坐力、换弹、点射或具体枪型动画。
- 不在 UI 硬编码武器容量、后坐力或适用训练模式。
- 不实现真实弹匣插拔、拉机柄、保险、卡弹等 Later 枪械操作。
- 不负责武器墙三维模型、挂点和灯光，task012 负责场景交付。

## 依赖关系

- 前置依赖：task003、task012。
- 可并行：task014、task015。
- 后续依赖：task016、task017。

## 联调说明

- 与 task003 联调：武器目录、预览选择、装备命令、适用性错误和 `WeaponChangedEvent`。
- 与 task012 联调：四个展示位、预览镜头/RenderTexture、占位区域和武器 ID 一致。
- 与 task008 联调：从主菜单进入及 `ReturnToScreen` 返回行为。
- 与 task016 联调：装备状态在页面切换后仍一致，且不会影响 P1/移动靶指定训练武器规则。

## 测试要求

### 无 VR PlayMode 自动化

- 通过测试 ID 找到四个武器条目、属性字段、装备和返回按钮。
- 当前装备为 95-1 自动步枪时，打开页面立即高亮 `w_951` 并渲染服务 DTO 属性。
- 依次选择 19-1、95-1、QBS-09、QJB-201，验证预览高亮互斥、属性刷新且已装备状态不变。
- 点击装备后仅在成功结果到达时改变装备标记；重复点击 Busy 状态下不重复发送命令。
- 未点击装备直接返回时，重新打开武器库仍显示原装备。
- 服务返回不适用模式时显示限制提示，装备按钮按契约禁用或要求确认，不由 View 自行决定。
- 缺失武器定义或加载失败时不显示静态示例数据，并提供可定位错误。

### VR 实机手工验收

- 四个武器条目、属性和装备按钮可用手柄射线稳定选择，武器墙模型不遮挡 UI 射线。
- 属性文字、当前装备和不可用提示在头显中可读，切换预览不造成明显闪烁或眩晕。
- 返回后页面上下文正确，未装备的预览不会泄漏为实际 Loadout。

## 验收标准

- 四个 BDD 武器均由 `WeaponDefinitionDto` 驱动显示，预览和装备状态严格分离。
- UI 不包含适用性、弹药或武器玩法规则。
- 无 VR PlayMode 测试通过，VR 射线与武器墙可读性完成手工记录。
