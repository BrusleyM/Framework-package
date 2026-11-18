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
        private bool _isExecuting;
        private bool _isCancelling;

        public override void Execute()
        {
            if (_isExecuting)
            {
                Debug.LogError($"{name} is already executing, so a parallel subtask cannot contain itself.", this);
                return;
            }

            _isExecuting = true;
            try
            {
                ExecuteGroup();
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private void ExecuteGroup()
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
                if (_isRunning)
                {
                    Cancel();
                }

                throw;
            }
        }

        public override void Cancel()
        {
            if (_isCancelling)
            {
                return;
            }

            _isCancelling = true;
            try
            {
                CancelGroup();
            }
            finally
            {
                _isCancelling = false;
            }
        }

        private void CancelGroup()
        {
            _isRunning = false;
            StopListeningToEverySubtask();
            var everySubtask = new List<GameObjectTask>();
            CollectDistinctSubtasksInto(everySubtask);
            for (var index = everySubtask.Count - 1; index >= 0; index--)
            {
                everySubtask[index].Cancel();
            }
            _runningSubtasks.Clear();
        }

        private void OnValidate()
        {
            for (var index = 0; index < _subtasks.Length; index++)
            {
                if (_subtasks[index] != null && ContainsThisGroup(_subtasks[index], new HashSet<GameObjectTask>()))
                {
                    Debug.LogError("A parallel subtask cannot contain itself, directly or through another group.", this);
                    _subtasks[index] = null;
                }
            }
        }

        private bool ContainsThisGroup(GameObjectTask candidate, HashSet<GameObjectTask> visited)
        {
            if (candidate == this)
            {
                return true;
            }

            var group = candidate as ParallelSubtask;
            if (group == null || !visited.Add(group))
            {
                return false;
            }

            foreach (var child in group._subtasks)
            {
                if (child != null && ContainsThisGroup(child, visited))
                {
                    return true;
                }
            }

            return false;
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
