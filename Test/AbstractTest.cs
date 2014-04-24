using System;
using ConcurrentLinkedDictionary;

namespace ConcurrentLinkedDictionary.Test
{
	public abstract class AbstractTest
	{
		long _capacity;

		protected IEvictionListener<int, int> listener;
		//protected Mock<IEvictionListener<int, int>> listenerMock;

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


		/* ---------------- Map providers -------------- */

		/** Provides a builder with the capacity set. */

		public Object[][] Builder() {
			return new Object[][] {
				new Object[] {new Builder<Object, Object>().MaximumWeightedCapacity(Capacity())}
			};
		}
		public Object[][] BuilderIntInt() {
			return new Object[][] {
				new Object[] {new Builder<int, int>().MaximumWeightedCapacity(Capacity())}
			};
		}
		public Object[][] BuilderIntByteArray() {
			return new Object[][] {
				new Object[] {new Builder<int, byte[]>().MaximumWeightedCapacity(Capacity())}
			};
		}

		protected enum TestType
		{
			Standard
		}
	}
}

