using System;
using UnityEngine;

namespace FruitTree
{
    public enum AudioChannel { Music, Nature, Effects }

    /// <summary>
    /// Player volume for music, nature (weather, birds, brook, ambience) and
    /// effects (picking, rustling leaves). Saved on the device.
    /// </summary>
    public static class AudioLevels
    {
        public const float Default = .8f;
        // The default level plays every sound at its designed loudness.
        private const float FullScale = 1f / Default;
        public static event Action Changed;

        private static string Key(AudioChannel channel) => "FruitTree.Volume." + channel;

        public static float Get(AudioChannel channel)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(Key(channel), Default));
        }

        public static void Set(AudioChannel channel, float level)
        {
            level = Mathf.Clamp01(level);
            if (Mathf.Approximately(level, Get(channel))) return;
            PlayerPrefs.SetFloat(Key(channel), level);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Multiplier for a source's designed volume; 1 at the default level.</summary>
        public static float Gain(AudioChannel channel) => GainFor(Get(channel));

        public static float GainFor(float level)
        {
            // Squared, so the slider feels even to the ear, from silence to 1.56x.
            float scaled = Mathf.Clamp01(level) * FullScale;
            return scaled * scaled;
        }
    }
}
