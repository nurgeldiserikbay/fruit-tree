using System;
using System.Collections;
using UnityEngine;

namespace FruitTree
{
    /// <summary>Small generated placeholder SFX, entirely offline and copyright free.</summary>
    public sealed class OrchardAudio : MonoBehaviour
    {
        public TreeGenerator tree;
        public EnvironmentController environment;
        [Header("Optional replacement recordings (licensed audio only)")]
        public AudioClip ambientRecording;
        [Header("Optional licensed ambient music, one track per scenery")]
        public AudioClip[] themeMusic = new AudioClip[EnvironmentThemes.Count];
        private AudioSource music;
        private bool musicMuted;
        public bool IsMusicMuted => musicMuted;
        public bool HasMusicForCurrentTheme
        {
            get
            {
                int index = (int)activeTheme;
                return themeMusic != null && index >= 0 &&
                    index < themeMusic.Length && themeMusic[index] != null;
            }
        }
        public AudioClip pickRecording;
        public UnityEngine.UI.Text soundStatusText;
        private AudioSource ambience, alternateAmbience, effects;
        private readonly AudioClip[] generatedThemeClips = new AudioClip[EnvironmentThemes.Count];
        private Coroutine fadeRoutine;
        private AudioSource fadingFrom, fadingTo;
        private float fadeProgress = 1f;
        private bool usingAlternate;
        private EnvironmentTheme activeTheme = EnvironmentTheme.Meadow;
        private AudioClip collect;
        public bool IsMuted => muted;
        public bool IsQuietAudio { get; private set; }
        public EnvironmentTheme CurrentAmbientTheme => activeTheme;
        public int CachedThemeClipCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < generatedThemeClips.Length; i++)
                    if (generatedThemeClips[i] != null) count++;
                return count;
            }
        }
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
            alternateAmbience = gameObject.AddComponent<AudioSource>();
            alternateAmbience.loop = true;
            alternateAmbience.playOnAwake = false;
            alternateAmbience.spatialBlend = 0f;
            alternateAmbience.volume = 0f;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = .045f;
            musicMuted = PlayerPrefs.GetInt("FruitTree.MusicMuted", 0) == 1;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0;
            effects.volume = .4f;
            ownsAmbientClip = ambientRecording == null;
            ownsPickClip = pickRecording == null;
            ambience.clip = ownsAmbientClip ? ThemeClip(EnvironmentTheme.Meadow) : ambientRecording;
            collect = ownsPickClip ? CreateCollect() : pickRecording;
            muted = PlayerPrefs.GetInt("FruitTree.Muted", 0) == 1;
            IsQuietAudio = PlayerPrefs.GetInt("FruitTree.QuietAudio", 1) == 1;
            ApplyVolume();
        }

        private void Start()
        {
            if (tree != null) tree.FruitPicked += OnFruitPicked;
            if (environment != null)
                environment.ThemeChanged += OnThemeChanged;
            EnvironmentTheme initialTheme = environment != null ?
                environment.CurrentTheme : EnvironmentTheme.Meadow;
            if (ownsAmbientClip)
                SelectTheme(initialTheme, true);
            else
            {
                activeTheme = initialTheme;
                ambience.Play();
            }
            SelectMusic(initialTheme);
            UpdateStatusText();
        }

        private void OnDestroy()
        {
            if (tree != null) tree.FruitPicked -= OnFruitPicked;
            if (environment != null) environment.ThemeChanged -= OnThemeChanged;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            if (ownsPickClip && collect != null) Destroy(collect);
            if (ownsAmbientClip)
                for (int i = 0; i < generatedThemeClips.Length; i++)
                    if (generatedThemeClips[i] != null) Destroy(generatedThemeClips[i]);
        }

        private void OnFruitPicked(Apple fruit)
        {
            // Restore/scene switching can update the HUD count without a real tap.
            // Only a successful player pick has a quiet, tactile SFX.
            if (fruit != null && !muted && collect != null) effects.PlayOneShot(collect);
        }

        public void ToggleSound()
        {
            muted = !muted;
            PlayerPrefs.SetInt("FruitTree.Muted", muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolume();
            // A scenery change while globally muted may have stopped the
            // current music track. Resume it when SOUND returns to ON.
            SelectMusic(activeTheme);
            UpdateStatusText();
        }
        public void ToggleMusic()
        {
            musicMuted = !musicMuted;
            PlayerPrefs.SetInt("FruitTree.MusicMuted", musicMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolume();
            SelectMusic(activeTheme);
        }

        private void SelectMusic(EnvironmentTheme theme)
        {
            if (music == null) return;
            int index = (int)theme;
            AudioClip next = themeMusic != null && index >= 0 &&
                index < themeMusic.Length ? themeMusic[index] : null;
            if (music.clip != next)
            {
                music.Stop();
                music.clip = next;
            }
            if (next != null && !musicMuted && !muted && !music.isPlaying)
                music.Play();
            else if ((musicMuted || muted || next == null) && music.isPlaying)
                music.Stop();
        }

        public void ToggleQuietAudio()
        {
            IsQuietAudio = !IsQuietAudio;
            PlayerPrefs.SetInt("FruitTree.QuietAudio", IsQuietAudio ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolume();
        }

        private void UpdateStatusText()
        {
            if (soundStatusText != null) soundStatusText.text = muted ? "SOUND OFF" : "SOUND ON";
        }
        private void ApplyVolume()
        {
            ambience.mute = muted;
            alternateAmbience.mute = muted;
            effects.mute = muted;
            music.mute = muted || musicMuted;
            music.volume = IsQuietAudio ? .035f : .07f;
            float target = IsQuietAudio ? .09f : .18f;
            if (fadingFrom != null && fadingTo != null)
            {
                fadingFrom.volume = target * (1f - fadeProgress);
                fadingTo.volume = target * fadeProgress;
            }
            else
            {
                ambience.volume = usingAlternate ? 0f : target;
                alternateAmbience.volume = usingAlternate ? target : 0f;
            }
            effects.volume = IsQuietAudio ? .22f : .4f;
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

        private AudioClip ThemeClip(EnvironmentTheme theme)
        {
            int index = (int)theme;
            if (!EnvironmentThemes.IsValid(index))
                throw new ArgumentOutOfRangeException(nameof(theme));
            if (generatedThemeClips[index] == null)
                generatedThemeClips[index] = CreateAmbient(theme);
            return generatedThemeClips[index];
        }

        private void OnThemeChanged(EnvironmentTheme theme)
        {
            if (ownsAmbientClip) SelectTheme(theme, false);
            activeTheme = theme;
            SelectMusic(theme);
        }

        /// <summary>
        /// Audio cross-fade only runs during scenery changes, not every frame.
        /// A licensed recording, if supplied, is always used unchanged.
        /// </summary>
        private void SelectTheme(EnvironmentTheme theme, bool immediate)
        {
            if (!ownsAmbientClip || !EnvironmentThemes.IsValid((int)theme))
                return;
            if (!immediate && theme == activeTheme && fadingFrom == null) return;
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
                if (fadingFrom != null) fadingFrom.Stop();
                fadingFrom = null;
                fadingTo = null;
                fadeProgress = 1f;
            }
            AudioClip clip = ThemeClip(theme);
            activeTheme = theme;
            if (immediate)
            {
                ambience.Stop();
                alternateAmbience.Stop();
                ambience.clip = clip;
                ambience.Play();
                usingAlternate = false;
                ApplyVolume();
                return;
            }

            var previous = usingAlternate ? alternateAmbience : ambience;
            var next = usingAlternate ? ambience : alternateAmbience;
            next.Stop();
            next.clip = clip;
            next.Play();
            usingAlternate = !usingAlternate;
            fadingFrom = previous;
            fadingTo = next;
            fadeProgress = 0f;
            ApplyVolume();
            fadeRoutine = StartCoroutine(FadeToTheme());
        }

        private IEnumerator FadeToTheme()
        {
            const float duration = 1.5f;
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                fadeProgress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(time / duration));
                ApplyVolume();
                yield return null;
            }
            if (fadingFrom != null) fadingFrom.Stop();
            fadingFrom = null;
            fadingTo = null;
            fadeProgress = 1f;
            fadeRoutine = null;
            ApplyVolume();
        }

        private static AudioClip CreateAmbient(EnvironmentTheme theme)
        {
            // Deliberately subtle, deterministic placeholder ambience.
            // 11k mono, 8 seconds, small cached clips, no runtime synthesis loop.
            const int rate = 11025;
            const int count = rate * 8;
            var random = new System.Random(305 + (int)theme * 73);
            float[] data = new float[count];
            float wind = 0f;
            float windStrength = theme == EnvironmentTheme.Mountains ? .095f :
                theme == EnvironmentTheme.CityPark ? .04f : .065f;
            int birdsCount = theme == EnvironmentTheme.Meadow ||
                theme == EnvironmentTheme.JapaneseGarden ? 3 :
                theme == EnvironmentTheme.Park ||
                theme == EnvironmentTheme.ChineseGarden ||
                theme == EnvironmentTheme.Lakeside ? 2 :
                theme == EnvironmentTheme.Mountains ||
                theme == EnvironmentTheme.EveningGarden ? 1 : 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                wind = Mathf.Lerp(wind,
                    (float)random.NextDouble() * 2f - 1f, .0015f);
                float airy = windStrength * wind *
                    (.8f + .2f * Mathf.Sin(t * .65f));
                float birds = 0f;
                for (int j = 0; j < birdsCount; j++)
                {
                    float start = 1.1f + j * (6f / Mathf.Max(1, birdsCount));
                    float tau = t - start;
                    if (tau > 0f && tau < .32f)
                    {
                        float envelope = Mathf.Sin(Mathf.PI * tau / .32f);
                        birds += .058f * envelope *
                            Mathf.Sin(2f * Mathf.PI *
                                (1050f * tau + 530f * tau * tau));
                    }
                }
                float water = theme == EnvironmentTheme.Lakeside ||
                    theme == EnvironmentTheme.JapaneseGarden ||
                    theme == EnvironmentTheme.ChineseGarden ?
                    .018f * Mathf.Sin(2f * Mathf.PI * 2.1f * t) *
                        (.5f + .5f * Mathf.Sin(t * .8f)) : 0f;
                float insects = theme == EnvironmentTheme.EveningGarden ?
                    .008f * Mathf.Sin(2f * Mathf.PI * 850f * t) *
                        Mathf.Max(0f, Mathf.Sin(t * 1.5f)) : 0f;
                float farCity = theme == EnvironmentTheme.CityPark ?
                    .014f * Mathf.Sin(2f * Mathf.PI * 61f * t) *
                        (.6f + .4f * Mathf.Sin(t * .6f)) : 0f;
                float fade = Mathf.Clamp01(Mathf.Min(t * 2f, (8f - t) * 2f));
                data[i] = Mathf.Clamp((airy + birds + farCity + water + insects) * fade, -.2f, .2f);
            }
            return MakeClip("Quiet " + EnvironmentThemes.DisplayName(theme) +
                " atmosphere", rate, data);
        }

        private static AudioClip MakeClip(string title, int rate, float[] data)
        {
            var clip = AudioClip.Create(title, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
