namespace Framework.Tasks
{
    public sealed class SubtaskSequencer
    {
        private readonly GameObjectTask[] _subtasks;
        private SubtaskFilter _filter;
        private int _index;
        private bool _isAdvancing;
        private bool _advanceRequested;

        public SubtaskSequencer(GameObjectTask[] subtasks)
        {
            _subtasks = subtasks ?? new GameObjectTask[0];
        }

        public GameObjectTask Current { get; private set; }

        public void Start(SubtaskFilter filter)
        {
            _filter = filter;
            _index = -1;
            Advance();
        }

        public void RunSetup()
        {
            foreach (var subtask in _subtasks)
            {
                if (subtask != null && subtask.IsSetup)
                {
                    subtask.Prepare();
                    subtask.Execute();
                }
            }
        }

        public void Stop()
        {
            if (Current != null)
            {
                Current.Completed -= Advance;
            }

            _advanceRequested = false;
        }

        public void TearDown(SubtaskFilter filter)
        {
            for (var index = _subtasks.Length - 1; index >= 0; index--)
            {
                var subtask = _subtasks[index];
                if (subtask != null && !(filter == SubtaskFilter.GuidanceOnly && subtask.IsSetup))
                {
                    subtask.Cancel();
                }
            }
        }

        private void Advance()
        {
            if (_isAdvancing)
            {
                _advanceRequested = true;
                return;
            }

            _isAdvancing = true;
            try
            {
                do
                {
                    _advanceRequested = false;
                    StartNextRunnableSubtask();
                }
                while (_advanceRequested);
            }
            finally
            {
                _isAdvancing = false;
                _advanceRequested = false;
            }
        }

        private void StartNextRunnableSubtask()
        {
            Stop();
            do
            {
                _index++;
            }
            while (_index < _subtasks.Length && !IsRunnable(_subtasks[_index]));

            if (_index >= _subtasks.Length)
            {
                Current = null;
                return;
            }

            Current = _subtasks[_index];
            Current.Prepare();
            Current.Completed += Advance;
            Current.Execute();
        }

        private bool IsRunnable(GameObjectTask subtask)
        {
            return subtask != null && !(_filter == SubtaskFilter.GuidanceOnly && subtask.IsSetup);
        }
    }
}
