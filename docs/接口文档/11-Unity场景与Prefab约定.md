# 11 Unity 场景与 Prefab 约定

2026-10-23 MainScene输入恢复：RestoreMenuInput检查InputActionAsset.devices；HasValue且Count为0或所有设备均已移除时，先恢复为真正的nullable null，再启用动作。保留包含有效设备的过滤器。测试恢复devices时显式区分nullable null与InputDevice[] null，避免隐式转换产生空过滤。Editor启用完整域/场景重载。服务接口不变，对应BDD02与UI task008。

2026-10-23 主菜单启动遮挡修复：以BDD34与接口26为准。手柄未跟踪或贴入头部时仅隐藏手臂网格，不禁用UI射线；恢复后自动显示。本次提供并运行PlayMode回归，覆盖旧修订中不运行测试的历史条款。

2026-10-22 回退修订：PR #28 的模式一、二改动已撤回至 `54073a0`，恢复100m距离及该基线的射校、UI、输入、场景与测试。仅保留模式三10血，以及模式三、四B键切肩、左扳机、手臂手指镜像和换弹（BDD29、接口21）；两份新契约不再覆盖P1/P2。

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

`MainScene` 的 VR 输入约定：左摇杆只接连续移动，右摇杆只接一种转向方式；右摇杆不得接移动，左摇杆不得接转向。真实 VR 移动由 XR Locomotion Provider 执行，无 VR 玩家移动替身不得再次移动 XR Origin。

VR 菜单通过 XRUIInputModule、TrackedDeviceGraphicRaycaster 和手柄 UI 射线交互。切入 VR 时须启用输入模块、射线组件及其 UI Press 动作；UI Press 同时支持数字扳机按钮和模拟 trigger 的 0.5 按压阈值。物体选择不得阻断菜单 UI；传送射线不得替代菜单射线。NearFarInteractor 必须启用远距离投射（enableFarCasting），否则 XRI 会清空 UI 输入模型；菜单射线组件及其父层级必须激活。业务射击与 UI 点击仍走各自输入适配入口。

2026-10-09 task019：训练枪近手抓取的 XRInputButtonReader 必须同时绑定数字 `gripPressed` 和模拟 `grip`。不得只绑定 Performed 而保留 Value 空动作；XRI 的交互强度读取使用 Value，空绑定会在设备切换或无驱动输入时访问无效状态。P1/P2 场景及场景构建器保持同一绑定。

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

P1/P2靶区后墙：共同显示 `Slogan_铁拳胜利`，子节点按 `Character_铁/拳/胜/利` 排列，四字采用 `ZeroingSloganIronFistVictory` 纹理的四等分UV与统一尺寸。历史测试ID `<Range>.Visual.Slogan.DataWins` 保留兼容，内容已改为铁拳胜利。墙面采用专用浅色混凝土材质，字体采用清楚的红色，文字平面在墙面正前方保持深度测试，不能用穿墙Shader掩盖遮挡。仅增量修复标语与专用背墙，不重建整场景。

MainScene XR主菜单：根节点以地面Y=0初始化、请求Floor跟踪；`CameraYOffset=1.65`仅作为运行时Device/Unbounded回退的Camera Offset，实际Floor偏移保持0，不改写HMD本地跟踪姿态。主菜单在激活及回场景时恢复InputActionManager动作、UI射线和UI Press；CurveVisualController未命中时延伸可见射线，避免默认0.25m收缩。真实扳机输入必须通过NearFarInteractor→XRUIInputModule→TrackedDeviceGraphicRaycaster完成点击。

2026-10-11 task020：共用训练步枪增加 `WeaponPrefabBinding.MagazineVisual`，绑定独立的 `Visual_training-rifle_Magazine` 网格，原 `MagazinePoint` 作为拔匣/装入锚点。`CombatReloadInstaller` 用分离的QBZ191身体/弹匣网格更新Prefab；保留原材质、握把、碰撞体和测试ID。P3运行时安装 `CombatRifleReloadPresentation`，从核心快照与时钟采样动作；P1/P2仅使用原位网格。桌面两只人物手使用现有Resources/Combat/Hands资源，VR左人物手临时跟随表现锚点，实际控制器/XRI/HMD不移动。

模式三task020修订：有效换弹时临时取消左手前握把选择并锁定该入口，人物左手脱离Left Controller父节点，由换弹锚点驱动。动作结束、取消或组件禁用时恢复原父节点与局部姿态，开放左手握枪；右手选择、实际手柄跟踪和UI射线不受锁定影响。模式四保持原握持逻辑。

P1/P2 训练武器必须在卧姿射击点正前方可见，并提供稳定绑定点供 XR、无 VR 调试输入和 PlayMode Test 使用。

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
    bool TriggerHeld { get; }
    bool TriggerReleased { get; }
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

