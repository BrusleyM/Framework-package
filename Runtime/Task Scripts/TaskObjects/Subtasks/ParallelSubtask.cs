using UnityEngine;

namespace Framework.Tasks
{
    public class ParallelSubtask : GameObjectTask
    {
        [SerializeField]
        private GameObjectTask[] _subtasks = new GameObjectTask[0];

        private bool _isRunning;

        public override void Execute()
        {
            _isRunning = true;
            PrepareAndListenToEverySubtask();
            if (!CompleteWhenEverySubtaskIsComplete())
            {
                ExecuteEverySubtask();
            }
        }

        public override void Cancel()
        {
            _isRunning = false;
            StopListeningToEverySubtask();
            foreach (var subtask in _subtasks)
            {
                if (subtask != null)
                {
                    subtask.Cancel();
                }
            }
        }

        private void PrepareAndListenToEverySubtask()
        {
            foreach (var subtask in _subtasks)
            {
                if (subtask != null)
                {
                    subtask.Prepare();
                    subtask.Completed -= OnSubtaskCompleted;
                    subtask.Completed += OnSubtaskCompleted;
                }
            }
        }

        private void ExecuteEverySubtask()
        {
            foreach (var subtask in _subtasks)
            {
                if (subtask != null && _isRunning)
                {
                    subtask.Execute();
                }
            }
        }

        private void StopListeningToEverySubtask()
        {
            foreach (var subtask in _subtasks)
            {
                if (subtask != null)
                {
                    subtask.Completed -= OnSubtaskCompleted;
                }
            }
        }

        private void OnSubtaskCompleted()
        {
            CompleteWhenEverySubtaskIsComplete();
        }

        private bool CompleteWhenEverySubtaskIsComplete()
        {
            if (!_isRunning)
            {
                return false;
            }

            foreach (var subtask in _subtasks)
            {
                if (subtask != null && !subtask.IsComplete)
                {
                    return false;
                }
            }

            _isRunning = false;
            StopListeningToEverySubtask();
            MarkComplete();
            return true;
        }
    }
}
