using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentLinkedDictionary.Test
{

    /// <summary>
    /// A unit-test to assert basic concurrency characteristics by validating the
    /// internal state after load.
    /// 
    /// Original author: ben.manes@gmail.com (Ben Manes)
    /// Ported by: Simon Matic Langford
    /// </summary>
    [TestFixture]
    [Category("development")]
    public sealed class MultiThreadedTest : AbstractTest
    {
        private readonly ConcurrentQueue<string> failures = new ConcurrentQueue<string> ();
        private readonly int iterations = 20000;
        private readonly int timeOut = 60;
        private readonly int threads = 20;


        public MultiThreadedTest() : base(TestType.Stress)
        {
            AbstractTest.debugEnabled = true;
        }

        [TearDown]
        public void tearDown() {
            string ignore;
            while (failures.TryDequeue (out ignore)) {
            }
        }

        [TestCaseSource("Builder")]
        public void concurrency(Builder<int,int> builder) {
            List<int> keys = new List<int> ();
            Random random = new Random();
            for (int i = 0; i < iterations; i++) {
                var key = (int)(random.NextDouble () * (iterations / 100));
                key++; // ensure we have no keys with value of zero
                    keys.Add(key);
            }
            List<List<int>> sets = Benchmarks.shuffle(threads, keys);
            ConcurrentLinkedDictionary<int, int> map = builder
                .MaximumWeightedCapacity(Capacity())
                .ConcurrencyLevel(threads)
                .Build();
            executeWithTimeOut(map, () => {
                return ConcurrentTestHarness.timeTasks(threads, new Thrasher(this, map, sets).Run);
            });
        }

        [TestCaseSource("BuilderIntListOfInt")]
        public void weightedConcurrency(Builder<int, IList<int>> builder) {
            ConcurrentLinkedDictionary<int, IList<int>> map = builder
                .Weigher(Weighers.List<int>())
                .MaximumWeightedCapacity(threads)
                .ConcurrencyLevel(threads)
                .Build();
            ConcurrentQueue<List<int>> values = new ConcurrentQueue<List<int>>();
            for (int i = 1; i <= threads; i++) {
                int[] array = new int[i];
                array.SetAll (int.MinValue);
                values.Enqueue(array.ToList());
            }
            executeWithTimeOut(map, () => {
                return ConcurrentTestHarness.timeTasks(threads, () => {
                    // todo: was concurrentlinkedqueue.poll blocking?
                    List<int> value;
                    if (values.TryDequeue(out value))
                    {
                            for (int i = 0; i < iterations; i++) {
                                map.put(i % 10, value);
                            }
                        }
                    });
            });
        }

        /**
   * Executes operations against the map to simulate random load.
   */
        private sealed class Thrasher {
            private readonly MultiThreadedTest _test;
                    private readonly ConcurrentLinkedDictionary<int, int> map;
                    private readonly List<List<int>> sets;
                    private readonly AtomicInteger index;

            public Thrasher(MultiThreadedTest test, ConcurrentLinkedDictionary<int, int> map, List<List<int>> sets) {
                this.index = new AtomicInteger();
                _test = test;
                this.map = map;
                this.sets = sets;
            }

                    public void Run() {

                Operation[] ops = (Operation[]) Enum.GetValues(typeof(Operation));
                int id = index.GetAndIncrement();
                Random random = new Random();
                //debug("#%d: STARTING", id);
                foreach (int key in sets[id]) {
                    Operation operation = ops[random.NextInt(ops.Length)];
                    try {
                        ActionForOperation(operation)(map,key);
                    } catch (AssertionException ae) {
                        String error =
                            String.Format("Failed: key {0} on operation {1} for node {2}",
                                key, operation, nodeToString(findNode(key, map)));
                        _test.failures.Enqueue(error);
                        Console.WriteLine (ae);
                        //throw e;
                    } catch (Exception e) {
                        String error =
                            String.Format("Halted: key {0} on operation {1} for node {2} with {3}: {4}",
                                key, operation, nodeToString(findNode(key, map)), e.GetType(), e.Message);
                        _test.failures.Enqueue(error);
                        Console.WriteLine (e);
                    }
                }
            }
        }

        private static Action<ConcurrentLinkedDictionary<int,int>,int> ActionForOperation(Operation op)
        {
            switch (op) {
            case Operation.CONTAINS_KEY:
                return (cache, key) => cache.ContainsKey (key); 
            case Operation.CONTAINS_VALUE:
                return (cache, key) => cache.containsValue (key); 
            case Operation.IS_EMPTY:
                return (cache, key) => cache.Any(); 
            case Operation.SIZE:
                return (cache, key) => Assert.True (cache.Count >= 0);
            case Operation.WEIGHTED_SIZE:
                return (cache, key) => Assert.True (cache.WeightedSize () >= 0);
            case Operation.CAPACITY:
                return (cache, key) => cache.setCapacity ((int)cache.Capacity ());
            case Operation.GET:
                return (cache, key) => {
                    int value;
                    cache.TryGetValue(key, out value);
                    // java doesn't mind if you get a miss, .net rather does..
//                    var x = cache [key];
                }; 
            case Operation.GET_QUIETLY:
                return (cache, key) => cache.GetQuietly (key); 
            case Operation.PUT:
                return (cache, key) => cache.put (key, key);
            case Operation.PUT_IF_ABSENT:
                return (cache, key) => cache.putIfAbsent (key, key); 
            case Operation.REMOVE:
                return (cache, key) => cache.remove (key); 
            case Operation.REMOVE_IF_EQUAL:
                return (cache, key) => cache.remove (key, key); 
            case Operation.REPLACE:
                return (cache, key) => cache.replace (key, key); 
            case Operation.REPLACE_IF_EQUAL:
                return (cache, key) => cache.replace (key, key, key); 
            case Operation.CLEAR:
                return (cache, key) => cache.Clear (); 
            case Operation.KEY_SET:
                return (cache, key) => {
                    foreach (int i in cache.Keys) {
                        Assert.AreNotEqual(0,i);
                    }
                    var x = cache.Keys;
                };
            case Operation.ASCENDING:
                return (cache, key) => {
                    foreach (int i in cache.AscendingKeySet()) {
                        Assert.AreNotEqual(0,i);
                    }
                    foreach (var entry in cache.AscendingDictionary()) {
                        Assert.NotNull(entry);
                    }
                };
            case Operation.DESCENDING:
                return (cache, key) => {
                    foreach (int i in cache.DescendingKeySet()) {
                        Assert.AreNotEqual(0,i);
                    }
                    foreach (var entry in cache.DescendingDictionary()) {
                        Assert.NotNull(entry);
                    }
                };
            case Operation.VALUES:
                return (cache, key) => {
                    foreach (int i in cache.Values) {
                        Assert.AreNotEqual(0,i);
                    }
                    var x = cache.Values;
                };
            case Operation.ENTRY_SET:
                return (cache, key) => {
                    foreach (var entry in cache) {
                        Assert.NotNull(entry);
                        Assert.AreNotEqual(0,entry.Key);
                        Assert.AreNotEqual(0,entry.Value);
                    }
                    var x = cache.Entries;
                };
            case Operation.HASHCODE:
                return (cache, key) => cache.GetHashCode (); 
            case Operation.EQUALS:
                return (cache, key) => cache.Equals (cache); 
            case Operation.TO_STRING:
                return (cache, key) => cache.ToString (); 
//            case Operation.SERIALIZE:
                //return (cache, key) => cache.Clear (); 
//                throw new NotSupportedException();
            }
            throw new Exception ("unrecognized op " + op);
        }

        /// <summary>
        /// The public operations that can be performed on the cache.
        /// </summary>
        enum Operation {
            CONTAINS_KEY,
            CONTAINS_VALUE,
            IS_EMPTY,
            SIZE,
            WEIGHTED_SIZE,
            CAPACITY,
            GET,
            GET_QUIETLY,
            PUT,
            PUT_IF_ABSENT,
            REMOVE,
            REMOVE_IF_EQUAL,
            REPLACE,
            REPLACE_IF_EQUAL,
            CLEAR,
            KEY_SET,
            ASCENDING,
            DESCENDING,
            VALUES,
            ENTRY_SET,
            HASHCODE,
            EQUALS,
            TO_STRING
            //            ,SERIALIZE -- not supported in .net impl right now
        }

        /* ---------------- Utilities -------------- */

        private void executeWithTimeOut<K,V>(
            ConcurrentLinkedDictionary<K, V> map, Func<long> task) {

            long result = 0;

            var t = Task.Run (() => Interlocked.Add(ref result, task ()));
            var ret = Task.WaitAny (new [] {t}, TimeSpan.FromSeconds (timeOut));
            if (ret == 0) {
                debug("\nExecuted in {0} second(s)", TimeSpan.FromTicks(result).TotalSeconds);
                Assert.That(map, validConcurrentLinkedDictionary<K,V>());
                return;
            }
            handleTimeout (map);
        }

        private void handleTimeout<K, V>(
            ConcurrentLinkedDictionary<K, V> cache) {
//            ,
//            ExecutorService es,
//            TimeoutException e) {
            // todo: print stack traces of all threads - this causes npe
            //foreach (Thread thread in Process.GetCurrentProcess().Threads)
            //{
            //    info (thread.GetCompressedStack ().ToString ());
            //}
            //for (StackTraceElement[] trace : Thread.getAllStackTraces().values()) {
            //    for (StackTraceElement element : trace) {
            //        info("\tat " + element);
            //    }
            //    if (trace.length > 0) {
            //        info("------");
            //    }
            //}
//            es.shutdownNow();
//            try {
//                es.awaitTermination(10, SECONDS);
//            } catch (InterruptedException ex) {
//                fail("", ex);
//            }

            // Print the state of the cache
            debug("Cached Elements: {0}", cache.ToString());
            debug("Deque Forward:\n{0}", ascendingToString(cache));
            debug("Deque Backward:\n{0}", descendingToString(cache));

            // Print the recorded failures
            foreach (String failure in failures) {
                debug(failure);
            }
            Assert.Fail("Spun forever");
        }

        static String ascendingToString<K,V>(ConcurrentLinkedDictionary<K,V> map) {
            return dequeToString(map, true);
        }

        static String descendingToString<K,V>(ConcurrentLinkedDictionary<K,V> map) {
            return dequeToString(map, false);
        }

        //@SuppressWarnings("rawtypes")
        private static String dequeToString<K,V>(ConcurrentLinkedDictionary<K,V> map, bool ascending) {
            map.evictionLock.EnterWriteLock ();
            try {
                StringBuilder buffer = new StringBuilder("\n");
                ISet<object> seen = new HashSet<object>();
                IEnumerator<ConcurrentLinkedDictionary<K,V>.Node> iterator = ascending
                    ? (IEnumerator<ConcurrentLinkedDictionary<K,V>.Node>) map.evictionDeque.GetEnumerator()
                    : map.evictionDeque.GetDescendingEnumerator();
                while (iterator.MoveNext()) {
                    ConcurrentLinkedDictionary<K,V>.Node node = iterator.Current;
                    buffer.Append(nodeToString(node)).Append("\n");
                    bool added = seen.Add(node);
                    if (!added) {
                        buffer.Append("Failure: Loop detected\n");
                        break;
                    }
                }
                return buffer.ToString();
            } finally {
                map.evictionLock.ExitWriteLock();
            }
        }

        //@SuppressWarnings("rawtypes")
        static String nodeToString<K,V>(ConcurrentLinkedDictionary<K,V>.Node node) {
            return (node == null) ? "null" : String.Format("{0}={1}", node.Key, node.GetValue());
        }

        /** Finds the node in the map by walking the list. Returns null if not found. */
        static ConcurrentLinkedDictionary<int,int>.Node findNode(
            int key, ConcurrentLinkedDictionary<int, int> map) {
            map.evictionLock.EnterWriteLock ();
            try {
                foreach (ConcurrentLinkedDictionary<int,int>.Node node in map.evictionDeque) {
                    if (node.Key.Equals(key)) {
                        return node;
                    }
                }
                return null;
            } finally {
                map.evictionLock.ExitWriteLock();
            }
        }
    }
}

