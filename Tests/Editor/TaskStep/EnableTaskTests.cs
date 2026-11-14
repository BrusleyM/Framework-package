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
        public void A_sub_task_listed_after_the_enable_task_still_runs()
        {
            var action = _host.AddComponent<ActionTask>();
            var laterSubTask = _host.AddComponent<ProbeSubTask>();
            var serialized = new SerializedObject(action);
            var subTasks = serialized.FindProperty("_subTasks");
            subTasks.arraySize = 2;
            subTasks.GetArrayElementAtIndex(0).objectReferenceValue = _enableTask;
            subTasks.GetArrayElementAtIndex(1).objectReferenceValue = laterSubTask;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            action.StartTask();

            Assert.IsTrue(_target.activeSelf);
            Assert.AreEqual(1, laterSubTask.ExecuteCount);
        }

        [Test]
        public void An_enable_task_without_a_game_object_refuses_to_execute()
        {
            Assign(_enableTask, "gameObjectToEnable", null);

            Assert.Throws<System.InvalidOperationException>(() => _enableTask.Execute());
        }

        private static void Assign(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class ProbeSubTask : GameObjectTask
        {
            public int ExecuteCount { get; private set; }

            public override void Execute()
            {
                ExecuteCount++;
                MarkComplete();
            }
        }
    }
}
