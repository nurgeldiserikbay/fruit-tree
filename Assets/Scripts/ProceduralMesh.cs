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
                    Vertex(center + normal * radius, normal, baseColor * (0.88f + 0.12f * Mathf.Cos(t)), new Vector2(s / (float)sides, (ring == 0 ? 0f : Vector3.Distance(start, finish)) * .65f));
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
                        new Vector2(side / (float)sides, distanceAlong * .65f));
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
        // Map upright procedural leaf coordinates onto a diagonal photo
        // cutout (stem bottom-left, tip upper-right). Leave padding at the
        // atlas edges so clamp + bilinear filtering stay transparent.
                // This mapped UV frame is used for optional diagonal leaf artwork.
        // Restrict the sampling region to the interior of the image, leaving
        // a small guard band for bilinear filtering and mipmap downsampling.
        private static Vector2 LeafArtUV(float x, float y)
        {
            float cross = (x - .5f) * .74f;
            float u = .5f + (y - .5f) * .70f + cross * .70f;
            float v = .5f + (y - .5f) * .70f - cross * .70f;
            return new Vector2(Mathf.Clamp(u, .02f, .98f),
                Mathf.Clamp(v, .02f, .98f));
        }

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
            Vector3 surface = Vector3.Cross(axis, side).normalized;
            Vector3 camber = surface * (width * .22f);
            // A gentle midrib fold gives both sides a readable silhouette
            // without adding vertices, triangles, materials or draw calls.
            var positions = new[]
            {
                center,
                center + axis * (length * .19f) - side * (width * .46f) + camber * .18f,
                center + axis * (length * .19f) + side * (width * .51f) + camber * .18f,
                center + axis * (length * widestAt) - side * (width * maxWidth * .91f) + camber,
                center + axis * (length * widestAt) + side * (width * maxWidth * 1.08f) + camber * .85f,
                center + axis * (length * .81f) - side * (width * .39f) + camber * .64f,
                center + axis * (length * .81f) + side * (width * .46f) + camber * .58f,
                center + axis * length + surface * (width * .12f)
            };
            // The optional photographed leaf runs diagonally from lower
            // left (stem) to upper right (tip). Rotate the foliage UV frame
            // to match the art instead of stretching its veins sideways.
            // Both texture sampling and leaf-hit alpha tests use these UVs.
            var texcoords = new[]
            {
                LeafArtUV(.5f, 0f),
                LeafArtUV(0f, .19f), LeafArtUV(1f, .19f),
                LeafArtUV(0f, widestAt), LeafArtUV(1f, widestAt),
                LeafArtUV(.2f, .81f), LeafArtUV(.8f, .81f),
                LeafArtUV(.5f, 1f)
            };
            Vector3 normal = Vector3.Cross(side, axis).normalized;
            int first = this.positions.Count;
            for (int i = 0; i < positions.Length; i++)
            {
                Color tint = (i == 0 ? color * .84f : i == 7 ? color * 1.08f : color);
                // Gradual normals provide a subtle curved highlight instead of
                // one uniformly lit cardboard plane.
                Vector3 shapedNormal = (normal + side * (i % 2 == 0 ? -.12f : .12f) +
                    axis * (i == 7 ? .08f : -.04f)).normalized;
                Vertex(positions[i], shapedNormal, tint, texcoords[i]);
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


        /// <summary>
        /// Gently heart-shaped, with a small natural longitudinal peach seam.
        /// Shares apple topology and therefore reuses the existing optimized mesh path.
        /// </summary>
        public static Mesh Peach()
        {
            var mesh = Apple();
            mesh.name = "Soft Peach Fruit";
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                float angle = Mathf.Atan2(v.z, v.x);
                float latitude = Mathf.Clamp01((v.y + .18f) / .38f);
                float seam = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle)), 26f);
                float seamDepth = .012f * seam * Mathf.Sin(latitude * Mathf.PI);
                float taper = Mathf.Lerp(1.045f, .94f, latitude);
                verts[i] = new Vector3(v.x * taper - seamDepth * Mathf.Cos(angle),
                    v.y * .96f, v.z * taper - seamDepth * Mathf.Sin(angle));
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Round citrus fruit with a subtle flattened pole and tiny radial pores.
        /// A compact 12x16 cherry topology is enough for mobile-size oranges.
        /// </summary>
        public static Mesh Orange()
        {
            var mesh = Cherry();
            mesh.name = "Round Citrus Fruit";
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                float angle = Mathf.Atan2(v.z, v.x);
                float t = Mathf.Clamp01((v.y + .15f) / .30f);
                float ribbing = 1f + .012f * Mathf.Cos(angle * 10f) *
                    Mathf.Pow(Mathf.Abs(2f * t - 1f), 2f);
                verts[i] = new Vector3(v.x * 1.06f * ribbing,
                    v.y * .99f, v.z * 1.06f * ribbing);
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Short, rounded apricot with a muted cheek seam.</summary>
        public static Mesh Apricot()
        {
            var mesh = Peach();
            mesh.name = "Small Golden Apricot";
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = new Vector3(vertices[i].x * .94f,
                    vertices[i].y * .86f, vertices[i].z * .94f);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Oval purple plum; lower-poly than a full apple.</summary>
        public static Mesh Plum()
        {
            var mesh = Cherry();
            mesh.name = "Long Purple Plum";
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                float t = Mathf.Clamp01((v.y + .15f) / .3f);
                float taper = Mathf.Lerp(1.12f, .91f, t);
                vertices[i] = new Vector3(v.x * taper, v.y * 1.37f, v.z * taper);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>True citrus lemon shape with little tapered end points.</summary>
        public static Mesh Lemon()
        {
            var mesh = Cherry();
            mesh.name = "Tapered Yellow Lemon";
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                float t = Mathf.Clamp01((v.y + .15f) / .3f);
                float taper = .95f + .15f * Mathf.Sin(t * Mathf.PI);
                float tip = .032f * Mathf.Pow(Mathf.Abs(t * 2f - 1f), 8f);
                vertices[i] = new Vector3(v.x * taper, v.y * 1.43f +
                    (t > .5f ? tip : -tip), v.z * taper);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Low-poly flattened mandarin with a broad citrus shoulder.</summary>
        public static Mesh Mandarin()
        {
            var mesh = Orange();
            mesh.name = "Flat Tangerine Mandarin";
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                vertices[i] = new Vector3(v.x * 1.12f, v.y * .72f,
                    v.z * 1.12f);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Red pomegranate with five calyx sepals beneath the fruit, opposite the stem.
        /// No additional GameObjects, meshes or colliders for the crown.
        /// </summary>
        public static Mesh Pomegranate()
        {
            var mesh = Apple();
            mesh.name = "Crowned Pomegranate";
            var verts = new List<Vector3>(mesh.vertices);
            var uv = new List<Vector2>(mesh.uv);
            var tris = new List<int>(mesh.triangles);
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                verts[i] = new Vector3(v.x * 1.025f, v.y * .96f, v.z * 1.025f);
            }
            for (int petal = 0; petal < 5; petal++)
            {
                float a = petal * Mathf.PI * 2f / 5f;
                float b = (petal + 1) * Mathf.PI * 2f / 5f;
                float mid = (a + b) * .5f;
                int start = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a) * .035f, -.175f,
                    Mathf.Sin(a) * .035f));
                verts.Add(new Vector3(Mathf.Cos(b) * .035f, -.175f,
                    Mathf.Sin(b) * .035f));
                verts.Add(new Vector3(Mathf.Cos(mid) * .062f, -.244f,
                    Mathf.Sin(mid) * .062f));
                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(1f, 0f));
                uv.Add(new Vector2(.5f, 1f));
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start + 2); tris.Add(start + 1); tris.Add(start);
            }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
