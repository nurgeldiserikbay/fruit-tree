using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class ScreenFlowTests
    {
        [Test]
        public void PlayAndReturnHomePreserveNavigationState()
        {
            var root = new GameObject("Screen Flow Test");
            var home = new GameObject("Home Panel");
            var gameplay = new GameObject("Gameplay Panel");
            home.transform.SetParent(root.transform);
            gameplay.transform.SetParent(root.transform);
            try
            {
                var flow = root.AddComponent<FruitTreeScreenFlow>();
                flow.homePanel = home;
                flow.gameplayControls = gameplay;
                flow.Play();
                Assert.IsTrue(flow.IsPlaying);
                Assert.IsFalse(home.activeSelf);
                Assert.IsTrue(gameplay.activeSelf);
                flow.ReturnHome();
                Assert.IsFalse(flow.IsPlaying);
                Assert.IsTrue(home.activeSelf);
                Assert.IsFalse(gameplay.activeSelf);
                flow.Play();
                Assert.IsTrue(flow.IsPlaying);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AndroidBackFirstClosesHelpAndSettings()
        {
            var root = new GameObject("Back Button Test");
            var settings = new GameObject("Settings");
            var help = new GameObject("Help");
            var privacy = new GameObject("Privacy");
            settings.transform.SetParent(root.transform);
            help.transform.SetParent(root.transform);
            privacy.transform.SetParent(root.transform);
            try
            {
                var flow = root.AddComponent<FruitTreeScreenFlow>();
                flow.settingsPanel = settings;
                flow.helpPanel = help;
                flow.privacyPanel = privacy;
                flow.HandleBackButton();
                Assert.IsFalse(privacy.activeSelf);
                flow.HandleBackButton();
                Assert.IsFalse(help.activeSelf);
                flow.HandleBackButton();
                Assert.IsFalse(settings.activeSelf);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ZenModeHidesTheHudAndRestoresItWhenLeaving()
        {
            var root = new GameObject("Zen Mode Test");
            var hud = new GameObject("Game HUD");
            var exitButtonLayer = new GameObject("Exit Zen");
            hud.transform.SetParent(root.transform);
            exitButtonLayer.transform.SetParent(root.transform);
            try
            {
                var flow = root.AddComponent<FruitTreeScreenFlow>();
                flow.gameplayControls = hud;
                flow.zenExitPanel = exitButtonLayer;
                flow.EnterZenMode();
                Assert.IsTrue(flow.IsZenMode);
                Assert.IsTrue(flow.IsPlaying);
                Assert.IsFalse(hud.activeSelf);
                Assert.IsTrue(exitButtonLayer.activeSelf);
                flow.HandleBackButton();
                Assert.IsFalse(flow.IsZenMode);
                Assert.IsTrue(flow.IsPlaying);
                Assert.IsTrue(hud.activeSelf);
                Assert.IsFalse(exitButtonLayer.activeSelf);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(-1, false)]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(5, true)]
        [TestCase(10, true)]
        [TestCase(11, false)]
        public void HelpOnlyAppearsForLastTenFruits(int remaining, bool expected)
        {
            Assert.AreEqual(expected, OrchardGameController.ShouldOfferHint(remaining));
        }

        [Test]
        public void RemainingFruitGuideIsOptionalAndItsFadeEndsCleanly()
        {
            Assert.AreEqual(0f, RemainingFruitGuide.Visibility(-1f));
            Assert.AreEqual(0f, RemainingFruitGuide.Visibility(0f));
            Assert.That(RemainingFruitGuide.Visibility(.25f), Is.EqualTo(1f).Within(.001f));
            Assert.That(RemainingFruitGuide.Visibility(3f), Is.EqualTo(1f).Within(.001f));
            Assert.That(RemainingFruitGuide.Visibility(4.05f),
                Is.InRange(.45f, .55f));
            Assert.AreEqual(0f, RemainingFruitGuide.Visibility(4.5f));
            Assert.AreEqual(0f, RemainingFruitGuide.Visibility(8f));
        }

        [Test]
        public void FruitPointerNeverCollectsOrChangesASelectedFruit()
        {
            var root = new GameObject("Last Fruit Guide Test");
            var fruitObject = new GameObject("Unpicked Fruit", typeof(SphereCollider));
            fruitObject.transform.SetParent(root.transform);
            try
            {
                var fruit = fruitObject.AddComponent<Apple>();
                fruit.Initialize(7);
                var guide = root.AddComponent<RemainingFruitGuide>();
                // Without UI/camera the optional pointer does nothing.
                guide.PointAt(fruit);
                Assert.IsFalse(guide.IsVisible);
                Assert.IsFalse(fruit.IsHarvested);
                Assert.IsTrue(fruit.GetComponent<Collider>().enabled);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void OnlyPublishedGoogleDocsHttpsLinksAreAllowed()
        {
            Assert.IsTrue(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e/examplePublishedId/pub"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(""));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "http://docs.google.com/document/d/e/example/pub"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/example/edit"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://example.com/document/d/e/example/pub"));
        }
    }
}
