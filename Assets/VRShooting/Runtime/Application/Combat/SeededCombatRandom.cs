using System;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    public sealed class SeededCombatRandom : ICombatRandom
    {
        uint state = 0x6d2b79f5;
        public void Reset(RandomSeed seed) { state = unchecked((uint)seed.Value); if(state==0) state=0x6d2b79f5; }
        public int NextInt(int minInclusive,int maxExclusive)
        {
            if(minInclusive>=maxExclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (int)(minInclusive+(long)(state%(ulong)((long)maxExclusive-minInclusive)));
        }
    }
}
