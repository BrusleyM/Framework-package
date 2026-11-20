using Framework.Audio;
using Framework.Tasks;

namespace Framework.Sequencing
{
    public sealed class TaskStep : StepBase
    {
        private readonly IVoicePlayer _audioPlayer;
        private bool _taskCompletedButStillSettling;

        public TaskStep(Task task, IVoicePlayer audioPlayer) : base(task.TaskName)
        {
            Task = task;
            _audioPlayer = audioPlayer;
        }

        public Task Task { get; }

        private bool IsTaskSettling => (Task as ISettlingTask)?.IsSettling ?? false;

        protected override void OnEnter()
        {
            _taskCompletedButStillSettling = false;
            Task.Completed += OnTaskCompleted;
            Task.Prepare();
            Task.SetAudioPlayer(_audioPlayer);
            Task.StartTask();
        }

        protected override void OnPause()
        {
            (Task as IPausableTask)?.PauseTask();
        }

        protected override void OnResume()
        {
            (Task as IPausableTask)?.ResumeTask();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            if (_taskCompletedButStillSettling && !IsTaskSettling)
            {
                Finish();
            }
        }

        protected override void OnExit(ExitReason reason)
        {
            Task.Completed -= OnTaskCompleted;

            if (Task == null)
            {
                return;
            }

            switch (reason)
            {
                case ExitReason.Completed:
                    SafeEvents.Invoke(Task.OnCompleteTask, Task);
                    break;
                case ExitReason.Skipped:
                    Task.Skip();
                    SafeEvents.Invoke(Task.OnCompleteTask, Task);
                    break;
                default:
                    Task.CancelTask();
                    break;
            }
        }

        private void OnTaskCompleted()
        {
            if (IsTaskSettling)
            {
                _taskCompletedButStillSettling = true;
                return;
            }

            Finish();
        }
    }
}
