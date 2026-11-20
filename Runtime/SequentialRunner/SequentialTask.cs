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
        private bool _allowCuttingIntro;
        [SerializeField]
        private List<MyClip> _introAudio = new List<MyClip>();
        [SerializeField]
        private List<MyClip> _completeAudio = new List<MyClip>();

        private SequenceRunner _runner;
        private Coroutine _clock;
        private IVoicePlayer _audioPlayer;
        private readonly List<UnguidedStep> _unguidedSteps = new List<UnguidedStep>();
        private GuidanceMode _appliedMode;
        private bool _hasApplied;
        private bool _runnerCameFromTaskList;
        private bool _runnerIsIntro;
        private Coroutine _modeRestart;
        private bool _restartIsPaused;

        public GuidanceMode Mode => _mode;
        public List<Task> TaskList => _taskList;
        public IReadOnlyList<Task> CompletionOrder => CombinedCompletionOrder();
        public event System.Action<GuidanceMode> ModeChanged;
        public event System.Action<Task, int> UnguidedTaskCompleted;
        public event System.Action<bool> UnguidedCompleted;
        public SequenceRunner Runner => _runner;
        public Task CurrentTask => (ActiveTaskRunner?.CurrentStep as TaskStep)?.Task;
        public string CurrentStageName => (_runner?.CurrentStep as StageStep)?.Name;
        public int CurrentStageIndex => _runner != null && !_runnerIsIntro && !(_runner.CurrentStep is AudioStep) ? _runner.CurrentIndex : -1;
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

            LogSetupIssues();
            if (HasClips(_introAudio, ObtainAudioPlayer()))
            {
                PlayIntro();
            }
            else
            {
                PlayTaskList();
            }
        }

        public void Play(SequenceRunner runner)
        {
            StartRunner(runner, cameFromTaskList: false, isIntro: false);
        }

        private void PlayIntro()
        {
            var intro = new SequenceRunner(IntroStepName, new ISequenceStep[] { new AudioStep(IntroStepName, _introAudio, ObtainAudioPlayer()) });
            intro.Completed += () => PlayTasksWhenIntroEnds(intro);
            StartRunner(intro, cameFromTaskList: true, isIntro: true);
        }

        private void PlayTasksWhenIntroEnds(SequenceRunner intro)
        {
            if (_runner == intro)
            {
                PlayTaskList();
            }
        }

        private void PlayTaskList()
        {
            StartRunner(BuildRunnerFromTaskList(), cameFromTaskList: true, isIntro: false);
        }

        private void StartRunner(SequenceRunner runner, bool cameFromTaskList, bool isIntro)
        {
            if (!isActiveAndEnabled)
            {
                throw new System.InvalidOperationException($"{name} must be active and enabled to run a sequence.");
            }

            Cancel();

            if (!cameFromTaskList)
            {
                _unguidedSteps.Clear();
            }

            _runnerCameFromTaskList = cameFromTaskList;
            _runnerIsIntro = isIntro;
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

            if (_runnerIsIntro && IsRunning && !_allowCuttingIntro)
            {
                _mode = mode;
                _appliedMode = mode;
                ModeChanged?.Invoke(mode);
                return;
            }

            var restartAlreadyPending = _modeRestart != null;
            var shouldRestart = restartAlreadyPending || (IsRunning && _runnerCameFromTaskList && isActiveAndEnabled);
            if (!restartAlreadyPending)
            {
                _restartIsPaused = _runner != null && _runner.State == SequenceState.Paused;
            }

            CancelRunner();
            _unguidedSteps.Clear();
            _mode = mode;
            _appliedMode = mode;
            ModeChanged?.Invoke(mode);
            LogSetupIssues();
            if (shouldRestart && !restartAlreadyPending)
            {
                _modeRestart = StartCoroutine(RestartInNewModeNextFrame());
            }
        }

        public void UseVoicePlayer(IVoicePlayer voicePlayer)
        {
            _audioPlayer = voicePlayer;
        }

        public IEnumerable<Task> AllTasks
        {
            get
            {
                if (_stages.Count == 0)
                {
                    return _taskList;
                }

                var tasks = new List<Task>();
                foreach (var stage in _stages)
                {
                    if (stage != null)
                    {
                        tasks.AddRange(stage.Tasks);
                    }
                }

                return tasks;
            }
        }

        public IReadOnlyList<SetupIssue> GetSetupIssues()
        {
            return SequenceSetupRules.Check(
                _mode,
                _taskList,
                _stages,
                _audioSource != null || _audioPlayer != null,
                ContainsClip(_introAudio),
                ContainsClip(_completeAudio),
                _hintAfterIdleSeconds);
        }

        public bool RequestHint() => ActiveUnguidedStep?.RequestHint() ?? false;

        public void Pause()
        {
            if (_modeRestart != null)
            {
                _restartIsPaused = true;
                return;
            }

            _runner?.Pause();
        }

        public void Resume()
        {
            if (_modeRestart != null)
            {
                _restartIsPaused = false;
                return;
            }

            _runner?.Resume();
        }

        public void SkipCurrentTask() => ActiveTaskRunner?.SkipCurrentStep();

        public void GoBack() => ActiveTaskRunner?.GoBack();

        public void SkipCurrentStage() => _runner?.SkipCurrentStep();

        public void GoBackStage() => _runner?.GoBack();

        public void RestartCurrentStage() => _runner?.RestartCurrentStep();

        public void Restart()
        {
            if (_runner == null || _modeRestart != null)
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
            StopModeRestart();
            CancelRunner();
        }

        private void CancelRunner()
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

        private SequenceRunner BuildRunnerFromTaskList()
        {
            var audioPlayer = ObtainAudioPlayer();
            var steps = new List<ISequenceStep>();
            _unguidedSteps.Clear();

            steps.AddRange(TaskAndStageSteps(audioPlayer));
            if (HasClips(_completeAudio, audioPlayer))
            {
                steps.Add(new AudioStep("Complete", _completeAudio, audioPlayer));
            }

            return new SequenceRunner(name, steps);
        }

        private static bool ContainsClip(List<MyClip> clips)
        {
            foreach (var clip in clips)
            {
                if (clip != null && clip.clip != null)
                {
                    return true;
                }
            }
            return false;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogSetupIssues()
        {
            foreach (var issue in GetSetupIssues())
            {
                Debug.LogWarning($"{name}: {issue.Message}", this);
            }
        }

        private static bool HasClips(List<MyClip> clips, IVoicePlayer audioPlayer)
        {
            return audioPlayer != null && ContainsClip(clips);
        }

        private List<ISequenceStep> TaskAndStageSteps(IVoicePlayer audioPlayer)
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

        private List<ISequenceStep> StepsFor(string stepName, IEnumerable<Task> tasks, IVoicePlayer audioPlayer)
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

        private IVoicePlayer ObtainAudioPlayer()
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

        private IEnumerator RestartInNewModeNextFrame()
        {
            yield return null;
            _modeRestart = null;
            PlayTaskList();
            if (_restartIsPaused)
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
