using System;
using System.Collections.Generic;

namespace ConcurrentLinkedDictionary.Test
{
	public class ImmutableDictionary<K,V> : IDictionary<K,V>
	{

		private IDictionary<K,V> _dict = new Dictionary<K, V> ();

		public ImmutableDictionary (IDictionary<K,V> source)
		{
			_dict = new Dictionary<K,V> ();
			foreach (var kv in source) {
				_dict.Add (kv.Key, kv.Value);
			}
		}

		#region IDictionary implementation

		public void Add (K key, V value)
		{
			throw new NotSupportedException ();
		}

		public bool ContainsKey (K key)
		{
			return _dict.ContainsKey (key);
		}

		public bool Remove (K key)
		{
			throw new NotSupportedException ();
		}

		public bool TryGetValue (K key, out V value)
		{
			return _dict.TryGetValue (key, out value);
		}

		public V this [K index] {
			get {
				return _dict [index];
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public ICollection<K> Keys {
			get {
				return _dict.Keys;
			}
		}

		public ICollection<V> Values {
			get {
				return _dict.Values;
			}
		}

		#endregion

		#region ICollection implementation

		public void Add (KeyValuePair<K, V> item)
		{
			throw new NotSupportedException ();
		}

		public void Clear ()
		{
			throw new NotSupportedException ();
		}

		public bool Contains (KeyValuePair<K, V> item)
		{
			return _dict.Contains (item);
		}

		public void CopyTo (KeyValuePair<K, V>[] array, int arrayIndex)
		{
			_dict.CopyTo (array, arrayIndex);
		}

		public bool Remove (KeyValuePair<K, V> item)
		{
			throw new NotSupportedException ();
		}

		public int Count {
			get {
				return _dict.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return true;
			}
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator ()
		{
			return _dict.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return _dict.GetEnumerator ();
		}

		#endregion
	}
}

