using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcurrentLinkedDictionary.Test
{
    /**
 * A set of utilities for writing benchmarks.
 *
 * @author ben.manes@gmail.com (Ben Manes)
 */
    public sealed class Benchmarks
    {
        private Benchmarks() {}

        /**
   * Creates a random working set based on the distribution.
   *
   * @param generator the distribution generator
   * @param size the size of the working set
   * @return a random working set
   *
        public static List<string> createWorkingSet(Generator generator, int size) {
            List<string> workingSet = newArrayListWithCapacity<string>(size);
            for (int i = 0; i < size; i++) {
                workingSet.add(generator.nextString());
            }
            return workingSet;
        }

        /**
   * Creates a random working set based on the distribution.
   *
   * @param generator the distribution generator
   * @param size the size of the working set
   * @return a random working set
   *
        public static List<Integer> createWorkingSet(IntegerGenerator generator, int size) {
            Integer[] ints = new Integer[size];
            for (int i = 0; i < ints.length; i++) {
                ints[i] = generator.nextInt();
            }
            return Arrays.asList(ints);
        }

        /**
   * Based on the passed in working set, creates N shuffled variants.
   *
   * @param samples the number of variants to create
   * @param workingSet the base working set to build from
   */
        public static List<List<T>> shuffle<T>(int samples, ICollection<T> workingSet) {
            List<List<T>> sets = new List<List<T>> ();
            for (var i = 0; i < samples; i++) {
                List<T> set = workingSet.ToList ();
                set.Shuffle ();
                sets.Add(set);
            }
            return sets;
        }

        /**
   * Determines the hit/miss rate of a cache.
   *
   * @param cache the self-evicting map
   * @param workingSet the request working set
   * @return the efficiency of the execution run
   */
        public static EfficiencyRun determineEfficiency<T>(IDictionary<T, T> cache, List<T> workingSet) {
            int hits = 0;
            foreach (T key in workingSet) {
                if (Equals(cache[key], default(T))) {
                    cache[key] = key;
                } else {
                    hits++;
                }
            }
            return EfficiencyRun.of(hits, workingSet.Count);
        }

        public sealed class EfficiencyRun {
            public readonly int hitCount;
            public readonly int missCount;
            public readonly double hitRate;
            public readonly double missRate;
            public readonly int workingSetSize;

            private EfficiencyRun(int hitCount, int workingSetSize) {
                this.workingSetSize = workingSetSize;
                this.hitCount = hitCount;
                this.missCount = workingSetSize - hitCount;
                this.hitRate = ((double) hitCount) / workingSetSize;
                this.missRate = ((double) missCount) / workingSetSize;
            }

            public static EfficiencyRun of(int hitCount, int workingSetSize) {
                return new EfficiencyRun(hitCount, workingSetSize);
            }

            public override String ToString() {
                return String.Format ("hits={0:#,###} ({1:0.00} percent), misses={2:#,###} ({3:0.00} percent)",
                    hitCount, hitRate, missCount, missRate);
            }
        }
    }
}

