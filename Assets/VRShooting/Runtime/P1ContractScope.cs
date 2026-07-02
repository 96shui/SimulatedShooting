namespace VRShooting
{
    /// <summary>
    /// P1 基础契约归属说明（task001）。
    /// 本程序集 <c>VRShooting.Runtime</c> 定义 P1 后续任务共用的 DTO、枚举与通用返回结构；
    /// task002 及之后任务应引用这些类型，不得在其他程序集中重复定义同名契约。
    /// </summary>
    /// <remarks>
    /// 权威文档：
    /// <list type="bullet">
    /// <item><description>docs/接口文档/00-UI与玩法服务层交互总约束.md — ServiceResult、ErrorCode、RandomSeed</description></item>
    /// <item><description>docs/接口文档/02-训练Session数据模型.md — TrainingMode、SessionState、ResultGrade、PlayerPosture、ShoulderSide、AmmoDto</description></item>
    /// <item><description>docs/接口文档/11-Unity场景与Prefab约定.md — UITestId</description></item>
    /// </list>
    /// 目录约定：
    /// <list type="bullet">
    /// <item><description>Runtime/Infrastructure — 通用基础设施</description></item>
    /// <item><description>Runtime/Application — 应用服务（路由、Session 等）</description></item>
    /// <item><description>Runtime/Unity — MonoBehaviour 与场景绑定脚本</description></item>
    /// <item><description>Config — ScriptableObject 与静态配置资源</description></item>
    /// </list>
    /// </remarks>
    public static class P1ContractScope
    {
        public const string TaskId = "task001";
        public const string RuntimeAssembly = "VRShooting.Runtime";
        public const string EditModeTestsAssembly = "VRShooting.Runtime.EditModeTests";
        public const string PlayModeTestsAssembly = "VRShooting.Runtime.PlayModeTests";
    }
}
