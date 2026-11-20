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
            yield return Until(() => finished > 0);

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
        public IEnumerator A_second_player_on_the_same_source_takes_over_and_the_first_forwards_to_it()
        {
            var droppedFinished = 0;
            _player.Play(Clip(1f), _owner, () => droppedFinished++);
            yield return null;

            var second = new AudioPlayer(_source);
            var forwardedFinished = 0;
            _player.Play(Clip(0.2f), _other, () => forwardedFinished++);
            var playingThroughSecond = second.IsPlayingFor(_other);
            yield return Until(() => forwardedFinished > 0);

            Assert.That(droppedFinished, Is.EqualTo(0));
            Assert.That(playingThroughSecond, Is.True);
            Assert.That(forwardedFinished, Is.EqualTo(1));
            Assert.That(second.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator Deactivating_the_audio_host_mid_clip_finishes_the_clip_so_a_chain_does_not_hang()
        {
            var finished = 0;
            _player.Play(Clip(2f), _owner, () => finished++);
            yield return null;

            _host.SetActive(false);

            Assert.That(finished, Is.EqualTo(1));
            Assert.That(_player.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator A_chain_that_keeps_playing_from_the_callback_while_the_host_is_deactivated_does_not_recurse_or_warn()
        {
            var plays = 0;
            var inactiveWarnings = 0;
            Application.LogCallback count = (message, stack, type) =>
            {
                if (message.Contains("inactive"))
                {
                    inactiveWarnings++;
                }
            };
            Application.logMessageReceived += count;
            System.Action again = null;
            again = () =>
            {
                plays++;
                _player.Play(Clip(1f), _owner, again);
            };
            _player.Play(Clip(2f), _owner, again);
            yield return null;

            _host.SetActive(false);

            Application.logMessageReceived -= count;
            Assert.That(plays, Is.EqualTo(1));
            Assert.That(inactiveWarnings, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator A_null_owner_is_refused_without_cutting_the_current_audio()
        {
            _player.Play(Clip(1f), _owner);
            yield return null;

            Assert.Throws<System.ArgumentNullException>(() => _player.Play(Clip(0.2f), null));

            Assert.That(_source.isPlaying, Is.True);
            Assert.That(_player.IsPlayingFor(_owner), Is.True);
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

        private static IEnumerator Until(System.Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
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
