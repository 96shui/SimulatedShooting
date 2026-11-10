using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace VRShooting.Contracts
{
    /// <summary>
    /// 可注入的随机种子，保证测试可重复。参见 docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    public readonly struct RandomSeed
    {
        static readonly object RandomLock = new object();
        static readonly HashSet<int> IssuedSeeds = new HashSet<int>();

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

        /// <summary>Generates a non-repeating runtime seed for this process.</summary>
        public static RandomSeed CreateRandom()
        {
            lock (RandomLock)
            {
                using (var random = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[4];
                    int value;
                    do
                    {
                        random.GetBytes(bytes);
                        value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
                    } while (!IssuedSeeds.Add(value));
                    return Unfixed(value);
                }
            }
        }
    }
}
