using System.Collections.Generic;
using Framework.Audio;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class AudioTaskTests
    {
        private GameObject _host;
        private AudioTask _task;
        private ManualVoicePlayer _voice;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _task = _host.AddComponent<AudioTask>();
            _voice = new ManualVoicePlayer();
            _task.SetAudioPlayer(_voice);
            PrivateMembers.SetField(_task, "_audioClips", new List<MyClip> { Clip("one"), null, Clip("two") });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void The_clips_play_one_after_another_skipping_missing_ones_and_the_task_completes_after_the_last()
        {
            _task.StartTask();
            var first = _voice.CurrentClip.name;
            _voice.FinishCurrent();
            var second = _voice.CurrentClip.name;
            var completedEarly = _task.IsComplete;
            _voice.FinishCurrent();

            Assert.That(first, Is.EqualTo("one"));
            Assert.That(second, Is.EqualTo("two"));
            Assert.That(completedEarly, Is.False);
            Assert.That(_task.IsComplete, Is.True);
        }

        [Test]
        public void Pausing_holds_the_clip_and_resuming_lets_it_continue()
        {
            _task.StartTask();

            _task.PauseTask();
            _voice.FinishCurrent();
            var heldOn = _voice.CurrentClip.name;
            _task.ResumeTask();
            _voice.FinishCurrent();

            Assert.That(heldOn, Is.EqualTo("one"));
            Assert.That(_voice.CurrentClip.name, Is.EqualTo("two"));
        }

        [Test]
        public void Cancelling_stops_only_its_own_audio_and_the_chain_ends()
        {
            _task.StartTask();

            _task.CancelTask();
            _voice.FinishCurrent();

            Assert.That(_voice.IsPlaying, Is.False);
            Assert.That(_task.IsComplete, Is.False);
            Assert.That(_voice.Played.Count, Is.EqualTo(1));
        }

        [Test]
        public void Cancelling_does_not_stop_audio_that_belongs_to_someone_else()
        {
            _task.StartTask();
            var other = new object();
            _voice.Play(Clip("narration").clip, other);

            _task.CancelTask();

            Assert.That(_voice.IsPlayingFor(other), Is.True);
        }

        [Test]
        public void Skipping_completes_the_task_as_skipped_and_stops_its_audio()
        {
            _task.StartTask();

            _task.Skip();

            Assert.That(_task.IsSkip, Is.True);
            Assert.That(_task.IsComplete, Is.True);
            Assert.That(_voice.IsPlaying, Is.False);
        }

        [Test]
        public void A_task_with_no_clips_completes_at_once()
        {
            PrivateMembers.SetField(_task, "_audioClips", new List<MyClip>());
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No audio clips"));

            _task.StartTask();

            Assert.That(_task.IsComplete, Is.True);
        }

        private static MyClip Clip(string name)
        {
            return new MyClip { name = name, clip = AudioClip.Create(name, 100, 1, 44100, false) };
        }
    }
}
