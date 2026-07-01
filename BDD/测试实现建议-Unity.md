# Unity 测试实现建议

当前项目使用 Unity 2022.3.62f3、XR Interaction Toolkit 2.2.0、TextMeshPro 3.0.6、UGUI 和 Unity Test Framework 1.1.31。BDD 文档不要求团队必须使用某个自动化框架，但建议按以下方式落地。

## 推荐测试层级

| 层级 | 建议工具 | 覆盖内容 |
|---|---|---|
| EditMode Test | Unity Test Framework | UI Presenter、状态机、配置持久化、评级算法、弹着偏差计算 |
| PlayMode Test | Unity Test Framework | 场景加载、XR 输入替身、HUD 刷新、按钮跳转、训练流程 |
| 手工验收 | VR 头显实机 | 舒适度、空间感、按钮可读性、射击手感、视野遮挡 |

## 建议的可测试架构

- `GameFlowState` 管理主菜单、模式选择、任务说明、HUD、弹窗、结算等状态。
- `TrainingSession` 保存当前模式、地图、武器、回合、弹药、命中、用时和结算数据。
- `UIRouter` 负责打开和关闭 Canvas 页面，不直接计算游戏规则。
- `HUDPresenter` 只负责把 `TrainingSession` 的状态渲染为 TMP 文本和进度条。
- `WeaponLoadoutService` 管理已选择武器、弹匣容量、备用弹药和可用场景。
- `SettingsService` 管理 VR 舒适度、转向、移动、亮度、HUD 透明度、音量、语言，并负责持久化。

## XR 输入测试替身

BDD 中的“玩家选择/确认/返回/扳机/换弹/姿态切换”建议映射为可注入输入接口：

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

自动化测试中使用假输入对象驱动场景，避免依赖真实头显硬件。

## UI 元素命名建议

- 所有按钮命名为 `Button_<页面>_<动作>`，例如 `Button_MainMenu_100mZeroing`。
- 所有文本命名为 `Text_<页面>_<数据>`，例如 `Text_ZeroingHUD_Round`。
- 所有面板命名为 `Panel_<页面>_<用途>`，例如 `Panel_ImpactAnalysis_Data`。
- 所有素材占位可替换为真实 3D 渲染或 RenderTexture，但测试 ID 保持不变。

## 随机与可重复性

- 100m 射校的初始弹着偏移应允许注入固定种子。
- 堑壕和城镇敌人生成应允许测试指定敌人数量、标红区域和实际偏移。
- 移动靶速度、方向和端点停留应由状态机驱动，测试可快进时间。
- 所有结算评级算法应能在不加载完整 3D 场景时单独测试。

## 非功能验收

- VR UI 文本在默认头显分辨率下应可读，不应被模型、武器或手柄遮挡。
- 战斗 HUD 不应阻挡主要射击视线。
- 场景加载期间应禁用重复点击，避免多次创建训练 Session。
- 返回按钮应释放当前页面临时状态，但不清除已应用的全局设置和已装备武器。

