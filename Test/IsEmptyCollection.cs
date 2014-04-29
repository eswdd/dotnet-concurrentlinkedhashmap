using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections.Generic;

namespace ConcurrentLinkedDictionary.Test
{
	public class IsEmptyCollection<T> : IResolveConstraint
	{
		public IsEmptyCollection ()
		{
		}


		public Constraint Resolve ()
		{
			return new IsEmptyCollectionConstraint<T> ();
		}

	}

	public sealed class IsEmptyCollectionConstraint<Type> : Constraint
	{
		public override bool Matches (object actual)
		{
			if (!(actual is ICollection<Type>)) 
			{
				return false;
			}
			var collection = (ICollection<Type>)actual;
			if (!CheckCollection (collection)) {
				return false;
			}

			if (collection is ISet<Type>)
			{
				if (!CheckSet((ISet<Type>)collection))
				{
					return false;
				}
			}

			if (collection is Queue<Type>)
			{
				if (!CheckQueue((Queue<Type>)collection))
				{
					return false;
				}
			}

			if (collection is IDeque<Type>)
			{
				if (!CheckDeque((IDeque<Type>)collection))
				{
					return false;
				}
			}

			return true;
		}

		private bool CheckCollection(ICollection<Type> c)
		{
			if (c.Count != 0)
				return false;
			if (c.GetEnumerator ().MoveNext ())
				return false;
			return true;
		}

		private bool CheckSet(ISet<Type> c)
		{
			if (!c.SetEquals(new HashSet<Type>())) return false;
			return true;
		}

		private bool CheckQueue(Queue<Type> c)
		{
			if (!Object.Equals (c.Peek (), default(Type)))
				return false;
			if (!Object.Equals (c.Dequeue (), default(Type)))
				return false;
			if (c.ToArray ().Length != 0)
				return false;
			return true;
		}

		private bool CheckDeque(IDeque<Type> c)
		{
			if (!Object.Equals (c.Peek (), default(Type)))
				return false;
			if (!Object.Equals (c.Dequeue (), default(Type)))
				return false;
			if (c.ToArray () != null && c.ToArray().Length != 0)
				return false;
			if (c.GetDescendingEnumerator ().MoveNext ())
				return false;
			return true;
		}

		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.Write ("empty");
		}
	}
}

