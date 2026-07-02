namespace VRShooting.Common
{
    /// <summary>
    /// Session 结束原因。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public enum SessionEndReason
    {
        Completed = 0,
        PlayerDead,
        OutOfAmmo,
        Cancelled,
        SystemError
    }
}
