using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class VisualPolishTests
    {
        [Test]
        public void AppleSilhouetteHasCompactRoundedDimensionsAndStemDimple()
        {
            Mesh apple = ProceduralMesh.Apple();
            try
            {
                Assert.Greater(apple.vertexCount, 300);
                Assert.That(apple.bounds.size.x, Is.InRange(.25f, .52f));
                Assert.That(apple.bounds.size.y, Is.InRange(.30f, .42f));
                Assert.That(apple.bounds.max.y, Is.InRange(.10f, .25f));
                Assert.That(apple.bounds.min.y, Is.InRange(-.22f, -.11f));
                foreach (Vector3 v in apple.vertices)
                    Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) ||
                        float.IsNaN(v.z) || float.IsInfinity(v.x) ||
                        float.IsInfinity(v.y) || float.IsInfinity(v.z));
            }
            finally { Object.DestroyImmediate(apple); }
        }

        [Test]
        public void CherryUsesSmallerRoundMeshInsteadOfAppleMesh()
        {
            Mesh cherry = ProceduralMesh.Cherry();
            Mesh apple = ProceduralMesh.Apple();
            try
            {
                Assert.Less(cherry.vertexCount, apple.vertexCount);
                Assert.That(cherry.bounds.size.x, Is.InRange(.29f, .34f));
                Assert.That(cherry.bounds.size.y, Is.InRange(.25f, .32f));
                Assert.Less(cherry.vertexCount, 300);
            }
            finally
            {
                Object.DestroyImmediate(cherry);
                Object.DestroyImmediate(apple);
            }
        }

        [Test]
        public void PearAndCherrySkinsShareSmallMipmapBudget()
        {
            foreach (string species in new[] { "pear", "cherry" })
            {
                Texture2D skin = AppleSkinTexture.CreateForSpecies(species);
                try
                {
                    Assert.AreEqual(64, skin.width);
                    Assert.AreEqual(64, skin.height);
                    Assert.AreEqual(TextureFormat.RGB24, skin.format);
                    Assert.Greater(skin.mipmapCount, 1);
                }
                finally { Object.DestroyImmediate(skin); }
            }
        }

        [Test]
        public void CurvedGrassFitsMobileBudgetAndRemainsShort()
        {
            Mesh grass = GroundGenerator.CreateGrassMesh();
            try
            {
                Assert.Less(grass.vertexCount, 2000);
                Assert.Greater(grass.triangles.Length, 1000);
                Assert.Less(grass.bounds.max.y, .24f);
                Assert.Greater(grass.bounds.size.x, 7f);
                Assert.Greater(grass.bounds.size.z, 7f);
            }
            finally { Object.DestroyImmediate(grass); }
        }

        [Test]
        public void QuietClearNoticeUsesSmoothFadeWithoutFlashing()
        {
            Assert.AreEqual(0f, CalmCompletionFeedback.EvaluateAlpha(0f));
            Assert.That(CalmCompletionFeedback.EvaluateAlpha(.4f), Is.InRange(.1f, .9f));
            Assert.That(CalmCompletionFeedback.EvaluateAlpha(1.5f), Is.EqualTo(.92f).Within(.001f));
            Assert.That(CalmCompletionFeedback.EvaluateAlpha(4f), Is.InRange(0f, .92f));
            Assert.AreEqual(0f, CalmCompletionFeedback.EvaluateAlpha(4.5f));
        }
    }
}
