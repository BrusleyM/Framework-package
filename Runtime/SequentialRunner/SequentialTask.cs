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
        private GuidanceMode _mode = GuidanceMode.Guided;
        [SerializeField]
        private AudioSource _audioSource;
        [SerializeField]
        private List<Task> _taskList = new List<Task>();
        [SerializeField]
        private List<TaskStage> _stages = new List<TaskStage>();
        [SerializeField]
        private List<Task> _unguidedTaskList = new List<Task>();

        private readonly SequenceRunner[] _runnersByMode = new SequenceRunner[2];
        private GuidanceMode _appliedMode;

        private Coroutine _clock;
        private AudioPlayer _audioPlayer;

        public GuidanceMode Mode => _mode;
        public List<Task> TaskList => _taskList;
        public List<Task> UnguidedTaskList => _unguidedTaskList;
        public event System.Action<GuidanceMode> ModeChanged;
        public SequenceRunner Runner => ActiveRunner;
        public Task CurrentTask => (ActiveTaskRunner?.CurrentStep as TaskStep)?.Task;
        public string CurrentStageName => (ActiveRunner?.CurrentStep as StageStep)?.Name;
        public int CurrentStageIndex => ActiveRunner != null ? ActiveRunner.CurrentIndex : -1;
        public bool HasRun => ActiveRunner != null && ActiveRunner.State != SequenceState.Idle;
        public bool IsRunning => ActiveRunner != null && ActiveRunner.IsInProgress;

        private SequenceRunner ActiveRunner
        {
            get => _runnersByMode[(int)_mode];
            set => _runnersByMode[(int)_mode] = value;
        }

        private SequenceRunner ActiveTaskRunner => (ActiveRunner?.CurrentStep as StageStep)?.Stage ?? ActiveRunner;

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

        public void RunOrResume()
        {
            if (!HasRun)
            {
                Run();
            }
            else
            {
                Resume();
            }
        }

        public void SetMode(GuidanceMode mode)
        {
            if (mode == _appliedMode)
            {
                _mode = mode;
                return;
            }

            ActiveRunner?.Pause();
            _mode = mode;
            _appliedMode = mode;
            if (isActiveAndEnabled && ActiveRunner != null && ActiveRunner.IsInProgress)
            {
                EnsureClockIsRunning();
            }

            ModeChanged?.Invoke(mode);
        }

        public void Play(SequenceRunner runner)
        {
            if (!isActiveAndEnabled)
            {
                throw new System.InvalidOperationException($"{name} must be active and enabled to run a sequence.");
            }

            Cancel();

            ActiveRunner = runner;
            ActiveRunner.Faulted += LogFault;
            EnsureClockIsRunning();
            ActiveRunner.Start();
        }

        public void Pause() => ActiveRunner?.Pause();

        public void Resume() => ActiveRunner?.Resume();

        public void SkipCurrentTask() => ActiveTaskRunner?.SkipCurrentStep();

        public void GoBack() => ActiveTaskRunner?.GoBack();

        public void SkipCurrentStage() => ActiveRunner?.SkipCurrentStep();

        public void GoBackStage() => ActiveRunner?.GoBack();

        public void RestartCurrentStage() => ActiveRunner?.RestartCurrentStep();

        public void Restart()
        {
            if (ActiveRunner == null)
            {
                return;
            }

            ActiveRunner.Faulted -= LogFault;
            ActiveRunner.Faulted += LogFault;
            ActiveRunner.Restart();
            EnsureClockIsRunning();
        }

        public void Cancel()
        {
            StopClock();

            if (ActiveRunner == null)
            {
                return;
            }

            ActiveRunner.Faulted -= LogFault;
            ActiveRunner.Cancel();
        }

        public void StopExecution() => Cancel();

        private void Awake()
        {
            _appliedMode = _mode;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _mode != _appliedMode)
            {
                var requested = _mode;
                _mode = _appliedMode;
                SetMode(requested);
            }
        }

        private void OnDisable()
        {
            StopClock();
            foreach (var runner in _runnersByMode)
            {
                if (runner != null)
                {
                    runner.Faulted -= LogFault;
                    runner.Cancel();
                }
            }
        }

        private SequenceRunner BuildRunnerFromTaskList()
        {
            var audioPlayer = ObtainAudioPlayer();

            if (_mode == GuidanceMode.Unguided)
            {
                return new SequenceRunner(name, StepsFor(_unguidedTaskList, audioPlayer));
            }

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
            while (ActiveRunner != null && (ActiveRunner.State == SequenceState.Idle || ActiveRunner.IsInProgress))
            {
                try
                {
                    ActiveRunner.Advance(Time.deltaTime);
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
