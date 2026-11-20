using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Framework.Sequencing.Tests
{
    internal static class AudioDevice
    {
        private static bool? _isAvailable;

        public static IEnumerator RequireOrIgnore()
        {
            if (_isAvailable == null)
            {
                var host = new GameObject("audio device probe");
                var source = host.AddComponent<AudioSource>();
                var clip = AudioClip.Create("probe", 44100, 1, 44100, false);
                clip.SetData(new float[44100], 0);
                source.clip = clip;
                source.Play();
                yield return null;
                yield return null;
                _isAvailable = source.isPlaying;
                Object.Destroy(host);
            }

            if (!_isAvailable.Value)
            {
                Assert.Ignore("No audio device is available here, so real playback cannot be tested. The same logic is tested with ManualVoicePlayer.");
            }
        }
    }
}
