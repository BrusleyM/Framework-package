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

        protected AudioPlayer _audioPlayer;

        public string TaskName => _taskName;
        public bool IsDoing { get; protected set; }
        public bool IsComplete { get; protected set; }
        public bool IsSkip { get; protected set; }
        public UnityEvent OnCompleteTask => _onCompleteTask;
        public virtual bool IsSettling => false;

        public event Action Completed;

        public void SetAudioPlayer(AudioPlayer player)
        {
            _audioPlayer = player;
        }

        public void Prepare()
        {
            IsComplete = false;
            IsSkip = false;
            IsDoing = false;
        }

        public abstract void StartTask();

        public virtual void StartTaskUnguided()
        {
            StartTask();
        }

        public virtual void ShowHint()
        {
        }

        public virtual void HideHint()
        {
        }

        public virtual void TaskComplete()
        {
            MarkComplete();
        }

        public virtual void Skip()
        {
            IsSkip = true;
            MarkComplete();
        }

        public virtual void CancelTask()
        {
        }

        protected void MarkComplete()
        {
            if (IsComplete)
            {
                return;
            }

            IsComplete = true;
            IsDoing = false;
            Completed?.Invoke();
        }
    }
}
