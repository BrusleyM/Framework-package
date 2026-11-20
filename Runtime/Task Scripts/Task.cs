using System;
using Framework.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace Framework.Tasks
{
    public abstract class Task : MonoBehaviour
    {
        [SerializeField]
        private string _taskName;
        [SerializeField]
        private UnityEvent _onCompleteTask = new UnityEvent();

        protected IVoicePlayer _audioPlayer;

        public string TaskName => _taskName;
        public TaskOutcome Outcome { get; private set; }
        public bool IsComplete => Outcome != TaskOutcome.Pending;
        public bool IsSkip => Outcome == TaskOutcome.Skipped;
        public UnityEvent OnCompleteTask => _onCompleteTask;
        public ITaskClock Clock { get; private set; } = TaskClocks.Scaled;

        public event Action Completed;

        public void SetAudioPlayer(IVoicePlayer player)
        {
            _audioPlayer = player;
        }

        public void SetClock(ITaskClock clock)
        {
            Clock = clock ?? TaskClocks.Scaled;
        }

        public virtual void Prepare()
        {
            Outcome = TaskOutcome.Pending;
            Clock = TaskClocks.Scaled;
        }

        public abstract void StartTask();

        public virtual void TaskComplete()
        {
            MarkComplete();
        }

        public virtual void Skip()
        {
            MarkSkipped();
        }

        public virtual void CancelTask()
        {
        }

        protected void MarkComplete()
        {
            Finish(TaskOutcome.Completed);
        }

        protected void MarkSkipped()
        {
            Finish(TaskOutcome.Skipped);
        }

        private void Finish(TaskOutcome outcome)
        {
            if (IsComplete)
            {
                return;
            }

            Outcome = outcome;
            Completed?.Invoke();
        }
    }
}
