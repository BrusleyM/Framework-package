using System;
using UnityEngine;

namespace Framework.Audio
{
    public sealed class AudioPlayer : IVoicePlayer
    {
        private readonly AudioSource _audioSource;
        private readonly VoiceChannel _channel = new VoiceChannel();
        private readonly AudioPlayerPump _pump;

        public AudioPlayer(AudioSource audioSource)
        {
            _audioSource = audioSource;
            _pump = audioSource.gameObject.AddComponent<AudioPlayerPump>();
            _pump.Bind(this);
        }

        public bool IsPlaying => _channel.IsPlaying;

        public bool IsPlayingFor(object owner)
        {
            return _channel.IsPlayingFor(owner);
        }

        public void Play(AudioClip clip, object owner, Action onFinished = null)
        {
            _audioSource.Stop();
            _channel.Begin(owner, onFinished);
            _audioSource.clip = clip;
            if (clip != null)
            {
                _audioSource.Play();
            }

            _pump.enabled = true;
        }

        public void Pause(object owner)
        {
            if (_channel.Pause(owner))
            {
                _audioSource.Pause();
            }
        }

        public void Resume(object owner)
        {
            if (_channel.Resume(owner))
            {
                _audioSource.UnPause();
            }
        }

        public void Stop(object owner)
        {
            if (_channel.Stop(owner))
            {
                _audioSource.Stop();
                _pump.enabled = false;
            }
        }

        public void StopAll()
        {
            if (_channel.StopAll())
            {
                _audioSource.Stop();
                _pump.enabled = false;
            }
        }

        private static bool IsAppUnfocused => !Application.isBatchMode && !Application.isFocused;

        internal void Tick()
        {
            if (!_channel.IsPlaying || _channel.IsPaused || _audioSource.isPlaying || IsAppUnfocused)
            {
                return;
            }

            _channel.Finish();
            if (!_channel.IsPlaying)
            {
                _pump.enabled = false;
            }
        }
    }
}
