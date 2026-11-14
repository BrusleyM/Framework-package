using System;
using System.Collections.Generic;

namespace Framework.Sequencing.Tests
{
    internal sealed class RecordingStep : StepBase
    {
        private readonly List<string> _journal;
        private readonly bool _finishesWhenEntered;

        public RecordingStep(string name, List<string> journal, bool finishesWhenEntered = false) : base(name)
        {
            _journal = journal;
            _finishesWhenEntered = finishesWhenEntered;
        }

        public float AdvancedSeconds { get; private set; }
        public ExitReason? LastExitReason { get; private set; }

        public void FinishNow() => Finish();

        protected override void OnEnter()
        {
            _journal.Add($"enter {Name}");
            if (_finishesWhenEntered)
            {
                Finish();
            }
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            AdvancedSeconds += deltaSeconds;
        }

        protected override void OnExit(ExitReason reason)
        {
            LastExitReason = reason;
            _journal.Add($"exit {Name} {reason}");
        }
    }

    internal sealed class ThrowingStep : StepBase
    {
        private readonly bool _throwsOnEnter;
        private readonly bool _throwsOnExit;

        public ThrowingStep(string name, bool throwsOnEnter, bool throwsOnExit = false) : base(name)
        {
            _throwsOnEnter = throwsOnEnter;
            _throwsOnExit = throwsOnExit;
        }

        public void FinishNow() => Finish();

        protected override void OnExit(ExitReason reason)
        {
            if (_throwsOnExit)
            {
                throw new InvalidOperationException("step could not clean up");
            }
        }

        protected override void OnEnter()
        {
            if (_throwsOnEnter)
            {
                throw new InvalidOperationException("step could not start");
            }
        }
    }

    internal sealed class ManualCondition : IStepCondition
    {
        public event Action Satisfied;
        public int SubscriberCount => Satisfied?.GetInvocationList().Length ?? 0;
        public bool IsArmed { get; private set; }
        public bool SatisfiedImmediately { get; set; }

        public void Arm()
        {
            IsArmed = true;
            if (SatisfiedImmediately)
            {
                Satisfied?.Invoke();
            }
        }

        public void Disarm() => IsArmed = false;

        public void Satisfy() => Satisfied?.Invoke();
    }
}
