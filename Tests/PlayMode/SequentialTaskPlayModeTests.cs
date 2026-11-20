using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        public IEnumerator Pausing_and_resuming_the_sequence_pauses_and_resumes_the_task_that_is_running()
        {
            var task = AddTask("only");
            _sequence.Run();
            yield return null;

            _sequence.Pause();
            var pausedWhilePaused = task.PauseCount;
            _sequence.Resume();

            Assert.AreEqual(1, pausedWhilePaused);
            Assert.AreEqual(1, task.ResumeCount);
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

        [UnityTest]
        public IEnumerator The_guided_mode_runs_the_guided_list_and_the_unguided_mode_runs_the_unguided_list()
        {
            var guided = AddTask("guided");
            var unguided = AddUnguidedTask("unguided");

            _sequence.Run();
            yield return null;
            var guidedRan = guided.StartCount;
            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.Run();
            yield return null;

            Assert.AreEqual(1, guidedRan);
            Assert.AreEqual(0, unguided.StartCount - 1);
            Assert.AreSame(unguided, _sequence.CurrentTask);
        }

        [UnityTest]
        public IEnumerator Switching_mode_pauses_the_running_mode_and_coming_back_resumes_it_where_it_was()
        {
            var first = AddTask("first");
            var second = AddTask("second");
            AddUnguidedTask("free");
            _sequence.Run();
            yield return null;
            first.Finish();
            yield return null;

            _sequence.SetMode(GuidanceMode.Unguided);
            var guidedRunner = _runnerOfMode(GuidanceMode.Guided);
            var pausedWhileAway = guidedRunner.State;
            _sequence.RunOrResume();
            yield return null;
            _sequence.SetMode(GuidanceMode.Guided);
            _sequence.RunOrResume();
            yield return null;

            Assert.AreEqual(SequenceState.Paused, pausedWhileAway);
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
            Assert.AreSame(second, _sequence.CurrentTask);
        }

        [UnityTest]
        public IEnumerator Each_mode_keeps_its_own_progress_when_the_other_one_runs()
        {
            AddTask("guided one");
            var free = AddUnguidedTask("free one");
            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.Run();
            yield return null;
            free.Finish();
            yield return null;

            _sequence.SetMode(GuidanceMode.Guided);

            Assert.IsFalse(_sequence.HasRun);
            _sequence.SetMode(GuidanceMode.Unguided);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Changing_the_mode_raises_the_mode_changed_event_once_with_the_new_mode()
        {
            var reported = new List<GuidanceMode>();
            _sequence.ModeChanged += reported.Add;

            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.SetMode(GuidanceMode.Unguided);
            yield return null;

            CollectionAssert.AreEqual(new List<GuidanceMode> { GuidanceMode.Unguided }, reported);
        }

        [UnityTest]
        public IEnumerator Run_or_resume_starts_a_mode_that_has_not_run_and_resumes_one_that_is_paused()
        {
            AddTask("only");
            _sequence.RunOrResume();
            yield return null;
            var started = _sequence.HasRun;

            _sequence.Pause();
            _sequence.RunOrResume();

            Assert.IsTrue(started);
            Assert.AreEqual(SequenceState.Running, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Disabling_the_sequence_cancels_the_runs_of_both_modes()
        {
            AddTask("guided");
            AddUnguidedTask("free");
            _sequence.Run();
            yield return null;
            var guidedRunner = _sequence.Runner;
            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.Run();
            yield return null;
            var unguidedRunner = _sequence.Runner;

            _sequence.enabled = false;

            Assert.AreEqual(SequenceState.Cancelled, guidedRunner.State);
            Assert.AreEqual(SequenceState.Cancelled, unguidedRunner.State);
        }

        private SequenceRunner _runnerOfMode(GuidanceMode mode)
        {
            var runners = (SequenceRunner[])typeof(SequentialTask).GetField("_runnersByMode", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_sequence);
            return runners[(int)mode];
        }

        private ProbeTask AddUnguidedTask(string taskName)
        {
            var task = _host.AddComponent<ProbeTask>();
            task.Label = taskName;
            _sequence.UnguidedTaskList.Add(task);
            return task;
        }

        private ProbeTask AddTask(string taskName)
        {
            var task = _host.AddComponent<ProbeTask>();
            task.Label = taskName;
            _sequence.AddTask(task);
            return task;
        }
    }
}
