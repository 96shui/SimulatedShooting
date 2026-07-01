# 11 Unity 场景与 Prefab 约定

## 目标

约束 Unity 后端开发时的场景、Prefab、Canvas、UI 对象名和测试 ID，保证接口文档、BDD 和实现可互相追踪。

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

## 后端服务挂载建议

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
    bool CommandMenuHeld { get; }
    Vector2 TurnAxis { get; }
    Vector2 MoveAxis { get; }
}
```

## 验收约束

- 所有 BDD 中出现的按钮必须存在对应 `Button_*` 测试 ID。
- 所有 HUD 必填字段必须存在对应 `Text_*` 或 `Hud_*` 测试 ID。
- PlayMode Test 应能通过测试 ID 找到控件并模拟点击。
- 场景加载期间路由应进入 Busy 状态，避免重复点击。

