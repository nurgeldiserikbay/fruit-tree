using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class NatureTests
    {
        [Test]
        public void BrookStaysClearOfTheTreeAndOrbit()
        {
            for (float x = -Brook.HalfLength; x <= Brook.HalfLength; x += .25f)
            {
                Vector3 center = Brook.CenterAt(x);
                float nearestBank = new Vector2(center.x, center.z).magnitude - Brook.WidthAt(x) * .5f;
                Assert.Greater(nearestBank, 3.9f, "x " + x);
            }
        }

        [Test]
        public void NatureMeshesStaySmall()
        {
            Mesh[] meshes =
            {
                Brook.BuildWater(), Brook.BuildStones(731), Wildlife.BuildFlyingBird(),
                Wildlife.BuildSparrow(), Wildlife.BuildButterfly(Color.white)
            };
            try
            {
                foreach (Mesh mesh in meshes)
                {
                    Assert.Greater(mesh.vertexCount, 0, mesh.name);
                    Assert.Less(mesh.vertexCount, 6000, mesh.name);
                }
            }
            finally
            {
                foreach (Mesh mesh in meshes) Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void NatureMaterialsShipWithTheirShaders()
        {
            Assert.AreEqual("FruitTree/Water", Resources.Load<Material>("WaterMaterial").shader.name);
            Assert.AreEqual("FruitTree/Wildlife", Resources.Load<Material>("WildlifeMaterial").shader.name);
        }

        [Test]
        public void FruitMustBeCloseToPick()
        {
            Assert.Less(OrbitPickingCamera.MaximumPickDistance, OrbitPickingCamera.MaximumOrbitDistance);
            Assert.Greater(OrbitPickingCamera.MaximumPickDistance, OrbitPickingCamera.MinimumOrbitDistance);
        }

        [Test]
        public void BrookSoundLoopsWithoutClipping()
        {
            AudioClip clip = Brook.CreateBrookSound();
            try
            {
                var samples = new float[clip.samples];
                clip.GetData(samples, 0);
                foreach (float sample in samples) Assert.That(sample, Is.InRange(-.8f, .8f));
            }
            finally { Object.DestroyImmediate(clip); }
        }
    }
}
