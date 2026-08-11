# 11 Unity 场景与 Prefab 约定

## 目标

约束 Unity 场景与 Prefab 开发时的场景、Canvas、UI 对象名和测试 ID，保证接口文档、BDD 和实现可互相追踪。

## 场景建议

| SceneId | 场景用途 | 关联页面 |
|---|---|---|
| MainMenuScene | 主菜单、模式选择 | 02、03 |
| ZeroingRangeScene | 100m 射校任务说明、HUD、分析、结算 | 04-07 |
| MovingTargetRangeScene | 移动靶设置、HUD、结算 | 08、09、11 |
| TrenchScene | 堑壕地图、简报、HUD、结算 | 12、14-17 |
| UrbanScene | 城镇地图、街道、建筑、结算 | 18-21 |

如开发团队决定合并场景，必须保留 `SceneId` 概念供路由和测试使用。

## UI Prefab 命名

| 类型 | 命名格式 | 示例 |
|---|---|---|
| 页面根节点 | `Screen_<ScreenId>` | `Screen_MainMenu` |
| 面板 | `Panel_<ScreenId>_<Name>` | `Panel_ZeroingImpactAnalysis_Data` |
| 按钮 | `Button_<ScreenId>_<Action>` | `Button_MainMenu_OpenZeroing` |
| 文本 | `Text_<ScreenId>_<Data>` | `Text_ZeroingHud_Round` |
| HUD 元素 | `Hud_<HudType>_<Name>` | `Hud_Trench_Ammo` |
| 素材占位 | `Placeholder_<ScreenId>_<Asset>` | `Placeholder_MovingTargetSettings_Preview` |

## 武器 Prefab 与场景绑定

P1 训练武器必须在第一人称视角中可见，并提供稳定绑定点供 XR、无 VR 调试输入和 PlayMode Test 使用。

| 类型 | 命名格式 | 示例 |
|---|---|---|
| 武器 Prefab | `Weapon_<WeaponId>_<Name>` | `Weapon_training-rifle_Blockout` |
| 武器根节点 | `WeaponRoot_<WeaponId>` | `WeaponRoot_training-rifle` |
| 枪架/武器台插槽 | `Socket_<WeaponId>_Rack` | `Socket_training-rifle_Rack` |
| 后手握把 | `Grip_<WeaponId>_RearHand` | `Grip_training-rifle_RearHand` |
| 前手握把 | `Grip_<WeaponId>_FrontHand` | `Grip_training-rifle_FrontHand` |
| 后手近距抓取区 | `GrabZone_<WeaponId>_RearHand` | `GrabZone_training-rifle_RearHand` |
| 前手近距抓取区 | `GrabZone_<WeaponId>_FrontHand` | `GrabZone_training-rifle_FrontHand` |
| 后手附着姿态 | `Attach_<WeaponId>_RearHand` | `Attach_training-rifle_RearHand` |
| 前手附着姿态 | `Attach_<WeaponId>_FrontHand` | `Attach_training-rifle_FrontHand` |
| 近距拾取提示 | `Prompt_<WeaponId>_Pickup` | `Prompt_training-rifle_Pickup` |
| 后坐力表现根节点 | `RecoilRoot_<WeaponId>` | `RecoilRoot_training-rifle` |
| 枪托/肩托 | `Stock_<WeaponId>` | `Stock_training-rifle` |
| 枪口 | `Muzzle_<WeaponId>` | `Muzzle_training-rifle` |
| 瞄准线参考 | `AimLine_<WeaponId>` | `AimLine_training-rifle` |
| 弹匣位置 | `Magazine_<WeaponId>` | `Magazine_training-rifle` |
| 左肩参考点 | `Shoulder_<WeaponId>_Left` | `Shoulder_training-rifle_Left` |
| 右肩参考点 | `Shoulder_<WeaponId>_Right` | `Shoulder_training-rifle_Right` |
| 弹道/曳光实例 | `Tracer_<WeaponId>_<ShotIndex>` | `Tracer_training-rifle_001` |
| 弹体视觉 | `ProjectileVisual_<WeaponId>_<ShotIndex>` | `ProjectileVisual_training-rifle_001` |
| 枪口焰 | `MuzzleFlash_<WeaponId>` | `MuzzleFlash_training-rifle` |
| 武器反馈音源 | `Audio_<WeaponId>_Feedback` | `Audio_training-rifle_Feedback` |
| 左右虚拟手视觉 | `VirtualHand_<Hand>` | `VirtualHand_Right` |
| 玩家脚步音源 | `Audio_Player_Footsteps` | `Audio_Player_Footsteps` |
| 弹着反馈 | `Impact_<TargetId>_<ShotIndex>` | `Impact_ZeroingTarget_001` |

