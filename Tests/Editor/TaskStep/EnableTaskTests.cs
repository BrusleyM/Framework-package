using Framework.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    public class EnableTaskTests
    {
        private GameObject _host;
        private GameObject _target;
        private EnableTask _enableTask;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("host");
            _target = new GameObject("target");
            _target.SetActive(false);
            _enableTask = _host.AddComponent<EnableTask>();
            Assign(_enableTask, "gameObjectToEnable", _target);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void Executing_enables_the_game_object_and_completes_the_task()
        {
            var completions = 0;
            _enableTask.Completed += () => completions++;

            _enableTask.Execute();

            Assert.IsTrue(_target.activeSelf);
            Assert.IsTrue(_enableTask.IsComplete);
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void Disabling_after_executing_does_not_complete_the_task_a_second_time()
        {
            var completions = 0;
            _enableTask.Completed += () => completions++;
            _enableTask.Execute();

            _enableTask.DisableGameObject();

            Assert.IsFalse(_target.activeSelf);
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void Sub_tasks_listed_after_the_enable_task_run_in_the_order_they_are_listed()
        {
            var journal = new System.Collections.Generic.List<string>();
            var action = ActionTaskRunning(_enableTask, ProbeSubTask("first", journal), ProbeSubTask("second", journal));

            action.StartTask();

            Assert.IsTrue(_target.activeSelf);
            Assert.AreEqual(new[] { "first", "second" }, journal);
        }

        [Test]
        public void An_enable_task_without_a_game_object_refuses_to_execute_and_stays_incomplete()
        {
            Assign(_enableTask, "gameObjectToEnable", null);

            Assert.Throws<System.InvalidOperationException>(() => _enableTask.Execute());
            Assert.IsFalse(_enableTask.IsComplete);
        }

        [Test]
        public void Cancelling_the_action_task_after_the_enable_task_completed_disables_the_game_object_again()
        {
            var action = ActionTaskRunning(_enableTask);
            action.StartTask();
            Assert.IsTrue(_target.activeSelf);

            action.CancelTask();

            Assert.IsFalse(_target.activeSelf);
        }

        [Test]
        public void Skipping_the_action_task_after_the_enable_task_completed_disables_the_game_object_again()
        {
            var action = ActionTaskRunning(_enableTask);
            action.StartTask();

            action.Skip();

            Assert.IsFalse(_target.activeSelf);
        }

        [Test]
        public void Cancelling_an_executed_enable_task_switches_its_game_object_off_without_completing_it()
        {
            _enableTask.Execute();
            _enableTask.Prepare();

            _enableTask.Cancel();

            Assert.IsFalse(_target.activeSelf);
            Assert.IsFalse(_enableTask.IsComplete);
        }

        [Test]
        public void Cancelling_the_action_task_switches_off_an_enable_task_nested_in_a_parallel_group()
        {
            var group = ParallelGroupOf(_enableTask);
            var action = ActionTaskRunning(group);
            action.StartTask();
            Assert.IsTrue(_target.activeSelf);

            action.CancelTask();

            Assert.IsFalse(_target.activeSelf);
        }

        [Test]
        public void Skipping_the_action_task_switches_off_an_enable_task_nested_in_a_parallel_group()
        {
            var group = ParallelGroupOf(_enableTask);
            var action = ActionTaskRunning(group);
            action.StartTask();

            action.Skip();

            Assert.IsFalse(_target.activeSelf);
        }

        private ParallelSubtask ParallelGroupOf(params GameObjectTask[] subtasks)
        {
            var group = _host.AddComponent<ParallelSubtask>();
            var serialized = new SerializedObject(group);
            var property = serialized.FindProperty("_subtasks");
            property.arraySize = subtasks.Length;
            for (var index = 0; index < subtasks.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = subtasks[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return group;
        }

        private ActionTask ActionTaskRunning(params GameObjectTask[] subTasks)
        {
            var action = _host.AddComponent<ActionTask>();
            var serialized = new SerializedObject(action);
            var property = serialized.FindProperty("_subTasks");
            property.arraySize = subTasks.Length;
            for (var index = 0; index < subTasks.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = subTasks[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return action;
        }

        private GameObjectTask ProbeSubTask(string label, System.Collections.Generic.List<string> journal)
        {
            var probe = _host.AddComponent<RecordingSubTask>();
            probe.Label = label;
            probe.Journal = journal;
            return probe;
        }

        [Test]
        public void Executing_with_the_off_state_disables_the_game_object_and_completes_the_task()
        {
            _target.SetActive(true);
            AssignState(SubtaskState.Off);

            _enableTask.Execute();

            Assert.IsFalse(_target.activeSelf);
            Assert.IsTrue(_enableTask.IsComplete);
        }

        [Test]
        public void Executing_with_the_toggle_state_flips_the_game_object_each_time()
        {
            AssignState(SubtaskState.Toggle);

            _enableTask.Execute();
            var activeAfterFirst = _target.activeSelf;
            _enableTask.Prepare();
            _enableTask.Execute();

            Assert.IsTrue(activeAfterFirst);
            Assert.IsFalse(_target.activeSelf);
        }

        [Test]
        public void A_new_enable_task_defaults_to_the_on_state()
        {
            _enableTask.Execute();

            Assert.IsTrue(_target.activeSelf);
        }

        private void AssignState(SubtaskState state)
        {
            var serialized = new SerializedObject(_enableTask);
            serialized.FindProperty("_state").enumValueIndex = (int)state;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class RecordingSubTask : GameObjectTask
        {
            public string Label { get; set; }
            public System.Collections.Generic.List<string> Journal { get; set; }

            public override void Execute()
            {
                Journal.Add(Label);
                MarkComplete();
            }
        }
    }
}
