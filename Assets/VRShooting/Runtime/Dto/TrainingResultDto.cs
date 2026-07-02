namespace VRShooting.Common
{
    /// <summary>
    /// 训练结算 DTO。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public readonly struct TrainingResultDto
    {
        public string SessionId { get; init; }
        public TrainingMode Mode { get; init; }
        public bool Victory { get; init; }
        public ResultGrade Grade { get; init; }
        public float ElapsedSeconds { get; init; }
        public int RemainingAmmo { get; init; }
        public string SummaryJson { get; init; }
    }
}
