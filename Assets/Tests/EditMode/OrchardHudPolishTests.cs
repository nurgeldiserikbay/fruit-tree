using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class OrchardHudPolishTests
    {
        [TestCase(0, 120, 0f)]
        [TestCase(30, 120, .25f)]
        [TestCase(60, 120, .5f)]
        [TestCase(120, 120, 1f)]
        [TestCase(200, 120, 1f)]
        [TestCase(-3, 120, 0f)]
        [TestCase(5, 0, 0f)]
        public void ProgressFractionStaysWithinZeroAndOne(
            int picked, int total, float expected)
        {
            Assert.AreEqual(expected, OrchardProgressBar.Fraction(picked, total), .0001f);
        }

        [Test]
        public void ProgressRectUpdatesWithoutChangingTreeState()
        {
            var root = new GameObject("Progress Test", typeof(RectTransform));
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(root.transform, false);
            try
            {
                var bar = root.AddComponent<OrchardProgressBar>();
                bar.fill = fill.GetComponent<RectTransform>();
                bar.OnHarvestChanged(80, 160);
                Assert.That(bar.fill.anchorMax.x, Is.EqualTo(.5f).Within(.0001f));
                bar.OnHarvestChanged(160, 160);
                Assert.That(bar.fill.anchorMax.x, Is.EqualTo(1f).Within(.0001f));
                bar.OnHarvestChanged(0, 140);
                Assert.That(bar.fill.anchorMax.x, Is.EqualTo(0f).Within(.0001f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PickPulseIsShortAndDoesNotFlash()
        {
            Assert.AreEqual(1f, FruitPickPulse.Opacity(0f), .001f);
            Assert.That(FruitPickPulse.Opacity(.5f), Is.InRange(.20f, .30f));
            Assert.AreEqual(0f, FruitPickPulse.Opacity(1f), .001f);
            Assert.AreEqual(0f, FruitPickPulse.Opacity(3f), .001f);
            Assert.AreEqual(1f, FruitPickPulse.Opacity(-2f), .001f);
            float previous = 1f;
            for (int i = 1; i <= 20; i++)
            {
                float current = FruitPickPulse.Opacity(i / 20f);
                Assert.LessOrEqual(current, previous);
                previous = current;
            }
        }
    }
}
