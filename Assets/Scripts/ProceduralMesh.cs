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
                Tri(a, b, c);
                Tri(b, d, c);
            }
        }

        /// <summary>
        /// Creates a continuous tapered tube along a limb or curved trunk.
        /// Shared rings and smooth normals eliminate hard cylinder joints.
        /// </summary>
        public void BranchCurve(Vector3[] points, float[] radii, Color baseColor)
        {
            if (points == null || radii == null || points.Length != radii.Length || points.Length < 2)
                throw new System.ArgumentException("BranchCurve requires matching path and radius arrays.");
            const int sides = 8;
            int first = positions.Count;
            float distanceAlong = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 tangent = i == 0 ? points[1] - points[0] :
                    i == points.Length - 1 ? points[i] - points[i - 1] :
                    points[i + 1] - points[i - 1];
                if (tangent.sqrMagnitude < .000001f)
                    throw new System.ArgumentException("Branch path points must not overlap.");
                tangent.Normalize();
                Vector3 reference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .93f ?
                    Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(tangent, reference).normalized;
                Vector3 forward = Vector3.Cross(tangent, right).normalized;
                if (i > 0) distanceAlong += Vector3.Distance(points[i], points[i - 1]);
                float radius = Mathf.Max(.003f, radii[i]);
                for (int side = 0; side <= sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    Vector3 normal = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                    Color shade = baseColor * (.92f + .08f * Mathf.Cos(angle * 2f));
                    Vertex(points[i] + normal * radius, normal, shade,
                        new Vector2(side / (float)sides, distanceAlong * .45f));
                }
            }
            for (int ring = 0; ring < points.Length - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = first + ring * (sides + 1) + side;
                    int b = a + 1;
                    int c = a + sides + 1;
                    int d = c + 1;
                    Tri(a, b, c);
                    Tri(b, d, c);
                }
            }
        }

        /// <summary>
        /// A broad, softly tapered leaf silhouette. Eight shared vertices and
        /// six triangles are sufficient for a readable mobile-sized canopy.
        /// UV.y gradually increases to bend only the leaf tip in the wind.
        /// </summary>
        public void Leaf(Vector3 center, Vector3 outward, float length, float width,
            Color color, float ovalness = .5f)
        {
            // Pear: fuller oval leaves, Cherry: slimmer pointed leaves.
            // Eight vertices and six triangles remain fixed for mobile budgets.
            float shape = Mathf.Clamp01(ovalness);
            float widestAt = Mathf.Lerp(.41f, .57f, shape);
            float maxWidth = Mathf.Lerp(.60f, .83f, shape);
            Vector3 axis = outward.sqrMagnitude < .0001f ? Vector3.up : outward.normalized;
            Vector3 side = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > .9f
                ? Vector3.forward : Vector3.up).normalized;
            Vector3 camber = Vector3.Cross(axis, side).normalized * (width * .12f);
            var positions = new[]
            {
                center,
                center + axis * (length * .19f) - side * (width * .50f) + camber * .3f,
                center + axis * (length * .19f) + side * (width * .50f) + camber * .3f,
                center + axis * (length * widestAt) - side * (width * maxWidth) + camber,
                center + axis * (length * widestAt) + side * (width * maxWidth) + camber,
                center + axis * (length * .81f) - side * (width * .43f) + camber * .6f,
                center + axis * (length * .81f) + side * (width * .43f) + camber * .6f,
                center + axis * length
            };
            var texcoords = new[]
            {
                new Vector2(.5f, 0f),
                new Vector2(0f, .19f), new Vector2(1f, .19f),
                new Vector2(0f, widestAt), new Vector2(1f, widestAt),
                new Vector2(.2f, .81f), new Vector2(.8f, .81f),
                new Vector2(.5f, 1f)
            };
            Vector3 normal = Vector3.Cross(side, axis).normalized;
            int first = this.positions.Count;
            for (int i = 0; i < positions.Length; i++)
            {
                Color tint = (i == 0 ? color * .84f : i == 7 ? color * 1.08f : color);
                Vertex(positions[i], normal, tint, texcoords[i]);
            }
            Tri(first, first+1, first+2);
            Tri(first+1, first+3, first+2);
            Tri(first+2, first+3, first+4);
            Tri(first+3, first+5, first+4);
            Tri(first+4, first+5, first+6);
            Tri(first+5, first+7, first+6);
            // One-sided mesh; the leaf material renders both sides.
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
            // Rounded shoulders, five subtle calyx lobes and a small dimple
            // make the silhouette feel like an apple rather than a plain sphere.
            // Preserve the same vertex topology for stable rendering and tests.
            var mesh = new Mesh { name = "Five-Lobed Orchard Apple" };
            const int latitude = 16, longitude = 20;
            var verts = new List<Vector3>((latitude + 1) * (longitude + 1));
            var tris = new List<int>(latitude * longitude * 6);
            var uvs = new List<Vector2>(verts.Capacity);
            for (int y = 0; y <= latitude; y++)
            {
                float t = y / (float)latitude;
                float angle = t * Mathf.PI;
                float sine = Mathf.Max(0f, Mathf.Sin(angle));
                float shoulder = Mathf.SmoothStep(.53f, .91f, t);
                float body = Mathf.Pow(sine, .69f) * (.176f + .028f * t);
                float upperDimple = Mathf.Pow(Mathf.Clamp01((t - .86f) / .14f), 2f);
                float baseY = -.18f + .375f * t - .044f * upperDimple;
                for (int x = 0; x <= longitude; x++)
                {
                    float theta = x * Mathf.PI * 2f / longitude;
                    float lobes = Mathf.Cos(theta * 5f);
                    float variation = 1f + .039f * lobes * shoulder;
                    float bottomLobes = .007f * Mathf.Pow(1f - t, 9f) * lobes;
                    float upperLobes = .012f * shoulder * upperDimple * lobes;
                    verts.Add(new Vector3(body * variation * Mathf.Cos(theta),
                        baseY + bottomLobes + upperLobes,
                        body * variation * Mathf.Sin(theta)));
                    uvs.Add(new Vector2(x / (float)longitude, t));
                }
            }
            for (int y = 0; y < latitude; y++)
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * (longitude + 1) + x;
                    int b = a + longitude + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Pear()
        {
            var mesh = Apple();
            mesh.name = "Soft Pear";
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                float t = Mathf.Clamp01((v.y + .18f) / .38f);
                float width = Mathf.Lerp(1.32f, .67f, t);
                vertices[i] = new Vector3(v.x * width, v.y * 1.48f, v.z * width);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Cherry()
        {
            // True compact cherry silhouette rather than a reduced apple with
            // five exaggerated shoulders. A smaller mesh helps dense cherry trees.
            var mesh = new Mesh { name = "Round Cherry Fruit" };
            const int latitude = 12, longitude = 16;
            var verts = new List<Vector3>((latitude + 1) * (longitude + 1));
            var tris = new List<int>(latitude * longitude * 6);
            var uvs = new List<Vector2>(verts.Capacity);
            for (int y = 0; y <= latitude; y++)
            {
                float t = y / (float)latitude;
                float radius = .16f * Mathf.Max(0f, Mathf.Sin(t * Mathf.PI));
                float dimple = .014f * Mathf.Pow(t, 14f);
                float height = -.15f + .30f * t - dimple;
                for (int x = 0; x <= longitude; x++)
                {
                    float angle = x * Mathf.PI * 2f / longitude;
                    verts.Add(new Vector3(radius * Mathf.Cos(angle), height,
                        radius * Mathf.Sin(angle)));
                    uvs.Add(new Vector2(x / (float)longitude, t));
                }
            }
            for (int y = 0; y < latitude; y++)
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * (longitude + 1) + x;
                    int b = a + longitude + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
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
