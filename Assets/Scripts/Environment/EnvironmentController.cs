using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Switches four low-cost environments in one scene, with no extra cameras,
    /// colliders, particle systems or per-frame allocation.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class EnvironmentController : MonoBehaviour
    {
        private const string PreferenceKey = "FruitTree.EnvironmentTheme";
        public GroundGenerator ground;
        public Renderer skyRenderer;
        public Light sun;

        private Material sceneryMaterial;
        private Material skyMaterial;
        private Mesh sceneryMesh;
        private MeshFilter sceneryFilter;
        private int treeIndex;
        private int overrideIndex = -1;

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
            Apply();
        }

        public void UseForTree(int index)
        {
            treeIndex = Mathf.Max(0, index);
            Apply();
        }

        public void CycleScenery()
        {
            // Automatic -> Meadow -> Park -> City Park -> Mountains -> Automatic.
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
            CurrentTheme = theme;
            if (ground != null)
                ground.SetPalette(EnvironmentThemes.GroundTint(theme),
                    EnvironmentThemes.GrassTint(theme));
            if (skyMaterial != null)
            {
                if (skyMaterial.HasProperty("_Horizon"))
                    skyMaterial.SetColor("_Horizon", EnvironmentThemes.Horizon(theme));
                if (skyMaterial.HasProperty("_Zenith"))
                    skyMaterial.SetColor("_Zenith", EnvironmentThemes.Zenith(theme));
            }
            if (sun != null)
            {
                sun.color = EnvironmentThemes.Sun(theme);
                sun.intensity = theme == EnvironmentTheme.CityPark ? .96f : 1.08f;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = EnvironmentThemes.Horizon(theme);
            RenderSettings.fogStartDistance = 19f;
            RenderSettings.fogEndDistance = 45f;
            RenderSettings.ambientLight = Color.Lerp(EnvironmentThemes.Horizon(theme),
                Color.white, .14f);

            Mesh next = CreateSceneryMesh(theme);
            if (sceneryFilter != null) sceneryFilter.sharedMesh = next;
            if (sceneryMesh != null)
            {
                if (Application.isPlaying) Destroy(sceneryMesh);
                else DestroyImmediate(sceneryMesh);
            }
            sceneryMesh = next;
        }

        public static Mesh CreateSceneryMesh(EnvironmentTheme theme)
        {
            var mesh = new EnvironmentMeshBuilder();
            switch (theme)
            {
                case EnvironmentTheme.Park: BuildPark(mesh); break;
                case EnvironmentTheme.CityPark: BuildCityPark(mesh); break;
                case EnvironmentTheme.Mountains: BuildMountains(mesh); break;
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

        private static void BuildPark(EnvironmentMeshBuilder mesh)
        {
            mesh.RingPath(9.6f, .65f, 48, new Color(.78f, .73f, .59f));
            Bench(mesh, -12.2f, 2.5f, 0f);
            Bench(mesh, 12.2f, -2.5f, 0f);
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
                mesh.Box(new Vector3(Mathf.Cos(a) * radius, height * .5f,
                    Mathf.Sin(a) * radius), new Vector3(1.8f, height, 1.85f),
                    blocks[i % blocks.Length]);
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
            if (sceneryMesh != null)
            {
                if (Application.isPlaying) Destroy(sceneryMesh);
                else DestroyImmediate(sceneryMesh);
            }
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
