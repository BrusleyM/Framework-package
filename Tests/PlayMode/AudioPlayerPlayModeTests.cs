using System.Collections;
using Framework.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class AudioPlayerPlayModeTests
    {
        private GameObject _host;
        private AudioSource _source;
        private AudioPlayer _player;
        private readonly object _owner = new object();
        private readonly object _other = new object();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("audio host");
            _source = _host.AddComponent<AudioSource>();
            _player = new AudioPlayer(_source);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator The_finished_callback_is_raised_once_when_the_clip_ends()
        {
            var finished = 0;

            _player.Play(Clip(0.2f), _owner, () => finished++);
            yield return new WaitForSeconds(0.7f);

            Assert.That(finished, Is.EqualTo(1));
            Assert.That(_player.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator Stopping_by_another_owner_does_nothing_and_stopping_by_the_owner_ends_it_without_the_callback()
        {
            var finished = 0;
            _player.Play(Clip(1f), _owner, () => finished++);
            yield return null;

            _player.Stop(_other);
            var stillPlaying = _source.isPlaying;
            _player.Stop(_owner);
            yield return new WaitForSeconds(0.3f);

            Assert.That(stillPlaying, Is.True);
            Assert.That(_source.isPlaying, Is.False);
            Assert.That(finished, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator A_paused_clip_does_not_finish_until_it_is_resumed()
        {
            var finished = 0;
            _player.Play(Clip(0.3f), _owner, () => finished++);
            yield return null;

            _player.Pause(_owner);
            yield return new WaitForSeconds(0.6f);
            var finishedWhilePaused = finished;
            _player.Resume(_owner);
            yield return new WaitForSeconds(0.6f);

            Assert.That(finishedWhilePaused, Is.EqualTo(0));
            Assert.That(finished, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Another_owner_playing_replaces_the_first_and_only_the_new_callback_is_raised()
        {
            var firstFinished = 0;
            var secondFinished = 0;
            _player.Play(Clip(1f), _owner, () => firstFinished++);
            yield return null;

            _player.Play(Clip(0.2f), _other, () => secondFinished++);
            yield return new WaitForSeconds(0.7f);

            Assert.That(firstFinished, Is.EqualTo(0));
            Assert.That(secondFinished, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Playing_for_an_owner_is_only_true_for_that_owner()
        {
            _player.Play(Clip(1f), _owner);
            yield return null;

            Assert.That(_player.IsPlayingFor(_owner), Is.True);
            Assert.That(_player.IsPlayingFor(_other), Is.False);
        }

        [UnityTest]
        public IEnumerator Playing_on_an_inactive_audio_source_skips_the_clip_and_still_raises_the_callback()
        {
            var finished = 0;
            _source.enabled = false;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("inactive"));

            _player.Play(Clip(0.5f), _owner, () => finished++);
            yield return null;

            Assert.That(finished, Is.EqualTo(1));
            Assert.That(_player.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator A_second_player_on_the_same_source_takes_over_and_the_first_one_goes_quiet()
        {
            var firstFinished = 0;
            _player.Play(Clip(1f), _owner, () => firstFinished++);
            yield return null;

            var second = new AudioPlayer(_source);
            var secondFinished = 0;
            second.Play(Clip(0.2f), _other, () => secondFinished++);
            _player.Play(Clip(0.2f), _owner, () => firstFinished++);
            yield return new WaitForSeconds(0.7f);

            Assert.That(firstFinished, Is.EqualTo(0));
            Assert.That(secondFinished, Is.EqualTo(1));
            Assert.That(_player.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator Stopping_after_the_audio_host_was_destroyed_does_not_throw()
        {
            _player.Play(Clip(1f), _owner);
            yield return null;
            Object.Destroy(_host);
            yield return null;

            Assert.DoesNotThrow(() => _player.Stop(_owner));
            Assert.DoesNotThrow(() => _player.StopAll());
        }

        private static AudioClip Clip(float seconds)
        {
            var samples = (int)(44100 * seconds);
            var clip = AudioClip.Create("clip", samples, 1, 44100, false);
            clip.SetData(new float[samples], 0);
            return clip;
        }
    }
}
