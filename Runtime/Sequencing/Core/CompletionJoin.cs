using System;
using System.Collections.Generic;

namespace Framework.Sequencing
{
    public sealed class CompletionJoin<T>
    {
        private readonly HashSet<T> _members = new HashSet<T>();
        private readonly HashSet<T> _finished = new HashSet<T>();
        private bool _isActive;
        private int _holdCount;

        public event Action AllFinished;

        public bool IsActive => _isActive;
        public int MemberCount => _members.Count;
        public int FinishedCount => _finished.Count;
        public bool HasEveryMemberFinished => _finished.Count == _members.Count;

        public void Begin(IEnumerable<T> members)
        {
            _members.Clear();
            _finished.Clear();
            foreach (var member in members)
            {
                _members.Add(member);
            }

            _isActive = true;
            _holdCount = 1;
        }

        public bool Finish(T member)
        {
            if (!_isActive || !_members.Contains(member) || !_finished.Add(member))
            {
                return false;
            }

            RaiseWhenEveryMemberHasFinished();
            return true;
        }

        public bool IsFinished(T member)
        {
            return _finished.Contains(member);
        }

        public void Hold()
        {
            if (_isActive)
            {
                _holdCount++;
            }
        }

        public void Release()
        {
            if (!_isActive || _holdCount == 0)
            {
                return;
            }

            _holdCount--;
            RaiseWhenEveryMemberHasFinished();
        }

        public void Stop()
        {
            _isActive = false;
        }

        private void RaiseWhenEveryMemberHasFinished()
        {
            if (_holdCount > 0 || _finished.Count < _members.Count)
            {
                return;
            }

            _isActive = false;
            AllFinished?.Invoke();
        }
    }
}
