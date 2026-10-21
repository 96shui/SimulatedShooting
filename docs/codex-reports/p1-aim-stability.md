# P1实机难瞄准修订（2026-10-21）

## 查明的问题与边界

1. 旧路径仅凭`HitPoint.z≈100`判断其单位。实际中央靶就在世界Z≈100处，脱靶射线返回的世界米坐标也满足该条件，因此实际偏60cm可能被记录为0.6cm，靶心高度也混入垂直偏差。这会使分析平均值与后续补偿错误；之前理想靶面命中测试没有覆盖该路径。
2. 旧步枪关闭了全部XRI姿态平滑。100m处0.1°角度变化对应约17.45cm偏移，而10环半径仅5cm。已确认代码直接跟随，未采集真实设备运动来断言具体用户的抖动幅度。

## 实现

- 输入/结果新增明确的`HasTargetPlaneOffset`和`TargetPlaneOffsetCm`，从实际枪口射线与绑定靶面求交得到厘米偏差，含超出靶面的交点；服务优先读取该字段，避免米与厘米歧义。WeaponControl、连射快照和协调器完整传递；非有限坐标拒绝为InvalidInput。旧替身未提供字段时保留兼容路径。
- 模式一两手均通过XRI抓住步枪时启用XRI自带位置/旋转平滑。Dynamic与BeforeRender每次分别使用位置7/s、旋转4/s、tighten=0；枪体、机械瞄准线与实际射线共用该姿态，不提供靶心吸附。单手、左手禁握或其他模式关闭支持，继续即时跟随。HMD跟踪不变。
- 保持当前UI、十环5cm及三发全中条件、固定种子偏移、归零系数和应用幂等。本轮未改共享手部素材及场景布局。场景物理弹孔与服务校准记录仍采用历史设计的独立显示，未再次引入补偿弹孔重绘或弹道收敛。

## 实际测试结果

| 范围 | 结果 | 证据 |
|---|---|---|
| 射校规则/服务/HUD、武器、连射、协调器 | EditMode 78/78 | [editmode.xml](evidence/p1-aim-stability/editmode.xml) |
| 中央靶、±60cm脱靶厘米传递、两轮校准、UI、枪口反馈、平滑与模式门禁 | PlayMode 27/27 | [playmode.xml](evidence/p1-aim-stability/playmode.xml) |
| 实际XRI双手抓取/释放与模式二扳机、重试流程 | PlayMode 3/3 | [xr-compatibility.xml](evidence/p1-aim-stability/xr-compatibility.xml) |

抖动测试调用当前安装XRI 3.1.2的真实`StepSmoothing`，90Hz、每帧Dynamic/BeforeRender两次更新，输入8Hz的±0.12°水平、±0.10°垂直及1mm位置扰动。100m落点误差RMS从19.332cm降到3.154cm，稳态最大4.201cm；12°主动转向在1秒内收敛到0.05°以内。该数据为模拟输入对比，不是头显实测。

实景脱靶测试使用实际保存场景、枪口、Physics命中与BuildFireSnapshot→WeaponControl.Fire→ZeroingShotRecordedEvent全链路。±60cm脱靶保持物理Hit=false、服务记录实际厘米且不计入10环。实际XRI选择测试确认单手不启用、双手启用平滑；关闭P1支持后原行走即时跟随与掉落契约通过，P2输入流程通过。

`MainMenuUI.cs`前后SHA256一致，见证据目录。编译及Editor测试完成后恢复MainScene且停止播放。真实头显的舒适度、主观延迟及稳定瞄准手感仍需用户复核。
