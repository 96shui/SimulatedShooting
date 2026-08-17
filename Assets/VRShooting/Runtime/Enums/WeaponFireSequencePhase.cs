namespace VRShooting.Common
{
    /// <summary>
    /// 射击序列阶段。参见 docs/接口文档/06-武器与弹药服务.md。
    /// </summary>
    public enum WeaponFireSequencePhase
    {
        Idle = 0,
        InitialTwoShots,
        ContinuousFire,
        Stopped
    }
}
