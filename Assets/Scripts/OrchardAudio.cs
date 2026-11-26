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
                return HasPlaylist || themeMusic != null && index >= 0 &&
                    index < themeMusic.Length && themeMusic[index] != null;
            }
        }

        // Playlist from Assets/Audio: each tree has its own track, which repeats
        // with soft fades; changing tree cross-fades to that tree's track.
        private const float PlaylistFadeIn = 3f, PlaylistFadeOut = 5f, TreeSwitchFade = 1.5f;
        private AudioClip[] playlist;
        private int playlistIndex, treeIndex;
        private bool playingPlaylist;
        private float switchFade = -1f;
        private bool HasPlaylist => playlist != null && playlist.Length > 0;
        private float PlaylistVolume => .5f * AudioLevels.Gain(AudioChannel.Music);

        // Only the real game scene gets music, not bare test rigs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGameMusic()
        {
            var scenery = FindFirstObjectByType<EnvironmentController>();
            if (scenery == null || scenery.skyRenderer == null) return;
            var sound = FindFirstObjectByType<OrchardAudio>();
            var tracks = Resources.Load<MusicPlaylist>("MusicPlaylist");
            if (sound != null && tracks != null) sound.UsePlaylist(tracks.tracks);
        }

        /// <summary>Uses the tracks in file-name order; tree N plays track N (wrapping).</summary>
        public void UsePlaylist(AudioClip[] tracks)
        {
            playlist = tracks == null ? null : (AudioClip[])tracks.Clone();
            playlistIndex = TrackForTree(treeIndex, HasPlaylist ? playlist.Length : 0);
            SelectMusic(activeTheme);
        }

        public static int TrackForTree(int tree, int trackCount)
        {
            return trackCount <= 0 ? 0 : (Mathf.Max(0, tree) % trackCount);
        }

        /// <summary>Called whenever a tree is shown: fades over to that tree's track.</summary>
        public void SelectTreeMusic(int tree)
        {
            treeIndex = Mathf.Max(0, tree);
            if (!HasPlaylist) return;
            int wanted = TrackForTree(treeIndex, playlist.Length);
            if (wanted == playlistIndex) return;
            playlistIndex = wanted;
            if (playingPlaylist && music != null && music.isPlaying) switchFade = 0f;
            else SelectMusic(activeTheme);
        }

        private void Update()
        {
            if (!playingPlaylist || music == null || music.clip == null) return;
            if (switchFade >= 0f)
            {
                // Fade the old tree's track down, then start the new one.
                switchFade += Time.unscaledDeltaTime / TreeSwitchFade;
                music.volume = Mathf.Min(music.volume, PlaylistVolume * (1f - switchFade));
                if (switchFade < 1f) return;
                switchFade = -1f;
                music.Stop();
                music.clip = playlist[playlistIndex];
                music.volume = 0f;
                if (!muted && !musicMuted) music.Play();
                return;
            }
            if (music.isPlaying)
            {
                float fade = Mathf.Clamp01(Mathf.Min(music.time / PlaylistFadeIn,
                    (music.clip.length - music.time) / PlaylistFadeOut));
                music.volume = PlaylistVolume * fade;
            }
            else if (!muted && !musicMuted && !AudioListener.pause)
            {
                // The tree's track ended: play it again from a soft start.
                music.clip = playlist[playlistIndex];
                music.volume = 0f;
                music.Play();
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
            if (ownsPickClip)
                for (int i = 0; i < pickVariants.Length; i++) pickVariants[i] = CreatePick(i);
            AudioLevels.Changed += ApplyVolume;
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
            AudioLevels.Changed -= ApplyVolume;
            if (ownsPickClip) ReleaseGeneratedClip(collect);
            foreach (AudioClip variant in pickVariants) ReleaseGeneratedClip(variant);
            if (ownsAmbientClip)
                for (int i = 0; i < generatedThemeClips.Length; i++)
                    ReleaseGeneratedClip(generatedThemeClips[i]);
        }

        private static void ReleaseGeneratedClip(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) Destroy(clip);
            else DestroyImmediate(clip);
        }

        private void OnFruitPicked(Apple fruit)
        {
            // Restore/scene switching can update the HUD count without a real tap.
            // Only a successful player pick has a quiet, tactile SFX.
            if (fruit == null || muted || collect == null) return;
            if (!ownsPickClip || pickVariants[0] == null)
            {
                effects.PlayOneShot(collect);
                return;
            }
            // A different take each time, pitched to the fruit: no machine-gun repetition.
            effects.pitch = PickPitch(tree != null ? tree.SpeciesId : null) *
                (.93f + (float)pickRandom.NextDouble() * .14f);
            effects.PlayOneShot(pickVariants[pickRandom.Next(pickVariants.Length)]);
        }

        private readonly AudioClip[] pickVariants = new AudioClip[4];
        private readonly System.Random pickRandom = new System.Random();

        /// <summary>Small fruit sound higher, big fruit lower.</summary>
        public static float PickPitch(string species)
        {
            switch (species)
            {
                case "cherry": return 1.18f;
                case "plum": case "apricot": case "mandarin": return 1.06f;
                case "orange": case "pomegranate": return .9f;
                case "lemon": case "pear": return .96f;
                default: return 1f;
            }
        }

        /// <summary>
        /// One pick: a crisp stem snap, a short brush of leaves and a soft, low
        /// "tock" as the fruit lands in the hand. Each variant differs a little.
        /// </summary>
        public static AudioClip CreatePick(int variant)
        {
            const int rate = 22050;
            const float duration = .34f;
            var random = new System.Random(4411 + variant * 97);
            float Between(float low, float high) => low + (float)random.NextDouble() * (high - low);
            float snapAt = Between(.004f, .012f), tockAt = Between(.035f, .06f);
            float tockPitch = Between(170f, 230f), leafLength = Between(.12f, .2f);
            float leafCentre = Between(.05f, .09f);
            var data = new float[(int)(rate * duration)];
            float high = 0f, low = 0f, band = 0f, previous = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float white = (float)random.NextDouble() * 2f - 1f;
                // Stem snap: a bright, very short crack.
                high = .55f * (white - previous) + .45f * high;
                previous = white;
                float snap = t >= snapAt ? high * Mathf.Exp(-(t - snapAt) * 260f) * .55f : 0f;
                // Leaves: soft band of noise that swells and fades.
                low += .18f * (white - low);
                band += .45f * (low - band);
                float leafEnvelope = Mathf.Exp(-Mathf.Pow((t - leafCentre) / (leafLength * .5f), 2f));
                float leaves = (low - band) * leafEnvelope * .5f;
                // Fruit: a muted, slightly falling low tone.
                float tock = 0f;
                if (t >= tockAt)
                {
                    float tau = t - tockAt;
                    tock = Mathf.Sin(2f * Mathf.PI * (tockPitch * tau - 160f * tau * tau)) *
                        Mathf.Exp(-tau * 38f) * Mathf.Min(1f, tau * 900f) * .22f;
                }
                float fade = Mathf.Clamp01(Mathf.Min(t * 2000f, (duration - t) * 40f));
                data[i] = Mathf.Clamp((snap + leaves + tock) * fade, -.85f, .85f);
            }
            return MakeClip("Fruit pick " + variant, rate, data);
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
            // A track assigned to this scenery wins; otherwise the playlist continues.
            playingPlaylist = next == null && HasPlaylist;
            if (playingPlaylist) next = playlist[playlistIndex];
            music.loop = !playingPlaylist;
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
            // Player sliders set the mix; "quiet audio" is replaced by them.
            if (!playingPlaylist) music.volume = .07f * AudioLevels.Gain(AudioChannel.Music);
            float target = .18f * AudioLevels.Gain(AudioChannel.Nature);
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
            effects.volume = .4f * AudioLevels.Gain(AudioChannel.Effects);
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
