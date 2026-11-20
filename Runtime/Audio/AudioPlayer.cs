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
        private bool _hostInactive;

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

        public bool IsPlaying => Active != null && Active._channel.IsPlaying;

        public bool IsPlayingFor(object owner)
        {
            return Active != null && Active._channel.IsPlayingFor(owner);
        }

        public void Play(AudioClip clip, object owner, Action onFinished = null)
        {
            var player = Active;
            if (player == null)
            {
                return;
            }

            if (player != this)
            {
                player.Play(clip, owner, onFinished);
                return;
            }

            if (_hostInactive && !_audioSource.gameObject.activeInHierarchy)
            {
                return;
            }

            _hostInactive = false;
            _channel.Begin(owner, onFinished);
            _audioSource.Stop();
            if (!_audioSource.isActiveAndEnabled)
            {
                Debug.LogWarning("The audio source is inactive, so the clip is skipped.", _audioSource);
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
            var player = Active;
            if (player != null && player._channel.Pause(owner))
            {
                player._audioSource.Pause();
            }
        }

        public void Resume(object owner)
        {
            var player = Active;
            if (player != null && player._channel.Resume(owner))
            {
                player._audioSource.UnPause();
            }
        }

        public void Stop(object owner)
        {
            var player = Active;
            if (player != null && player._channel.Stop(owner))
            {
                player.StopSource();
            }
        }

        public void StopAll()
        {
            var player = Active;
            if (player != null && player._channel.StopAll())
            {
                player.StopSource();
            }
        }

        private static bool IsAppUnfocused => !Application.isBatchMode && !Application.isFocused;

        private AudioPlayer Active
        {
            get
            {
                if (_audioSource == null)
                {
                    return null;
                }

                if (_pump != null)
                {
                    return this;
                }

                return PlayerOfSource.TryGetValue(_audioSource, out var successor) && successor != this ? successor : null;
            }
        }

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

        internal void HostDeactivated()
        {
            _hostInactive = true;
            _channel.FinishEvenIfPaused();
            if (_pump != null)
            {
                _pump.enabled = false;
            }
        }

        internal void HostDestroyed(AudioPlayerPump pump)
        {
            if (ReferenceEquals(pump, _pump))
            {
                _channel.StopAll();
                StopSource();
            }
        }

        private void Supersede()
        {
            _channel.StopAll();
            StopSource();
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
