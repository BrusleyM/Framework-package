using System;
using System.Collections.Generic;

namespace Framework.Sequencing
{
    public sealed class ParallelStep : StepBase
    {
        private readonly ISequenceStep[] _children;
        private readonly bool[] _childFinished;
        private readonly Action[] _childFinishedHandlers;
        private int _enteredChildCount;

        public ParallelStep(string name, params ISequenceStep[] children) : base(name)
        {
            foreach (var child in children)
            {
                if (child == null)
                {
                    throw new ArgumentException("A parallel step cannot contain a null child.", nameof(children));
                }
            }

            _children = children;
            _childFinished = new bool[children.Length];
            _childFinishedHandlers = new Action[children.Length];
            for (var index = 0; index < children.Length; index++)
            {
                var childIndex = index;
                _childFinishedHandlers[index] = () => OnChildFinished(childIndex);
            }
        }

        protected override void OnEnter()
        {
            Array.Clear(_childFinished, 0, _childFinished.Length);
            _enteredChildCount = 0;

            for (var index = 0; index < _children.Length; index++)
            {
                _children[index].Finished += _childFinishedHandlers[index];
            }

            for (var index = 0; index < _children.Length; index++)
            {
                _enteredChildCount = index + 1;
                _children[index].Enter();
            }

            FinishWhenEveryChildFinished();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            for (var index = 0; index < _children.Length && IsWaiting; index++)
            {
                if (!_childFinished[index])
                {
                    _children[index].Advance(deltaSeconds);
                }
            }
        }

        protected override void OnExit(ExitReason reason)
        {
            for (var index = 0; index < _children.Length; index++)
            {
                _children[index].Finished -= _childFinishedHandlers[index];
            }

            var failures = new List<Exception>();
            for (var index = 0; index < _enteredChildCount; index++)
            {
                try
                {
                    _children[index].Exit(_childFinished[index] ? ExitReason.Completed : reason);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            }

            if (failures.Count > 0)
            {
                throw failures.Count == 1 ? failures[0] : new AggregateException(failures);
            }
        }

        private void OnChildFinished(int childIndex)
        {
            _childFinished[childIndex] = true;
            FinishWhenEveryChildFinished();
        }

        private void FinishWhenEveryChildFinished()
        {
            if (_enteredChildCount < _children.Length)
            {
                return;
            }

            foreach (var hasFinished in _childFinished)
            {
                if (!hasFinished)
                {
                    return;
                }
            }

            Finish();
        }
    }
}
