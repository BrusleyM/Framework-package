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
        public void A_signal_raised_before_arming_is_ignored()
        {
            var runner = Sequence.Named("press").WaitFor("press the button", _signal).Build();
            runner.Start();
            runner.Cancel();

            _signal.Raise();

            Assert.AreEqual(SequenceState.Cancelled, runner.State);
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
