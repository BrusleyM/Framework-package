using System;
using System.Collections.Generic;

namespace Framework.Sequencing
{
    public sealed class ParallelStep : StepBase
    {
        private readonly ISequenceStep[] _children;
        private readonly CompletionJoin<int> _join = new CompletionJoin<int>();
        private readonly int[] _childIndexes;
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
            _join.AllFinished += Finish;
            _childIndexes = new int[children.Length];
            for (var index = 0; index < children.Length; index++)
            {
                _childIndexes[index] = index;
            }

            _childFinishedHandlers = new Action[children.Length];
            for (var index = 0; index < children.Length; index++)
            {
                var childIndex = index;
                _childFinishedHandlers[index] = () => OnChildFinished(childIndex);
            }
        }

        protected override void OnEnter()
        {
            _join.Begin(_childIndexes);
            _enteredChildCount = 0;

            for (var index = 0; index < _children.Length; index++)
            {
                _children[index].Finished += _childFinishedHandlers[index];
                _children[index].Failed += Fail;
            }

            for (var index = 0; index < _children.Length; index++)
            {
                _enteredChildCount = index + 1;
                _children[index].Enter();
            }

            _join.Release();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            for (var index = 0; index < _children.Length && IsWaiting; index++)
            {
                if (!_join.IsFinished(index))
                {
                    _children[index].Advance(deltaSeconds);
                }
            }
        }

        protected override void OnPause()
        {
            for (var index = 0; index < _enteredChildCount; index++)
            {
                _children[index].Pause();
            }
        }

        protected override void OnResume()
        {
            for (var index = 0; index < _enteredChildCount; index++)
            {
                _children[index].Resume();
            }
        }

        protected override void OnExit(ExitReason reason)
        {
            _join.Stop();
            for (var index = 0; index < _children.Length; index++)
            {
                _children[index].Finished -= _childFinishedHandlers[index];
                _children[index].Failed -= Fail;
            }

            var failures = new List<Exception>();
            for (var index = 0; index < _enteredChildCount; index++)
            {
                try
                {
                    _children[index].Exit(_join.IsFinished(index) ? ExitReason.Completed : reason);
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
            _join.Finish(childIndex);
        }
    }
}