关键武器对象必须带稳定测试 ID：

- `ZeroingRange.Weapon.PlayerRoot`
- `ZeroingRange.Weapon.TrainingRifle`
- `ZeroingRange.Weapon.RackSocket`
- `ZeroingRange.Weapon.Grip.RearHand`
- `ZeroingRange.Weapon.Grip.FrontHand`
- `ZeroingRange.Weapon.GrabZone.RearHand`
- `ZeroingRange.Weapon.GrabZone.FrontHand`
- `ZeroingRange.Weapon.Attach.RearHand`
- `ZeroingRange.Weapon.Attach.FrontHand`
- `ZeroingRange.Weapon.Prompt.Pickup`
- `ZeroingRange.Weapon.RecoilRoot`
- `ZeroingRange.Weapon.Muzzle`
- `ZeroingRange.Weapon.AimLine`
- `ZeroingRange.Weapon.Shoulder.Left`
- `ZeroingRange.Weapon.Shoulder.Right`
- `ZeroingRange.Weapon.DebugInput`
- `ZeroingRange.Weapon.TracerRoot`
- `ZeroingRange.Weapon.Feedback`
- `ZeroingRange.Weapon.MuzzleFlash`
- `ZeroingRange.Player.Footsteps`
- `ZeroingRange.Origin.VR.HeadPose`
- `ZeroingRange.Origin.VR.Hand.Right`
- `ZeroingRange.Origin.VR.Hand.Left`
- `ZeroingRange.Origin.VR.HandVisual.Right`
- `ZeroingRange.Origin.VR.HandVisual.Left`
- `ZeroingRange.Origin.VR.Interactor.Direct.Right`
- `ZeroingRange.Origin.VR.Interactor.Direct.Left`

武器 Prefab 必须提供一个绑定脚本或等效组件，序列化引用 Rigidbody、物理 Collider、枪口、瞄准线、前后手握把、前后手近距抓取区、前后手附着姿态、后坐力表现根节点、肩侧参考点和弹匣位置。PlayMode Test 不应通过对象名猜测这些引用，而应读取绑定组件验证完整性。

P1 训练步枪必须使用 `XRGrabInteractable`、可支持主/副手选择的组合 Interactable，或等效的可测试实现：

- 后手抓取区只接受右手 Direct Interactor，前手抓取区只接受左手 Direct Interactor；不得用远距 Ray Interactor 隔空吸枪。
- 后手是主选择，负责武器持有生命周期；前手只能在后手已选择时接入。
- 武器在架时由枪架/武器台插槽稳定放置；被抓取后由双手枪体解算接管；后手释放后恢复 Rigidbody、重力和场景碰撞。
- 后坐力只作用于 `RecoilRoot_*` 或等效局部表现层，不直接移动 HMD、XR Origin、Interactor 或跟踪姿态源。
- 虚拟手网格可以为握把姿势对齐，但控制器和 HMD Transform 始终是输入权威，不能反向被枪械脚本改写。
- 虚拟手必须使用手部网格或清晰的控制器手替身，不得以立方体作为最终视觉；握持时只在 `VirtualHand_*` 视觉子树内切换手指姿势和握把对齐。
- P1 右手握持姿态必须让手腕纵轴近似竖直并与后握把方向一致，虎口贴合握把后缘，中指、无名指和小指自然包住握把，食指转向扳机并保持相对伸直，拇指从另一侧对向支撑；左手必须掌心朝上，以拇指和食指之间的开放虎口及掌根共同托住前护木，拇指与四指分列护木两侧，四指仅小幅向上弯曲，不得越过顶部导轨或卷成闭合拳圈。姿态偏移只属于 `VirtualHand_*` 视觉配置，除手指环绕握把的预期接触外不得与枪体明显穿模。
- `VRControllerHandVisual` 必须提供不依赖 Play Mode、XR 设备或抓取输入的 Edit Mode 握姿预览入口。开始预览后必须自动在 Scene 视图中聚焦枪与预览手，并提供重新聚焦入口；由于 Edit Mode 下 XR 控制器层级可以默认禁用，预览必须使用脱离该层级且不保存到场景的临时手部副本，不得改变原场景虚拟手根节点和手指骨骼。结束预览、进入 Play Mode、脚本重载或保存场景前必须销毁临时副本，不得把预览位置误存为控制器跟踪姿态。
- 枪口焰、移动弹体/曳光、命中 VFX 与空间音效必须由有效射击结果驱动，不得在视觉组件中重新决定弹药消耗或命中。

