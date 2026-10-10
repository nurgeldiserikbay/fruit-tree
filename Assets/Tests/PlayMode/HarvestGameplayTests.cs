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
            tree.Generate();
            Assert.AreEqual(0, tree.HarvestedCount);
            Assert.AreEqual(30, tree.Apples.Count);
            foreach (var apple in tree.Apples)
                Assert.IsFalse(apple.IsHarvested);
            yield return null;
            Object.Destroy(root);
        }
    }
}
