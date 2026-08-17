namespace VRShooting.Common
{
    /// <summary>
    /// 移动靶路线阶段。参见 docs/接口文档/05-移动目标服务.md。
    /// </summary>
    public enum TargetMovePhase
    {
        WaitingCountdown = 0,
        MovingRightToLeft,
        LeftEndpointHold,
        MovingLeftToRight,
        Completed
    }
}
