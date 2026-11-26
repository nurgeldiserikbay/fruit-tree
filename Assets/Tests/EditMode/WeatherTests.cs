using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class WeatherTests
    {
        [Test]
        public void WeatherAlwaysChangesAndStormsOnlyFollowCloudsOrRain()
        {
            foreach (WeatherKind current in System.Enum.GetValues(typeof(WeatherKind)))
            {
                for (int step = 0; step < 1000; step++)
                {
                    WeatherKind next = WeatherController.PickNext(current, step / 1000.0);
                    Assert.AreNotEqual(current, next);
                    if (next == WeatherKind.Storm)
                        Assert.That(current == WeatherKind.Cloudy || current == WeatherKind.Rain);
                }
            }
        }

        [Test]
        public void SessionNeverOpensInRainOrStorm()
        {
            for (int step = 0; step < 1000; step++)
            {
                WeatherKind first = WeatherController.PickInitial(step / 1000.0);
                Assert.AreNotEqual(WeatherKind.Rain, first);
                Assert.AreNotEqual(WeatherKind.Storm, first);
            }
        }

        [Test]
        public void BirdsAreSilentDuringRain()
        {
            Assert.IsTrue(WeatherLook.For(WeatherKind.Clear).HasBirds);
            Assert.IsFalse(WeatherLook.For(WeatherKind.Rain).HasBirds);
            Assert.IsFalse(WeatherLook.For(WeatherKind.Storm).HasBirds);
        }

        [Test]
        public void LightningFlashIsSoftAndEnds()
        {
            for (float time = 0f; time <= WeatherController.FlashSeconds; time += .005f)
                Assert.That(WeatherController.FlashAt(time), Is.InRange(0f, .55f));
            Assert.AreEqual(0f, WeatherController.FlashAt(WeatherController.FlashSeconds + .01f));
        }

        [Test]
        public void ThundersDifferAndStaySoft()
        {
            float[] previous = null;
            for (int variant = 0; variant < WeatherController.ThunderVariants; variant++)
            {
                AudioClip clip = WeatherController.CreateThunder(variant);
                try
                {
                    var samples = new float[clip.samples];
                    clip.GetData(samples, 0);
                    float peak = 0f;
                    foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                    Assert.That(peak, Is.InRange(.4f, .85f), clip.name);
                    Assert.Greater(clip.length, 4f, clip.name);
                    Assert.Less(Mathf.Abs(samples[samples.Length - 1]), .01f, "Ends silently.");
                    if (previous != null) CollectionAssert.AreNotEqual(previous, samples);
                    previous = samples;
                }
                finally { Object.DestroyImmediate(clip); }
            }
        }

        [Test]
        public void ThunderMatchesDistanceAndNeverRepeatsAtOnce()
        {
            for (int previous = -1; previous < WeatherController.ThunderVariants; previous++)
            {
                for (int step = 0; step < 100; step++)
                {
                    double roll = step / 100.0;
                    int far = WeatherController.PickThunder(false, previous, roll);
                    int near = WeatherController.PickThunder(true, previous, roll);
                    Assert.That(far, Is.InRange(0, WeatherController.FirstNearThunder - 1));
                    Assert.That(near, Is.InRange(WeatherController.FirstNearThunder,
                        WeatherController.ThunderVariants - 1));
                    Assert.AreNotEqual(previous, far);
                    Assert.AreNotEqual(previous, near);
                }
            }
        }

        [Test]
        public void RainMeshStaysSmallForPhones()
        {
            Mesh mesh = WeatherController.BuildRainMesh(800, 1);
            try
            {
                Assert.AreEqual(3200, mesh.vertexCount);
                Assert.AreEqual(4800, mesh.triangles.Length);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void RainMaterialShipsWithTheRainShader()
        {
            var material = Resources.Load<Material>("RainMaterial");
            Assert.IsNotNull(material);
            Assert.AreEqual("FruitTree/Rain", material.shader.name);
        }
    }
}
