using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;

namespace ConcurrentLinkedDictionary.Test
{
	public class CollectionConstraints
	{

		public static Constraint HasCount(int expected)
		{
			return new HasCountConstraint (expected);
		}



		public static Constraint ContainsInAnyOrder<T> (params T[] expect)
		{
			return new ContainsInAnyOrderConstraint<T> (expect);
		}

		internal class ContainsInAnyOrderConstraint<T> : Constraint
		{
			private T[] _lookingFor;
			private List<T> _missing;

			internal ContainsInAnyOrderConstraint(T[] lookingFor)
			{
				_lookingFor = lookingFor;
			}

			IEnumerable<T> coll;

			public override bool Matches (object actual)
			{
				coll = actual as IEnumerable<T>;
				if (coll == null) {
					return false;
				}

				_missing = _lookingFor.ToList ();
				foreach (var t in coll) 
				{
					_missing.Remove (t);
				}
				return _missing.Count == 0;
			}
			public override void WriteDescriptionTo (MessageWriter writer)
			{
				if (coll == null) {
					writer.WriteLine ("given object is not an IEnumerable<" + typeof(T) + ">");
				} else {
					writer.WritePredicate ("members not found: ");
					writer.WriteExpectedValue (_missing.Select (x => x.ToString ()).ToArray());
				}
			}
		}

		internal class HasCountConstraint : Constraint
		{
			private int _lookingFor;
			private int _actual;

			ICollection coll;

			internal HasCountConstraint(int lookingFor)
			{
				_lookingFor = lookingFor;
			}

			public override bool Matches (object actual)
			{
				coll = actual as ICollection;
				if (coll == null) {
					return false;
				}
				_actual = coll.Count;
				return _actual == _lookingFor;
			}
			public override void WriteDescriptionTo (MessageWriter writer)
			{
				if (coll == null) {
					writer.WriteLine ("given object is not an ICollection");
				} else {
					writer.WritePredicate ("Count equal to");
					writer.WriteExpectedValue (_lookingFor);
					writer.WriteLine (" but was ");
					writer.WriteActualValue (_actual);
				}
			}
		}
	}
}

