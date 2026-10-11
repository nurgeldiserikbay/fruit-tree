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
