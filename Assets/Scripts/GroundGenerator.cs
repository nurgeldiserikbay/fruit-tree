using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>A quiet low-poly meadow; two meshes and no individual grass GameObjects.</summary>
    public sealed class GroundGenerator : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> generated = new List<UnityEngine.Object>();
        private Material meadowMaterial;
        private Material bladesMaterial;
        private Color appliedGroundTint = Color.white;
        private Color appliedGrassTint = Color.white;

        public void SetPalette(Color meadowTint, Color grassTint)
        {
            // Theme may arrive before GroundGenerator.Awake. Cache it so the
            // runtime materials always receive the same chosen palette.
            appliedGroundTint = meadowTint;
            appliedGrassTint = grassTint;
            ApplyStoredPalette();
        }

        private void ApplyStoredPalette()
        {
            if (meadowMaterial != null && meadowMaterial.HasProperty("_Color"))
                meadowMaterial.SetColor("_Color", appliedGroundTint);
            if (bladesMaterial != null && bladesMaterial.HasProperty("_Color"))
                bladesMaterial.SetColor("_Color", appliedGrassTint);
        }

        private void Awake()
        {
            var shader = Shader.Find("FruitTree/VertexLit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "Meadow" };
            var grassMaterial = new Material(shader) { name = "Grass Blades" };
            if (grassMaterial.HasProperty("_Wind")) grassMaterial.SetFloat("_Wind", .018f);
            if (material.HasProperty("_AmbientLight")) material.SetFloat("_AmbientLight", .67f);
            if (grassMaterial.HasProperty("_AmbientLight")) grassMaterial.SetFloat("_AmbientLight", .70f);
            if (grassMaterial.HasProperty("_Cull")) grassMaterial.SetFloat("_Cull", 0f);
            meadowMaterial = material;
            bladesMaterial = grassMaterial;
            ApplyStoredPalette();
            generated.Add(material);
            generated.Add(grassMaterial);
            Create("Meadow Terrain", CreateTerrainMesh(), material);
            Create("Merged Grass", CreateGrassMesh(), grassMaterial);
            CreateIllustratedGrass();
        }

        // Art pack path: Assets/Art/Vegetation/Grass/grass_01.png ...
        // Resources copies are preferred for player builds. In Editor Play Mode
        // load the original Art folder directly, so merely importing the ZIP
        // immediately changes the scene without rebuilding serialized scenes.
        private void CreateIllustratedGrass()
        {
            const int variants = 10;
            for (int variant = 0; variant < variants; variant++)
            {
                string filename = "grass_" + (variant + 1).ToString("00");
                Texture2D texture = Resources.Load<Texture2D>(
                    "Vegetation/Grass/" + filename);
#if UNITY_EDITOR
                if (texture == null)
                    texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                        "Assets/Art/Vegetation/Grass/" + filename + ".png");
#endif
                if (texture == null) continue;
                var shader = Shader.Find("Unlit/Transparent Cutout");
                if (shader == null) shader = Shader.Find("Unlit/Transparent");
                if (shader == null) continue;
                var material = new Material(shader) { name = "Grass Card " + filename };
                material.mainTexture = texture;
                if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", .38f);
                generated.Add(material);
                Create("Illustrated " + filename,
                    CreateGrassCards(variant), material);
            }
        }

        private static Mesh CreateGrassCards(int variant)
        {
            var rng = new System.Random(2400 + variant * 31);
            // Dense, smaller tufts create ground cover rather than isolated bushes.
            const int count = 105;
            var vertices = new List<Vector3>(count * 8);
            var uvs = new List<Vector2>(count * 8);
            var triangles = new List<int>(count * 12);
            for (int i = 0; i < count; i++)
            {
                // Organic clusters separated by short clearings. Most
                // grass is low ground cover; occasional taller accents avoid
                // the previous look of evenly scattered miniature bushes.
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(2.0f, 17f,
                    Mathf.Sqrt((float)rng.NextDouble()));
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float patch = Mathf.PerlinNoise(x * .18f + 27f,
                    z * .18f + 39f);
                // Reposition points in bare patches into their neighbours,
                // keeping the exact vertex and draw-call budget unchanged.
                if (patch < .39f)
                {
                    x += 1.25f * Mathf.Cos(angle + 1.1f);
                    z += 1.25f * Mathf.Sin(angle + 1.1f);
                }
                float heightVariation = (float)rng.NextDouble();
                float scale = Mathf.Lerp(.12f, .32f, heightVariation);
                if (variant == 4 || variant == 6 || variant == 7 ||
                    variant == 8) scale *= .78f;
                // Reserve a subtle open patch around the main trunk.
                float rootDistance = Mathf.Sqrt(x * x + z * z);
                if (rootDistance < 2.0f) scale *= .55f;
                float y = -.055f + (Mathf.PerlinNoise(
                    x * .33f + 34f, z * .33f + 12f) - .5f) * .13f *
                    Mathf.Clamp01((radius - 1f) / 3f);
                // Two perpendicular cutout cards produce a full tuft
                // from every orbit angle without a billboard Update().
                for (int card = 0; card < 2; card++)
                {
                    float orientation = angle + card * Mathf.PI * .5f;
                    Vector3 sideways = new Vector3(
                        Mathf.Cos(orientation), 0f, Mathf.Sin(orientation));
                    Vector3 basePoint = new Vector3(x, y, z);
                    int start = vertices.Count;
                    vertices.Add(basePoint - sideways * scale * .5f);
                    vertices.Add(basePoint + sideways * scale * .5f);
                    vertices.Add(basePoint - sideways * scale * .5f + Vector3.up * scale);
                    vertices.Add(basePoint + sideways * scale * .5f + Vector3.up * scale);
                    uvs.Add(new Vector2(0f, 0f));
                    uvs.Add(new Vector2(1f, 0f));
                    uvs.Add(new Vector2(0f, 1f));
                    uvs.Add(new Vector2(1f, 1f));
                    // Double-sided triangles; reliable on mobile cutout shaders.
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                    triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start + 3);
                    triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start);
                    triangles.Add(start + 3); triangles.Add(start + 2); triangles.Add(start + 1);
                }
            }
            var mesh = new Mesh { name = "Illustrated Grass Cards " + variant };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Create(string label, Mesh mesh, Material material)
        {
            generated.Add(mesh);
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void OnDestroy()
        {
            foreach (var asset in generated)
            {
                if (asset == null) continue;
                if (Application.isPlaying) Destroy(asset);
                else DestroyImmediate(asset);
            }
            generated.Clear();
        }

        // Soft baked canopy footprint: no shadow map or extra renderer.
        // Kept close to the trunk so it reads naturally across all biomes.
        public static float OrchardContactShade(float x, float z)
        {
            float trunk = Mathf.Sqrt(x * x + z * z);
            float core = 1f - Mathf.SmoothStep(.25f, 2.2f, trunk);
            // A faint offset penumbra suggests low-angle sun without a hard disk.
            float dx = (x + .65f) / 3.0f;
            float dz = (z - .42f) / 2.15f;
            float canopy = 1f - Mathf.SmoothStep(.35f, 1.0f,
                Mathf.Sqrt(dx * dx + dz * dz));
            float noise = Mathf.PerlinNoise(x * 1.6f + 13f, z * 1.6f + 19f);
            return Mathf.Clamp01(.17f * core + .075f * canopy * (.65f + .35f * noise));
        }

        public static Mesh CreateTerrainMesh()
        {
            const int divisions = 64;
            const float size = 70f;
            var vertices = new List<Vector3>((divisions + 1) * (divisions + 1));
            var colors = new List<Color>(vertices.Capacity);
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(divisions * divisions * 6);
            for (int z = 0; z <= divisions; z++)
                for (int x = 0; x <= divisions; x++)
                {
                    float px = (x / (float)divisions - .5f) * size;
                    float pz = (z / (float)divisions - .5f) * size;
                    float distance = Mathf.Sqrt(px * px + pz * pz);
                    float outerBump = Mathf.Clamp01((distance - 1f) / 3f);
                    float noise = Mathf.PerlinNoise(px * .33f + 34f, pz * .33f + 12f);
                    vertices.Add(new Vector3(px, -.055f + (noise - .5f) * .13f * outerBump, pz));
                    float variation = Mathf.PerlinNoise(px * .24f + 91f, pz * .24f + 16f);
                    float fine = Mathf.PerlinNoise(px * .91f + 57f, pz * .91f + 43f);
                    float shade = .82f + variation * .12f + fine * .06f;
                    // A soft built-in contact shade grounds the trunk even on
                    // Android tiers without real-time shadow mapping.
                    float contact = 1f - Mathf.SmoothStep(.5f, 3.3f, distance);
                    shade *= 1f - OrchardContactShade(px, pz);
                    float clearing = 1f - Mathf.SmoothStep(1.1f, 3.8f, distance);
                    // Broad warm/cool patches read as real turf instead of
                    // a perfectly uniform green floor. Vertex colors keep
                    // this texture-free and cost nothing per frame.
                    float earth = Mathf.PerlinNoise(px * .12f + 7f, pz * .12f + 51f);
                    float dry = Mathf.SmoothStep(.48f, .84f, earth) * .24f;
                    Color turf = Color.Lerp(
                        new Color(.38f, .58f, .30f),
                        new Color(.54f, .54f, .30f), dry);
                    float edge = Mathf.SmoothStep(14f, 33f, distance);
                    Color farTurf = new Color(.55f, .65f, .49f);
                    Color groundColor = Color.Lerp(turf * shade,
                        farTurf * (.88f + variation * .10f), edge * .55f);
                    colors.Add(Color.Lerp(groundColor,
                        new Color(.48f, .61f, .37f) * (1f - contact * .12f),
                        clearing * .20f));
                    uv.Add(new Vector2(x / (float)divisions, z / (float)divisions));
                }
            for (int z = 0; z < divisions; z++)
                for (int x = 0; x < divisions; x++)
                {
                    int a = z * (divisions + 1) + x;
                    int b = a + 1, c = a + divisions + 1, d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            var mesh = new Mesh { name = "Rolling Meadow" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateGrassMesh()
        {
            // Dense but short grass avoids the sparse sharp spikes seen in the
            // first Unity screenshots. Still one merged mesh/renderer.
            var random = new System.Random(902);
            const int bladeCount = 900;
            var vertices = new List<Vector3>(bladeCount * 5);
            var colors = new List<Color>(bladeCount * 5);
            var uv = new List<Vector2>(bladeCount * 5);
            var triangles = new List<int>(bladeCount * 12);
            for (int i = 0; i < bladeCount; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * 8.7f;
                if (radius < .8f) continue;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float noise = Mathf.PerlinNoise(x * .33f + 34f, z * .33f + 12f);
                float y = -.055f + (noise - .5f) * .13f *
                    Mathf.Clamp01((radius - 1f) / 3f);
                float height = .038f + .052f * (float)random.NextDouble();
                float width = .013f + .018f * (float)random.NextDouble();
                float rot = (float)random.NextDouble() * Mathf.PI * 2f;
                Vector3 side = new Vector3(Mathf.Cos(rot), 0f, Mathf.Sin(rot));
                Vector3 lean = new Vector3(-side.z, 0f, side.x) * height * .30f;
                Vector3 basePos = new Vector3(x, y, z);
                Vector3 middle = basePos + Vector3.up * (height * .52f) + lean * .32f;
                int first = vertices.Count;
                vertices.Add(basePos - side * width);
                vertices.Add(basePos + side * width);
                vertices.Add(middle - side * width * .61f);
                vertices.Add(middle + side * width * .61f);
                vertices.Add(basePos + Vector3.up * height + lean);
                float tone = (float)random.NextDouble();
                var leaf = new Color(.34f + .09f * tone,
                    .53f + .12f * tone, .25f + .08f * tone);
                colors.Add(leaf * .80f);
                colors.Add(leaf * .80f);
                colors.Add(leaf * .95f);
                colors.Add(leaf * .95f);
                colors.Add(leaf * 1.12f);
                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(1f, 0f));
                uv.Add(new Vector2(.2f, .52f));
                uv.Add(new Vector2(.8f, .52f));
                uv.Add(new Vector2(.5f, 1f));
                triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 1);
                triangles.Add(first + 1); triangles.Add(first + 2); triangles.Add(first + 3);
                triangles.Add(first + 2); triangles.Add(first + 4); triangles.Add(first + 3);
                triangles.Add(first + 3); triangles.Add(first + 4); triangles.Add(first + 2);
            }
            var mesh = new Mesh { name = "Soft Curved Grass" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
