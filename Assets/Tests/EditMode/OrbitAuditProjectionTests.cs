using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class OrbitAuditProjectionTests
    {
        [Test]
        public void AuditOrbitMatchesTheGameplayCameraAtNeutralRotation()
        {
            Vector3 camera = OrbitPickingCamera.OrbitPosition(
                Vector3.zero, 8f, 0f, 0f, 2.65f);
            Assert.That(Vector3.Distance(camera, new Vector3(0f, 2.65f, -8f)),
                Is.LessThan(.0001f));

            Vector3 turned = OrbitPickingCamera.OrbitPosition(
                Vector3.zero, 8f, 90f, 0f, 2.65f);
            Assert.That(Vector3.Distance(turned, new Vector3(-8f, 2.65f, 0f)),
                Is.LessThan(.0001f));
        }

        [Test]
        public void TopAndBottomOrbitPitchesReachDifferentViewHeights()
        {
            Vector3 down = OrbitPickingCamera.OrbitPosition(
                Vector3.zero, 10f, 20f, -20f, 2.65f);
            Vector3 up = OrbitPickingCamera.OrbitPosition(
                Vector3.zero, 10f, 20f, 58f, 2.65f);
            Assert.Less(down.y, 2.65f);
            Assert.Greater(up.y, 2.65f);
            Assert.That(Vector3.Distance(down, Vector3.up * 2.65f),
                Is.EqualTo(10f).Within(.0001f));
            Assert.That(Vector3.Distance(up, Vector3.up * 2.65f),
                Is.EqualTo(10f).Within(.0001f));
        }

        [Test]
        public void PortraitAuditRejectsFruitBeyondHorizontalAndVerticalEdges()
        {
            Vector3 camera = new Vector3(0f, 2.65f, -10f);
            Vector3 focus = new Vector3(0f, 2.65f, 0f);
            Assert.IsTrue(OrbitPickingCamera.IsInsidePortraitView(
                camera, focus, focus));
            Assert.IsFalse(OrbitPickingCamera.IsInsidePortraitView(
                camera, focus, new Vector3(4f, 2.65f, 0f)));
            Assert.IsFalse(OrbitPickingCamera.IsInsidePortraitView(
                camera, focus, new Vector3(0f, 9f, 0f)));
            Assert.IsFalse(OrbitPickingCamera.IsInsidePortraitView(
                camera, focus, new Vector3(0f, 2.65f, -12f)));
        }

        [Test]
        public void NarrowPhoneIsMoreRestrictiveThanWidePhone()
        {
            Vector3 camera = new Vector3(0f, 2.65f, -10f);
            Vector3 focus = new Vector3(0f, 2.65f, 0f);
            Vector3 fruit = new Vector3(2.1f, 2.65f, 0f);
            Assert.IsFalse(OrbitPickingCamera.IsInsidePortraitView(
                camera, focus, fruit, 47f, 9f / 20f));
            Assert.IsTrue(OrbitPickingCamera.IsInsidePortraitView(
                camera, focus, fruit, 47f, 9f / 16f));
        }

        [Test]
        public void DegenerateAuditViewCannotMarkFruitAsVisible()
        {
            Assert.IsFalse(OrbitPickingCamera.IsInsidePortraitView(
                Vector3.zero, Vector3.zero, Vector3.forward));
            Assert.IsFalse(OrbitPickingCamera.IsInsidePortraitView(
                Vector3.zero, Vector3.forward, Vector3.forward, 47f, 0f));
        }
    }
}
