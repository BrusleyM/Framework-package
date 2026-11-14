using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class ConditionTests
    {
        private GameObject _host;
        private SignalCondition _signal;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("signal host");
            _signal = _host.AddComponent<SignalCondition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void A_signal_raised_before_the_step_starts_is_ignored()
        {
            var runner = Sequence.Named("press").WaitFor("press the button", _signal).Build();

            _signal.Raise();
            runner.Start();

            Assert.AreEqual(SequenceState.Running, runner.State);
        }

        [Test]
        public void A_signal_raised_after_the_sequence_was_cancelled_is_ignored()
        {
            var runner = Sequence.Named("press").WaitFor("press the button", _signal).Build();
            runner.Start();
            runner.Cancel();

            _signal.Raise();

            Assert.AreEqual(SequenceState.Cancelled, runner.State);
        }

        [Test]
        public void A_condition_task_completes_when_its_signal_is_raised()
        {
            var task = CreateWaitingTask();
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(task, null) });
            runner.Start();

            _signal.Raise();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void Skipping_a_condition_task_disarms_it_so_a_late_signal_changes_nothing()
        {
            var task = CreateWaitingTask();
            var lateCompletions = 0;
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(task, null) });
            task.Completed += () => lateCompletions++;
            runner.Start();

            runner.SkipCurrentStep();
            _signal.Raise();

            Assert.AreEqual(1, lateCompletions);
        }

        [Test]
        public void Restarting_a_condition_task_never_stacks_subscriptions()
        {
            var task = CreateWaitingTask();
            var completions = 0;
            task.Completed += () => completions++;
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(task, null) });
            runner.Start();
            runner.Restart();

            _signal.Raise();

            Assert.AreEqual(1, completions);
        }

        [Test]
        public void A_condition_task_without_a_condition_source_faults_the_sequence()
        {
            var task = _host.AddComponent<WaitForConditionTask>();
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(task, null) });

            runner.Start();

            Assert.AreEqual(SequenceState.Faulted, runner.State);
        }

        private WaitForConditionTask CreateWaitingTask()
        {
            var task = _host.AddComponent<WaitForConditionTask>();
            var serialized = new UnityEditor.SerializedObject(task);
            serialized.FindProperty("_conditionSource").objectReferenceValue = _signal;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return task;
        }

        [Test]
        public void A_signal_raised_while_a_step_waits_completes_that_step()
        {
            var runner = Sequence.Named("press").WaitFor("press the button", _signal).Build();
            runner.Start();

            _signal.Raise();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_signal_raised_after_the_step_completed_does_nothing_more()
        {
            var satisfiedCount = 0;
            _signal.Satisfied += () => satisfiedCount++;
            var runner = Sequence.Named("press").WaitFor("press the button", _signal).Build();
            runner.Start();

            _signal.Raise();
            _signal.Raise();

            Assert.AreEqual(1, satisfiedCount);
        }
    }
}
