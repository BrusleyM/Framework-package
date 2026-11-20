using System.Collections;
using System.Reflection;
using Framework.Audio;
using Framework.CheckPoints;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class ActionTaskAudioPlayModeTests
    {
        private GameObject _host;
        private ActionTask _task;
        private AudioSource _source;

        [UnitySetUp]
        public IEnumerator RequireAnAudioDevice()
        {
            yield return AudioDevice.RequireOrIgnore();
        }

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("action host");
            _source = _host.AddComponent<AudioSource>();
            _task = _host.AddComponent<ActionTask>();
            SetPrivate("_checkPoints", new CheckPointsParent());
            SetPrivate("_subTasks", new GameObjectTask[0]);
            SetPrivate("_instructionalAudio", Clip("instruction", 3f));
            _task.SetAudioPlayer(new AudioPlayer(_source));
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator Completing_an_unguided_task_stops_the_hint_audio_that_was_playing()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();
            yield return null;
            var playingWhileHinting = _source.isPlaying;

            _task.CheckPointsParent.OnAllChecked.Invoke();
            yield return null;

            Assert.IsTrue(playingWhileHinting);
            Assert.IsFalse(_source.isPlaying);
            Assert.IsFalse(_task.IsSettling);
        }

        private void SetPrivate(string fieldName, object value)
        {
            for (var type = _task.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(_task, value);
                    return;
                }
            }

            throw new System.MissingFieldException(_task.GetType().Name, fieldName);
        }

        private static MyClip Clip(string name, float seconds)
        {
            var samples = (int)(44100 * seconds);
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            clip.SetData(new float[samples], 0);
            return new MyClip { name = name, clip = clip };
        }
    }
}
