namespace VRShooting.Common
{
    /// <summary>
    /// UI 层上报的命令事件。参见 docs/接口文档/01-页面导航与UI事件.md。
    /// </summary>
    public readonly struct UICommand
    {
        public UIEventId EventId { get; init; }
        public ScreenId SourceScreen { get; init; }
        public string PayloadJson { get; init; }
        public double ClientTime { get; init; }
    }
}
