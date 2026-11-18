using System.Collections.Generic;
using UnityEngine;

namespace Framework.Tasks
{
    public class ParallelSubtask : GameObjectTask
    {
        [SerializeField]
        private GameObjectTask[] _subtasks = new GameObjectTask[0];

        private readonly List<GameObjectTask> _runningSubtasks = new List<GameObjectTask>();
        private bool _isRunning;

        public override void Execute()
        {
            _isRunning = true;
            CollectDistinctSubtasksInto(_runningSubtasks);
            PrepareAndListenToEverySubtask();
            if (CompleteWhenEverySubtaskIsComplete())
            {
                return;
            }

            try
            {
                ExecuteEverySubtask();
            }
            catch
            {
                Cancel();
                throw;
            }
        }

        public override void Cancel()
        {
            _isRunning = false;
            StopListeningToEverySubtask();
            var everySubtask = new List<GameObjectTask>();
            CollectDistinctSubtasksInto(everySubtask);
            foreach (var subtask in everySubtask)
            {
                subtask.Cancel();
            }
            _runningSubtasks.Clear();
        }

        private void CollectDistinctSubtasksInto(List<GameObjectTask> destination)
        {
            destination.Clear();
            foreach (var subtask in _subtasks)
            {
                if (subtask != null && !destination.Contains(subtask))
                {
                    destination.Add(subtask);
                }
            }
        }

        private void PrepareAndListenToEverySubtask()
        {
            foreach (var subtask in _runningSubtasks)
            {
                subtask.Prepare();
                subtask.Completed -= OnSubtaskCompleted;
                subtask.Completed += OnSubtaskCompleted;
            }
        }

        private void ExecuteEverySubtask()
        {
            foreach (var subtask in _runningSubtasks.ToArray())
            {
                if (_isRunning)
                {
                    subtask.Execute();
                }
            }
        }

        private void StopListeningToEverySubtask()
        {
            foreach (var subtask in _runningSubtasks)
            {
                subtask.Completed -= OnSubtaskCompleted;
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

            foreach (var subtask in _runningSubtasks)
            {
                if (!subtask.IsComplete)
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
