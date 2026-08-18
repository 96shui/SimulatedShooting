namespace VRShooting.Common
{
    /// <summary>
    /// 连射调度配置。P2 起射发数固定为 2，间隔由应用配置注入。
    /// </summary>
    public readonly struct WeaponAutoFireConfigDto
    {
        public int InitialShotCount { get; init; }
        public float ShotIntervalSeconds { get; init; }

        public static WeaponAutoFireConfigDto P2Default => new WeaponAutoFireConfigDto
        {
            InitialShotCount = 2,
            ShotIntervalSeconds = 0.1f
        };

        public static WeaponAutoFireConfigDto P1SingleShot => new WeaponAutoFireConfigDto
        {
            InitialShotCount = 1,
            ShotIntervalSeconds = 0.1f
        };
    }
}
