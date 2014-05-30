using System;
using System.Threading;

namespace ConcurrentLinkedDictionary
{

	public class AtomicReference<T> where T : class
	{
		public AtomicReference() {}
		public AtomicReference(T t) {
			_value = t;
		}

		private T _value;

		public T GetValue() {
			// todo: cant find this!
			return Volatile.Read<T>(ref _value);
		}

		// no lazy set in c# :(
		public void LazySet (T i)
		{
			SetValue (i);
		}


		public void SetValue(T i) {
			Interlocked.Exchange (ref _value, i);
		}

		public bool CompareAndSet (T compare, T newValue)
		{
			// todo: desperately need some atomicreference tests
			T ret = Interlocked.CompareExchange (ref _value, newValue, compare);
			return (ret == null && newValue == null) || (ret != null && Equals (ret, compare));
		}

	}

	public class AtomicLong
	{
		long _value;

		public AtomicLong() {
		}
		public AtomicLong(long v) {
			_value = v;
		}

		public long GetValue() {
			return Interlocked.Read (ref _value);
		}

		// no lazy set in c# :(
		public void LazySet (long i)
		{
			SetValue (i);
		}

		public void SetValue(long i) {
			Interlocked.Exchange (ref _value, i);
		}


	}

	/*			*
   * An AtomicReference with heuristic padding to lessen cache effects of this
   * heavily CAS'ed location. While the padding adds noticeable space, the
   * improved throughput outweighs using extra space.
   */
	class PaddedAtomicReference<T> : AtomicReference<T> where T : class {

		// Improve likelihood of isolation on <= 64 byte cache lines
		long q0, q1, q2, q3, q4, q5, q6, q7, q8, q9, qa, qb, qc, qd, qe;

		public PaddedAtomicReference() {}

		public PaddedAtomicReference(T value) : base(value) {}
	}

	/*			*
   * An AtomicLong with heuristic padding to lessen cache effects of this
   * heavily CAS'ed location. While the padding adds noticeable space, the
   * improved throughput outweighs using extra space.
   */
	public class PaddedAtomicLong : AtomicLong {

		// Improve likelihood of isolation on <= 64 byte cache lines
		long q0, q1, q2, q3, q4, q5, q6, q7, q8, q9, qa, qb, qc, qd, qe;

		public PaddedAtomicLong() {}

		public PaddedAtomicLong(long value) : base(value) {
		}
	}
	public class AtomicReferenceArray<T> where T : class
	{
		private T[] _array;

		public AtomicReferenceArray(int length)
		{
			_array = new T[length];
		}

		public int Length {
			get { return _array.Length; }
		}

		public T this [int index] {
			get {
				return Volatile.Read (ref _array [index]);
			}
			set {
				Volatile.Write (ref _array [index], value);
			}
		}

		// todo: need to complete the api
	}
}

