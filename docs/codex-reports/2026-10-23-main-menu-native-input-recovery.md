# MainScene 原生VR姿态冻结整改

范围：P1 UI task008、BDD02、接口11。保留既有场景布局、手臂显示保护与VR武器修复。

## 根因与修复

12:53原生播放采样：HeadTrackingOpenXR native=True、enabled=True、isTracked=1，centerEyePosition=(-0.56,0.82,0.52)，但Main Camera本地位置仍为(0,0,0)。输入资产92/92动作已启用，devices却是有值的空列表；全部Pose Driver的controls为空。单纯Enable动作不能恢复设备绑定。

XRSceneHandoffTests清理把InputDevice[]类型的null赋给nullable ReadOnlyArray属性。Input System的隐式数组转换先构造非空struct，再包装nullable，最终成为空的排他过滤器。Editor实际开启DisableDomainReload，使该资产运行状态跨次播放保留。此前只隐藏未跟踪手臂，未修复输入过滤问题。

- MainMenuXRModeController在恢复动作前清除空过滤器或仅含已移除设备的过滤器；包含有效设备的测试过滤保持不变。
- 测试清理显式区分null与有效数组，恢复真正的nullable null。
- EditorSettings恢复完整域和场景重载，并通过Editor API确认options=None、optionsEnabled=False。
- 不添加固定眼高来覆盖原生HMD姿态；Floor模式仍使用真实位置。

## 验证

新增Screen02_MenuRecoversEmptyAndRemovedDeviceFilters先红后绿：修复前稳定失败“Empty devices blocks every native head/hand binding”；修复后空过滤、移除设备过滤恢复，保留有效过滤且真实Pose Driver接收输入替身。

XRSceneHandoffTests共10/10 PlayMode通过，覆盖头手跟踪、左移动/右转向配置、真实扳机UI路由、训练场景与城镇往返、手臂遮挡恢复。结果见evidence/main-menu-native-input-20261023/playmode-10-passed.json。用例日志包含Starter Assets缺少眼动设备的提示及模拟设备无触觉能力提示，不宣称Console零警告。

测试退出后恢复原生OpenXR配置（MockRuntime关闭），正常重新播放MainScene，不创建模拟设备或强制VR模式。12:55实机采样确认filter=none，头显Pose绑定/HeadTrackingOpenXR/centereyeposition，Main Camera本地位置与硬件上报(-0.56,0.82,0.52)一致；Rig地面Y=0。原生相机也已应用头显旋转。截图是当前放置姿态，不能代表佩戴舒适度验收。

当前原生运行时只枚举头显，没有左右手柄设备，因此手柄实机射线、摇杆移动和扳机点击仍待两个手柄连接并唤醒后验证。SteamVR日志已成功加载oculus_touch到pico_controller的映射，没有据此更改Profile。已向用户请求确认设备连接状态，不能将输入替身10通过表述为手柄实机已通过。

原生快照见evidence/main-menu-native-input-20261023/native-play-state.txt。保持MainScene原生播放，便于用户连接手柄复验；未提交或推送。
