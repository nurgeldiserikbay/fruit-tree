using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FruitTree.Tests
{
    public sealed class HarvestGameplayTests
    {
        [UnityTest]
        public IEnumerator PickingSameAppleTwiceOnlyCountsOnce()
        {
            var root = new GameObject("Harvest Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            Apple apple = tree.Apples[0];
            apple.Pick();
            apple.Pick();
            Assert.IsTrue(apple.IsHarvested);
            Assert.AreEqual(1, tree.HarvestedCount);
            yield return null;
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator RestartReplenishesApplesAndClearsProgress()
        {
            var root = new GameObject("Restart Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            tree.Apples[0].Pick();
            Assert.AreEqual(1, tree.HarvestedCount);
            Mesh before = root.transform.Find("Merged Wood").GetComponent<MeshFilter>().sharedMesh;
            tree.ResetHarvest();
            Assert.AreSame(before, root.transform.Find("Merged Wood").GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(0, tree.HarvestedCount);
            Assert.AreEqual(30, tree.Apples.Count);
            foreach (var apple in tree.Apples)
            {
                Assert.IsFalse(apple.IsHarvested);
                Assert.IsTrue(apple.gameObject.activeSelf);
                Assert.IsTrue(apple.GetComponent<Collider>().enabled);
            }
            yield return null;
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator RestartWhileAppleAnimationIsRunningRestoresTransform()
        {
            var root = new GameObject("Interrupted Animation Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            Apple apple = tree.Apples[0];
            Vector3 position = apple.transform.localPosition;
            Vector3 size = apple.transform.localScale;
            apple.Pick();
            yield return null; // animation has started
            tree.ResetHarvest();
            Assert.IsFalse(apple.IsHarvested);
            Assert.IsTrue(apple.gameObject.activeSelf);
            Assert.That(Vector3.Distance(apple.transform.localPosition, position), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(apple.transform.localScale, size), Is.LessThan(.001f));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator PickingAfterRestartIsCountedExactlyOnce()
        {
            var root = new GameObject("Replay Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            Apple apple = tree.Apples[0];
            apple.Pick();
            yield return null;
            tree.ResetHarvest();
            apple.Pick();
            apple.Pick();
            Assert.AreEqual(1, tree.HarvestedCount);
            Assert.IsTrue(apple.IsHarvested);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PauseMenuRestartRestoresHarvestedApple()
        {
            var root = new GameObject("Pause Restart Test");
            var treeObject = new GameObject("Tree");
            var menu = new GameObject("Menu");
            treeObject.transform.SetParent(root.transform);
            menu.transform.SetParent(root.transform);
            menu.SetActive(false);
            var tree = treeObject.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            var hud = root.AddComponent<HarvestHUD>();
            hud.tree = tree;
            var pause = root.AddComponent<PauseController>();
            pause.panel = menu;
            pause.hud = hud;
            try
            {
                tree.Apples[0].Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
                pause.Toggle();
                pause.Restart();
                Assert.AreEqual(0, tree.HarvestedCount);
                Assert.IsFalse(pause.IsPaused);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.IsTrue(tree.Apples[0].gameObject.activeSelf);
            }
            finally
            {
                pause.Resume();
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrchardPauseRestartMustNotClearIncompleteFruitProgress()
        {
            var root = new GameObject("Protected Orchard Pause Test");
            var treeObject = new GameObject("Orchard Tree");
            treeObject.transform.SetParent(root.transform);
            var panel = new GameObject("Pause Panel");
            panel.transform.SetParent(root.transform);
            panel.SetActive(false);
            var tree = treeObject.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            var hud = root.AddComponent<HarvestHUD>();
            hud.tree = tree;
            hud.OrchardMode = true;
            var pause = root.AddComponent<PauseController>();
            pause.hud = hud;
            pause.panel = panel;
            try
            {
                tree.Apples[0].Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
                pause.Toggle();
                pause.Restart();
                Assert.AreEqual(1, tree.HarvestedCount);
                Assert.IsTrue(tree.Apples[0].IsHarvested);
                Assert.IsTrue(pause.IsPaused,
                    "An incomplete orchard must not silently restart or lose saved fruits.");
            }
            finally
            {
                pause.Resume();
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseAndResumeRestoreGameTime()
        {
            var root = new GameObject("Pause Test");
            var menu = new GameObject("Pause Menu");
            menu.transform.SetParent(root.transform);
            menu.SetActive(false);
            var pause = root.AddComponent<PauseController>();
            pause.panel = menu;
            try
            {
                pause.Toggle();
                Assert.IsTrue(pause.IsPaused);
                Assert.IsTrue(menu.activeSelf);
                Assert.AreEqual(0f, Time.timeScale);
                pause.Resume();
                Assert.IsFalse(pause.IsPaused);
                Assert.IsFalse(menu.activeSelf);
                Assert.AreEqual(1f, Time.timeScale);
            }
            finally
            {
                pause.Resume();
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RebloomIsGradualAndOnlyMatureFruitCanBePicked()
        {
            var root = new GameObject("Rebloom Visual Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            var fruit = tree.Apples[0];
            var fullSize = fruit.transform.localScale;
            try
            {
                fruit.Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
                tree.ResetHarvestAnimated();

                Assert.IsTrue(tree.IsReblooming);
                Assert.AreEqual(0, tree.HarvestedCount);
                Assert.IsFalse(fruit.GetComponent<Collider>().enabled);
                Assert.AreEqual(Vector3.zero, fruit.transform.localScale);
                fruit.Pick();
                Assert.AreEqual(0, tree.HarvestedCount,
                    "Growing fruit must not be harvestable even through the public Pick method");

                var flowers = root.transform.Find("Merged Rebloom Flowers");
                Assert.IsNotNull(flowers, "The blossoms should share one merged draw mesh");
                Assert.IsTrue(flowers.gameObject.activeSelf);
                Assert.Less(flowers.GetComponent<MeshFilter>().sharedMesh.vertexCount, 13000);

                yield return new WaitForSecondsRealtime(2.1f);
                Assert.IsFalse(tree.IsReblooming);
                Assert.IsTrue(fruit.GetComponent<Collider>().enabled);
                Assert.That(Vector3.Distance(fullSize, fruit.transform.localScale),
                    Is.LessThan(.0001f));
                Assert.IsFalse(flowers.gameObject.activeSelf);
                fruit.Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReducedMotionRebloomSkipsBloomAndAllowsImmediatePicking()
        {
            int previous = PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0);
            var root = new GameObject("No Motion Orchard Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            try
            {
                tree.SetReducedMotion(true);
                tree.Apples[0].Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
                tree.ResetHarvestAnimated();
                Assert.IsFalse(tree.IsReblooming);
                Assert.IsTrue(tree.Apples[0].GetComponent<Collider>().enabled);
                Assert.IsTrue(tree.Apples[0].gameObject.activeSelf);
                Assert.IsNull(root.transform.Find("Merged Rebloom Flowers"),
                    "Reduced Motion should not allocate blossom meshes.");
                tree.Apples[0].Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
            }
            finally
            {
                PlayerPrefs.SetInt("FruitTree.ReducedMotion", previous);
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TurningOnReducedMotionDuringBloomFinishesItSafely()
        {
            int previous = PlayerPrefs.GetInt("FruitTree.ReducedMotion", 0);
            var root = new GameObject("Reduced Motion Toggle Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            try
            {
                tree.SetReducedMotion(false);
                tree.ResetHarvestAnimated();
                Assert.IsTrue(tree.IsReblooming);
                tree.SetReducedMotion(true);
                Assert.IsFalse(tree.IsReblooming);
                foreach (Apple fruit in tree.Apples)
                {
                    Assert.IsTrue(fruit.GetComponent<Collider>().enabled);
                    Assert.IsFalse(fruit.IsHarvested);
                    Assert.IsTrue(fruit.gameObject.activeSelf);
                }
            }
            finally
            {
                PlayerPrefs.SetInt("FruitTree.ReducedMotion", previous);
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SwitchingTreeDuringRebloomCancelsOldVisuals()
        {
            var root = new GameObject("Rebloom Interrupt Test");
            var tree = root.AddComponent<TreeGenerator>();
            if (tree.Apples.Count == 0) tree.Generate();
            try
            {
                tree.ResetHarvestAnimated();
                Assert.IsTrue(tree.IsReblooming);
                tree.Configure("pear", 140, 319);
                Assert.IsFalse(tree.IsReblooming);
                Assert.AreEqual(140, tree.Apples.Count);
                Assert.AreEqual(0, tree.HarvestedCount);
                Assert.IsTrue(tree.Apples[0].GetComponent<Collider>().enabled);
                var obsoleteFlowers = root.transform.Find("Merged Rebloom Flowers");
                if (obsoleteFlowers != null)
                    Assert.IsFalse(obsoleteFlowers.gameObject.activeSelf,
                        "Old bloom visuals must be hidden immediately on tree switch");
                yield return null; // Unity defers Object.Destroy until frame end.
                Assert.IsNull(root.transform.Find("Merged Rebloom Flowers"));
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneryLightingFinishesItsFadeEvenWhileGameIsPaused()
        {
            var root = new GameObject("Paused Scenery Color Test");
            var sunObject = new GameObject("Test Sun", typeof(Light));
            sunObject.transform.SetParent(root.transform, false);
            var sceneryObject = new GameObject("Environment");
            sceneryObject.transform.SetParent(root.transform, false);
            var environment = sceneryObject.AddComponent<EnvironmentController>();
            var sun = sunObject.GetComponent<Light>();
            environment.sun = sun;
            float originalScale = Time.timeScale;

            try
            {
                environment.ApplyThemeDirect(EnvironmentTheme.Meadow);
                Time.timeScale = 0f;
                environment.ApplyThemeDirect(EnvironmentTheme.CityPark);
                Assert.AreEqual(EnvironmentTheme.CityPark, environment.CurrentTheme);
                yield return new WaitForSecondsRealtime(1.2f);
                Color target = EnvironmentThemes.Sun(EnvironmentTheme.CityPark);
                Assert.That(sun.color.r, Is.EqualTo(target.r).Within(.015f));
                Assert.That(sun.color.g, Is.EqualTo(target.g).Within(.015f));
                Assert.That(sun.intensity, Is.EqualTo(.96f).Within(.015f));
                Assert.AreEqual(0f, Time.timeScale);
            }
            finally
            {
                Time.timeScale = originalScale;
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThemeAudioAndSceneryMeshReuseOnRepeatedSwitching()
        {
            var root = new GameObject("Environment Audio Integration Test");
            var scenery = new GameObject("Test Scenery");
            scenery.transform.SetParent(root.transform);
            var environment = scenery.AddComponent<EnvironmentController>();
            var audioNode = new GameObject("Test Ambient Sound");
            audioNode.transform.SetParent(root.transform);
            var sound = audioNode.AddComponent<OrchardAudio>();
            sound.environment = environment;
            yield return null; // Awake and Start of both runtime components.

            try
            {
                Assert.AreEqual(EnvironmentTheme.Meadow, sound.CurrentAmbientTheme);
                Assert.AreEqual(1, sound.CachedThemeClipCount);
                environment.UseForTree(1); // Park.
                Assert.AreEqual(EnvironmentTheme.Park, sound.CurrentAmbientTheme);
                Assert.AreEqual(2, sound.CachedThemeClipCount);

                var sceneryMesh = scenery.transform.Find("Merged Theme Scenery")
                    .GetComponent<MeshFilter>();
                Mesh parkMesh = sceneryMesh.sharedMesh;
                Assert.IsNotNull(parkMesh);
                environment.UseForTree(0);
                environment.UseForTree(1);
                Assert.AreSame(parkMesh, sceneryMesh.sharedMesh,
                    "Revisiting a biome must reuse cached geometry.");
                Assert.AreEqual(2, sound.CachedThemeClipCount,
                    "Revisiting a biome must reuse the small cached ambient clip.");

                environment.UseForTree(2);
                environment.UseForTree(3);
                Assert.AreEqual(4, sound.CachedThemeClipCount);
                Assert.AreEqual(EnvironmentTheme.CityPark, sound.CurrentAmbientTheme);
                Assert.AreEqual(1, scenery.GetComponentsInChildren<MeshRenderer>().Length,
                    "Only one combined decorative mesh renderer should be active.");
                yield return new WaitForSecondsRealtime(1.6f);
                Assert.AreEqual(EnvironmentTheme.CityPark, sound.CurrentAmbientTheme);
            }
            finally
            {
                Object.Destroy(root);
            }
            yield return null;
        }

    }
}
