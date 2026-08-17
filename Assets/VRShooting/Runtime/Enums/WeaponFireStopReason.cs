namespace VRShooting.Common
{
    /// <summary>
    /// 射击序列停止原因。参见 docs/接口文档/06-武器与弹药服务.md。
    /// </summary>
    public enum WeaponFireStopReason
    {
        TriggerReleased = 0,
        AmmoDepleted,
        ShootingBecameForbidden,
        TrainingCompleted,
        WeaponBecameInvalid
    }
}
