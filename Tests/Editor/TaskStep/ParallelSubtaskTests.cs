using System.Collections.Generic;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class ParallelSubtaskTests
    {
        private GameObject _host;
        private ParallelSubtask _parallel;
        private ManualSubtask _first;
        private ManualSubtask _second;
        private int _completions;
        private readonly List<GameObject> _tracked = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _parallel = _host.AddComponent<ParallelSubtask>();
            _first = _host.AddComponent<ManualSubtask>();
            _second = _host.AddComponent<ManualSubtask>();
            AssignSubtasks(_parallel, _first, _second);
            _completions = 0;
            _parallel.Completed += () => _completions++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            foreach (var tracked in _tracked)
            {
                Object.DestroyImmediate(tracked);
            }
            _tracked.Clear();
        }

        private GameObject Track(GameObject gameObject)
        {
            _tracked.Add(gameObject);
            return gameObject;
        }

        [Test]
        public void Executing_starts_every_subtask_before_any_of_them_has_finished()
        {
            _parallel.Execute();

            Assert.AreEqual(1, _first.ExecuteCount);
            Assert.AreEqual(1, _second.ExecuteCount);
        }

        [Test]
        public void The_group_stays_incomplete_while_one_subtask_is_still_running()
        {
            _parallel.Execute();

            _first.Finish();

            Assert.IsFalse(_parallel.IsComplete);
        }

        [Test]
        public void The_group_completes_once_when_the_last_subtask_finishes()
        {
            _parallel.Execute();
            _first.Finish();

            _second.Finish();

            Assert.IsTrue(_parallel.IsComplete);
            Assert.AreEqual(1, _completions);
        }

        [Test]
        public void Subtasks_that_finish_themselves_instantly_complete_the_group_immediately()
        {
            var firstTarget = Track(new GameObject("firstTarget"));
            var secondTarget = Track(new GameObject("secondTarget"));
            firstTarget.SetActive(false);
            secondTarget.SetActive(false);
            var firstEnable = _host.AddComponent<EnableTask>();
            var secondEnable = _host.AddComponent<EnableTask>();
            AssignTarget(firstEnable, firstTarget);
            AssignTarget(secondEnable, secondTarget);
            AssignSubtasks(_parallel, firstEnable, secondEnable);

            _parallel.Execute();

            Assert.IsTrue(firstTarget.activeSelf);
            Assert.IsTrue(secondTarget.activeSelf);
            Assert.IsTrue(_parallel.IsComplete);
        }

        [Test]
        public void An_empty_group_completes_immediately()
        {
            AssignSubtasks(_parallel);

            _parallel.Execute();

            Assert.IsTrue(_parallel.IsComplete);
        }

        [Test]
        public void Empty_slots_in_the_list_are_ignored()
        {
            AssignSubtasks(_parallel, null, _first, null);
            _parallel.Execute();

            _first.Finish();

            Assert.IsTrue(_parallel.IsComplete);
        }

        [Test]
        public void Cancelling_cancels_every_subtask_without_completing_the_group()
        {
            _parallel.Execute();

            _parallel.Cancel();

            Assert.AreEqual(1, _first.CancelCount);
            Assert.AreEqual(1, _second.CancelCount);
            Assert.IsFalse(_parallel.IsComplete);
        }

        [Test]
        public void A_subtask_finishing_after_the_group_was_cancelled_does_not_complete_it()
        {
            _parallel.Execute();
            _parallel.Cancel();

            _first.Finish();
            _second.Finish();

            Assert.IsFalse(_parallel.IsComplete);
            Assert.AreEqual(0, _completions);
        }

        [Test]
        public void Executing_again_after_completing_runs_every_subtask_once_more()
        {
            _parallel.Execute();
            _first.Finish();
            _second.Finish();

            _parallel.Prepare();
            _parallel.Execute();

            Assert.AreEqual(2, _first.ExecuteCount);
            Assert.AreEqual(2, _second.ExecuteCount);
            Assert.IsFalse(_parallel.IsComplete);
        }

        [Test]
        public void A_group_inside_a_group_completes_when_the_inner_group_completes()
        {
            var inner = _host.AddComponent<ParallelSubtask>();
            AssignSubtasks(_parallel, inner, _second);
            AssignSubtasks(inner, _first);
            _parallel.Execute();

            _first.Finish();
            _second.Finish();

            Assert.IsTrue(_parallel.IsComplete);
        }

        [Test]
        public void Subtasks_are_started_in_the_order_they_are_listed()
        {
            var journal = new List<string>();
            _first.OnExecute = () => journal.Add("first");
            _second.OnExecute = () => journal.Add("second");

            _parallel.Execute();

            CollectionAssert.AreEqual(new[] { "first", "second" }, journal);
        }

        [Test]
        public void A_subtask_that_throws_cancels_the_ones_already_started_and_stops_the_group()
        {
            var thrower = _host.AddComponent<ThrowingSubtask>();
            AssignSubtasks(_parallel, _first, thrower, _second);

            Assert.Throws<System.InvalidOperationException>(() => _parallel.Execute());

            Assert.AreEqual(1, _first.CancelCount);
            Assert.AreEqual(0, _second.ExecuteCount);
            _first.Finish();
            Assert.IsFalse(_parallel.IsComplete);
        }

        [Test]
        public void A_subtask_listed_twice_is_started_once()
        {
            AssignSubtasks(_parallel, _first, _first, _second);

            _parallel.Execute();
            _first.Finish();
            _second.Finish();

            Assert.AreEqual(1, _first.ExecuteCount);
            Assert.IsTrue(_parallel.IsComplete);
        }

        [Test]
        public void A_group_whose_slots_are_all_empty_completes_immediately()
        {
            AssignSubtasks(_parallel, null, null);

            _parallel.Execute();

            Assert.IsTrue(_parallel.IsComplete);
        }

        [Test]
        public void Executing_again_after_a_mid_run_cancel_waits_for_the_new_run_to_finish()
        {
            _parallel.Execute();
            _parallel.Cancel();

            _parallel.Prepare();
            _parallel.Execute();
            _first.Finish();

            Assert.IsFalse(_parallel.IsComplete);
            _second.Finish();
            Assert.IsTrue(_parallel.IsComplete);
            Assert.AreEqual(1, _completions);
        }

        [Test]
        public void A_group_cannot_be_given_itself_as_a_subtask()
        {
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("cannot contain itself"));

            AssignSubtasks(_parallel, _parallel, _first);

            Assert.IsFalse(_parallel.IsComplete);
            Assert.AreEqual(1, SubtaskCountOf(_parallel));
        }

        [Test]
        public void A_group_cannot_contain_a_group_that_contains_it()
        {
            var inner = _host.AddComponent<ParallelSubtask>();
            AssignSubtasks(inner, _parallel);
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("cannot contain itself"));

            AssignSubtasks(_parallel, inner);

            Assert.AreEqual(0, SubtaskCountOf(_parallel));
        }

        [Test]
        public void A_throw_after_the_group_already_completed_does_not_cancel_its_children()
        {
            var thrower = _host.AddComponent<ThrowingSubtask>();
            AssignSubtasks(_parallel, _first);
            _parallel.Completed += () => thrower.Execute();
            _first.OnExecute = () => _first.Finish();

            Assert.Throws<System.InvalidOperationException>(() => _parallel.Execute());

            Assert.AreEqual(0, _first.CancelCount);
            Assert.IsTrue(_parallel.IsComplete);
        }

        private static int SubtaskCountOf(ParallelSubtask group)
        {
            var serialized = new SerializedObject(group);
            var count = 0;
            var list = serialized.FindProperty("_subtasks");
            for (var index = 0; index < list.arraySize; index++)
            {
                if (list.GetArrayElementAtIndex(index).objectReferenceValue != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssignSubtasks(ParallelSubtask group, params GameObjectTask[] subtasks)
        {
            var serialized = new SerializedObject(group);
            var list = serialized.FindProperty("_subtasks");
            list.arraySize = subtasks.Length;
            for (var index = 0; index < subtasks.Length; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = subtasks[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTarget(EnableTask enable, GameObject target)
        {
            var serialized = new SerializedObject(enable);
            serialized.FindProperty("gameObjectToEnable").objectReferenceValue = target;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            public int ExecuteCount { get; private set; }
            public int CancelCount { get; private set; }
            public System.Action OnExecute { get; set; }

            public override void Execute()
            {
                ExecuteCount++;
                OnExecute?.Invoke();
            }

            public override void Cancel()
            {
                CancelCount++;
            }

            public void Finish()
            {
                MarkComplete();
            }
        }
    }
}
