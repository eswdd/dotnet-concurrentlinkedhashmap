using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections.Generic;

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
		public override bool Matches (object actual)
		{
			if (!(actual is IDictionary<K,V>)) 
			{
				return false;
			}
			var map = (IDictionary<K,V>)actual;

			if (!new IsEmptyCollectionConstraint<K>.Matches (map.Keys))
				return false;
			if (!new IsEmptyCollectionConstraint<V>.Matches (map.Values))
				return false;
			if (!new IsEmptyCollectionConstraint<KeyValuePair<K,V>>.Matches (map))
				return false;
			if (map.Count != 0)
				return false;

			if (map is ConcurrentLinkedDictionary<K,V>) {
				CheckIsEmpty((ConcurrentLinkedDictionary<K, V>) map);
			}

			return true;
		}

		private bool CheckIsEmpty(ConcurrentLinkedDictionary<K, V> map) {
			map.drainBuffers();

			builder.expectThat("Internal not empty", map.data.isEmpty(), is(true));
			builder.expectThat("Internal size != 0", map.data.size(), is(0));
			builder.expectThat("Weighted size != 0", map.weightedSize(), is(0L));
			builder.expectThat("Internal weighted size != 0", map.weightedSize.get(), is(0L));
			builder.expectThat("first not null: " + map.evictionDeque,
				map.evictionDeque.peekFirst(), is(nullValue()));
			builder.expectThat("last not null", map.evictionDeque.peekLast(), is(nullValue()));
		}

		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.Write ("empty");
		}
	}
}

