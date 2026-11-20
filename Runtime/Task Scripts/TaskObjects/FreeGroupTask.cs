using System.Collections;
using System.Collections.Generic;
using Framework.Audio;
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
        private MyClip _feedbackAudio;
        [SerializeField]
        private MyClip _outOfOrderFeedbackAudio;
        [SerializeField]
        private UnityEvent _onCompletedInListedOrder = new UnityEvent();
        [SerializeField]
        private UnityEvent _onCompletedOutOfOrder = new UnityEvent();
        [SerializeField]
        private UnityEvent<Task, int> _onTaskCompletedAtPosition = new UnityEvent<Task, int>();
        [SerializeField]
        private UnityEvent<Task> _onTaskCompleted = new UnityEvent<Task>();
        [SerializeField]
        private UnityEvent<Task> _onHintShown = new UnityEvent<Task>();

        private readonly List<Task> _running = new List<Task>();
        private readonly HashSet<Task> _acknowledged = new HashSet<Task>();
        private readonly List<Task> _completionOrder = new List<Task>();
        private Task _hintedTask;
        private Coroutine _idleHint;
        private bool _isRunning;
        private bool _isStarting;
        private bool _hasPlayedFeedback;
        private bool _isPaused;

        public UnityEvent<Task> OnTaskCompleted => _onTaskCompleted;
        public UnityEvent<Task, int> OnTaskCompletedAtPosition => _onTaskCompletedAtPosition;
        public UnityEvent OnCompletedInListedOrder => _onCompletedInListedOrder;
        public UnityEvent OnCompletedOutOfOrder => _onCompletedOutOfOrder;
        public IReadOnlyList<Task> CompletionOrder => _completionOrder;
        public bool WasCompletedInListedOrder => CompletionMatchesListedOrder();
        public UnityEvent<Task> OnHintShown => _onHintShown;
        public IReadOnlyList<Task> Tasks => _tasks;
        public Task HintedTask => _hintedTask;
        public int CompletedCount => _acknowledged.Count;
        public int OpenCount => _running.Count - _acknowledged.Count;

        public int PositionOf(Task task)
        {
            return _completionOrder.IndexOf(task) + 1;
        }

        public override bool IsSettling
        {
            get
            {
                if (_hasPlayedFeedback && (_audioPlayer?.IsPlaying() ?? false))
                {
                    return true;
                }

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
            _hintedTask = null;
            _hasPlayedFeedback = false;
            _isPaused = false;
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

        public override void ShowHint()
        {
            RequestHint();
        }

        public override void HideHint()
        {
            HideCurrentHint();
        }

        public override void PauseTask()
        {
            _isPaused = true;
            StopIdleHintTimer();
            foreach (var task in _running)
            {
                if (!task.IsComplete)
                {
                    task.PauseTask();
                }
            }
        }

        public override void ResumeTask()
        {
            _isPaused = false;
            foreach (var task in _running)
            {
                if (!task.IsComplete)
                {
                    task.ResumeTask();
                }
            }

            if (_isRunning)
            {
                RestartIdleHintTimer();
            }
        }

        public override void TaskComplete()
        {
            if (IsComplete)
            {
                return;
            }

            HideCurrentHint();
            CancelOpenTasks();
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
            CancelOpenTasks();
            StopEverythingInProgress();
            _running.Clear();
            _acknowledged.Clear();
            _completionOrder.Clear();
        }

        private void OnEnable()
        {
            if (_isRunning)
            {
                RestartIdleHintTimer();
            }
        }

        private void OnDisable()
        {
            StopIdleHintTimer();
        }

        private void CancelOpenTasks()
        {
            foreach (var task in _running)
            {
                task.Completed -= OnTaskFinished;
                if (!task.IsComplete)
                {
                    task.CancelTask();
                }
            }
        }

        private void CollectDistinctTasks()
        {
            _running.Clear();
            _acknowledged.Clear();
            _completionOrder.Clear();
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
                task.HideHint();
            }

            if (!task.IsSkip)
            {
                _completionOrder.Add(task);
            }

            task.OnCompleteTask.Invoke();
            _onTaskCompleted.Invoke(task);
            _onTaskCompletedAtPosition.Invoke(task, PositionOf(task));
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

            AnnounceOrderAndPlayFeedback();
            TaskComplete();
        }

        private void AnnounceOrderAndPlayFeedback()
        {
            if (IsSkip)
            {
                return;
            }

            var inOrder = CompletionMatchesListedOrder();
            var clip = !inOrder && _outOfOrderFeedbackAudio != null && _outOfOrderFeedbackAudio.clip != null
                ? _outOfOrderFeedbackAudio
                : _feedbackAudio;
            if (clip != null && clip.clip != null)
            {
                _audioPlayer?.LoadAndPlay(clip.clip);
                _hasPlayedFeedback = true;
            }

            (inOrder ? _onCompletedInListedOrder : _onCompletedOutOfOrder).Invoke();
        }

        private bool CompletionMatchesListedOrder()
        {
            var position = 0;
            foreach (var task in _running)
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
            if (!_isRunning || _isPaused || task == null)
            {
                return false;
            }

            if (task == _hintedTask && !replayIfAlreadyShown)
            {
                return true;
            }

            if (IsSettling)
            {
                return false;
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
            if (_hintAfterIdleSeconds > 0f && !_isPaused && isActiveAndEnabled)
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
