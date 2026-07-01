# task001 P1 基础契约与测试程序集规划

## 负责人

功能A-玩法流程与规则

## 目标

建立 P1 所有后续任务共用的代码契约、命名空间、程序集和测试目录，让 UI、场景、武器输入可以在同一套 DTO/接口上并行开发。

## 参考资料

- `docs/需求/阶段化整体需求说明书.md`
- `docs/BDD/README.md`
- `docs/BDD/测试实现建议-Unity.md`
- `docs/接口文档/00-前后端交互总约束.md`
- `docs/接口文档/02-训练Session数据模型.md`
- `docs/接口文档/11-Unity场景与Prefab约定.md`

## 交付内容

- 建立项目业务代码目录和程序集规划，建议至少包含：
  - Runtime 业务程序集
  - EditMode Tests 程序集
  - PlayMode Tests 程序集
- 实现或定义 P1 基础契约：
  - `ServiceResult<T>`
  - `ErrorCode`
  - `RandomSeed`
  - `TrainingMode`
  - `SessionState`
  - `ResultGrade`
  - `PlayerPosture`
  - `ShoulderSide`
  - `AmmoDto`
  - `UITestId`
- 统一命名空间和文件夹约定，避免四个人各自创建不兼容的类型。
- 给出测试命名约定：测试名需要能追溯 BDD feature 和场景。

## 不包含

- 不实现具体 100m 射校规则。
- 不实现 UI 页面。
- 不实现武器控制。

## 依赖关系

- 前置依赖：无。
- 后续依赖：task002、task003、task006、task007、task014。

## 联调说明

- 与 UI 联调：确认 `UITestId` 和 DTO 字段命名是否满足 UI 绑定。
- 与 功能B 联调：确认 `ShoulderSide`、`AmmoDto`、输入相关类型不会冲突。

## 测试要求

- EditMode 单元测试：
  - `ServiceResult<T>` 成功/失败返回。
  - `ErrorCode` 默认值和错误分支。
  - DTO 默认值不会导致空引用或 UI 崩溃。
- 测试必须无需加载 Unity 场景。

## 验收标准

- 工程中存在可编译的 Runtime 和 Test 程序集规划。
- P1 基础 DTO/枚举/通用返回结构可被其他任务引用。
- EditMode 测试通过。
- 文档或代码注释说明 P1 类型归属，避免重复定义。
