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
        public void Empty_slots_between_instant_subtasks_are_skipped_in_order()
        {
            var first = _host.AddComponent<InstantSubtask>();
            var second = _host.AddComponent<InstantSubtask>();
            var action = ActionTaskWith(first, null, null, second);

            action.StartTask();

            Assert.That(first.IsComplete, Is.True);
            Assert.That(second.IsComplete, Is.True);
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
            var serialized = new SerializedObject(action);
            var property = serialized.FindProperty("_subTasks");
            property.arraySize = subtasks.Length;
            for (var index = 0; index < subtasks.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = subtasks[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return action;
        }

        private static void SetSubtasksWithoutValidation(ParallelSubtask group, params GameObjectTask[] subtasks)
        {
            var field = typeof(ParallelSubtask).GetField("_subtasks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(group, subtasks);
        }

        private sealed class InstantSubtask : GameObjectTask
        {
            public override void Execute()
            {
                MarkComplete();
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
