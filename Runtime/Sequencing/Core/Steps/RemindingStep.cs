using System;

namespace Framework.Sequencing
{
    public sealed class RemindingStep : StepBase
    {
        private readonly ISequenceStep _inner;
        private readonly float _intervalSeconds;
        private readonly Action _remind;
        private float _secondsSinceLastReminder;

        public RemindingStep(ISequenceStep inner, float intervalSeconds, Action remind) : base(NameOf(inner))
        {
            _inner = inner;
            _intervalSeconds = intervalSeconds;
            _remind = remind;
        }

        protected override void OnEnter()
        {
            _secondsSinceLastReminder = 0f;
            _inner.Finished += Finish;
            _inner.Enter();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            _inner.Advance(deltaSeconds);
            if (!IsWaiting || _intervalSeconds <= 0f)
            {
                return;
            }

            _secondsSinceLastReminder += deltaSeconds;
            if (_secondsSinceLastReminder >= _intervalSeconds)
            {
                _secondsSinceLastReminder %= _intervalSeconds;
                _remind?.Invoke();
            }
        }

        protected override void OnExit(ExitReason reason)
        {
            _inner.Finished -= Finish;
            _inner.Exit(reason);
        }

        private static string NameOf(ISequenceStep inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            return inner.Name;
        }
    }
}
