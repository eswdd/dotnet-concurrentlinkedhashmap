using System;

namespace ConcurrentLinkedDictionary
{
    public static class ArrayExt
    {
        public static void SetAll<T>(this T[] array, T value)
        {
            for (var i = 0; i<array.Length; i++)
            {
                array [i] = value;
            }
        }

        public static void Swap<T>(this T[] array, int i1, int i2)
        {
            var tmp = array [i1];
            array [i1] = array [i2];
            array [i2] = tmp;
        }
    }
}

