using System;
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
			return collection.Count == 0;
			// todo: expand me a bit more..
		}
		public override void WriteDescriptionTo (MessageWriter writer)
		{
			writer.Write ("empty");
		}
	}
}

