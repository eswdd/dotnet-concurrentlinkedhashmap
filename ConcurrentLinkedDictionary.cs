using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections;

namespace ConcurrentLinkedDictionary
{
	public class ConcurrentLinkedDictionary<K, V> : IDictionary<K, V>
    {

        /// <summary>
        /// The number of CPUs
        /// </summary>
    	static readonly int NCPU = Environment.ProcessorCount;

        /// <summary>
        /// The maximum weighted capacity of the map.
        /// </summary>
    	public static readonly long MAXIMUM_CAPACITY = long.MaxValue - int.MaxValue;

        /// <summary>
        /// The number of read buffers to use.
        /// </summary>
    	static readonly int NUMBER_OF_READ_BUFFERS = ceilingNextPowerOfTwo(NCPU);

        /// <summary>
        /// Mask value for indexing into the read buffers.
        /// </summary>
    	static readonly int READ_BUFFERS_MASK = NUMBER_OF_READ_BUFFERS - 1;

        /// <summary>
        /// The number of pending read operations before attempting to drain.
        /// </summary>
    	internal static readonly int READ_BUFFER_THRESHOLD = 32;

        /// <summary>
        /// The maximum number of read operations to perform per amortized drain.
        /// </summary>
    	static readonly int READ_BUFFER_DRAIN_THRESHOLD = 2 * READ_BUFFER_THRESHOLD;

        /// <summary>
        /// The maximum number of pending reads per buffer.
        /// </summary>
    	static readonly int READ_BUFFER_SIZE = 2 * READ_BUFFER_DRAIN_THRESHOLD;

        /// <summary>
        /// Mask value for indexing into the read buffer.
        /// </summary>
    	static readonly int READ_BUFFER_INDEX_MASK = READ_BUFFER_SIZE - 1;

        /// <summary>
        /// The maximum number of write operations to perform per amortized drain.
        /// </summary>
    	static readonly int WRITE_BUFFER_DRAIN_THRESHOLD = 16;

        /// <summary>
        /// A queue that discards all entries.
        /// </summary>
    	static readonly IDeque<Action> DISCARDING_QUEUE = new DiscardingQueue<Action>();

    	static int ceilingNextPowerOfTwo(int x) {
            // From Hacker's Delight, Chapter 3, Harry S. Warren Jr.
            // todo: use the above
        	int pow = 1;
        	while (pow < x) {
            	pow <<= 1;
            }
        	return pow;
        }

        // The backing data store holding the key-value associations
    	internal readonly ConcurrentDictionary<K, Node> data;
    	internal readonly int concurrencyLevel;

        // These fields provide support to bound the map by a maximum capacity
        //@GuardedBy("evictionLock")
    	internal readonly long[] readBufferReadCount;
        //@GuardedBy("evictionLock")
    	internal readonly LinkedDeque<Node> evictionDeque;

        //@GuardedBy("evictionLock") // must write under lock
        // was PaddedAtomicLong
    	internal readonly PaddedAtomicLong weightedSize;
        //@GuardedBy("evictionLock") // must write under lock
        // was PaddedAtomicLong
    	internal readonly PaddedAtomicLong capacity;

        // todo: used to be reentrant lock
    	internal readonly ReaderWriterLockSlim evictionLock;
    	internal readonly ConcurrentQueue<Action> writeBuffer;
        // was PaddedAtomicLong[]
    	internal readonly PaddedAtomicLong[] readBufferWriteCount;
        // was PaddedAtomicLong[]
    	internal readonly PaddedAtomicLong[] readBufferDrainAtWriteCount;
    	internal readonly PaddedAtomicReference<Node>[][] readBuffers;

    	internal readonly PaddedAtomicReference<string> drainStatus;
    	internal readonly IEntryWeigher<K, V> weigher;

        // These fields provide support for notifying a listener.
    	internal readonly IDeque<Node> pendingNotifications;
    	internal readonly IEvictionListener<K, V> listener;

        // todo: were transient
        //ISet<K> keySet;
    	ICollection<V> values;
        //ISet<KeyValuePair<K, V>> entrySet;

    	internal ConcurrentLinkedDictionary(Builder<K, V> builder) {
            // The data store and its maximum capacity
        	concurrencyLevel = builder.concurrencyLevel;
        	capacity = new PaddedAtomicLong(Math.Min(builder.capacity, MAXIMUM_CAPACITY));
        	data = new ConcurrentDictionary<K, Node>(concurrencyLevel, builder.initialCapacity);

            // The eviction support
        	weigher = builder.weigher;
        	evictionLock = new ReaderWriterLockSlim ();
        	weightedSize = new PaddedAtomicLong();
        	evictionDeque = new LinkedDeque<Node>();
        	writeBuffer = new ConcurrentQueue<Action>();
        	drainStatus = new PaddedAtomicReference<string>(DrainStatus.IDLE);

        	readBufferReadCount = new long[NUMBER_OF_READ_BUFFERS];
        	readBufferWriteCount = new PaddedAtomicLong[NUMBER_OF_READ_BUFFERS];
        	readBufferDrainAtWriteCount = new PaddedAtomicLong[NUMBER_OF_READ_BUFFERS];
            // todo: work out how to initialise
        	readBuffers = new PaddedAtomicReference<Node>[NUMBER_OF_READ_BUFFERS][];
        	for (int i = 0; i < NUMBER_OF_READ_BUFFERS; i++) {
            	readBufferWriteCount[i] = new PaddedAtomicLong();
            	readBufferDrainAtWriteCount[i] = new PaddedAtomicLong();
            	readBuffers[i] = new PaddedAtomicReference<Node>[READ_BUFFER_SIZE];
            	for (int j = 0; j < READ_BUFFER_SIZE; j++) {
                	readBuffers[i][j] = new PaddedAtomicReference<Node>();
                }
            }

            // The notification queue and listener
        	listener = builder.listener;
        	if (listener is DiscardingListener<K,V>) {
            	pendingNotifications = new DiscardingQueue<Node> ();
            } else {
            	pendingNotifications = new Deque<Node> ();
            }
        }

        /*        * Ensures that the object is not null. */
    	private static void checkNotNull(Object o) {
        	if (o == null) {
            	throw new ArgumentNullException();
            }
        }

        /*        * Ensures that the argument expression is true. */
    	private static void checkArgument(bool expression) {
        	if (!expression) {
            	throw new ArgumentException();
            }
        }

        /*        * Ensures that the argument expression is true. */
    	private static void checkArgumentRange(bool expression) {
        	if (!expression) {
            	throw new ArgumentOutOfRangeException();
            }
        }

        /*        * Ensures that the state expression is true. */
    	private static void checkState(bool expression) {
        	if (!expression) {
            	throw new InvalidOperationException();
            }
        }

        /*         ---------------- Eviction Support -------------- */

        /**
        * Retrieves the maximum weighted capacity of the map.
        *
        * @return the maximum weighted capacity
            */
        	public long Capacity() {
        	return capacity.GetValue();
        }

