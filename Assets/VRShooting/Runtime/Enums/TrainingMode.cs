namespace VRShooting.Common
{
    /// <summary>
    /// 训练模式。参见 docs/接口文档/02-训练Session数据模型.md。
    /// </summary>
    public enum TrainingMode
    {
        None = 0,
        Zeroing100m,
        MovingTarget,
        Trench,
        Urban
    }
}
