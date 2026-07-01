# task004 100m 靶场 Blockout 与场景结构

## 负责人

场景

## 目标

搭建 P1 100m 靶场原型场景，先满足射击流程、空间比例、靶标识别、UI/武器联调，不追求最终美术精度。

## 参考资料

- `UI/Sample/vr-shooting-100m-zeroing-first-person-hud-ui.png`
- `UI/Sample/vr-shooting-100m-zeroing-briefing-ui.png`
- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/screens/04-100m任务说明.feature.md`
- `docs/BDD/screens/05-100m射击HUD.feature.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## 交付内容

- 100m 靶场原型场景或现有场景中的 P1 区域。
- 射击位、玩家出生点、靶标点位。
- 标准胸靶占位：50cm x 50cm，10 环直径 10cm。
- 基础地面、背景、光照和距离感。
- VR Origin / 测试相机摆放点。
- 场景对象命名和必要测试 ID。

## 风格要求

- 参考 UI 样图的写实军事训练氛围。
- 模型和材质保持低/中精度，优先保证 VR 原型流畅。
- 不使用过重后处理和高面数装饰物阻碍性能。

## 不包含

- 不做移动靶。
- 不做敌人和队友。
- 不做最终美术 polish。

## 依赖关系

- 前置依赖：无。
- 后续依赖：task005、task008、task009、task012、task016。

## 联调说明

- 与 UI 联调：确认 HUD/Canvas 在射击位视野内可读。
- 与 功能A 联调：确认场景 ID、训练开始点、靶标 ID。
- 与 功能B 联调：确认武器发射点和靶标碰撞范围。

## 测试要求

- PlayMode 测试：
  - 场景可加载。
  - 能找到射击位、靶标、测试相机/VR Origin。
  - 靶标对象具备稳定名称或测试 ID。

## 验收标准

- 从射击位看向 100m 靶标时空间关系清晰。
- 靶标可被射线或基础弹道命中。
- 场景能支持无 VR 和 VR 两种测试路径。
- 没有阻挡 HUD 和武器视线的明显物体。
