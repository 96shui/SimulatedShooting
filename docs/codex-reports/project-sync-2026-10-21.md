# 当前项目同步到 main

## 同步范围

- 当前场景、脚本、UI、XR/项目配置，以及选定的手套手臂网格、材质、贴图、靶场标语、手雷音效和对应 `.meta` 一并纳入版本管理。
- 精简手臂素材约 11.1 MiB，位于 `Assets/SimulatedShooting/Art/Characters/ModularGlovedArms`。运行使用独立导出的网格与贴图，不需要原始整包或百度网盘目录。
- `TempAssetResearch` 是素材提取暂存与旧资源备份，运行不依赖它；Unity 生成的 Library/Temp/Logs 也不参与同步。
- P1 弹着偏移继续保留 `ab53e9e` 的最终回退，以 `7361aba` 为归档基准；此前据枪平滑、显式靶平面坐标和枪口校正报告只记录已撤销的历史工作，不代表当前实现。

## 检查

- Unity 编辑器没有未保存场景。
- 6 个启用场景及 Resources 预制体共 1150 个递归依赖，未发现依赖文件缺失。
- 953 条项目内依赖及对应 `.meta` 均在 Git 索引内；新增 67 个 Assets 文件，没有超过 50 MiB 的新增文件或缺失 `.meta`。
- 检查了本次变动中的 18 个 Unity 序列化文件，未发现无法解析的 GUID 引用。
- Git LFS 完整性检查通过，并确认当前分支所需的 142 MB 角色模型已上传。
- Unity 编译错误为 0；本次同步不重新导入原始素材整包。
- 此次资源同步不重新运行玩法测试；另一台电脑的头显和 OpenXR Runtime 仍需实际启动验收。

## 另一台电脑

1. 安装 Git LFS，执行 `git lfs install`；克隆仓库或更新到 main，然后执行 `git lfs pull`。
2. 使用项目指定的 Unity **2022.3.62f3c1** 打开项目，等待 Packages 与素材导入完成。依赖版本保存在 `Packages/manifest.json` 和 `Packages/packages-lock.json`。
3. 使用 `Assets/Scenes/MainScene.unity` 启动。配置与当前设备适配的 OpenXR Runtime，并连接头显和控制器。

以上同步保存项目内容和资源引用；不同显卡、运行时、头显分辨率及本机保存的用户设置可能影响最终显示或操作体验，不能将未在目标设备运行视为实机效果验证完成。
