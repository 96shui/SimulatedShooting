# 手雷重爆炸声音与双靶场铁拳胜利修复

基线提交 `8a5782d`，Unity 2022.3.62f3c1。本次采用增量修复，交付对应P3玩法task015、P1场景task016、P2场景task006；BDD27及BDD00、接口19及接口11同步更新。

## 1. 手雷爆炸音效

旧实现是运行时生成的0.5秒噪声，2m起快速对数衰减、28m到远距，缺少重爆炸冲击和尾音。

替换为Mike Koenig的 [Grenade Explosion](https://soundbible.com/1467-Grenade-Explosion.html)，CC BY 3.0。原始WAV、加工脚本、SHA256及加工记录存于 `Assets/Resources/Combat/Audio/Source~/`；署名和许可记录见旁边的 `GrenadeExplosion_Heavy.ATTRIBUTION.md`。

新音源为48kHz单声道PCM16，2.5375秒。保留原爆炸冲击与衰减，略增强现有低频、软限幅，峰值0.98；前200ms RMS从原素材0.256提升至0.384。Unity默认、Standalone和Android均使用PCM导入，避免Vorbis解码峰值溢出。

生产AudioSource音量1，3D空间声，12m以内完整音量、100m归零，线性衰减；18m正常投掷距离对应约0.932距离增益。烟尘视觉仍按原寿命结束，但根对象保留至2.54秒声音尾音结束；禁用、退出或重试停止并回收旧会话音源。轨迹、半径、伤害、冷却及结算未修改。

实际Unity输出证据：采用CombatScene的唯一正式AudioListener，爆炸点距离18m，监听输出最大窗口RMS **0.3639664**，源输出RMS **0.333214**，音源非虚拟、主音量1且未暂停。记录见 [audio-playback.txt](evidence/grenade-audio-slogans/audio-playback.txt)。

## 2. 模式一、模式二墙面文字

模式一已有正确图集与网格，但暗红字配黑色背板对比度低；模式二仍用旧“数据致胜”纹理与UV。统一为 `Slogan_铁拳胜利`，子节点依次为 `Character_铁/拳/胜/利`，使用完整图集四等分UV和统一字高。历史 `<Range>.Visual.Slogan.DataWins` 测试ID保留兼容。

四字使用更清楚的红色，原背墙改用专用浅色混凝土材质；保留深度测试，文字在墙面前方，未改为穿墙显示。通过 `RangeIronFistVictoryRepair` 增量更新现有场景，并让场景Builder沿用同一修复，避免重新生成后回退旧字。

从正式射击相机及固定卧姿参考点的双眼替身视点，检查四字均在视野内且无遮挡；每字在1920×1080渲染中均有可辨的红色像素。六张实际渲染与逐字计数记录位于 [evidence/grenade-audio-slogans](evidence/grenade-audio-slogans)。

## 3. 测试与门禁

- EditMode：两个已保存场景的正确文字、材质与完整四等分UV，**2/2通过**。
- PlayMode：双场景文字可见性、桌面/双眼替身渲染、原场景绑定回归、爆炸音源/完整尾音/禁用停止、生产H键投掷→实际爆炸→新音源→退出回收，**9/9通过**。
- 音效输出测试补强后，两个音效用例再次**2/2通过**；使用唯一的正式监听器，验证18m实际混音输出。
- 结果和job ID见 [validation.json](evidence/grenade-audio-slogans/validation.json)。未执行全量回归。
- 首轮验证暴露Vorbis峰值溢出及静态合批不可读UV，已通过PCM导入、EditMode检查原UV和PlayMode检查实际渲染解决；最终音频输出测试不添加第二监听器。

## VR 实机检查项

模式一与模式二进入实射后观察后墙四字，检查双眼可读性；模式三在正常投掷距离听重爆炸和完整尾音，检查相对枪声的主观响度；退出/重试后确认没有旧爆炸尾音。自动化使用无设备替身，不将其表述为实机舒适度通过。
