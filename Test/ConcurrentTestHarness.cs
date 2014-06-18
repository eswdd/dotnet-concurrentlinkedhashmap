using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace ConcurrentLinkedDictionary.Test
{
    /**
 * A testing harness for concurrency related executions.
 * <p/>g
 * This harness will ensure that all threads execute at the same time, records
 * the full execution time, and optionally retrieves the responses from each
 * thread. This harness can be used for performance tests, investigations of
 * lock contention, etc.
 * <p/>
 * This code was adapted from <tt>Java Concurrency in Practice</tt>, using an
 * example of a {@link CountDownLatch} for starting and stopping threads in
 * timing tests.
 *
 * @author ben.manes@gmail.com (Ben Manes)
 */
    public sealed class ConcurrentTestHarness
    {
        private ConcurrentTestHarness() {
            throw new InvalidOperationException("Cannot instantiate static class");
        }

        /**
   * Executes a task, on N threads, all starting at the same time.
   *
   * @param nThreads the number of threads to execute
   * @param task the task to execute in each thread
   * @return the execution time for all threads to complete, in nanoseconds
   */
        public static long timeTasks(int nThreads, Action task) {
            return timeTasks(nThreads, task, "Thread");
        }

        /**
   * Executes a task, on N threads, all starting at the same time.
   *
   * @param nThreads the number of threads to execute
   * @param task the task to execute in each thread
   * @param baseThreadName the base name for each thread in this task set
   * @return the execution time for all threads to complete, in nanoseconds
   */
        public static long timeTasks(int nThreads, Action task, String baseThreadName)
         {
            return timeTasks<object>(nThreads, () => { task(); return null; }, baseThreadName).getExecutionTime();
        }

        /**
   * Executes a task, on N threads, all starting at the same time.
   *
   * @param nThreads the number of threads to execute
   * @param task the task to execute in each thread
   * @return the result of each task and the full execution time, in nanoseconds
   */
        public static TestResult<T> timeTasks<T>(int nThreads, Func<T> task) where T : class
         {
            return timeTasks(nThreads, task, "Thread");
        }

        /**
   * Executes a task, on N threads, all starting at the same time.
   *
   * @param nThreads the number of threads to execute
   * @param task the task to execute in each thread
   * @param baseThreadName the base name for each thread in this task set
   * @return the result of each task and the full execution time, in
   *     nanoseconds
   */
        public static TestResult<T> timeTasks <T>(int nThreads, Func<T> task,
            String baseThreadName) where T : class
         {
            CountdownEvent startGate = new CountdownEvent(1);
            CountdownEvent endGate = new CountdownEvent(nThreads);
            AtomicReferenceArray<T> results = new AtomicReferenceArray<T>(nThreads);

            IList<Thread> threads = new List<Thread>(nThreads);
            for (int i = 0; i < nThreads; i++) {
                int index = i;
                var thread = new Thread (() => {
                    startGate.Wait();
                    try {
                        results[index] = task();
                    } finally {
                        endGate.Signal();
                    }
                }) { Name = baseThreadName + "-" + i, IsBackground = true };
                thread.Start();
                threads.Add(thread);
            }

            var sw = Stopwatch.StartNew ();
            startGate.Signal ();
            endGate.Wait ();
            return new TestResult<T>(sw.Elapsed.Ticks, toList(results));
        }

        /**
   * Migrates the data from the atomic array to a {@link List} for easier
   * consumption.
   *
   * @param data the per-thread results from the test
   * @return the per-thread results as a standard collection
   */
        private static IList<T> toList<T>(AtomicReferenceArray<T> data) where T:class {
            var list = new List<T>(data.Length);
            for (int i = 0; i < data.Length; i++) {
                list.Add(data[i]);
            }
            return list;
        }

        /**
   * The results of the test harness's execution.
   *
   * @param <T> the data type produced by the task
   */
        public class TestResult<T> {
            private readonly long executionTime;
            private readonly IList<T> results;

            public TestResult(long executionTime, IList<T> results) {
                this.executionTime = executionTime;
                this.results = results;
            }

            /**
     * The test's execution time, in nanoseconds.
     *
     * @return The time to complete the test.
     */
            public long getExecutionTime() {
                return executionTime;
            }

            /**
     * The results from executing the tasks.
     *
     * @return The outputs from the tasks.
     */
            public IList<T> getResults() {
                return results;
            }
        }
    }
}

