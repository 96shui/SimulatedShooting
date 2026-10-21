# MainScene 视点与 XR UI 交互恢复

## 修复范围

P1 UI task008 / BDD02。主菜单优先使用 Floor 跟踪，实际 Floor 模式不叠加固定眼高；MainScene XR 根节点恢复地面 Y=0。CameraYOffset 配置为1.65m，仅供 XROrigin 的 Device/Unbounded 跟踪回退使用，不修改 HMD 本地跟踪姿态。

MainMenuXRModeController 为当前启用的主菜单 Rig 恢复共享动作资产，避免另一个场景或测试关闭动作后，InputActionManager 已启用而没有再次触发 OnEnable，导致头手姿态和 UI Press 不再更新。此恢复仅在 MainScene 的 VR 模式运行。

主菜单 CurveVisualController 原默认采用失去命中后收缩到0.25m的视觉策略；主菜单改为 Traditional 并延伸空命中射线，使玩家在射线尚未命中菜单时也可看到指向。UI 点击仍经过 NearFarInteractor、XRUIInputModule 和 TrackedDeviceGraphicRaycaster。

此前拉取合并过的 OpenXR 本地配置已备份到 `Logs/mainmenu-xr-recovery/OpenXR-before-recovery.asset`，恢复到基线 main 的设置。Unity 导入可能重新生成 feature 子资产引用 ID，不能仅按 ID 变化判断设备配置被修改。

MainScene 的 GameMain 原有本地位置改动保留；只恢复 XR Rig 高度并修改回退眼高。模式三换弹修改保留。

## 验证

Unity 2022.3.62f3c1，基线提交8a5782d，工作区修复。四个相关运行时/PlayMode程序集编译通过。

通过项目文件请求测试桥执行以下 PlayMode：XRSceneHandoffTests、Screen02_05_SceneOwnedUIFlowTests、Screen02_04_MainMenuZeroingBriefingUITests、CombatMagazineReloadPlayModeTests、P3XRTrainingInputTests、VRHandPresentationTests。

结果：30通过，0失败，0跳过。XML位于 `Logs/p3-task001/mainmenu-xr-recovery-playmode-retry.xml`；摘要见本报告同名 `.validation.json`。第一次运行因停止播放中断，不计入结果。

新增 `Screen02_MainMenuEyeHeightVisibleRaysAndTrackedTriggerRecover` 使用模拟 XR 设备和生产 Pose Driver，验证地面根节点、正常眼高、Floor不重复偏移、空命中射线可见、动作关闭后的恢复，并从真实输入链路按下/松开扳机导航到100m任务说明，没有直接调用按钮回调。

头显中的实际地面校准和眼高、手柄按键映射及双眼菜单可读性仍须设备验收。编辑器重新打开 MainScene 后播放，确保使用已保存的场景配置。

## 实机反馈后的追加排查

用户截图反馈视点仍低、无移动与射线。前述30个模拟设备用例只能证明模拟链路，不能代表实际设备已恢复。日志和EditorSettings确认工程使用 `DisableDomainReload | DisableSceneReload`，会保留编辑器播放与测试中的部分对象和输入状态；恢复普通动作启用不足以排除残留的设备禁用、过滤或Pose Driver状态。

本次将Editor播放配置恢复完整域和场景重载。新增只读、文件请求启用的Editor诊断桥 `MainMenuXrLiveDiagnostics`，记录实际XR设备的native/enabled/tracking状态、相机与Rig高度、Pose Driver和动作绑定，不创建模拟设备、不启动PlayMode测试。显式clean-start请求在编辑模式恢复正常播放选项，重新启用被禁用的原生XR设备并清除指向已移除测试设备的动作过滤。

诊断脚本已通过Unity C#编译。实际设备恢复结论需以 `Logs/mainmenu-xr-recovery/live-play-state.txt` 及用户实际操作确认，暂不将本轮标记为实机通过。

实际连接采样：首次播放采样没有左右XR设备；SteamVR `vrserver.txt` 同时报PICO controller 1/2 `update pose without activate`。SteamVR成功加载Oculus Touch到PICO控制器的映射，未发现需要额外启用其他控制器Profile的证据。随后原生HeadTrackingOpenXR及左右OculusTouchControllerOpenXR设备出现且启用，MainScene相机记录到约1.41m姿态，左右Pose Driver绑定到原生控制器，并进入BunkersOriginalMode3CombatScene。快照保存于 `Logs/mainmenu-xr-recovery/live-native-devices-and-scene-handoff.txt`；是否在头显中可正常看见射线、移动和点击仍待用户确认。

注意Input System具有独立的Editor与Player输入缓冲。EditorApplication.update读取的isTracked值不能直接用作游戏内失踪结论；诊断已改为在Dynamic/BeforeRender输入更新后保存Player缓冲，并为报告标记inputBuffer。实际设备枚举、启用状态及Pose Driver变换可用于补充观察。
