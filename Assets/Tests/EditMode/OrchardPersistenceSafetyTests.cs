using System;
using System.IO;
using FruitTree.Progression;
using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class OrchardPersistenceSafetyTests
    {
        private string folder;

        [SetUp]
        public void CreateDirectory()
        {
            folder = Path.Combine(Path.GetTempPath(),
                "fruit-tree-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void RemoveDirectory()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private static OrchardSaveData Snapshot(int picked)
        {
            var game = new OrchardProgressionService(new[]
            {
                new TreeLevelDefinition("tree_001", "apple", 120),
                new TreeLevelDefinition("tree_002", "pear", 140)
            });
            for (int i = 0; i < picked; i++)
                Assert.IsTrue(game.TryPickActive("fruit_" + i.ToString("D4")));
            return game.ToSave();
        }

        [Test]
        public void SavingTwiceRestoresNewestPickingProgress()
        {
            var repo = new OrchardSaveRepository(folder);
            Assert.IsTrue(repo.Save(Snapshot(3)));
            Assert.IsTrue(repo.Save(Snapshot(14)));
            OrchardSaveData result = repo.Load();
            Assert.IsNotNull(result);
            var current = new OrchardProgressionService(new[]
            {
                new TreeLevelDefinition("tree_001", "apple", 120),
                new TreeLevelDefinition("tree_002", "pear", 140)
            }, result);
            Assert.AreEqual(14, current.GetTree("tree_001").PickedCount);
            Assert.IsFalse(current.IsUnlocked("tree_002"));
        }

        [Test]
        public void CompleteTemporarySaveRecoversWhenPrimaryIsCorrupt()
        {
            var repo = new OrchardSaveRepository(folder);
            Assert.IsTrue(repo.Save(Snapshot(3)));
            string path = Path.Combine(folder, "fruit-tree-orchard.json");
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(Snapshot(9)));
            File.WriteAllText(path, "{invalid truncated content");

            OrchardSaveData result = repo.Load();
            Assert.IsNotNull(result);
            Assert.AreEqual(9, result.trees[0].pickedFruitIds.Count,
                "Valid completed temporary write should beat an older backup.");
        }

        [Test]
        public void PreviousKnownGoodBackupSurvivesCorruptMainAndTemp()
        {
            var repo = new OrchardSaveRepository(folder);
            Assert.IsTrue(repo.Save(Snapshot(3)));
            Assert.IsTrue(repo.Save(Snapshot(9)));
            string path = Path.Combine(folder, "fruit-tree-orchard.json");
            File.WriteAllText(path, "{bad");
            File.WriteAllText(path + ".tmp", "{bad");
            OrchardSaveData recovered = repo.Load();
            Assert.IsNotNull(recovered);
            Assert.AreEqual(3, recovered.trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void LegacyVisualRestartCannotErasePartiallyPickedOrchardTree()
        {
            var root = new GameObject("Protected Orchard Restart");
            try
            {
                var tree = root.AddComponent<TreeGenerator>();
                if (tree.Apples.Count == 0) tree.Generate();
                var hud = root.AddComponent<HarvestHUD>();
                hud.OrchardMode = true;
                hud.tree = tree;
                tree.Apples[0].Pick();
                Assert.AreEqual(1, tree.HarvestedCount);
                hud.Restart();
                Assert.AreEqual(1, tree.HarvestedCount);
                Assert.IsTrue(tree.Apples[0].IsHarvested);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
