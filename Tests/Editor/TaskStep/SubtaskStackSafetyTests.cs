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
        public void The_call_stack_stays_shallow_however_many_instant_subtasks_are_chained()
        {
            InstantSubtask.DeepestStack = 0;
            var subtasks = new GameObjectTask[400];
            for (var index = 0; index < subtasks.Length; index++)
            {
                subtasks[index] = _host.AddComponent<InstantSubtask>();
            }
            var action = ActionTaskWith(subtasks);

            action.StartTask();

            Assert.That(InstantSubtask.DeepestStack, Is.LessThan(60));
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
            public static int DeepestStack { get; set; }
            public System.Action OnExecute { get; set; }
            public System.Action AfterCompleting { get; set; }

            public override void Execute()
            {
                DeepestStack = System.Math.Max(DeepestStack, new System.Diagnostics.StackTrace().FrameCount);
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
