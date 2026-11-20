using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class TaskOutcomeTests
    {
        private GameObject _host;
        private RecordingTask _task;
        private int _completedRaised;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("task");
            _task = _host.AddComponent<RecordingTask>();
            _completedRaised = 0;
            _task.Completed += () => _completedRaised++;
            _task.Prepare();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void A_prepared_task_is_pending_and_neither_complete_nor_skipped()
        {
            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Pending));
            Assert.That(_task.IsComplete, Is.False);
            Assert.That(_task.IsSkip, Is.False);
        }

        [Test]
        public void Completing_a_task_records_it_as_completed_and_not_skipped()
        {
            _task.Finish();

            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Completed));
            Assert.That(_task.IsComplete, Is.True);
            Assert.That(_task.IsSkip, Is.False);
        }

        [Test]
        public void Skipping_a_task_records_it_as_skipped_and_complete()
        {
            _task.Skip();

            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Skipped));
            Assert.That(_task.IsComplete, Is.True);
            Assert.That(_task.IsSkip, Is.True);
            Assert.That(_completedRaised, Is.EqualTo(1));
        }

        [Test]
        public void A_task_that_skips_itself_is_recorded_as_skipped()
        {
            _task.SkipItself();

            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Skipped));
        }

        [TestCase(TaskOutcome.Completed)]
        [TestCase(TaskOutcome.Skipped)]
        public void The_outcome_is_already_set_when_the_completed_event_is_raised(TaskOutcome expected)
        {
            var seen = TaskOutcome.Pending;
            _task.Completed += () => seen = _task.Outcome;

            if (expected == TaskOutcome.Skipped)
            {
                _task.Skip();
            }
            else
            {
                _task.Finish();
            }

            Assert.That(seen, Is.EqualTo(expected));
        }

        [Test]
        public void Skipping_a_task_that_already_completed_leaves_it_completed()
        {
            _task.Finish();

            _task.Skip();

            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Completed));
            Assert.That(_completedRaised, Is.EqualTo(1));
        }

        [Test]
        public void Completing_a_task_that_was_skipped_leaves_it_skipped()
        {
            _task.Skip();

            _task.Finish();

            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Skipped));
            Assert.That(_completedRaised, Is.EqualTo(1));
        }

        [Test]
        public void Preparing_a_finished_task_makes_it_pending_again_and_lets_it_complete_again()
        {
            _task.Skip();

            _task.Prepare();
            _task.Finish();

            Assert.That(_task.Outcome, Is.EqualTo(TaskOutcome.Completed));
            Assert.That(_completedRaised, Is.EqualTo(2));
        }
    }
}