        /*        *
   * Sets the maximum weighted capacity of the map and eagerly evicts entries
   * until it shrinks to the appropriate size.
   *
   * @param capacity the maximum weighted capacity of the map
   * @throws IllegalArgumentException if the capacity is negative
   */
    	public void setCapacity(long capacity) {
        	checkArgumentRange(capacity >= 0);
        	evictionLock.EnterWriteLock ();
        	try
            {
                // no lazy set :(
            	this.capacity.LazySet (Math.Min (capacity, MAXIMUM_CAPACITY));
            	DrainBuffers();
            	evict();
            }
        	finally {
            	evictionLock.ExitWriteLock ();
            }
        	notifyListener();
        }

        /*        * Determines whether the map has exceeded its capacity. */
        //@GuardedBy("evictionLock")
    	bool hasOverflowed() {
        	return weightedSize.GetValue() > capacity.GetValue();
        }

        /*        *
   * Evicts entries from the map while it exceeds the capacity and appends
   * evicted entries to the notification queue for processing.
   */
        //@GuardedBy("evictionLock")
    	void evict() {
            // Attempts to evict entries from the map if it exceeds the maximum
            // capacity. If the eviction fails due to a concurrent removal of the
            // victim, that removal may cancel out the addition that triggered this
            // eviction. The victim is eagerly unlinked before the removal task so
            // that if an eviction is still required then a new victim will be chosen
            // for removal.
        	while (hasOverflowed()) {
            	Node node = evictionDeque.Peek ();

                // If weighted values are used, then the pending operations will adjust
                // the size to reflect the correct weight
            	if (node == null) {
                	return;
                }

                // need to dequeue that peek - i hate .net queue api. why is there no TryDequeue??
            	node = evictionDeque.Dequeue ();

                // Notify the listener only if the entry was evicted
            	if (DataTryRemove(node.Key, node)) {
                	pendingNotifications.Enqueue(node);
                }

            	makeDead(node);
            }
        }

    	private bool DataTryRemove(K key, Node value) {
        	return ((ICollection<KeyValuePair<K, Node>>)data).Remove(new KeyValuePair<K,Node>(key, value));
        }

        /*        *
   * Performs the post-processing work required after a read.
   *
   * @param node the entry in the page replacement policy
   */
    	internal void afterRead(Node node) {
             int bufferIndex = readBufferIndex();
         long writeCount = recordRead(bufferIndex, node);
        	drainOnReadIfNeeded(bufferIndex, writeCount);
        	notifyListener();
        }

        /*        * Returns the index to the read buffer to record into. */
    	internal static int readBufferIndex() {
            // A buffer is chosen by the thread's id so that tasks are distributed in a
            // pseudo evenly manner. This helps avoid hot entries causing contention
            // due to other threads trying to append to the same buffer.

        	return Thread.CurrentThread.ManagedThreadId & READ_BUFFERS_MASK;
        }

        /*        *
   * Records a read in the buffer and return its write count.
   *
   * @param bufferIndex the index to the chosen read buffer
   * @param node the entry in the page replacement policy
   * @return the number of writes on the chosen read buffer
   */
    	long recordRead(int bufferIndex, Node node) {
            // The location in the buffer is chosen in a racy fashion as the increment
            // is not atomic with the insertion. This means that concurrent reads can
            // overlap and overwrite one another, resulting in a lossy buffer.
        	var writeCount = readBufferWriteCount [bufferIndex].GetValue ();
        	readBufferWriteCount [bufferIndex].SetValue (writeCount + 1);

        	int index = (int) (writeCount & READ_BUFFER_INDEX_MASK);
        	readBuffers[bufferIndex][index].LazySet(node);

        	return writeCount;
        }

        /*        *
   * Attempts to drain the buffers if it is determined to be needed when
   * post-processing a read.
   *
   * @param bufferIndex the index to the chosen read buffer
   * @param writeCount the number of writes on the chosen read buffer
   */
    	void drainOnReadIfNeeded(int bufferIndex, long writeCount) {
        	long pending = (writeCount - readBufferDrainAtWriteCount[bufferIndex].GetValue());
        	bool delayable = (pending < READ_BUFFER_THRESHOLD);
        	string status = drainStatus.GetValue();
        	if (ShouldDrainBuffers(status, delayable)) {
            	tryToDrainBuffers();
            }
        }

        /*        *
   * Performs the post-processing work required after a write.
   *
   * @param task the pending operation to be applied
   */
    	internal void afterWrite(Action task) {
        	writeBuffer.Enqueue(task);
        	drainStatus.LazySet(DrainStatus.REQUIRED);
        	tryToDrainBuffers();
        	notifyListener();
        }

        /*        *
   * Attempts to acquire the eviction lock and apply the pending operations, up
   * to the amortized threshold, to the page replacement policy.
   */
    	internal void tryToDrainBuffers() {
        	if (evictionLock.TryEnterWriteLock(0)) {
            	try {
                	drainStatus.LazySet(DrainStatus.PROCESSING);
                	DrainBuffers();
                } finally {
                	drainStatus.CompareAndSet(DrainStatus.PROCESSING, DrainStatus.IDLE);
                    	evictionLock.ExitWriteLock();
                }
            }
        }

        /*        * Drains the read and write buffers up to an amortized threshold. */
        //@GuardedBy("evictionLock")
    	internal void DrainBuffers() {
        	drainReadBuffers();
        	drainWriteBuffer();
        }

        /*        * Drains the read buffers, each up to an amortized threshold. */
        //@GuardedBy("evictionLock")
    	void drainReadBuffers() {
        	int start = Thread.CurrentThread.ManagedThreadId;
            int end = start + NUMBER_OF_READ_BUFFERS;
        	for (int i = start; i < end; i++) {
            	drainReadBuffer(i & READ_BUFFERS_MASK);
            }
        }

        /*        * Drains the read buffer up to an amortized threshold. */
        //@GuardedBy("evictionLock")
    	void drainReadBuffer(int bufferIndex) {
        	long writeCount = readBufferWriteCount [bufferIndex].GetValue ();
        	for (int i = 0; i < READ_BUFFER_DRAIN_THRESHOLD; i++) {
            	int index = (int) (readBufferReadCount[bufferIndex] & READ_BUFFER_INDEX_MASK);
                PaddedAtomicReference<Node> slot = readBuffers[bufferIndex][index];
            	Node node = slot.GetValue();
            	if (node == null) {
                	break;
                }

            	slot.LazySet(null);
            	applyRead(node);
            	readBufferReadCount[bufferIndex]++;
            }
            // unfortunately we don't have a lazySet operation
        	readBufferDrainAtWriteCount [bufferIndex].LazySet(writeCount);
        }

        /*        * Updates the node's location in the page replacement policy. */
        //@GuardedBy("evictionLock")
    	void applyRead(Node node) {
            // An entry may be scheduled for reordering despite having been removed.
            // This can occur when the entry was concurrently read while a writer was
            // removing it. If the entry is no longer linked then it does not need to
            // be processed.
        	if (evictionDeque.Contains(node)) {
            	evictionDeque.moveToBack(node);
            }
        }

