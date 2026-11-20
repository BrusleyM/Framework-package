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
        public void Disabling_the_task_takes_the_hint_down_so_it_can_be_shown_again_later()
        {
            _task.StartTaskUnguided();
            _task.ShowHint();

            PrivateMembers.Invoke(_task, "OnDisable");
            _task.ShowHint();

            Assert.That(_highlight.CancelCount, Is.EqualTo(1));
            Assert.That(_highlight.ExecuteCount, Is.EqualTo(2));
        }

        [Test]
        public void Disabling_an_action_task_that_never_started_and_has_no_check_points_does_not_throw()
        {
            var bare = _host.AddComponent<ActionTask>();

            Assert.DoesNotThrow(() => PrivateMembers.Invoke(bare, "OnDisable"));
        }

        [Test]
        public void Starting_an_action_task_without_check_points_says_so()
        {
            var bare = _host.AddComponent<ActionTask>();

            Assert.Throws<System.InvalidOperationException>(() => bare.StartTask());
        }

        [Test]
        public void Guided_start_still_runs_the_subtasks_without_asking_for_a_hint()
        {
            _task.StartTask();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Starting_unguided_runs_the_setup_subtasks_and_leaves_the_guidance_ones()
        {
            var setup = AddSetupSubtask();

            _task.StartTaskUnguided();

            Assert.That(setup.ExecuteCount, Is.EqualTo(1));
            Assert.That(_highlight.ExecuteCount, Is.EqualTo(0));
        }

        [Test]
        public void Showing_the_hint_runs_only_the_guidance_subtasks_and_not_the_setup_again()
        {
            var setup = AddSetupSubtask();
            _task.StartTaskUnguided();

            _task.ShowHint();

            Assert.That(setup.ExecuteCount, Is.EqualTo(1));
            Assert.That(_highlight.ExecuteCount, Is.EqualTo(1));
        }

        [Test]
        public void Hiding_the_hint_cancels_the_guidance_subtasks_but_keeps_the_setup_in_place()
        {
            var setup = AddSetupSubtask();
            _task.StartTaskUnguided();
            _task.ShowHint();

            _task.HideHint();

            Assert.That(_highlight.CancelCount, Is.EqualTo(1));
            Assert.That(setup.CancelCount, Is.EqualTo(0));
        }

        [Test]
        public void Guided_start_runs_the_setup_and_guidance_subtasks_one_after_another()
        {
            var setup = AddSetupSubtask();
            setup.CompleteWhenExecuted = true;
            _highlight.CompleteWhenExecuted = true;

            _task.StartTask();

            Assert.That(_highlight.ExecuteCount, Is.EqualTo(1));
            Assert.That(setup.ExecuteCount, Is.EqualTo(1));
        }

        private CountingSubtask AddSetupSubtask()
        {
            var setup = _host.AddComponent<CountingSubtask>();
            PrivateMembers.SetField(setup, "_isSetup", true);
            SetPrivate(_task, "_subTasks", new GameObjectTask[] { _highlight, setup });
            return setup;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            PrivateMembers.SetField(target, fieldName, value);
        }

        private sealed class CountingSubtask : GameObjectTask
        {
            public int ExecuteCount { get; private set; }
            public int CancelCount { get; private set; }

            public bool CompleteWhenExecuted { get; set; }

            public override void Execute()
            {
                ExecuteCount++;
                if (CompleteWhenExecuted)
                {
                    MarkComplete();
                }
            }

            public override void Cancel() => CancelCount++;
        }
    }
}
