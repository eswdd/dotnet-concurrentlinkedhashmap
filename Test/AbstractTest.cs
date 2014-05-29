using System;
using ConcurrentLinkedDictionary;
using NUnit.Framework.Constraints;
using System.Collections.Generic;
using NUnit.Framework;
using System.Threading;
using System.Linq;

namespace ConcurrentLinkedDictionary.Test
{
	public abstract class AbstractTest
	{
		long _capacity;

		// todo: moq on mono
		protected DummyEvictionListener<int, int> listener;
		//protected Mock<IEvictionListener<int, int>> listenerMock;

		[SetUp]
		public void Before()
		{
			listener = new DummyEvictionListener<int,int>();
		}

		protected class DummyEvictionListener<K,V> : IEvictionListener<K,V> 
		{
			private List<KeyValuePair<K,V>> _evictions = new List<KeyValuePair<K, V>> ();
			public void onEviction (K key, V value)
			{
				_evictions.Add (new KeyValuePair<K, V> (key, value));
			}

			public List<KeyValuePair<K,V>> Evictions {
				get { return _evictions; }
			}
		}
		protected class FailingEvictionListener<K,V> : IEvictionListener<K,V> 
		{

			public void onEviction (K key, V value)
			{
				throw new InvalidOperationException ();
			}

		}

		protected AbstractTest (TestType tt)
		{
			switch (tt) 
			{
			case TestType.Standard:
				InitClass (100);
				break;
			default:
				throw new ArgumentException (""+tt);
			}
		}

		public void InitClass(long capacity) {
			_capacity = capacity;
			InitMocks();
		}

		void InitMocks ()
		{
			// todo
		}

		/** Retrieves the maximum weighted capacity to build maps with. */
		protected long Capacity() {
			return _capacity;
		}

		protected IDictionary<K,V> immutableCopyOf<K,V>(IDictionary<K,V> source)
		{
			return new ImmutableDictionary<K,V> (source);
		}

		/* ---------------- Map providers -------------- */

		/** Provides a builder with the capacity set. */

		public Object[][] Builder() {
			return new Object[][] {
				new Object[] {new Builder<int, int>().MaximumWeightedCapacity(Capacity())}
			};
		}

		public Object[][] BuilderIntByteArray() {
			return new Object[][] {
				new Object[] {new Builder<int, byte[]>().MaximumWeightedCapacity(Capacity())}
			};
		}

		public Object[][] BuilderIntCollectionOfInt() {
			return new Object[][] {
				new Object[] {new Builder<int, ICollection<int>>().MaximumWeightedCapacity(Capacity())}
			};
		}

		public Object[][] EmptyMap<K,V>() {
			return new Object[][] {
			    new Object[] { newEmptyMap<K,V>() }
			};
		}

		/** Creates a map with the default capacity. */
		protected ConcurrentLinkedDictionary<K, V> newEmptyMap<K,V>() {
			return new Builder<K, V>()
					.MaximumWeightedCapacity(Capacity())
					.Build();
		}

		/** Provides a guarded map for test methods. */
		public object[][] GuardedMap() {
			return new object[][] {
				new object[]{ newGuarded<int,int>() }
			};
		}

		/** Creates a map that fails if an eviction occurs. */
		protected ConcurrentLinkedDictionary<K, V> newGuarded<K,V>() {
			var listener = guardingListener<K,V>();
			return new Builder<K, V>()
					.MaximumWeightedCapacity(Capacity())
					.Listener(listener)
					.Build();
		}

		public Object[][] EmptyWeightedMap<K,V>() {
			return new Object[][] {
				 new Object[] { newEmptyWeightedMap<K,V>() }
			};
		}

		private ConcurrentLinkedDictionary<K, IEnumerable<V>> newEmptyWeightedMap<K,V>() {
			return new Builder<K, IEnumerable<V>>()
					.MaximumWeightedCapacity(Capacity())
					.Weigher(Weighers.Enumerable<V>())
					.Build();
		}

		public Object[][] GuardedWeightedMap<K,V>() {
			return new Object[][] {
				new Object[] { newGuardedWeightedMap<K,V>() }
			};
		}

		private ConcurrentLinkedDictionary<K, IEnumerable<V>> newGuardedWeightedMap<K, V>() {
			var listener = guardingListener<K,IEnumerable<V>>();
			return new Builder<K, IEnumerable<V>>()
					.MaximumWeightedCapacity(Capacity())
					.Weigher(Weighers.Enumerable<V>())
					.Listener(listener)
					.Build();
		}

		private IEvictionListener<K, V> guardingListener<K, V>() {
			// todo: for now have got a concrete impl until can get moq working in mono
			/*
			var guardingListener = (IEvictionListener<K, V>) listener;
			doThrow(new AssertionError()).when(guardingListener)
				.onEviction(Mockito.<K>any(), Mockito.<V>any());
			return guardingListener;
			*/
			return new GuardingListener<K, V> ();
		}

		private class GuardingListener<K,V> : IEvictionListener<K,V>
		{
			public void onEviction (K key, V value)
			{
				throw new AssertionException ("onEviction called on guarded listener");
			}
		}

		/** Creates a map with warmed to capacity. */
		protected ConcurrentLinkedDictionary<int,int> newWarmedMap() {
			var map = newEmptyMap<int,int>();
			WarmUp(map, 1, Capacity());
			return map;
		}

		public Object[][] WarmedMap() {
			return new Object[][] {
				new Object[] { newWarmedMap() }
			};
		}


		/* ---------------- Utility methods ------------- */

		/**
		* Populates the map with the half-closed interval [start, start+count) where the
			* value is the negation of the key.
			*/
			protected static void WarmUp(IDictionary<int, int> map, int start, long count) {
				for (var i = start; i < start+count; i++) {
					Assert.That (map.ContainsKey(i), Is.False);
					map [i] = -i;
				}
			}

			/* ------------ Constraint providers ------------ */
			protected IResolveConstraint emptyCollection<T>()
			{
				return new IsEmptyCollection<T> ();
			}
			protected IResolveConstraint emptyMap<K,V>()
			{
				return new IsEmptyDictionary<K,V> ();
			}

			protected IResolveConstraint validConcurrentLinkedDictionary<K,V>()
		{
			return new IsValidConcurrentLinkedDictionary<K,V> ();
		}

			protected Constraint HasCount(int count)
			{
				return CollectionConstraints.HasCount (count);
			}

		protected enum TestType
		{
			Standard
		}


			protected void waitUntil(Func<bool> func)
			{
				while (!func ()) {
					Thread.Sleep (10);
				}
			}

			protected IList<T> asList<T> (params T[] ts)
			{
				return ts.ToList ();
			}

			protected IDictionary<K,V> singletonMap<K,V> (K k, V v)
			{
				var ret = new Dictionary<K,V> ();
				ret.Add (k, v);
				return ret;
			}

			protected IDictionary<K, V> newLinkedHashMap<K,V> ()
			{
				return new SortedDictionary<K, V>();
			}
	}
}

