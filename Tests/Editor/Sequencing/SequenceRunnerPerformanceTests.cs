using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class SequenceRunnerPerformanceTests
    {
        private const int WarmUpTicks = 1000;
        private const int MeasuredTicks = 100000;
        private const double TimeBudgetMilliseconds = 500.0;

        private static SequenceRunner RunningRunnerWithLongDelay()
        {
            var runner = new SequenceRunner("performance", new ISequenceStep[] { new DelayStep("long wait", float.MaxValue) });
            runner.Start();
            return runner;
        }

        private static void Tick(SequenceRunner runner, int count)
        {
            for (var tick = 0; tick < count; tick++)
            {
                runner.Advance(0.016f);
            }
        }

        [Test]
        public void A_steady_state_tick_allocates_no_memory()
        {
            var runner = RunningRunnerWithLongDelay();
            Tick(runner, WarmUpTicks);

            var before = GC.GetAllocatedBytesForCurrentThread();
            Tick(runner, MeasuredTicks);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0L, allocatedBytes);
        }

        [Test]
        public void A_hundred_thousand_steady_state_ticks_finish_inside_the_time_budget()
        {
            var runner = RunningRunnerWithLongDelay();
            Tick(runner, WarmUpTicks);

            var stopwatch = Stopwatch.StartNew();
            Tick(runner, MeasuredTicks);
            stopwatch.Stop();

            Assert.Less(stopwatch.Elapsed.TotalMilliseconds, TimeBudgetMilliseconds);
        }
    }
}