        /*        * Drains the read buffer up to an amortized threshold. */
        //@GuardedBy("evictionLock")
    	void drainWriteBuffer() {
        	for (int i = 0; i < WRITE_BUFFER_DRAIN_THRESHOLD; i++) {
            	Action task;
            	if (!writeBuffer.TryDequeue(out task)) {
                	break;
                }
            	task();
            }
        }

        /*        *
   * Attempts to transition the node from the <tt>alive</tt> state to the
   * <tt>retired</tt> state.
   *
   * @param node the entry in the page replacement policy
   * @param expect the expected weighted value
   * @return if successful
   */
    	internal bool tryToRetire(Node node, WeightedValue expect) {

        	if (expect.isAlive()) {
            	WeightedValue retired = new WeightedValue(expect.value, -expect.weight);
            	return node.CompareAndSet(expect, retired);
            }
        	return false;
        }

        /*        *
   * Atomically transitions the node from the <tt>alive</tt> state to the
   * <tt>retired</tt> state, if a valid transition.
   *
   * @param node the entry in the page replacement policy
   */
    	internal void makeRetired(Node node) {
        	for (;;) {
            	WeightedValue current = node.GetValue();
            	if (!current.isAlive()) {
                	return;
                }
            	WeightedValue retired = new WeightedValue(current.value, -current.weight);
            	if (node.CompareAndSet(current, retired)) {
                	return;
                }
            }
        }

        /*        *
   * Atomically transitions the node to the <tt>dead</tt> state and decrements
   * the <tt>weightedSize</tt>.
   *
   * @param node the entry in the page replacement policy
   */
        //@GuardedBy("evictionLock")
    	void makeDead(Node node) {
        	for (;;) {
            	WeightedValue current = node.GetValue();
            	WeightedValue dead = new WeightedValue(current.value, 0);
            	if (node.CompareAndSet(current, dead)) {
                	weightedSize.LazySet(weightedSize.GetValue() - Math.Abs(current.weight));
                	return;
                }
            }
        }

        /*        * Notifies the listener of entries that were evicted. */
    	void notifyListener() {
        	while (!pendingNotifications.IsEmpty) {
                var node = pendingNotifications.Dequeue();
            	listener.onEviction(node.Key, node.Value);
            }
        }

    	interface Runnable
        {
        	void Run ();
        }

    	private Action AddTask(Node node, int weight) {
        	return () => {
            	weightedSize.LazySet (weightedSize.GetValue () + weight);

                // ignore out-of-order write operations
            	if (node.GetValue ().isAlive ()) {
                	evictionDeque.Enqueue (node);
                	evict ();
                }
            };
        }

    	private Action RemovalTask(Node node) {
        	return () => {
                // add may not have been processed yet
            	evictionDeque.Remove (node);
            	makeDead (node);
            };
        }

    	private Action UpdateTask(Node node, int weightDifference) {
        	return () => {
            	weightedSize.LazySet (weightedSize.GetValue () + weightDifference);
            	applyRead (node);
            	evict ();
            };
        }

        #region IDictionary implementation

    	public void Add (K key, V value)
        {
        	throw new NotImplementedException ();
        }

    	public bool ContainsKey (K key)
        {
        	return data.ContainsKey(key);
        }

    	public bool Remove (K key)
        {
        	throw new NotImplementedException ();
        }

    	public bool TryGetValue (K key, out V value)
        {
        	Node node;
        	if (!data.TryGetValue (key, out node)) {
            	value = default(V);
            	return false;
            }
        	afterRead(node);
        	value = node.GetValue().value;
        	return true;
        }

    	public V this [K index] {
        	get {
            	Node node;
            	if (!data.TryGetValue (index, out node)) {
                	throw new ArgumentException ("Not found: " + index);
                }
            	afterRead(node);
            	return node.GetValue().value;
            }
        	set {
            	put(index, value, false);
            }
        }

    	public ICollection<K> Keys {
        	get {
            	return data.Keys;
            }
        }

    	public ICollection<V> Values {
        	get {
            	ICollection<V> vs = values;
            	return (vs == null) ? (values = new InternalValues(this)) : vs;
            }
        }

    	public ICollection<KeyValuePair<K,V>> Entries {
        	get {
            	HashSet<KeyValuePair<K,V>> ret = new HashSet<KeyValuePair<K, V>> ();
            	foreach (var kvp in this) {
                	ret.Add (kvp);
                }
            	return ret;
            }
        }

        #endregion

        #region ICollection implementation

    	public void Add (KeyValuePair<K, V> item)
        {
        	throw new NotImplementedException ();
        }

    	public void Clear ()
        {
            evictionLock.EnterWriteLock();
        	try {
                // Discard all entries
            	Node node;
            	while (evictionDeque.Peek() != null) {
                	node = evictionDeque.Dequeue();
                	DataTryRemove (node.Key, node);
                	makeDead(node);
                }

                // Discard all pending reads
            	foreach (AtomicReference<Node>[] buffer in readBuffers) {
                	foreach (AtomicReference<Node> slot in buffer) {
                    	slot.LazySet(null);
                    }
                }

                // Apply all pending writes
            	Action task;
            	while (writeBuffer.TryDequeue(out task)) {
                	task();
                }
            }
            finally{
                	evictionLock.ExitWriteLock();
            }
        }

    	public bool Contains (KeyValuePair<K, V> item)
        {
        	throw new NotImplementedException ();
        }

        // todo: should provide a ToArray() method, since the one in IEnumerable is not threadsafe, but we could create
        //       a thread-safe one using an internal temporary list which is later converted to array.
        //       nicely demonstrated by our MultiThreadedTest..
    	public void CopyTo (KeyValuePair<K, V>[] array, int arrayIndex)
        {
            // does't this need to be threadsafe? presumably it will if it uses an enumerator..
        	var e = GetEnumerator ();
        	while (e.MoveNext ()) {
            	if (arrayIndex >= array.Length) {
                	throw new ArgumentOutOfRangeException ("Have more elements than will fit in array");
                }
            	array [arrayIndex++] = e.Current;
            }
        }

    	public bool Remove (KeyValuePair<K, V> item)
        {
        	throw new NotImplementedException ();
        }

    	public int Count {
        	get {
            	return data.Count;
            }
        }

    	public bool IsReadOnly {
        	get {
            	return false;
            }
        }

        #endregion

        #region IEnumerable implementation

    	public IEnumerator<KeyValuePair<K, V>> GetEnumerator ()
        {
        	return new InternalKeyValueEnumerator (data.GetEnumerator());
        }

    	public IEnumerator<Entry<K, V>> GetEntryEnumerator ()
        {
        	return new InternalEntryEnumerator (this);
        }

        #endregion

        #region IEnumerable implementation

    	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
        	return new InternalKeyValueEnumerator (data.GetEnumerator());
        }

        #endregion

