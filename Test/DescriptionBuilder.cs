using System;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace ConcurrentLinkedDictionary.Test
{
	public class DescriptionBuilder
	{
		private TextMessageWriter _buffer = new TextMessageWriter();
		private bool _matches = true;

		public void ExpectException<TException> (Action a) where TException : Exception
		{
			ExpectException<TException> (a, "");
		}

		public void ExpectException<TException> (Action a, string reason) where TException : Exception
		{
			try
			{
				a();
				if (reason != "") {
					reason = "\n" + reason;
				}
				_buffer.WriteMessageLine ("\n"+reason+"\nExpected "+typeof(TException)+" but was none");
				_matches = false;
			}
			catch (Exception e) {
				//if (!e.GetType().Equals(typeof(TException))) {
				if (!(e is TException)) {
					if (reason != "") {
						reason = "\n" + reason;
					}
					_buffer.WriteMessageLine ("\n"+reason+"\nExpected "+typeof(TException)+" but was "+e.GetType());
					_matches = false;
				}
			}
		}

		public void ExpectThat<T>(T actual, Constraint constraint) {
			ExpectThat("", actual, constraint);
		}
		public void ExpectThat<T>(string reason, T actual, Constraint constraint) {
			if (!constraint.Matches (actual)) 
			{
				if (reason != "") {
					reason = "\n" + reason;
				}
				_buffer.WriteMessageLine ("\n"+reason+"\nExpected: ");
				constraint.WriteDescriptionTo (_buffer);
				_buffer.WriteMessageLine ("\n but was: ");
				constraint.WriteActualValueTo(_buffer);
				// todo: work this out
				//_buffer.WriteMessageLine("\nLocation: ");
				//_buffer.WriteMessageLine(new Exception().StackTrace);
				_matches = false;
			}
		}

		public bool Matches {
			get { 
				return _matches;
			}
		}

		public void DescribeTo(MessageWriter writer)
		{
			writer.WriteValue (_buffer.ToString ());
		}
	}
}

