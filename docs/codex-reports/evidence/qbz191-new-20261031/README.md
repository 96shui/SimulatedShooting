# 新 QBZ191 枪械替换验收记录

- 素材为用户提供的 `191.max` 同包 `QBZ191.fbx` 及 PBR 贴图；玩家共用步枪预制体使用新模型，旧模型停用保留。
- 保留左右手原有抓握锚点，调整新模型方向、枪口、瞄准线和弹匣绑定；换弹使用新模型的独立弹匣。
- `grip-preview.png` 为 Unity 编辑器内使用场景手部模型及握持姿势渲染的侧面预览，不是 VR 实机截图。
- 无设备 PlayMode 测试：`Bdd05_NewRifleKeepsGripMuzzleAndRemovableMagazineBindings`、`Bdd05_AnyRiflePartCanBeGrabbedWithOriginalHandAnchors` 和 `HandPullsMagazineBelowViewThenSeatsBeforeAmmoCompletes` 通过。
- 待实机验证：不同视角手指贴合及穿模、枪口闪光、机械瞄具与弹着一致性、模式三四完整换弹动画；旧模型机瞄测试中的固定网格坐标不适用于新模型，须依据新模型重新校准并补测。