        // ---------- ConcurrentDictionary API equivalence




        /*        *
   * Returns the weighted size of this map.
   *
   * @return the combined weight of the values in this map
   */
    	public long WeightedSize() {
        	return Math.Max(0, weightedSize.GetValue());
        }



        //@Override
    	public bool containsValue(Object value) {
        	checkNotNull(value);

        	return data.Any ((kvp) => Equals(kvp.Value.Value, value));
        }

        /// <summary>
        /// Returns the value to which the specified key is mapped, or {@code null}
        /// if this map contains no mapping for the key. This method differs from
        /// {@link #get(Object)} in that it does not record the operation with the
        /// page replacement policy.
        ///
        /// @param key the key whose associated value is to be returned
        /// @return the value to which the specified key is mapped, or
        ///    {@code null} if this map contains no mapping for the key
        /// @throws NullPointerException if the specified key is null
        /// </summary>
    	public V GetQuietly(K key) {
        	Node node;
        	if (!data.TryGetValue(key, out node)) {
            	return default(V);
            }
        	return node.GetValue ().value;
        }

        //@Override
    	public V put(K key, V value) {
        	return put(key, value, false);
        }

        //@Override
    	public V putIfAbsent(K key, V value) {
        	return put(key, value, true);
        }

        /*        *
   * Adds a node to the list and the data store. If an existing node is found,
   * then its value is updated if allowed.
   *
   * @param key key with which the specified value is to be associated
   * @param value value to be associated with the specified key
   * @param onlyIfAbsent a write is performed only if the key is not already
   *     associated with a value
   * @return the prior value in the data store or null if no mapping was found
   */
    	V put(K key, V value, bool onlyIfAbsent) {
        	checkNotNull(key);
        	checkNotNull(value);

             int weight = weigher.weightOf(key, value);
             WeightedValue weightedValue = new WeightedValue(value, weight);
             Node node = new Node(key, weightedValue);

        	for (;;) {
            	Node prior = data.GetOrAdd(node.Key, node);
            	if (prior == node) { // ie added
                	afterWrite(AddTask(node, weight));
                	return default(V);
                } else if (onlyIfAbsent) {
                	afterRead(prior);
                	return prior.GetValue().value;
                }
            	for (;;) {
                	WeightedValue oldWeightedValue = prior.GetValue();
                	if (!oldWeightedValue.isAlive()) {
                    	break;
                    }

                	if (prior.CompareAndSet(oldWeightedValue, weightedValue)) {
                         int weightedDifference = weight - oldWeightedValue.weight;
                    	if (weightedDifference == 0) {
                        	afterRead(prior);
                        } else {
                        	afterWrite(UpdateTask(prior, weightedDifference));
                        }
                    	return oldWeightedValue.value;
                    }
                }
            }
        }

        //@Override
    	public V remove(K key) {
        	Node node;
        	if (!data.TryRemove(key, out node)) {
            	return default(V);
            }

        	makeRetired(node);
        	afterWrite (RemovalTask (node));
        	return node.GetValue().value;
        }

        //@Override
    	public bool remove(K key, V value) {
        	Node node;
        	data.TryGetValue (key, out node);
        	if ((node == null) || (value == null)) {
            	return false;
            }

        	WeightedValue weightedValue = node.GetValue();
        	for (;;) {
            	if (weightedValue.contains(value)) {
                	if (tryToRetire(node, weightedValue)) {
                    	if (DataTryRemove(node.Key, node)) {
                        	afterWrite(RemovalTask(node));
                        	return true;
                        }
                    } else {
                    	weightedValue = node.GetValue();
                    	if (weightedValue.isAlive()) {
                            // retry as an intermediate update may have replaced the value with
                            // an equal instance that has a different reference identity
                        	continue;
                        }
                    }
                }
            	return false;
            }
        }

        //@Override
    	public V replace(K key, V value) {
        	checkNotNull(key);
        	checkNotNull(value);

             int weight = weigher.weightOf(key, value);
             WeightedValue weightedValue = new WeightedValue(value, weight);

        	Node node;
        	if (!data.TryGetValue (key, out node)) {
            	return default(V);
            }
        	for (;;) {
            	WeightedValue oldWeightedValue = node.GetValue();
            	if (!oldWeightedValue.isAlive()) {
                	return default(V);
                }
            	if (node.CompareAndSet(oldWeightedValue, weightedValue)) {
                     int weightedDifference = weight - oldWeightedValue.weight;
                	if (weightedDifference == 0) {
                    	afterRead(node);
                    } else {
                    	afterWrite(UpdateTask(node, weightedDifference));
                    }
                	return oldWeightedValue.value;
                }
            }
        }

        //@Override
    	public bool replace(K key, V oldValue, V newValue) {
        	checkNotNull(key);
        	checkNotNull(oldValue);
        	checkNotNull(newValue);

             int weight = weigher.weightOf(key, newValue);
             WeightedValue newWeightedValue = new WeightedValue(newValue, weight);

        	Node node;
        	if (!data.TryGetValue(key, out node)) {
            	return false;
            }
        	for (;;) {
            	WeightedValue weightedValue = node.GetValue();
            	if (!weightedValue.isAlive() || !weightedValue.contains(oldValue)) {
                	return false;
                }
            	if (node.CompareAndSet(weightedValue, newWeightedValue)) {
                     int weightedDifference = weight - weightedValue.weight;
                	if (weightedDifference == 0) {
                    	afterRead(node);
                    } else {
                    	afterWrite(UpdateTask(node, weightedDifference));
                    }
                	return true;
                }
            }
        }
        /*
        //@Override
    	public ISet<K> KeySet() {
        	ISet<K> ks = keySet;
        	return (ks == null) ? (keySet = new KeySet()) : ks;
        }*/

        /*        *
   * Returns a unmodifiable snapshot {@link Set} view of the keys contained in
   * this map. The set's iterator returns the keys whose order of iteration is
   * the ascending order in which its entries are considered eligible for
   * retention, from the least-likely to be retained to the most-likely.
   * <p>
   * Beware that, unlike in {@link #keySet()}, obtaining the set is <em>NOT</em>
   * a constant-time operation. Because of the asynchronous nature of the page
   * replacement policy, determining the retention ordering requires a traversal
   * of the keys.
   *
   * @return an ascending snapshot view of the keys in this map
            */
    	public ISet<K> AscendingKeySet() {
        	return AscendingKeySetWithLimit(int.MaxValue);
        }

        /*        *
   * Returns an unmodifiable snapshot {@link Set} view of the keys contained in
   * this map. The set's iterator returns the keys whose order of iteration is
   * the ascending order in which its entries are considered eligible for
   * retention, from the least-likely to be retained to the most-likely.
   * <p>
   * Beware that, unlike in {@link #keySet()}, obtaining the set is <em>NOT</em>
   * a constant-time operation. Because of the asynchronous nature of the page
   * replacement policy, determining the retention ordering requires a traversal
   * of the keys.
   *
   * @param limit the maximum size of the returned set
   * @return a ascending snapshot view of the keys in this map
   * @throws IllegalArgumentException if the limit is negative
   */
    	public ISet<K> AscendingKeySetWithLimit(int limit) {
        	return OrderedKeySet(true, limit);
        }

