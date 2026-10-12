using System.Collections.Generic;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Static, vertex-colored scenery primitives. No imported models, colliders,
    /// textures, individual prop renderers or runtime Update operations.
    /// </summary>
    public sealed class EnvironmentMeshBuilder
    {
        private readonly List<Vector3> points = new List<Vector3>(2048);
        private readonly List<Vector3> normals = new List<Vector3>(2048);
        private readonly List<Color> colors = new List<Color>(2048);
        private readonly List<int> faces = new List<int>(4096);

        public int VertexCount => points.Count;
        public int TriangleCount => faces.Count / 3;

        private void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int start = points.Count;
            points.Add(a); points.Add(b); points.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            colors.Add(color); colors.Add(color); colors.Add(color);
            faces.Add(start); faces.Add(start + 1); faces.Add(start + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            Triangle(a, b, c, color);
            Triangle(a, c, d, color);
        }

        public void Box(Vector3 center, Vector3 size, Color color)
        {
            Vector3 h = size * .5f;
            Vector3 p000 = center + new Vector3(-h.x, -h.y, -h.z);
            Vector3 p001 = center + new Vector3(-h.x, -h.y, h.z);
            Vector3 p010 = center + new Vector3(-h.x, h.y, -h.z);
            Vector3 p011 = center + new Vector3(-h.x, h.y, h.z);
            Vector3 p100 = center + new Vector3(h.x, -h.y, -h.z);
            Vector3 p101 = center + new Vector3(h.x, -h.y, h.z);
            Vector3 p110 = center + new Vector3(h.x, h.y, -h.z);
            Vector3 p111 = center + new Vector3(h.x, h.y, h.z);
            Quad(p001, p101, p111, p011, color);
            Quad(p100, p000, p010, p110, color * .85f);
            Quad(p000, p001, p011, p010, color * .9f);
            Quad(p101, p100, p110, p111, color * .95f);
            Quad(p011, p111, p110, p010, color * 1.05f);
            Quad(p000, p100, p101, p001, color * .76f);
        }

        public void Cone(Vector3 baseCenter, float radius, float height, int segments, Color color)
        {
            if (segments < 3) return;
            Vector3 tip = baseCenter + Vector3.up * height;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 v0 = baseCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                Vector3 v1 = baseCenter + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * radius;
                Triangle(v1, v0, tip, color * (.91f + .09f * Mathf.Cos(a)));
            }
        }

        public void FlowerPatch(Vector3 center, float radius, int petals, Color flower, Color leaf)
        {
            // Farther from the tree; tiny colored ground petals are an accent.
            float angleStep = Mathf.PI * 2f / petals;
            for (int i = 0; i < petals; i++)
            {
                float a = i * angleStep;
                float b = a + angleStep * .6f;
                Vector3 a0 = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                Vector3 b0 = center + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * radius;
                Triangle(center + Vector3.up * .015f,
                    a0 + Vector3.up * .015f, b0 + Vector3.up * .015f, flower);
            }
            Cone(center, radius * .35f, .06f, 5, leaf);
        }

        public void RingPath(float radius, float width, int segments, Color color)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 innerA = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius - width);
                Vector3 outerA = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius + width);
                Vector3 innerB = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * (radius - width);
                Vector3 outerB = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * (radius + width);
                innerA.y = .025f; outerA.y = .025f; innerB.y = .025f; outerB.y = .025f;
                Quad(innerA, innerB, outerB, outerA, color);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (points.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(points);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            // No wind on landmarks; UVs default to zero.
            mesh.SetTriangles(faces, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
