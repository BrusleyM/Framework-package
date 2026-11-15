using System.Collections;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class SequentialTaskPlayModeTests
    {
        private GameObject _host;
        private SequentialTask _sequence;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("sequence host");
            _sequence = _host.AddComponent<SequentialTask>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator Tasks_run_one_after_another_and_the_sequence_completes_when_the_last_one_does()
        {
            var first = AddTask("first");
            var second = AddTask("second");

            _sequence.Run();
            yield return null;
            Assert.AreSame(first, _sequence.CurrentTask);

            first.Finish();
            yield return null;
            Assert.AreSame(second, _sequence.CurrentTask);

            second.Finish();
            yield return null;
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator A_timed_step_only_advances_while_the_sequence_is_not_paused()
        {
            _sequence.Play(Sequence.Named("timed").Wait(0.3f).Build());
            yield return null;

            _sequence.Pause();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(SequenceState.Paused, _sequence.Runner.State);

            _sequence.Resume();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Going_back_returns_to_the_previous_task_and_restart_returns_to_the_first()
        {
            var first = AddTask("first");
            AddTask("second");
            _sequence.Run();
            yield return null;

            _sequence.SkipCurrentTask();
            yield return null;
            Assert.AreEqual(1, _sequence.Runner.CurrentIndex);

            _sequence.GoBack();
            yield return null;
            Assert.AreSame(first, _sequence.CurrentTask);

            _sequence.SkipCurrentTask();
            _sequence.Restart();
            yield return null;
            Assert.AreEqual(0, _sequence.Runner.CurrentIndex);
        }

        [UnityTest]
        public IEnumerator Cancelling_cleans_up_the_running_task_and_a_new_run_starts_over()
        {
            var first = AddTask("first");
            _sequence.Run();
            yield return null;

            _sequence.Cancel();
            yield return null;
            Assert.AreEqual(SequenceState.Cancelled, _sequence.Runner.State);
            Assert.AreEqual(1, first.CancelCount);

            _sequence.Run();
            yield return null;
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
            Assert.AreEqual(0, _sequence.Runner.CurrentIndex);
        }

        [UnityTest]
        public IEnumerator Restarting_a_finished_sequence_keeps_it_ticking_so_it_finishes_again()
        {
            _sequence.Play(Sequence.Named("timed").Wait(0.1f).Build());
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);

            _sequence.Restart();
            yield return null;
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Disabling_the_player_cancels_the_running_sequence()
        {
            AddTask("first");
            _sequence.Run();
            yield return null;

            _sequence.enabled = false;

            Assert.AreEqual(SequenceState.Cancelled, _sequence.Runner.State);
        }

        private ProbeTask AddTask(string taskName)
        {
            var task = _host.AddComponent<ProbeTask>();
            task.Label = taskName;
            _sequence.AddTask(task);
            return task;
        }

        private sealed class ProbeTask : Task
        {
            public string Label { get; set; }
            public int CancelCount { get; private set; }

            public void Finish() => TaskComplete();

            public override void StartTask()
            {
            }

            public override void CancelTask() => CancelCount++;
        }
    }
}
