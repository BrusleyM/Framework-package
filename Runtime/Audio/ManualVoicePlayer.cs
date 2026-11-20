using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Audio
{
    public sealed class ManualVoicePlayer : IVoicePlayer
    {
        private readonly VoiceChannel _channel = new VoiceChannel();
        private readonly List<AudioClip> _played = new List<AudioClip>();

        public bool IsPlaying => _channel.IsPlaying;
        public bool IsPaused => _channel.IsPaused;
        public object CurrentOwner => _channel.Owner;
        public AudioClip CurrentClip { get; private set; }
        public IReadOnlyList<AudioClip> Played => _played;

        public bool IsPlayingFor(object owner)
        {
            return _channel.IsPlayingFor(owner);
        }

        public void Play(AudioClip clip, object owner, Action onFinished = null)
        {
            _channel.Begin(owner, onFinished);
            CurrentClip = clip;
            _played.Add(clip);
        }

        public void Pause(object owner)
        {
            _channel.Pause(owner);
        }

        public void Resume(object owner)
        {
            _channel.Resume(owner);
        }

        public void Stop(object owner)
        {
            _channel.Stop(owner);
        }

        public void StopAll()
        {
            _channel.StopAll();
        }

        public void FinishCurrent()
        {
            _channel.Finish();
        }
    }
}
