using System.Linq;
using Framework.Tasks;
using NUnit.Framework;
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
            typeof(GuidedTask).GetField("_subTasks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_task, new GameObjectTask[] { _guidance });
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
        public void The_task_assembly_does_not_depend_on_check_points_or_outlines()
        {
            var referenced = typeof(GuidedTask).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToList();

            Assert.That(referenced, Does.Not.Contain("Framework.CheckPoints"));
            Assert.That(referenced, Does.Not.Contain("Framework.Outline"));
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
