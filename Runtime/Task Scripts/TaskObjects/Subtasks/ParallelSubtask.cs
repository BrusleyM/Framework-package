using System.Collections.Generic;
using Framework.Sequencing;
using UnityEngine;

namespace Framework.Tasks
{
    public class ParallelSubtask : GameObjectTask
    {
        [SerializeField]
        private GameObjectTask[] _subtasks = new GameObjectTask[0];

        private readonly List<GameObjectTask> _runningSubtasks = new List<GameObjectTask>();
        private readonly CompletionJoin<GameObjectTask> _join = new CompletionJoin<GameObjectTask>();
        private bool _isRunning;
        private bool _isExecuting;
        private bool _isCancelling;
        private bool _isJoinWired;

        public override void Execute()
        {
            if (_isExecuting)
            {
                Debug.LogError($"{name} is already executing, so a parallel subtask cannot contain itself.", this);
                return;
            }

            WireTheJoin();
            _isExecuting = true;
            try
            {
                ExecuteGroup();
            }
            finally
            {
                _isExecuting = false;
                _join.Release();
            }
        }

        private void ExecuteGroup()
        {
            _isRunning = true;
            CollectDistinctSubtasksInto(_runningSubtasks);
            _join.Begin(_runningSubtasks);
            PrepareAndListenToEverySubtask();
            if (_join.HasEveryMemberFinished)
            {
                return;
            }

            try
            {
                ExecuteEverySubtask();
            }
            catch
            {
                if (_isRunning && !_join.HasEveryMemberFinished)
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

        private void WireTheJoin()
        {
            if (_isJoinWired)
            {
                return;
            }

            _isJoinWired = true;
            _join.AllFinished += OnEverySubtaskFinished;
        }

        private void OnEverySubtaskFinished()
        {
            _isRunning = false;
            StopListeningToEverySubtask();
            MarkComplete();
        }

        private void CancelGroup()
        {
            _isRunning = false;
            _join.Stop();
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
                if (subtask.IsComplete)
                {
                    _join.Finish(subtask);
                }
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
            _join.Hold();
            try
            {
                foreach (var subtask in _runningSubtasks.ToArray())
                {
                    if (subtask.IsComplete)
                    {
                        _join.Finish(subtask);
                    }
                }
            }
            finally
            {
                _join.Release();
            }
        }
    }
}