Grip 和 Trigger 都必须暴露按下、保持、释放三种状态。Trigger 还必须暴露 `0-1` 模拟量，并由适配层以可配置迟滞阈值生成 `TriggerPressed/TriggerHeld/TriggerReleased`。P1 只消费按下边沿执行单发；P2 使用完整状态执行两发起射和长按连射。场景脚本不得同时读取该 Action 和硬件按钮，避免重复击发。

输入适配是 P1 基础能力：

- 模拟输入、XR Device Simulator、键鼠调试输入和真实 XR 输入都必须适配到同一接口。
- 服务层只消费抽象输入事件或命令，不直接读取具体手柄按键、键盘按键或设备 API。
- PlayMode Test 必须能注入测试输入，覆盖确认、返回、左右手 Grip 按下/保持/释放、右手 Trigger 按下/保持/释放、换弹、左右肩切换和无 VR 瞄准模式。
- 真实 VR 设备到位前，无 VR 输入替身路径必须可完成 P1 100m 射校闭环。
- 无 VR 调试输入必须能在 Editor Play Mode 中模拟头部视角、后手姿态、前手姿态和枪线变化。
- 无 VR 瞄准模式下，视觉相机或 ADS 代理可对齐 `AimLine_*`；真实 VR 不使用代理相机或 FOV 缩放，而由玩家自然对齐 HMD 与机械瞄具。两条路径的有效射击方向、可见弹道和命中计算都必须使用同一枪线。

## XR 与无 VR 运行时视角切换

- OpenXR Loader 启动且 HMD 可用时：启用 `XR Origin`、HMD Camera、XR Controller、左右虚拟手和 Direct Interactor；禁用 `Camera_NoVR`、其 `AudioListener` 和键鼠视角驱动。
- XR 不可用时：禁用 XR Camera/AudioListener 和设备交互输出，启用 `Camera_NoVR`、无 VR 姿态及输入替身。
- 任一时刻最多一个活动玩家 Camera 和一个活动 `AudioListener`。不得依赖人工切换 Hierarchy 作为正常启动步骤。
- P1/P2 卧姿训练场景的 `XR Origin` 必须显式请求 `Floor` Tracking Origin，`Camera Y Offset` 与 `Camera Floor Offset Object` 的编辑态初始 Y 均为 `0m`；真实头部高度只使用 OpenXR Runtime 或 XR Device Simulator 相对于地面的跟踪姿态，禁止叠加固定站立眼高或用脚本锁死 HMD。
- 运行时不得直接改写 HMD Camera 或 XR Origin 的跟踪姿态来修正玩家高度；若 Runtime 无法提供 Floor 模式，应由独立兼容层降级处理，不得同时使用真实 HMD 高度和固定 Y Offset。
- P1/P2 大型 World Space UI 由场景 `LargeUiAnchor` 放在卧姿视线正前方；具体距离和尺寸集中在射击位配置中，并以真实 VR 舒适验收为准。首次启用后只允许调整 UI 表现根，不得移动 HMD 或 XR Origin。
- 场景地面 `Y=0m` 是 UI/武器安全约束；大型 UI 和武器不得进入地面或要求玩家起身/行走。低头或房间尺度微动只改变观察关系，不移动固定玩家根。
- 靶场初始步枪应位于 `WeaponRackAnchor` 的卧姿自然伸手范围；枪械不得依赖相机高度偏移来取得舒适位置。
- P1/P2 禁用连续移动、传送、攀爬和人工平移；允许真实 HMD/双手跟踪。无 VR 调试相机允许观察/瞄准但不得用 WASD 改变固定射击点。
- 真实 VR 中禁止武器、ADS、切肩或后坐力逻辑写入 HMD Camera、XR Origin 或 XR 投影/FOV；HMD 姿态只来自跟踪系统。
- 枪械和虚拟手在 `OnBeforeRender`、XRI Dynamic/Late 更新或等效低延迟阶段消费最新控制器姿态，额外显示延迟不超过一个显示帧。

## 验收约束

- 所有 BDD 中出现的按钮必须存在对应 `Button_*` 测试 ID。
- 所有 HUD 必填字段必须存在对应 `Text_*` 或 `Hud_*` 测试 ID。
- PlayMode Test 应能通过测试 ID 找到控件并模拟点击。
- 场景加载期间路由应进入 Busy 状态，避免重复点击。
- PlayMode Test 必须通过绑定组件验证枪架插槽、前后抓取区、前后附着姿态、Rigidbody、碰撞体和后坐力表现根节点完整。
- 真实 VR 实机必须验证右手近距 Grip 拾取、左手前握把选择、P1 Trigger 单发、P2 快速扣动两发、P2 长按连射、放手掉落和再次拾取；范围外不得隔空抓取。
- VR 模式必须验证唯一活动相机/AudioListener、HMD 姿态未被武器逻辑改写、自然机瞄无 FOV 跳变以及枪体/虚拟手没有明显跟踪延迟。

