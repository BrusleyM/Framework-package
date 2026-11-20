using System.Collections.Generic;
using Framework.Audio;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class GuidedTaskAudioTests
    {
        private GameObject _host;
        private VoiceTask _task;
        private ManualVoicePlayer _voice;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _task = _host.AddComponent<VoiceTask>();
            _voice = new ManualVoicePlayer();
            _task.SetAudioPlayer(_voice);
            PrivateMembers.SetField(_task, "_instructionalAudio", Clip("instruction"));
            PrivateMembers.SetField(_task, "_feedbackAudioClip", Clip("feedback"));
            PrivateMembers.SetField(_task, "_subTasks", new GameObjectTask[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Starting_guided_plays_the_instruction_as_the_tasks_own_audio()
        {
            _task.StartTask();

            Assert.That(_voice.CurrentClip.name, Is.EqualTo("instruction"));
            Assert.That(_voice.IsPlayingFor(_task), Is.True);
        }

        [Test]
        public void Completing_a_guided_task_plays_its_feedback_and_the_task_is_settling_until_it_ends()
        {
            _task.StartTask();

            _task.Answer();
            var settlingWhilePlaying = _task.IsSettling;
            _voice.FinishCurrent();

            Assert.That(_voice.CurrentClip.name, Is.EqualTo("feedback"));
            Assert.That(settlingWhilePlaying, Is.True);
            Assert.That(_task.IsSettling, Is.False);
        }

        [Test]
        public void The_task_is_not_settling_while_only_its_instruction_plays()
        {
            _task.StartTask();

            Assert.That(_task.IsSettling, Is.False);
        }

        [Test]
        public void Completing_a_guided_task_without_a_feedback_clip_stops_its_instruction()
        {
            PrivateMembers.SetField(_task, "_feedbackAudioClip", null);
            _task.StartTask();

            _task.Answer();

            Assert.That(_voice.IsPlaying, Is.False);
            Assert.That(_task.IsSettling, Is.False);
        }

        [Test]
        public void Feedback_that_another_owner_replaced_no_longer_counts_as_settling()
        {
            _task.StartTask();
            _task.Answer();

            _voice.Play(Clip("someone else").clip, new object());

            Assert.That(_task.IsSettling, Is.False);
        }

        [Test]
        public void An_unguided_task_plays_no_feedback_when_it_completes()
        {
            _task.StartTaskUnguided();

            _task.Answer();

            Assert.That(_voice.Played, Is.Empty);
        }

        [Test]
        public void Audio_of_another_owner_does_not_make_the_task_settling_and_is_not_stopped_when_it_is_skipped()
        {
            var other = new object();
            _voice.Play(Clip("someone else").clip, other);
            _task.StartTaskUnguided();

            var settling = _task.IsSettling;
            _task.Skip();

            Assert.That(settling, Is.False);
            Assert.That(_voice.IsPlayingFor(other), Is.True);
        }

        [Test]
        public void Skipping_a_guided_task_stops_its_own_instruction()
        {
            _task.StartTask();

            _task.Skip();

            Assert.That(_voice.IsPlaying, Is.False);
        }

        [Test]
        public void A_hint_plays_the_instruction_and_completing_the_hinted_task_stops_it()
        {
            _task.StartTaskUnguided();

            _task.ShowHint();
            var hintPlaying = _voice.IsPlayingFor(_task);
            _task.Answer();

            Assert.That(hintPlaying, Is.True);
            Assert.That(_voice.IsPlaying, Is.False);
        }

        [Test]
        public void Hiding_the_hint_stops_the_hint_audio()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();

            _task.HideHint();

            Assert.That(_voice.IsPlaying, Is.False);
        }

        [Test]
        public void Pausing_the_task_pauses_its_audio_and_resuming_resumes_it()
        {
            _task.StartTask();

            _task.PauseTask();
            var paused = _voice.IsPaused;
            _task.ResumeTask();

            Assert.That(paused, Is.True);
            Assert.That(_voice.IsPaused, Is.False);
        }

        [Test]
        public void A_task_without_instruction_audio_does_not_interrupt_what_is_playing()
        {
            PrivateMembers.SetField(_task, "_instructionalAudio", null);
            var other = new object();
            _voice.Play(Clip("narration").clip, other);

            _task.StartTask();

            Assert.That(_voice.IsPlayingFor(other), Is.True);
        }

        private static MyClip Clip(string name)
        {
            return new MyClip { name = name, clip = AudioClip.Create(name, 100, 1, 44100, false) };
        }

        private sealed class VoiceTask : GuidedTask
        {
            public void Answer() => TaskComplete();

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
