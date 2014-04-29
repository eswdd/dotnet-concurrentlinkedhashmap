using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ConcurrentLinkedDictionary
{
	public interface IDeque<Type> : ICollection<Type>
	{
		bool IsEmpty { get; }

		Type Dequeue();
		void Enqueue(Type value);
		Type Peek();
		/*
		bool Offer(Type value);
		Type PeekLast ();
		Type PeekFirst ();
		Type GetLast ();
		Type GetFirst ();
		Type Element ();*/
		Type[] ToArray ();

		IEnumerator<Type> GetDescendingEnumerator ();
	}
}

