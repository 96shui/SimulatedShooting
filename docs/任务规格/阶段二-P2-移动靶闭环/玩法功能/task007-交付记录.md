# task007 交付记录：两发起射/长按连射、武器弹药、命中与应用流程

## 交付状态

- 工程实现与自动化测试已通过，日期：2026-08-18。
- EditMode 相关用例 32/32 通过；PlayMode 无 VR 用例 5/5 通过。
- 阶段审核节点 C 仍需与场景 task006 组合验收；VR 实机舒适度保留到节点 D。

## 实现清单

- `IXRTrainingInput` 增加 `TriggerHeld` / `TriggerReleased`，迟滞阈值集中在 `WeaponTriggerHysteresis`（0.75 / 0.25）。
- `IAmmoService` 增加 `ReserveAmmo` / `ConsumeReservedAmmo` / `ReleaseAmmoReservation`；P2 起射两发原子预留，持续段逐发扣弹。
- `IWeaponAutomaticFireService` + `WeaponAutomaticFireService`：可注入配置与显式 Tick，支持 P1 单发与 P2 两发起射/长按连射。
- `TrainingWeaponFireCoordinator`：串联展示门禁、移动靶倒计时/禁射、连射调度、逐发 `RecordShot` 和序列收口。
- 场景武器控制器改为提交 Trigger 状态和枪线快照，消费 `WeaponShotResultEvent` 做逐发表现，不再在 Trigger 边沿直接调用 `Fire`。

## 测试追溯

| 测试 | BDD |
|---|---|
| `Screen09_WeaponAutomaticFireServiceTests` | 09 快速两发、长按连射、10 发耗尽、1 发拒绝、禁射不自动恢复、大步长 Tick、幂等 |
| `Screen09_TrainingWeaponFireCoordinatorTests` | 09 倒计时不计分、左端停火、结算保留序列；00 P1 长按仍单发且三发一轮 |
| `Screen09_AutoFireNoVrPlayModeTests` | 09 无 VR 快速两发与长按 10 发 |
| `Screen05_XRTrainingInputTests` Task007 | 00/05 Trigger Held/Released 与迟滞 |

## 关闭编辑器后直接跑测试

```powershell
& "D:\Unity Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" -batchmode -nographics -projectPath "d:\UnityProject\VR" -runTests -testPlatform EditMode -testFilter "Screen09_WeaponAutomaticFireServiceTests|Screen09_TrainingWeaponFireCoordinatorTests|Screen05_XRTrainingInputTests" -testResults "d:\UnityProject\VR\TestResults-task007-editmode.xml" -logFile "d:\UnityProject\VR\TestResults-task007-editmode.log"
```

```powershell
& "D:\Unity Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" -batchmode -nographics -projectPath "d:\UnityProject\VR" -runTests -testPlatform PlayMode -testFilter "Screen09_AutoFireNoVrPlayModeTests" -testResults "d:\UnityProject\VR\TestResults-task007-playmode.xml" -logFile "d:\UnityProject\VR\TestResults-task007-playmode.log"
```
