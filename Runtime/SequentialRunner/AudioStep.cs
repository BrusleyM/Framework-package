using System.Collections.Generic;
using Framework.Audio;

namespace Framework.Sequencing
{
    public sealed class AudioStep : StepBase
    {
        private readonly List<MyClip> _clips = new List<MyClip>();
        private readonly IVoicePlayer _audioPlayer;
        private int _nextClipIndex;

        public AudioStep(string name, IEnumerable<MyClip> clips, IVoicePlayer audioPlayer) : base(name)
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
            PlayNextClipOrFinish();
        }

        protected override void OnPause()
        {
            _audioPlayer?.Pause(this);
        }

        protected override void OnResume()
        {
            _audioPlayer?.Resume(this);
        }

        protected override void OnExit(ExitReason reason)
        {
            if (reason != ExitReason.Completed)
            {
                _audioPlayer?.Stop(this);
            }
        }

        private void PlayNextClipOrFinish()
        {
            if (_audioPlayer == null || _nextClipIndex >= _clips.Count)
            {
                Finish();
                return;
            }

            _audioPlayer.Play(_clips[_nextClipIndex++].clip, this, PlayNextClipOrFinish);
        }
    }
}
