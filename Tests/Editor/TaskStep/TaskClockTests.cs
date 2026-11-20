using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class TaskClockTests
    {
        private GameObject _host;
        private RecordingTask _task;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("task host");
            _task = _host.AddComponent<RecordingTask>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void The_scaled_clock_waits_with_game_time_and_the_unscaled_clock_with_real_time()
        {
            Assert.That(TaskClocks.Scaled.WaitFor(1f), Is.TypeOf<WaitForSeconds>());
            Assert.That(TaskClocks.Unscaled.WaitFor(1f), Is.TypeOf<WaitForSecondsRealtime>());
        }

        [Test]
        public void Ignoring_the_time_scale_picks_the_unscaled_clock()
        {
            Assert.That(TaskClocks.For(true), Is.SameAs(TaskClocks.Unscaled));
            Assert.That(TaskClocks.For(false), Is.SameAs(TaskClocks.Scaled));
        }

        [Test]
        public void A_new_task_uses_the_scaled_clock_and_clearing_the_clock_goes_back_to_it()
        {
            Assert.That(_task.Clock, Is.SameAs(TaskClocks.Scaled));

            _task.SetClock(TaskClocks.Unscaled);
            _task.SetClock(null);

            Assert.That(_task.Clock, Is.SameAs(TaskClocks.Scaled));
        }

        [Test]
        public void A_task_step_gives_its_task_the_clock_when_it_is_entered()
        {
            var step = new TaskStep(_task, null, TaskClocks.Unscaled);

            step.Enter();

            Assert.That(_task.Clock, Is.SameAs(TaskClocks.Unscaled));
        }

        [Test]
        public void A_task_step_without_a_clock_puts_its_task_back_on_the_scaled_clock()
        {
            _task.SetClock(TaskClocks.Unscaled);
            var step = new TaskStep(_task, null);

            step.Enter();

            Assert.That(_task.Clock, Is.SameAs(TaskClocks.Scaled));
        }

        [Test]
        public void Preparing_a_task_puts_it_back_on_the_scaled_clock()
        {
            _task.SetClock(TaskClocks.Unscaled);

            _task.Prepare();

            Assert.That(_task.Clock, Is.SameAs(TaskClocks.Scaled));
        }

        [Test]
        public void An_unguided_step_gives_every_task_the_clock_when_it_is_entered()
        {
            var other = _host.AddComponent<RecordingTask>();
            var step = new UnguidedStep("free", new Task[] { _task, other }, null, 0f, TaskClocks.Unscaled);

            step.Enter();

            Assert.That(_task.Clock, Is.SameAs(TaskClocks.Unscaled));
            Assert.That(other.Clock, Is.SameAs(TaskClocks.Unscaled));
        }
    }
}
