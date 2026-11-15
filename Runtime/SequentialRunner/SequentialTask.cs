using System.Collections;
using System.Collections.Generic;
using Framework.Audio;
using Framework.Tasks;
using UnityEngine;

namespace Framework.Sequencing
{
    public class SequentialTask : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;
        [SerializeField]
        private List<Task> _taskList = new List<Task>();
        [SerializeField]
        private List<TaskStage> _stages = new List<TaskStage>();

        private SequenceRunner _runner;
        private Coroutine _clock;
        private AudioPlayer _audioPlayer;

        public List<Task> TaskList => _taskList;
        public SequenceRunner Runner => _runner;
        public Task CurrentTask => (ActiveTaskRunner?.CurrentStep as TaskStep)?.Task;
        public string CurrentStageName => (_runner?.CurrentStep as StageStep)?.Name;
        public int CurrentStageIndex => _runner != null ? _runner.CurrentIndex : -1;
        public bool HasRun => _runner != null && _runner.State != SequenceState.Idle;
        public bool IsRunning => _runner != null && _runner.IsInProgress;

        private SequenceRunner ActiveTaskRunner => (_runner?.CurrentStep as StageStep)?.Stage ?? _runner;

        public void AddTask(Task task)
        {
            _taskList.Add(task);
        }

        public void AddStage(TaskStage stage)
        {
            _stages.Add(stage);
        }

        public void Run()
        {
            if (IsRunning)
            {
                return;
            }

            Play(BuildRunnerFromTaskList());
        }

        public void Play(SequenceRunner runner)
        {
            if (!isActiveAndEnabled)
            {
                throw new System.InvalidOperationException($"{name} must be active and enabled to run a sequence.");
            }

            Cancel();

            _runner = runner;
            _runner.Faulted += LogFault;
            EnsureClockIsRunning();
            _runner.Start();
        }

        public void Pause() => _runner?.Pause();

        public void Resume() => _runner?.Resume();

        public void SkipCurrentTask() => ActiveTaskRunner?.SkipCurrentStep();

        public void GoBack() => ActiveTaskRunner?.GoBack();

        public void SkipCurrentStage() => _runner?.SkipCurrentStep();

        public void GoBackStage() => _runner?.GoBack();

        public void RestartCurrentStage() => _runner?.RestartCurrentStep();

        public void Restart()
        {
            if (_runner == null)
            {
                return;
            }

            _runner.Faulted -= LogFault;
            _runner.Faulted += LogFault;
            _runner.Restart();
            EnsureClockIsRunning();
        }

        public void Cancel()
        {
            StopClock();

            if (_runner == null)
            {
                return;
            }

            _runner.Faulted -= LogFault;
            _runner.Cancel();
        }

        public void StopExecution() => Cancel();

        private void OnDisable()
        {
            Cancel();
        }

        private SequenceRunner BuildRunnerFromTaskList()
        {
            var audioPlayer = ObtainAudioPlayer();

            if (_stages.Count == 0)
            {
                return new SequenceRunner(name, StepsFor(_taskList, audioPlayer));
            }

            var stageSteps = new List<ISequenceStep>();
            foreach (var stage in _stages)
            {
                if (stage != null)
                {
                    var stageRunner = new SequenceRunner(stage.StageName, StepsFor(stage.Tasks, audioPlayer));
                    stageSteps.Add(new StageStep(stage.StageName, stageRunner));
                }
            }

            return new SequenceRunner(name, stageSteps);
        }

        private static List<ISequenceStep> StepsFor(IEnumerable<Task> tasks, AudioPlayer audioPlayer)
        {
            var steps = new List<ISequenceStep>();
            foreach (var task in tasks)
            {
                if (task != null)
                {
                    steps.Add(new TaskStep(task, audioPlayer));
                }
            }

            return steps;
        }

        private AudioPlayer ObtainAudioPlayer()
        {
            if (_audioPlayer == null && _audioSource != null)
            {
                _audioPlayer = new AudioPlayer(_audioSource);
            }

            return _audioPlayer;
        }

        private void EnsureClockIsRunning()
        {
            if (!isActiveAndEnabled)
            {
                throw new System.InvalidOperationException($"{name} must be active and enabled to run a sequence.");
            }

            if (_clock == null)
            {
                _clock = StartCoroutine(TickWhileInProgress());
            }
        }

        private void StopClock()
        {
            if (_clock != null)
            {
                StopCoroutine(_clock);
                _clock = null;
            }
        }

        private IEnumerator TickWhileInProgress()
        {
            while (_runner != null && (_runner.State == SequenceState.Idle || _runner.IsInProgress))
            {
                try
                {
                    _runner.Advance(Time.deltaTime);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception, this);
                }

                yield return null;
            }

            _clock = null;
        }

        private static void LogFault(System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
