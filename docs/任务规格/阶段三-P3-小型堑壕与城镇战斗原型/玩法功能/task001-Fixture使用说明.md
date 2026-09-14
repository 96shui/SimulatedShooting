# P3 task001 Fixture 使用说明

版本：`P3.Contracts.v1`。生产类型位于 `Assets/VRShooting/Runtime/Contracts/P3/`，假服务位于 `Assets/VRShooting/Tests/Support/`。以下全部是假数据/测试消费端示例，不是生产战斗实现。

## 程序集引用

- 生产 UI/场景/玩法只需引用 `VRShooting.Contracts` 获取共享类型和 P3 端口；不从本 task 引用具体生产 UI 或场景类。
- 各自测试程序集额外引用 `VRShooting.P3.TestSupport`。该程序集有 `UNITY_INCLUDE_TESTS` 与 `TestAssemblies` 约束，不进入正常玩家构建。
- 现有共享 DTO/Enums/Contracts 保留文件路径、命名空间和 GUID，以 `.asmref` 归到 `VRShooting.Contracts`。原 Runtime 与直接消费者增加该引用；未改 P1/P2 业务代码。
- `ITrenchService`、`IUrbanService` 与 `ICombat*` 位于 `VRShooting.Application` 命名空间；数据仍在 `VRShooting.Common`，结果和错误仍在 `VRShooting.Contracts`。

## UI 轨道：显式重放

```csharp
using var fake = new FakeUrbanService();
IUrbanService port = fake;
fake.Bind(P3Fixtures.UrbanSession("demo", 1));
port.SessionChanged += Render; // Render 只读 DTO
fake.Publish(P3Fixtures.UrbanSession("demo", 2, UrbanPhase.Building));
fake.NextError = ErrorCode.Busy; // 只作用于下一次命令，不影响查询
var busy = port.OpenRoomDoor("demo", "urban-a.room-001");
fake.SetResult(new UrbanResultDto {
    SessionId = "demo", Revision = 3, Victory = false, RoomsSearched = 1
});
port.SessionChanged -= Render;
```

`FakeTrenchService` 使用相同 Bind/Publish/SetResult 模式。先 Bind 再调用 StartSession；开始命令返回已绑定的脚本快照，不生成敌人、不自建会话。设置 `Maps = Array.Empty<...>()` 可验证空列表；参数已复制，不要用其承载生产配置。

- `GetMaps/SelectMap/GetBriefing` 提供地图A/简报预览，B返回NotFound。
- `GetSession/GetResult` 的查询和事件读取同一份脚本快照。
- Publish 只接受当前SessionId、递增Revision；重复、旧修订和旧会话忽略。
- SetResult 只接受当前会话的更高Revision且只能一次；它**不计算胜负**，用于UI结果渲染。
- Cancel重复当前已取消会话成功；新局必须显式Bind新ID。过期会话查询/命令NotFound。
- `NextError` 用于拒绝分支；失败不触发状态事件。Commands记录命令意图，MarkSearchNode/MarkRoomSearched 不自动修改搜索数据。
- Bind/Publish必须从同一测试线程调用；测试替身不是并发服务。结束时Dispose、UI销毁时取消事件订阅。
- Fake只验证通用会话/参数与脚本顺序；完整节点/敌人/入口的语义验证仍是task007/010的生产测试，不将Fake接受命令当作规则通过。

## 场景轨道：纯视觉与绑定配置

使用 `P3Fixtures.TrenchDefinition/UrbanDefinition` 获取独立场景配置，并调用 Validate；两者不加载任何 `.unity` 资源。配置包含候选点、地图边界、楼层/房间和搜索节点。真实地形可达性仍由场景负责人验证，Fixture的Navigable是测试输入。

使用 `FixtureFeed<CombatVisualSnapshotDto>` 或 `FakeCombatStateService.Visuals` 推送Entities/Doors；场景实现 `ICombatSceneView.Apply`，只渲染实体位置/朝向/状态/尸体与门开合。用 `FakeCombatWorld` 记录交互输入和导航请求，注入 `NextError=ResourceUnavailable` 验证导航失败分支。

参考 [PlayMode测试](../../../../Assets/VRShooting/Tests/PlayMode/Infrastructure/Screen22_P3FixturePlayModeTests.cs)：测试创建独立空白场景，挂接文本与角色/门Probe，卸载后验证订阅数归零。Probe不属于正式HUD或战斗Prefab。

## 玩法轨道：确定性输入

- `FakeCombatClock.Advance(seconds)`：显式时间和Tick，不读Time.deltaTime；拒绝负数/NaN/Infinity。
- `FakeCombatRandom(RandomSeed.Fixed(n))`：固定xorshift32序列，Reset重放；零种子有固定非零状态。Unfixed也只使用调用者给定Value，不在Fixture内生成隐式系统随机数。
- `FakeCombatWorld.Submit`：记录Hit、Perception、区域、姿态等输入原值；`Move`记录导航请求，均可注入一次失败。
- `FakeCombatStateService`：玩家/队伍/视觉分别Bind和Publish，提供带修订查询与事件；P3队友指令为空，战术命令返回InvalidState。
- `FakeCombatSummaryStore`：按模式保存最近摘要，可注入PersistenceFailed；失败不覆盖既有值，清除故障后可重试；不是磁盘存储实现。

## 在已打开的 Unity Editor 中运行

先使用 Assets → Refresh 导入代码，等待编译完成。测试运行器只在下列显式请求文件存在时启动；不会自动运行场景Builder或修改场景资产。

```powershell
@{mode='EditMode'; filter='Screen22_P3ContractTests'; name='p3-contract-editmode'} |
  ConvertTo-Json | Set-Content -Encoding utf8 Logs/p3-task001-request.json
```

完成后读取 `Logs/p3-task001/p3-contract-editmode.xml.status` 和对应 XML，再提交第二次请求：

```powershell
@{mode='PlayMode'; filter='Screen22_P3FixturePlayModeTests'; name='p3-contract-playmode'} |
  ConvertTo-Json | Set-Content -Encoding utf8 Logs/p3-task001-request.json
```

`filter=''` 执行该模式全量测试。Editor处于编译/导入/播放时等待，不并发启动测试；可从Unity Test Runner直接运行同名测试。不得复用已运行的请求名称来误读旧结果。

## 无设备与实机边界

本 task 无设备依赖，验证契约/Fixture/空白场景消费；真实移动、机瞄、地图通行、AI与性能测量属于后续 task 和节点D。设备字段待场景/审核登记，不能把72Hz目标预算当作设备实测结果。