## P1/P2 卧姿射击位绑定

P1/P2 场景必须提供 `TrainingRangeSceneBindings` 或等效组件，序列化引用 PlayerRoot、ProneHeadReference、AimForward、LargeUiAnchor、MinimalHudAnchor、WeaponRackAnchor 和 TargetRoot。完整字段、测试 ID、责任边界与错误处理见 `13-P1P2卧姿射击与界面显隐契约.md`。

- UI 负责人只制作可挂到锚点的 Prefab/Presenter，不直接引用场景文件。
- 场景负责人只提供锚点、碰撞体、相机/XR 根和视觉驱动器，不引用具体 Presenter 或业务服务。
- 玩法功能负责人只发布 DTO/事件，不引用 `TrainingRangeSceneBindings` 或 Transform。
- 组合与真实性验证由审核测试与优化负责人按 P2 阶段审核清单完成，不建立审核类 task。


## P3 v1 补充

P3 新端口、配置、错误/幂等、会话修订、场景ID与Fixture遵循 [14-P3战斗契约与独立测试基础](14-P3战斗契约与独立测试基础.md)。共用类型保持唯一，生产实现不引用替身。

## 2026-10-20 左右背景标语板

模式一/二中央“铁拳胜利”两侧增加白底红字标语：左“争头功 争任务 争第一”，右“老实 朴实 扎实”；两板左上角均为红色五角星和“八一”。同场景使用共享材质/贴图，采用有深度遮挡的世界空间网格，不增加交互或玩法逻辑，不改变射击位与路线。对象使用 `Slogan_Side_Left/Right`，稳定ID为场景前缀加 `.Visual.Slogan.Side.Left/Right`。手工验收：分别打开两场景，从射击位观察左右文字、标志和遮挡；检查无遮挡靶标、无镜像、无闪烁，头显可读性待实机确认。本次不新增或运行自动化测试，后续获授权后补入场景 PlayMode 覆盖。

## 2026-10-20 玩家戴手套手臂精简替换

按用户提供的 MODULAR FPS ARMS PACK1.0，清理误导入整包，仅选择一套带手套的军人手臂及必要依赖。保留左右 `VirtualHand` 资源入口和手柄跟随、握枪及换弹表现接口；只更换蒙皮与适配手指姿势，不改变输入/服务规则。原unitypackage保留在项目外。按两手独立骨架裁出前臂，贴图限1024，禁止导入其他款式、场景、身体或PSD。手工验收项目为开合手、双手握枪、换弹期间左手动画/禁握、结束恢复以及头显观感；本次不新增或运行自动化测试，后续获授权后补到既有手部表现/换弹PlayMode回归。

## 2026-10-20 用户审核通过的正前方握枪姿势

以 `docs/codex-reports/evidence/approved-forward-grip/approved-reference.png` 为视觉验收参考。右手掌贴住后握把，三指包握、食指位于扳机护圈附近；左手从前护木下方承托并自然包握，两只手腕与前臂自然衔接、手臂不交叉穿插。只修改表现层的握持位置、旋转与手指关节配置，不强制移动头显或手柄跟踪，不改变射击规则及换弹门禁。共享手Prefab和模式一/二内置实例同步，生成工具保留配置。手工验收含正前方及侧面渲染、握持接触、松手恢复、换弹恢复；头显内舒适度/精细贴合待实机。本次未新增或运行自动化测试，后续获授权后补入手部表现PlayMode回归。

### 2026-10-20 多角度实机截图复修

用户反馈上一轮姿势仍不通过：左掌离护木、右手未包握、前臂交叉。重新按掌骨行方向标定：右手指根沿握把上下排列，左手指根沿护木前后排列；逐指校正接触位置，使用实际枪网格的多角度渲染检查，而非只看背面单视角。上一轮交付不作为实机验收通过。共享Prefab、场景和生成工具保持同步；原射击/换弹门禁及硬件跟踪契约不变。

### 食指扣扳机修订

用户确认复修后的整体握持效果，追加要求右手食指弯入扳机护圈、指腹贴住扳机。保留本轮掌部/前臂/其他四指配置，仅调整右手食指三关节的表现姿势；这不是射击输入或击发判定变化，仍由原扳机输入适配触发。覆盖旧说明中“食指伸直放在护圈外侧”的视觉要求。

### 整体接触与穿模复核

用户指出右掌/指节仍与握把穿插，需整体消除明显穿模并保留食指扣扳机。采用实际枪网格定位握把、扳机和护木表面，调整掌部间距、各指包合路径和食指护圈位置；从已保存资源重新载入并做多角度接触检查。几何检查仅为编辑器中的蒙皮顶点/枪面深穿插筛查，不代替实机验收或射击/换弹流程测试。
