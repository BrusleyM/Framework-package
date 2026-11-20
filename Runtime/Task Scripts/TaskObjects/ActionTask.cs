using System.Collections;
using Framework.Audio;
using Framework.CheckPoints;
using UnityEngine;

namespace Framework.Tasks
{
    public class ActionTask : Task
    {
        [SerializeField]
        private MyClip _instructionalAudio;
        [SerializeField]
        private MyClip _feedbackAudioClip;
        [SerializeField]
        private float _reminderInterval;
        [SerializeField]
        private GameObjectTask[] _subTasks;
        [SerializeField]
        private CheckPointsParent _checkPoints;

        private Coroutine _reminderRoutine;
        private int _subTaskIndex;
        private bool _isAdvancingSubtasks;
        private bool _advanceRequested;
        private bool _isShowingHint;

        public CheckPointsParent CheckPointsParent => _checkPoints;
        public GameObjectTask CurrentSubtask { get; private set; }
        public override bool IsSettling => IsFeedbackAudioPlaying();

        public override void StartTaskUnguided()
        {
            StopEverythingInProgress();
            _isShowingHint = false;
            LinkCheckPointsToThisTask();
            _checkPoints.ResetAllPoints();
            ListenForAllCheckPointsChecked();
        }

        public override void ShowHint()
        {
            if (IsComplete || _isShowingHint)
            {
                return;
            }

            _isShowingHint = true;
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
            _isShowingHint = false;
            StopEverythingInProgress();
            LinkCheckPointsToThisTask();
            _checkPoints.ResetAllPoints();
            ListenForAllCheckPointsChecked();
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
            StopShowingHint();
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
            _checkPoints.ResetAllPoints();
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
            TearDownSubTasks();
        }

        private void StopEverythingInProgress()
        {
            StopReminder();
            StopListeningForAllCheckPointsChecked();
            DetachFromCurrentSubTask();
            _advanceRequested = false;
        }

        private void LinkCheckPointsToThisTask()
        {
            foreach (var point in _checkPoints.CheckPoints)
            {
                if (point != null)
                {
                    point.Parent = _checkPoints;
                }
            }
        }

        private void ListenForAllCheckPointsChecked()
        {
            _checkPoints.OnAllChecked.AddListener(TaskComplete);
        }

        private void StopListeningForAllCheckPointsChecked()
        {
            _checkPoints.OnAllChecked.RemoveListener(TaskComplete);
        }

        private void PlayInstructionalAudio()
        {
            _audioPlayer?.LoadAndPlay(_instructionalAudio?.clip);
        }

        private void PlayFeedbackAudio()
        {
            if (!IsSkip && _feedbackAudioClip != null)
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
                if (!IsComplete)
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
            while (_subTasks != null && _subTaskIndex < _subTasks.Length && _subTasks[_subTaskIndex] == null);

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

        private void DetachFromCurrentSubTask()
        {
            if (CurrentSubtask != null)
            {
                CurrentSubtask.Completed -= StartNextSubTask;
            }
        }

        private void TearDownSubTasks()
        {
            if (_subTasks == null)
            {
                return;
            }

            for (var index = _subTasks.Length - 1; index >= 0; index--)
            {
                if (_subTasks[index] != null)
                {
                    _subTasks[index].Cancel();
                }
            }
        }
    }
}
