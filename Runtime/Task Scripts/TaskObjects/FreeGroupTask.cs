using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Framework.Tasks
{
    public class FreeGroupTask : Task
    {
        [SerializeField]
        private List<Task> _tasks = new List<Task>();
        [SerializeField]
        private float _hintAfterIdleSeconds;
        [SerializeField]
        private UnityEvent<Task> _onTaskCompleted = new UnityEvent<Task>();
        [SerializeField]
        private UnityEvent<Task> _onHintShown = new UnityEvent<Task>();

        private readonly List<Task> _running = new List<Task>();
        private readonly HashSet<Task> _acknowledged = new HashSet<Task>();
        private Task _hintedTask;
        private Coroutine _idleHint;
        private bool _isRunning;
        private bool _isStarting;

        public UnityEvent<Task> OnTaskCompleted => _onTaskCompleted;
        public UnityEvent<Task> OnHintShown => _onHintShown;
        public IReadOnlyList<Task> Tasks => _tasks;
        public Task HintedTask => _hintedTask;
        public int CompletedCount => _acknowledged.Count;
        public int OpenCount => _running.Count - _acknowledged.Count;

        public override bool IsSettling
        {
            get
            {
                foreach (var task in _running)
                {
                    if (task.IsSettling)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void StartTask()
        {
            StopEverythingInProgress();
            _isRunning = true;
            CollectDistinctTasks();
            _isStarting = true;
            try
            {
                StartEveryTaskUnguided();
            }
            finally
            {
                _isStarting = false;
            }

            CompleteWhenEveryTaskIsComplete();
            if (_isRunning)
            {
                RestartIdleHintTimer();
            }
        }

        public bool RequestHint()
        {
            var shown = ShowHintOn(FirstOpenTask(), true);
            if (shown)
            {
                RestartIdleHintTimer();
            }
            return shown;
        }

        public override void TaskComplete()
        {
            if (IsComplete)
            {
                return;
            }

            StopEverythingInProgress();
            MarkComplete();
        }

        public override void Skip()
        {
            if (IsComplete)
            {
                return;
            }

            IsSkip = true;
            HideCurrentHint();
            foreach (var task in _running.ToArray())
            {
                if (!task.IsComplete)
                {
                    task.Skip();
                }
            }

            StopEverythingInProgress();
            MarkComplete();
        }

        public override void CancelTask()
        {
            HideCurrentHint();
            foreach (var task in _running)
            {
                task.Completed -= OnTaskFinished;
                if (!task.IsComplete)
                {
                    task.CancelTask();
                }
            }

            StopEverythingInProgress();
            _running.Clear();
            _acknowledged.Clear();
        }

        private void OnDisable()
        {
            StopIdleHintTimer();
        }

        private void CollectDistinctTasks()
        {
            _running.Clear();
            _acknowledged.Clear();
            foreach (var task in _tasks)
            {
                if (task != null && task != this && !_running.Contains(task))
                {
                    _running.Add(task);
                }
            }
        }

        private void StartEveryTaskUnguided()
        {
            foreach (var task in _running)
            {
                task.Prepare();
                task.SetAudioPlayer(_audioPlayer);
                task.Completed += OnTaskFinished;
            }

            foreach (var task in _running.ToArray())
            {
                if (_isRunning && !task.IsComplete)
                {
                    task.StartTaskUnguided();
                }
            }
        }

        private void OnTaskFinished()
        {
            foreach (var task in _running.ToArray())
            {
                if (_isRunning && task.IsComplete && _acknowledged.Add(task))
                {
                    AcknowledgeCompletion(task);
                }
            }

            if (!_isStarting)
            {
                CompleteWhenEveryTaskIsComplete();
            }
        }

        private void AcknowledgeCompletion(Task task)
        {
            task.Completed -= OnTaskFinished;
            if (task == _hintedTask)
            {
                _hintedTask = null;
            }

            task.OnCompleteTask.Invoke();
            _onTaskCompleted.Invoke(task);
            if (_isRunning)
            {
                RestartIdleHintTimer();
            }
        }

        private void CompleteWhenEveryTaskIsComplete()
        {
            if (!_isRunning)
            {
                return;
            }

            foreach (var task in _running)
            {
                if (!task.IsComplete)
                {
                    return;
                }
            }

            foreach (var task in _running.ToArray())
            {
                if (_acknowledged.Add(task))
                {
                    AcknowledgeCompletion(task);
                }
            }

            TaskComplete();
        }

        private Task FirstOpenTask()
        {
            foreach (var task in _running)
            {
                if (!task.IsComplete)
                {
                    return task;
                }
            }
            return null;
        }

        private bool ShowHintOn(Task task, bool replayIfAlreadyShown)
        {
            if (!_isRunning || task == null)
            {
                return false;
            }

            if (task == _hintedTask && !replayIfAlreadyShown)
            {
                return true;
            }

            HideCurrentHint();
            _hintedTask = task;
            task.ShowHint();
            _onHintShown.Invoke(task);
            return true;
        }

        private void HideCurrentHint()
        {
            if (_hintedTask != null)
            {
                _hintedTask.HideHint();
                _hintedTask = null;
            }
        }

        private void StopEverythingInProgress()
        {
            _isRunning = false;
            StopIdleHintTimer();
            foreach (var task in _running)
            {
                task.Completed -= OnTaskFinished;
            }
        }

        private void RestartIdleHintTimer()
        {
            StopIdleHintTimer();
            if (_hintAfterIdleSeconds > 0f && isActiveAndEnabled)
            {
                _idleHint = StartCoroutine(HintWhileIdle());
            }
        }

        private void StopIdleHintTimer()
        {
            if (_idleHint != null)
            {
                StopCoroutine(_idleHint);
                _idleHint = null;
            }
        }

        private IEnumerator HintWhileIdle()
        {
            var wait = new WaitForSeconds(_hintAfterIdleSeconds);
            while (_isRunning)
            {
                yield return wait;
                ShowHintOn(FirstOpenTask(), false);
            }
        }
    }
}