        /*        *
   * Returns an unmodifiable snapshot {@link Set} view of the keys contained in
   * this map. The set's iterator returns the keys whose order of iteration is
   * the descending order in which its entries are considered eligible for
   * retention, from the most-likely to be retained to the least-likely.
   * <p>
   * Beware that, unlike in {@link #keySet()}, obtaining the set is <em>NOT</em>
   * a constant-time operation. Because of the asynchronous nature of the page
   * replacement policy, determining the retention ordering requires a traversal
   * of the keys.
   *
   * @return a descending snapshot view of the keys in this map
            */
    	public ISet<K> DescendingKeySet() {
        	return DescendingKeySetWithLimit(int.MaxValue);
        }

        /*        *
   * Returns an unmodifiable snapshot {@link Set} view of the keys contained in
   * this map. The set's iterator returns the keys whose order of iteration is
   * the descending order in which its entries are considered eligible for
   * retention, from the most-likely to be retained to the least-likely.
   * <p>
   * Beware that, unlike in {@link #keySet()}, obtaining the set is <em>NOT</em>
   * a constant-time operation. Because of the asynchronous nature of the page
   * replacement policy, determining the retention ordering requires a traversal
   * of the keys.
   *
   * @param limit the maximum size of the returned set
   * @return a descending snapshot view of the keys in this map
   * @throws IllegalArgumentException if the limit is negative
                */
    	public ISet<K> DescendingKeySetWithLimit(int limit) {
        	return OrderedKeySet(false, limit);
        }

    	internal ISet<K> OrderedKeySet(bool ascending, int limit) {
        	checkArgumentRange(limit >= 0);
        	evictionLock.EnterWriteLock ();
        	try
            {
            	DrainBuffers();

//            	int initialCapacity = (weigher == Weighers.EntrySingleton<>())
//                    ? Math.Min(limit, (int) WeightedSize())
//                                  : 16;
                //int initialCapacity = Math.Min (limit, Count);
            	ISet<K> keys = new SortedSet<K> ();
            	IEnumerator<Node> iterator = ascending
                    ? (IEnumerator<Node>) evictionDeque.GetEnumerator()
                                                 : evictionDeque.GetDescendingEnumerator();
            	while (iterator.MoveNext()  && (limit > keys.Count)) {
                	keys.Add(iterator.Current.Key);
                }
            	return keys;
                // todo: readonly wrapper!
                //return unmodifiableSet(keys);
            }
        	finally {
            	evictionLock.ExitWriteLock ();
            }
        }
        /*
        //@Override
    	public ICollection<V> Values() {
        	ICollection<V> vs = values;
        	return (vs == null) ? (values = new Values()) : vs;
        }

        //@Override
    	public ISet<KeyValuePair<K, V>> EntrySet() {
        	ISet<KeyValuePair<K, V>> es = entrySet;
        	return (es == null) ? (entrySet = new EntrySet()) : es;
        }*/

        /*        *
   * Returns an unmodifiable snapshot {@link Map} view of the mappings contained
   * in this map. The map's collections return the mappings whose order of
   * iteration is the ascending order in which its entries are considered
   * eligible for retention, from the least-likely to be retained to the
   * most-likely.
   * <p>
   * Beware that obtaining the mappings is <em>NOT</em> a constant-time
   * operation. Because of the asynchronous nature of the page replacement
   * policy, determining the retention ordering requires a traversal of the
   * entries.
   *
   * @return a ascending snapshot view of this map
                */
            	public IDictionary<K, V> AscendingDictionary() {
            	return AscendingDictionaryWithLimit(int.MaxValue);
            }

        /*        *
   * Returns an unmodifiable snapshot {@link Map} view of the mappings contained
   * in this map. The map's collections return the mappings whose order of
   * iteration is the ascending order in which its entries are considered
   * eligible for retention, from the least-likely to be retained to the
   * most-likely.
   * <p>
   * Beware that obtaining the mappings is <em>NOT</em> a constant-time
   * operation. Because of the asynchronous nature of the page replacement
   * policy, determining the retention ordering requires a traversal of the
   * entries.
   *
   * @param limit the maximum size of the returned map
   * @return a ascending snapshot view of this map
   * @throws IllegalArgumentException if the limit is negative
   */
    	public IDictionary<K, V> AscendingDictionaryWithLimit(int limit) {
        	return orderedMap(true, limit);
        }

        /*        *
   * Returns an unmodifiable snapshot {@link Map} view of the mappings contained
   * in this map. The map's collections return the mappings whose order of
   * iteration is the descending order in which its entries are considered
   * eligible for retention, from the most-likely to be retained to the
   * least-likely.
   * <p>
   * Beware that obtaining the mappings is <em>NOT</em> a constant-time
   * operation. Because of the asynchronous nature of the page replacement
   * policy, determining the retention ordering requires a traversal of the
   * entries.
   *
   * @return a descending snapshot view of this map
   */
    	public IDictionary<K, V> DescendingDictionary() {
        	return DescendingDictionaryWithLimit(int.MaxValue);
        }

        /*        *
   * Returns an unmodifiable snapshot {@link Map} view of the mappings contained
   * in this map. The map's collections return the mappings whose order of
   * iteration is the descending order in which its entries are considered
   * eligible for retention, from the most-likely to be retained to the
   * least-likely.
   * <p>
   * Beware that obtaining the mappings is <em>NOT</em> a constant-time
   * operation. Because of the asynchronous nature of the page replacement
   * policy, determining the retention ordering requires a traversal of the
   * entries.
   *
   * @param limit the maximum size of the returned map
   * @return a descending snapshot view of this map
   * @throws IllegalArgumentException if the limit is negative
   */
    	public IDictionary<K, V> DescendingDictionaryWithLimit(int limit) {
        	return orderedMap(false, limit);
        }

    	IDictionary<K, V> orderedMap(bool ascending, int limit) {
        	checkArgumentRange(limit >= 0);
        	evictionLock.EnterWriteLock ();
        	try
            {
            	DrainBuffers();

                //int initialCapacity = (weigher == Weighers.entrySingleton())
                //                ? Math.min(limit, (int) weightedSize())
                //                : 16;
            	IDictionary<K, V> map = new SortedDictionary<K, V> ();
            	IEnumerator<Node> iterator = ascending
                    ? (IEnumerator<Node>) evictionDeque.GetEnumerator()
                                                   : evictionDeque.GetDescendingEnumerator();
            	while (iterator.MoveNext() && (limit > map.Count)) {
                	Node node = iterator.Current;
                	map[node.Key] = node.Value;
                }
            	return map;
                // todo: readonly map
                //return unmodifiableMap(map);
            } 
        	finally {
            	evictionLock.ExitWriteLock ();
            }
        }

