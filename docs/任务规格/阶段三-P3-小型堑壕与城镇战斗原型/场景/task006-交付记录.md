# task006 场景交付记录

状态：`[~]` 场景实现与自测已交付，独立审核/真实服务联调/VR 实机待验。按用户明确范围，仅做场景部分。

## 实际交付

烘焙导航；敌人/队友 Prefab 的命中体、感知源、枪口、音源、尸体根和执行器；命中去重、死亡停导航与尸体保留、导航到达/失败事实、简化无人机投影和独立 Fixture。

统一场景：`Assets/Scenes/CombatScene.unity`，保留 TrenchScene / UrbanScene 逻辑入口。实现位于 `Assets/SimulatedShooting/Runtime/Scene/Combat*.cs`、`Editor/CombatSceneBuilder.cs`；资源位于 `Art/Combat/`、`Prefabs/Combat/`。完整运行步骤、按任务核对、资源许可及限制见 [场景3交付说明](../../../codex-reports/scene3/README.md)。

## BDD / 接口 / 测试

- BDD：14 无人机投影/小队配置；15 命中与死亡表现；17 重新开始/平面图。 只验场景职责，不以 Fixture 冒充真实任务闭环。
- 接口：03、06、07、08、09、11 中对应场景责任；新增场景本地端口说明 14，不替代玩法服务契约。
- EditMode：`CombatSceneTests`；全项目本轮 163/163 通过。
- PlayMode：`CombatScenePlayModeTests`；本轮场景专项 9/9 通过，含实际 CharacterController 从堑壕到五个房间并返回、门范围/射线/幂等、导航到达失败、墙遮挡、尸体、复位/卸载、相机互斥。
- [结果 XML 与截图](../../../codex-reports/scene3/README.md) 已归档；可复现命令见总交付说明。

## 替身、联调与待验

`CombatSceneFixture` 提供固定场景输出，默认显示 13 个敌人视觉实例和两名队友。无生产 Session、血量、弹药、搜索进度或结算。关闭 Fixture 后，真实装配经绑定及表现端口接入；真实服务联调尚未进行。

VR：未连接头显；已验证替身下相机互斥和头部姿态不受射击表现改写。移动/转身、机瞄、队友遮挡、楼梯舒适度、最坏视角 CPU/GPU 预算须设备到位后验收。当前角色为简化原型，非写实人体美术终稿。批处理性能样本不是 VR 帧率证明。

提交号：未创建提交；起始 HEAD 为 `5605031`，本次文件保留于工作区供检查。未改动 P1/P2 保存场景。
