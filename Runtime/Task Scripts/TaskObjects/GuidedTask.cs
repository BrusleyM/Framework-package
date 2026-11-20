using System.Collections;
using Framework.Audio;
using UnityEngine;

namespace Framework.Tasks
{
    public abstract class GuidedTask : Task
    {
        [SerializeField]
        private MyClip _instructionalAudio;
        [SerializeField]
        private MyClip _feedbackAudioClip;
        [SerializeField]
        private float _reminderInterval;
        [SerializeField]
        private GameObjectTask[] _subTasks;

        private Coroutine _reminderRoutine;
        private int _subTaskIndex;
        private bool _isAdvancingSubtasks;
        private bool _advanceRequested;
        private bool _isShowingHint;
        private bool _isUnguided;
        private bool _runsGuidanceOnly;
        private bool _isPaused;

        public GameObjectTask CurrentSubtask { get; private set; }
        public override bool IsSettling => IsFeedbackAudioPlaying();
        public override bool HasHint => true;

        public override void PauseTask()
        {
            _isPaused = true;
            _audioPlayer?.Pause();
        }

        public override void ResumeTask()
        {
            _isPaused = false;
            _audioPlayer?.Resume();
        }

        public override void StartTaskUnguided()
        {
            _isPaused = false;
            StopEverythingInProgress();
            _isShowingHint = false;
            _isUnguided = true;
            BeginCompletionTracking();
            RunSetupSubTasks();
        }

        public override void ShowHint()
        {
            if (IsComplete || _isShowingHint)
            {
                return;
            }

            _isShowingHint = true;
            _runsGuidanceOnly = true;
            PlayInstructionalAudio();
            StartSubTasks();
        }

        public override void HideHint()
        {
            if (!_isShowingHint)
            {
                return;
            }

            StopShowingHint();
            _audioPlayer?.Stop();
        }

        public override void StartTask()
        {
            _isUnguided = false;
            _runsGuidanceOnly = false;
            _isPaused = false;
            _isShowingHint = false;
            StopEverythingInProgress();
            BeginCompletionTracking();
            PlayInstructionalAudio();
            StartReminder();
            StartSubTasks();
        }

        public override void TaskComplete()
        {
            if (IsComplete)
            {
                return;
            }

            StopEverythingInProgress();
            if (_isShowingHint)
            {
                StopShowingHint();
                _audioPlayer?.Stop();
            }

            PlayFeedbackAudio();
            MarkComplete();
        }

        public override void Skip()
        {
            if (IsComplete)
            {
                return;
            }

            IsSkip = true;
            StopEverythingInProgress();
            _isShowingHint = false;
            _audioPlayer?.Stop();
            TearDownSubTasks();
            MarkComplete();
        }

        public override void CancelTask()
        {
            StopEverythingInProgress();
            _isShowingHint = false;
            _audioPlayer?.Stop();
            TearDownSubTasks();
            ResetCompletionTracking();
            IsDoing = false;
        }

        public bool IsFeedbackAudioPlaying()
        {
            return _audioPlayer?.IsPlaying() ?? false;
        }

        private void OnDisable()
        {
            StopEverythingInProgress();
            StopShowingHint();
        }

        private void StopShowingHint()
        {
            if (!_isShowingHint)
            {
                return;
            }

            _isShowingHint = false;
            DetachFromCurrentSubTask();
            _advanceRequested = false;
            TearDownSubTasks(guidanceOnly: true);
        }

        private void RunSetupSubTasks()
        {
            if (_subTasks == null)
            {
                return;
            }

            foreach (var subTask in _subTasks)
            {
                if (subTask != null && subTask.IsSetup)
                {
                    subTask.Prepare();
                    subTask.Execute();
                }
            }
        }

        private void StopEverythingInProgress()
        {
            StopReminder();
            EndCompletionTracking();
            DetachFromCurrentSubTask();
            _advanceRequested = false;
        }

        protected abstract void BeginCompletionTracking();

        protected abstract void EndCompletionTracking();

        protected abstract void ResetCompletionTracking();

        private void PlayInstructionalAudio()
        {
            _audioPlayer?.LoadAndPlay(_instructionalAudio?.clip);
        }

        private void PlayFeedbackAudio()
        {
            if (!IsSkip && !_isUnguided && _feedbackAudioClip != null)
            {
                _audioPlayer?.LoadAndPlay(_feedbackAudioClip.clip);
            }
        }

        private void StartReminder()
        {
            if (_reminderInterval > 0)
            {
                _reminderRoutine = StartCoroutine(RemindUntilComplete());
            }
        }

        private void StopReminder()
        {
            if (_reminderRoutine != null)
            {
                StopCoroutine(_reminderRoutine);
                _reminderRoutine = null;
            }
        }

        private IEnumerator RemindUntilComplete()
        {
            var interval = new WaitForSeconds(_reminderInterval);
            while (!IsComplete)
            {
                yield return interval;
                if (!IsComplete && !_isPaused)
                {
                    PlayInstructionalAudio();
                }
            }
        }

        private void StartSubTasks()
        {
            _subTaskIndex = -1;
            StartNextSubTask();
        }

        private void StartNextSubTask()
        {
            if (_isAdvancingSubtasks)
            {
                _advanceRequested = true;
                return;
            }

            _isAdvancingSubtasks = true;
            try
            {
                do
                {
                    _advanceRequested = false;
                    StartTheNextListedSubTask();
                }
                while (_advanceRequested);
            }
            finally
            {
                _isAdvancingSubtasks = false;
                _advanceRequested = false;
            }
        }

        private void StartTheNextListedSubTask()
        {
            DetachFromCurrentSubTask();
            do
            {
                _subTaskIndex++;
            }
            while (_subTasks != null && _subTaskIndex < _subTasks.Length && !IsRunnableNow(_subTasks[_subTaskIndex]));

            if (_subTasks == null || _subTaskIndex >= _subTasks.Length)
            {
                CurrentSubtask = null;
                return;
            }

            CurrentSubtask = _subTasks[_subTaskIndex];
            CurrentSubtask.Prepare();
            CurrentSubtask.Completed += StartNextSubTask;
            CurrentSubtask.Execute();
        }

        private bool IsRunnableNow(GameObjectTask subTask)
        {
            return subTask != null && !(_runsGuidanceOnly && subTask.IsSetup);
        }

        private void DetachFromCurrentSubTask()
        {
            if (CurrentSubtask != null)
            {
                CurrentSubtask.Completed -= StartNextSubTask;
            }
        }

        private void TearDownSubTasks(bool guidanceOnly = false)
        {
            if (_subTasks == null)
            {
                return;
            }

            for (var index = _subTasks.Length - 1; index >= 0; index--)
            {
                if (_subTasks[index] != null && !(guidanceOnly && _subTasks[index].IsSetup))
                {
                    _subTasks[index].Cancel();
                }
            }
        }
    }
}
