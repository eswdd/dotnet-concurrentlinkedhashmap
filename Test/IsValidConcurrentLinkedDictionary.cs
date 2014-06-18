using System;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;

namespace ConcurrentLinkedDictionary.Test
{
    public class IsValidConcurrentLinkedDictionary<K,V> : IResolveConstraint
    {
        public IsValidConcurrentLinkedDictionary ()
        {
        }

        public Constraint Resolve ()
        {
            return new IsValidConcurrentLinkedDictionaryConstraint<K,V> ();
        }
    }

    public sealed class IsValidConcurrentLinkedDictionaryConstraint<K,V> : Constraint
    {
        private DescriptionBuilder builder = new DescriptionBuilder();

        public override bool Matches (object actual)
        {
            builder.ExpectThat("map not a CLD", actual, Is.InstanceOf<ConcurrentLinkedDictionary<K,V>>());
            if (!builder.Matches) {
                return false;
            }

            matchesSafely((ConcurrentLinkedDictionary<K,V>)actual);
            return builder.Matches;
        }

        private void matchesSafely(ConcurrentLinkedDictionary<K, V> map) {

            drain(map);
            checkMap(map);
            checkEvictionDeque(map);
        }

        private void drain(ConcurrentLinkedDictionary<K, V> map) {
            for (int i = 0; i < map.readBuffers.Length; i++) {
                for (;;) {
                    map.DrainBuffers();

                    bool fullyDrained = map.writeBuffer.IsEmpty;
                    for (int j = 0; j < map.readBuffers.Length; j++) {
                        fullyDrained &= (map.readBuffers[i][j].GetValue() == null);
                    }
                    if (fullyDrained) {
                        break;
                    }
                    map.readBufferReadCount[i]++;
                }
            }
        }

        private void checkMap(ConcurrentLinkedDictionary<K, V> map) {
            builder.ExpectThat("listenerQueue", map.pendingNotifications.IsEmpty, Is.True);
            builder.ExpectThat("Inconsistent size", map.data.Count, Is.EqualTo(map.Count));
            builder.ExpectThat("weightedSize", map.WeightedSize(), Is.EqualTo(map.weightedSize.GetValue()));
            builder.ExpectThat("capacity", map.Capacity(), Is.EqualTo(map.capacity.GetValue()));
            builder.ExpectThat("overflow", map.capacity.GetValue(),
                Is.GreaterThanOrEqualTo(map.WeightedSize()));
            // todo: how do you do this for an implicit lock?
            //builder.ExpectThat(((ReentrantLock) map.evictionLock).isLocked(), Is.False);


            if (map.Count == 0) {
                builder.ExpectThat("map not empty", map, new IsEmptyDictionary<K,V>().Resolve());
            }
        }

        private void checkEvictionDeque(ConcurrentLinkedDictionary<K, V> map) {
            var deque = map.evictionDeque;

            checkLinks(map);
            builder.ExpectThat("dequeue count incorrect", deque.Count, Is.EqualTo(map.Count));
            // todo: need to implement validLinkedDequeue!!
            //validLinkedDeque().matchesSafely(map.evictionDeque, builder.getDescription());
        }

        private void checkLinks(ConcurrentLinkedDictionary<K, V> map) {
            long weightedSize = 0;
            var seen = new HashSet<ConcurrentLinkedDictionary<K, V>.Node>();
            foreach (var n in map.evictionDeque) {
                var node = (ConcurrentLinkedDictionary<K, V>.Node) n;
                String errorMsg = String.Format("Loop detected: {0}, saw {1} in {2}", node, seen, map);
                builder.ExpectThat (errorMsg, seen.Contains (node), Is.False);
                seen.Add(node);
                weightedSize += ((ConcurrentLinkedDictionary<K, V>.WeightedValue) node.GetValue()).weight;
                checkNode(map, node);
            }

            builder.ExpectThat("Size != list length", map.Count, Is.EqualTo(seen.Count));
            builder.ExpectThat("WeightedSize != link weights"
                + " [" + map.WeightedSize() + " vs. " + weightedSize + "]"
                + " {size: " + map.Count + " vs. " + seen.Count + "}",
                map.WeightedSize(), Is.EqualTo(weightedSize));
        }

        private void checkNode(ConcurrentLinkedDictionary<K, V> map, ConcurrentLinkedDictionary<K, V>.Node node) {
            //if (Equals (node.Key, default(K))) {
            //    builder.ExpectThat (true, Is.False);
            //}
            builder.ExpectThat("key is null or default", node.Key, Is.Not.Null & Is.Not.EqualTo(default(K)));
            builder.ExpectThat("node.GetValue() is null or default", node.GetValue(), Is.Not.Null & Is.Not.EqualTo(default(V)));
            builder.ExpectThat("node.Value is null or default", node.Value, Is.Not.Null & Is.Not.EqualTo(default(V)));
            builder.ExpectThat("weight", ((ConcurrentLinkedDictionary<K, V>.WeightedValue) node.GetValue()).weight,
                Is.EqualTo(((IEntryWeigher<K,V>) map.weigher).weightOf(node.Key, node.Value)));

            builder.ExpectThat("inconsistent", map, DictionaryConstraint.HasKey<K,V>(node.Key));
            builder.ExpectThat("Could not find value: " + node.Value, map, DictionaryConstraint.HasValue<K,V>(node.Value));
            builder.ExpectThat("found wrong node", map.data, DictionaryConstraint.HasEntry<K,ConcurrentLinkedDictionary<K, V>.Node>(node.Key, node));
        }

        public override void WriteDescriptionTo (MessageWriter writer)
        {
            writer.WriteExpectedValue ("valid");
            builder.DescribeTo (writer);
        }
    }
}

