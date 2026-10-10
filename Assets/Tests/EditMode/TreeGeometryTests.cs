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
