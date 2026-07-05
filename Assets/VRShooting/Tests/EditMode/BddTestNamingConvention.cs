namespace VRShooting.Tests
{
    /// <summary>
    /// BDD 测试命名约定（task001）。
    /// </summary>
    /// <remarks>
    /// 测试类与方法命名应能追溯到 BDD feature 与场景，推荐格式：
    /// <list type="bullet">
    /// <item><description>类名：<c>{FeatureId}_{Area}Tests</c>，例如 <c>Screen05_ZeroingHudTests</c></description></item>
    /// <item><description>方法名：<c>{FeatureId}_{ScenarioSlug}_{ExpectedBehavior}</c></description></item>
    /// </list>
    /// FeatureId 对应 docs/BDD/screens 文件名前缀，例如：
    /// <list type="bullet">
    /// <item><description>05-100m射击HUD.feature.md → FeatureId = Screen05</description></item>
    /// <item><description>07-射校最终评级.feature.md → FeatureId = Screen07</description></item>
    /// </list>
    /// 基础设施契约测试使用 FeatureId = Infrastructure，并引用 docs/接口文档 章节。
    /// </remarks>
    public static class BddTestNamingConvention
    {
        public const string InfrastructureFeatureId = "Infrastructure";
        public const string InterfaceDoc00 = "docs/接口文档/00-UI与玩法服务层交互总约束.md";
        public const string InterfaceDoc02 = "docs/接口文档/02-训练Session数据模型.md";
        public const string InterfaceDoc11 = "docs/接口文档/11-Unity场景与Prefab约定.md";
    }
}
