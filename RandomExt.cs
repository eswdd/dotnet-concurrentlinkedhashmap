using System;

namespace ConcurrentLinkedDictionary
{
    public static class RandomExt
    {
        public static int NextInt(this Random random, int maxExclusive)
        {
            return (int)(random.NextDouble () * maxExclusive);
        }
    }
}

