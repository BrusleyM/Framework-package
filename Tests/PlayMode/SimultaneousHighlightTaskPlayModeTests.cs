using System.Collections;
using Framework.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class SimultaneousHighlightTaskPlayModeTests
    {
        private GameObject _host;
        private GameObject _box;
        private Outline _outline;
        private SimultaneousHighlightTask _task;

        [SetUp]
        public void SetUp()
        {
            _box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _outline = _box.AddComponent<Outline>();
            _outline.OutlineActive = new UnityEngine.Events.UnityEvent();
            _host = new GameObject("host");
            _task = _host.AddComponent<SimultaneousHighlightTask>();
            AssignBox();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
            Object.Destroy(_box);
        }

        [UnityTest]
        public IEnumerator Executing_with_the_default_on_state_highlights_the_object_and_completes()
        {
            yield return null;

            _task.Execute();

            Assert.IsTrue(_outline.IsOutLined);
            Assert.IsTrue(_task.IsComplete);
        }

        [UnityTest]
        public IEnumerator Executing_with_the_off_state_removes_the_highlight_and_completes()
        {
            yield return null;
            _outline.EnableOutline();
            AssignState(SubtaskState.Off);

            _task.Execute();

            Assert.IsFalse(_outline.IsOutLined);
            Assert.IsTrue(_task.IsComplete);
        }

        [UnityTest]
        public IEnumerator Executing_with_the_toggle_state_flips_the_highlight_each_time()
        {
            yield return null;
            AssignState(SubtaskState.Toggle);

            _task.Execute();
            var highlightedAfterFirst = _outline.IsOutLined;
            _task.Prepare();
            _task.Execute();

            Assert.IsTrue(highlightedAfterFirst);
            Assert.IsFalse(_outline.IsOutLined);
        }

        private void AssignBox()
        {
            var field = typeof(HighlightTask).GetField("gameObjectsToHighlight", Fields);
            field.SetValue(_task, new[] { _box });
        }

        private void AssignState(SubtaskState state)
        {
            var field = typeof(SimultaneousHighlightTask).GetField("_state", Fields);
            field.SetValue(_task, state);
        }

        private const System.Reflection.BindingFlags Fields =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    }
}
