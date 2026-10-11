using System;
using UnityEngine;

namespace FruitTree
{
    /// <summary>Small generated placeholder SFX, entirely offline and copyright free.</summary>
    public sealed class OrchardAudio : MonoBehaviour
    {
        public TreeGenerator tree;
        [Header("Optional replacement recordings (licensed audio only)")]
        public AudioClip ambientRecording;
        public AudioClip pickRecording;
        public UnityEngine.UI.Text soundStatusText;
        private AudioSource ambience, effects;
        private AudioClip collect;
        public bool IsMuted => muted;
        private bool muted;
        private bool ownsAmbientClip;
        private bool ownsPickClip;
        private void Awake()
        {
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0;
            ambience.volume = .18f;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0;
            effects.volume = .4f;
            ownsAmbientClip = ambientRecording == null;
            ownsPickClip = pickRecording == null;
            ambience.clip = ownsAmbientClip ? CreateAmbient() : ambientRecording;
            collect = ownsPickClip ? CreateCollect() : pickRecording;
            muted = PlayerPrefs.GetInt("FruitTree.Muted", 0) == 1;
            ApplyVolume();
        }

        private void Start()
        {
            if (tree != null) tree.HarvestChanged += OnHarvest;
            ambience.Play();
            UpdateStatusText();
        }

        private void OnDestroy()
        {
            if (tree != null) tree.HarvestChanged -= OnHarvest;
            if (ownsPickClip && collect != null) Destroy(collect);
            if (ownsAmbientClip && ambience != null && ambience.clip != null) Destroy(ambience.clip);
        }

        private int lastHarvested;
        private void OnHarvest(int current, int total)
        {
            if (current == 0) { lastHarvested = 0; return; }
            if (current > lastHarvested && !muted) effects.PlayOneShot(collect);
            lastHarvested = current;
        }

        public void ToggleSound()
        {
            muted = !muted;
            PlayerPrefs.SetInt("FruitTree.Muted", muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolume();
            UpdateStatusText();
        }
        private void UpdateStatusText()
        {
            if (soundStatusText != null) soundStatusText.text = muted ? "SOUND OFF" : "SOUND ON";
        }
        private void ApplyVolume()
        {
            ambience.mute = muted;
            effects.mute = muted;
        }

        private static AudioClip CreateCollect()
        {
            // A short dry twig snap and soft resonant fruit pluck, deliberately
            // avoiding the arcade-like electronic "coin" sound.
            const int sampleRate = 22050;
            const float duration = .23f;
            float[] data = new float[(int)(sampleRate * duration)];
            var random = new System.Random(71);
            float lowNoise = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sampleRate;
                lowNoise += .1f * ((float)random.NextDouble() * 2f - 1f - lowNoise);
                float snap = lowNoise * Mathf.Exp(-t * 72f) * .4f;
                float lowPop = Mathf.Sin(2f * Mathf.PI * (230f * t - 120f * t * t)) *
                    Mathf.Exp(-t * 24f) * .23f;
                float faintResonance = Mathf.Sin(2f * Mathf.PI * 140f * t) *
                    Mathf.Exp(-t * 27f) * .055f;
                float fade = Mathf.Min(1f, t * 300f, (duration - t) * 50f);
                data[i] = (snap + lowPop + faintResonance) * Mathf.Clamp01(fade);
            }
            return MakeClip("Soft fruit pluck", sampleRate, data);
        }

        private static AudioClip CreateAmbient()
        {
            const int rate = 22050;
            const int count = rate * 8;
            var random = new System.Random(305);
            float[] data = new float[count];
            float wind = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                wind = Mathf.Lerp(wind, (float)random.NextDouble() * 2f - 1f, .001f);
                float birds = 0f;
                for (int j = 0; j < 3; j++)
                {
                    float start = 1f + j * 2.2f;
                    float tau = t - start;
                    if (tau > 0 && tau < .42f)
                    {
                        float e = Mathf.Sin(Mathf.PI * tau / .42f);
                        birds += .10f * e * Mathf.Sin(2 * Mathf.PI * (1250f * tau + 650f * tau * tau));
                    }
                }
                float fade = Mathf.Min(1f, t * 2f, (8f - t) * 2f);
                data[i] = (.1f * wind + birds) * Mathf.Clamp01(fade);
            }
            return MakeClip("Quiet wind and birds", rate, data);
        }

        private static AudioClip MakeClip(string title, int rate, float[] data)
        {
            var clip = AudioClip.Create(title, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
