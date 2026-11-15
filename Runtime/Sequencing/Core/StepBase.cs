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

        public event Action<Exception> Failed;

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

        public void Pause()
        {
            if (_isActive)
            {
                OnPause();
            }
        }

        public void Resume()
        {
            if (_isActive)
            {
                OnResume();
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

        protected void Fail(Exception failure)
        {
            if (_isActive && !_hasFinished)
            {
                Failed?.Invoke(failure);
            }
        }

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

        protected virtual void OnPause() { }

        protected virtual void OnResume() { }

        protected virtual void OnExit(ExitReason reason) { }
    }
}
