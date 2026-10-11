using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>A quiet low-poly meadow; two meshes and no individual grass GameObjects.</summary>
    public sealed class GroundGenerator : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> generated = new List<UnityEngine.Object>();

        private void Awake()
        {
            var shader = Shader.Find("FruitTree/VertexLit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "Meadow" };
            var grassMaterial = new Material(shader) { name = "Grass Blades" };
            if (grassMaterial.HasProperty("_Wind")) grassMaterial.SetFloat("_Wind", .018f);
            if (grassMaterial.HasProperty("_Cull")) grassMaterial.SetFloat("_Cull", 0f);
            generated.Add(material);
            generated.Add(grassMaterial);
            Create("Meadow Terrain", CreateTerrainMesh(), material);
            Create("Merged Grass", CreateGrassMesh(), grassMaterial);
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
                    float variation = Mathf.PerlinNoise(px * .47f + 91f, pz * .47f + 16f);
                    colors.Add(new Color(.40f + variation * .075f, .57f + variation * .085f, .29f + variation * .04f));
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
            var random = new System.Random(902);
            const int bladeCount = 315;
            var vertices = new List<Vector3>(bladeCount * 3);
            var colors = new List<Color>(bladeCount * 3);
            var uv = new List<Vector2>(bladeCount * 3);
            var triangles = new List<int>(bladeCount * 3);
            for (int i = 0; i < bladeCount; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * 5.6f;
                if (radius < .65f) continue;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float noise = Mathf.PerlinNoise(x * .33f + 34f, z * .33f + 12f);
                float y = -.055f + (noise - .5f) * .18f * Mathf.Clamp01((radius - 1f) / 3f);
                float height = .06f + .10f * (float)random.NextDouble();
                float width = .04f + .03f * (float)random.NextDouble();
                float rot = (float)random.NextDouble() * Mathf.PI * 2f;
                Vector3 side = new Vector3(Mathf.Cos(rot), 0f, Mathf.Sin(rot)) * width;
                Vector3 origin = new Vector3(x, y, z);
                int first = vertices.Count;
                vertices.Add(origin - side);
                vertices.Add(origin + side);
                vertices.Add(origin + new Vector3(side.z * 1.3f, height, -side.x * 1.3f));
                var green = new Color(.29f + .09f * (float)random.NextDouble(),
                    .52f + .14f * (float)random.NextDouble(), .20f);
                colors.Add(green * .85f);
                colors.Add(green * .85f);
                colors.Add(green * 1.15f);
                uv.Add(Vector2.zero);
                uv.Add(Vector2.right);
                uv.Add(Vector2.up);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
            }
            var mesh = new Mesh { name = "Merged Wild Grass" };
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
