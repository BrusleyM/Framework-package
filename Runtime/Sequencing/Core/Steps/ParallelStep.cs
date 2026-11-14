using System;

namespace Framework.Sequencing
{
    public sealed class ParallelStep : StepBase
    {
        private readonly ISequenceStep[] _children;
        private readonly bool[] _childFinished;
        private readonly Action[] _childFinishedHandlers;

        public ParallelStep(string name, params ISequenceStep[] children) : base(name)
        {
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

            for (var index = 0; index < _children.Length; index++)
            {
                _children[index].Finished += _childFinishedHandlers[index];
            }

            for (var index = 0; index < _children.Length; index++)
            {
                _children[index].Enter();
            }

            FinishWhenEveryChildFinished();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            for (var index = 0; index < _children.Length; index++)
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
                _children[index].Exit(_childFinished[index] ? ExitReason.Completed : reason);
            }
        }

        private void OnChildFinished(int childIndex)
        {
            _childFinished[childIndex] = true;
            FinishWhenEveryChildFinished();
        }

        private void FinishWhenEveryChildFinished()
        {
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
