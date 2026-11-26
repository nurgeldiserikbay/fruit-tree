using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// A shallow stream that bends past the tree, lined with stones and with
    /// its own gentle water sound. Shown only in natural photo sceneries.
    /// </summary>
    public sealed class Brook : MonoBehaviour
    {
        public const float HalfLength = 26f;
        private const float Volume = .16f;
        private EnvironmentController scenery;
        private OrchardAudio orchardAudio;
        private GameObject water, stones;
        private Material waterMaterial, stoneMaterial;
        private AudioSource sound;
        private AudioClip clip;
        private readonly List<Object> owned = new List<Object>();
        private bool built;

        public void Initialize(EnvironmentController owner)
        {
            scenery = owner;
            scenery.ThemeChanged += OnThemeChanged;
        }

        private void Start()
        {
            var source = Resources.Load<Material>("WaterMaterial");
            Shader stoneShader = Shader.Find("FruitTree/VertexLit") ?? Shader.Find("Standard");
            if (source == null || stoneShader == null || scenery == null)
            {
                enabled = false;
                return;
            }
            orchardAudio = FindFirstObjectByType<OrchardAudio>();
            waterMaterial = Track(new Material(source) { name = "Runtime Brook Water" });
            stoneMaterial = Track(new Material(stoneShader) { name = "Brook Stones" });
            if (stoneMaterial.HasProperty("_AmbientLight")) stoneMaterial.SetFloat("_AmbientLight", .7f);
            water = Part("Brook Water", Track(BuildWater()), waterMaterial);
            stones = Part("Brook Stones", Track(BuildStones(731)), stoneMaterial);
            sound = gameObject.AddComponent<AudioSource>();
            sound.loop = true;
            sound.playOnAwake = false;
            sound.spatialBlend = 0f;
            built = true;
            OnThemeChanged(scenery.CurrentTheme);
        }

        private GameObject Part(string label, Mesh mesh, Material material)
        {
            var part = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(transform, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return part;
        }

        private T Track<T>(T asset) where T : Object
        {
            owned.Add(asset);
            return asset;
        }

        private void OnThemeChanged(EnvironmentTheme theme)
        {
            if (!built) return;
            bool visible = scenery.PhotoSceneActive && PhotoScene.HasBrook(theme);
            water.SetActive(visible);
            stones.SetActive(visible);
            bool fade = scenery.CurrentPhotoScene.FadeGroundToPhoto;
            waterMaterial.SetFloat("_GroundFade", fade ? 1f : 0f);
            if (stoneMaterial.HasProperty("_GroundFade"))
            {
                // Same treatment as the ground: dissolve into the photo far away.
                stoneMaterial.SetFloat("_GroundFade", fade ? 1f : 0f);
                stoneMaterial.SetFloat("_SrcBlend", (float)(fade ?
                    UnityEngine.Rendering.BlendMode.SrcAlpha : UnityEngine.Rendering.BlendMode.One));
                stoneMaterial.SetFloat("_DstBlend", (float)(fade ?
                    UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.Zero));
                stoneMaterial.renderQueue = fade ? 1960 : -1;
            }
            if (visible && !sound.isPlaying)
            {
                if (clip == null) clip = Track(CreateBrookSound());
                sound.clip = clip;
                sound.Play();
            }
            else if (!visible && sound.isPlaying) sound.Stop();
        }

        private void Update()
        {
            if (sound == null) return;
            bool muted = orchardAudio != null && orchardAudio.IsMuted;
            float quiet = AudioLevels.Gain(AudioChannel.Nature);
            sound.mute = muted;
            sound.volume = Volume * quiet;
        }

        private void OnDestroy()
        {
            if (scenery != null) scenery.ThemeChanged -= OnThemeChanged;
            foreach (Object asset in owned)
                if (asset != null) Destroy(asset);
        }

        /// <summary>Centre line of the stream: it passes about five metres from the trunk.</summary>
        public static Vector3 CenterAt(float x)
        {
            float z = 6.8f + 1.1f * Mathf.Sin(x * .18f + .6f) + .5f * Mathf.Sin(x * .43f);
            return new Vector3(x, 0f, z);
        }

        public static float WidthAt(float x)
        {
            return 1.5f + .35f * Mathf.Sin(x * .31f);
        }

        private static Vector3 SideAt(float x)
        {
            Vector3 tangent = (CenterAt(x + .05f) - CenterAt(x - .05f)).normalized;
            return new Vector3(-tangent.z, 0f, tangent.x);
        }

        public static Mesh BuildWater()
        {
            const float step = .5f;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            float along = 0f;
            Vector3 previous = CenterAt(-HalfLength);
            int rows = 0;
            for (float x = -HalfLength; x <= HalfLength + .001f; x += step)
            {
                Vector3 center = CenterAt(x);
                along += Vector3.Distance(previous, center);
                previous = center;
                Vector3 side = SideAt(x);
                float half = WidthAt(x) * .5f;
                for (int column = 0; column < 3; column++)
                {
                    float across = column * .5f;
                    Vector3 point = center + side * ((across - .5f) * 2f * half);
                    point.y = GroundGenerator.HeightAt(point.x, point.z) + .05f;
                    vertices.Add(point);
                    uv.Add(new Vector2(along, across));
                }
                if (rows > 0)
                {
                    int a = (rows - 1) * 3, b = rows * 3;
                    for (int column = 0; column < 2; column++)
                    {
                        triangles.Add(a + column); triangles.Add(b + column); triangles.Add(a + column + 1);
                        triangles.Add(a + column + 1); triangles.Add(b + column); triangles.Add(b + column + 1);
                    }
                }
                rows++;
            }
            var mesh = new Mesh { name = "Brook Water" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh BuildStones(int seed)
        {
            var rng = new System.Random(seed);
            float Between(float low, float high) => low + (float)rng.NextDouble() * (high - low);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            const int sides = 6;
            for (float x = -HalfLength; x <= HalfLength; x += .55f)
            {
                for (int bank = -1; bank <= 1; bank += 2)
                {
                    int count = rng.NextDouble() < .4 ? 2 : 1;
                    for (int n = 0; n < count; n++)
                    {
                        float at = x + Between(-.25f, .25f);
                        float size = Between(.07f, .2f);
                        Vector3 center = CenterAt(at) + SideAt(at) *
                            (bank * (WidthAt(at) * .5f + Between(-.05f, .25f)));
                        AddStone(vertices, colors, triangles, center, size, sides, rng);
                    }
                }
                // A few stones in the current break up the surface.
                if (rng.NextDouble() < .18)
                    AddStone(vertices, colors, triangles, CenterAt(x) + SideAt(x) * Between(-.3f, .3f),
                        Between(.06f, .12f), sides, rng);
            }
            var mesh = new Mesh { name = "Brook Stones" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddStone(List<Vector3> vertices, List<Color> colors, List<int> triangles,
            Vector3 center, float size, int sides, System.Random rng)
        {
            float Between(float low, float high) => low + (float)rng.NextDouble() * (high - low);
            center.y = GroundGenerator.HeightAt(center.x, center.z) + size * .22f;
            Color color = new Color(.46f, .45f, .42f) * Between(.72f, 1.08f);
            if (rng.NextDouble() < .3) color = Color.Lerp(color, new Color(.35f, .42f, .25f), .5f);
            color.a = 1f;
            float spin = Between(0f, Mathf.PI);
            int first = vertices.Count;
            int top = first;
            vertices.Add(center + Vector3.up * size * .55f);
            colors.Add(color * 1.08f);
            for (int ring = 0; ring < 2; ring++)
            {
                float height = ring == 0 ? size * .25f : -size * .2f;
                float scale = ring == 0 ? .8f : 1f;
                for (int s = 0; s < sides; s++)
                {
                    float angle = spin + s * Mathf.PI * 2f / sides;
                    float radius = size * scale * Between(.8f, 1.15f);
                    vertices.Add(center + new Vector3(Mathf.Cos(angle) * radius, height,
                        Mathf.Sin(angle) * radius * .8f));
                    colors.Add(color);
                }
            }
            for (int s = 0; s < sides; s++)
            {
                int a = first + 1 + s, b = first + 1 + (s + 1) % sides;
                triangles.Add(top); triangles.Add(b); triangles.Add(a);
                int c = a + sides, d = b + sides;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        /// <summary>Trickling water: soft rush plus many tiny rising bubbles.</summary>
        public static AudioClip CreateBrookSound()
        {
            const int rate = 22050, count = rate * 6, overlap = rate / 2;
            var rng = new System.Random(2207);
            var data = new float[count + overlap];
            float low = 0f, band = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float white = (float)rng.NextDouble() * 2f - 1f;
                low += .08f * (white - low);
                band += .3f * (white - band);
                data[i] = low * .22f + (band - low) * .05f;
            }
            int bubbles = (int)(data.Length / (float)rate * 30f);
            for (int n = 0; n < bubbles; n++)
            {
                int start = rng.Next(data.Length);
                float frequency = 350f + (float)rng.NextDouble() * 850f;
                float duration = .02f + (float)rng.NextDouble() * .05f;
                float amplitude = .04f + (float)rng.NextDouble() * .08f;
                int length = (int)(duration * rate * 3f);
                float phase = 0f;
                for (int k = 0; k < length && start + k < data.Length; k++)
                {
                    float tau = k / (float)rate;
                    float pitch = frequency * (1f + 1.5f * tau / duration);
                    phase += 2f * Mathf.PI * pitch / rate;
                    float envelope = Mathf.Exp(-tau / duration * 3f) * Mathf.Min(1f, tau * 400f);
                    data[start + k] += Mathf.Sin(phase) * envelope * amplitude;
                }
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
            var result = AudioClip.Create("Brook", count, 1, rate, false);
            result.SetData(loop, 0);
            return result;
        }
    }
}
