using System;

namespace ConcurrentLinkedDictionary
{
	public interface IEntryWeigher<K,V>
	{
		int weightOf(K key, V value);
	}
}