    	private bool ShouldDrainBuffers(string status, bool delayable) 
        {
        	switch (status) {
        	case DrainStatus.IDLE:
            	return !delayable;
        	case DrainStatus.REQUIRED:
            	return true;
        	case DrainStatus.PROCESSING:
            	return false;
            }
        	throw new NotSupportedException ("DrainStatus."+status);
        }

        /*        * The draining status of the buffers. */
    	internal class DrainStatus  {

            /*            * A drain is not taking place. */
        	public const string IDLE = "IDLE";

            /*            * A drain is required due to a pending write modification. */
        	public const string REQUIRED  = "REQUIRED";

            /*            * A drain is in progress. */
        	public const string PROCESSING = "PROCESSING";

        }

        /*        * A value, its weight, and the entry's status. */
        //@Immutable
    	internal sealed class WeightedValue {
        	internal readonly int weight;
        	internal readonly V value;

        	internal WeightedValue(V value, int weight) {
            	this.weight = weight;
            	this.value = value;
            }

        	internal bool contains(V o) {
            	return (Object.ReferenceEquals(o, value)) || value.Equals(o);
            }

            /*            *
     * If the entry is available in the hash-table and page replacement policy.
     */
        	internal bool isAlive() {
            	return weight > 0;
            }

            /*            *
     * If the entry was removed from the hash-table and is awaiting removal from
     * the page replacement policy.
     */
        	internal bool isRetired() {
            	return weight < 0;
            }

            /*            *
     * If the entry was removed from the hash-table and the page replacement
     * policy.
     */
        	internal bool isDead() {
            	return weight == 0;
            }
        }

        /*        *
   * A node contains the key, the weighted value, and the linkage pointers on
   * the page-replacement algorithm's data structures.
   */
        //@SuppressWarnings("serial")
    	internal sealed class Node : AtomicReference<WeightedValue>, ILinked<Node> {
            readonly K key;
            //@GuardedBy("evictionLock")
        	Node prev;
            //@GuardedBy("evictionLock")
        	Node next;

            /*            * Creates a new, unlinked node. */
        	internal Node(K key, WeightedValue weightedValue) : base(weightedValue) {
            	this.key = key;
            }

            //@Override
            //@GuardedBy("evictionLock")
        	public Node Previous {
            	get { return prev; }
            	set { prev = value; }
            }
        	public Node Next {
            	get { return next; }
            	set { next = value; }
            }

            /*            * Retrieves the value held by the current <tt>WeightedValue</tt>. */
        	public V Value {
            	get { return GetValue().value; }
            }

        	public K Key {
            	get { return key; }
            }

        }

        /*        * An adapter to safely externalize the keys. */
    	sealed class InternalKeySet : AbstractSet<K> {
        	readonly ConcurrentLinkedDictionary<K, V> map;

        	InternalKeySet(ConcurrentLinkedDictionary<K,V> map) {
            	this.map = map;
            }

            //@Override
        	public int size() {
            	return map.Count;
            }

            //@Override
        	public void clear() {
            	map.Clear();
            }
            /*
            //@Override
        	public IEnumerator<K> iterator() {
            	return new InternalKeyEnumerator();
            }

            //@Override
        	public bool contains(Object obj) {
            	return containsKey(obj);
            }

            //@Override
        	public bool remove(Object obj) {
            	return (map.remove(obj) != null);
            }

            //@Override
        	public Object[] toArray() {
            	return map.data.keySet().toArray();
            }

            //@Override
        	public K[] toArray(K[] array) {
            	return map.data.keySet().toArray(array);
            }*/
        }

        /*        * An adapter to safely externalize the key iterator. */
        /*
    	sealed class InternalKeyEnumerator : IEnumerator<K> {
        	readonly IEnumerator<K> iterator = data.keySet().iterator();
        	K current;

        	readonly ConcurrentLinkedDictionary<K,V> map;
        	public InternalKeyEnumerator(ConcurrentLinkedDictionary<K,V> map) {
            	this.map = map;
            }

            //@Override
        	public bool hasNext() {
            	return iterator.hasNext();
            }

            //@Override
        	public K next() {
            	current = iterator.next();
            	return current;
            }

            //@Override
        	public void remove() {
            	checkState(current != null);
            	map.remove(current);
            	current = null;
            }
        }*/



        /*        * An adapter to safely externalize the values. */
    	sealed class InternalValues : AbstractCollection<V> {
        	readonly ConcurrentLinkedDictionary<K,V> map;
        	public InternalValues(ConcurrentLinkedDictionary<K,V> map) {
            	this.map = map;
            }

        	public override int Count {
            	get {
                	return map.Count;
                }
            }

            //@Override
        	public override void Clear() {
            	map.Clear();
            }

            //@Override
        	public override IEnumerator<V> GetEnumerator () {
            	return new InternalValueEnumerator(map);
            }

            //@Override
        	public override bool Contains (V item) {
            	return map.containsValue (item);
            }
        }

        /*        * An adapter to safely externalize the value iterator. */
    	sealed class InternalValueEnumerator : IEnumerator<V> {
        	readonly IEnumerator<Node> mapValuesEnumerator;
        	readonly ConcurrentLinkedDictionary<K,V> map;
        	public InternalValueEnumerator(ConcurrentLinkedDictionary<K,V> map) {
            	this.map = map;
            	mapValuesEnumerator = map.data.Values.GetEnumerator();
            }

            #region IEnumerator implementation

        	public bool MoveNext ()
            {
            	return mapValuesEnumerator.MoveNext ();
            }

        	public void Reset ()
            {
            	mapValuesEnumerator.Reset ();
            }

            object System.Collections.IEnumerator.Current {
            	get {
                	return mapValuesEnumerator.Current.GetValue ();
                }
            }

            #endregion

            #region IDisposable implementation

        	public void Dispose ()
            {
            	mapValuesEnumerator.Dispose ();
            }

            #endregion

            #region IEnumerator implementation

        	public V Current {
            	get {
                	return mapValuesEnumerator.Current.Value;
                }
            }

            #endregion

        }

        /*        * An adapter to safely externalize the entries. */
    	sealed class InternalEntrySet : ICollection<KeyValuePair<K, V>> {
        	readonly ConcurrentLinkedDictionary<K, V> map;

        	InternalEntrySet(ConcurrentLinkedDictionary<K,V> map) {
            	this.map = map;
            }

