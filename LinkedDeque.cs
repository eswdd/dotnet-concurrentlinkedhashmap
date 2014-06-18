using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;

namespace ConcurrentLinkedDictionary
{
    /// <summary>
    /// Linked list implementation of the {@link Deque} interface where the link
    /// pointers are tightly integrated with the element. Linked deques have no
    /// capacity restrictions; they grow as necessary to support usage. They are not
    /// thread-safe; in the absence of external synchronization, they do not support
    /// concurrent access by multiple threads. Null elements are prohibited.
    /// 
    /// Most <tt>LinkedDeque</tt> operations run in constant time by assuming that
    /// the {@link Linked} parameter is associated with the deque instance. Any usage
    /// that violates this assumption will result in non-deterministic behavior.
    /// 
    /// The iterators returned by this class are <em>not</em> <i>fail-fast</i>: If
    /// the deque is modified at any time after the iterator is created, the iterator
    /// will be in an unknown state. Thus, in the face of concurrent modification,
    /// the iterator risks arbitrary, non-deterministic behavior at an undetermined
    /// time in the future.
    /// Original author: ben.manes@google.com (Ben Manes)
    /// Ported by: Simon Matic Langford
    /// </summary>
    public sealed class LinkedDeque<E> : IDeque<E> where E : ILinked<E>
    {

        // This class provides a doubly-linked list that is optimized for the virtual
        // machine (I assume the optimisation holds true for the CLR). The first 
        // and last elements are manipulated instead of a slightly
        // more convenient sentinel element to avoid the insertion of null checks with
        // NullPointerException throws in the byte code. The links to a removed
        // element are cleared to help a generational garbage collector if the
        // discarded elements inhabit more than one generation.

        /*        *
  Pointer to first node.
  Invariant: (first == null && last == null) ||
             (first.prev == null)
   */
        internal E first;

        /*        *
  Pointer to last node.
  Invariant: (first == null && last == null) ||
             (last.next == null)
   */
        internal E last;

        /*        *
  Links the element to the front of the deque so that it becomes the first
  element.
   *
  @param e the unlinked element
   */
        void linkFirst(E e) {
            E f = first;
            first = e;

            if (f == null) {
                last = e;
            } else {
                f.Previous = e;
                e.Next = f;
            }
        }

        /*        *
  Links the element to the back of the deque so that it becomes the last
  element.
   *
  @param e the unlinked element
   */
        void linkLast(E e) {
            E l = last;
            last = e;

            if (l == null) {
                first = e;
            } else {
                l.Next = e;
                e.Previous = l;
            }
        }

        /*        * Unlinks the non-null first element. */
        E unlinkFirst() {
            E f = first;
            E next = f.Next;
            f.Next = default(E);

            first = next;
            if (next == null) {
                last = default(E);
            } else {
                next.Previous = default(E);
            }
            return f;
        }

        /*        * Unlinks the non-null last element. */
        E unlinkLast() {
            E l = last;
            E prev = l.Previous;
            l.Previous = default(E);
            last = prev;
            if (prev == null) {
                first = default(E);
            } else {
                prev.Next = default(E);
            }
            return l;
        }

