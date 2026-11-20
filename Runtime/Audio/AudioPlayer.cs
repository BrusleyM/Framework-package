using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Framework.Audio
{
    public sealed class AudioPlayer : IVoicePlayer
    {
        private static readonly ConditionalWeakTable<AudioSource, AudioPlayer> PlayerOfSource = new ConditionalWeakTable<AudioSource, AudioPlayer>();

        private readonly AudioSource _audioSource;
        private readonly VoiceChannel _channel = new VoiceChannel();
        private AudioPlayerPump _pump;

        public AudioPlayer(AudioSource audioSource)
        {
            _audioSource = audioSource;
            if (PlayerOfSource.TryGetValue(audioSource, out var previous))
            {
                previous.Supersede();
                PlayerOfSource.Remove(audioSource);
            }

            PlayerOfSource.Add(audioSource, this);
            _pump = audioSource.gameObject.AddComponent<AudioPlayerPump>();
            _pump.Bind(this);
        }

        public bool IsPlaying => _pump != null && _channel.IsPlaying;

        public bool IsPlayingFor(object owner)
        {
            return _pump != null && _channel.IsPlayingFor(owner);
        }

        public void Play(AudioClip clip, object owner, Action onFinished = null)
        {
            if (_pump == null)
            {
                return;
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            _channel.Begin(owner, onFinished);
            if (_audioSource == null || !_audioSource.isActiveAndEnabled)
            {
                Debug.LogWarning("The audio source is missing or inactive, so the clip is skipped.", _audioSource);
                _channel.Finish();
                return;
            }

            _audioSource.clip = clip;
            if (clip != null)
            {
                _audioSource.Play();
            }

            _pump.enabled = true;
        }

        public void Pause(object owner)
        {
            if (_pump != null && _channel.Pause(owner) && _audioSource != null)
            {
                _audioSource.Pause();
            }
        }

        public void Resume(object owner)
        {
            if (_pump != null && _channel.Resume(owner) && _audioSource != null)
            {
                _audioSource.UnPause();
            }
        }

        public void Stop(object owner)
        {
            if (_pump != null && _channel.Stop(owner))
            {
                StopSource();
            }
        }

        public void StopAll()
        {
            if (_pump != null && _channel.StopAll())
            {
                StopSource();
            }
        }

        private static bool IsAppUnfocused => !Application.isBatchMode && !Application.isFocused;

        internal void Tick()
        {
            if (!_channel.IsPlaying || _channel.IsPaused || IsAppUnfocused)
            {
                return;
            }

            if (_audioSource != null && _audioSource.isPlaying)
            {
                return;
            }

            try
            {
                _channel.Finish();
            }
            finally
            {
                if (!_channel.IsPlaying && _pump != null)
                {
                    _pump.enabled = false;
                }
            }
        }

        private void Supersede()
        {
            _channel.StopAll();
            if (_pump != null)
            {
                UnityEngine.Object.Destroy(_pump);
                _pump = null;
            }
        }

        private void StopSource()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            if (_pump != null)
            {
                _pump.enabled = false;
            }
        }
    }
}
