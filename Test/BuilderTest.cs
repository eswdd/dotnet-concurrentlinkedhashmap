using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using ConcurrentLinkedDictionary;

namespace ConcurrentLinkedDictionary.Test
{
	/// <summary>
	/// A unit-test for the builder methods.
	/// Original author: bmanes@google.com (Ben Manes)
	/// Ported by: Simon Matic Langford
	/// </summary>
	[TestFixture]
	[Category("development")]
	public class BuilderTest : AbstractTest
	{

		public BuilderTest() : base(TestType.Standard)
		{
		}

		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void unconfigured() {
			new Builder<int,int>().Build();
		}

	
		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void initialCapacity_withNegative(Builder<int,int> builder) {
			builder.InitialCapacity(-100);
		}


		[Test]
		[TestCaseSource("Builder")]
		public void initialCapacity_withDefault(Builder<int,int> builder) {
			Assert.That (builder.initialCapacity, Is.EqualTo (ConcurrentLinkedDictionary.Builder<Object,Object>.DEFAULT_INITIAL_CAPACITY));
			builder.Build(); // can't check, so just assert that it builds
		}

		[Test]
		[TestCaseSource("Builder")]
		public void initialCapacity_withCustom(Builder<int, int> builder) {
			Assert.That(builder.InitialCapacity(100).initialCapacity, Is.EqualTo(100));
			builder.Build(); // can't check, so just assert that it builds
		}


		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void maximumWeightedCapacity_withNegative(Builder<int,int> builder) {
			builder.MaximumWeightedCapacity(-100);
		}

		[Test]
		[TestCaseSource("Builder")]
		public void maximumWeightedCapacity(Builder<int,int> builder) {
			Assert.That(builder.Build().Capacity(), Is.EqualTo(Capacity()));
		}

		[Test]
		[TestCaseSource("Builder")]
		public void maximumWeightedCapacity_aboveMaximum(Builder<int,int> builder) {
			builder.MaximumWeightedCapacity(ConcurrentLinkedDictionary<Object, Object>.MAXIMUM_CAPACITY + 1);
			Assert.That(builder.Build().Capacity(), Is.EqualTo(ConcurrentLinkedDictionary<Object, Object>.MAXIMUM_CAPACITY));
		}

		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void concurrencyLevel_withZero(Builder<int,int> builder) {
			builder.ConcurrencyLevel(0);
		}

		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void concurrencyLevel_withNegative(Builder<int,int> builder) {
			builder.ConcurrencyLevel(-100);
		}

		[Test]
		[TestCaseSource("Builder")]
		public void concurrencyLevel_withDefault(Builder<int,int> builder) {
			Assert.That(builder.Build().concurrencyLevel, Is.EqualTo(ConcurrentLinkedDictionary.Builder<int,int>.DEFAULT_CONCURRENCY_LEVEL));
		}

		[Test]
		[TestCaseSource("Builder")]
		public void concurrencyLevel_withCustom(Builder<int,int> builder) {
			Assert.That(builder.ConcurrencyLevel(32).Build().concurrencyLevel, Is.EqualTo(32));
		}

		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void listener_withNull(Builder<int,int> builder) {
			builder.Listener(null);
		}


		[Test]
		[TestCaseSource("Builder")]
		public void listener_withDefault(Builder<int,int> builder) {
			Assert.That(builder.Build().listener, Is.InstanceOf<DiscardingListener<int,int>>());
		}

		[Test] // todo: need to get moq integrated
		public void listener_withCustom() {
			Builder<int, int> builder = new Builder<int, int>()
				.MaximumWeightedCapacity(Capacity())
				.Listener(listener);
			Assert.That(builder.Build().listener, Is.SameAs(listener));
		}

		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void weigher_withNull(Builder<int,int> builder) {
			builder.Weigher((IWeigher<int>)null);
		}

		[Test]
		[TestCaseSource("Builder")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void weigher_withNull_entry(Builder<int,int> builder) {
			builder.Weigher((IEntryWeigher<int, int>) null);
		}
			
		[Test]
		[TestCaseSource("Builder")]
		public void weigher_withDefault(Builder<int, int> builder) {
			Assert.That(builder.Build().weigher, Is.InstanceOf<SingletonEntryWeigher<int,int>>());
		}

		[Test]
		[TestCaseSource("BuilderIntByteArray")]
		public void weigher_withCustom(Builder<int, byte[]> builder) {
			builder.Weigher(Weighers.ByteArray());
			IEntryWeigher<int, byte[]> weigher = ((BoundedEntryWeigher<int, byte[]>) builder.Build().weigher).weigher;
			IWeigher<byte[]> customWeigher = ((EntryWeigherView<int, byte[]>) weigher).weigher;
			Assert.That(customWeigher, Is.SameAs((Object) Weighers.ByteArray()));
		}

		[Test]
		[TestCaseSource("Builder")]
		public void weigher_withCustom_entry(Builder<int, int> builder) {
			IEntryWeigher<int, int> custom = new CustomEntryWeigher();
			builder.Weigher(custom);
			IEntryWeigher<int, int> weigher = ((BoundedEntryWeigher<int, int>) builder.Build().weigher).weigher;
			Assert.That(weigher, Is.SameAs(custom));
		}

		private class CustomEntryWeigher : IEntryWeigher<int, int>
		{
			public int weightOf(int key, int value) {
				return key + value;
			}
		}
	}
}

