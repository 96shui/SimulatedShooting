namespace VRShooting.Common
{
    /// <summary>
    /// 当前射击序列状态。参见 docs/接口文档/06-武器与弹药服务.md。
    /// </summary>
    public readonly struct WeaponFireSequenceStateDto
    {
        public string SessionId { get; init; }
        public string SequenceId { get; init; }
        public WeaponFireMode FireMode { get; init; }
        public WeaponFireSequencePhase Phase { get; init; }
        public int ShotsFired { get; init; }
        public bool TriggerHeld { get; init; }
        public bool TriggerArmedForNewSequence { get; init; }
        public WeaponFireStopReason? StopReason { get; init; }

        public static WeaponFireSequenceStateDto Empty => new WeaponFireSequenceStateDto
        {
            SessionId = string.Empty,
            SequenceId = string.Empty,
            FireMode = WeaponFireMode.InitialTwoThenAutomatic,
            Phase = WeaponFireSequencePhase.Idle
        };
    }
}