## 测试 ID

每个可交互 UI 和关键文本必须提供稳定测试 ID：

```csharp
public sealed class UITestId : MonoBehaviour
{
    public string Id;
}
```

测试 ID 格式与对象名一致，但不得随美术命名调整而变化。

## 必备 UI 组件

| UI 类型 | 必备组件 |
|---|---|
| Button | Button 或 XR UI 可交互组件、UITestId、可禁用状态 |
| Text | TMP_Text、UITestId |
| Panel | RectTransform、Image 或 CanvasGroup、UITestId |
| HUD | CanvasGroup、Presenter 绑定脚本、UITestId |
| Radial Menu | 输入保持检测、选项高亮、取消逻辑 |

## 玩法服务挂载建议

| 服务 | 生命周期 |
|---|---|
| UIRouter | 全局单例或场景持久对象 |
| TrainingSessionService | 全局单例，切场景不丢失当前 Session |
| HUDService | 每个训练场景创建，订阅当前 Session |
| WeaponService | 全局单例，保存当前训练主武器状态 |
| SquadCommandService | 战斗场景创建，随 Session 销毁 |

## 输入适配

UI 不直接读取具体手柄按键，统一走输入适配：

```csharp
public interface IXRTrainingInput
{
    bool ConfirmPressed { get; }
    bool BackPressed { get; }
    bool RightGripPressed { get; }
    bool RightGripHeld { get; }
    bool RightGripReleased { get; }
    bool LeftGripPressed { get; }
    bool LeftGripHeld { get; }
    bool LeftGripReleased { get; }
    float RightTriggerValue { get; }
    bool TriggerPressed { get; }
    bool ReloadPressed { get; }
    bool SwitchShoulderPressed { get; }
    bool AimPressed { get; }
    bool AimHeld { get; }
    bool CommandMenuHeld { get; }
    Vector2 TurnAxis { get; }
    Vector2 MoveAxis { get; }
}
```

第一人称武器姿态由场景层通过可替换姿态来源提供，不由玩法服务直接读取硬件设备：

```csharp
public interface IWeaponPoseInput
{
    bool HeadTracked { get; }
    bool RearHandTracked { get; }
    bool FrontHandTracked { get; }
    Pose HeadPose { get; }
    Pose RearHandPose { get; }
    Pose FrontHandPose { get; }
}
```

`IXRTrainingInput` 负责按钮、模拟扳机和轴命令，`IWeaponPoseInput` 负责头、后手、前手姿态与跟踪有效性。真实 XR、XR Device Simulator 和无 VR 调试替身都应能适配到这两个抽象入口。

P1 默认 Input Action 映射如下，允许按设备 Profile 覆盖绑定，但不得改变语义：

| 语义 | XRI Action | 默认控制器 |
|---|---|---|
| 后握把拾取/保持 | `XRI RightHand Interaction/Select` | 右手 Grip |
| 前握把选择/保持 | `XRI LeftHand Interaction/Select` | 左手 Grip |
| 单发击发 | `XRI RightHand Interaction/Activate` | 右手 Trigger |

Grip 必须暴露按下、保持、释放三种状态；Trigger 必须暴露 `0-1` 模拟量，并由适配层以可配置迟滞阈值生成一次性 `TriggerPressed`。场景脚本不得同时读取该 Action 和硬件按钮，避免一发触发两次。

输入适配是 P1 基础能力：

