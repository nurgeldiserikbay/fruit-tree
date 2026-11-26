using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class VisualQualityTests
    {
        [Test]
        public void EverySpeciesHasARealPeel()
        {
            foreach (var level in OrchardGameController.LevelDefinitions)
                Assert.IsNotNull(Resources.Load<Texture2D>("TreeTextures/Fruit/" + level.SpeciesId),
                    level.SpeciesId + " needs a peel texture");
        }

        [Test]
        public void LowGraphicsGrowsFewerLeaves()
        {
            float saved = TreeGenerator.LeafDensity;
            var root = new GameObject("Leaf Density");
            try
            {
                var tree = root.AddComponent<TreeGenerator>();
                tree.SetAutomaticGeneration(false);
                TreeGenerator.LeafDensity = 1f;
                tree.Configure("apple", 30, 122);
                int full = root.transform.Find("Merged Leaves").GetComponent<MeshFilter>().sharedMesh.vertexCount;
                TreeGenerator.LeafDensity = .7f;
                tree.Configure("apple", 30, 122);
                int low = root.transform.Find("Merged Leaves").GetComponent<MeshFilter>().sharedMesh.vertexCount;
                Assert.Less(low, full * .8f, "LOW must noticeably lighten the crown.");
                Assert.AreEqual(30, tree.Apples.Count, "Fruit count never changes with graphics.");
            }
            finally
            {
                TreeGenerator.LeafDensity = saved;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShadowBlurKeepsShapeInRange()
        {
            var source = new float[16 * 16];
            source[8 * 16 + 8] = 1f;
            float[] blurred = CrownShadow.Blur(source, 16, 2);
            float total = 0f;
            foreach (float value in blurred)
            {
                Assert.That(value, Is.InRange(0f, 1f));
                total += value;
            }
            Assert.That(total, Is.EqualTo(1f).Within(.001f), "Blurring spreads the shadow, it never adds darkness.");
        }

        [Test]
        public void InterfaceFontAndShadowMaterialShip()
        {
            Assert.IsNotNull(Resources.Load<Font>(UiPolish.FontPath));
            Assert.AreEqual("FruitTree/CrownShadow", Resources.Load<Material>("CrownShadowMaterial").shader.name);
        }
    }
}
