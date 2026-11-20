using System.Collections.Generic;
using Framework.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class AudioStepTests
    {
        private ManualVoicePlayer _voice;
        private int _finishes;

        [SetUp]
        public void SetUp()
        {
            _voice = new ManualVoicePlayer();
            _finishes = 0;
        }

        [Test]
        public void Entering_plays_the_first_clip_for_the_step_itself()
        {
            var step = NewStep("one", "two");

            step.Enter();

            Assert.That(_voice.CurrentClip.name, Is.EqualTo("one"));
            Assert.That(_voice.IsPlayingFor(step), Is.True);
        }

        [Test]
        public void Each_clip_ends_with_the_next_one_starting_and_the_step_finishes_after_the_last()
        {
            var step = NewStep("one", "two");
            step.Enter();

            _voice.FinishCurrent();
            var secondPlaying = _voice.CurrentClip.name;
            var finishedEarly = _finishes;
            _voice.FinishCurrent();

            Assert.That(secondPlaying, Is.EqualTo("two"));
            Assert.That(finishedEarly, Is.EqualTo(0));
            Assert.That(_finishes, Is.EqualTo(1));
        }

        [Test]
        public void A_step_with_no_clips_finishes_at_once()
        {
            var step = NewStep();

            step.Enter();

            Assert.That(_finishes, Is.EqualTo(1));
        }

        [Test]
        public void Missing_clips_are_left_out()
        {
            var step = new AudioStep("voice", new[] { new MyClip(), null, Clip("only") }, _voice);
            step.Finished += () => _finishes++;

            step.Enter();

            Assert.That(_voice.CurrentClip.name, Is.EqualTo("only"));
        }

        [Test]
        public void Pausing_and_resuming_the_step_pauses_and_resumes_its_audio_and_a_paused_clip_does_not_advance()
        {
            var step = NewStep("one", "two");
            step.Enter();

            step.Pause();
            var paused = _voice.IsPaused;
            _voice.FinishCurrent();
            step.Resume();

            Assert.That(paused, Is.True);
            Assert.That(_voice.CurrentClip.name, Is.EqualTo("one"));
            Assert.That(_voice.IsPaused, Is.False);
        }

        [Test]
        public void Leaving_the_step_early_stops_its_audio_and_the_chain()
        {
            var step = NewStep("one", "two");
            step.Enter();

            step.Exit(ExitReason.Cancelled);
            _voice.FinishCurrent();

            Assert.That(_voice.IsPlaying, Is.False);
            Assert.That(_voice.Played.Count, Is.EqualTo(1));
            Assert.That(_finishes, Is.EqualTo(0));
        }

        [Test]
        public void Another_owner_playing_does_not_get_cut_by_this_step_exiting()
        {
            var step = NewStep("one");
            step.Enter();
            var other = new object();
            _voice.Play(Clip("other").clip, other);

            step.Exit(ExitReason.Cancelled);

            Assert.That(_voice.IsPlayingFor(other), Is.True);
        }

        [Test]
        public void A_step_without_a_voice_player_finishes_at_once()
        {
            var step = new AudioStep("voice", new[] { Clip("one") }, null);
            step.Finished += () => _finishes++;

            step.Enter();

            Assert.That(_finishes, Is.EqualTo(1));
        }

        private AudioStep NewStep(params string[] clipNames)
        {
            var clips = new List<MyClip>();
            foreach (var name in clipNames)
            {
                clips.Add(Clip(name));
            }

            var step = new AudioStep("voice", clips, _voice);
            step.Finished += () => _finishes++;
            return step;
        }

        private static MyClip Clip(string name)
        {
            return new MyClip { name = name, clip = AudioClip.Create(name, 100, 1, 44100, false) };
        }
    }
}
