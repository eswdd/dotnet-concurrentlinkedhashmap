using System;
using NUnit.Framework;
using System.Threading;
using System.IO;

namespace ConcurrentLinkedDictionary.Test
{
    /**
 * A unit-test to assert that the cache does not have a memory leak by not being
 * able to drain the buffers fast enough.
 *
 * @author ben.manes@gmail.com (Ben Manes)
 */
    [TestFixture]
    [Category("load")]
    public class MemoryLeakTest
    {
        private readonly long statusInterval = 5;
        private readonly int threads = 250;
        private long runningTime;

        private ConcurrentLinkedDictionary<long, long> map;

        private Timer timer;

        string statusFile = "output.log";

        StreamWriter stream;

        // todo
        //@Parameters({"threads", "statusInterval"})
//        public MemoryLeakTest(int threads, long statusInterval) {
//            this.statusInterval = statusInterval;
//            this.threads = threads;
//        }

        [TestFixtureSetUp]
        public void beforeMemoryLeakTest() {
            runningTime = 0;

            stream = new StreamWriter(new FileStream(statusFile, FileMode.OpenOrCreate));

            timer = new Timer (Status, null, statusInterval*1000, statusInterval*1000);


            map = new Builder<long, long>()
                .MaximumWeightedCapacity(threads)
                .Build();
        }

        [TestFixtureTearDown]
        public void afterMemoryLeakTest() {
            timer.Dispose ();
        }

        [Test]
        public void memoryLeak()  {
            ConcurrentTestHarness.timeTasks(threads, () => {
                var id = Thread.CurrentThread.ManagedThreadId;
                map.put(id, id);
                for (;;) {
                    var x= map[id];
                    Thread.Yield();
                    Thread.Sleep(TimeSpan.FromTicks(1));
                    //LockSupport.parkNanos(1L);
                }
            });
        }

        private void Status(object state) {
                long reads = 0;
            for (int i = 0; i < map.readBuffers.Length; i++) {
                for (int j = 0; j < map.readBuffers[i].Length; j++) {
                    if (map.readBuffers[i][j].GetValue() != null) {
                            reads++;
                        }
                    }
                }
            runningTime += statusInterval*1000;
            String elapsedTime = TimeSpan.FromMilliseconds (runningTime).ToString ();
            stream.WriteLine("---------- {0} ----------", elapsedTime);
            stream.WriteLine("Pending reads = {0:#,###}", reads);
            stream.WriteLine("Pending write = {0:#,###}", map.writeBuffer.Count);
            stream.WriteLine("Drain status = {0}\n", map.drainStatus.GetValue());
            stream.Flush ();
        }
    }
}

