# 11 Unity 场景与 Prefab 约定

## 目标

约束 Unity 场景与 Prefab 开发时的场景、Canvas、UI 对象名和测试 ID，保证接口文档、BDD 和实现可互相追踪。

## 场景建议

| SceneId | 场景用途 | 关联页面 |
|---|---|---|
| MainMenuScene | 主菜单、模式选择、武器库、设置 | 02、03、13、22 |
| ZeroingRangeScene | 100m 射校任务说明、HUD、分析、结算 | 04-07 |
| MovingTargetRangeScene | 移动靶设置、HUD、结算 | 08-11 |
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
| 素材占位 | `Placeholder_<ScreenId>_<Asset>` | `Placeholder_Armory_WeaponWall` |

## 武器 Prefab 与场景绑定

P1 训练武器必须在第一人称视角中可见，并提供稳定绑定点供 XR、无 VR 调试输入和 PlayMode Test 使用。

| 类型 | 命名格式 | 示例 |
|---|---|---|
| 武器 Prefab | `Weapon_<WeaponId>_<Name>` | `Weapon_training-rifle_Blockout` |
| 武器根节点 | `WeaponRoot_<WeaponId>` | `WeaponRoot_training-rifle` |
| 后手握把 | `Grip_<WeaponId>_RearHand` | `Grip_training-rifle_RearHand` |
| 前手握把 | `Grip_<WeaponId>_FrontHand` | `Grip_training-rifle_FrontHand` |
| 枪托/肩托 | `Stock_<WeaponId>` | `Stock_training-rifle` |
| 枪口 | `Muzzle_<WeaponId>` | `Muzzle_training-rifle` |
| 瞄准线参考 | `AimLine_<WeaponId>` | `AimLine_training-rifle` |
| 弹匣位置 | `Magazine_<WeaponId>` | `Magazine_training-rifle` |
| 左肩参考点 | `Shoulder_<WeaponId>_Left` | `Shoulder_training-rifle_Left` |
| 右肩参考点 | `Shoulder_<WeaponId>_Right` | `Shoulder_training-rifle_Right` |
| 弹道/曳光实例 | `Tracer_<WeaponId>_<ShotIndex>` | `Tracer_training-rifle_001` |
| 弹着反馈 | `Impact_<TargetId>_<ShotIndex>` | `Impact_ZeroingTarget_001` |

关键武器对象必须带稳定测试 ID：

- `ZeroingRange.Weapon.PlayerRoot`
- `ZeroingRange.Weapon.TrainingRifle`
- `ZeroingRange.Weapon.Grip.RearHand`
- `ZeroingRange.Weapon.Grip.FrontHand`
- `ZeroingRange.Weapon.Muzzle`
- `ZeroingRange.Weapon.AimLine`
- `ZeroingRange.Weapon.Shoulder.Left`
- `ZeroingRange.Weapon.Shoulder.Right`
- `ZeroingRange.Weapon.DebugInput`
- `ZeroingRange.Weapon.TracerRoot`

武器 Prefab 必须提供一个绑定脚本或等效组件，序列化引用枪口、瞄准线、前后手握把、肩侧参考点和弹匣位置。PlayMode Test 不应通过对象名猜测这些引用，而应读取绑定组件验证完整性。

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
| WeaponService | 全局单例，保存当前装备 |
| SettingsService | 全局单例，负责持久化 |
| SquadCommandService | 战斗场景创建，随 Session 销毁 |

## 输入适配

UI 不直接读取具体手柄按键，统一走输入适配：

```csharp
public interface IXRTrainingInput
{
    bool ConfirmPressed { get; }
    bool BackPressed { get; }
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
    bool RearHandActive { get; }
    bool FrontHandActive { get; }
    Pose HeadPose { get; }
    Pose RearHandPose { get; }
    Pose FrontHandPose { get; }
}
```

`IXRTrainingInput` 负责按钮和轴命令，`IWeaponPoseInput` 负责头、后手、前手姿态。真实 XR、XR Device Simulator 和无 VR 调试替身都应能适配到这两个抽象入口。

输入适配是 P1 基础能力：

- 模拟输入、XR Device Simulator、键鼠调试输入和真实 XR 输入都必须适配到同一接口。
- 服务层只消费抽象输入事件或命令，不直接读取具体手柄按键、键盘按键或设备 API。
- PlayMode Test 必须能注入测试输入，覆盖确认、返回、扳机、换弹、左右肩切换和瞄准模式。
- 真实 VR 设备到位前，无 VR 输入替身路径必须可完成 P1 100m 射校闭环。
- 无 VR 调试输入必须能在 Editor Play Mode 中模拟头部视角、后手姿态、前手姿态和枪线变化。
- 瞄准模式下，视觉相机或 ADS 代理必须对齐 `AimLine_*`，有效射击方向、可见弹道和命中计算必须使用同一枪线。

## 验收约束

- 所有 BDD 中出现的按钮必须存在对应 `Button_*` 测试 ID。
- 所有 HUD 必填字段必须存在对应 `Text_*` 或 `Hud_*` 测试 ID。
- PlayMode Test 应能通过测试 ID 找到控件并模拟点击。
- 场景加载期间路由应进入 Busy 状态，避免重复点击。

