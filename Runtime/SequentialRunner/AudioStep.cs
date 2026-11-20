using System.Collections.Generic;
using Framework.Audio;

namespace Framework.Sequencing
{
    public sealed class AudioStep : StepBase
    {
        private readonly List<MyClip> _clips = new List<MyClip>();
        private readonly AudioPlayer _audioPlayer;
        private int _nextClipIndex;
        private bool _isPaused;

        public AudioStep(string name, IEnumerable<MyClip> clips, AudioPlayer audioPlayer) : base(name)
        {
            foreach (var clip in clips)
            {
                if (clip != null && clip.clip != null)
                {
                    _clips.Add(clip);
                }
            }

            _audioPlayer = audioPlayer;
        }

        protected override void OnEnter()
        {
            _nextClipIndex = 0;
            _isPaused = false;
            PlayNextClip();
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            if (_isPaused || (_audioPlayer?.IsPlaying() ?? false))
            {
                return;
            }

            if (_nextClipIndex < _clips.Count)
            {
                PlayNextClip();
            }
            else
            {
                Finish();
            }
        }

        protected override void OnPause()
        {
            _isPaused = true;
            _audioPlayer?.Pause();
        }

        protected override void OnResume()
        {
            _isPaused = false;
            _audioPlayer?.Resume();
        }

        protected override void OnExit(ExitReason reason)
        {
            if (reason != ExitReason.Completed)
            {
                _audioPlayer?.Stop();
            }
        }

        private void PlayNextClip()
        {
            if (_audioPlayer != null && _nextClipIndex < _clips.Count)
            {
                _audioPlayer.LoadAndPlay(_clips[_nextClipIndex].clip);
            }

            _nextClipIndex++;
        }
    }
}
