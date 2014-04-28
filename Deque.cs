using System;
using System.Collections.Generic;

namespace ConcurrentLinkedDictionary
{
	public class Deque<T> : Queue<T>, IDeque<T>
	{

		public Deque ()
		{
		}

		public void Add (T item)
		{
			Enqueue (item);
		}

		public bool Remove(T item)
		{
			throw new NotSupportedException ();
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public bool IsEmpty {
			get {
				return Count == 0;
			}
		}
	}
}

