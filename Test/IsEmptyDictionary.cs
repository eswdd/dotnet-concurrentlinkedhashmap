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
		public override bool Matches (object actual)
		{
			if (!(actual is IDictionary<K,V>)) 
			{
				return false;
			}
			var map = (IDictionary<K,V>)actual;

			if (!new IsEmptyCollectionConstraint<K>().Matches (map.Keys))
				return false;
			if (!new IsEmptyCollectionConstraint<V>().Matches (map.Values))
				return false;
			if (!new IsEmptyCollectionConstraint<KeyValuePair<K,V>>().Matches (map))
				return false;
			if (map.Count != 0)
				return false;

			if (map is ConcurrentLinkedDictionary<K,V>) {
				CheckIsEmpty((ConcurrentLinkedDictionary<K, V>) map);
			}

			return true;
		}

		private bool CheckIsEmpty(ConcurrentLinkedDictionary<K, V> map) {
			map.DrainBuffers();

			if (!map.data.IsEmpty)
				return false;
			if (map.data.Count != 0)
				return false;
			if (map.WeightedSize() != 0)
				return false;
			if (map.weightedSize.GetValue () != 0)
				return false;
			if (map.evictionDeque.Peek () != null)
				return false;

			return true;
		}

		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.Write ("empty");
		}
	}
}

