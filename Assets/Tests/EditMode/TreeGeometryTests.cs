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
                Assert.IsFalse(float.IsInfinity(vertex.x));
                Assert.IsFalse(float.IsInfinity(vertex.y));
                Assert.IsFalse(float.IsInfinity(vertex.z));
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
        public void AppleTopPoleContainsFiniteValues()
        {
            var mesh = ProceduralMesh.Apple();
            try
            {
                Vector3[] vertices = mesh.vertices;
                for (int i = vertices.Length - 21; i < vertices.Length; i++)
                {
                    Assert.IsFalse(float.IsNaN(vertices[i].x));
                    Assert.IsFalse(float.IsNaN(vertices[i].y));
                    Assert.IsFalse(float.IsNaN(vertices[i].z));
                    Assert.IsFalse(float.IsInfinity(vertices[i].x));
                    Assert.That(vertices[i].y, Is.InRange(.1f, .3f));
                }
            }
            finally { Object.DestroyImmediate(mesh); }
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
        public void EachAppleHasAVisibleStemAttachedToItsParent()
        {
            var root = new GameObject("Tree Stems Test");
            try
            {
                var tree = root.AddComponent<TreeGenerator>();
                if (tree.Apples.Count == 0) tree.Generate();
                foreach (Apple apple in tree.Apples)
                {
                    Transform stem = apple.transform.Find("Stem");
                    Assert.IsNotNull(stem);
                    Assert.Greater(stem.localScale.y, 0.02f);
                    Assert.IsTrue(stem.gameObject.activeSelf);
                    Assert.IsTrue(stem.IsChildOf(apple.transform));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }


        [Test]
        public void PortraitCameraAutomaticallyFramesGeneratedTree()
        {
            var root = new GameObject("Camera Fit Test");
            var camera = new GameObject("Test Camera");
            try
            {
                var generator = root.AddComponent<TreeGenerator>();
                if (generator.Apples.Count == 0) generator.Generate();
                var lens = camera.AddComponent<Camera>();
                lens.fieldOfView = 47f;
                var orbit = camera.AddComponent<OrbitPickingCamera>();
                orbit.target = root.transform;
                orbit.FitTreeInPortrait();
                Assert.Greater(orbit.distance, 5f);
                Assert.LessOrEqual(orbit.distance, 18f);
                Assert.That(orbit.focusHeight, Is.InRange(1.4f, 4.1f));
            }
            finally
            {
                Object.DestroyImmediate(camera);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LeafGeometryDoesNotDuplicateReverseFaces()
        {
            var builder = new ProceduralMesh();
            builder.Leaf(Vector3.zero, Vector3.up, .25f, .13f, Color.green);
            Mesh leaf = builder.Build("leaf");
            try
            {
                Assert.AreEqual(6, leaf.vertexCount);
                Assert.AreEqual(12, leaf.triangles.Length);
            }
            finally { Object.DestroyImmediate(leaf); }
        }

        [Test]
        public void GroundMeshesAreReasonablySmall()
        {
            var terrain = GroundGenerator.CreateTerrainMesh();
            var grass = GroundGenerator.CreateGrassMesh();
            try
            {
                Assert.Less(terrain.vertexCount, 2000);
                Assert.Less(grass.vertexCount, 2000);
                Assert.Greater(terrain.triangles.Length, 1000);
                Assert.Greater(grass.triangles.Length, 100);
            }
            finally
            {
                Object.DestroyImmediate(terrain);
                Object.DestroyImmediate(grass);
            }
        }

        [Test]
        public void AppleSkinTextureFitsMobileBudget()
        {
            Texture2D skin = AppleSkinTexture.Create();
            try
            {
                Assert.AreEqual(128, skin.width);
                Assert.AreEqual(128, skin.height);
                Assert.AreEqual(TextureFormat.RGB24, skin.format);
                Assert.Greater(skin.mipmapCount, 1);
            }
            finally { Object.DestroyImmediate(skin); }
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
