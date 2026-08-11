# task005 跨模式 HUD、移动靶结算聚合

## 负责人

功能A-玩法流程与规则

## 目标

把 P1 的 100m HUD 能力扩展为可按训练模式选择 Provider 的聚合服务，为移动靶 HUD 提供稳定 DTO/事件，为结算页提供可重复查询的移动靶结果。

## 参考资料

- `docs/接口文档/03-HUD显示数据.md`
- `docs/接口文档/05-移动目标服务.md`
- `docs/BDD/screens/09-移动靶HUD.feature.md`
- `docs/BDD/screens/11-移动靶结算.feature.md`

## BDD 场景追溯

- HUD 显示等待/弹药/速度/方向/禁射/命中等状态。
- 结算显示命中、消耗弹药、速度、评级、点射记录。

## 交付物

- 扩展后的 `IHUDService` / Provider 选择。
- `MovingTargetResultDto`（或等效）查询接口。
- 对应 EditMode 测试。

