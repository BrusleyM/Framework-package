using System;
using Framework.Audio;
using NUnit.Framework;

namespace Framework.Sequencing.Tests
{
    public class VoiceChannelTests
    {
        private readonly object _owner = new object();
        private readonly object _other = new object();
        private VoiceChannel _channel;
        private int _finished;

        [SetUp]
        public void SetUp()
        {
            _channel = new VoiceChannel();
            _finished = 0;
        }

        [Test]
        public void An_empty_channel_is_not_playing_and_has_no_owner()
        {
            Assert.That(_channel.IsPlaying, Is.False);
            Assert.That(_channel.Owner, Is.Null);
        }

        [Test]
        public void Beginning_makes_the_owner_the_one_playing()
        {
            _channel.Begin(_owner, null);

            Assert.That(_channel.IsPlayingFor(_owner), Is.True);
            Assert.That(_channel.IsPlayingFor(_other), Is.False);
        }

        [Test]
        public void Audio_without_an_owner_is_refused_so_nobody_can_end_up_unable_to_stop_it()
        {
            Assert.Throws<ArgumentNullException>(() => _channel.Begin(null, null));
        }

        [Test]
        public void Only_the_owner_can_stop_its_audio()
        {
            _channel.Begin(_owner, () => _finished++);

            var stoppedByOther = _channel.Stop(_other);
            var stoppedByOwner = _channel.Stop(_owner);

            Assert.That(stoppedByOther, Is.False);
            Assert.That(stoppedByOwner, Is.True);
            Assert.That(_channel.IsPlaying, Is.False);
        }

        [Test]
        public void Stopping_all_stops_whoever_is_playing()
        {
            _channel.Begin(_owner, null);

            Assert.That(_channel.StopAll(), Is.True);
            Assert.That(_channel.StopAll(), Is.False);
        }

        [Test]
        public void Only_the_owner_can_pause_and_resume_its_audio()
        {
            _channel.Begin(_owner, null);

            var pausedByOther = _channel.Pause(_other);
            var pausedByOwner = _channel.Pause(_owner);
            var resumedByOther = _channel.Resume(_other);
            var stillPaused = _channel.IsPaused;
            var resumedByOwner = _channel.Resume(_owner);

            Assert.That(pausedByOther, Is.False);
            Assert.That(pausedByOwner, Is.True);
            Assert.That(resumedByOther, Is.False);
            Assert.That(stillPaused, Is.True);
            Assert.That(resumedByOwner, Is.True);
            Assert.That(_channel.IsPaused, Is.False);
        }

        [Test]
        public void Finishing_raises_the_callback_once_and_frees_the_channel()
        {
            _channel.Begin(_owner, () => _finished++);

            _channel.Finish();
            _channel.Finish();

            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(_channel.IsPlaying, Is.False);
        }

        [Test]
        public void A_paused_channel_does_not_finish()
        {
            _channel.Begin(_owner, () => _finished++);
            _channel.Pause(_owner);

            _channel.Finish();

            Assert.That(_finished, Is.EqualTo(0));
            Assert.That(_channel.IsPlaying, Is.True);
        }

        [Test]
        public void A_stopped_channel_never_raises_the_callback()
        {
            _channel.Begin(_owner, () => _finished++);
            _channel.Stop(_owner);

            _channel.Finish();

            Assert.That(_finished, Is.EqualTo(0));
        }

        [Test]
        public void A_new_owner_takes_over_and_the_old_owners_callback_is_dropped()
        {
            _channel.Begin(_owner, () => _finished += 10);
            _channel.Begin(_other, () => _finished++);

            _channel.Finish();

            Assert.That(_finished, Is.EqualTo(1));
            Assert.That(_channel.IsPlayingFor(_owner), Is.False);
        }

        [Test]
        public void The_callback_can_start_the_next_clip_for_the_same_owner()
        {
            var plays = 0;
            Action again = null;
            again = () =>
            {
                plays++;
                if (plays < 3)
                {
                    _channel.Begin(_owner, again);
                }
            };
            _channel.Begin(_owner, again);

            _channel.Finish();
            _channel.Finish();
            _channel.Finish();

            Assert.That(plays, Is.EqualTo(3));
            Assert.That(_channel.IsPlaying, Is.False);
        }
    }
}
