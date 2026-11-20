using System.Collections;
using System.Reflection;
using Framework.Audio;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class IgnoreTimeScalePlayModeTests
    {
        private const float ReminderSeconds = 0.1f;

        private GameObject _host;
        private SequentialTask _sequence;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _host = new GameObject("host");
            _sequence = _host.AddComponent<SequentialTask>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator A_sequence_that_ignores_the_time_scale_keeps_running_while_the_time_scale_is_zero()
        {
            _sequence.IgnoreTimeScale = true;
            Time.timeScale = 0f;

            _sequence.Play(Sequence.Named("timed").Wait(0.1f).Build());

            yield return Await.Until(() => _sequence.Runner.State == SequenceState.Completed, "the timed step completes with the time scale at zero");
        }

        [UnityTest]
        public IEnumerator A_sequence_that_follows_the_time_scale_stands_still_while_the_time_scale_is_zero()
        {
            Time.timeScale = 0f;

            _sequence.Play(Sequence.Named("timed").Wait(0.1f).Build());
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(_sequence.Runner.State, Is.EqualTo(SequenceState.Running));
        }

        [UnityTest]
        public IEnumerator A_reminder_on_the_unscaled_clock_repeats_while_the_time_scale_is_zero()
        {
            var voice = new ManualVoicePlayer();
            var step = ReminderStepOn(TaskClocks.Unscaled, voice);
            Time.timeScale = 0f;

            step.Enter();

            yield return Await.Until(() => voice.Played.Count >= 2, "the reminder plays the instruction again");
            step.Exit(ExitReason.Cancelled);
        }

        [UnityTest]
        public IEnumerator A_reminder_on_the_scaled_clock_waits_while_the_time_scale_is_zero()
        {
            var voice = new ManualVoicePlayer();
            var step = ReminderStepOn(TaskClocks.Scaled, voice);
            Time.timeScale = 0f;

            step.Enter();
            yield return new WaitForSecondsRealtime(ReminderSeconds * 4f);

            Assert.That(voice.Played.Count, Is.EqualTo(1));
            step.Exit(ExitReason.Cancelled);
        }

        [UnityTest]
        public IEnumerator A_sequence_that_ignores_the_time_scale_gives_its_guided_tasks_the_unscaled_clock()
        {
            var task = _host.AddComponent<ReminderTask>();
            _sequence.AddTask(task);
            _sequence.IgnoreTimeScale = true;

            _sequence.Run();
            yield return null;

            Assert.That(task.Clock, Is.SameAs(TaskClocks.Unscaled));
        }

        [UnityTest]
        public IEnumerator A_sequence_that_follows_the_time_scale_gives_its_guided_tasks_the_scaled_clock()
        {
            var task = _host.AddComponent<ReminderTask>();
            _sequence.AddTask(task);

            _sequence.Run();
            yield return null;

            Assert.That(task.Clock, Is.SameAs(TaskClocks.Scaled));
        }

        [UnityTest]
        public IEnumerator A_sequence_that_ignores_the_time_scale_gives_its_unguided_tasks_the_unscaled_clock()
        {
            var task = _host.AddComponent<ReminderTask>();
            _sequence.AddTask(task);
            _sequence.IgnoreTimeScale = true;
            _sequence.SetMode(GuidanceMode.Unguided);

            _sequence.Run();
            yield return null;

            Assert.That(task.Clock, Is.SameAs(TaskClocks.Unscaled));
        }

        private TaskStep ReminderStepOn(ITaskClock clock, ManualVoicePlayer voice)
        {
            var task = _host.AddComponent<ReminderTask>();
            SetField(task, "_reminderInterval", ReminderSeconds);
            SetField(task, "_instructionalAudio", new MyClip { name = "instruction", clip = AudioClip.Create("instruction", 100, 1, 44100, false) });
            return new TaskStep(task, voice, clock);
        }

        private static void SetField(object target, string name, object value)
        {
            typeof(GuidedTask).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class ReminderTask : GuidedTask
        {
            protected override void BeginCompletionTracking()
            {
            }

            protected override void EndCompletionTracking()
            {
            }

            protected override void ResetCompletionTracking()
            {
            }
        }
    }
}
