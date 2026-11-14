using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class ProbeCondition : MonoBehaviour, IStepCondition
    {
        public int ArmCount { get; private set; }
        public int DisarmCount { get; private set; }
        public bool SatisfiedWhenArmed { get; set; }
        public bool ThrowWhenArmed { get; set; }
        public int SubscriberCount => _satisfied?.GetInvocationList().Length ?? 0;

        private System.Action _satisfied;

        public event System.Action Satisfied
        {
            add => _satisfied += value;
            remove => _satisfied -= value;
        }

        public void Arm()
        {
            ArmCount++;
            if (ThrowWhenArmed)
            {
                throw new System.InvalidOperationException("cannot arm");
            }

            if (SatisfiedWhenArmed)
            {
                _satisfied?.Invoke();
            }
        }

        public void Disarm() => DisarmCount++;
    }

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
        public void Skipping_a_condition_task_disarms_its_condition_exactly_once()
        {
            var probe = _host.AddComponent<ProbeCondition>();
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(CreateWaitingTask(probe), null) });
            runner.Start();

            runner.SkipCurrentStep();

            Assert.AreEqual(1, probe.DisarmCount);
            Assert.AreEqual(0, probe.SubscriberCount);
        }

        [Test]
        public void Restarting_a_condition_task_rearms_it_with_a_single_subscription()
        {
            var probe = _host.AddComponent<ProbeCondition>();
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(CreateWaitingTask(probe), null) });
            runner.Start();

            runner.Restart();

            Assert.AreEqual(2, probe.ArmCount);
            Assert.AreEqual(1, probe.SubscriberCount);
        }

        [Test]
        public void A_condition_task_whose_condition_is_already_satisfied_completes_while_starting()
        {
            var probe = _host.AddComponent<ProbeCondition>();
            probe.SatisfiedWhenArmed = true;
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(CreateWaitingTask(probe), null) });

            runner.Start();

            Assert.AreEqual(SequenceState.Completed, runner.State);
        }

        [Test]
        public void A_condition_that_throws_while_arming_leaves_no_subscription_behind()
        {
            var probe = _host.AddComponent<ProbeCondition>();
            probe.ThrowWhenArmed = true;
            var runner = new SequenceRunner("task", new ISequenceStep[] { new TaskStep(CreateWaitingTask(probe), null) });

            runner.Start();

            Assert.AreEqual(SequenceState.Faulted, runner.State);
            Assert.AreEqual(0, probe.SubscriberCount);
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
            return CreateWaitingTask(_signal);
        }

        private WaitForConditionTask CreateWaitingTask(MonoBehaviour conditionSource)
        {
            var task = _host.AddComponent<WaitForConditionTask>();
            var serialized = new UnityEditor.SerializedObject(task);
            serialized.FindProperty("_conditionSource").objectReferenceValue = conditionSource;
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
