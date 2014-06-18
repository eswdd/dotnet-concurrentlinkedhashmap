using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcurrentLinkedDictionary
{
    public static class CollectionExt
    {

        public static void Shuffle<T>(this IList<T> list) {
            list.Shuffle (new Random ());
        }
        public static void Shuffle<T>(this IList<T> list, Random rnd) {
            int size = list.Count;
//            if (size < SHUFFLE_THRESHOLD || list instanceof RandomAccess) {
//                for (int i=size; i>1; i--)
//                    swap(list, i-1, rnd.nextInt(i));
//            } else {

            var arr = list.ToArray();

                // Shuffle array
                for (int i=size; i>1; i--)
                arr.Swap(i-1, rnd.NextInt(i));

                // Dump array back into list
            for (var i = 0; i < size; i++) {
                list [i] = arr [i];
            }


//            }
        }
    }
}

