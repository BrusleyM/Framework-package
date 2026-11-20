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
        private SubtaskSequencer _sequencer;
        private GameObjectTask[] _sequencerSource;
        private bool _isPaused;

        public GuidanceState State { get; private set; }
        public GameObjectTask CurrentSubtask => _sequencer?.Current;
        public override bool IsSettling => IsFeedbackAudioPlaying();
        public override bool HasHint => true;

        private SubtaskSequencer Subtasks
        {
            get
            {
                if (_sequencer == null || !ReferenceEquals(_sequencerSource, _subTasks))
                {
                    _sequencer?.Stop();
                    _sequencerSource = _subTasks;
                    _sequencer = new SubtaskSequencer(_subTasks);
                }

                return _sequencer;
            }
        }

        public override void Prepare()
        {
            base.Prepare();
            if (State == GuidanceState.Completed)
            {
                State = GuidanceState.Idle;
            }
        }

        public override void StartTask()
        {
            Restart(GuidanceState.Guided);
            PlayInstructionalAudio();
            StartReminder();
            Subtasks.Start(SubtaskFilter.All);
        }

        public override void StartTaskUnguided()
        {
            Restart(GuidanceState.Unguided);
            Subtasks.RunSetup();
        }

        public override void ShowHint()
        {
            if (State != GuidanceState.Unguided || IsComplete)
            {
                return;
            }

            State = GuidanceState.Hinting;
            PlayInstructionalAudio();
            Subtasks.Start(SubtaskFilter.GuidanceOnly);
        }

        public override void HideHint()
        {
            if (State != GuidanceState.Hinting)
            {
                return;
            }

            StopHint();
            _audioPlayer?.Stop();
        }

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

        public override void TaskComplete()
        {
            if (State == GuidanceState.Completed || IsComplete)
            {
                return;
            }

            var wasGuided = State == GuidanceState.Guided;
            var wasHinting = State == GuidanceState.Hinting;
            StopWatching();
            if (wasHinting)
            {
                Subtasks.TearDown(SubtaskFilter.GuidanceOnly);
                _audioPlayer?.Stop();
            }

            State = GuidanceState.Completed;
            if (wasGuided)
            {
                PlayFeedbackAudio();
            }

            MarkComplete();
        }

        public override void Skip()
        {
            if (State == GuidanceState.Completed || IsComplete)
            {
                return;
            }

            IsSkip = true;
            StopWatching();
            State = GuidanceState.Completed;
            _audioPlayer?.Stop();
            Subtasks.TearDown(SubtaskFilter.All);
            MarkComplete();
        }

        public override void CancelTask()
        {
            StopWatching();
            State = GuidanceState.Idle;
            _audioPlayer?.Stop();
            Subtasks.TearDown(SubtaskFilter.All);
            ResetCompletionTracking();
            IsDoing = false;
        }

        public bool IsFeedbackAudioPlaying()
        {
            return _audioPlayer?.IsPlaying() ?? false;
        }

        protected abstract void BeginCompletionTracking();

        protected abstract void EndCompletionTracking();

        protected abstract void ResetCompletionTracking();

        private void OnDisable()
        {
            StopWatching();
            if (State == GuidanceState.Hinting)
            {
                StopHint();
            }
        }

        private void Restart(GuidanceState newState)
        {
            _isPaused = false;
            StopWatching();
            State = newState;
            BeginCompletionTracking();
        }

        private void StopWatching()
        {
            StopReminder();
            EndCompletionTracking();
            _sequencer?.Stop();
        }

        private void StopHint()
        {
            State = GuidanceState.Unguided;
            Subtasks.Stop();
            Subtasks.TearDown(SubtaskFilter.GuidanceOnly);
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
                if (!IsComplete && !_isPaused)
                {
                    PlayInstructionalAudio();
                }
            }
        }
    }
}
