using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ConcurrentLinkedDictionary
{
	public interface IDequeue<Type> : ICollection<Type>
	{
		bool IsEmpty { get; }

		Type Dequeue();
		void Enqueue(Type value);
		Type Peek();
		Type[] ToArray ();
	}
}

