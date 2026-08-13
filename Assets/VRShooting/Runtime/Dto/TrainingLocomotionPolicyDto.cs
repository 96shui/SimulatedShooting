namespace VRShooting.Common
{
    /// <summary>
    /// P1/P2 移动策略。玩法层只输出策略，不访问 XR 组件。
    /// 参见 docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md。
    /// </summary>
    public readonly struct TrainingLocomotionPolicyDto
    {
        public TrainingMode Mode { get; init; }
        public TrainingPostureMode Posture { get; init; }
        public bool AllowContinuousMove { get; init; }
        public bool AllowTeleport { get; init; }
        public bool AllowArtificialTurn { get; init; }
        public bool AllowRoomScaleHeadAndHandTracking { get; init; }
    }
}
