using System;
using System.Collections.Generic;
using System.Collections;

namespace ConcurrentLinkedDictionary
{
	public sealed class Weighers
	{
		private Weighers ()
		{
		}

		private static IWeigher<byte[]> byteArrayWeigher = new ByteArrayWeigher();

		public static IEntryWeigher<K, V> AsEntryWeigher<K,V>(IWeigher<V> weigher) {
			// todo: old code (weigher == singleton()) ? Weighers.entrySingleton<K, V>() : 
			return new EntryWeigherView<K, V>(weigher);
		}


		public static IEntryWeigher<K, V> EntrySingleton<K,V>() {
			// todo: old code used to have a true singleton here
			return new SingletonEntryWeigher<K,V>();
		}
		/*
		public static IWeigher<V> singleton() {
			return (Weigher<V>) SingletonWeigher.INSTANCE;
		}*/

		public static IWeigher<byte[]> ByteArray() {
			return byteArrayWeigher;
		}

		public static IWeigher<IEnumerable> Enumerable() {
			return new EnumerableWeigher ();
		}

		public static IWeigher<ICollection> Collection() {
			return new CollectionWeigher ();
		}

		public static IWeigher<IList> List() {
			return new ListWeigher ();
		}

		public static IWeigher<IDictionary> Dictionary() {
			return new DictionaryWeigher ();
		}


	}
	sealed class EntryWeigherView<K, V> : IEntryWeigher<K, V> {
		readonly IWeigher<V> weigher;

		internal EntryWeigherView(IWeigher<V> weigher) {
			if (weigher == null) {
				throw new ArgumentNullException();
			}
			this.weigher = weigher;
		}

		public int weightOf(K key, V value) {
			return weigher.weightOf(value);
		}
	}

	sealed class SingletonEntryWeigher<K,V> : IEntryWeigher<K, V> {

		public int weightOf(K key, V value) {
			return 1;
		}
	}

	sealed class SingletonWeigher : IWeigher<Object> {


		public int weightOf(Object value) {
			return 1;
		}
	}

	sealed class ByteArrayWeigher : IWeigher<byte[]> {


		public int weightOf(byte[] value) {
			return value.Length;
		}
	}

	sealed class EnumerableWeigher : IWeigher<IEnumerable> {

		public int weightOf(IEnumerable values) {
			if (values is ICollection) {
				return ((ICollection) values).Count;
			}
			int size = 0;
			IEnumerator enumerator = values.GetEnumerator ();

			while (enumerator.Current != null) {
				size++;
				enumerator.MoveNext();
			}

			return size;
		}
	}

	sealed class CollectionWeigher : IWeigher<ICollection> {


		public int weightOf(ICollection values) {
			return values.Count;
		}
	}

	sealed class ListWeigher : IWeigher<IList> {


		public int weightOf(IList values) {
			return values.Count;
		}
	}

	sealed class DictionaryWeigher : IWeigher<IDictionary> {


		public int weightOf(IDictionary values) {
			return values.Count;
		}
	}
}

