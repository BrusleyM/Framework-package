using System;
using System.Collections.Generic;
using Framework.Audio;
using Framework.Timing;
using Framework.Tasks;

namespace Framework.Sequencing
{
    public sealed class UnguidedStep : StepBase
    {
        private readonly List<Task> _tasks = new List<Task>();
        private readonly HashSet<Task> _acknowledged = new HashSet<Task>();
        private readonly List<Task> _completionOrder = new List<Task>();
        private readonly AudioPlayer _audioPlayer;
        private readonly CountdownTimer _idleTimer;

        private Task _hintedTask;
        private bool _isPaused;
        private bool _isStarting;
        private bool _isAllComplete;

        public UnguidedStep(
            string name,
            IEnumerable<Task> tasks,
            AudioPlayer audioPlayer,
            float hintAfterIdleSeconds = 0f) : base(name)
        {
            foreach (var task in tasks)
            {
                if (task != null && !(task is IGuidanceOnlyTask) && !_tasks.Contains(task))
                {
                    _tasks.Add(task);
                }
            }

            _audioPlayer = audioPlayer;
            if (hintAfterIdleSeconds > 0f)
            {
                _idleTimer = new CountdownTimer(hintAfterIdleSeconds);
                _idleTimer.Expired += ShowIdleHint;
            }
        }

        public event Action<Task, int> TaskCompleted;

        public event Action<bool> AllTasksCompleted;

        public IReadOnlyList<Task> Tasks => _tasks;
        public IReadOnlyList<Task> CompletionOrder => _completionOrder;
        public Task HintedTask => _hintedTask;
        public int CompletedCount => _acknowledged.Count;
        public int OpenCount => _tasks.Count - _acknowledged.Count;
        public bool WasCompletedInListedOrder => CompletionMatchesListedOrder();

        public int PositionOf(Task task)
        {
            return _completionOrder.IndexOf(task) + 1;
        }

        public bool RequestHint()
        {
            var shown = ShowHintOn(FirstOpenTask(), true);
            if (shown)
            {
                _idleTimer?.Restart();
            }
            return shown;
        }

        protected override void OnEnter()
        {
            _acknowledged.Clear();
            _completionOrder.Clear();
            _hintedTask = null;
            _isPaused = false;
            _isAllComplete = false;
            _isStarting = true;
            try
            {
                StartEveryTask();
            }
            finally
            {
                _isStarting = false;
            }

            CompleteWhenEveryTaskIsComplete();
            if (!_isAllComplete)
            {
                _idleTimer?.Restart();
            }
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            if (_isAllComplete)
            {
                FinishWhenSettled();
                return;
            }

            _idleTimer?.Advance(deltaSeconds);
        }

        protected override void OnPause()
        {
            _isPaused = true;
            _idleTimer?.Pause();
            foreach (var task in _tasks)
            {
                if (!task.IsComplete)
                {
                    (task as IPausableTask)?.PauseTask();
                }
            }
        }

        protected override void OnResume()
        {
            _isPaused = false;
            _idleTimer?.Resume();
            foreach (var task in _tasks)
            {
                if (!task.IsComplete)
                {
                    (task as IPausableTask)?.ResumeTask();
                }
            }
        }

        protected override void OnExit(ExitReason reason)
        {
            _idleTimer?.Stop();
            HideCurrentHint();
            foreach (var task in _tasks.ToArray())
            {
                if (task.IsComplete)
                {
                    task.Completed -= OnTaskFinished;
                    continue;
                }

                if (reason == ExitReason.Skipped)
                {
                    task.Skip();
                    task.OnCompleteTask.Invoke();
                }
                else
                {
                    task.CancelTask();
                }
                task.Completed -= OnTaskFinished;
            }
        }

        private static bool IsSettling(Task task)
        {
            return (task as ISettlingTask)?.IsSettling ?? false;
        }

        private static void StartUnguided(Task task)
        {
            if (task is IUnguidedTask unguided)
            {
                unguided.StartTaskUnguided();
            }
            else
            {
                task.StartTask();
            }
        }

        private void StartEveryTask()
        {
            foreach (var task in _tasks)
            {
                task.Prepare();
                task.SetAudioPlayer(_audioPlayer);
                task.Completed += OnTaskFinished;
            }

            foreach (var task in _tasks.ToArray())
            {
                if (!task.IsComplete)
                {
                    StartUnguided(task);
                }
            }
        }

        private void OnTaskFinished()
        {
            if (!IsWaiting)
            {
                return;
            }

            foreach (var task in _tasks.ToArray())
            {
                if (!IsWaiting)
                {
                    return;
                }

                if (task.IsComplete && _acknowledged.Add(task))
                {
                    Acknowledge(task);
                }
            }

            if (!_isStarting)
            {
                CompleteWhenEveryTaskIsComplete();
            }
        }

        private void Acknowledge(Task task)
        {
            task.Completed -= OnTaskFinished;
            if (task == _hintedTask)
            {
                _hintedTask = null;
                (task as IHintable)?.HideHint();
            }

            if (!task.IsSkip)
            {
                _completionOrder.Add(task);
            }

            _idleTimer?.Restart();
            task.OnCompleteTask.Invoke();
            TaskCompleted?.Invoke(task, PositionOf(task));
        }

        private void CompleteWhenEveryTaskIsComplete()
        {
            if (_isAllComplete || !IsWaiting)
            {
                return;
            }

            foreach (var task in _tasks)
            {
                if (!task.IsComplete)
                {
                    return;
                }
            }

            foreach (var task in _tasks.ToArray())
            {
                if (!IsWaiting)
                {
                    return;
                }

                if (_acknowledged.Add(task))
                {
                    Acknowledge(task);
                }
            }

            if (!IsWaiting)
            {
                return;
            }

            _isAllComplete = true;
            _idleTimer?.Stop();
            AllTasksCompleted?.Invoke(CompletionMatchesListedOrder());
        }

        private void FinishWhenSettled()
        {
            foreach (var task in _tasks)
            {
                if (IsSettling(task))
                {
                    return;
                }
            }

            Finish();
        }

        private bool CompletionMatchesListedOrder()
        {
            var position = 0;
            foreach (var task in _tasks)
            {
                if (task.IsSkip)
                {
                    continue;
                }

                if (position >= _completionOrder.Count || _completionOrder[position] != task)
                {
                    return false;
                }
                position++;
            }
            return position == _completionOrder.Count;
        }

        private Task FirstOpenTask()
        {
            foreach (var task in _tasks)
            {
                if (!task.IsComplete && task is IHintable)
                {
                    return task;
                }
            }
            return null;
        }

        private bool IsAnyTaskSettling()
        {
            foreach (var task in _tasks)
            {
                if (IsSettling(task))
                {
                    return true;
                }
            }
            return false;
        }

        private void ShowIdleHint()
        {
            ShowHintOn(FirstOpenTask(), false);
            _idleTimer.Restart();
        }

        private bool ShowHintOn(Task task, bool replayIfAlreadyShown)
        {
            if (!IsWaiting || _isAllComplete || _isPaused || task == null)
            {
                return false;
            }

            if (task == _hintedTask && !replayIfAlreadyShown)
            {
                return true;
            }

            if (IsAnyTaskSettling())
            {
                return false;
            }

            HideCurrentHint();
            _hintedTask = task;
            ((IHintable)task).ShowHint();
            return true;
        }

        private void HideCurrentHint()
        {
            if (_hintedTask != null)
            {
                (_hintedTask as IHintable)?.HideHint();
                _hintedTask = null;
            }
        }
    }
}
