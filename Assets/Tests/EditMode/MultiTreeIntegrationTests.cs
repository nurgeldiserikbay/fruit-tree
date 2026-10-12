using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using FruitTree.Progression;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class MultiTreeIntegrationTests
    {
        [Test]
        public void SpeciesGenerateRequestedCountAndStableDistinctIds()
        {
            var root = new GameObject("Species Test");
            try
            {
                var tree = root.AddComponent<TreeGenerator>();
                CheckSpecies(tree, "apple", 120, 122);
                CheckSpecies(tree, "pear", 140, 319);
                CheckSpecies(tree, "cherry", 220, 516);
                CheckSpecies(tree, "peach", 130, 713);
                CheckSpecies(tree, "orange", 160, 910);
                CheckSpecies(tree, "apricot", 180, 1107);
                CheckSpecies(tree, "plum", 160, 1304);
                CheckSpecies(tree, "lemon", 140, 1501);
                CheckSpecies(tree, "mandarin", 200, 1698);
                CheckSpecies(tree, "pomegranate", 110, 1895);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CheckSpecies(TreeGenerator tree, string species, int count, int seed)
        {
            tree.Configure(species, count, seed);
            Assert.AreEqual(species, tree.SpeciesId);
            Assert.AreEqual(count, tree.Apples.Count);
            Assert.AreEqual(count, tree.TotalApples);
            var ids = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual("fruit_" + i.ToString("D4"), tree.Apples[i].StableFruitId);
                Assert.IsTrue(ids.Add(tree.Apples[i].StableFruitId));
            }
            Assert.AreEqual(count, ids.Count);
        }

        [Test]
        public void RestoringPickedFruitIdsDoesNotEmitPickEvents()
        {
            var root = new GameObject("Restore Test");
            try
            {
                var tree = root.AddComponent<TreeGenerator>();
                tree.Configure("apple", 120, 122);
                int pickEvents = 0;
                tree.FruitPicked += _ => pickEvents++;
                tree.RestorePickedFruits(new[] { "fruit_0000", "fruit_0042" });
                Assert.AreEqual(2, tree.HarvestedCount);
                Assert.IsFalse(tree.Apples[0].gameObject.activeSelf);
                Assert.IsFalse(tree.Apples[42].gameObject.activeSelf);
                Assert.IsTrue(tree.Apples[1].gameObject.activeSelf);
                Assert.AreEqual(0, pickEvents);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void SaveRepositoryRestoresActiveTreeAndPickedFruits()
        {
            string folder = Path.Combine(Path.GetTempPath(), "FruitTreeTest_" +
                Guid.NewGuid().ToString("N"));
            try
            {
                var catalog = new[]
                {
                    new TreeLevelDefinition("tree_001", "apple", 120),
                    new TreeLevelDefinition("tree_002", "pear", 140)
                };
                var game = new OrchardProgressionService(catalog);
                for (int i = 0; i < 120; i++)
                    Assert.IsTrue(game.TryPickActive(catalog[0].FruitIdAt(i)));
                Assert.IsTrue(game.SelectTree("tree_002"));
                Assert.IsTrue(game.TryPickActive("fruit_0017"));
                var repository = new OrchardSaveRepository(folder);
                Assert.IsTrue(repository.Save(game.ToSave()));
                var loaded = new OrchardProgressionService(catalog, repository.Load());
                Assert.AreEqual("tree_002", loaded.ActiveTreeId);
                Assert.IsTrue(loaded.IsUnlocked("tree_002"));
                Assert.AreEqual(1, loaded.GetTree("tree_002").PickedCount);
                Assert.IsTrue(new List<string>(loaded.GetTree("tree_002").PickedFruitIds)
                    .Contains("fruit_0017"));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void CorruptPrimarySaveFallsBackToPreviousValidCopy()
        {
            string folder = Path.Combine(Path.GetTempPath(), "FruitTreeBackupTest_" +
                Guid.NewGuid().ToString("N"));
            try
            {
                var store = new OrchardSaveRepository(folder);
                var first = new OrchardSaveData { activeTreeId = "tree_001" };
                first.trees.Add(new TreeSaveData { treeId = "tree_001", roundNumber = 1 });
                Assert.IsTrue(store.Save(first));
                var second = new OrchardSaveData { activeTreeId = "tree_002" };
                Assert.IsTrue(store.Save(second));
                File.WriteAllText(Path.Combine(folder, "fruit-tree-orchard.json"), "broken");
                Assert.AreEqual("tree_001", store.Load().activeTreeId);
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
    }
}
