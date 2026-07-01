# task003 输入适配、无 VR 测试替身与 XR 预留

## 负责人

功能B-武器输入与AI基础

## 目标

建立 P1 输入抽象，保证没有 VR 设备时也能在 Unity Editor 中完成确认、返回、开火、换弹、左右肩切换；VR 设备到位后可替换到真实 XR 输入。

## 参考资料

- `docs/BDD/README.md`
- `docs/BDD/测试实现建议-Unity.md`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## 交付内容

- `IXRTrainingInput` 或等效输入接口：
  - `ConfirmPressed`
  - `BackPressed`
  - `TriggerPressed`
  - `ReloadPressed`
  - `SwitchShoulderPressed`
  - `TurnAxis`
  - `MoveAxis`
- 无 VR 输入替身：
  - PlayMode Test 可直接驱动。
  - Unity Editor 可用键鼠或调试脚本触发。
- XR 输入实现预留：
  - 真实 XR Controller / XR Device Simulator 后续可接入同一接口。
- 输入事件不得直接调用 UI 或玩法对象，必须通过命令/服务进入系统。

## 不包含

- 不实现武器开火逻辑。
- 不实现 UI 页面。

## 依赖关系

- 前置依赖：task001。
- 后续依赖：task005、task006、task008-task014。

## 联调说明

- 与 功能A 联调：确认输入命令如何驱动 Session 和射校服务。
- 与 UI 联调：确认按钮/测试 ID 与确认、返回输入一致。

## 测试要求

- EditMode 单元测试：
  - 输入替身可以设置和清空按键状态。
  - 连续帧输入不会重复触发一次性命令。
- PlayMode 测试：
  - 使用输入替身触发主菜单 100m 入口。
  - 使用输入替身触发开火、换弹、左右肩切换。

## 验收标准

- 无 VR 设备时可通过输入替身完成 P1 所需输入。
- 真实 XR 输入路径不影响无 VR 测试路径。
- 业务服务不直接读取硬件按键。
- EditMode 测试通过，PlayMode 输入驱动能力可被 task014 使用。
