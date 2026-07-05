namespace VRShooting.Common
{
    /// <summary>
    /// 页面路由参数。参见 docs/接口文档/01-页面导航与UI事件.md。
    /// </summary>
    public readonly struct NavigationArgs
    {
        public TrainingMode? Mode { get; init; }
        public string SessionId { get; init; }
        public string ReturnToScreen { get; init; }
    }
}
