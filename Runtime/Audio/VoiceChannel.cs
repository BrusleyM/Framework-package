using System;

namespace Framework.Audio
{
    public sealed class VoiceChannel
    {
        private object _owner;
        private Action _onFinished;

        public bool IsPlaying => _owner != null;
        public bool IsPaused { get; private set; }
        public object Owner => _owner;

        public bool IsPlayingFor(object owner)
        {
            return _owner != null && ReferenceEquals(_owner, owner);
        }

        public void Begin(object owner, Action onFinished)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "Audio needs an owner so only the owner can stop it.");
            }

            _owner = owner;
            _onFinished = onFinished;
            IsPaused = false;
        }

        public bool Pause(object owner)
        {
            if (!IsPlayingFor(owner) || IsPaused)
            {
                return false;
            }

            IsPaused = true;
            return true;
        }

        public bool Resume(object owner)
        {
            if (!IsPlayingFor(owner) || !IsPaused)
            {
                return false;
            }

            IsPaused = false;
            return true;
        }

        public bool Stop(object owner)
        {
            if (!IsPlayingFor(owner))
            {
                return false;
            }

            Clear();
            return true;
        }

        public bool StopAll()
        {
            if (_owner == null)
            {
                return false;
            }

            Clear();
            return true;
        }

        public void Finish()
        {
            if (_owner == null || IsPaused)
            {
                return;
            }

            var finished = _onFinished;
            Clear();
            finished?.Invoke();
        }

        private void Clear()
        {
            _owner = null;
            _onFinished = null;
            IsPaused = false;
        }
    }
}
