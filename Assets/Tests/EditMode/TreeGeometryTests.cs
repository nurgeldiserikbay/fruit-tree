using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class TreeGeometryTests
    {
        [Test]
        public void AppleMeshHasFiniteVerticesAndTriangles()
        {
            Mesh mesh = ProceduralMesh.Apple();
            Assert.Greater(mesh.vertexCount, 100);
            Assert.Greater(mesh.triangles.Length, 300);
            foreach (Vector3 vertex in mesh.vertices)
            {
                Assert.IsFalse(float.IsNaN(vertex.x));
                Assert.IsFalse(float.IsNaN(vertex.y));
                Assert.IsFalse(float.IsNaN(vertex.z));
            }
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void TreeGeneratesThirtyPickableFruitsAndMergedGeometry()
        {
            var root = new GameObject("Test Tree");
            try
            {
                var tree = root.AddComponent<TreeGenerator>();
                tree.Generate();
                Assert.AreEqual(30, tree.Apples.Count);
                Assert.AreEqual(30, root.GetComponentsInChildren<Apple>().Length);
                Assert.AreEqual(0, tree.HarvestedCount);
                Assert.IsNotNull(root.transform.Find("Merged Wood"));
                Assert.IsNotNull(root.transform.Find("Merged Leaves"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        [Test]
        public void AppleNormalsFaceOutward()
        {
            Mesh mesh = ProceduralMesh.Apple();
            try
            {
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                int checkedFaces = 0;
                int outwardFaces = 0;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    Vector3 midpoint = (a + b + c) / 3f;
                    Vector3 radial = new Vector3(midpoint.x, 0f, midpoint.z);
                    if (radial.sqrMagnitude < .001f || normal.sqrMagnitude < 0.000001f) continue;
                    if (Vector3.Dot(normal, radial) > 0f) outwardFaces++;
                    checkedFaces++;
                }
                Assert.Greater(checkedFaces, 80);
                // The small inward-facing top dimple is intentional apple geometry.
                Assert.Greater(outwardFaces, checkedFaces * .95f);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void BranchGeometryPointsOutward()
        {
            var builder = new ProceduralMesh();
            builder.Branch(Vector3.zero, Vector3.up, .2f, .1f, Color.white);
            Mesh mesh = builder.Build("branch-test");
            try
            {
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    Vector3 midpoint = (a + b + c) / 3f;
                    Assert.Greater(Vector3.Dot(normal, new Vector3(midpoint.x, 0, midpoint.z)), 0f);
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void BarkTextureIsSmallAndOpaque()
        {
            Texture2D bark = BarkTexture.Create();
            Assert.AreEqual(128, bark.width);
            Assert.AreEqual(256, bark.height);
            Assert.AreEqual(TextureFormat.RGB24, bark.format);
            Object.DestroyImmediate(bark);
        }
    }
}
