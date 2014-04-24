using System;
using System.Collections.Generic;

namespace ConcurrentLinkedDictionary
{
	public class Deque<T> : Queue<T>, IDequeue<T>
	{
		public Deque ()
		{
		}

		public void Add (T item)
		{
			Enqueue (item);
		}

		public bool Remove (T item)
		{
			throw new NotSupportedException ();
		}

		public bool IsReadOnly {
			get {
				throw new NotImplementedException ();
			}
		}

		public bool IsEmpty {
			get {
				throw new NotImplementedException ();
			}
		}
	}
}

