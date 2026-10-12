using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class TenSpeciesVisualTests
    {
        private static readonly string[] Species =
        {
            "apple", "pear", "cherry", "peach", "orange", "apricot",
            "plum", "lemon", "mandarin", "pomegranate"
        };

        [Test]
        public void TenSpeciesHaveValidDistinctVisualProfiles()
        {
            Assert.AreEqual(10, FruitSpeciesProfile.Count);
            foreach (string id in Species)
            {
                Assert.IsTrue(FruitSpeciesProfile.IsSupported(id));
                var profile = FruitSpeciesProfile.Get(id);
                Assert.AreEqual(id, profile.Id);
                Assert.That(profile.BranchCount, Is.InRange(15, 28));
                Assert.Greater(profile.CrownSpread, .6f);
                Assert.Greater(profile.CrownHeightHigh, profile.CrownHeightLow);
                Assert.Greater(profile.LeafLengthHigh, profile.LeafLengthLow);
                Assert.Greater(profile.LeafWidthHigh, profile.LeafWidthLow);
                Assert.That(profile.LeafOvalness, Is.InRange(0f, 1f));
                Assert.Greater(profile.FruitScaleLow, 0f);
                Assert.Greater(profile.FruitScaleHigh, profile.FruitScaleLow);
                Assert.Greater(profile.HitRadius, .12f);
            }
            Assert.IsFalse(FruitSpeciesProfile.IsSupported("mango"));
            Assert.Throws<System.ArgumentException>(() => FruitSpeciesProfile.Get("mango"));
        }

        [Test]
        public void TenTreeProfilesCreateDifferentCanopySilhouettesWithoutNewMeshes()
        {
            FruitSpeciesProfile pear = FruitSpeciesProfile.Get("pear");
            FruitSpeciesProfile cherry = FruitSpeciesProfile.Get("cherry");
            FruitSpeciesProfile peach = FruitSpeciesProfile.Get("peach");
            FruitSpeciesProfile apple = FruitSpeciesProfile.Get("apple");
            Assert.Greater(pear.BranchRise, apple.BranchRise);
            Assert.Greater(pear.TrunkHeight, cherry.TrunkHeight);
            Assert.Less(cherry.BranchRise, apple.BranchRise);
            Assert.Greater(cherry.FillerSpread, pear.FillerSpread);
            Assert.Less(peach.BranchRise, apple.BranchRise);

            foreach (string id in Species)
            {
                var profile = FruitSpeciesProfile.Get(id);
                Assert.That(profile.BranchRise, Is.InRange(.1f, .85f), id);
                Assert.That(profile.TwigUpwardFactor, Is.InRange(.65f, 1.25f), id);
                Assert.That(profile.TrunkHeight, Is.InRange(3.6f, 4.4f), id);
                Assert.Greater(profile.FillerTopHeight, profile.CrownHeightHigh, id);
                Assert.That(profile.FillerSpread, Is.InRange(.7f, 1.25f), id);
            }
        }

        [Test]
        public void AllTenFruitShapesHaveFiniteGeometryAndSmallMeshBudgets()
        {
            foreach (string id in Species)
            {
                Mesh mesh = FruitSpeciesProfile.Get(id).CreateFruitMesh();
                try
                {
                    Assert.That(mesh.vertexCount, Is.InRange(150, 450), id);
                    Assert.That(mesh.triangles.Length / 3, Is.InRange(150, 850), id);
                    Assert.That(mesh.bounds.size.x, Is.InRange(.22f, .6f), id);
                    Assert.That(mesh.bounds.size.y, Is.InRange(.18f, .65f), id);
                    foreach (Vector3 v in mesh.vertices)
                        Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) ||
                            float.IsNaN(v.z) || float.IsInfinity(v.x) ||
                            float.IsInfinity(v.y) || float.IsInfinity(v.z), id);
                }
                finally { Object.DestroyImmediate(mesh); }
            }
        }

        [Test]
        public void CitrusShapesAndPomegranateCrownHaveDistinctProportions()
        {
            Mesh lemon = ProceduralMesh.Lemon();
            Mesh mandarin = ProceduralMesh.Mandarin();
            Mesh pomegranate = ProceduralMesh.Pomegranate();
            try
            {
                Assert.Greater(lemon.bounds.size.y, mandarin.bounds.size.y * 1.7f);
                Assert.Greater(mandarin.bounds.size.x, lemon.bounds.size.x);
                Assert.Less(pomegranate.bounds.min.y, -.23f,
                    "The pomegranate crown belongs opposite the stem.");
                Assert.Greater(pomegranate.vertexCount, 357);
            }
            finally
            {
                Object.DestroyImmediate(lemon);
                Object.DestroyImmediate(mandarin);
                Object.DestroyImmediate(pomegranate);
            }
        }

        [Test]
        public void TenSpeciesUseOnlySmallSharedFruitTextures()
        {
            foreach (string id in Species)
            {
                Texture2D tex = AppleSkinTexture.CreateForSpecies(id);
                try
                {
                    int expected = id == "apple" ? 128 : 64;
                    Assert.AreEqual(expected, tex.width, id);
                    Assert.AreEqual(expected, tex.height, id);
                    Assert.AreEqual(TextureFormat.RGB24, tex.format, id);
                    Assert.Greater(tex.mipmapCount, 1, id);
                }
                finally { Object.DestroyImmediate(tex); }
            }
        }
    }
}
