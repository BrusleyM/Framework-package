using Framework.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class SubtaskStackSafetyTests
    {
        private const int VeryLongChain = 4000;

        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void A_very_long_chain_of_instant_subtasks_completes_without_growing_the_stack()
        {
            var subtasks = new GameObjectTask[VeryLongChain];
            for (var index = 0; index < subtasks.Length; index++)
            {
                subtasks[index] = _host.AddComponent<InstantSubtask>();
            }
            var action = ActionTaskWith(subtasks);

            Assert.DoesNotThrow(() => action.StartTask());

            foreach (var subtask in subtasks)
            {
                Assert.That(subtask.IsComplete, Is.True);
            }
        }

        [Test]
        public void The_call_stack_does_not_grow_along_a_chain_of_instant_subtasks()
        {
            var subtasks = new InstantSubtask[400];
            for (var index = 0; index < subtasks.Length; index++)
            {
                subtasks[index] = _host.AddComponent<InstantSubtask>();
            }
            var action = ActionTaskWith(subtasks);

            action.StartTask();

            Assert.That(subtasks[subtasks.Length - 1].FrameCountWhenExecuted - subtasks[0].FrameCountWhenExecuted, Is.LessThan(10));
        }

        [Test]
        public void Empty_slots_between_instant_subtasks_are_skipped_and_the_rest_run_in_the_listed_order()
        {
            var order = new System.Collections.Generic.List<string>();
            var first = _host.AddComponent<InstantSubtask>();
            var second = _host.AddComponent<InstantSubtask>();
            first.OnExecute = () => order.Add("first");
            second.OnExecute = () => order.Add("second");
            var action = ActionTaskWith(first, null, null, second);

            action.StartTask();

            CollectionAssert.AreEqual(new[] { "first", "second" }, order);
        }

        [Test]
        public void Cancelling_the_action_task_while_an_instant_subtask_is_finishing_does_not_start_the_next_one()
        {
            var action = _host.AddComponent<ActionTask>();
            var cancelling = _host.AddComponent<InstantSubtask>();
            var next = _host.AddComponent<InstantSubtask>();
            var nextRan = false;
            cancelling.AfterCompleting = action.CancelTask;
            next.OnExecute = () => nextRan = true;
            AssignSubtasks(action, cancelling, next);

            action.StartTask();

            Assert.That(nextRan, Is.False);
        }

        [Test]
        public void An_action_task_whose_subtask_throws_can_be_started_again()
        {
            var thrower = _host.AddComponent<ThrowingSubtask>();
            var action = ActionTaskWith(thrower);
            Assert.Throws<System.InvalidOperationException>(() => action.StartTask());

            Assert.Throws<System.InvalidOperationException>(() => action.StartTask());
        }

        [Test]
        public void A_group_may_be_executed_again_from_a_completed_listener_without_a_false_cycle_error()
        {
            var group = _host.AddComponent<ParallelSubtask>();
            var child = _host.AddComponent<InstantSubtask>();
            SetSubtasksWithoutValidation(group, child);
            var executions = 0;
            child.OnExecute = () => executions++;
            var restarted = false;
            group.Completed += () =>
            {
                if (!restarted)
                {
                    restarted = true;
                    group.Prepare();
                    group.Execute();
                }
            };

            group.Execute();

            Assert.That(executions, Is.EqualTo(2));
            Assert.That(group.IsComplete, Is.True);
        }

        [Test]
        public void A_subtask_that_completes_later_still_starts_the_next_one()
        {
            var slow = _host.AddComponent<ManualSubtask>();
            var after = _host.AddComponent<InstantSubtask>();
            var action = ActionTaskWith(slow, after);
            action.StartTask();
            Assert.That(after.IsComplete, Is.False);

            slow.Finish();

            Assert.That(after.IsComplete, Is.True);
        }

        [Test]
        public void A_group_that_contains_itself_through_another_group_refuses_to_run_and_logs_an_error()
        {
            var outer = _host.AddComponent<ParallelSubtask>();
            var inner = _host.AddComponent<ParallelSubtask>();
            SetSubtasksWithoutValidation(outer, inner);
            SetSubtasksWithoutValidation(inner, outer);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("already executing"));

            Assert.DoesNotThrow(() => outer.Execute());
        }

        [Test]
        public void Cancelling_a_group_that_contains_itself_through_another_group_does_not_recurse()
        {
            var outer = _host.AddComponent<ParallelSubtask>();
            var inner = _host.AddComponent<ParallelSubtask>();
            SetSubtasksWithoutValidation(outer, inner);
            SetSubtasksWithoutValidation(inner, outer);

            Assert.DoesNotThrow(() => outer.Cancel());
        }

        [Test]
        public void Restarting_the_action_task_from_inside_a_finishing_subtask_runs_the_list_again_without_growing_the_stack()
        {
            var action = _host.AddComponent<ActionTask>();
            var first = _host.AddComponent<InstantSubtask>();
            var second = _host.AddComponent<InstantSubtask>();
            var firstRuns = 0;
            var secondRuns = 0;
            var restarted = false;
            first.OnExecute = () => firstRuns++;
            second.OnExecute = () => secondRuns++;
            first.AfterCompleting = () =>
            {
                if (!restarted)
                {
                    restarted = true;
                    action.StartTask();
                }
            };
            AssignSubtasks(action, first, second);

            action.StartTask();

            Assert.That(firstRuns, Is.EqualTo(2));
            Assert.That(secondRuns, Is.EqualTo(1));
        }

        [Test]
        public void A_group_nested_in_a_group_completes_the_outer_group_exactly_once()
        {
            var outer = _host.AddComponent<ParallelSubtask>();
            var inner = _host.AddComponent<ParallelSubtask>();
            var instant = _host.AddComponent<InstantSubtask>();
            var other = _host.AddComponent<InstantSubtask>();
            SetSubtasksWithoutValidation(inner, instant);
            SetSubtasksWithoutValidation(outer, inner, other);
            var completions = 0;
            outer.Completed += () => completions++;

            outer.Execute();

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(inner.IsComplete, Is.True);
        }

        [Test]
        public void A_group_with_no_subtasks_completes_immediately()
        {
            var group = _host.AddComponent<ParallelSubtask>();

            group.Execute();

            Assert.That(group.IsComplete, Is.True);
        }

        [Test]
        public void A_group_with_a_subtask_that_finishes_later_completes_when_it_does()
        {
            var group = _host.AddComponent<ParallelSubtask>();
            var slow = _host.AddComponent<ManualSubtask>();
            SetSubtasksWithoutValidation(group, slow);
            group.Execute();
            Assert.That(group.IsComplete, Is.False);

            slow.Finish();

            Assert.That(group.IsComplete, Is.True);
        }

        [Test]
        public void A_subtask_that_throws_after_completing_still_lets_its_group_report_completion()
        {
            var group = _host.AddComponent<ParallelSubtask>();
            var child = _host.AddComponent<InstantSubtask>();
            child.AfterCompleting = () => throw new System.InvalidOperationException("late failure");
            SetSubtasksWithoutValidation(group, child);
            var completions = 0;
            group.Completed += () => completions++;

            Assert.Throws<System.InvalidOperationException>(() => group.Execute());

            Assert.That(completions, Is.EqualTo(1));
        }

        private ActionTask ActionTaskWith(params GameObjectTask[] subtasks)
        {
            var action = _host.AddComponent<ActionTask>();
            AssignSubtasks(action, subtasks);
            return action;
        }

        private static void AssignSubtasks(ActionTask action, params GameObjectTask[] subtasks)
        {
            var serialized = new SerializedObject(action);
            var property = serialized.FindProperty("_subTasks");
            property.arraySize = subtasks.Length;
            for (var index = 0; index < subtasks.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = subtasks[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSubtasksWithoutValidation(ParallelSubtask group, params GameObjectTask[] subtasks)
        {
            var field = typeof(ParallelSubtask).GetField("_subtasks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(group, subtasks);
        }

        private sealed class InstantSubtask : GameObjectTask
        {
            public int FrameCountWhenExecuted { get; private set; }
            public System.Action OnExecute { get; set; }
            public System.Action AfterCompleting { get; set; }

            public override void Execute()
            {
                FrameCountWhenExecuted = new System.Diagnostics.StackTrace().FrameCount;
                OnExecute?.Invoke();
                MarkComplete();
                AfterCompleting?.Invoke();
            }
        }

        private sealed class ThrowingSubtask : GameObjectTask
        {
            public override void Execute()
            {
                throw new System.InvalidOperationException("missing reference");
            }
        }

        private sealed class ManualSubtask : GameObjectTask
        {
            public override void Execute()
            {
            }

            public void Finish()
            {
                MarkComplete();
            }
        }
    }
}
