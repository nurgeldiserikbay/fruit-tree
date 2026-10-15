using System;
using System.Collections;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Switches eight low-cost environments in one scene, with no extra cameras,
    /// colliders, particle systems or per-frame allocation.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class EnvironmentController : MonoBehaviour
    {
        private const string PreferenceKey = "FruitTree.EnvironmentTheme";
        public GroundGenerator ground;
        public Renderer skyRenderer;
        public Light sun;
        [Tooltip("Optional 2:1 equirectangular textures in theme order. Empty entries use the procedural sky.")]
        public Texture2D[] panoramas = new Texture2D[EnvironmentThemes.Count];

        private Material sceneryMaterial;
        private Material skyMaterial;
        private Mesh sceneryMesh;
        private readonly Mesh[] themeMeshes = new Mesh[EnvironmentThemes.Count];
        private readonly Texture2D[] cachedPanoramas = new Texture2D[EnvironmentThemes.Count];
        private readonly bool[] panoramaLookedUp = new bool[EnvironmentThemes.Count];
        public event Action<EnvironmentTheme> ThemeChanged;
        private MeshFilter sceneryFilter;
        private int treeIndex;
        private int overrideIndex = -1;
        private Coroutine paletteTransition;
        private Palette displayedPalette;
        private bool paletteInitialized;

        // This immutable value bundle changes only when a theme is selected.
        private struct Palette
        {
            public Color ground, grass, horizon, zenith, sun, ambient;
            public float sunIntensity, hazeStrength;

            public static Palette FromTheme(EnvironmentTheme theme)
            {
                Color horizon = EnvironmentThemes.Horizon(theme);
                return new Palette
                {
                    ground = EnvironmentThemes.GroundTint(theme),
                    grass = EnvironmentThemes.GrassTint(theme),
                    horizon = horizon,
                    zenith = EnvironmentThemes.Zenith(theme),
                    sun = EnvironmentThemes.Sun(theme),
                    ambient = Color.Lerp(horizon, Color.white, .14f),
                    hazeStrength = theme == EnvironmentTheme.Mountains ? .20f :
                        theme == EnvironmentTheme.EveningGarden ? .55f : .35f,
                    sunIntensity = theme == EnvironmentTheme.EveningGarden ? .66f :
                        theme == EnvironmentTheme.CityPark ? .96f : 1.08f
                };
            }

            public static Palette Lerp(Palette from, Palette to, float t)
            {
                return new Palette
                {
                    ground = Color.Lerp(from.ground, to.ground, t),
                    grass = Color.Lerp(from.grass, to.grass, t),
                    horizon = Color.Lerp(from.horizon, to.horizon, t),
                    zenith = Color.Lerp(from.zenith, to.zenith, t),
                    sun = Color.Lerp(from.sun, to.sun, t),
                    ambient = Color.Lerp(from.ambient, to.ambient, t),
                    sunIntensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t),
                    hazeStrength = Mathf.Lerp(from.hazeStrength, to.hazeStrength, t)
                };
            }
        }

        public EnvironmentTheme CurrentTheme { get; private set; }
        public bool IsAutomatic => overrideIndex < 0;
        public int CurrentSceneryVertices => sceneryMesh != null ? sceneryMesh.vertexCount : 0;
        public int CurrentSceneryTriangles => sceneryMesh != null ? sceneryMesh.triangles.Length / 3 : 0;

        private void Awake()
        {
            if (ground == null) ground = GetComponent<GroundGenerator>();
            if (skyRenderer != null && skyRenderer.sharedMaterial != null)
            {
                skyMaterial = new Material(skyRenderer.sharedMaterial) { name = "Runtime Theme Sky" };
                skyRenderer.sharedMaterial = skyMaterial;
            }
            Shader shader = Shader.Find("FruitTree/VertexLit") ?? Shader.Find("Standard");
            sceneryMaterial = new Material(shader) { name = "Merged Background Landmarks" };
            if (sceneryMaterial.HasProperty("_Wind")) sceneryMaterial.SetFloat("_Wind", 0f);
            if (sceneryMaterial.HasProperty("_AmbientLight"))
                sceneryMaterial.SetFloat("_AmbientLight", .76f);
            if (sceneryMaterial.HasProperty("_Cull")) sceneryMaterial.SetFloat("_Cull", 0f);

            var scenery = new GameObject("Merged Theme Scenery", typeof(MeshFilter),
                typeof(MeshRenderer));
            scenery.transform.SetParent(transform, false);
            sceneryFilter = scenery.GetComponent<MeshFilter>();
            var renderer = scenery.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = sceneryMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            int stored = PlayerPrefs.GetInt(PreferenceKey, -1);
            overrideIndex = EnvironmentThemes.IsValid(stored) ? stored : -1;
            // GroundGenerator.Awake may not have run yet: wait until all Awake
            // calls finish before applying material tints or building decoration.
        }

        private void Start()
        {
            if (sceneryMesh == null) Apply();
        }

        public void UseForTree(int index)
        {
            treeIndex = Mathf.Max(0, index);
            Apply();
        }

        public void CycleScenery()
        {
            // Automatic -> eight themes -> Automatic; old saved values retain meaning.
            overrideIndex = overrideIndex == EnvironmentThemes.Count - 1 ?
                -1 : overrideIndex + 1;
            PlayerPrefs.SetInt(PreferenceKey, overrideIndex);
            PlayerPrefs.Save();
            Apply();
        }

        public void ApplyThemeDirect(EnvironmentTheme theme)
        {
            // For diagnostics and scene tests; does not write player preferences.
            BuildTheme(theme);
        }

        private void Apply()
        {
            EnvironmentTheme chosen = overrideIndex < 0 ?
                EnvironmentThemes.DefaultForTree(treeIndex) : (EnvironmentTheme)overrideIndex;
            BuildTheme(chosen);
        }

        private void BuildTheme(EnvironmentTheme theme)
        {
            if (!EnvironmentThemes.IsValid((int)theme))
                throw new ArgumentOutOfRangeException(nameof(theme));
            if (sceneryMesh != null && CurrentTheme == theme) return;
            CurrentTheme = theme;
            ApplyPanorama(theme);
            if (paletteTransition != null)
            {
                StopCoroutine(paletteTransition);
                paletteTransition = null;
            }
            Palette destination = Palette.FromTheme(theme);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            // Ground mesh ends at 35m along its nearest edges. Full fog must
            // take effect before that edge or the panorama shows a sharp,
            // horizontal green cut-off on portrait devices.
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 33f;
            // Initial scene preparation must be immediate (including Editor tests).
            // Subsequent theme selections gently blend color over one second.
            if (!paletteInitialized || !Application.isPlaying || !isActiveAndEnabled ||
                PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0) == 1)
                ApplyPalette(destination);
            else
                paletteTransition = StartCoroutine(BlendPalette(displayedPalette, destination));

            int index = (int)theme;
            // Generate only on first visit; later switching reuses the same small
            // GPU/CPU buffers. Only one theme is displayed by the single renderer.
            if (themeMeshes[index] == null)
                themeMeshes[index] = CreateSceneryMesh(theme);
            sceneryMesh = themeMeshes[index];
            if (sceneryFilter != null) sceneryFilter.sharedMesh = sceneryMesh;
            ThemeChanged?.Invoke(theme);
        }

        /// <summary>
        /// A gentle change of light, sky, fog and grass without new meshes.
        /// Always uses unscaled time: Settings can be open while the game is paused.
        /// </summary>
        private IEnumerator BlendPalette(Palette beginning, Palette ending)
        {
            const float duration = 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                if (PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0) == 1)
                    break;
                ApplyPalette(Palette.Lerp(beginning, ending,
                    TransitionWeight(elapsed / duration)));
                yield return null;
            }
            ApplyPalette(ending);
            paletteTransition = null;
        }

        public static float TransitionWeight(float progress)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        }

        public static bool IsValidPanorama(Texture2D texture)
        {
            return texture != null && texture.height > 0 &&
                texture.width == texture.height * 2;
        }

        // A 2:1 image is not necessarily a seamless 360-degree panorama.
        // Reject visibly discontinuous seams when CPU pixels are available.
        // Non-readable compressed Android textures cannot be inspected here.
        public static bool HasAcceptablePanoramaSeam(Texture2D texture)
        {
            if (!IsValidPanorama(texture) || !texture.isReadable) return true;
            float difference = 0f;
            const int samples = 16;
            for (int i = 0; i < samples; i++)
            {
                float v = (i + .5f) / samples;
                Color left = texture.GetPixelBilinear(0f, v);
                Color right = texture.GetPixelBilinear(1f, v);
                difference += (Mathf.Abs(left.r - right.r) +
                    Mathf.Abs(left.g - right.g) +
                    Mathf.Abs(left.b - right.b)) / 3f;
            }
            return difference / samples < .12f;
        }

        private void ApplyPanorama(EnvironmentTheme theme)
        {
            if (skyMaterial == null || !skyMaterial.HasProperty("_PanoramaBlend"))
                return;
            int index = (int)theme;
            Texture2D panorama = panoramas != null && index < panoramas.Length
                ? panoramas[index] : null;
            if (panorama == null)
            {
                // Missing Resources lookups are cached too; cycling biomes
                // must not hit the Resources index on every selection.
                if (!panoramaLookedUp[index])
                {
                    cachedPanoramas[index] = Resources.Load<Texture2D>(
                        "Panoramas/" + theme);
                    panoramaLookedUp[index] = true;
                }
                panorama = cachedPanoramas[index];
            }
            if (panorama != null && !IsValidPanorama(panorama))
            {
                Debug.LogWarning("Fruit Tree: " + theme +
                    " panorama requires a 2:1 equirectangular image.");
                panorama = null;
            }
            if (panorama != null && !HasAcceptablePanoramaSeam(panorama))
            {
                Debug.LogWarning("Fruit Tree: " + theme +
                    " panorama has a visible 360-degree seam; using gradient sky.");
                panorama = null;
            }
            if (panorama != null)
            {
                // Equirectangular longitude must join without a visible seam
                // when the camera rotates through 360 degrees.
                if (panorama.wrapModeU != TextureWrapMode.Repeat)
                    panorama.wrapModeU = TextureWrapMode.Repeat;
                if (panorama.wrapModeV != TextureWrapMode.Clamp)
                    panorama.wrapModeV = TextureWrapMode.Clamp;
                skyMaterial.SetTexture("_Panorama", panorama);
            }
            else
                skyMaterial.SetTexture("_Panorama", Texture2D.whiteTexture);
            skyMaterial.SetFloat("_PanoramaBlend", panorama == null ? 0f : 1f);
        }

        private void ApplyPalette(Palette palette)
        {
            displayedPalette = palette;
            paletteInitialized = true;
            if (ground != null) ground.SetPalette(palette.ground, palette.grass);
            if (skyMaterial != null)
            {
                if (skyMaterial.HasProperty("_Horizon"))
                    skyMaterial.SetColor("_Horizon", palette.horizon);
                if (skyMaterial.HasProperty("_Zenith"))
                    skyMaterial.SetColor("_Zenith", palette.zenith);
                if (skyMaterial.HasProperty("_HazeStrength"))
                    skyMaterial.SetFloat("_HazeStrength", palette.hazeStrength);
            }
            if (sun != null)
            {
                sun.color = palette.sun;
                sun.intensity = palette.sunIntensity;
            }
            RenderSettings.fogColor = palette.horizon;
            RenderSettings.ambientLight = palette.ambient;
        }

        public static Mesh CreateSceneryMesh(EnvironmentTheme theme)
        {
            var mesh = new EnvironmentMeshBuilder();
            switch (theme)
            {
                case EnvironmentTheme.Park: BuildPark(mesh); break;
                case EnvironmentTheme.CityPark: BuildCityPark(mesh); break;
                case EnvironmentTheme.Mountains: BuildMountains(mesh); break;
                case EnvironmentTheme.JapaneseGarden: EnvironmentGardenBuilder.Japanese(mesh); break;
                case EnvironmentTheme.ChineseGarden: EnvironmentGardenBuilder.Chinese(mesh); break;
                case EnvironmentTheme.Lakeside: EnvironmentGardenBuilder.Lakeside(mesh); break;
                case EnvironmentTheme.EveningGarden: EnvironmentGardenBuilder.Evening(mesh); break;
                default: BuildMeadow(mesh); break;
            }
            return mesh.Build("Merged Scenery - " + theme);
        }

        private static void Wildflowers(EnvironmentMeshBuilder mesh, int count, int seed,
            float innerRadius, float outerRadius, Color color)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(innerRadius, outerRadius,
                    Mathf.Sqrt((float)random.NextDouble()));
                Vector3 center = new Vector3(Mathf.Cos(angle) * radius, .035f,
                    Mathf.Sin(angle) * radius);
                mesh.FlowerPatch(center, .085f + .055f * (float)random.NextDouble(),
                    5, color * (.82f + .2f * (float)random.NextDouble()),
                    new Color(.48f, .60f, .28f));
            }
        }

        private static void BackgroundTree(EnvironmentMeshBuilder mesh, float x, float z,
            float size, Color foliage)
        {
            mesh.Box(new Vector3(x, .6f * size, z),
                new Vector3(.17f * size, 1.2f * size, .17f * size),
                new Color(.44f, .36f, .25f));
            mesh.Cone(new Vector3(x, 1.04f * size, z), size * .51f,
                size * 1.65f, 6, foliage);
        }

        private static void BuildMeadow(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(27f, -.12f, 1.35f, 40, .3f,
                new Color(.60f, .74f, .59f));
            mesh.RidgeBand(32f, -.12f, 2.0f, 36, 1.5f,
                new Color(.68f, .79f, .67f));
            Wildflowers(mesh, 58, 171, 5.7f, 15f, new Color(.96f, .83f, .56f));
            Wildflowers(mesh, 24, 208, 8f, 17f, new Color(.90f, .84f, .92f));
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + .1f;
                BackgroundTree(mesh, Mathf.Cos(a) * 20f, Mathf.Sin(a) * 20f,
                    .55f + (i % 3) * .16f, new Color(.48f, .67f, .43f));
            }
        }

        private static void Bench(EnvironmentMeshBuilder mesh, float x, float z, float rotation)
        {
            // A readable silhouette made from four cuboids, far outside the fruit radius.
            mesh.Box(new Vector3(x, .53f, z), new Vector3(1.55f, .14f, .54f),
                new Color(.58f, .42f, .31f));
            mesh.Box(new Vector3(x, .88f, z + .28f), new Vector3(1.55f, .58f, .13f),
                new Color(.66f, .48f, .35f));
            mesh.Box(new Vector3(x - .54f, .26f, z), new Vector3(.16f, .53f, .15f),
                new Color(.39f, .40f, .36f));
            mesh.Box(new Vector3(x + .54f, .26f, z), new Vector3(.16f, .53f, .15f),
                new Color(.39f, .40f, .36f));
        }

        private static void ParkLantern(EnvironmentMeshBuilder mesh, float x, float z)
        {
            // Static silhouette: narrow pillar and one pale lantern head.
            // It is deliberately away from the central picking/orbit space.
            mesh.Box(new Vector3(x, 1.25f, z), new Vector3(.09f, 2.5f, .09f),
                new Color(.39f, .43f, .38f));
            mesh.Box(new Vector3(x, 2.53f, z), new Vector3(.27f, .30f, .27f),
                new Color(.93f, .89f, .71f));
            mesh.Cone(new Vector3(x, 2.69f, z), .19f, .16f, 5,
                new Color(.42f, .46f, .39f));
        }

        private static void BuildPark(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(28f, -.11f, 1.15f, 36, 1f,
                new Color(.58f, .73f, .58f));
            mesh.RingPath(9.6f, .65f, 48, new Color(.78f, .73f, .59f));
            Bench(mesh, -12.2f, 2.5f, 0f);
            Bench(mesh, 12.2f, -2.5f, 0f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 2f * Mathf.PI / 6f + .35f;
                ParkLantern(mesh, Mathf.Cos(a) * 13.8f,
                    Mathf.Sin(a) * 13.8f);
            }
            Wildflowers(mesh, 46, 992, 10.6f, 17f, new Color(.94f, .74f, .75f));
            for (int i = 0; i < 12; i++)
            {
                float a = (i + .3f) * Mathf.PI * 2f / 12f;
                BackgroundTree(mesh, Mathf.Cos(a) * 19f, Mathf.Sin(a) * 19f,
                    .78f + (i % 4) * .13f, new Color(.34f, .62f, .42f));
            }
        }

        private static void BuildCityPark(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(33f, -.15f, 1.45f, 40, .15f,
                new Color(.73f, .77f, .75f));
            mesh.RingPath(9.8f, .7f, 48, new Color(.72f, .71f, .67f));
            Wildflowers(mesh, 24, 285, 10.7f, 16f, new Color(.92f, .84f, .65f));
            Bench(mesh, -12.2f, 3.3f, 0f);
            var blocks = new Color[]
            {
                new Color(.74f, .79f, .82f), new Color(.79f, .75f, .72f),
                new Color(.63f, .72f, .75f)
            };
            for (int i = 0; i < 17; i++)
            {
                float a = (i + .1f) * Mathf.PI * 2f / 17f;
                float radius = 24f + (i % 3) * .5f;
                float height = 3f + (i % 5) * 1.05f;
                Vector3 basePosition = new Vector3(Mathf.Cos(a) * radius,
                    0f, Mathf.Sin(a) * radius);
                mesh.Box(basePosition + Vector3.up * (height * .5f),
                    new Vector3(1.8f, height, 1.85f), blocks[i % blocks.Length]);
                // Three decorative windows on the wall facing the park;
                // vertices are part of the same mesh as the skyline.
                Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 inside = new Vector3(-Mathf.Cos(a), 0f, -Mathf.Sin(a));
                for (int floor = 0; floor < 3; floor++)
                {
                    float y = .9f + floor * .85f;
                    if (y >= height - .25f) continue;
                    Vector3 window = basePosition + inside * 1.35f +
                        Vector3.up * y;
                    float halfWidth = .22f, halfHeight = .16f;
                    mesh.Quad(window - tangent * halfWidth - Vector3.up * halfHeight,
                        window + tangent * halfWidth - Vector3.up * halfHeight,
                        window + tangent * halfWidth + Vector3.up * halfHeight,
                        window - tangent * halfWidth + Vector3.up * halfHeight,
                        floor % 2 == 0 ? new Color(.84f, .88f, .79f) :
                            new Color(.67f, .78f, .84f));
                }
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                BackgroundTree(mesh, Mathf.Cos(a) * 17.5f,
                    Mathf.Sin(a) * 17.5f, .7f, new Color(.33f, .59f, .41f));
            }
        }

        private static void BuildMountains(EnvironmentMeshBuilder mesh)
        {
            mesh.RidgeBand(33f, -.2f, 7.2f, 44, .6f,
                new Color(.69f, .79f, .80f));
            mesh.RidgeBand(28f, -.15f, 4.45f, 40, 1.3f,
                new Color(.56f, .71f, .69f));
            Wildflowers(mesh, 45, 439, 5.9f, 16f, new Color(.92f, .84f, .68f));
            for (int i = 0; i < 20; i++)
            {
                float a = (i + .22f) * Mathf.PI * 2f / 20f;
                float radius = 27f + (i % 3) * .7f;
                float height = 4.1f + (i % 5) * 1.05f;
                Color stone = i % 3 == 0 ? new Color(.68f, .72f, .70f) :
                    new Color(.55f, .67f, .66f);
                mesh.Cone(new Vector3(Mathf.Cos(a) * radius, -.25f,
                    Mathf.Sin(a) * radius), 3.4f, height, 5, stone);
                if (i % 3 == 0)
                    mesh.Cone(new Vector3(Mathf.Cos(a) * radius,
                        height * .67f, Mathf.Sin(a) * radius),
                        1.06f, height * .34f, 5, new Color(.90f, .94f, .90f));
            }
            for (int i = 0; i < 12; i++)
            {
                float a = (i + .4f) * Mathf.PI * 2f / 12f;
                BackgroundTree(mesh, Mathf.Cos(a) * 18f,
                    Mathf.Sin(a) * 18f, .67f + (i % 3) * .16f,
                    new Color(.33f, .54f, .48f));
            }
        }

        private void OnDestroy()
        {
            if (paletteTransition != null) StopCoroutine(paletteTransition);
            for (int i = 0; i < themeMeshes.Length; i++)
            {
                if (themeMeshes[i] == null) continue;
                if (Application.isPlaying) Destroy(themeMeshes[i]);
                else DestroyImmediate(themeMeshes[i]);
                themeMeshes[i] = null;
            }
            sceneryMesh = null;
            if (sceneryMaterial != null)
            {
                if (Application.isPlaying) Destroy(sceneryMaterial);
                else DestroyImmediate(sceneryMaterial);
            }
            if (skyMaterial != null)
            {
                if (Application.isPlaying) Destroy(skyMaterial);
                else DestroyImmediate(skyMaterial);
            }
        }
    }
}
