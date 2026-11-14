using System;

namespace Framework.Sequencing
{
    public abstract class StepBase : ISequenceStep
    {
        private bool _isActive;
        private bool _hasFinished;

        protected StepBase(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public event Action Finished;

        public void Enter()
        {
            _isActive = true;
            _hasFinished = false;
            OnEnter();
        }

        public void Advance(float deltaSeconds)
        {
            if (_isActive && !_hasFinished)
            {
                OnAdvance(deltaSeconds);
            }
        }

        public void Exit(ExitReason reason)
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            OnExit(reason);
        }

        protected bool IsWaiting => _isActive && !_hasFinished;

        protected void Finish()
        {
            if (!_isActive || _hasFinished)
            {
                return;
            }

            _hasFinished = true;
            Finished?.Invoke();
        }

        protected virtual void OnEnter() { }

        protected virtual void OnAdvance(float deltaSeconds) { }

        protected virtual void OnExit(ExitReason reason) { }
    }
}
