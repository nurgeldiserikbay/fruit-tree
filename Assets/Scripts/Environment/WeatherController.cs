using UnityEngine;

namespace FruitTree
{
    public enum WeatherKind { Clear, Mist, Cloudy, Rain, Storm }

    /// <summary>How one kind of weather looks and sounds.</summary>
    public readonly struct WeatherLook
    {
        public readonly Color Tint;
        public readonly float SunScale, FogScale, Rain, BirdGapMin, BirdGapMax;

        public WeatherLook(Color tint, float sunScale, float fogScale, float rain,
            float birdGapMin, float birdGapMax)
        {
            Tint = tint;
            SunScale = sunScale;
            FogScale = fogScale;
            Rain = rain;
            BirdGapMin = birdGapMin;
            BirdGapMax = birdGapMax;
        }

        public bool HasBirds => BirdGapMax > 0f;

        public static WeatherLook For(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Clear:
                    return new WeatherLook(new Color(1.04f, 1.03f, .98f), 1.22f, 1.25f, 0f, 2.5f, 7f);
                case WeatherKind.Mist:
                    return new WeatherLook(new Color(.96f, .98f, .98f), .80f, .85f, 0f, 5f, 12f);
                case WeatherKind.Cloudy:
                    return new WeatherLook(new Color(.76f, .79f, .84f), .55f, .90f, 0f, 8f, 18f);
                case WeatherKind.Rain:
                    return new WeatherLook(new Color(.56f, .60f, .67f), .32f, .62f, .6f, 0f, 0f);
                default:
                    return new WeatherLook(new Color(.36f, .39f, .47f), .18f, .50f, 1f, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// Random, slowly changing weather with procedural rain, thunder and birds.
    /// Uses one extra draw call for rain and only touches lighting while changing.
    /// </summary>
    public sealed class WeatherController : MonoBehaviour
    {
        private const float MinimumDuration = 70f, MaximumDuration = 150f, BlendSeconds = 14f, NewTreeBlendSeconds = 4f;
        private const float RainVolume = .34f, ThunderVolume = .6f, BirdVolume = .16f;
        private const int RainDrops = 1100, BirdSongs = 5;

        private EnvironmentController environment;
        private OrchardAudio orchardAudio;
        private System.Random random;
        private WeatherLook fromLook, toLook;
        private float blend = 1f, blendSeconds = BlendSeconds, remaining, nextBird, nextLightning = -1f;
        private OrchardGameController orchard;
        private float flashTime = -1f, thunderDelay = -1f;
        private bool lookDirty = true, flashWasVisible;

        private Mesh rainMesh;
        private Material rainMaterial;
        private MeshRenderer rainRenderer;
        private AudioSource rainSource, thunderSource, birdSource;
        private AudioClip rainClip, thunderClip;
        private readonly AudioClip[] birds = new AudioClip[BirdSongs];

        public WeatherKind Current { get; private set; }

        public void Initialize(EnvironmentController owner)
        {
            environment = owner;
        }

        // Only the real game scene (with a sky) gets weather, not bare test rigs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            var owner = FindFirstObjectByType<EnvironmentController>();
            if (owner == null || owner.skyRenderer == null) return;
            new GameObject("Weather").AddComponent<WeatherController>().Initialize(owner);
        }

        private void Start()
        {
            random = new System.Random(System.Environment.TickCount);
            orchardAudio = FindFirstObjectByType<OrchardAudio>();
            rainSource = NewSource(true);
            thunderSource = NewSource(false);
            birdSource = NewSource(false);
            for (int i = 0; i < birds.Length; i++) birds[i] = CreateBirdSong(i);
            CreateRainRenderer();

            // A session opens on a gentle sky; storms build up later.
            ForceWeather(PickInitial(random.NextDouble()));
            remaining = Between(MinimumDuration, MaximumDuration);
            nextBird = Between(1f, 3f);
            // Every tree the player moves to brings its own weather.
            orchard = FindFirstObjectByType<OrchardGameController>();
            if (orchard != null) orchard.TreeDisplayed += OnTreeDisplayed;
        }

        private void OnTreeDisplayed()
        {
            if (random == null) return;
            StartBlendTo(PickForNewTree(Current, random.NextDouble()), NewTreeBlendSeconds);
        }

        /// <summary>Random weather for a newly opened tree; always different from now.</summary>
        public static WeatherKind PickForNewTree(WeatherKind current, double roll)
        {
            WeatherKind pick = roll < .34 ? WeatherKind.Clear : roll < .54 ? WeatherKind.Mist :
                roll < .74 ? WeatherKind.Cloudy : roll < .92 ? WeatherKind.Rain : WeatherKind.Storm;
            return pick != current ? pick : PickNext(current, roll);
        }

        private AudioSource NewSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Immediately shows a weather; used at start and by previews.</summary>
        public void ForceWeather(WeatherKind kind)
        {
            Current = kind;
            fromLook = toLook = WeatherLook.For(kind);
            blend = 1f;
            lookDirty = true;
            nextLightning = kind == WeatherKind.Storm ? 4f : -1f;
            ApplyLook(toLook, 0f);
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            remaining -= delta;
            if (remaining <= 0f) BeginNext();

            if (blend < 1f)
            {
                blend = Mathf.Min(1f, blend + delta / blendSeconds);
                lookDirty = true;
            }
            WeatherLook look = blend >= 1f ? toLook : Mix(fromLook, toLook,
                Mathf.SmoothStep(0f, 1f, blend));

            float flash = UpdateLightning(delta);
            if (lookDirty || flash > 0f || flashWasVisible)
            {
                ApplyLook(look, flash);
                lookDirty = false;
                flashWasVisible = flash > 0f;
            }
            UpdateAudio(look, delta);
        }

        private void BeginNext()
        {
            StartBlendTo(PickNext(Current, random.NextDouble()), BlendSeconds);
        }

        private void StartBlendTo(WeatherKind kind, float seconds)
        {
            fromLook = blend >= 1f ? toLook : Mix(fromLook, toLook,
                Mathf.SmoothStep(0f, 1f, blend));
            Current = kind;
            toLook = WeatherLook.For(Current);
            blend = 0f;
            blendSeconds = seconds;
            remaining = Between(MinimumDuration, MaximumDuration);
            nextLightning = Current == WeatherKind.Storm ? seconds : -1f;
        }

        private float UpdateLightning(float delta)
        {
            if (nextLightning >= 0f)
            {
                nextLightning -= delta;
                if (nextLightning < 0f)
                {
                    // Reduced motion keeps the thunder but never flashes the screen.
                    if (PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0) != 1) flashTime = 0f;
                    thunderDelay = Between(.4f, 2.5f);
                    nextLightning = Between(9f, 22f);
                }
            }
            if (thunderDelay >= 0f)
            {
                thunderDelay -= delta;
                if (thunderDelay < 0f) PlayThunder();
            }
            if (flashTime < 0f) return 0f;
            flashTime += delta;
            float flash = FlashAt(flashTime);
            if (flashTime > FlashSeconds) flashTime = -1f;
            return flash;
        }

        private void ApplyLook(WeatherLook look, float flash)
        {
            if (environment != null)
                environment.SetWeatherLook(look.Tint, look.SunScale, look.FogScale, flash);
            if (rainMaterial != null) rainMaterial.SetFloat("_Intensity", look.Rain);
            if (rainRenderer != null) rainRenderer.enabled = look.Rain > .01f;
        }

        private void UpdateAudio(WeatherLook look, float delta)
        {
            bool muted = orchardAudio != null && orchardAudio.IsMuted;
            float quiet = AudioLevels.Gain(AudioChannel.Nature);
            rainSource.mute = thunderSource.mute = birdSource.mute = muted;
            thunderSource.volume = ThunderVolume * quiet;
            birdSource.volume = BirdVolume * quiet;

            rainSource.volume = RainVolume * quiet * look.Rain;
            if (look.Rain > .01f && !rainSource.isPlaying)
            {
                if (rainClip == null) rainClip = CreateRain();
                rainSource.clip = rainClip;
                rainSource.Play();
            }
            else if (look.Rain <= .01f && rainSource.isPlaying) rainSource.Stop();

            if (!look.HasBirds || look.Rain > .05f) return;
            nextBird -= delta;
            if (nextBird > 0f) return;
            nextBird = Between(look.BirdGapMin, look.BirdGapMax);
            if (birdSource.isPlaying) return;
            birdSource.clip = birds[random.Next(birds.Length)];
            birdSource.pitch = Between(.9f, 1.12f);
            birdSource.panStereo = Between(-.75f, .75f);
            birdSource.Play();
        }

        private void PlayThunder()
        {
            if (thunderClip == null) thunderClip = CreateThunder();
            thunderSource.pitch = Between(.75f, 1.1f);
            thunderSource.PlayOneShot(thunderClip);
        }

        private float Between(float low, float high)
        {
            return low + (float)random.NextDouble() * (high - low);
        }

        private static WeatherLook Mix(WeatherLook from, WeatherLook to, float t)
        {
            return new WeatherLook(Color.Lerp(from.Tint, to.Tint, t),
                Mathf.Lerp(from.SunScale, to.SunScale, t),
                Mathf.Lerp(from.FogScale, to.FogScale, t),
                Mathf.Lerp(from.Rain, to.Rain, t),
                Mathf.Lerp(from.BirdGapMin, to.BirdGapMin, t),
                t < .5f ? from.BirdGapMax : to.BirdGapMax);
        }

        public static WeatherKind PickInitial(double roll)
        {
            return roll < .5 ? WeatherKind.Clear : roll < .78 ? WeatherKind.Mist : WeatherKind.Cloudy;
        }

        /// <summary>Weather drifts naturally: storms only grow out of clouds or rain.</summary>
        public static WeatherKind PickNext(WeatherKind current, double roll)
        {
            switch (current)
            {
                case WeatherKind.Clear:
                    return roll < .3 ? WeatherKind.Mist : WeatherKind.Cloudy;
                case WeatherKind.Mist:
                    return roll < .45 ? WeatherKind.Clear :
                        roll < .85 ? WeatherKind.Cloudy : WeatherKind.Rain;
                case WeatherKind.Cloudy:
                    return roll < .3 ? WeatherKind.Clear : roll < .45 ? WeatherKind.Mist :
                        roll < .85 ? WeatherKind.Rain : WeatherKind.Storm;
                case WeatherKind.Rain:
                    return roll < .45 ? WeatherKind.Cloudy : roll < .65 ? WeatherKind.Mist :
                        roll < .9 ? WeatherKind.Storm : WeatherKind.Clear;
                default:
                    return roll < .6 ? WeatherKind.Rain : WeatherKind.Cloudy;
            }
        }

        public const float FlashSeconds = .42f;

        /// <summary>A soft double pulse, capped well below full white.</summary>
        public static float FlashAt(float seconds)
        {
            if (seconds < 0f || seconds > FlashSeconds) return 0f;
            float first = Mathf.Exp(-seconds * 22f);
            float second = seconds < .16f ? 0f : .7f * Mathf.Exp(-(seconds - .16f) * 18f);
            float fadeOut = Mathf.Clamp01((FlashSeconds - seconds) / .08f);
            return Mathf.Min(.55f, .55f * Mathf.Max(first, second)) * fadeOut;
        }

        private void CreateRainRenderer()
        {
            var source = Resources.Load<Material>("RainMaterial");
            if (source == null) return;
            rainMaterial = new Material(source) { name = "Runtime Rain" };
            rainMesh = BuildRainMesh(RainDrops, 4182);
            var rain = new GameObject("Rain", typeof(MeshFilter), typeof(MeshRenderer));
            rain.transform.SetParent(transform, false);
            rain.GetComponent<MeshFilter>().sharedMesh = rainMesh;
            rainRenderer = rain.GetComponent<MeshRenderer>();
            rainRenderer.sharedMaterial = rainMaterial;
            rainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rainRenderer.receiveShadows = false;
            rainRenderer.enabled = false;
        }

        /// <summary>All streaks in one mesh; the vertex shader animates and wraps them.</summary>
        public static Mesh BuildRainMesh(int drops, int seed)
        {
            var rng = new System.Random(seed);
            var vertices = new Vector3[drops * 4];
            var uv = new Vector2[drops * 4];
            var colors = new Color32[drops * 4];
            var triangles = new int[drops * 6];
            for (int i = 0; i < drops; i++)
            {
                var center = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(),
                    (float)rng.NextDouble());
                var tone = new Color32((byte)rng.Next(256), (byte)rng.Next(256), 0, 255);
                for (int corner = 0; corner < 4; corner++)
                {
                    int v = i * 4 + corner;
                    vertices[v] = center;
                    uv[v] = new Vector2(corner % 2 == 0 ? -1f : 1f, corner < 2 ? 0f : 1f);
                    colors[v] = tone;
                }
                int t = i * 6, b = i * 4;
                triangles[t] = b;
                triangles[t + 1] = b + 2;
                triangles[t + 2] = b + 1;
                triangles[t + 3] = b + 1;
                triangles[t + 4] = b + 2;
                triangles[t + 5] = b + 3;
            }
            var mesh = new Mesh { name = "Rain Streaks" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            // The shader places streaks around the camera, so never cull the mesh.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            return mesh;
        }

        private static AudioClip CreateRain()
        {
            const int rate = 22050, count = rate * 6, overlap = rate / 2;
            var rng = new System.Random(811);
            var data = new float[count + overlap];
            float low = 0f, body = 0f, drop = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)rng.NextDouble() * 2f - 1f;
                low += .22f * (white - low);
                body += .035f * (white - body);
                if (rng.NextDouble() < .0016) drop = .5f + .5f * (float)rng.NextDouble();
                drop *= .985f;
                data[i] = (white - low) * .11f + low * .16f + body * .34f +
                    white * drop * .09f;
            }
            var loop = new float[count];
            for (int i = 0; i < count; i++)
            {
                float sample = data[i];
                if (i < overlap)
                {
                    float t = i / (float)overlap;
                    sample = data[i] * Mathf.Sqrt(t) + data[count + i] * Mathf.Sqrt(1f - t);
                }
                loop[i] = Mathf.Clamp(sample, -.8f, .8f);
            }
            return MakeClip("Forest rain", rate, loop);
        }