            #region ICollection implementation
        	public void Add (KeyValuePair<K, V> item)
            {
            	map.Add (item);
            }
        	public void Clear ()
            {
            	map.Clear ();
            }
        	public bool Contains (KeyValuePair<K, V> item)
            {
            	return map.Contains (item);
            }
        	public void CopyTo (KeyValuePair<K, V>[] array, int arrayIndex)
            {
            	map.CopyTo (array, arrayIndex);
            }
        	public bool Remove (KeyValuePair<K, V> item)
            {
            	return map.Remove (item);
            }
        	public int Count {
            	get {
                	return map.Count;
                }
            }
        	public bool IsReadOnly {
            	get {
                	return map.IsReadOnly;
                }
            }
            #endregion
            #region IEnumerable implementation
        	public IEnumerator<KeyValuePair<K, V>> GetEnumerator ()
            {
            	return new InternalKeyValueEnumerator(map.data.GetEnumerator());
            }
            #endregion
            #region IEnumerable implementation
        	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
            	return GetEnumerator ();
            }
            #endregion

        }

        /*        * An adapter to safely externalize the entry iterator. */
    	sealed class InternalKeyValueEnumerator : IEnumerator<KeyValuePair<K, V>> {
        	readonly IEnumerator<KeyValuePair<K,Node>> iterator;

        	internal InternalKeyValueEnumerator(IEnumerator<KeyValuePair<K,Node>> iterator) {
            	this.iterator = iterator;
            }

            #region IEnumerator implementation
        	public bool MoveNext ()
            {
            	return iterator.MoveNext ();
            }
        	public void Reset ()
            {
            	iterator.Reset ();
            }
        	object System.Collections.IEnumerator.Current {
            	get {
                	return Current;
                }
            }
            #endregion
            #region IDisposable implementation
        	public void Dispose ()
            {
            	iterator.Dispose ();
            }
            #endregion
            #region IEnumerator implementation
        	public KeyValuePair<K, V> Current {
            	get {
                	var c = iterator.Current;
                	return new KeyValuePair<K,V> (c.Key, c.Value.Value);
                }
            }
            #endregion


        }
        /*        * An adapter to safely externalize the entry iterator. */
    	sealed class InternalEntryEnumerator : IEnumerator<Entry<K, V>> {
        	readonly IEnumerator<KeyValuePair<K,Node>> iterator;

        	readonly ConcurrentLinkedDictionary<K, V> dictionary;

        	internal InternalEntryEnumerator(ConcurrentLinkedDictionary<K,V> dictionary) {
            	this.iterator = dictionary.data.GetEnumerator();
            	this.dictionary = dictionary;
            }

            #region IEnumerator implementation
        	public bool MoveNext ()
            {
            	return iterator.MoveNext ();
            }
        	public void Reset ()
            {
            	iterator.Reset ();
            }
        	object System.Collections.IEnumerator.Current {
            	get {
                	return Current;
                }
            }
            #endregion
            #region IDisposable implementation
        	public void Dispose ()
            {
            	iterator.Dispose ();
            }
            #endregion
            #region IEnumerator implementation
        	public Entry<K, V> Current {
            	get {
                	return new WriteThroughEntry (dictionary, iterator.Current);
                }
            }
            #endregion


        }



        /*        * An entry that allows updates to write through to the map. */
    	sealed class WriteThroughEntry : Entry<K, V> {
        	private ConcurrentLinkedDictionary<K,V> dictionary;
        	internal WriteThroughEntry(ConcurrentLinkedDictionary<K,V> dictionary, KeyValuePair<K,Node> node) : base(node.Key, node.Value.Value) 
            {
            	this.dictionary = dictionary;
            }

        	public override V Value {
            	get {
                	return base.Value;
                }
            	set {
                	dictionary.put (Key, value);
                	base.Value = value;
                }
            }
        }


        /*        * A queue that discards all additions and is always empty. */
    	sealed class DiscardingQueue<T> : AbstractQueue<T> {


        	public override T Dequeue ()
            {
            	return default(T);
            }
        	public override void Enqueue (T value)
            {
            }
        	public override T Peek ()
            {
            	return default(T);
            }
        	public override T[] ToArray ()
            {
            	return new T[0];
            }
        	public override bool IsEmpty {
            	get {
                	return true;
                }
            }
        }


        /*         ---------------- Serialization Support -------------- */

        /*
    	Object writeReplace() {
        	return new SerializationProxy<K, V>(this);
        }*/


        /*        *
   * A proxy that is serialized instead of the map. The page-replacement
   * algorithm's data structures are not serialized so the deserialized
   * instance contains only the entries. This is acceptable as caches hold
   * transient data that is recomputable and serialization would tend to be
   * used as a fast warm-up process.
   *
   * todo: implement this or not?
    	static sealed class SerializationProxy<K, V> implements Serializable {
        	final IEntryWeigher<? super K, ? super V> weigher;
        	final EvictionListener<K, V> listener;
        	final int concurrencyLevel;
        	final Map<K, V> data;
        	final long capacity;

        	SerializationProxy(ConcurrentLinkedHashMap<K, V> map) {
            	concurrencyLevel = map.concurrencyLevel;
            	data = new HashMap<K, V>(map);
            	capacity = map.capacity.get();
            	listener = map.listener;
            	weigher = map.weigher;
            }

        	Object readResolve() {
            	ConcurrentLinkedHashMap<K, V> map = new Builder<K, V>()
                    .concurrencyLevel(concurrencyLevel)
                    .maximumWeightedCapacity(capacity)
                    .listener(listener)
                    .weigher(weigher)
                    .build();
            	map.putAll(data);
            	return map;
            }

        	static final long serialVersionUID = 1;
        }*/

    }

    /*             ---------------- Builder -------------- */

    /**
    * A builder that creates {@link ConcurrentLinkedHashMap} instances. It
    * provides a flexible approach for constructing customized instances with
        * a named parameter syntax. It can be used in the following manner:
        * <pre>{@code
        * ConcurrentMap<Vertex, Set<Edge>> graph = new Builder<Vertex, Set<Edge>>()
        *     .maximumWeightedCapacity(5000)
        *     .weigher(Weighers.<Edge>set())
        *     .build();
        * }</pre>
    */
	public sealed class Builder<K, V> {
    	public static readonly int DEFAULT_CONCURRENCY_LEVEL = 16;
    	public static readonly int DEFAULT_INITIAL_CAPACITY = 16;

    	internal IEvictionListener<K, V> listener;
    	internal IEntryWeigher<K, V> weigher;

    	internal int concurrencyLevel;
    	internal int initialCapacity;
    	internal long capacity;

    	public Builder() {
        	capacity = -1;
        	weigher = Weighers.EntrySingleton<K,V>();
        	initialCapacity = DEFAULT_INITIAL_CAPACITY;
        	concurrencyLevel = DEFAULT_CONCURRENCY_LEVEL;
        	listener = new DiscardingListener<K,V>();
        }

        /*                    *
     * Specifies the initial capacity of the hash table (default <tt>16</tt>).
     * This is the number of key-value pairs that the hash table can hold
     * before a resize operation is required.
     *
     * @param initialCapacity the initial capacity used to size the hash table
     *     to accommodate this many entries.
     * @throws IllegalArgumentException if the initialCapacity is negative
     */
    	public Builder<K, V> InitialCapacity(int initialCapacity) {
        	checkArgumentRange(initialCapacity >= 0);
        	this.initialCapacity = initialCapacity;
        	return this;
        }

        /*                    *
     * Specifies the maximum weighted capacity to coerce the map to and may
     * exceed it temporarily.
     *
     * @param capacity the weighted threshold to bound the map by
     * @throws IllegalArgumentException if the maximumWeightedCapacity is
     *     negative
     */
    	public Builder<K, V> MaximumWeightedCapacity(long capacity) {
        	checkArgumentRange(capacity >= 0);
        	this.capacity = capacity;
        	return this;
        }

        /*                    *
     * Specifies the estimated number of concurrently updating threads. The
     * implementation performs internal sizing to try to accommodate this many
     * threads (default <tt>16</tt>).
     *
     * @param concurrencyLevel the estimated number of concurrently updating
     *     threads
     * @throws IllegalArgumentException if the concurrencyLevel is less than or
     *     equal to zero
     */
    	public Builder<K, V> ConcurrencyLevel(int concurrencyLevel) {
        	checkArgumentRange(concurrencyLevel > 0);
        	this.concurrencyLevel = concurrencyLevel;
        	return this;
        }

        /*                    *
     * Specifies an optional listener that is registered for notification when
     * an entry is evicted.
     *
     * @param listener the object to forward evicted entries to
     * @throws NullPointerException if the listener is null
     */
    	public Builder<K, V> Listener(IEvictionListener<K, V> listener) {
        	checkNotNull(listener);
        	this.listener = listener;
        	return this;
        }

        /*                    *
     * Specifies an algorithm to determine how many the units of capacity a
     * value consumes. The default algorithm bounds the map by the number of
     * key-value pairs by giving each entry a weight of <tt>1</tt>.
     *
     * @param weigher the algorithm to determine a value's weight
     * @throws NullPointerException if the weigher is null
     */
    	public Builder<K, V> Weigher(IWeigher<V> weigher) {
            // todo: used to be same as next method,not sure what it was trying to achieves
        	this.weigher = new BoundedEntryWeigher<K, V>(Weighers.AsEntryWeigher<K,V>(weigher));
        	return this;
        }

        /*                    *
     * Specifies an algorithm to determine how many the units of capacity an
     * entry consumes. The default algorithm bound the map by the number of
     * key-value pairs by giving each entry a weight of <tt>1</tt>.
     *
     * @param weigher the algorithm to determine a entry's weight
     * @throws NullPointerException if the weigher is null
     */
    	public Builder<K, V> Weigher(IEntryWeigher<K, V> weigher) {
        	this.weigher = // (weigher == Weighers.entrySingleton())
                               // ? Weighers.<K, V>entrySingleton() :
                               new BoundedEntryWeigher<K, V>(weigher);
        	return this;
        }

        /*                    *
     * Creates a new {@link ConcurrentLinkedHashMap} instance.
     *
     * @throws IllegalStateException if the maximum weighted capacity was
     *     not set
     */
    	public ConcurrentLinkedDictionary<K, V> Build() {
        	checkState(capacity >= 0);
        	return new ConcurrentLinkedDictionary<K, V>(this);
        }

        /*                * Ensures that the object is not null. */
    	private void checkNotNull(Object o) {
        	if (o == null) {
            	throw new ArgumentNullException();
            }
        }

        /*                * Ensures that the argument expression is true. */
    	private void checkArgument(bool expression) {
        	if (!expression) {
            	throw new ArgumentException();
            }
        }

        /*                * Ensures that the argument expression is true. */
    	private void checkArgumentRange(bool expression) {
        	if (!expression) {
            	throw new ArgumentOutOfRangeException();
            }
        }

        /*                * Ensures that the state expression is true. */
    	private void checkState(bool expression) {
        	if (!expression) {
            	throw new InvalidOperationException();
            }
        }
    }

	class AbstractQueue<T> : AbstractCollection<T>, IDeque<T>
    {
    	public virtual T Dequeue ()
        {
        	throw new NotImplementedException ();
        }
    	public virtual void Enqueue (T value)
        {
        	throw new NotImplementedException ();
        }
    	public virtual T Peek ()
        {
        	throw new NotImplementedException ();
        }
    	public virtual T[] ToArray ()
        {
        	throw new NotImplementedException ();
        }
    	public virtual bool IsEmpty {
        	get {
            	throw new NotImplementedException ();
            }
        }
    	public virtual IEnumerator<T> GetDescendingEnumerator()
        {
        	throw new NotImplementedException ();
        }


    }

	class AbstractSet<T> : AbstractCollection<T>, ISet<T> 
    {
    	bool ISet<T>.Add (T item)
        {
        	throw new NotImplementedException ();
        }

    	public void ExceptWith (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public void IntersectWith (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public bool IsProperSubsetOf (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public bool IsProperSupersetOf (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public bool IsSubsetOf (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public bool IsSupersetOf (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public bool Overlaps (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public bool SetEquals (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public void SymmetricExceptWith (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }

    	public void UnionWith (IEnumerable<T> other)
        {
        	throw new NotImplementedException ();
        }


    }

	abstract class AbstractCollection<V> : ICollection<V>, ICollection {

    	public virtual void Add (V item)
        {
        }

    	public virtual void Clear ()
        {

        }

    	public virtual bool Contains (V item)
        {
        	return false;
        }

    	public virtual void CopyTo (V[] array, int arrayIndex)
        {

        }

    	public virtual bool Remove (V item)
        {
        	return false;
        }

    	public virtual int Count {
        	get {
            	return 0;
            }
        }

    	public virtual bool IsReadOnly {
        	get {
            	return true;
            }
        }

    	public virtual IEnumerator<V> GetEnumerator ()
        {
        	throw new NotImplementedException ();
        }

         System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
        	return GetEnumerator ();
        }

    	public void CopyTo (Array array, int index)
        {
        	throw new NotImplementedException ();
        }

    	public bool IsSynchronized {
        	get {
            	throw new NotImplementedException ();
            }
        }

    	public object SyncRoot {
        	get {
            	throw new NotImplementedException ();
            }
        }
    }

	public class Entry<K,V> {
    	public Entry(K key, V value) {
        	Key = key;
        	Value = value;
        }
    	public virtual K Key {
        	get;
        	private set;
        }
    	public virtual V Value {
        	get;
        	set;
        }
    }
    /*            * A weigher that enforces that the weight falls within a valid range. */
	internal sealed class BoundedEntryWeigher<K,V> : IEntryWeigher<K, V> {
    	internal readonly IEntryWeigher<K, V> weigher;

    	internal BoundedEntryWeigher(IEntryWeigher<K, V> weigher) {
        	if (weigher == null)
            {
            	throw new ArgumentNullException();
            }
        	this.weigher = weigher;
        }

        //@Override
    	public int weightOf(K key, V value) {
        	int weight = weigher.weightOf(key, value);
        	if (weight < 1) {
            	throw new ArgumentException ();
            }
        	return weight;
        }

    	Object writeReplace() {
        	return weigher;
        }
    }
    /*            * A listener that ignores all notifications. */
	internal sealed class DiscardingListener<K,V> : IEvictionListener<K, V> {

    	public void onEviction(K key, V value) {}
    }

}

