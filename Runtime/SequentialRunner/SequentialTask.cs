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
        private float _hintAfterIdleSeconds;
        [SerializeField]
        private List<MyClip> _introAudio = new List<MyClip>();
        [SerializeField]
        private List<MyClip> _completeAudio = new List<MyClip>();

        private SequenceRunner _runner;
        private Coroutine _clock;
        private AudioPlayer _audioPlayer;
        private readonly List<UnguidedStep> _unguidedSteps = new List<UnguidedStep>();
        private GuidanceMode _appliedMode;
        private bool _hasApplied;
        private bool _runnerCameFromTaskList;
        private int _leadingStepCount;
        private Coroutine _modeRestart;

        public GuidanceMode Mode => _mode;
        public List<Task> TaskList => _taskList;
        public IReadOnlyList<Task> CompletionOrder => CombinedCompletionOrder();
        public event System.Action<GuidanceMode> ModeChanged;
        public event System.Action<Task, int> UnguidedTaskCompleted;
        public event System.Action<bool> UnguidedCompleted;
        public SequenceRunner Runner => _runner;
        public Task CurrentTask => (ActiveTaskRunner?.CurrentStep as TaskStep)?.Task;
        public string CurrentStageName => (_runner?.CurrentStep as StageStep)?.Name;
        public int CurrentStageIndex => _runner != null && !(_runner.CurrentStep is AudioStep) ? _runner.CurrentIndex - _leadingStepCount : -1;
        public bool HasRun => _runner != null && _runner.State != SequenceState.Idle;
        public bool IsRunning => _runner != null && _runner.IsInProgress;

        private const string IntroStepName = "Intro";

        private UnguidedStep ActiveUnguidedStep => ActiveTaskRunner?.CurrentStep as UnguidedStep;

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

            PlayTaskList(includeIntro: true);
        }

        public void Play(SequenceRunner runner)
        {
            Start(runner, leadingStepCount: 0, cameFromTaskList: false);
        }

        private void PlayTaskList(bool includeIntro)
        {
            var runner = BuildRunnerFromTaskList(includeIntro, out var leadingStepCount);
            Start(runner, leadingStepCount, cameFromTaskList: true);
        }

        private void Start(SequenceRunner runner, int leadingStepCount, bool cameFromTaskList)
        {
            if (!isActiveAndEnabled)
            {
                throw new System.InvalidOperationException($"{name} must be active and enabled to run a sequence.");
            }

            Cancel();

            _runnerCameFromTaskList = cameFromTaskList;
            _leadingStepCount = leadingStepCount;
            _runner = runner;
            _runner.Faulted += LogFault;
            EnsureClockIsRunning();
            _runner.Start();
        }

        public void SetMode(GuidanceMode mode)
        {
            if (!_hasApplied || mode == _appliedMode)
            {
                _mode = mode;
                return;
            }

            var shouldRestart = IsRunning && _runnerCameFromTaskList && isActiveAndEnabled;
            var wasPaused = _runner != null && _runner.State == SequenceState.Paused;
            var wasInIntro = _runner?.CurrentStep is AudioStep step && step.Name == IntroStepName;
            Cancel();
            _unguidedSteps.Clear();
            _mode = mode;
            _appliedMode = mode;
            ModeChanged?.Invoke(mode);
            if (shouldRestart)
            {
                StopModeRestart();
                _modeRestart = StartCoroutine(RestartInNewModeNextFrame(wasInIntro, wasPaused));
            }
        }

        public bool RequestHint() => ActiveUnguidedStep?.RequestHint() ?? false;

        public void Pause() => _runner?.Pause();

        public void Resume() => _runner?.Resume();

        public void SkipCurrentTask() => ActiveTaskRunner?.SkipCurrentStep();

        public void GoBack() => GoBackWithin(ActiveTaskRunner);

        public void SkipCurrentStage() => _runner?.SkipCurrentStep();

        public void GoBackStage() => GoBackWithin(_runner);

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
            StopModeRestart();

            if (_runner == null)
            {
                return;
            }

            _runner.Faulted -= LogFault;
            _runner.Cancel();
        }

        public void StopExecution() => Cancel();

        private void Awake()
        {
            _appliedMode = _mode;
            _hasApplied = true;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _hasApplied && _mode != _appliedMode)
            {
                var requested = _mode;
                _mode = _appliedMode;
                SetMode(requested);
            }
        }

        private void OnDisable()
        {
            Cancel();
        }

        private SequenceRunner BuildRunnerFromTaskList(bool includeIntro, out int leadingStepCount)
        {
            var audioPlayer = ObtainAudioPlayer();
            var steps = new List<ISequenceStep>();
            _unguidedSteps.Clear();
            leadingStepCount = 0;
            if (includeIntro && HasClips(_introAudio, audioPlayer))
            {
                steps.Add(new AudioStep(IntroStepName, _introAudio, audioPlayer));
                leadingStepCount = 1;
            }

            steps.AddRange(TaskAndStageSteps(audioPlayer));
            if (HasClips(_completeAudio, audioPlayer))
            {
                steps.Add(new AudioStep("Complete", _completeAudio, audioPlayer));
            }

            return new SequenceRunner(name, steps);
        }

        private static bool HasClips(List<MyClip> clips, AudioPlayer audioPlayer)
        {
            if (audioPlayer == null)
            {
                return false;
            }

            foreach (var clip in clips)
            {
                if (clip != null && clip.clip != null)
                {
                    return true;
                }
            }
            return false;
        }

        private List<ISequenceStep> TaskAndStageSteps(AudioPlayer audioPlayer)
        {
            if (_stages.Count == 0)
            {
                return StepsFor(name, _taskList, audioPlayer);
            }

            var stageSteps = new List<ISequenceStep>();
            foreach (var stage in _stages)
            {
                if (stage != null)
                {
                    var stageRunner = new SequenceRunner(stage.StageName, StepsFor(stage.StageName, stage.Tasks, audioPlayer));
                    stageSteps.Add(new StageStep(stage.StageName, stageRunner));
                }
            }

            return stageSteps;
        }

        private List<ISequenceStep> StepsFor(string stepName, IEnumerable<Task> tasks, AudioPlayer audioPlayer)
        {
            var steps = new List<ISequenceStep>();
            if (_mode == GuidanceMode.Unguided)
            {
                var unguided = new UnguidedStep(stepName, tasks, audioPlayer, _hintAfterIdleSeconds);
                unguided.TaskCompleted += (task, position) => UnguidedTaskCompleted?.Invoke(task, position);
                unguided.AllTasksCompleted += inOrder => UnguidedCompleted?.Invoke(inOrder);
                _unguidedSteps.Add(unguided);
                steps.Add(unguided);
                return steps;
            }

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

        private IReadOnlyList<Task> CombinedCompletionOrder()
        {
            if (_unguidedSteps.Count == 0)
            {
                return null;
            }

            if (_unguidedSteps.Count == 1)
            {
                return _unguidedSteps[0].CompletionOrder;
            }

            var combined = new List<Task>();
            foreach (var step in _unguidedSteps)
            {
                combined.AddRange(step.CompletionOrder);
            }
            return combined;
        }

        private void GoBackWithin(SequenceRunner runner)
        {
            if (runner == null)
            {
                return;
            }

            var isTopLevel = runner == _runner;
            if (isTopLevel && _leadingStepCount > 0 && runner.CurrentIndex <= _leadingStepCount)
            {
                runner.RestartCurrentStep();
            }
            else
            {
                runner.GoBack();
            }
        }

        private IEnumerator RestartInNewModeNextFrame(bool includeIntro, bool pauseAgain)
        {
            yield return null;
            _modeRestart = null;
            PlayTaskList(includeIntro);
            if (pauseAgain)
            {
                _runner.Pause();
            }
        }

        private void StopModeRestart()
        {
            if (_modeRestart != null)
            {
                StopCoroutine(_modeRestart);
                _modeRestart = null;
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
