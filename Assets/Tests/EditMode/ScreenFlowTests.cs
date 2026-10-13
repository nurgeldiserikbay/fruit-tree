using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class ScreenFlowTests
    {
        [Test]
        public void EmptyAutosaveStillShowsNewGameState()
        {
            var empty = new Progression.OrchardSaveData();
            empty.trees.Add(new Progression.TreeSaveData { treeId = "tree_001" });
            Assert.IsFalse(OrchardGameController.HasMeaningfulProgress(null));
            Assert.IsFalse(OrchardGameController.HasMeaningfulProgress(empty));
            empty.trees[0].pickedFruitIds.Add("fruit_0000");
            Assert.IsTrue(OrchardGameController.HasMeaningfulProgress(empty));
        }

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
        public void FirstPlayStoreEditionIsEnglishOnlyAndCannotSellAdRemoval()
        {
            Assert.AreEqual("en", FruitTreeReleaseConfig.LanguageCode);
            Assert.IsFalse(FruitTreeReleaseConfig.AdvertisingEnabled);
            Assert.IsFalse(FruitTreeReleaseConfig.PurchasesEnabled);
            Assert.IsFalse(FruitTreeReleaseConfig.CanOfferRemoveAds(false));
            Assert.IsFalse(FruitTreeReleaseConfig.CanOfferRemoveAds(true));
        }

        [Test]
        public void SceneVersionChangesDoNotModifySavedFruitIds()
        {
            var go = new GameObject("Scene Revision");
            try
            {
                var version = go.AddComponent<FruitTreeSceneVersion>();
                Assert.IsFalse(version.IsCurrent);
                version.StampGeneratedScene();
                Assert.IsTrue(version.IsCurrent);
                Assert.AreEqual(FruitTreeSceneVersion.Current, version.GeneratedVersion);
                // Scene rebuild metadata is independent from local fruit progress.
                var tree = new Progression.TreeLevelDefinition("tree_001", "apple", 120);
                Assert.AreEqual("fruit_0000", tree.FruitIdAt(0));
                Assert.AreEqual("fruit_0119", tree.FruitIdAt(119));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void MissingThemeMusicShowsUnavailableAndDisablesItsButton()
        {
            var root = new GameObject("Music Settings Test");
            var buttonNode = new GameObject("Music Button",
                typeof(RectTransform), typeof(UnityEngine.UI.Button));
            buttonNode.transform.SetParent(root.transform, false);
            var labelNode = new GameObject("Music Label",
                typeof(RectTransform), typeof(UnityEngine.UI.Text));
            labelNode.transform.SetParent(buttonNode.transform, false);
            var clip = AudioClip.Create("Available Music Test", 22050, 1, 22050, false);
            try
            {
                var audio = root.AddComponent<OrchardAudio>();
                var flow = root.AddComponent<FruitTreeScreenFlow>();
                flow.sound = audio;
                flow.musicButtonLabel = labelNode.GetComponent<UnityEngine.UI.Text>();
                var button = buttonNode.GetComponent<UnityEngine.UI.Button>();

                flow.OpenSettingsFromHome();
                Assert.AreEqual("MUSIC: NOT AVAILABLE", flow.musicButtonLabel.text);
                Assert.IsFalse(button.interactable);
                Assert.IsFalse(audio.HasMusicForCurrentTheme);

                audio.themeMusic[(int)EnvironmentTheme.Meadow] = clip;
                flow.OpenSettingsFromHome();
                Assert.IsTrue(button.interactable);
                Assert.IsTrue(audio.HasMusicForCurrentTheme);
                Assert.AreEqual(audio.IsMusicMuted ? "MUSIC: OFF" : "MUSIC: ON",
                    flow.musicButtonLabel.text);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void OnlyPublishedGoogleDocsHttpsLinksAreAllowed()
        {
            Assert.IsTrue(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e/examplePublishedId/pub"));
            Assert.IsTrue(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e/2PACX-1abc_def/pub?embedded=true"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e/examplePublishedId/publish"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e/examplePublishedId/pub/preview"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e//pub"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com:444/document/d/e/examplePublishedId/pub"));
            Assert.IsFalse(FruitTreePrivacyConfig.IsPublishedUrlValid(
                "https://docs.google.com/document/d/e/examplePublishedId_extra/pub/other"));
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