        private static AudioClip CreateThunder()
        {
            const int rate = 11025;
            int count = rate * 6;
            var rng = new System.Random(4173);
            var data = new float[count];
            float brown = 0f, deep = 0f;
            float[] starts = { .05f, .55f, 1.3f, 2.4f };
            float[] sizes = { 1f, .72f, .55f, .3f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                brown = Mathf.Clamp(brown + white * .05f, -1f, 1f) * .996f;
                deep += .05f * (brown - deep);
                float envelope = 0f;
                for (int b = 0; b < starts.Length; b++)
                {
                    float tau = t - starts[b];
                    if (tau > 0f)
                        envelope += sizes[b] * Mathf.Min(1f, tau * 18f) * Mathf.Exp(-tau * 1.5f);
                }
                float fade = Mathf.Clamp01((6f - t) * 2f);
                data[i] = Mathf.Clamp(deep * envelope * 2.6f * fade, -.9f, .9f);
            }
            return MakeClip("Distant thunder", rate, data);
        }

        /// <summary>Short whistled phrases; each index is a different little song.</summary>
        public static AudioClip CreateBirdSong(int song)
        {
            const int rate = 22050;
            var rng = new System.Random(9301 + song * 131);
            int notes = 2 + song % 4;
            float noteLength = .09f + .035f * (song % 3);
            float gap = .05f + .02f * (song % 2);
            float baseFrequency = 2300f + 420f * song;
            int count = (int)(rate * (notes * (noteLength + gap) + .06f));
            var data = new float[count];
            int cursor = 0;
            for (int n = 0; n < notes; n++)
            {
                float start = baseFrequency * (.9f + .3f * (float)rng.NextDouble());
                float sweep = (float)(rng.NextDouble() - .45) * 2600f;
                int length = (int)(rate * noteLength);
                float phase = 0f;
                for (int i = 0; i < length && cursor + i < count; i++)
                {
                    float t = i / (float)length;
                    float frequency = start + sweep * t;
                    phase += 2f * Mathf.PI * frequency / rate;
                    float envelope = Mathf.Sin(Mathf.PI * t);
                    float trill = .78f + .22f * Mathf.Sin(2f * Mathf.PI * 38f * t * noteLength * 10f);
                    data[cursor + i] = (Mathf.Sin(phase) + .16f * Mathf.Sin(phase * 2f)) *
                        envelope * envelope * trill * .5f;
                }
                cursor += (int)(rate * (noteLength + gap));
            }
            return MakeClip("Bird song " + song, rate, data);
        }

        private static AudioClip MakeClip(string title, int rate, float[] data)
        {
            var clip = AudioClip.Create(title, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (orchard != null) orchard.TreeDisplayed -= OnTreeDisplayed;
            if (environment != null) environment.SetWeatherLook(Color.white, 1f, 1f, 0f);
            Shader.SetGlobalVector(EnvironmentController.WeatherDimId, Vector4.zero);
            if (rainMesh != null) Destroy(rainMesh);
            if (rainMaterial != null) Destroy(rainMaterial);
            if (rainClip != null) Destroy(rainClip);
            if (thunderClip != null) Destroy(thunderClip);
            foreach (AudioClip bird in birds)
                if (bird != null) Destroy(bird);
        }
    }
}