        /*        * Unlinks the non-null element. */
        void unlink(E e) {
            E prev = e.Previous;
            E next = e.Next;

            if (prev == null) {
                first = next;
            } else {
                prev.Next = next;
                e.Previous = default(E);
            }

            if (next == null) {
                last = prev;
            } else {
                next.Previous = prev;
                e.Next = default(E);
            }
        }
    

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        void checkNotEmpty() {
            if (IsEmpty) {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Beware that, unlike in most collections, this method is <em>NOT</em> a constant-time operation.
        /// </summary>
        public int Count { get {
                int size = 0;
                for (E e = first; e != null; e = e.Next) {
                    size++;
                }
                return size;
            }
        }

        //@Override
        public void Clear() {
            for (E e = first; e != null;) {
                E next = e.Next;
                e.Previous = default(E);
                e.Next = default(E);
                e = next;
            }
            first = last = default(E);
        }

        //@Override
        public bool Contains(Object o) {
            return (o is ILinked<E>) && Contains((ILinked<E>) o);
        }

        // A fast-path containment check
        public bool Contains(E e) {
            return (e.Previous != null)
                || (e.Next != null)
                || (object.ReferenceEquals(e,first));
        }

        /*        *
  Moves the element to the front of the deque so that it becomes the first
  element.
   *
  @param e the linked element
   */
        public void moveToFront(E e) {
            if (!object.ReferenceEquals(e,first)) {
                unlink(e);
                linkFirst(e);
            }
        }

        /*        *
  Moves the element to the back of the deque so that it becomes the last
  element.
   *
  @param e the linked element
   */
        public void moveToBack(E e) {
            if (!object.ReferenceEquals(e,last)) {
                unlink(e);
                linkLast(e);
            }
        }

        public bool IsEmpty { get { return (first == null); } }

        public E Dequeue()
        {
            if (IsEmpty) {
                throw new InvalidOperationException ("queue is empty");
            }
            return unlinkFirst();
        }
        public void Enqueue(E value)
        {
            offerLast (value);
        }
        public E Peek()
        {
            return first;
        }
        public E[] ToArray ()
        {
            //todo
            return null;
        }
        public void CopyTo(E[] array, int len)
        {
            // todo
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

        /*
        //@Override
        public E peek() {
            return peekFirst();
        }

        //@Override
        public E peekFirst() {
            return first;
        }

        //@Override
        public E peekLast() {
            return last;
        }

        //@Override
        public E getFirst() {
            checkNotEmpty();
            return peekFirst();
        }

        //@Override
        public E getLast() {
            checkNotEmpty();
            return peekLast();
        }

        //@Override
        public E element() {
            return getFirst();
        }

        //@Override
        public boolean offer(E e) {
            return offerLast(e);
        }

        //@Override
        public boolean offerFirst(E e) {
            if (contains(e)) {
                return false;
            }
            linkFirst(e);
            return true;
        }
        */
        //@Override
        private void offerLast(E e) {
            if (Contains(e)) {
                return;
            }
            linkLast(e);
        }
        //@Override
        public void Add(E e) {
            offerLast(e);
        }
        /*

        //@Override
        public boolean add(E e) {
            return offerLast(e);
        }


        //@Override
        public void addFirst(E e) {
            if (!offerFirst(e)) {
                throw new IllegalArgumentException();
            }
        }

        //@Override
        public void addLast(E e) {
            if (!offerLast(e)) {
                throw new IllegalArgumentException();
            }
        }

        //@Override
        public E poll() {
            return pollFirst();
        }

        //@Override
        public E pollFirst() {
            return isEmpty() ? null : unlinkFirst();
        }

        //@Override
        public E pollLast() {
            return isEmpty() ? null : unlinkLast();
        }

        //@Override
        public E remove() {
            return removeFirst();
        }
        */

        // A fast-path removal
        public bool Remove(E e) {
            if (Contains(e)) {
                unlink(e);
                return true;
            }
            return false;
        }

        /*
        @Override
        public E removeFirst() {
            checkNotEmpty();
            return pollFirst();
        }

        @Override
        public boolean removeFirstOccurrence(Object o) {
            return remove(o);
        }

        @Override
        public E removeLast() {
            checkNotEmpty();
            return pollLast();
        }

        @Override
        public boolean removeLastOccurrence(Object o) {
            return remove(o);
        }

        @Override
        public boolean removeAll(Collection<?> c) {
            boolean modified = false;
            for (Object o : c) {
                modified |= remove(o);
            }
            return modified;
        }

        @Override
        public void push(E e) {
            addFirst(e);
        }

        @Override
        public E pop() {
            return removeFirst();
        }

        @Override
        public Iterator<E> iterator() {
            return new AbstractLinkedIterator(first) {
                @Override E computeNext() {
                    return cursor.getNext();
                }
            };
        }

        @Override
        public Iterator<E> descendingIterator() {
            return new AbstractLinkedIterator(last) {
                @Override E computeNext() {
                    return cursor.getPrevious();
                }
            };
        }*/

        public IEnumerator GetEnumerator()
        {
            return new ForwardIterator<E> (first);
        }

        IEnumerator<E> IEnumerable<E>.GetEnumerator()
        {
            return new ForwardIterator<E>(first);
        }
        public IEnumerator<E> GetDescendingEnumerator()
        {
            return new BackwardIterator<E>(last);
        }

        internal class ForwardIterator<T> : AbstractLinkedIterator<T> where T : ILinked<T> {
            internal ForwardIterator(T start) : base(start) {}
            protected override T computeNext() {
                return cursor.Next;
            }
        }
        internal class BackwardIterator<T> : AbstractLinkedIterator<T> where T : ILinked<T> {
            internal BackwardIterator(T start) : base(start) {}
            protected override T computeNext() {
                return cursor.Previous;
            }
        }

        internal abstract class AbstractLinkedIterator<T> : IEnumerator<T> {
            T start;
            protected T cursor;
            protected bool started;

            /// <summary>
            /// Creates an iterator that can can traverse the deque.
            /// </summary>
            /// <param name="start">the initial element to begin traversal from</param>
            protected AbstractLinkedIterator(T start) {
                this.start = start;
            }

            public object Current 
            {
                get 
                {
                    return cursor;
                }
            }

            T IEnumerator<T>.Current 
            {
                get 
                {
                    return cursor;
                }
            }

            public bool MoveNext() {
                if (!started) {
                    cursor = start;
                    started = true;
                } 
                else {
                    if (Equals(cursor, default(T))) {
                        return false;
                    }
                    cursor = computeNext ();
                }
                return (cursor != null);
            }

            public void Reset() {
                cursor = default(T);
                started = true;
            }

            public void Dispose() {
            }

            /// <summary>
            /// Retrieves the next element to traverse to or <tt>null</tt> if there are no more elements.
            /// </summary>
            protected abstract T computeNext();
        }
    }

    /// <summary>
    /// An element that is linked on the <see cref="IDequeue"/>.
    /// </summary>
    public interface ILinked<T> where T : ILinked<T>
    {
        /// <summary>
        /// The previous element or <tt>null</tt> if either the element is
        /// unlinked or the first element on the deque.
        /// </summary>
        T Previous { get; set; }

        /// <summary>
        /// The next element or <tt>null</tt> if either the element is
        /// unlinked or the last element on the deque.
        /// </summary>
        T Next { get; set; }
    }
}

