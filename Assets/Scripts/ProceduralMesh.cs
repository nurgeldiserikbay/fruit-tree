using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>Creates merged geometry, reducing hundreds of leaf and branch draw calls.</summary>
    public sealed class ProceduralMesh
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        public int VertexCount => positions.Count;

        private void Vertex(Vector3 p, Vector3 n, Color color, Vector2 tex)
        {
            positions.Add(p);
            normals.Add(n.normalized);
            colors.Add(color);
            uv.Add(tex);
        }

        public void Branch(Vector3 start, Vector3 finish, float r0, float r1, Color baseColor)
        {
            Vector3 axis = (finish - start).normalized;
            Vector3 right = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > .95f ? Vector3.forward : Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(axis, right).normalized;
            int first = positions.Count;
            const int sides = 8;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = ring == 0 ? r0 : r1;
                Vector3 center = ring == 0 ? start : finish;
                for (int s = 0; s <= sides; s++)
                {
                    float t = s * Mathf.PI * 2f / sides;
                    Vector3 normal = right * Mathf.Cos(t) + forward * Mathf.Sin(t);
                    Vertex(center + normal * radius, normal, baseColor * (0.88f + 0.12f * Mathf.Cos(t)), new Vector2(s / (float)sides, ring));
                }
            }
            for (int s = 0; s < sides; s++)
            {
                int a = first + s, b = a + 1, c = first + sides + 1 + s, d = c + 1;
                Tri(a, c, b);
                Tri(b, c, d);
            }
        }

        public void Leaf(Vector3 center, Vector3 outward, float length, float width, Color color)
        {
            Vector3 axis = outward.normalized;
            Vector3 side = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 camber = Vector3.Cross(axis, side).normalized * (width * .16f);
            var points = new[] {
                center, center + axis * (length * .24f) - side * width * .42f + camber,
                center + axis * (length * .24f) + side * width * .42f + camber,
                center + axis * (length * .67f) - side * width * .45f + camber,
                center + axis * (length * .67f) + side * width * .45f + camber,
                center + axis * length
            };
            Vector3 normal = Vector3.Cross(side, axis).normalized;
            int first = positions.Count;
            for (int i = 0; i < points.Length; i++)
            {
                Color c = i == 0 ? color * .75f : (i == 5 ? color * 1.1f : color);
                Vertex(points[i], normal, c, new Vector2(i % 2, i / 5f));
            }
            Tri(first, first+1, first+2);
            Tri(first+1, first+3, first+2);
            Tri(first+2, first+3, first+4);
            Tri(first+3, first+5, first+4);
            // Reverse faces as leaf cards must be visible from both sides.
            Tri(first+2, first+1, first);
            Tri(first+2, first+3, first+1);
            Tri(first+4, first+3, first+2);
            Tri(first+4, first+5, first+3);
        }

        public void Tri(int a, int b, int c)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (positions.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Apple()
        {
            var mesh = new Mesh { name = "Rounded Apple" };
            const int latitude = 16, longitude = 20;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            for (int y = 0; y <= latitude; y++)
            {
                float t = y / (float)latitude;
                float a = t * Mathf.PI;
                float radius = Mathf.Pow(Mathf.Sin(a), .72f) * (.17f + .045f * t);
                for (int x = 0; x <= longitude; x++)
                {
                    float theta = x * Mathf.PI * 2f / longitude;
                    float scallop = 1f + .035f * Mathf.Cos(theta * 5f) * Mathf.Pow(t, 4f);
                    float indent = -.055f * Mathf.Pow(t, 18f) * Mathf.Pow(Mathf.Cos(theta * 2.5f), 2f);
                    verts.Add(new Vector3(radius * scallop * Mathf.Cos(theta), -.18f + .38f * t + indent, radius * scallop * Mathf.Sin(theta)));
                    uvs.Add(new Vector2(x / (float)longitude, t));
                }
            }
            for (int y = 0; y < latitude; y++)
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * (longitude + 1) + x, b = a + longitude + 1;
                    tris.Add(a); tris.Add(a+1); tris.Add(b);
                    tris.Add(a+1); tris.Add(b+1); tris.Add(b);
                }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
