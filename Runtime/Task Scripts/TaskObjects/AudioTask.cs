using System.Collections;
using System.Collections.Generic;
using Framework.Audio;
using UnityEngine;

namespace Framework.Tasks
{
    public class AudioTask : Task
    {
        [SerializeField]
        private List<MyClip> _audioClips = new List<MyClip>();

        private Coroutine _playback;

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

            _playback = StartCoroutine(PlayClipsOneAfterAnother());
        }

        public override void Skip()
        {
            CancelTask();
            base.Skip();
        }

        public override void CancelTask()
        {
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }

            _audioPlayer?.Stop();
        }

        private IEnumerator PlayClipsOneAfterAnother()
        {
            foreach (var clip in _audioClips)
            {
                if (clip == null)
                {
                    continue;
                }

                _audioPlayer.LoadAndPlay(clip.clip);
                yield return new WaitWhile(() => _audioPlayer.IsPlaying());
            }

            _playback = null;
            TaskComplete();
        }
    }
}
