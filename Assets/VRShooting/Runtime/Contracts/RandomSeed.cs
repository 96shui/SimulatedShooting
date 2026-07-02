namespace VRShooting.Contracts
{
    /// <summary>
    /// 可注入的随机种子，保证测试可重复。参见 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    public readonly struct RandomSeed
    {
        public int Value { get; init; }
        public bool IsFixed { get; init; }

        public static RandomSeed Fixed(int value)
        {
            return new RandomSeed { Value = value, IsFixed = true };
        }

        public static RandomSeed Unfixed(int value = 0)
        {
            return new RandomSeed { Value = value, IsFixed = false };
        }
    }
}
