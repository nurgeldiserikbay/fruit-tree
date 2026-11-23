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
