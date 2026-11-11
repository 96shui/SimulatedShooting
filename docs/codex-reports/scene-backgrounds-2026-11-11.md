# 四模式场景背景图 — 2026-11-11

## 交付与范围

四张 PNG 来自 Unity 编辑器 **Edit Mode 离屏渲染**，不是 PICO 4 真机画面或录屏。截图使用任务指定的四个 `.unity` 文件；没有进入 Play Mode 或生成运行时敌人、队友。场景中的固定标语、距离牌、店招、路面和停放车辆作为环境保留。

目视检查结果：四张图均有可辨识的场景内容，水平、无可见 UI、武器或手部模型，未发现整张纯黑、纯白或明显失焦。以下两项与期望画面存在差距，不能将它们描述成已满足全部构图要求：

- **模式一**：当前 `ZeroingRangeScene.unity` 中未发现 `CoastalCliff_Backdrop`、海崖 FBX 引用或 `TreeLine_Left/Right`；场景里另有 `HillTree_*` 小型对象，但截图中没有期望的两侧树列。海崖和两列树对象存在于 `ZeroingRangeScene_Backup_20260816.unity`，那是旧备份，并非本次指定的场景；其中海崖是**静态模型背景层**，不是真实海景。本次未从备份搬入对象，因此图一展示当前场景实际的靶排、地面、背景墙及天空，不能展示指定海崖和树列。
- **模式三**：指定的 `BunkersTrenchCombatScene.unity` 从出生位置可以看到土质壕沟、胸墙和远处环境，但沙袋较远、并不显著，无法充分呈现期望中的沙袋细节。当前 `UnityCombatSceneLoader.TrenchScenePath` 实际指向 **`BunkersOriginalMode3CombatScene.unity`**；本次仍按明确指定的 `BunkersTrenchCombatScene.unity` 采集，不能把该图说成当前运行时加载的模式三场景。

## 相机、隐藏对象和结果

坐标为 Unity 世界坐标，角度为 `(pitch, yaw, roll)`，FOV 为垂直视场角。所有图片均为 1920×1080；平均亮度是 Pillow RGB 转 `L` 后的 8 位亮度均值（0–255，系数约为 0.299R + 0.587G + 0.114B），并非线性光照强度。标准差及 RGB 方差均在同一 8 位像素空间统计。

| 模式与图片 | 相机位置 / 欧拉角 / FOV | PNG 字节数 | 平均亮度 / 亮度标准差 | RGB 方差 R / G / B |
|---|---|---:|---:|---:|
| [01 100m 射校](evidence/backgrounds-2026-11-11/01-ZeroingRange.png) | `(0, 1.500, 0.300)` / `(0°, 0°, 0°)` / `54°` | 1,898,276 | 105.520 / 31.788 | 1584.052 / 1043.934 / 3310.155 |
| [02 移动靶](evidence/backgrounds-2026-11-11/02-MovingTargetRange.png) | `(0, 1.500, 0)` / `(0°, 0°, 0°)` / `54°` | 1,853,779 | 109.102 / 29.390 | 1395.145 / 940.137 / 2717.614 |
| [03 堑壕](evidence/backgrounds-2026-11-11/03-Trench.png) | `(-11, 1.865, 0)` / `(0°, 90°, 0°)` / `60°` | 1,793,317 | 114.451 / 59.596 | 2563.100 / 3722.003 / 6603.371 |
| [04 城镇](evidence/backgrounds-2026-11-11/04-Urban.png) | `(24, 1.515, 48)` / `(0°, 45°, 0°)` / `60°` | 1,706,908 | 88.388 / 57.006 | 3211.615 / 3260.441 / 4416.755 |

相机来源与现场隐藏清单：

1. **射校**：使用 `ZeroingRange/TrainingAnchors/Camera_NoVR` 的场景位姿和 FOV。关闭 `TrainingAnchors/WeaponPlayerRoot/Weapon_training-rifle_Blockout`、`ZeroingStartAreaCopy/WeaponReference_Blockout`、左右 `VirtualHand_*`，以及场景内全部 Canvas 和控制器 UI 线。实际天空材质为 `MovingTargetDryClearSky`，使用 `Skybox/Procedural`。
2. **移动靶**：使用 `MovingTargetRange/TrainingAnchors/Camera_NoVR` 的场景位姿和 FOV；关闭对象类别与射校相同。靶和轨道保留，天空为 `MovingTargetDryClearSky` 程序化天空。画面顶边极细的深色边缘来自现有固定场景物体 `ZeroingStartAreaCopy/RangeGate/GateIdentificationStrip`，并非 UI；按保守隐藏规则保留。
3. **堑壕**：FOV 取自 `Player_Inspection/Camera_NoVR`。位置按 `Anchor_TrenchScene` 和现有 `CombatSpawnPlacement.Resolve` 计算；锚点为 `(-11, 0.350, 0)`，解析后脚底为 `(-11, 0.470, 0)`。沿用运行时桌面眼高计算：站立参考高度 `1.860 m` × `0.75 = 1.395 m`。关闭 `Anchor_Weapon/Weapon_training-rifle_Blockout`、`Anchor_Projection/TrenchProjection`、`Anchor_Drone`、全部 Canvas 和控制器 UI 线。
4. **城镇**：FOV 取自 `Player_Inspection/Camera_NoVR`，按 `Anchor_UrbanScene` 及同一落点、桌面眼高规则计算；锚点为 `(24, 0, 48)`，解析后脚底为 `(24, 0.120, 48)`。出生锚点原本朝向 `0°`，正对建筑立面；为了从同一出生位置看到街道纵深，将水平视线转向 `45°`，俯仰和滚转仍为零。关闭对象类别与堑壕相同。

