using System;
using NUnit.Framework;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using System.Linq;

namespace ConcurrentLinkedDictionary.Test
{
	/// <summary>
	/// A unit-test for <see cref="LinkedDeque"/> methods.
	/// Original author: ben.manes@google.com (Ben Manes)
	/// Ported by: Simon Matic Langford
	/// </summary>
	[TestFixture]
	[Category("development")]
	public class LinkedDequeTest : AbstractTest
	{

		public LinkedDequeTest() : base(TestType.Standard)
		{
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void clear_whenEmpty(IDeque<SimpleLinkedValue> deque) 
		{
			deque.Clear();
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void clear_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			deque.Clear();
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void isEmpty_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.IsEmpty, Is.True);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void isEmpty_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.IsEmpty, Is.False);
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void size_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Count, Is.EqualTo(0));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void size_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			// linq uses the IEnumerable interface
			Assert.That(deque.Count(), Is.EqualTo((int) Capacity()));
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(NullReferenceException))]
		public void contains_withNull(IDeque<SimpleLinkedValue> deque) {
			deque.Contains(null);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void contains_whenFound(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Contains(deque.ElementAt((int) Capacity() / 2)), Is.True);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void contains_whenNotFound(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue unlinked = new SimpleLinkedValue(1);
			Assert.That(deque.Contains(unlinked), Is.False);
		}



		/* ---------------- Move -------------- */

		// these tests removed because we don't support these operations
		/*
		[Test]
		[TestCaseSource("WarmedDeque")]
		public void moveToFront_first(LinkedDeque<SimpleLinkedValue> deque) {
			checkMoveToFront(deque, deque.GetFirst());
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void moveToFront_middle(LinkedDeque<SimpleLinkedValue> deque) {
			checkMoveToFront(deque, deque.ElementAt((int) Capacity() / 2));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void moveToFront_last(LinkedDeque<SimpleLinkedValue> deque) {
			checkMoveToFront(deque, deque.GetLast());
		}

		private void checkMoveToFront(LinkedDeque<SimpleLinkedValue> deque, SimpleLinkedValue element) {
			deque.moveToFront(element);
			Assert.That(deque.PeekFirst(), Is.SameAs(element));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void moveToBack_first(LinkedDeque<SimpleLinkedValue> deque) {
			checkMoveToBack(deque, deque.GetFirst());
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void moveToBack_middle(LinkedDeque<SimpleLinkedValue> deque) {
			checkMoveToBack(deque, deque.ElementAt((int) Capacity() / 2));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void moveToBack_last(LinkedDeque<SimpleLinkedValue> deque) {
			checkMoveToBack(deque, deque.GetLast());
		}

		private void checkMoveToBack(LinkedDeque<SimpleLinkedValue> deque, SimpleLinkedValue element) {
			deque.moveToBack(element);
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			Assert.That(deque.GetLast(), Is.SameAs(element));
		}*/


		/* ---------------- Peek -------------- */

		// tests removed because operations not supported


		[Test]
		[TestCaseSource("EmptyDeque")]
		public void peek_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Peek(), Is.Null);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void peek_whenPopulated(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.first;
			Assert.That(deque.Peek(), Is.SameAs(first));
			Assert.That(deque.first, Is.SameAs(first));
			Assert.That(deque.Count, Is.EqualTo((int)Capacity()));
			Assert.That(deque.Contains(first), Is.True);
		}


		// tests commented as methods not supported

		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		public void peekFirst_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.PeekFirst(), Is.Null);
		}
		[Test]
		[TestCaseSource("WarmedDeque")]
		public void peekFirst_whenPopulated(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.first;
			Assert.That(deque.PeekFirst(), Is.SameAs(first));
			Assert.That(deque.first, Is.SameAs(first));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			Assert.That(deque.Contains(first), Is.True);
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void peekLast_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.PeekLast(), Is.Null);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void peekLast_whenPopulated(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue last = deque.last;
			Assert.That(deque.PeekLast(), Is.SameAs(last));
			Assert.That(deque.last, Is.SameAs(last));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			Assert.That(deque.Contains(last), Is.True);
		}*/

		/* ---------------- Get -------------- */

		// tests removed because operations not supported

		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void getFirst_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			deque.GetFirst ();
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void getFirst_whenPopulated(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.first;
			Assert.That(deque.GetFirst(), Is.SameAs(first));
			Assert.That(deque.first, Is.SameAs(first));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			Assert.That(deque.Contains(first), Is.True);
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void getLast_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			deque.GetLast();
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void getLast_whenPopulated(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue last = deque.last;
			Assert.That(deque.GetLast(), Is.SameAs(last));
			Assert.That(deque.last, Is.SameAs(last));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			Assert.That(deque.Contains(last), Is.True);
		}*/

		/* ---------------- Element -------------- */

		// tests remove because operations not supported
		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void element_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			deque.Element();
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void element_whenPopulated(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.first;
			Assert.That(deque.Element(), Is.SameAs(first));
			Assert.That(deque.first, Is.SameAs(first));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
			Assert.That(deque.Contains(first), Is.True);
		}*/

		/* ---------------- Offer -------------- */

		// tests remove because operations not supported - todo: what about Enqueue??
		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		public void offer_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			Assert.That(deque.Offer(value), Is.True);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void offer_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			Assert.That(deque.Offer(value), Is.True);
			Assert.That(deque.PeekFirst(), Is.Not.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity() + 1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void offer_whenLinked(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Offer(deque.Peek()), Is.False);
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void offerFirst_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			Assert.That(deque.OfferFirst(value), Is.True);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void offerFirst_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			Assert.That(deque.OfferFirst(value), Is.True);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.Not.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity() + 1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void offerFirst_whenLinked(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.OfferFirst(deque.peek()), Is.False);
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void offerLast_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			Assert.That(deque.OfferLast(value), Is.True);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void offerLast_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			Assert.That(deque.OfferLast(value), Is.True);
			Assert.That(deque.PeekFirst(), Is.Not.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity() + 1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void offerLast_whenLinked(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.OfferLast(deque.peek()), Is.False);
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}*/


		/* ---------------- Add -------------- */

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void add_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			deque.Add (value);
			Assert.That(deque.Peek(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void add_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			deque.Add(value);
			Assert.That(deque.Peek(), Is.Not.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity() + 1));
		}
			
		[Test]
		[TestCaseSource("WarmedDeque")]
		public void add_whenLinked(IDeque<SimpleLinkedValue> deque) {
			var sizeBefore = deque.Count;
			deque.Add(deque.Peek());
			Assert.That (deque.Count, Is.EqualTo (sizeBefore));
		}

		// methods not supported
		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		public void addFirst_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			deque.AddFirst(value);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void addFirst_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			deque.AddFirst(value);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.Not.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		[ExpectedException(typeof(ArgumentException))]
		public void addFirst_whenLinked(IDeque<SimpleLinkedValue> deque) {
			deque.AddFirst(deque.Peek());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void addLast_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			deque.AddLast(value);
			Assert.That(deque.PeekFirst(), Is.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void addLast_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			deque.AddLast(value);
			Assert.That(deque.PeekFirst(), Is.Not.SameAs(value));
			Assert.That(deque.PeekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		[ExpectedException(typeof(ArgumentException))]
		public void addLast_whenLinked(IDeque<SimpleLinkedValue> deque) {
			deque.AddLast(deque.Peek());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void addAll_withEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.AddAll(new List<SimpleLinkedValue>()), Is.False);
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void addAll_withPopulated(IDeque<SimpleLinkedValue> deque) {
			IList<SimpleLinkedValue> expected = new List<SimpleLinkedValue> ();
			warmUp(expected);
			Assert.That(deque.AddAll(expected), Is.True);
			Assert.That(Iterables.elementsEqual(deque, expected), Is.True);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void addAll_withSelf(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.AddAll(deque), Is.False);
		}*/

		/* ---------------- Dequeue -------------- */

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void dequeue_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Dequeue(), Is.Null);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void dequeue_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.Peek();
			Assert.That(deque.Dequeue(), Is.SameAs(first));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity() - 1));
			Assert.That(deque.Contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void dequeue_toEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value;
			while ((value = deque.Dequeue()) != null) {
				Assert.That(deque.Contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		// operations not supported

		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		public void pollFirst_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.pollFirst(), Is.Null);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void pollFirst_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.peekFirst();
			Assert.That(deque.pollFirst(), Is.SameAs(first));
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void pollFirst_toEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value;
			while ((value = deque.pollFirst()) != null) {
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void pollLast_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.pollLast(), Is.Null);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void pollLast_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue last = deque.peekLast();
			Assert.That(deque.pollLast(), Is.SameAs(last));
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(last), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void pollLast_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value;
			while ((value = deque.pollLast()) != null) {
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}*/

		/* ---------------- Remove -------------- */



		[Test]
		[TestCaseSource("EmptyDeque")]
		public void removeElement_notFound(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.Remove(new SimpleLinkedValue(0)), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeElement_whenFound(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.Peek();
			Assert.That(deque.Remove(first), Is.True);
			Assert.That(deque.Count, Is.EqualTo((int) Capacity() - 1));
			Assert.That(deque.Contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeElement_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			while (!deque.IsEmpty) {
				SimpleLinkedValue value = deque.Peek();
				Assert.That(deque.Remove(value), Is.True);
				Assert.That(deque.Contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		// methods not supported
		/*
		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void removeFirst_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			deque.removeFirst();
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeFirst_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.peekFirst();
			Assert.That(deque.removeFirst(), Is.SameAs(first));
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeFirst_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			while (!deque.isEmpty()) {
				SimpleLinkedValue value = deque.removeFirst();
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		
		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void removeLast_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			deque.removeLast();
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeLast_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue last = deque.peekLast();
			Assert.That(deque.removeLast(), Is.SameAs(last));
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(last), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeLast_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			while (!deque.isEmpty()) {
				SimpleLinkedValue value = deque.removeLast();
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void removeFirstOccurrence_notFound(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.removeFirstOccurrence(new SimpleLinkedValue(0)), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeFirstOccurrence_whenFound(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.peekFirst();
			Assert.That(deque.removeFirstOccurrence(first), Is.True);
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeFirstOccurrence_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			while (!deque.isEmpty()) {
				SimpleLinkedValue value = deque.peek();
				Assert.That(deque.removeFirstOccurrence(value), Is.True);
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void removeLastOccurrence_notFound(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.removeLastOccurrence(new SimpleLinkedValue(0)), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeLastOccurrence_whenFound(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.peekFirst();
			Assert.That(deque.removeLastOccurrence(first), Is.True);
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeLastOccurrence_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			while (!deque.isEmpty()) {
				SimpleLinkedValue value = deque.peek();
				Assert.That(deque.removeLastOccurrence(value), Is.True);
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void removeAll_withEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.removeAll(ImmutableList.of()), Is.False);
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void remove_withPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.peekFirst();
			Assert.That(deque.removeAll(ImmutableList.of(first)), Is.True);
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void removeAll_toEmpty(IDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.removeAll(ImmutableList.copyOf(deque)), Is.True);
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}*/

		/* ---------------- Stack -------------- *

        // Stack support not required

		[Test]
		[TestCaseSource("EmptyDeque")]
		public void push_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue(1);
			deque.push(value);
			Assert.That(deque.peekFirst(), Is.SameAs(value));
			Assert.That(deque.peekLast(), Is.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo(1));
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void push_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue value = new SimpleLinkedValue((int) Capacity());
			deque.push(value);
			Assert.That(deque.peekFirst(), Is.SameAs(value));
			Assert.That(deque.peekLast(), Is.Not.SameAs(value));
			Assert.That(deque.Count, Is.EqualTo((int) Capacity()));
		}

		@Test(dataProvider = "warmedDeque", expectedExceptions = IllegalArgumentException.class)
		public void push_whenLinked(IDeque<SimpleLinkedValue> deque) {
			deque.push(deque.peek());
		}

		
		[Test]
		[TestCaseSource("EmptyDeque")]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void pop_whenEmpty(IDeque<SimpleLinkedValue> deque) {
			deque.pop();
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void pop_whenPopulated(IDeque<SimpleLinkedValue> deque) {
			SimpleLinkedValue first = deque.peekFirst();
			Assert.That(deque.pop(), Is.SameAs(first));
			Assert.That(deque, hasSize((int) Capacity() - 1));
			Assert.That(deque.contains(first), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void pop_toEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			while (!deque.isEmpty()) {
				SimpleLinkedValue value = deque.pop();
				Assert.That(deque.contains(value), Is.False);
			}
			Assert.That(deque, emptyCollection<SimpleLinkedValue>());
		}

		/* ---------------- Enumerators -------------- */


		[Test]
		[TestCaseSource("EmptyDeque")]
		public void iterator_whenEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			Assert.That (deque.GetEnumerator ().MoveNext(), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void iterator_whenWarmed(LinkedDeque<SimpleLinkedValue> deque) {
			IList<SimpleLinkedValue> expected = new List<SimpleLinkedValue> ();
			WarmUp(expected);

			// first item from expected has no next or prev
			Assert.That(elementsEqual(((IDeque<SimpleLinkedValue>)deque).GetEnumerator(), expected.GetEnumerator()), Is.True);
		}

		// .net enumerators don't support removal
		/*
		[Test]
		[TestCaseSource("WarmedDeque")]
		[ExpectedException(typeof(NotSupportedException))]
		public void iterator_removal(LinkedDeque<SimpleLinkedValue> deque) {
			deque.iterator().remove();
		}*/


		[Test]
		[TestCaseSource("EmptyDeque")]
		public void descendingIterator_whenEmpty(LinkedDeque<SimpleLinkedValue> deque) {
			Assert.That(deque.GetDescendingEnumerator().MoveNext(), Is.False);
		}

		[Test]
		[TestCaseSource("WarmedDeque")]
		public void descendingIterator_whenWarmed(LinkedDeque<SimpleLinkedValue> deque) {
			IList<SimpleLinkedValue> expected = new List<SimpleLinkedValue> ();
			WarmUp(expected);
			IEnumerable<SimpleLinkedValue> expected2 = expected.Reverse ();

			Assert.That(elementsEqual(deque.GetDescendingEnumerator(), expected2.GetEnumerator()), Is.True);
		}

		// .net enumerators don't support removal
		/*
		[Test]
		[TestCaseSource("WarmedDeque")]
		[ExpectedException(typeof(NotSupportedException))]
		public void descendingIterator_removal(LinkedDeque<SimpleLinkedValue> deque) {
			deque.descendingIterator().remove();
		}*/

		/* ---------------- Deque providers -------------- */

		public Object[][] EmptyDeque() 
		{
			return new Object[][] 
			{
				new Object[] {new LinkedDeque<SimpleLinkedValue>()}
			};
		}

		public Object[][] WarmedDeque() 
		{
			LinkedDeque<SimpleLinkedValue> deque = new LinkedDeque<SimpleLinkedValue>();
			WarmUp(deque);
			return new Object[][] { new Object[]{ deque }};
		}

		void WarmUp(ICollection<SimpleLinkedValue> collection) 
		{
			for (int i = 0; i < Capacity(); i++) {
				collection.Add(new SimpleLinkedValue(i));
			}
		}

		bool elementsEqual<T>(IEnumerator<T> one, IEnumerator<T> two)
		{
			var i = 0;
			while (one.MoveNext ()) {
				if (!two.MoveNext ()) {
					Assert.Fail ("Fail 1: "+i);
					return false;
				}
				if (!one.Current.Equals(two.Current)) {
					Assert.Fail ("Fail 2: "+i+" ("+one.Current+","+two.Current+")");
					return false;
				}
				i++;
			}
			if (two.MoveNext ()) {
				Assert.Fail ("Fail 3: "+i);
				return false;
			}
			return true;
		}

		public sealed class SimpleLinkedValue : ILinked<SimpleLinkedValue> {
			SimpleLinkedValue prev;
			SimpleLinkedValue next;
			readonly int value;

			public SimpleLinkedValue(int value) {
				this.value = value;
			}

			public SimpleLinkedValue Previous {
				get { return prev; }
				set { prev = value; }
			}

			public SimpleLinkedValue Next {
				get { return next; }
				set { next = value; }
			}

			public override bool Equals(Object o) {
				if (!(o is SimpleLinkedValue)) {
					return false;
				}
				return value == ((SimpleLinkedValue) o).value;
			}

			public override int GetHashCode() {
				return value;
			}

			public override String ToString() {
				return String.Format("value={0} prev={1}, next={2}]", value,
					(prev == null) ? null : (int?)prev.value,
					(next == null) ? null : (int?)next.value);
			}
		}
	}

}

