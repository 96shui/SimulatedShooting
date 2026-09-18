# AmericanSoldier 与派生角色

- 原模型：用户提供 `army3.fbx`，来自此前选择的 AMERICAN SOLDIER RIGGED 下载。
- 下载页：https://sketchfab.com/3d-models/american-soldier-rigged-5b1967e7f83547be8a1c3ae656ab1ad4
- 发布者：RedVeil studio；页面标注 CC Attribution。保留来源与署名，发布项目时一并列入素材鸣谢。
- `Source~/AmericanSoldier-original.zip`：用户提供文件原名误为 `mott_var01_body_n.tex.jpeg`，检查文件签名后发现是 ZIP；仅改名保存，内容不变。
- `Embedded/`：通过 Unity 从原 FBX 提取的内嵌贴图；外部 JPEG 存在名称错位，因此正式材质引用内嵌版本。原外部图片保留。
- 模型：52 个蒙皮骨骼，原始动画只有约 0.033 秒的姿势片段；不是完整动作包。实际贴图为 256/512 级别，不宣称高清 4K。
- 派生内容：URP 材质、友军军服/帽子色调、蓝色臂带与胸背标记、武器挂点及手部姿势。
- 动作来源：项目现有 Quaternius SWAT，CC0，详见 `../CC0TacticalSWAT/ATTRIBUTION.md`。
- `Animations/`：由 AmericanSoldierInstaller 将 SWAT 待机、走路、跑步、射击、受击和死亡姿势离线转换为此模型骨骼的动画曲线。运行时动画不负责位移或伤害判断。
