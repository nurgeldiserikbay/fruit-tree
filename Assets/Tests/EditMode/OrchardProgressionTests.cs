using NUnit.Framework;
using FruitTree.Progression;

namespace FruitTree.Tests
{
    public sealed class OrchardProgressionTests
    {
        private static OrchardProgressionService NewGame()
        {
            return new OrchardProgressionService(new[]
            {
                new TreeLevelDefinition("tree_001", "apple", 120),
                new TreeLevelDefinition("tree_002", "pear", 140),
                new TreeLevelDefinition("tree_003", "cherry", 220),
                new TreeLevelDefinition("tree_004", "peach", 130),
                new TreeLevelDefinition("tree_005", "orange", 160)
            });
        }

        [Test]
        public void OnlyFirstTreeIsAvailableInitially()
        {
            var game = NewGame();
            Assert.IsTrue(game.IsUnlocked("tree_001"));
            Assert.IsFalse(game.IsUnlocked("tree_002"));
            Assert.IsFalse(game.IsUnlocked("tree_003"));
            Assert.IsFalse(game.SelectTree("tree_002"));
            Assert.AreEqual("tree_001", game.ActiveTreeId);
        }

        [Test]
        public void LastFruitUnlocksExactlyNextTree()
        {
            var game = NewGame();
            var tree = game.GetTree("tree_001");
            for (int i = 0; i < 119; i++)
                Assert.IsTrue(game.TryPickActive(tree.Definition.FruitIdAt(i)));
            Assert.IsFalse(game.IsUnlocked("tree_002"));
            Assert.IsTrue(game.TryPickActive("fruit_0119"));
            Assert.IsTrue(tree.IsCleared);
            Assert.AreEqual(1, tree.TimesCleared);
            Assert.IsTrue(game.IsUnlocked("tree_002"));
            Assert.IsFalse(game.IsUnlocked("tree_003"));
            Assert.IsFalse(game.TryPickActive("fruit_0119"));
            Assert.AreEqual(1, tree.TimesCleared);
        }

        [Test]
        public void ReplayKeepsEarlierUnlocks()
        {
            var game = NewGame();
            for (int i = 0; i < 120; i++)
                game.TryPickActive(game.GetTree("tree_001").Definition.FruitIdAt(i));
            Assert.IsTrue(game.TryRebloomActive());
            Assert.AreEqual(0, game.GetTree("tree_001").PickedCount);
            Assert.AreEqual(2, game.GetTree("tree_001").RoundNumber);
            Assert.IsTrue(game.IsUnlocked("tree_002"));
            Assert.IsFalse(game.TryRebloomActive());
        }

        [Test]
        public void SaveRoundTripKeepsExactFruitsAndUnlockedTree()
        {
            var game = NewGame();
            for (int i = 0; i < 120; i++)
                game.TryPickActive(game.GetTree("tree_001").Definition.FruitIdAt(i));
            Assert.IsTrue(game.SelectTree("tree_002"));
            for (int i = 0; i < 72; i++)
                Assert.IsTrue(game.TryPickActive(game.GetTree("tree_002").Definition.FruitIdAt(i)));

            var saved = game.ToSave();
            var loaded = new OrchardProgressionService(game.Definitions, saved);
            Assert.AreEqual("tree_002", loaded.ActiveTreeId);
            Assert.AreEqual(72, loaded.GetTree("tree_002").PickedCount);
            Assert.IsTrue(loaded.IsUnlocked("tree_002"));
            Assert.IsFalse(loaded.IsUnlocked("tree_003"));
            Assert.IsFalse(loaded.TryPickActive("fruit_0000"));
            Assert.IsTrue(loaded.TryPickActive("fruit_0072"));
        }

        [Test]
        public void InvalidFruitIdsCannotBypassCompletion()
        {
            var game = NewGame();
            Assert.IsFalse(game.TryPickActive("fruit_0120"));
            Assert.IsFalse(game.TryPickActive("fruit_fake"));
            Assert.IsFalse(game.TryPickActive(null));
            Assert.IsFalse(game.IsUnlocked("tree_002"));
        }

        [Test]
        public void PeachAndOrangeRequireFullPreviousClearEvenAfterReplay()
        {
            var game = NewGame();
            Assert.IsFalse(game.IsUnlocked("tree_004"));
            Assert.IsFalse(game.IsUnlocked("tree_005"));
            foreach (string treeId in new[] { "tree_001", "tree_002", "tree_003" })
            {
                Assert.IsTrue(game.SelectTree(treeId));
                var level = game.GetTree(treeId).Definition;
                for (int i = 0; i < level.FruitCount - 1; i++)
                    Assert.IsTrue(game.TryPickActive(level.FruitIdAt(i)));
                string nextId = treeId == "tree_001" ? "tree_002" :
                    treeId == "tree_002" ? "tree_003" : "tree_004";
                Assert.IsFalse(game.IsUnlocked(nextId));
                Assert.IsTrue(game.TryPickActive(level.FruitIdAt(level.FruitCount - 1)));
                Assert.IsTrue(game.IsUnlocked(nextId));
            }
            Assert.IsTrue(game.SelectTree("tree_004"));
            Assert.IsFalse(game.IsUnlocked("tree_005"));
            for (int i = 0; i < 129; i++)
                Assert.IsTrue(game.TryPickActive("fruit_" + i.ToString("D4")));
            Assert.IsFalse(game.IsUnlocked("tree_005"));
            Assert.IsTrue(game.TryPickActive("fruit_0129"));
            Assert.IsTrue(game.IsUnlocked("tree_005"));
            Assert.IsTrue(game.SelectTree("tree_001"));
            Assert.IsTrue(game.TryRebloomActive());
            Assert.IsTrue(game.IsUnlocked("tree_005"));
        }

        [Test]
        public void ExistingThreeTreeSaveUpgradesWithoutGrantingNewLevels()
        {
            var previous = new OrchardProgressionService(new[]
            {
                new TreeLevelDefinition("tree_001", "apple", 120),
                new TreeLevelDefinition("tree_002", "pear", 140),
                new TreeLevelDefinition("tree_003", "cherry", 220)
            });
            foreach (string id in new[] { "tree_001", "tree_002" })
            {
                Assert.IsTrue(previous.SelectTree(id));
                var def = previous.GetTree(id).Definition;
                for (int i = 0; i < def.FruitCount; i++)
                    Assert.IsTrue(previous.TryPickActive(def.FruitIdAt(i)));
            }
            Assert.IsTrue(previous.SelectTree("tree_003"));
            Assert.IsTrue(previous.TryPickActive("fruit_0007"));

            OrchardSaveData oldSave = previous.ToSave();
            var updated = new OrchardProgressionService(NewGame().Definitions, oldSave);
            Assert.AreEqual("tree_003", updated.ActiveTreeId);
            Assert.AreEqual(1, updated.GetTree("tree_003").PickedCount);
            Assert.IsTrue(updated.IsUnlocked("tree_003"));
            Assert.IsFalse(updated.IsUnlocked("tree_004"));
            Assert.IsFalse(updated.IsUnlocked("tree_005"));
        }

        [Test]
        public void InvalidSavedActiveTreeCannotSkipTheSequence()
        {
            var save = new OrchardSaveData { activeTreeId = "tree_003" };
            var game = new OrchardProgressionService(NewGame().Definitions, save);
            Assert.AreEqual("tree_001", game.ActiveTreeId);
            Assert.IsFalse(game.IsUnlocked("tree_003"));
        }
    }
}
