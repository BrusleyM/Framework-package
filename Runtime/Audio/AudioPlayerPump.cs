using UnityEngine;

namespace Framework.Audio
{
    internal sealed class AudioPlayerPump : MonoBehaviour
    {
        private AudioPlayer _player;

        public void Bind(AudioPlayer player)
        {
            _player = player;
            hideFlags = HideFlags.HideInInspector;
            enabled = false;
        }

        private void Update()
        {
            _player?.Tick();
        }
    }
}
