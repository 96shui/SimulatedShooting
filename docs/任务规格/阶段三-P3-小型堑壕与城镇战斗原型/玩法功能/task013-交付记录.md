# task013 应用装配交付记录（部分完成）

2026-09-15。开发基线 `e170331`，本文件所在 Git 提交是本次交付版本。task007 已推送 `91139e5`，task010 已推送 `e170331`。

**状态 `[~]`：应用层可独立完成的工程已交付；正式 UI、场景 loader、真实无 VR E2E 尚未交付，不宣称 task013 或阶段验收完成。**

## 已实现内容

- `Assets/VRShooting/Runtime/Application/Combat/CombatApplicationCoordinator.cs`：模式地图路由、异步加载、堑壕先简报再创建 Session、城镇加载后创建 Session、Busy、加载失败恢复、取消、快速切模式、迟到 lease 清理、重试和返回。独占 lease 防止旧加载清理新场景；每轮重新构造生产任务服务，保留地图/武器/种子并更新 SessionId。
- `CombatMission.cs`：复用 task004/007/010 的生产战斗、队形和任务服务，提供 HUD/状态/世界事实/帧推进端口；街道与建筑持续使用同一 Session；结果摘要由服务 DTO 产生，记录一次 UTC 完成时间。
- `CombatApplicationPresenter.cs`：将应用快照交给 View，Dispose 退订。无 UGUI 业务规则；HUD、简报和结果继续使用原服务契约。
- `CombatSummaryFileStore.cs`：实现既有 `ICombatSummaryStore`，每模式一个最近结果 JSON，临时文件刷盘后同目录原子替换；存储失败保留结果页和上一份文件，恢复后可重试返回。默认路径为 `Application.persistentDataPath/CombatResults/`。
- `ApplicationServices.CreateDefault`：新增可注入 `combatScenes/combatStore` 参数，注册 `Combat`；未提供正式 loader 时明确返回 ResourceUnavailable，绝不默认接入 Fake。`GameMain.OnDestroy` 清理 Combat 生命周期。
- `Runtime/Contracts/P3/CombatApplicationContracts.cs`：应用快照、场景加载/租约、帧推进和 View 端口。`IHUDService.cs` 连同原 meta/GUID 从 Application 移至 Contracts，命名空间与 API 不变，独立消费者不再依赖 Runtime 程序集。

## 测试与证据

Unity 2022.3.62f3c1 / Unity Test Framework 1.1.33 / Windows / 无 VR。

最终结果见 [原始证据目录](../../../codex-reports/evidence/p3-task013)。全量 EditMode/PlayMode 包含 P1/P2 以及 P3 task001/004/007/010。

| 最终复验 | 通过/总数 | 失败/跳过 |
|---|---:|---:|
| EditMode | 246/246 | 0/0 |
| PlayMode | 132/132 | 0/0 |

本任务新增 EditMode 17 例、PlayMode 2 例。最终 `p3-task013-editmode-verified.xml` 和 `p3-task013-playmode-verified.xml` 已提交；无 C# 编译错误或警告。无设备 OpenXR 初始化及许可证访问令牌更新仍是环境提示，不影响无 VR 自动测试，也不代表 VR 通过。

| 测试 | BDD 与职责 |
|---|---|
| `Screen26_CombatApplicationTests` | BDD14/18：加载失败、Busy、错误场景、激活失败回滚、加载中取消/销毁、快速切模式与旧 lease 隔离；BDD17/21：三次重试、新 Session/初始弹药、旧事实拒绝、摘要保存失败恢复、回调重入保护、Presenter 退订 |
| `CombatSummaryFileStoreTests` | BDD17/21 返回：跨实例读取、各模式独立覆盖、无历史增长、无效摘要不覆盖、损坏文件、无法写入及锁定目标文件时保留旧结果 |
| `Screen26_CombatCompositionPlayModeTests` | BDD14/17/18/19/20/21：UGUI 测试按钮→生产应用服务→生产玩法服务，双模式胜利、失败、重试、返回与保存失败恢复，街道/建筑同一 Session |

执行命令（依次运行，避免两个 Editor 占用同一工程）：

