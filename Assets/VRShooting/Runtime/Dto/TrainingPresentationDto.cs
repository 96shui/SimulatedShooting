namespace VRShooting.Common
{
    /// <summary>
    /// P1/P2 展示快照。UI 只渲染本 DTO，不自行组合显隐布尔值。
    /// 参见 docs/接口文档/13-P1P2卧姿射击与界面显隐契约.md。
    /// </summary>
    public readonly struct TrainingPresentationDto
    {
        public string SessionId { get; init; }
        public TrainingMode Mode { get; init; }
        public TrainingPresentationPhase Phase { get; init; }
        public TrainingPostureMode Posture { get; init; }
        public ScreenId ActiveScreen { get; init; }
        public bool LargePanelVisible { get; init; }
        public bool MinimalHudVisible { get; init; }
        public bool ShootingAllowed { get; init; }
        public bool ArtificialLocomotionAllowed { get; init; }
        public bool AwaitingWeaponPickup { get; init; }
        public string FiringStationId { get; init; }
        public string VisibilityReason { get; init; }

        public static TrainingPresentationDto Empty => new TrainingPresentationDto
        {
            SessionId = string.Empty,
            FiringStationId = string.Empty,
            VisibilityReason = string.Empty
        };
    }
}
