using System.Linq;
using Framework.Tasks;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class GuidedTaskTests
    {
        private GameObject _host;
        private AnswerTask _task;
        private RecordingSubtask _guidance;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _task = _host.AddComponent<AnswerTask>();
            _guidance = _host.AddComponent<RecordingSubtask>();
            PrivateMembers.SetField(_task, "_subTasks", new GameObjectTask[] { _guidance });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void A_guided_task_that_has_nothing_to_do_with_check_points_runs_its_guidance_when_started()
        {
            _task.StartTask();

            Assert.That(_task.BeganTracking, Is.EqualTo(1));
            Assert.That(_guidance.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void It_completes_through_its_own_completion_hook()
        {
            var completions = 0;
            _task.Completed += () => completions++;
            _task.StartTask();
            var endedBeforeAnswer = _task.EndedTracking;

            _task.Answer();

            Assert.That(_task.IsComplete, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(_task.EndedTracking, Is.EqualTo(endedBeforeAnswer + 1));
        }

        [Test]
        public void Started_unguided_it_tracks_completion_but_runs_no_guidance_until_a_hint_is_asked_for()
        {
            _task.StartTaskUnguided();
            var beforeHint = _guidance.ExecuteCount;

            _task.ShowHint();

            Assert.That(_task.BeganTracking, Is.EqualTo(1));
            Assert.That(beforeHint, Is.EqualTo(0));
            Assert.That(_guidance.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Cancelling_it_resets_the_completion_tracking()
        {
            _task.StartTask();

            _task.CancelTask();

            Assert.That(_task.ResetTracking, Is.EqualTo(1));
            Assert.That(_guidance.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void Ending_completion_tracking_before_it_began_does_not_throw()
        {
            Assert.DoesNotThrow(() => PrivateMembers.Invoke(_task, "OnDisable"));
            Assert.That(_task.EndedTracking, Is.EqualTo(1));
        }

        [Test]
        public void Skipping_a_task_ends_the_tracking_but_does_not_reset_it()
        {
            _task.StartTask();
            var resetsBefore = _task.ResetTracking;

            _task.Skip();

            Assert.That(_task.IsSkip, Is.True);
            Assert.That(_task.ResetTracking, Is.EqualTo(resetsBefore));
        }

        [Test]
        public void The_task_assembly_is_declared_without_the_check_point_and_outline_assemblies()
        {
            var tasks = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Single(assembly => assembly.name == "Framework.Tasks");

            var declared = tasks.assemblyReferences.Select(assembly => assembly.name).ToList();

            Assert.That(declared, Does.Contain("Framework.Audio"));
            Assert.That(declared, Does.Not.Contain("Framework.CheckPoints"));
            Assert.That(declared, Does.Not.Contain("Framework.Outline"));
        }

        [Test]
        public void A_new_task_is_idle()
        {
            Assert.That(_task.State, Is.EqualTo(GuidanceState.Idle));
        }

        [Test]
        public void Starting_guided_is_the_guided_state_and_starting_unguided_the_unguided_state()
        {
            _task.StartTask();
            var guided = _task.State;
            _task.StartTaskUnguided();

            Assert.That(guided, Is.EqualTo(GuidanceState.Guided));
            Assert.That(_task.State, Is.EqualTo(GuidanceState.Unguided));
        }

        [Test]
        public void A_hint_moves_an_unguided_task_to_hinting_and_hiding_it_moves_it_back()
        {
            _task.StartTaskUnguided();

            _task.ShowHint();
            var hinting = _task.State;
            _task.HideHint();

            Assert.That(hinting, Is.EqualTo(GuidanceState.Hinting));
            Assert.That(_task.State, Is.EqualTo(GuidanceState.Unguided));
        }

        [Test]
        public void Asking_for_a_hint_twice_runs_the_guidance_once()
        {
            _task.StartTaskUnguided();

            _task.ShowHint();
            _task.ShowHint();

            Assert.That(_guidance.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void A_hint_is_ignored_unless_the_task_is_unguided()
        {
            _task.ShowHint();
            var whileIdle = _task.State;
            _task.StartTask();
            var executedByStart = _guidance.ExecuteCount;
            _task.ShowHint();

            Assert.That(whileIdle, Is.EqualTo(GuidanceState.Idle));
            Assert.That(_task.State, Is.EqualTo(GuidanceState.Guided));
            Assert.That(_guidance.ExecuteCount, Is.EqualTo(executedByStart));
        }

        [Test]
        public void Completing_from_guided_unguided_or_hinting_ends_in_completed()
        {
            var states = new System.Collections.Generic.List<GuidanceState>();

            _task.StartTask();
            _task.Answer();
            states.Add(_task.State);
            _task.Prepare();
            _task.StartTaskUnguided();
            _task.Answer();
            states.Add(_task.State);
            _task.Prepare();
            _task.StartTaskUnguided();
            _task.ShowHint();
            _task.Answer();
            states.Add(_task.State);

            Assert.That(states, Is.EqualTo(new[] { GuidanceState.Completed, GuidanceState.Completed, GuidanceState.Completed }));
        }

        [Test]
        public void Completing_while_hinting_takes_the_hint_down_but_a_task_that_is_not_hinting_cancels_nothing()
        {
            _task.StartTaskUnguided();
            _task.Answer();
            var cancelledWithoutHint = _guidance.CancelCount;
            _task.Prepare();
            _task.StartTaskUnguided();
            _task.ShowHint();

            _task.Answer();

            Assert.That(cancelledWithoutHint, Is.EqualTo(0));
            Assert.That(_guidance.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void Skipping_completes_the_task_as_skipped_and_cancels_all_subtasks()
        {
            _task.StartTask();

            _task.Skip();

            Assert.That(_task.State, Is.EqualTo(GuidanceState.Completed));
            Assert.That(_task.IsSkip, Is.True);
            Assert.That(_guidance.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void A_completed_task_ignores_further_completion_skip_and_hints()
        {
            _task.StartTaskUnguided();
            _task.Answer();
            var completions = 0;
            _task.Completed += () => completions++;

            _task.Answer();
            _task.Skip();
            _task.ShowHint();

            Assert.That(completions, Is.EqualTo(0));
            Assert.That(_task.State, Is.EqualTo(GuidanceState.Completed));
            Assert.That(_guidance.ExecuteCount, Is.EqualTo(0));
        }

        [Test]
        public void Cancelling_returns_the_task_to_idle()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();

            _task.CancelTask();

            Assert.That(_task.State, Is.EqualTo(GuidanceState.Idle));
        }

        [Test]
        public void Disabling_a_hinting_task_takes_the_hint_down_and_leaves_it_unguided_so_it_can_be_hinted_again()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();

            PrivateMembers.Invoke(_task, "OnDisable");
            var afterDisable = _task.State;
            _task.ShowHint();

            Assert.That(afterDisable, Is.EqualTo(GuidanceState.Unguided));
            Assert.That(_guidance.CancelCount, Is.EqualTo(1));
            Assert.That(_guidance.ExecuteCount, Is.EqualTo(2));
        }

        [Test]
        public void Disabling_a_completed_task_leaves_it_completed()
        {
            _task.StartTask();
            _task.Answer();

            PrivateMembers.Invoke(_task, "OnDisable");

            Assert.That(_task.State, Is.EqualTo(GuidanceState.Completed));
        }

        [Test]
        public void A_completed_task_can_be_prepared_and_started_again()
        {
            _task.StartTask();
            _task.Answer();

            _task.Prepare();
            _task.StartTask();

            Assert.That(_task.State, Is.EqualTo(GuidanceState.Guided));
            Assert.That(_guidance.ExecuteCount, Is.EqualTo(2));
        }

        private sealed class AnswerTask : GuidedTask
        {
            public int BeganTracking { get; private set; }
            public int EndedTracking { get; private set; }
            public int ResetTracking { get; private set; }

            public void Answer() => TaskComplete();

            protected override void BeginCompletionTracking() => BeganTracking++;

            protected override void EndCompletionTracking() => EndedTracking++;

            protected override void ResetCompletionTracking() => ResetTracking++;
        }

        private sealed class RecordingSubtask : GameObjectTask
        {
            public int ExecuteCount { get; private set; }
            public int CancelCount { get; private set; }

            public override void Execute() => ExecuteCount++;

            public override void Cancel() => CancelCount++;
        }
    }
}