```powershell
& 'D:/Unity Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/UnityProject/VR' -runTests -testPlatform EditMode -testResults 'D:/UnityProject/VR/Logs/p3-task013-editmode-verified.xml' -logFile 'D:/UnityProject/VR/Logs/p3-task013-editmode-verified.log'
& 'D:/Unity Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -batchmode -projectPath 'D:/UnityProject/VR' -runTests -testPlatform PlayMode -testResults 'D:/UnityProject/VR/Logs/p3-task013-playmode-verified.xml' -logFile 'D:/UnityProject/VR/Logs/p3-task013-playmode-verified.log'
```

首次 EditMode 有 10 个新测试因该版本测试框架不支持 `[Test] async Task` 而未执行；改为受控加载完成后同步断言，没有删除或放宽行为断言。后续 244/244 EditMode、132/132 PlayMode 已通过；再加场景激活/清理及存储重入测试后重新执行全量，最终 246/246、132/132。完整 Editor 日志留本机 Logs，原始 XML 提交；不上传含许可证会话信息的 Editor 日志。

## 替身与当前联调缺口

1. 本次使用 `CombatApplicationFixture` 独占 lease、`P3Fixtures` 地图、`FakeCombatClock/Random/World` 和存储故障替身。PlayMode 的 `ButtonView` 是测试用 UGUI View，命中/范围事实由测试提交。**它们不是正式 UI、实际场景、XR 输入或真实枪线 E2E。** 本地文件存储测试使用唯一临时目录。
2. main 中没有 P3 正式 UI task002/005/008/011；主菜单 P3 按钮仍禁用。远程 `origin/UI-Design` 位于 `a4d9b2b`（既有 UI 重构），不能作为 P3 UI 接入证据。
3. 场景轨 `origin/dev/scene-p3` 检查时为 `d10a714`，提交前 fetch 已更新至 `8fd6793`（导航和可行走区域修复）。该分支提供独立 CombatScene、角色/门和调试 Fixture，尚未以正式 lease/事实/输入端口接入 main，也无本次接口验收。未直接合并该分支或将调试 Fixture 注册成正式玩法。
4. 任务规格明确“不接管 UI/场景主体制作”，真实集成依赖这些轨道的已交付产物。因此保留任务013真实交付缺口，不把本次应用层自测计为 C/D 通过；无需通过改任务边界虚构完成。

## 无 VR 复现与正式接入步骤

当前自动演示：Unity Test Runner → PlayMode → `Screen26_CombatCompositionPlayModeTests`，两个测试自动执行胜利→重试→死亡失败→保存失败→恢复保存→返回；不需要手动修改 Hierarchy。EditMode 运行 `Screen26_CombatApplicationTests` 可复验加载失败、取消和三次重试。

正式产物到位后的剩余步骤：

1. 场景轨把选定地图加载为独占 `ICombatSceneLease`，提供经 Validate 通过的 Definition/Clock/Random/Navigation；只在 Activate 创建角色并绑定 `CombatInputController`、世界范围/门/楼层事实、视觉快照与 HUD。每帧输入及事实后调用传入的 `ICombatTickPort.Advance`；Deactivate/Dispose 幂等清理且不得抛异常。
2. UI 轨在现有页面绑定 `CombatApplicationPresenter` 与任务 DTO；使用 `OpenMode/SelectMapAsync/Start/Retry/BackToMaps/ReturnToMainMenu`，将错误/Busy 渲染到 UI。正式 loader 通过组合根参数注入后才能启用主菜单入口。
3. 使用正式 UI、实际场景和同一无 VR 输入适配分别完成两模式胜利/失败/重试/退出；补充场景加载失败、快速切模式、保存失败和三次重试的真实 PlayMode E2E，并提交场景版本/截图/原始测试证据。
4. 审核负责人独立执行 B/C/D。VR 待验：记录头显与控制器，测试两模式移动/姿态/机瞄/双手握持、UI 可读可达、队友路径遮挡、进入建筑与门交互、结果返回后的输入和场景清理、72Hz 性能。当前均未实机执行。

异常复位：加载失败停在对应地图页可重选；返回/切模式取消旧加载，迟到资源自清理；激活失败取消新 Session 并回地图；保存失败留结果页，恢复存储后再按返回；旧 Session 的帧推进和事实在 Dispose 后拒绝。P2 阶段签核、VR/性能遗留不因本次自动回归而视为完成。
