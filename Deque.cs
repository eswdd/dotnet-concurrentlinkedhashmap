using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcurrentLinkedDictionary
{
	public class Deque<T> : IDeque<T>
	{
		private Queue<T> _queue;

		public Deque ()
		{
			_queue = new Queue<T> ();
		}

		public void Enqueue (T value)
		{
			_queue.Enqueue (value);
		}

		public T Peek ()
		{
			return _queue.Peek ();
		}

		public T[] ToArray ()
		{
			return _queue.ToArray ();
		}

		public void Clear ()
		{
			_queue.Clear ();
		}

		public bool Contains (T item)
		{
			return _queue.Contains (item);
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			_queue.CopyTo (array, arrayIndex);
		}

		public int Count {
			get {
				return _queue.Count;
			}
		}

		public IEnumerator<T> GetEnumerator ()
		{
			return _queue.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return ((System.Collections.IEnumerable)_queue).GetEnumerator ();
		}

		public void Add (T item)
		{
			_queue.Enqueue (item);
		}

		public bool Remove(T item)
		{
			throw new NotSupportedException ();
		}

		public T Dequeue()
		{
			return _queue.Dequeue ();
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public bool IsEmpty {
			get {
				return _queue.Count == 0;
			}
		}

		public IEnumerator<T> GetDescendingEnumerator()
		{
			return _queue.Reverse ().GetEnumerator ();
		}

		public void CopyTo (Array array, int index)
		{
			throw new NotImplementedException ();
		}

		public bool IsSynchronized {
			get {
				return false;
			}
		}

		public object SyncRoot {
			get {
				return this;
			}
		}
	}
}