每场景的 9 个 Canvas 均来自初始未激活的 XR 控制器提示层；仍逐一禁用以覆盖 World Space 和 Screen Space 情况。工具还会禁用场景中存在的 `BaseRaycaster`；本次四个静态场景没有发现此组件。控制器中的 Near-Far 射线、传送射线和提示曲线都按路径逐一关闭。完整的对象路径和原启用状态见本机日志中的 `[VRBG] HIDE` 条目。新建临时相机不复制相机挂载脚本，关闭 URP 后处理，并保留场景灯光、地形、天空、靶、建筑及静态道具。

## 执行与验证

- Unity 版本 `2022.3.62f3c1`，`-batchmode`，图形设备为 Direct3D 11 / NVIDIA GeForce RTX 4070 Ti SUPER，**未使用 `-nographics`**。
- 工具为 [SceneBackgroundCapture.cs](../../Assets/VRShooting/Editor/SceneBackgroundCapture.cs)。通过 `EditorSceneManager.OpenScene(..., OpenSceneMode.Single)` 在内存中逐场景处理；临时相机复制玩家相机设置，以 1920×1080、24 位深度、ARGB32 的 RenderTexture 调用 `Camera.Render()`，再 `ReadPixels` 和 `EncodeToPNG`。没有使用 `ScreenCapture.CaptureScreenshot`，也没有调用 `SaveScene`。
- 四场景批处理日志为本机 `Build/background-capture.log`，记录 `[VRBG] COMPLETE 4 scene(s)`，Unity 退出码 `0`。`Build/` 被 Git 忽略，日志不属于提交文件。另以 `VRBG_SCENE=CombatScene` 和独立输出目录重跑单场景，日志记录 `COMPLETE 1 scene(s)`；其 PNG 的 SHA-256 与交付的城镇图一致。
- Pillow 成功解码并验证四张 PNG 均为 1920×1080；上表亮度、方差和文件大小检查均通过。人工检查了四张完整图像和战斗场景多个候选朝向；这些检查不能替代设备实机验收。
- 场景、Prefab、材质、运行时代码和 `Assets/Resources/UI/Tactical/TrainingChinese.asset` 均没有产生 Git 跟踪改动。开始前已有未跟踪的 `Exports/01-local-art.zip`、`02-thirdparty-bunkers.zip` 和 `03-rest.zip`，原样保留；因此整个工作区本来就不是完全干净状态。

### Unity 日志警告和信息

四个场景的采集区间没有出现 `[VRBG] FAILED`，但本次 Unity 日志**有警告和许可信息**：

- 第三方包 `Assets/TheTalesFactory/Bunkers & Trenches Pack/ReadMe & Publisher Window/Inspector Info/Icons.meta` 的 GUID 无效，Unity 报告相应资源被忽略。该条为资源导入警告；截图渲染仍完成，未修改第三方文件。
- Unity 退出时 XR Management 报告 `Call to StopSubsystems without an initialized manager`，发生在全部图片保存完成之后。
- 许可模块先记录 `LicensingClient has failed validation; ignoring` 与 `Access token is unavailable; failed to update`，随后记录 `Successfully updated license` 和 entitlement 解析成功。批处理最终返回 0；上述信息不应被表述成无警告。
- 启动时另有 WindowsStandalone、Android、WebGL 的 `Native extension ... not found` 信息；本次仅进行编辑器渲染，不据此推断这些平台的构建状态。

## 重跑

先确保没有其他 Unity 进程打开同一项目。`C:\vrproj` 是指向本仓库的联接；使用 PowerShell，保留图形上下文：

```powershell
$unityProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath','C:\vrproj','-executeMethod','VRShooting.EditorTools.SceneBackgroundCapture.CaptureAll','-logFile','C:\vrproj\Build\background-capture.log') -WindowStyle Hidden -Wait -PassThru
$unityProcess.ExitCode
```

仅重跑某个场景时，在同一 PowerShell 会话先设置环境变量，例如：

```powershell
$env:VRBG_SCENE = 'CombatScene' # 也可填写 Assets/Scenes/CombatScene.unity
$env:VRBG_OUT = 'C:\vrproj\Build\background-single-verification'
# 执行上面的 Start-Process 命令；退出码为 0 后检查对应 PNG 和日志。
Remove-Item Env:VRBG_SCENE, Env:VRBG_OUT
```

`VRBG_OUT` 不设置时输出到 `docs/codex-reports/evidence/backgrounds-2026-11-11/`。无效场景名或无图形设备会报错，而不会伪造截图。
