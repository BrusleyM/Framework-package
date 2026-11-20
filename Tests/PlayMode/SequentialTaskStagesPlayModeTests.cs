using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework.Sequencing.Tests
{
    public class SequentialTaskStagesPlayModeTests
    {
        private GameObject _host;
        private SequentialTask _sequence;
        private RecordingTask _introHello;
        private RecordingTask _introGrab;
        private RecordingTask _handsPoint;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("sequence host");
            _sequence = _host.AddComponent<SequentialTask>();
            _introHello = NewTask("hello");
            _introGrab = NewTask("grab");
            _handsPoint = NewTask("point");
            AddStage("Intro", _introHello, _introGrab);
            AddStage("Hands", _handsPoint);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
        }

        [UnityTest]
        public IEnumerator Tasks_run_inside_their_stage_and_the_next_stage_starts_when_a_stage_is_done()
        {
            _sequence.Run();
            yield return null;
            Assert.AreEqual("Intro", _sequence.CurrentStageName);
            Assert.AreSame(_introHello, _sequence.CurrentTask);

            _introHello.Finish();
            Assert.AreSame(_introGrab, _sequence.CurrentTask);

            _introGrab.Finish();
            Assert.AreEqual("Hands", _sequence.CurrentStageName);
            Assert.AreSame(_handsPoint, _sequence.CurrentTask);

            _handsPoint.Finish();
            Assert.AreEqual(SequenceState.Completed, _sequence.Runner.State);
        }

        [UnityTest]
        public IEnumerator Skipping_a_task_moves_within_its_stage_and_skipping_the_stage_moves_to_the_next_one()
        {
            _sequence.Run();
            yield return null;

            _sequence.SkipCurrentTask();
            Assert.AreSame(_introGrab, _sequence.CurrentTask);
            Assert.AreEqual("Intro", _sequence.CurrentStageName);

            _sequence.SkipCurrentStage();
            Assert.AreEqual("Hands", _sequence.CurrentStageName);
            Assert.AreEqual(1, _introGrab.CancelCount);
        }

        [UnityTest]
        public IEnumerator Restarting_the_stage_begins_it_again_from_its_first_task()
        {
            _sequence.Run();
            yield return null;
            _introHello.Finish();
            Assert.AreSame(_introGrab, _sequence.CurrentTask);

            _sequence.RestartCurrentStage();

            Assert.AreEqual("Intro", _sequence.CurrentStageName);
            Assert.AreSame(_introHello, _sequence.CurrentTask);
            Assert.AreEqual(2, _introHello.StartCount);
        }

        [UnityTest]
        public IEnumerator Going_back_a_stage_returns_to_the_first_task_of_the_previous_stage()
        {
            _sequence.Run();
            yield return null;
            _sequence.SkipCurrentStage();
            Assert.AreEqual("Hands", _sequence.CurrentStageName);

            _sequence.GoBackStage();

            Assert.AreEqual("Intro", _sequence.CurrentStageName);
            Assert.AreSame(_introHello, _sequence.CurrentTask);
        }

        [UnityTest]
        public IEnumerator Cancelling_cleans_up_the_task_running_inside_the_stage()
        {
            _sequence.Run();
            yield return null;

            _sequence.Cancel();

            Assert.AreEqual(1, _introHello.CancelCount);
            Assert.AreEqual(SequenceState.Cancelled, _sequence.Runner.State);
        }

        private RecordingTask NewTask(string label)
        {
            var task = _host.AddComponent<RecordingTask>();
            task.Label = label;
            return task;
        }

        private void AddStage(string stageName, params RecordingTask[] tasks)
        {
            var stageHost = new GameObject(stageName);
            stageHost.transform.SetParent(_host.transform);
            var stage = stageHost.AddComponent<TaskStage>();
            foreach (var task in tasks)
            {
                stage.AddTask(task);
            }

            _sequence.AddStage(stage);
        }
    }
}
