using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using NUnit.Framework.Constraints;

namespace ConcurrentLinkedDictionary.Test
{
    /// <summary>
    /// A unit-test for the page replacement algorithm and its public methods.
    /// Original author: ben.manes@google.com (Ben Manes)
    /// Ported by: Simon Matic Langford    
    /// </summary>
    [TestFixture]
    [Category("development")]
    public class EvictionTest : AbstractTest
    {
        public EvictionTest() : base(TestType.Standard)
        {
        }

        /* ---------------- Capacity -------------- */

        [Test]
        [TestCaseSource("WarmedMap")]
        public void capacity_increase(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = immutableCopyOf(newWarmedMap());
            long newMaxCapacity = 2 * Capacity();

            map.setCapacity(newMaxCapacity);
            Assert.That(map, Is.EqualTo(expected));
            Assert.That(map.Capacity(), Is.EqualTo(newMaxCapacity));
        }

        protected static long MAXIMUM_CAPACITY = ConcurrentLinkedDictionary<int,int>.MAXIMUM_CAPACITY;

        [Test]
        [TestCaseSource("WarmedMap")]
        public void capacity_increaseToMaximum(ConcurrentLinkedDictionary<int, int> map) {
            map.setCapacity(MAXIMUM_CAPACITY);
            Assert.That(map.Capacity(), Is.EqualTo(MAXIMUM_CAPACITY));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void capacity_increaseAboveMaximum(ConcurrentLinkedDictionary<int, int> map) {
            map.setCapacity(MAXIMUM_CAPACITY + 1);
            Assert.That(map.Capacity(), Is.EqualTo(MAXIMUM_CAPACITY));
        }

        [Test]
        public void capacity_decrease() {
            checkDecreasedCapacity(Capacity() / 2);
        }

        [Test]
        public void capacity_decreaseToMinimum() {
            checkDecreasedCapacity(0);
        }

        private void checkDecreasedCapacity(long newMaxCapacity) {
            var map = new Builder<int, int>()
                .MaximumWeightedCapacity(Capacity())
                .Listener(listener)
                .Build();
            WarmUp(map, 1, Capacity());
            map.setCapacity(newMaxCapacity);


            Assert.That(map, validConcurrentLinkedDictionary<int,int>(), "is valid cld");
            Assert.That(map.Count, Is.EqualTo((int) newMaxCapacity), "map size is new max capacity");
            Assert.That(map.Capacity(), Is.EqualTo(newMaxCapacity), "map cap is new max cap");
            // todo: moq on mono
            Assert.That (listener.Evictions, HasCount ((int)(Capacity () - newMaxCapacity)), "correct num of evictions");
//            verify(listener, times((int) (Capacity() - newMaxCapacity))).onEviction(anyInt(), anyInt());
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void capacity_decreaseBelowMinimum(ConcurrentLinkedDictionary<int, int> map) {
            try {
                map.setCapacity(-1);
            } finally {
                Assert.That(map.Capacity(), Is.EqualTo(Capacity()));
            }
        }
        /* ---------------- Eviction -------------- */

        [Test]
        [TestCaseSource("Builder")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void evict_listenerFails(Builder<int, int> builder) {
            var listener = new FailingEvictionListener<int, int> ();
            ConcurrentLinkedDictionary<int, int> map = builder
                .MaximumWeightedCapacity(0)
                .Listener(listener)
                .Build();
            try {
                WarmUp(map, 1, Capacity());
            } finally {
                Assert.That(map, validConcurrentLinkedDictionary<int,int>());
            }
        }

        [Test]
        public void evict_alwaysDiscard() {
            ConcurrentLinkedDictionary<int, int> map = new Builder<int, int>()
                .MaximumWeightedCapacity(0)
                .Listener(listener)
                .Build();
            WarmUp(map, 1, 100);

            Assert.That(map, validConcurrentLinkedDictionary<int,int>());
            Assert.That (listener.Evictions, HasCount (100));
        }

        [Test]
        public void evict() {
            ConcurrentLinkedDictionary<int, int> map = new Builder<int, int>()
                .MaximumWeightedCapacity(10)
                .Listener(listener)
                .Build();
            WarmUp(map, 1, 20);

            Assert.That(map, validConcurrentLinkedDictionary<int,int>());
            Assert.That(map.Count, Is.EqualTo(10));
            Assert.That(map.WeightedSize(), Is.EqualTo(10L));
            Assert.That (listener.Evictions, HasCount (10));
        }

        [Test]
        [TestCaseSource("BuilderIntCollectionOfInt")]
        public void evict_weighted(Builder<int, ICollection<int>> builder) {
            ConcurrentLinkedDictionary<int, ICollection<int>> map = builder
                .Weigher(Weighers.Collection<int>())
                .MaximumWeightedCapacity(10)
                .Build();

            map.put(1, asList(1, 2));
            map.put(2, asList(3, 4, 5, 6, 7));
            map.put(3, asList(8, 9, 10));
            Assert.That(map.WeightedSize(), Is.EqualTo(10L));

            // evict (1)
            map.put(4, asList(11));
            Assert.That(map.ContainsKey(1), Is.EqualTo(false));
            Assert.That(map.WeightedSize(), Is.EqualTo(9L));

            // evict (2, 3)
            map.put(5, asList(12, 13, 14, 15, 16, 17, 18, 19, 20));
            Assert.That(map.WeightedSize(), Is.EqualTo(10L));

            Assert.That(map, validConcurrentLinkedDictionary<int,ICollection<int>>());
        }

        [Test]
        [TestCaseSource("Builder")]
        public void evict_maximumCapacity(Builder<int, int> builder) {
            ConcurrentLinkedDictionary<int, int> map = builder
                .MaximumWeightedCapacity(MAXIMUM_CAPACITY)
                .Build();
            map.put(1, 2);
            map.capacity.SetValue(MAXIMUM_CAPACITY);
            map.weightedSize.SetValue(MAXIMUM_CAPACITY);

            map.put(2, 3);
            Assert.That(map.WeightedSize(), Is.EqualTo(MAXIMUM_CAPACITY));
            Assert.That(map, Is.EqualTo(singletonMap(2, 3)));
        }

        [Test]
        public void evict_alreadyRemoved() {
            ConcurrentLinkedDictionary<int, int> map = new Builder<int, int>()
                .MaximumWeightedCapacity(1)
                .Listener(listener)
                .Build();
            map.put(0, 0);
            map.evictionLock.EnterWriteLock ();
            try
            {
                ConcurrentLinkedDictionary<int, int>.Node node = map.data[0];
                checkStatus(map, node, Status.ALIVE);
                new System.Threading.Thread (() => {
                    map.put(1,1);
                    Assert.That(map.remove(0), Is.EqualTo(0));
                }).Start ();
                waitUntil(() => !((IDictionary<int, int>) map).ContainsKey(0));
                checkStatus(map, node, Status.RETIRED);
                map.DrainBuffers();

                checkStatus(map, node, Status.DEAD);
                Assert.That(map.ContainsKey(1), Is.True);
                Assert.That (listener.Evictions.Count, Is.EqualTo (0));
            }
            finally {
                map.evictionLock.ExitWriteLock ();
            }
        }

        enum Status { ALIVE, RETIRED, DEAD }

        private static void checkStatus(ConcurrentLinkedDictionary<int, int> map,
            ConcurrentLinkedDictionary<int, int>.Node node, Status expected) {
            Assert.That(node.GetValue().isAlive(), (expected == Status.ALIVE) ? (Constraint)Is.True : Is.False);
            Assert.That(node.GetValue().isRetired(), (expected == Status.RETIRED) ? (Constraint)Is.True : Is.False);
            Assert.That(node.GetValue().isDead(), (expected == Status.DEAD) ? (Constraint)Is.True : Is.False);

            if (node.GetValue().isRetired() || node.GetValue().isDead()) {
                Assert.That(map.tryToRetire(node, node.GetValue()), Is.False);
            }
            if (node.GetValue().isDead()) {
                map.makeRetired(node);
                Assert.That(node.GetValue().isRetired(), Is.False);
            }
        }

        [Test]
        [TestCaseSource("Builder")]
        public void evict_lru(Builder<int, int> builder) {
            ConcurrentLinkedDictionary<int, int> map = builder
                .MaximumWeightedCapacity(10)
                .Build();
            WarmUp(map, 1, 10);
            checkContainsInOrder(map,  1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            // re-order
            checkReorder(map, asList(1, 2, 3), 4, 5, 6, 7, 8, 9, 10, 1, 2, 3);

            // evict 4, 5, 6
            checkEvict(map, asList(11, 12, 13), 7, 8, 9, 10, 1, 2, 3, 11, 12, 13);

            // re-order
            checkReorder(map, asList(7, 8, 9), 10, 1, 2, 3, 11, 12, 13, 7, 8, 9);

            // evict 9, 10, 1
            checkEvict(map, asList(14, 15, 16), 3, 11, 12, 13, 7, 8, 9, 14, 15, 16);

            Assert.That(map, validConcurrentLinkedDictionary<int,int>());
        }

        private void checkReorder(ConcurrentLinkedDictionary<int, int> map,
            IList<int> keys, params int[] expect) {
            foreach (int i in keys) {
                var a = map [i];
            }
            checkContainsInOrder(map, expect);
        }

        private void checkEvict(ConcurrentLinkedDictionary<int, int> map,
            IList<int> keys, params int[] expect) {
            foreach (int i in keys) {
                map.put(i, i);
            }
            checkContainsInOrder(map, expect);
        }

        private void checkContainsInOrder(ConcurrentLinkedDictionary<int, int> map,
            params int[] expect) {
            map.DrainBuffers();
            List<int> evictionList = new List<int> ();
            foreach (ConcurrentLinkedDictionary<int, int>.Node node in map.evictionDeque) {
                evictionList.Add(node.Key);
            }
            Assert.That(map.Count, Is.EqualTo(expect.Length));
            Assert.That(map.Keys, CollectionConstraints.ContainsInAnyOrder<int>(expect));
            Assert.That(evictionList, Is.EqualTo(asList(expect)));
        }

        /*
         TODO: Generators
        [Test]
        public void evict_efficiency() {
            IDictionary<string, string> expected = new CacheFactory()
                .MaximumCapacity((int) Capacity())
                .MakeCache(CacheType.LinkedHashMap_Lru_Sync);
            IDictionary<string, string> actual = new Builder<string, string>()
                .MaximumWeightedCapacity(Capacity())
                .Build();

            Generator generator = new ScrambledZipfianGenerator(10 * Capacity());
            List<String> workingSet = createWorkingSet(generator, 10 * (int) Capacity());

            EfficiencyRun runExpected = determineEfficiency(expected, workingSet);
            EfficiencyRun runActual = determineEfficiency(actual, workingSet);

            String reason = String.format("Expected [%s] but was [%s]", runExpected, runActual);
            Assert.That(runActual.hitCount, Is.EqualTo(runExpected.hitCount), reason);
        }*/

        [Test]
        [TestCaseSource("WarmedMap")]
        public void updateRecency_onGet(ConcurrentLinkedDictionary<int, int> map) {
            ConcurrentLinkedDictionary<int, int>.Node first = map.evictionDeque.Peek();
            updateRecency (map, () => { var x = map [first.Key];});
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void updateRecency_onGetQuietly(ConcurrentLinkedDictionary<int, int> map) {
            PaddedAtomicLong drainCounter = map.readBufferDrainAtWriteCount[ConcurrentLinkedDictionary<int, int>.readBufferIndex()];

            var first = map.evictionDeque.Peek();
            long drained = drainCounter.GetValue();

            map.GetQuietly(first.Key);
            map.DrainBuffers();

            Assert.That(map.evictionDeque.Peek(), Is.SameAs(first));
            Assert.That(drainCounter.GetValue(), Is.EqualTo(drained));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void updateRecency_onPutIfAbsent( ConcurrentLinkedDictionary<int, int> map) {
            ConcurrentLinkedDictionary<int, int>.Node first = map.evictionDeque.Peek();
            updateRecency(map, () => map.putIfAbsent(first.Key, first.Key));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void updateRecency_onPut( ConcurrentLinkedDictionary<int, int> map) {
            ConcurrentLinkedDictionary<int, int>.Node first = map.evictionDeque.Peek();
            updateRecency(map, () => map.put(first.Key, first.Key));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void updateRecency_onReplace( ConcurrentLinkedDictionary<int, int> map) {
            ConcurrentLinkedDictionary<int, int>.Node first = map.evictionDeque.Peek();
            updateRecency(map, () => map.replace(first.Key, first.Key));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void updateRecency_onReplaceConditionally(
             ConcurrentLinkedDictionary<int, int> map) {
            ConcurrentLinkedDictionary<int, int>.Node first = map.evictionDeque.Peek();
            updateRecency(map, () => map.replace(first.Key, first.Value, first.Value));
        }

        private void updateRecency(ConcurrentLinkedDictionary<int, int> map, Action operation) {
            ConcurrentLinkedDictionary<int, int>.Node first = map.evictionDeque.Peek();

            operation();
            map.DrainBuffers();

            Assert.That(map.evictionDeque.Peek(), Is.Not.SameAs(first));
            Assert.That (map.evictionDeque.Count, Is.Not.EqualTo(1));
            Assert.That(map, validConcurrentLinkedDictionary<int,int>());
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void exceedsMaximumBufferSize_onRead(ConcurrentLinkedDictionary<int, int> map) {
            PaddedAtomicLong drainCounter = map.readBufferDrainAtWriteCount[ConcurrentLinkedDictionary<int, int>.readBufferIndex()];
            map.readBufferWriteCount[ConcurrentLinkedDictionary<int, int>.readBufferIndex()].SetValue(ConcurrentLinkedDictionary<int, int>.READ_BUFFER_THRESHOLD - 1);

            map.afterRead(null);
            Assert.That(drainCounter.GetValue(), Is.EqualTo(0L));

            map.afterRead(null);
            Assert.That(drainCounter.GetValue(), Is.EqualTo(ConcurrentLinkedDictionary<int, int>.READ_BUFFER_THRESHOLD + 1L));
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void exceedsMaximumBufferSize_onWrite(ConcurrentLinkedDictionary<int, int> map) {
            var b = new bool[1];
            map.afterWrite (() => {
                b [0] = true;
            });
            Assert.That (b [0], Is.True);

            Assert.That(map.writeBuffer, HasCount(0));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void drain_onRead(ConcurrentLinkedDictionary<int, int> map) {
            PaddedAtomicReference<ConcurrentLinkedDictionary<int, int>.Node>[] buffer = map.readBuffers[ConcurrentLinkedDictionary<int, int>.readBufferIndex()];
            PaddedAtomicLong writeCounter = map.readBufferWriteCount[ConcurrentLinkedDictionary<int, int>.readBufferIndex()];

            for (int i = 0; i < ConcurrentLinkedDictionary<int, int>.READ_BUFFER_THRESHOLD; i++) {
                var x = map[1];
            }

            int pending = 0;
            foreach (PaddedAtomicReference<ConcurrentLinkedDictionary<int, int>.Node> slot in buffer) {
                if (slot.GetValue() != null) {
                    pending++;
                }
            }
            Assert.That(pending, Is.EqualTo(ConcurrentLinkedDictionary<int, int>.READ_BUFFER_THRESHOLD));
            Assert.That((int) writeCounter.GetValue(), Is.EqualTo(pending));

            var k = map [1];
            Assert.That(map.readBufferReadCount[ConcurrentLinkedDictionary<int, int>.readBufferIndex()], Is.EqualTo(writeCounter.GetValue()));
            for (int i = 0; i < map.readBuffers.Length; i++) {
                Assert.That(map.readBuffers[ConcurrentLinkedDictionary<int, int>.readBufferIndex()][i].GetValue(), Is.Null);
            }
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_onWrite(ConcurrentLinkedDictionary<int, int> map) {
            map.put(1, 1);
            Assert.That(map.writeBuffer, HasCount(0));
            Assert.That(map.evictionDeque, HasCount(1));
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_nonblocking( ConcurrentLinkedDictionary<int, int> map)
        {
            var done = new bool[1];
            var thread = new Thread(() => {
                map.drainStatus.SetValue(ConcurrentLinkedDictionary<int, int>.DrainStatus.REQUIRED);
                map.tryToDrainBuffers();
                done[0] = true;
            });
            map.evictionLock.EnterWriteLock ();
            try
            {
                thread.Start();
                waitUntil(() => done[0]);
            }
            finally{
                map.evictionLock.ExitWriteLock ();
            }
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_blocksClear( ConcurrentLinkedDictionary<int, int> map)
        {
            checkDrainBlocks(map, () => {
                map.Clear();
            });
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_blocksAscendingKeySet(ConcurrentLinkedDictionary<int, int> map)
        {
            checkDrainBlocks(map, () => {
                map.AscendingKeySet();
            });
            checkDrainBlocks(map, () => {
                map.AscendingKeySetWithLimit((int) Capacity());
            });
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_blocksDescendingKeySet( ConcurrentLinkedDictionary<int, int> map)
        {
            checkDrainBlocks(map, () => {
                map.DescendingKeySet();
            });
            checkDrainBlocks(map, () => {
                map.DescendingKeySetWithLimit((int) Capacity());
            });
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_blocksAscendingMap( ConcurrentLinkedDictionary<int, int> map)
        {
            checkDrainBlocks(map, () => {
                map.AscendingDictionary();
            });
            checkDrainBlocks(map, () => {
                map.AscendingDictionaryWithLimit((int) Capacity());
            });
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_blocksDescendingMap( ConcurrentLinkedDictionary<int, int> map)
        {
            checkDrainBlocks(map, () => {
                map.DescendingDictionary();
            });
            checkDrainBlocks(map, () => {
                map.DescendingDictionaryWithLimit((int) Capacity());
            });
        }

        [Test]
        [TestCaseSource("GuardedMap")]
        public void drain_blocksCapacity( ConcurrentLinkedDictionary<int, int> map)
        {
            checkDrainBlocks(map, () => {
                map.setCapacity(0);
            });
        }

        void checkDrainBlocks( ConcurrentLinkedDictionary<int, int> map, Action task)
        {
            //ReentrantLock lock1 = (ReentrantLock) map.evictionLock;


            var done = new bool[1];
            Thread thread = new Thread(() => {

                Console.WriteLine("z1");
                map.drainStatus.SetValue(ConcurrentLinkedDictionary<int, int>.DrainStatus.REQUIRED);
                task();
                Console.WriteLine("z2");
                done[0] = true;
                Console.WriteLine("z3");
            });
            Console.WriteLine("a");
            map.evictionLock.EnterWriteLock ();
            try {
                Console.WriteLine("b");
                thread.Start();
                Console.WriteLine("c");
                waitUntil(() => {
                    return map.evictionLock.WaitingWriteCount == 1;
                });
                Console.WriteLine("d");
            }
            finally{
                map.evictionLock.ExitWriteLock ();
            }
            Console.WriteLine("e");
            waitUntil(() => done[0]);
            Console.WriteLine("f");
        }


        /* ---------------- Ascending KeySet -------------- */

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingKeySet(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int> ();
            WarmUp(expected, 1, Capacity());

            Assert.That(map.AscendingKeySet(), Is.EqualTo(expected.Keys));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingKeySet_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity());

            ISet<int> original = map.AscendingKeySet();
            map.put((int) Capacity(), (int) -Capacity());
        
            Assert.That(original, Is.EqualTo(expected.Keys));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingKeySetWithLimit_greaterThan(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity() / 2);

            Assert.That(map.AscendingKeySetWithLimit((int) Capacity() / 2), Is.EqualTo(expected.Keys));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingKeySetWithLimit_lessThan(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity());

            Assert.That(map.AscendingKeySetWithLimit((int) Capacity() * 2), Is.EqualTo(expected.Keys));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingKeySetWithLimit_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity() / 2);

            ISet<int> original = map.AscendingKeySetWithLimit((int) Capacity() / 2);
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That(original, Is.EqualTo(expected.Keys));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingKeySetWithLimit_zero(ConcurrentLinkedDictionary<int, int> map) {
            Assert.That(map.AscendingKeySetWithLimit(0), emptyCollection<int>());
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AscendingKeySetWithLimit_negative(ConcurrentLinkedDictionary<int, int> map) {
            map.AscendingKeySetWithLimit(-1);
        }

        /* ---------------- Descending KeySet -------------- */

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingKeySet(ConcurrentLinkedDictionary<int, int> map) {
            ISet<int> expected = new SortedSet<int>();
            for (int i = (int) Capacity(); i > 0; i--) {
                expected.Add(i);
            }

            Assert.That(map.DescendingKeySet(), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingKeySet_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            ISet<int> expected = new SortedSet<int>();
            for (int i = (int) Capacity(); i > 0; i--) {
                expected.Add(i);
            }

            ISet<int> original = map.DescendingKeySet();
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That(original, Is.EqualTo(original));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingKeySetWithLimit_greaterThan(ConcurrentLinkedDictionary<int, int> map) {
            ISet<int> expected = new SortedSet<int>();
            for (int i = (int) Capacity(); i > Capacity() / 2; i--) {
                expected.Add(i);
            }
            Assert.That(map.DescendingKeySetWithLimit((int) Capacity() / 2), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingKeySetWithLimit_lessThan(ConcurrentLinkedDictionary<int, int> map) {
            ISet<int> expected = new SortedSet<int>();
            for (int i = (int) Capacity(); i > 0; i--) {
                expected.Add(i);
            }
            Assert.That(map.DescendingKeySetWithLimit((int) Capacity() * 2), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingKeySetWithLimit_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            ISet<int> expected = new SortedSet<int>();
            for (int i = (int) Capacity(); i > Capacity() / 2; i--) {
                expected.Add(i);
            }

            ISet<int> original = map.DescendingKeySetWithLimit((int) Capacity() / 2);
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That(original, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingKeySetWithLimit_zero(ConcurrentLinkedDictionary<int, int> map) {
            Assert.That(map.DescendingKeySetWithLimit(0), emptyCollection<int>());
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void DescendingKeySetWithLimit_negative(ConcurrentLinkedDictionary<int, int> map) {
            map.DescendingKeySetWithLimit(-1);
        }

        /* ---------------- Ascending Map -------------- */

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingDictionary(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity());

            var dict = map.AscendingDictionary ();
            Assert.That (dict, Is.InstanceOf<SortedDictionary<int,int>>());
            Assert.That(dict.ToArray(), Is.EqualTo(expected.ToArray()));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingDictionary_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity());

            IDictionary<int, int> original = map.AscendingDictionary();
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That (original, Is.InstanceOf<SortedDictionary<int,int>>());
            Assert.That(original.ToArray(), Is.EqualTo(expected.ToArray()));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingDictionaryWithLimit_greaterThan(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity() / 2);

            Assert.That(map.AscendingDictionaryWithLimit((int) Capacity() / 2), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingDictionaryWithLimit_lessThan(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity());

            var dict = map.AscendingDictionaryWithLimit ((int)Capacity () * 2);
            Assert.That (dict, Is.InstanceOf<SortedDictionary<int,int>>());
            Assert.That(dict.ToArray(), Is.EqualTo(expected.ToArray()));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingDictionaryWithLimit_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            WarmUp(expected, 1, Capacity() / 2);

            IDictionary<int, int> original = map.AscendingDictionaryWithLimit((int) Capacity() / 2);
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That (original, Is.InstanceOf<SortedDictionary<int,int>>());
            Assert.That(original.ToArray(), Is.EqualTo(expected.ToArray()));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void AscendingDictionaryWithLimit_zero(ConcurrentLinkedDictionary<int, int> map) {
            Assert.That(map.AscendingDictionaryWithLimit(0), emptyMap<int,int>());
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AscendingDictionaryWithLimit_negative(ConcurrentLinkedDictionary<int, int> map) {
            map.AscendingDictionaryWithLimit(-1);
        }

        /* ---------------- Descending Map -------------- */

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingDictionary(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            for (int i = (int) Capacity(); i > 0; i--) {
                expected[i] = -i;
            }
            Assert.That(map.DescendingDictionary(), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingDictionary_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            for (int i = (int) Capacity(); i > 0; i--) {
                expected[i] = -i;
            }

            IDictionary<int, int> original = map.DescendingDictionary();
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That(original, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingDictionaryWithLimit_greaterThan(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            for (int i = (int) Capacity(); i > Capacity() / 2; i--) {
                expected[i] = -i;
            }
            Assert.That(map.DescendingDictionaryWithLimit((int) Capacity() / 2), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingDictionaryWithLimit_lessThan(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            for (int i = (int) Capacity(); i > 0; i--) {
                expected[i] = -i;
            }
            Assert.That(map.DescendingDictionaryWithLimit((int) Capacity() * 2), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingDictionaryWithLimit_snapshot(ConcurrentLinkedDictionary<int, int> map) {
            IDictionary<int, int> expected = newLinkedHashMap<int,int>();
            for (int i = (int) Capacity(); i > Capacity() / 2; i--) {
                expected[i] = -i;
            }

            IDictionary<int, int> original = map.DescendingDictionaryWithLimit((int) Capacity() / 2);
            map.put((int) Capacity(), (int) -Capacity());

            Assert.That(original, Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("WarmedMap")]
        public void DescendingDictionaryWithLimit_zero(ConcurrentLinkedDictionary<int, int> map) {
            Assert.That(map.DescendingDictionaryWithLimit(0), emptyMap<int,int>());
        }
        [Test]
        [TestCaseSource("GuardedMap")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void DescendingDictionaryWithLimit_negative(ConcurrentLinkedDictionary<int, int> map) {
            map.DescendingDictionaryWithLimit(-1);
        }


    }
}

