using VRShooting.Contracts;

namespace VRShooting.Common
{
    /// <summary>
    /// 训练 Session 快照。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public readonly struct TrainingSessionDto
    {
        public string SessionId { get; init; }
        public TrainingMode Mode { get; init; }
        public SessionState State { get; init; }
        public string MapId { get; init; }
        public string WeaponId { get; init; }
        public RandomSeed Seed { get; init; }
        public float ElapsedSeconds { get; init; }
        public AmmoDto Ammo { get; init; }
        public PlayerStatusDto Player { get; init; }
        public TrainingPostureMode PostureMode { get; init; }
        public string FiringStationId { get; init; }
        public bool ArtificialLocomotionAllowed { get; init; }
        public SquadStatusDto Squad { get; init; }
        public ResultGrade CurrentGrade { get; init; }
        public string FailureReason { get; init; }
    }
}
