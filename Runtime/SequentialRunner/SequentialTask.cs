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

        private SequenceRunner _runner;
        private Coroutine _clock;
        private AudioPlayer _audioPlayer;

        public List<Task> TaskList => _taskList;
        public SequenceRunner Runner => _runner;
        public Task CurrentTask => (_runner?.CurrentStep as TaskStep)?.Task;
        public bool HasRun => _runner != null && _runner.State != SequenceState.Idle;
        public bool IsRunning => _runner != null && _runner.IsInProgress;

        public void AddTask(Task task)
        {
            _taskList.Add(task);
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
            Cancel();

            _runner = runner;
            _runner.Faulted += LogFault;
            _runner.Start();
            StartClock();
        }

        public void Pause() => _runner?.Pause();

        public void Resume() => _runner?.Resume();

        public void SkipCurrentTask() => _runner?.SkipCurrentStep();

        public void GoBack() => _runner?.GoBack();

        public void Restart() => _runner?.Restart();

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
            var steps = new List<ISequenceStep>();
            var audioPlayer = ObtainAudioPlayer();

            foreach (var task in _taskList)
            {
                if (task != null)
                {
                    steps.Add(new TaskStep(task, audioPlayer));
                }
            }

            return new SequenceRunner(name, steps);
        }

        private AudioPlayer ObtainAudioPlayer()
        {
            if (_audioPlayer == null && _audioSource != null)
            {
                _audioPlayer = new AudioPlayer(_audioSource);
            }

            return _audioPlayer;
        }

        private void StartClock()
        {
            StopClock();
            _clock = StartCoroutine(TickWhileInProgress(_runner));
        }

        private void StopClock()
        {
            if (_clock != null)
            {
                StopCoroutine(_clock);
                _clock = null;
            }
        }

        private IEnumerator TickWhileInProgress(SequenceRunner runner)
        {
            while (runner.IsInProgress)
            {
                runner.Advance(Time.deltaTime);
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
