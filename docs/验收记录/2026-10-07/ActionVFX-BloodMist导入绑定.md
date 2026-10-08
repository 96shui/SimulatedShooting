# ActionVFX Blood Mist 导入绑定记录

已下载原始文件 C:/Users/Administrator/Downloads/Blood_Mist_1_0700.mov，43853381字节；成功解码2048x1080、59.94fps、5.57秒MOV。
已转换64帧8x8透明序列贴图、创建URP材质和VR立体渲染宏的Shader，Resources路径Combat/VFX/BloodMist/ActionVFX_BloodMist1。
WeaponFeedbackController.PlayImpact(hitFlesh=true)替换为RecordedBloodMistVfx，沿用CombatSceneRuntime提供的实际命中位置和法线。原弹道/伤害逻辑未修改。动画0.7秒自回收，碰撞器立即禁用。
已在编辑器用实际材质绘制相机预览，白底去除、红色血雾透明叠加成功，Temp/UnityMcpTools/BloodMist-preview.png。
Unity自带Roslyn编译SimulatedShooting.Runtime全部源码成功，exit code 0。新增PlayMode资产绑定/不阻挡射线/回收回归用例，按用户要求未运行。
Unity Editor依然显示isCompiling=true，新运行时代码尚未加载。因此本次不能标记VR实机/完整命中播放验收通过。素材和代码已落盘；需待编辑器编译队列恢复后检查实际中弹播放。
未提交或推送。

## 2026-10-07 可见性复查（覆盖上文编译阻塞状态）
Unity当前编译完成，RecordedBloodMistVfx与材质均已加载。确认EnemyHit直接调用PlayConfirmedFleshImpact，取消人物血雾对弹道到达回调的依赖，避免两个入口重复触发；命中碰撞体射线优先定位，失败时用该敌人的ClosestPoint。仅反馈EntityId=session.player时播放。
渲染不透明度提高4倍，RGB稍压暗，Quad增加至1.05m×0.554m，保留录像完整画面比例；实际血雾只占画面部分。生命周期0.85秒，外层回收0.9秒，向相机推近6cm防止血雾被人体表面截掉，保留深度测试。
编辑器临时预览实际调用WeaponFeedbackController.PlayConfirmedFleshImpact：spawned=True、ImpactFeedbackCount=1、绑定ActionVFX_BloodMist1材质、compiling=False；已渲染并查看Temp/UnityMcpTools/BloodMist-visible-preview.png。临时预览场景关闭，没有修改主场景。
按要求未运行自动测试；VR实际距离、双眼及战斗射击仍需实机观察。未提交。

## 2026-10-07 血雾出现在玩家面前的修复
原因：死亡同步先禁用Actor.HitCollider，旧代码继续调用ClosestPoint(origin)，禁用碰撞体查询会把枪口origin作为结果返回，导致在玩家附近创建血雾。
新增CombatBloodImpactLocator：有效碰撞体优先射线命中；死亡后从Capsule/Box/Sphere的本地中心尺寸和Transform构造身体世界范围，不依赖禁用collider.bounds或ClosestPoint。所有回退也限制在敌人身体范围。
直接编辑器检查：敌人位于(3,0,20)、碰撞体禁用，命中点(2.96,1.20,19.70)、偏离射线回退点(2.70,1.20,19.70)，距枪口19.92m、距敌人胸部0.303m。新类已加载，compiling=False，控制台error=0。
添加死亡禁用碰撞体/射线偏离两种定位回归断言；按用户要求未运行测试用例。未提交。
