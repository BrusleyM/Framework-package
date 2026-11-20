using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Framework.Audio;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class AudioTaskPlayModeTests
    {
        private GameObject _host;
        private AudioTask _task;
        private AudioSource _source;

        [UnitySetUp]
        public IEnumerator RequireAnAudioDevice()
        {
            yield return AudioDevice.RequireOrIgnore();
        }

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("audio host");
            _source = _host.AddComponent<AudioSource>();
            _task = _host.AddComponent<AudioTask>();
            var clips = new List<MyClip> { Clip("first"), Clip("second") };
            typeof(AudioTask).GetField("_audioClips", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_task, clips);
            _task.SetAudioPlayer(new AudioPlayer(_source));
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator A_paused_audio_task_holds_on_the_clip_it_was_playing_and_does_not_move_on()
        {
            _task.StartTask();
            yield return null;

            _task.PauseTask();
            yield return new WaitForSeconds(0.8f);

            Assert.That(_source.clip.name, Is.EqualTo("first"));
            Assert.That(_task.IsComplete, Is.False);
        }

        [UnityTest]
        public IEnumerator A_resumed_audio_task_plays_the_rest_and_completes()
        {
            _task.StartTask();
            yield return null;
            _task.PauseTask();
            yield return new WaitForSeconds(0.3f);

            _task.ResumeTask();
            yield return Await.Until(() => _task.IsComplete, "the resumed audio task completes");

            Assert.That(_task.IsComplete, Is.True);
        }

        private static MyClip Clip(string name)
        {
            var samples = 22050;
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            clip.SetData(new float[samples], 0);
            return new MyClip { name = name, clip = clip };
        }
    }
}
