namespace VRShooting.Common
{
    /// <summary>
    /// 训练 Session 生命周期状态。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public enum SessionState
    {
        NotStarted = 0,
        Preparing,
        Countdown,
        Running,
        Paused,
        Analysis,
        Completed,
        Failed,
        Cancelled
    }
}