- 模拟输入、XR Device Simulator、键鼠调试输入和真实 XR 输入都必须适配到同一接口。
- 服务层只消费抽象输入事件或命令，不直接读取具体手柄按键、键盘按键或设备 API。
- PlayMode Test 必须能注入测试输入，覆盖确认、返回、左右手 Grip 按下/保持/释放、右手模拟扳机、换弹、左右肩切换和无 VR 瞄准模式。
- 真实 VR 设备到位前，无 VR 输入替身路径必须可完成 P1 100m 射校闭环。
- 无 VR 调试输入必须能在 Editor Play Mode 中模拟头部视角、后手姿态、前手姿态和枪线变化。
- 无 VR 瞄准模式下，视觉相机或 ADS 代理可对齐 `AimLine_*`；真实 VR 不使用代理相机或 FOV 缩放，而由玩家自然对齐 HMD 与机械瞄具。两条路径的有效射击方向、可见弹道和命中计算都必须使用同一枪线。

## XR 与无 VR 运行时视角切换

- OpenXR Loader 启动且 HMD 可用时：启用 `XR Origin`、HMD Camera、XR Controller、左右虚拟手和 Direct Interactor；禁用 `Camera_NoVR`、其 `AudioListener` 和键鼠视角驱动。
- XR 不可用时：禁用 XR Camera/AudioListener 和设备交互输出，启用 `Camera_NoVR`、无 VR 姿态及输入替身。
- 任一时刻最多一个活动玩家 Camera 和一个活动 `AudioListener`。不得依赖人工切换 Hierarchy 作为正常启动步骤。
- P1 站立训练场景的 `XR Origin` 必须显式请求 `Floor` Tracking Origin，`Camera Y Offset` 与 `Camera Floor Offset Object` 的编辑态初始 Y 均为 `0m`；真实眼高只使用 OpenXR Runtime 或 XR Device Simulator 相对于地面的跟踪姿态，禁止再次叠加固定站立眼高。
- 运行时不得直接改写 HMD Camera 或 XR Origin 的跟踪姿态来修正玩家高度；若 Runtime 无法提供 Floor 模式，应由独立兼容层降级处理，不得同时使用真实 HMD 高度和固定 Y Offset。
- VR World Space Canvas 默认位于 HMD 水平方向前方 `1.2m` 至 `1.5m`、中心略低于水平视线，参考分辨率画布的水平可读视角应不小于 `65°`；首次启用后应在短暂的跟踪稳定窗口内根据最新 HMD 姿态重新摆放，避免使用启动首帧的无效低位姿态。
- 场景地面 `Y=0m` 是 World Space Canvas 的下沿安全约束而不是默认对齐目标；画布下沿不得低于地面 `0.10m`，低头、蹲下或暂时返回低位姿态时只允许调整 UI，不反向移动 HMD 或 XR Origin。
- 靶场初始步枪应位于玩家右前方 `0.5m` 至 `0.8m` 的自然伸手范围，后握把处于地面上方约 `1.0m` 至 `1.2m`；枪械不得依赖相机高度偏移来取得舒适位置。
- 真实 VR 中禁止武器、ADS、切肩或后坐力逻辑写入 HMD Camera、XR Origin 或 XR 投影/FOV；HMD 姿态只来自跟踪系统。
- 枪械和虚拟手在 `OnBeforeRender`、XRI Dynamic/Late 更新或等效低延迟阶段消费最新控制器姿态，额外显示延迟不超过一个显示帧。

## 验收约束

- 所有 BDD 中出现的按钮必须存在对应 `Button_*` 测试 ID。
- 所有 HUD 必填字段必须存在对应 `Text_*` 或 `Hud_*` 测试 ID。
- PlayMode Test 应能通过测试 ID 找到控件并模拟点击。
- 场景加载期间路由应进入 Busy 状态，避免重复点击。
- PlayMode Test 必须通过绑定组件验证枪架插槽、前后抓取区、前后附着姿态、Rigidbody、碰撞体和后坐力表现根节点完整。
- 真实 VR 实机必须验证右手近距 Grip 拾取、左手前握把选择、右手 Trigger 单发、放手掉落和再次拾取；范围外不得隔空抓取。
- VR 模式必须验证唯一活动相机/AudioListener、HMD 姿态未被武器逻辑改写、自然机瞄无 FOV 跳变以及枪体/虚拟手没有明显跟踪延迟。

