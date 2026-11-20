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

        [UnityTest]
        public IEnumerator In_unguided_mode_every_task_of_the_same_list_starts_at_once_without_guidance()
        {
            var first = AddTask("first");
            var second = AddTask("second");
            _sequence.SetMode(GuidanceMode.Unguided);

            _sequence.Run();
            yield return null;

            Assert.AreEqual(1, first.UnguidedStartCount);
            Assert.AreEqual(1, second.UnguidedStartCount);
            Assert.AreEqual(0, first.StartCount);
        }

        [UnityTest]
        public IEnumerator In_unguided_mode_tasks_finish_in_any_order_and_the_sequence_completes_after_the_last()
        {
            var first = AddTask("first");
            var second = AddTask("second");
            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.Run();
            yield return null;

            second.Finish();
            yield return null;
            var stateAfterOne = _sequence.Runner.State;
            first.Finish();
            yield return null;

            Assert.AreEqual(SequenceState.Running, stateAfterOne);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
            CollectionAssert.AreEqual(new List<Task> { second, first }, new List<Task>(_sequence.CompletionOrder));
        }

        [UnityTest]
        public IEnumerator In_unguided_mode_the_sequence_asks_for_a_hint_and_reports_each_finished_task_with_its_position()
        {
            var first = AddTask("first");
            var second = AddTask("second");
            var reports = new List<string>();
            _sequence.UnguidedTaskCompleted += (task, position) => reports.Add(position + ":" + ((ProbeTask)task).Label);
            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.Run();
            yield return null;

            var hinted = _sequence.RequestHint();
            second.Finish();
            yield return null;

            Assert.IsTrue(hinted);
            Assert.AreEqual(1, first.HintShowCount);
            CollectionAssert.AreEqual(new List<string> { "1:second" }, reports);
        }

        [UnityTest]
        public IEnumerator In_guided_mode_there_is_no_hint_to_ask_for()
        {
            AddTask("first");
            _sequence.Run();
            yield return null;

            Assert.IsFalse(_sequence.RequestHint());
        }

        [UnityTest]
        public IEnumerator Changing_the_mode_while_running_restarts_the_tasks_in_the_new_mode_and_reports_the_change()
        {
            var task = AddTask("only");
            var reported = new List<GuidanceMode>();
            _sequence.ModeChanged += reported.Add;
            _sequence.Run();
            yield return null;

            _sequence.SetMode(GuidanceMode.Unguided);
            yield return null;

            CollectionAssert.AreEqual(new List<GuidanceMode> { GuidanceMode.Unguided }, reported);
            Assert.AreEqual(1, task.CancelCount);
            Assert.AreEqual(1, task.UnguidedStartCount);
            Assert.IsTrue(_sequence.IsRunning);
        }

        [UnityTest]
        public IEnumerator Setting_the_same_mode_again_changes_nothing()
        {
            var task = AddTask("only");
            _sequence.Run();
            yield return null;

            _sequence.SetMode(GuidanceMode.Guided);

            Assert.AreEqual(0, task.CancelCount);
        }

        [UnityTest]
        public IEnumerator A_guidance_only_task_such_as_audio_is_left_out_in_unguided_mode()
        {
            var guidance = AddTask("welcome");
            guidance.GuidanceOnly = true;
            var real = AddTask("real");
            _sequence.SetMode(GuidanceMode.Unguided);

            _sequence.Run();
            yield return null;

            Assert.AreEqual(0, guidance.UnguidedStartCount);
            Assert.AreEqual(1, real.UnguidedStartCount);
        }

        [UnityTest]
        public IEnumerator The_intro_audio_plays_before_the_first_task_starts_in_either_mode()
        {
            var task = AddTask("only");
            UseAudio(intro: new[] { Clip("intro", 0.3f) });

            _sequence.Run();
            yield return null;
            var startedDuringIntro = task.StartCount;
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual(0, startedDuringIntro);
            Assert.AreEqual(1, task.StartCount);
        }

        [UnityTest]
        public IEnumerator The_intro_is_also_played_before_the_unguided_tasks()
        {
            var task = AddTask("only");
            UseAudio(intro: new[] { Clip("intro", 0.3f) });
            _sequence.SetMode(GuidanceMode.Unguided);

            _sequence.Run();
            yield return null;
            var startedDuringIntro = task.UnguidedStartCount;
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual(0, startedDuringIntro);
            Assert.AreEqual(1, task.UnguidedStartCount);
        }

        [UnityTest]
        public IEnumerator The_complete_audio_plays_after_the_last_task_and_only_then_the_sequence_completes()
        {
            var task = AddTask("only");
            UseAudio(complete: new[] { Clip("complete", 0.4f) });
            _sequence.Run();
            yield return null;

            task.Finish();
            yield return null;
            var stateWhilePlaying = _sequence.Runner.State;
            yield return new WaitForSeconds(0.9f);

            Assert.AreEqual(SequenceState.Running, stateWhilePlaying);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator The_complete_audio_plays_after_the_unguided_tasks_are_all_done()
        {
            var first = AddTask("first");
            var second = AddTask("second");
            UseAudio(complete: new[] { Clip("complete", 0.4f) });
            _sequence.SetMode(GuidanceMode.Unguided);
            _sequence.Run();
            yield return null;

            second.Finish();
            first.Finish();
            yield return null;
            var stateWhilePlaying = _sequence.Runner.State;
            yield return new WaitForSeconds(0.9f);

            Assert.AreEqual(SequenceState.Running, stateWhilePlaying);
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Switching_the_mode_while_running_does_not_play_the_intro_again()
        {
            var task = AddTask("only");
            UseAudio(intro: new[] { Clip("intro", 0.2f) });
            _sequence.Run();
            yield return new WaitForSeconds(0.6f);

            _sequence.SetMode(GuidanceMode.Unguided);
            yield return null;

            Assert.AreEqual(1, task.UnguidedStartCount);
        }

        [UnityTest]
        public IEnumerator Without_intro_or_complete_audio_the_sequence_has_only_its_tasks()
        {
            AddTask("only");
            UseAudio();

            _sequence.Run();
            yield return null;

            Assert.AreEqual(1, _sequence.Runner.StepCount);
        }

        private void UseAudio(MyClip[] intro = null, MyClip[] complete = null)
        {
            var source = _host.AddComponent<AudioSource>();
            Set("_audioSource", source);
            Set("_introAudio", new List<MyClip>(intro ?? new MyClip[0]));
            Set("_completeAudio", new List<MyClip>(complete ?? new MyClip[0]));
        }

        private void Set(string fieldName, object value)
        {
            typeof(SequentialTask).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_sequence, value);
        }

        private static MyClip Clip(string name, float seconds)
        {
            var samples = (int)(44100 * seconds);
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            clip.SetData(new float[samples], 0);
            return new MyClip { name = name, clip = clip };
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
