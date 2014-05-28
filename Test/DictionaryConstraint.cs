using System;
using NUnit.Framework.Constraints;
using System.Collections.Generic;

namespace ConcurrentLinkedDictionary.Test
{
	public abstract class DictionaryConstraint
	{
		public static Constraint HasKey<K,V>(K key)
		{
			return new HasKeyConstraint<K,V> (key);
		}

		public static Constraint HasValue<K,V>(V value)
		{
			return new HasValueConstraint<K,V>(value);
		}
		public static Constraint HasEntry<K,V>(K key, V value)
		{
			return new HasEntryConstraint<K,V>(new KeyValuePair<K, V>(key,value));
		}


	}
	internal class HasKeyConstraint<K,V> : Constraint
	{
		private K _lookingFor;

		internal HasKeyConstraint(K lookingFor)
		{
			_lookingFor = lookingFor;
		}

		public override bool Matches (object actual)
		{
			var dict = actual as IDictionary<K,V>;
			if (dict == null) {
				return false;
			}
			return dict.ContainsKey (_lookingFor);
		}
		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.WritePredicate ("contains");
			writer.WriteValue (_lookingFor);
		}
	}
	internal class HasValueConstraint<K,V> : Constraint
	{
		private V _lookingFor;

		internal HasValueConstraint(V lookingFor)
		{
			_lookingFor = lookingFor;
		}

		public override bool Matches (object actual)
		{
			var dict = actual as IDictionary<K,V>;
			if (dict == null) {
				return false;
			}
			return dict.Values.Contains (_lookingFor);
		}
		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.WritePredicate ("contains");
			writer.WriteValue (_lookingFor);
		}
	}
	internal class HasEntryConstraint<K,V> : Constraint
	{
		private KeyValuePair<K,V> _lookingFor;

		internal HasEntryConstraint(KeyValuePair<K,V> lookingFor)
		{
			_lookingFor = lookingFor;
		}

		public override bool Matches (object actual)
		{
			var dict = actual as IDictionary<K,V>;
			if (dict == null) {
				return false;
			}
			return dict.Contains (_lookingFor);
		}
		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.WritePredicate ("contains");
			writer.WriteValue (_lookingFor);
		}
	}


}

