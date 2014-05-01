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
		private DescriptionBuilder builder = new DescriptionBuilder();

		public override bool Matches (object actual)
		{
			builder.ExpectThat ("not a collection", actual, Is.InstanceOf<ICollection<Type>> ());
			if (!(actual is ICollection<Type>)) 
			{
				return false;
			}
			var collection = (ICollection<Type>)actual;
			CheckCollection (collection);

			if (collection is ISet<Type>)
			{
				CheckSet((ISet<Type>)collection);
			}

			if (collection is IList<Type>)
			{
				CheckList((IList<Type>)collection);
			}

			if (collection is Queue<Type>)
			{
				CheckQueue ((Queue<Type>)collection);
			}

			if (collection is IDeque<Type>)
			{
				CheckDeque ((IDeque<Type>)collection);
			}

			return builder.Matches;
		}

		private void CheckCollection(ICollection<Type> c)
		{
			builder.ExpectThat (c.Count, Is.EqualTo (0));
			builder.ExpectThat ("iterator has data", c.GetEnumerator ().MoveNext (), Is.False);
		}

		private void CheckSet(ISet<Type> c)
		{
			builder.ExpectThat ("collection not equal to empty set", c, Is.EqualTo (new HashSet<Type> ()));
		}

		private void CheckList(IList<Type> c)
		{
			builder.ExpectThat ("collection not equal to empty list", c, Is.EqualTo (new List<Type> ()));
		}

		private void CheckQueue(Queue<Type> c)
		{
			builder.ExpectThat (c.Peek (), Is.Null);
			builder.ExpectThat (c.Dequeue (), Is.Null);
			if (c.ToArray () != null) {
				builder.ExpectThat (c.ToArray ().Length, Is.EqualTo (0));
			}
			builder.ExpectThat (c.GetEnumerator ().MoveNext (), Is.False);
		}

		private void CheckDeque(IDeque<Type> c)
		{
			builder.ExpectThat (c.Peek (), Is.Null);
			builder.ExpectThat (c.Dequeue (), Is.Null);
			if (c.ToArray () != null) {
				builder.ExpectThat (c.ToArray ().Length, Is.EqualTo (0));
			}
			builder.ExpectThat (c.GetEnumerator ().MoveNext (), Is.False);
			builder.ExpectThat (c.GetDescendingEnumerator ().MoveNext (), Is.False);
		}

		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.WriteExpectedValue ("empty");
			builder.DescribeTo (writer);
		}
	}
}

