using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ConcurrentLinkedDictionary.Test
{
    public class IsEmptyDictionary<K,V> : IResolveConstraint
    {
        public IsEmptyDictionary ()
        {
        }


        public Constraint Resolve ()
        {
            return new IsEmptyDictionaryConstraint<K,V> ();
        }

    }

    public sealed class IsEmptyDictionaryConstraint<K,V> : Constraint
    {
        private DescriptionBuilder builder = new DescriptionBuilder();

        public override bool Matches (object actual)
        {
            builder.ExpectThat ("not a dictionary", actual, Is.InstanceOf<IDictionary<K,V>> ());
            if (!(actual is IDictionary<K,V>)) 
            {
                return false;
            }
            var map = (IDictionary<K,V>)actual;

            builder.ExpectThat (map.Keys, new IsEmptyCollectionConstraint<K> ());
            builder.ExpectThat (map.Values, new IsEmptyCollectionConstraint<V> ());
            builder.ExpectThat (map, new IsEmptyCollectionConstraint<KeyValuePair<K,V>> ());
            builder.ExpectThat (map.Count, Is.EqualTo (0));

            if (map is ConcurrentLinkedDictionary<K,V>) {
                CheckIsEmpty((ConcurrentLinkedDictionary<K, V>) map);
            }

            return true;
        }

        private void CheckIsEmpty(ConcurrentLinkedDictionary<K, V> map) {
            map.DrainBuffers();

            builder.ExpectThat (map.data.IsEmpty, Is.True);
            builder.ExpectThat (map.data.Count, Is.EqualTo (0));
            builder.ExpectThat (map.WeightedSize (), Is.EqualTo (0));
            builder.ExpectThat (map.weightedSize.GetValue(), Is.EqualTo (0));
            builder.ExpectThat (map.evictionDeque.Peek(), Is.Null);
        }

        public override void WriteDescriptionTo (MessageWriter writer)
        {
            writer.Write ("empty");
            builder.DescribeTo (writer);
        }
    }
}

