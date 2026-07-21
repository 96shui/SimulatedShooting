# Task013 交付报告：VR 拾枪、双手持枪与后坐力

- 日期：2026-07-22
- 工作分支：`feature/task013-vr-weapon-interaction`
- 基线：`main` / `origin/main` @ `176fe6492a1d21f95dc48d73a2d553110d5ed7d8`
- 当前状态：开发、自动化验证与真实 VR 核心交互验收完成

## 交付结果

- 增加 `OnRack`、`RearHandHeld`、`TwoHandHeld`、`Dropped` 武器持有状态机；只有双手握持、双手跟踪有效且弹药/训练规则允许时才可开火。
- 使用 XRI Direct Interactor 实现右手后握把近距 Grip 拾枪、左手前握把二次选择、释放前手禁射、释放后手原子取消前手与掉落/越界回架；后/前握把半径分别固定为 0.10m/0.12m，并有 PlayMode 断言。
- 左右 Grip 与右手模拟量 Trigger 通过输入适配层进入；Trigger 使用 0.75/0.25 迟滞，键鼠/手工输入替身保留同等状态转换能力。
- 增加 VR/无 VR 自动运行模式：检测到运行中的 XR Display 时启用 XR Origin/HMD Camera，否则启用 `Camera_NoVR`；玩家相机、AudioListener 与 XRInteractionManager 保持唯一；可直接从 `ZeroingRangeScene` 进入 Play，不再被启动状态机切回主菜单。
- 后坐力改为确定性枪体局部冲量：包含上跳、后退、轻微横摆、短促击发阶段与阻尼回正；后手主触觉、前手弱触觉，不修改 HMD、XR Origin 或 VR FOV。
- `WeaponShotResultDto` 冻结射击时刻的持有状态、跟踪状态、稳定度、原始/有效枪线、射击序号和后坐力冲量，射校与 HUD 消费同一份结果。
- 清理 OpenXR Settings 中不再受当前包支持的 MockDriver 与 3 个无脚本孤立子资源，运行时 Missing Script 已消失。

## 自动化测试

- EditMode：73/73 通过，0 失败，job `3490e0a8c58041e190147f65c9e4e54f`。
- PlayMode：43/43 通过，0 失败，job `69172899275e4d59929ee8de7ed02c1f`。
- 覆盖重点：拾枪/双手/掉落状态转换、单手禁射、跟踪丢失、Trigger 迟滞、确定性后坐力、枪体局部回正、触觉分配、射击快照、XR/无 VR 相机与监听器互斥。

## Unity MCP 运行态证据

- Unity：2022.3.62f3c1；场景：`ZeroingRangeScene`。
- 无头显稳定态：`vrMode=false`，`xrDisplaysRunning=0`。
- 活动对象：`Camera_NoVR` 1 个、AudioListener 1 个、XRInteractionManager 1 个、EventSystem 1 个。
- 武器：控制器状态 `OnRack`，抓取组件状态 `OnRack`，`CanShoot=false`，拾取半径 0.10m/0.12m，XR Origin 未激活。
- 稳定态控制台：0 Error / 0 Warning。
- Prefab 序列化检查：枪械绑定、Grab Interactable、Rigidbody、3 个 Collider 均存在，前后握把锚点位于 `RecoilRoot` 外。
- 截图：[task013-no-vr-runtime-1.png](evidence/task013-no-vr-runtime-1.png)

## 真实 VR 实机验收

- 2026-07-22 用户确认真实 VR 设备能够通过 OpenXR 进入场景，移动和头部视角转动正常。
- 用户确认能够近距拾取训练步枪，并在双手持枪后使用右手扳机成功开火，明确本次 task013 验收成功。
- HMD/控制器型号、OpenXR Runtime、连接方式、刷新率和触觉能力未在本次会话中提供，后续建立正式性能基线时补录。
- 已知独立问题：MainScene 主菜单 UI 在头显中不可见。它不回退 ZeroingRangeScene 武器交互的验收结论，后续归入 task015/总联调修复。

## 评审重点

- 确认 `WeaponHoldState` 转换和 `CanShoot` 门禁保持由服务层负责，场景组件只同步 XRI 交互状态。
- 确认后坐力仅作用于枪体 `RecoilRoot`，不修改 HMD、XR Origin 或 VR FOV。
- 确认 `WeaponShotResultDto` 冻结射击时刻数据，射校、弹着、HUD 与触觉消费同一次确定性结果。
