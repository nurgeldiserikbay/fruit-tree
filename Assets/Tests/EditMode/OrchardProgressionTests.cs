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
                new TreeLevelDefinition("tree_003", "cherry", 220)
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
        public void InvalidSavedActiveTreeCannotSkipTheSequence()
        {
            var save = new OrchardSaveData { activeTreeId = "tree_003" };
            var game = new OrchardProgressionService(NewGame().Definitions, save);
            Assert.AreEqual("tree_001", game.ActiveTreeId);
            Assert.IsFalse(game.IsUnlocked("tree_003"));
        }
    }
}
