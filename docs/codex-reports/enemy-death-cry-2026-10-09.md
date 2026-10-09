# 敌人倒地惨叫

日期：2026-10-09；基线：`dce6242`；P3 task019 / BDD23。交付目标：用户授权推送远程 main。

## 行为

- 敌人生命2→1时不播放惨叫；1→0并进入倒地状态时播放一次约1.01秒的“呃啊！”式真人男声。
- 声音跟随角色胸部骨骼，使用独立3D音源，音量0.9、近距2m、最大距离35m、不循环、关闭多普勒变调。枪声和命中声音保持自己的音源。
- 死亡快照重复刷新不会重播；复位、禁用或退出停止旧声音，新局允许再次播放。只给敌人Prefab绑定素材，友军及玩家不自动继承。
- 不修改两发死亡、子弹/手雷伤害或结算。手雷造成敌人死亡时沿用同一倒地声音。

## 素材

使用 HaelDB 的 [Male Grunt/Yelling sounds](https://opengameart.org/content/male-gruntyelling-sounds)，选用CC0许可。同一男声的 `3grunt4.wav` 与 `3yell7.wav` 经去空白、单声道混合、20ms交叉淡化与电平整理，输出PCM16/44.1kHz。

- [成品音效](../../Assets/SimulatedShooting/Audio/Voices/enemy-death-uh-ah.wav)
- [来源与处理记录](../../Assets/SimulatedShooting/Audio/ASSET_SOURCE.md)
- 原始片段保留在 `Assets/SimulatedShooting/Audio/Voices/Source~/`，Unity不导入该备份目录。
- SHA256：`c436983cb6169811589f290defe6de7825c15226516ffdca161aa5e25292985e`。

## 验证

- [EditMode 2/2](evidence/enemy-death-cry-2026-10-09/editmode.json)：敌人绑定、友军未绑定、单声道、长度、预加载及真实非零波形/无削波。
- [PlayMode 11/11](evidence/enemy-death-cry-2026-10-09/playmode.json)：两张生产地图真实子弹命中到一次死亡音效、重复死亡去重、角色复位、独立并发音源、自然结束、禁用停止，以及原倒地/重试/退出闭环。
- 素材可用与播放状态通过自动检查；真实VR空间方向、音量和惨叫听感仍需实机复验，不宣称已完成实机听感签核。

## 手工复验

进入堑壕或城镇，连续两次击中同一敌人：第一发仍站立，第二发倒地同时惨叫一次。靠近/远离应听到距离变化；重试后再次击倒可再次播放，返回菜单不能残留声音。
