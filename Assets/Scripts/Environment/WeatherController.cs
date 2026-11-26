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
        private float flashTime = -1f, flashStrength = 1f, thunderDelay = -1f, thunderLoudness = 1f;
        private int thunderVariant, lastThunder = -1;
        private bool lookDirty = true, flashWasVisible;

        private Mesh rainMesh;
        private Material rainMaterial;
        private MeshRenderer rainRenderer;
        private AudioSource rainSource, thunderSource, birdSource;
        private AudioClip rainClip;
        private readonly AudioClip[] thunders = new AudioClip[ThunderVariants];

        /// <summary>
        /// 0-2 are distant rolls heard in any rain; 3-4 are close claps that only
        /// come with a storm, right after a lightning flash.
        /// </summary>
        public const int ThunderVariants = 5, FirstNearThunder = 3;
        private const float BrownDepth = .06f;
        private readonly AudioClip[] birds = new AudioClip[BirdSongs];

        public WeatherKind Current { get; private set; }
        public static readonly int WindGustId = Shader.PropertyToID("_WindGust");

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
            nextLightning = FirstStrikeDelay(kind, 4f);
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
            nextLightning = FirstStrikeDelay(Current, seconds);
        }

        /// <summary>Storms open with lightning; plain rain rumbles a little later.</summary>
        private float FirstStrikeDelay(WeatherKind kind, float stormDelay)
        {
            if (kind == WeatherKind.Storm) return stormDelay;
            return kind == WeatherKind.Rain ? stormDelay + Between(6f, 16f) : -1f;
        }

        private float UpdateLightning(float delta)
        {
            if (nextLightning >= 0f)
            {
                nextLightning -= delta;
                if (nextLightning < 0f) Strike();
            }
            if (thunderDelay >= 0f)
            {
                thunderDelay -= delta;
                if (thunderDelay < 0f) PlayThunder();
            }
            if (flashTime < 0f) return 0f;
            flashTime += delta;
            float flash = FlashAt(flashTime) * flashStrength;
            if (flashTime > FlashSeconds) flashTime = -1f;
            return flash;
        }

        /// <summary>
        /// One thunder. A storm brings close claps (bright flash, sound almost at
        /// once) and farther rolls (dim flash, longer wait). Plain rain only has
        /// far-off rumbles with no visible flash.
        /// </summary>
        private void Strike()
        {
            bool storm = Current == WeatherKind.Storm;
            bool near = storm && random.NextDouble() < .45;
            thunderVariant = PickThunder(near, lastThunder, random.NextDouble());
            lastThunder = thunderVariant;
            if (storm)
            {
                flashStrength = near ? 1f : .5f;
                // Reduced motion keeps the thunder but never flashes the screen.
                if (PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0) != 1) flashTime = 0f;
                thunderDelay = near ? Between(.15f, .6f) : Between(1.4f, 3.2f);
                thunderLoudness = near ? 1f : .75f;
                nextLightning = Between(9f, 22f);
            }
            else
            {
                thunderDelay = 0f;
                thunderLoudness = Between(.4f, .6f);
                nextLightning = Between(20f, 45f);
            }
        }

        /// <summary>A thunder of the right distance, never the same one twice in a row.</summary>
        public static int PickThunder(bool near, int previous, double roll)
        {
            int first = near ? FirstNearThunder : 0;
            int count = near ? ThunderVariants - FirstNearThunder : FirstNearThunder;
            bool skip = previous >= first && previous < first + count;
            int choices = skip ? count - 1 : count;
            int pick = first + Mathf.Min(choices - 1, (int)(roll * choices));
            if (skip && pick >= previous) pick++;
            return pick;
        }

        private void ApplyLook(WeatherLook look, float flash)
        {
            if (environment != null)
                environment.SetWeatherLook(look.Tint, look.SunScale, look.FogScale, flash);
            if (rainMaterial != null) rainMaterial.SetFloat("_Intensity", look.Rain);
            // Leaves move more in wind and rain; falling leaves come more often.
            Shader.SetGlobalFloat(WindGustId, look.Rain * 2f + Mathf.Clamp01(1f - look.SunScale) * .8f);
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
            AudioClip clip = thunders[thunderVariant];
            if (clip == null) clip = thunders[thunderVariant] = CreateThunder(thunderVariant);
            thunderSource.pitch = Between(.85f, 1.08f);
            thunderSource.panStereo = Between(-.45f, .45f);
            thunderSource.PlayOneShot(clip, thunderLoudness);
        }

        private float Between(float low, float high)
        {
            // Editor previews may force a weather before Start has run.
            if (random == null) random = new System.Random(System.Environment.TickCount);
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

        /// <summary>
        /// One of <see cref="ThunderVariants"/> thunders. Each is a low rolling
        /// rumble built from a few swells; the close ones also open with a sharp
        /// crack. Peaks stay soft enough for a children's game.
        /// </summary>
        public static AudioClip CreateThunder(int variant)
        {
            // Per swell: start (s), size, attack rate, decay rate.
            float seconds, cutoff, peak;
            float[] swells, cracks;
            string title;
            switch (variant)
            {
                case 0:
                    title = "Far grumble"; seconds = 7f; cutoff = .05f; peak = .5f;
                    swells = new[] { 0f, .6f, 1.4f, .9f, 1.1f, 1f, 1.1f, .75f, 2.9f, .5f, 1.3f, .9f };
                    cracks = new float[0];
                    break;
                case 1:
                    title = "Rolling thunder"; seconds = 7.5f; cutoff = .07f; peak = .62f;
                    swells = new[] { .1f, .8f, 6f, 1.4f, .9f, 1f, 4f, 1.1f, 2f, .7f, 4f, 1f,
                        3.3f, .5f, 3f, .9f, 4.6f, .3f, 3f, 1f };
                    cracks = new float[0];
                    break;
                case 2:
                    title = "Low boom"; seconds = 6f; cutoff = .06f; peak = .66f;
                    swells = new[] { .05f, 1f, 22f, 1.3f, .8f, .5f, 5f, 1f, 2.2f, .3f, 3f, 1f };
                    cracks = new float[0];
                    break;
                case 3:
                    title = "Near clap"; seconds = 6.5f; cutoff = .11f; peak = .8f;
                    swells = new[] { .04f, 1f, 30f, 1.6f, .6f, .7f, 6f, 1.2f, 1.6f, .5f, 4f, 1f,
                        3f, .3f, 3f, 1f };
                    cracks = new[] { .02f };
                    break;
                default:
                    title = "Double clap"; seconds = 7f; cutoff = .1f; peak = .8f;
                    swells = new[] { .04f, .8f, 30f, 1.8f, .36f, 1f, 25f, 1.4f, 1.4f, .55f, 4f, 1f,
                        2.9f, .3f, 3f, .9f };
                    cracks = new[] { .02f, .34f };
                    break;
            }

            const int rate = 22050;
            int count = (int)(rate * seconds);
            var rng = new System.Random(4173 + variant * 977);
            var rumble = new float[count];
            var crack = new float[count];
            float brown = 0f, low1 = 0f, low2 = 0f, hissLow = 0f, midLow = 0f;
            float flutterFrom = 1f, flutterTo = 1f;
            int flutterAt = 0, flutterLength = 1;
            float tear = 1f;
            float rumblePeak = 1e-6f, crackPeak = 1e-6f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float white = (float)rng.NextDouble() * 2f - 1f;

                // Rumble: mostly 100-400 Hz so phone speakers can play it, with
                // a little brown noise underneath for depth on headphones.
                brown = Mathf.Clamp(brown * .998f + white * .04f, -1f, 1f);
                low1 += cutoff * (white + brown * BrownDepth - low1);
                low2 += cutoff * (low1 - low2);
                // The sky "rolls": loudness wanders every 90-220 ms.
                if (i - flutterAt >= flutterLength)
                {
                    flutterAt = i;
                    flutterLength = (int)(rate * (.09f + .13f * (float)rng.NextDouble()));
                    flutterFrom = flutterTo;
                    flutterTo = .45f + .55f * (float)rng.NextDouble();
                }
                float flutter = Mathf.Lerp(flutterFrom, flutterTo,
                    Mathf.SmoothStep(0f, 1f, (i - flutterAt) / (float)flutterLength));
                float envelope = 0f;
                for (int s = 0; s + 3 < swells.Length; s += 4)
                {
                    float tau = t - swells[s];
                    if (tau > 0f)
                        envelope += swells[s + 1] * (1f - Mathf.Exp(-tau * swells[s + 2])) *
                            Mathf.Exp(-tau * swells[s + 3]);
                }
                rumble[i] = low2 * envelope * flutter;
                rumblePeak = Mathf.Max(rumblePeak, Mathf.Abs(rumble[i]));

                // Crack: bright, torn noise that dies away within half a second.
                hissLow += .3f * (white - hissLow);
                midLow += .06f * (white - midLow);
                float crackEnvelope = 0f;
                foreach (float start in cracks)
                {
                    float tau = t - start;
                    if (tau > 0f && tau < .6f)
                        crackEnvelope += (1f - Mathf.Exp(-tau * 500f)) * Mathf.Exp(-tau * 11f);
                }
                if (crackEnvelope > 0f)
                {
                    if (rng.NextDouble() < .004) tear = .25f + .75f * (float)rng.NextDouble();
                    crack[i] = ((white - hissLow) * .45f + (hissLow - midLow) * .9f) *
                        crackEnvelope * tear;
                    crackPeak = Mathf.Max(crackPeak, Mathf.Abs(crack[i]));
                }
            }

            var data = new float[count];
            float drive = Tanh(1.6f), mixPeak = 1e-6f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float sample = rumble[i] / rumblePeak + .75f * crack[i] / crackPeak;
                // Soft saturation keeps the boom round instead of clipped.
                sample = Tanh(1.6f * sample / 1.75f) / drive;
                sample *= Mathf.Clamp01(t / .004f) * Mathf.Clamp01((seconds - t) / .8f);
                data[i] = sample;
                mixPeak = Mathf.Max(mixPeak, Mathf.Abs(sample));
            }
            for (int i = 0; i < count; i++) data[i] *= peak / mixPeak;
            return MakeClip(title, rate, data);
        }

        private static float Tanh(float x)
        {
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
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
            Shader.SetGlobalFloat(WindGustId, 0f);
            if (environment != null) environment.SetWeatherLook(Color.white, 1f, 1f, 0f);
            Shader.SetGlobalVector(EnvironmentController.WeatherDimId, Vector4.zero);
            if (rainMesh != null) Destroy(rainMesh);
            if (rainMaterial != null) Destroy(rainMaterial);
            if (rainClip != null) Destroy(rainClip);
            foreach (AudioClip thunder in thunders)
                if (thunder != null) Destroy(thunder);
            foreach (AudioClip bird in birds)
                if (bird != null) Destroy(bird);
        }
    }
}
