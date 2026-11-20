using System;
using UnityEngine;

namespace Framework.Audio
{
    public interface IVoicePlayer
    {
        bool IsPlaying { get; }

        bool IsPlayingFor(object owner);

        void Play(AudioClip clip, object owner, Action onFinished = null);

        void Pause(object owner);

        void Resume(object owner);

        void Stop(object owner);

        void StopAll();
    }
}
