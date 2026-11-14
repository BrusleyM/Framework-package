using System.Collections;
using System.Linq;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class SampleSceneSequenceTests
    {
        private SequentialTask _sequence;

        [UnitySetUp]
        public IEnumerator LoadSampleScene()
        {
#if UNITY_EDITOR
            var sceneGuid = AssetDatabase.FindAssets("Test scene t:Scene").FirstOrDefault();
            Assert.IsNotNull(sceneGuid, "The Test scene sample is not imported in this project.");

            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("Sample scene tests run in the editor only.");
            yield break;
#endif

            _sequence = Object.FindFirstObjectByType<SequentialTask>();
            Assert.IsNotNull(_sequence, "The sample scene has no SequentialTask.");
            _sequence.Cancel();
        }

        [UnityTest]
        public IEnumerator Skipping_every_task_walks_the_whole_sample_sequence_to_completion()
        {
            var taskCount = _sequence.TaskList.Count;
            Assert.Greater(taskCount, 0);

            _sequence.Run();
            yield return null;

            for (var attempt = 0; attempt < taskCount * 2 && _sequence.IsRunning; attempt++)
            {
                _sequence.SkipCurrentTask();
                yield return null;
            }

            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Pausing_freezes_the_current_task_and_resuming_continues_it()
        {
            _sequence.Run();
            yield return null;
            var taskWhenPaused = _sequence.CurrentTask;

            _sequence.Pause();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(SequenceState.Paused, _sequence.Runner.State);
            Assert.AreSame(taskWhenPaused, _sequence.CurrentTask);

            _sequence.Resume();
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Going_back_returns_to_the_previous_task_and_restart_returns_to_the_first()
        {
            _sequence.Run();
            yield return null;
            var firstTask = _sequence.CurrentTask;

            _sequence.SkipCurrentTask();
            yield return null;
            Assert.AreNotSame(firstTask, _sequence.CurrentTask);

            _sequence.GoBack();
            yield return null;
            Assert.AreSame(firstTask, _sequence.CurrentTask);

            _sequence.SkipCurrentTask();
            yield return null;
            _sequence.Restart();
            yield return null;
            Assert.AreSame(firstTask, _sequence.CurrentTask);
        }

        [UnityTest]
        public IEnumerator Cancelling_stops_the_sequence_and_a_fresh_run_starts_over()
        {
            _sequence.Run();
            yield return null;

            _sequence.Cancel();
            yield return null;
            Assert.AreEqual(SequenceState.Cancelled, _sequence.Runner.State);
            Assert.IsFalse(_sequence.IsRunning);

            _sequence.Run();
            yield return null;
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
            Assert.AreEqual(0, _sequence.Runner.CurrentIndex);
        }
    }
}
