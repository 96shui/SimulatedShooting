namespace VRShooting.Contracts
{
    /// <summary>
    /// 通用服务错误码。参见 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    public enum ErrorCode
    {
        None = 0,
        InvalidState,
        InvalidInput,
        NotFound,
        Busy,
        Cooldown,
        ResourceUnavailable,
        PersistenceFailed,
        TestOnlyFailure
    }
}
