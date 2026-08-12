# task003：P1/P2 场景壳与卧姿射击点重构

## 负责人

场景。

## 目标

把 P1 与 P2 场景重构为同一套“固定卧姿训练场景壳”：进入场景即位于射击点，禁用人工移动，保留头手真实追踪；大型 UI、最小 HUD、枪、瞄准方向和目标区域都通过独立稳定 Anchor 暴露。场景壳必须在没有真实 UI 和玩法服务时单独打开、校验和演示。

## 前置条件

`TrainingRangeSceneBindings`、稳定标识和场景测试输入已经冻结。不得依赖 task001、task002 或任何真实 UI/玩法实现。

## 必读资料

- `docs/BDD/screens/00-P1P2卧姿固定射击交互.feature.md`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/BDD/screens/08-移动靶设置.feature.md`
- `docs/BDD/screens/09-移动靶HUD.feature.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`
- `docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md`

## 实现内容

### 1. 共用场景壳

- 建立可复用的固定卧姿训练场景壳 Prefab 或场景组件，只包含 XR Rig 入口、稳定 Anchor、环境挂点和校验组件。
- `TrainingRangeSceneBindings` 至少暴露：`PlayerRootAnchor`、`ProneHeadReference`、`AimForwardAnchor`、`LargeUiAnchor`、`MinimalHudAnchor`、`WeaponRackAnchor`、`TargetRootAnchor`。
- 每个引用都由 Inspector 显式绑定；缺失或重复必须在编辑器校验和 PlayMode 启动时给出明确错误。
- Anchor 代表合同语义，不能依赖 UI Prefab 或枪 Prefab 的具体子节点。

### 2. 固定卧姿与 XR Rig

- 场景加载时把 XR Origin 放到唯一固定射击点；不提供可选出生点和行走区。
- 移除或禁用 Continuous Move、Teleportation、Snap/Smooth Turn、脚步声及相关输入绑定。
- 不锁定 HMD/Controller 的真实空间追踪，不通过每帧强制写头手 Transform 模拟卧姿。
- 提供无 VR 测试替身，验证 Origin 不受摇杆、传送或人工转向输入影响。

### 3. 独立正前方锚点

- `AimForwardAnchor` 作为卧姿初始朝向参考，供其他 Anchor 的编辑期摆位与校验使用。
- `LargeUiAnchor`、`MinimalHudAnchor`、`WeaponRackAnchor` 是三个独立节点；移动、隐藏或替换其中一个不应改变另外两个。
- 大型 UI 和枪在玩家卧姿正前方的舒适范围内，但互不重叠、均不占据中心瞄准保留区。
- 场景只提供位置/朝向，不实例化页面、不处理 UI 显隐、不判断枪是否拿起。

### 4. P1 场景迁移

- 将 `ZeroingRangeScene` 接入新场景壳，删除站姿眼高、行走范围和脚步相关假设。
- 保留 P1 已完成的靶场几何、命中表面和性能成果；只改与新合同冲突的结构。
- 旧脚本若同时承担场景绑定和业务逻辑，拆出纯场景适配部分；玩法规则留给玩法线迁移。

### 5. 场景隔离演示

- 用 Fake Presentation/Target Visual Driver 在无 UI、无 Session、无武器服务时显示锚点 Gizmo、开火许可指示和目标占位运动。
- 演示工具仅用于 Editor/Development，不得成为运行时业务真相源。

## 交付物

- 共用固定卧姿场景壳及 `.meta`。
- 重构后的 `ZeroingRangeScene` 和 P2 场景壳入口。
- `TrainingRangeSceneBindings`、编辑器校验器、Fake 视觉驱动。
- Anchor 命名/用途/局部坐标记录和 P1 迁移说明。
- 无真实 UI/玩法的 EditMode/PlayMode 场景测试。

## 测试要求

- 场景可单独加载，所有必需绑定非空且唯一。
- 模拟连续移动、传送、人工转向输入时 Player Root 不发生人工位移/旋转。
- 模拟 HMD/手柄本地追踪时头手姿态仍能变化。
- 三个 UI/枪 Anchor 独立；修改或禁用一个不影响其他绑定。
- Fake 驱动能在不引用真实玩法的情况下更新目标占位表现。
- `ZeroingRangeScene` 的靶面、距离语义和已有关键测试 ID 不被无关破坏。

## 验收标准

- P1/P2 场景均不提供走路、传送和人工转向功能，玩家从固定卧姿射击点开始。
- UI、最小 HUD、枪的摆位通过独立 Anchor 表达，不互相成为父子实现依赖。
- 场景脚本不包含训练阶段、取枪显隐、倒计时、弹药、评级或结算规则。
- 未加载真实 UI/玩法时仍可完成场景校验和 PlayMode 测试。

## 联调契约（非实现依赖）

- UI 通过组合层获得 Anchor，不要求场景引用 UI Prefab。
- 玩法输出移动策略和视觉状态，不要求场景引用玩法具体实现。
- 真实适配器只在阶段审核节点 D 的组合根中接到绑定对象；本任务不做三线硬连接。
