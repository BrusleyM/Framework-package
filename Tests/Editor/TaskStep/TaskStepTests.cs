using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class TaskStepTests
    {
        private GameObject _host;
        private ProbeTask _task;
        private SequenceRunner _runner;
        private int _completeEventCount;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("task host");
            _task = _host.AddComponent<ProbeTask>();
            _task.OnCompleteTask.AddListener(() => _completeEventCount++);
            _runner = new SequenceRunner("probe", new ISequenceStep[] { new TaskStep(_task, null) });
            _completeEventCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Entering_the_step_starts_the_task_from_a_clean_state()
        {
            _task.MarkLeftOverComplete();

            _runner.Start();

            Assert.AreEqual(1, _task.StartCount);
            Assert.IsFalse(_task.IsComplete);
        }

        [Test]
        public void A_completing_task_completes_the_sequence_and_raises_its_event_exactly_once()
        {
            _runner.Start();

            _task.CompleteFromOutside();

            Assert.AreEqual(SequenceState.Completed, _runner.State);
            Assert.AreEqual(1, _completeEventCount);
        }

        [Test]
        public void Skipping_asks_the_task_to_skip_and_still_raises_the_complete_event()
        {
            _runner.Start();

            _runner.SkipCurrentStep();

            Assert.AreEqual(1, _task.SkipCount);
            Assert.AreEqual(1, _completeEventCount);
            Assert.AreEqual(SequenceState.Completed, _runner.State);
        }

        [Test]
        public void Cancelling_cleans_the_task_up_without_raising_the_complete_event()
        {
            _runner.Start();

            _runner.Cancel();

            Assert.AreEqual(1, _task.CancelCount);
            Assert.AreEqual(0, _completeEventCount);
        }

        [Test]
        public void Restarting_cleans_up_and_starts_the_task_again()
        {
            _runner.Start();

            _runner.Restart();

            Assert.AreEqual(1, _task.CancelCount);
            Assert.AreEqual(2, _task.StartCount);
        }

        [Test]
        public void A_task_still_settling_keeps_the_sequence_waiting_until_it_has_settled()
        {
            _task.IsStillSettling = true;
            _runner.Start();

            _task.CompleteFromOutside();
            _runner.Advance(0.1f);
            Assert.AreEqual(SequenceState.Running, _runner.State);

            _task.IsStillSettling = false;
            _runner.Advance(0.1f);
            Assert.AreEqual(SequenceState.Completed, _runner.State);
        }

        [Test]
        public void A_task_that_cannot_start_faults_the_sequence_instead_of_throwing_to_the_caller()
        {
            _task.ThrowOnStart = true;

            Assert.DoesNotThrow(() => _runner.Start());
            Assert.AreEqual(SequenceState.Faulted, _runner.State);
        }

        [Test]
        public void A_task_destroyed_while_running_does_not_fault_the_sequence_when_it_is_cancelled()
        {
            _runner.Start();
            Object.DestroyImmediate(_host);

            _runner.Cancel();

            Assert.AreEqual(SequenceState.Cancelled, _runner.State);
        }

        [Test]
        public void A_listener_that_cancels_from_the_complete_event_cleans_up_the_next_task_it_started()
        {
            var secondHost = new GameObject("second host");
            var secondTask = secondHost.AddComponent<ProbeTask>();
            var runner = new SequenceRunner("two", new ISequenceStep[] { new TaskStep(_task, null), new TaskStep(secondTask, null) });
            _task.OnCompleteTask.AddListener(runner.Cancel);
            runner.Start();

            _task.CompleteFromOutside();

            Assert.AreEqual(SequenceState.Cancelled, runner.State);
            Assert.AreEqual(1, secondTask.CancelCount);
            Assert.IsNull(runner.CurrentStep);
            Object.DestroyImmediate(secondHost);
        }

        private sealed class ProbeTask : Task, ISettlingTask
        {
            public int StartCount { get; private set; }
            public int SkipCount { get; private set; }
            public int CancelCount { get; private set; }
            public bool IsStillSettling { get; set; }
            public bool ThrowOnStart { get; set; }
            public bool IsSettling => IsStillSettling;

            public void CompleteFromOutside() => TaskComplete();

            public void MarkLeftOverComplete() => MarkComplete();

            public override void StartTask()
            {
                StartCount++;
                if (ThrowOnStart)
                {
                    throw new System.InvalidOperationException("task could not start");
                }
            }

            public override void Skip()
            {
                SkipCount++;
                base.Skip();
            }

            public override void CancelTask() => CancelCount++;
        }
    }
}
