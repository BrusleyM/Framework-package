using Framework.Audio;
using Framework.Tasks;

namespace Framework.Sequencing
{
    public sealed class TaskStep : StepBase
    {
        private readonly AudioPlayer _audioPlayer;
        private bool _taskCompletedButStillSettling;

        public TaskStep(Task task, AudioPlayer audioPlayer) : base(task.TaskName)
        {
            Task = task;
            _audioPlayer = audioPlayer;
        }

        public Task Task { get; }

        protected override void OnEnter()
        {
            _taskCompletedButStillSettling = false;
            Task.Completed += OnTaskCompleted;
            Task.Prepare();
            Task.SetAudioPlayer(_audioPlayer);
            Task.StartTask();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            if (_taskCompletedButStillSettling && !Task.IsSettling)
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
                    Task.OnCompleteTask.Invoke();
                    break;
                case ExitReason.Skipped:
                    Task.Skip();
                    Task.OnCompleteTask.Invoke();
                    break;
                default:
                    Task.CancelTask();
                    break;
            }
        }

        private void OnTaskCompleted()
        {
            if (Task.IsSettling)
            {
                _taskCompletedButStillSettling = true;
                return;
            }

            Finish();
        }
    }
}
