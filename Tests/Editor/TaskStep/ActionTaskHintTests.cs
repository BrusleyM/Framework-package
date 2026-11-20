using System.Reflection;
using Framework.CheckPoints;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class ActionTaskHintTests
    {
        private GameObject _host;
        private ActionTask _task;
        private CountingSubtask _highlight;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _task = _host.AddComponent<ActionTask>();
            _highlight = _host.AddComponent<CountingSubtask>();
            SetPrivate(_task, "_checkPoints", new CheckPointsParent());
            SetPrivate(_task, "_subTasks", new GameObjectTask[] { _highlight });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void Starting_unguided_runs_no_subtask_so_nothing_is_highlighted()
        {
            _task.StartTaskUnguided();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(0));
        }

        [Test]
        public void An_unguided_task_still_completes_when_its_check_points_are_all_checked()
        {
            _task.StartTaskUnguided();

            _task.CheckPointsParent.OnAllChecked.Invoke();

            Assert.That(_task.IsComplete, Is.True);
        }

        [Test]
        public void Showing_the_hint_runs_the_subtasks()
        {
            _task.StartTaskUnguided();

            _task.ShowHint();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Showing_the_hint_twice_runs_the_subtasks_only_once()
        {
            _task.StartTaskUnguided();

            _task.ShowHint();
            _task.ShowHint();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Hiding_the_hint_cancels_the_subtasks_and_lets_the_hint_be_shown_again()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();

            _task.HideHint();
            _task.ShowHint();

            Assert.That(_highlight.CancelCount, Is.EqualTo(1));
            Assert.That(_highlight.ExecuteCount, Is.EqualTo(2));
        }

        [Test]
        public void Hiding_a_hint_that_is_not_shown_does_nothing()
        {
            _task.StartTaskUnguided();

            _task.HideHint();

            Assert.That(_highlight.CancelCount, Is.EqualTo(0));
        }

        [Test]
        public void Completing_the_task_while_the_hint_is_shown_takes_the_hint_down()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();

            _task.CheckPointsParent.OnAllChecked.Invoke();

            Assert.That(_highlight.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void Showing_a_hint_on_a_completed_task_does_nothing()
        {
            _task.StartTaskUnguided();
            _task.CheckPointsParent.OnAllChecked.Invoke();

            _task.ShowHint();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(0));
        }

        [Test]
        public void Guided_start_still_runs_the_subtasks_without_asking_for_a_hint()
        {
            _task.StartTask();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(1));
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class CountingSubtask : GameObjectTask
        {
            public int ExecuteCount { get; private set; }
            public int CancelCount { get; private set; }

            public override void Execute() => ExecuteCount++;

            public override void Cancel() => CancelCount++;
        }
    }
}
