using System.Collections.Generic;
using Framework.Audio;
using UnityEngine;

namespace Framework.Tasks
{
    public class AudioTask : Task, IPausableTask, IGuidanceOnlyTask
    {
        [SerializeField]
        private List<MyClip> _audioClips = new List<MyClip>();

        private int _nextClipIndex;

        public override void StartTask()
        {
            if (_audioClips.Count == 0)
            {
                Debug.LogWarning("No audio clips found for this AudioTask.", this);
                TaskComplete();
                return;
            }

            if (_audioPlayer == null)
            {
                Debug.LogError("AudioPlayer is not set for this AudioTask.", this);
                TaskComplete();
                return;
            }

            _nextClipIndex = 0;
            PlayNextClipOrComplete();
        }

        public void PauseTask()
        {
            _audioPlayer?.Pause(this);
        }

        public void ResumeTask()
        {
            _audioPlayer?.Resume(this);
        }

        public override void Skip()
        {
            CancelTask();
            base.Skip();
        }

        public override void CancelTask()
        {
            _audioPlayer?.Stop(this);
        }

        private void PlayNextClipOrComplete()
        {
            while (_nextClipIndex < _audioClips.Count)
            {
                var clip = _audioClips[_nextClipIndex++];
                if (clip != null && clip.clip != null)
                {
                    _audioPlayer.Play(clip.clip, this, PlayNextClipOrComplete);
                    return;
                }
            }

            TaskComplete();
        }
    }
}
