namespace VRShooting.Common
{
    /// <summary>
    /// 玩家状态 DTO。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public readonly struct PlayerStatusDto
    {
        public float Health { get; init; }
        public bool IsAlive { get; init; }
        public PlayerPosture Posture { get; init; }
        public ShoulderSide Shoulder { get; init; }
        public bool CornerShootingAvailable { get; init; }

        public static PlayerStatusDto Default => new PlayerStatusDto
        {
            Health = 100f,
            IsAlive = true,
            Posture = PlayerPosture.Standing,
            Shoulder = ShoulderSide.Right,
            CornerShootingAvailable = false
        };
    }
}
