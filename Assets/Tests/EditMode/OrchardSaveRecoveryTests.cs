using System;
using System.IO;
using NUnit.Framework;
using FruitTree.Progression;

namespace FruitTree.Tests
{
    public sealed class OrchardSaveRecoveryTests
    {
        private string folder;
        private OrchardSaveRepository repository;

        [SetUp]
        public void Setup()
        {
            folder = Path.Combine(Path.GetTempPath(),
                "FruitTreeRecovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            repository = new OrchardSaveRepository(folder);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private static OrchardSaveData Create(int picked)
        {
            var game = new OrchardProgressionService(new[]
            {
                new TreeLevelDefinition("tree_001", "apple", 120)
            });
            for (int i = 0; i < picked; i++)
                game.TryPickActive("fruit_" + i.ToString("D4"));
            return game.ToSave();
        }

        [Test]
        public void CorruptPrimaryFallsBackToLastValidBackup()
        {
            Assert.IsTrue(repository.Save(Create(4)));
            Assert.IsTrue(repository.Save(Create(6)));
            File.WriteAllText(Path.Combine(folder, "fruit-tree-orchard.json"),
                "{ \"schemaVersion\":1, \"activeTreeId\":\"tree_001\", \"trees\":[] }");
            var restored = repository.Load();
            Assert.IsNotNull(restored);
            var state = new OrchardProgressionService(new[]
            {
                new TreeLevelDefinition("tree_001", "apple", 120)
            }, restored);
            Assert.AreEqual(4, state.GetTree("tree_001").PickedCount);
        }

        [Test]
        public void TemporaryValidSnapshotRecoversInterruptedWrite()
        {
            Assert.IsTrue(repository.Save(Create(3)));
            File.WriteAllText(Path.Combine(folder, "fruit-tree-orchard.json.tmp"),
                UnityEngine.JsonUtility.ToJson(Create(11)));
            File.WriteAllText(Path.Combine(folder, "fruit-tree-orchard.json"),
                "not json");
            var restored = repository.Load();
            Assert.IsNotNull(restored);
            Assert.AreEqual(11, restored.trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void InvalidInMemorySnapshotCannotReplaceValidOrchardProgress()
        {
            Assert.IsTrue(repository.Save(Create(8)));
            string savePath = Path.Combine(folder, "fruit-tree-orchard.json");
            string before = File.ReadAllText(savePath);

            var malformed = Create(10);
            malformed.trees.Clear();
            Assert.IsFalse(repository.Save(malformed));
            Assert.AreEqual(before, File.ReadAllText(savePath));
            Assert.AreEqual(8, repository.Load().trees[0].pickedFruitIds.Count);

            var duplicate = Create(12);
            duplicate.trees.Add(duplicate.trees[0]);
            Assert.IsFalse(repository.Save(duplicate));
            Assert.AreEqual(before, File.ReadAllText(savePath));
            Assert.AreEqual(8, repository.Load().trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void DuplicateTreeEntriesAreRejectedInFavorOfBackup()
        {
            Assert.IsTrue(repository.Save(Create(2)));
            Assert.IsTrue(repository.Save(Create(5)));
            var broken = Create(7);
            broken.trees.Add(broken.trees[0]);
            File.WriteAllText(Path.Combine(folder, "fruit-tree-orchard.json"),
                UnityEngine.JsonUtility.ToJson(broken));
            Assert.AreEqual(2, repository.Load().trees[0].pickedFruitIds.Count);
        }
        [Test]
        public void DuplicateOrMalformedFruitIdCannotOverwriteValidProgress()
        {
            Assert.IsTrue(repository.Save(Create(14)));
            string primary = Path.Combine(folder, "fruit-tree-orchard.json");
            string before = File.ReadAllText(primary);

            var duplicated = Create(15);
            duplicated.trees[0].pickedFruitIds.Add("fruit_0000");
            Assert.IsFalse(repository.Save(duplicated));
            Assert.AreEqual(before, File.ReadAllText(primary));

            foreach (string invalid in new[]
                { null, "", "fruit_fake", "fruit_001", "fruit_000x" })
            {
                var malformed = Create(15);
                malformed.trees[0].pickedFruitIds.Add(invalid);
                Assert.IsFalse(repository.Save(malformed), invalid ?? "<null>");
                Assert.AreEqual(before, File.ReadAllText(primary));
            }
            Assert.AreEqual(14, repository.Load().trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void CorruptFruitIdInPrimaryFallsBackToLastValidBackup()
        {
            Assert.IsTrue(repository.Save(Create(3)));
            Assert.IsTrue(repository.Save(Create(9)));
            var broken = Create(12);
            broken.trees[0].pickedFruitIds.Add("fruit_0000");
            string primary = Path.Combine(folder, "fruit-tree-orchard.json");
            File.WriteAllText(primary, UnityEngine.JsonUtility.ToJson(broken));

            // The last known-good backup is more reliable than a malformed
            // primary JSON that merely looks structurally complete.
            Assert.AreEqual(3, repository.Load().trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void NewerCompleteTemporaryIsRecoveredEvenIfPrimaryIsValid()
        {
            Assert.IsTrue(repository.Save(Create(3)));
            string main = Path.Combine(folder, "fruit-tree-orchard.json");
            string temporary = main + ".tmp";
            File.WriteAllText(temporary, UnityEngine.JsonUtility.ToJson(Create(11)));
            File.SetLastWriteTimeUtc(main, DateTime.UtcNow.AddMinutes(-10));
            File.SetLastWriteTimeUtc(temporary, DateTime.UtcNow.AddMinutes(-5));

            Assert.AreEqual(11, repository.Load().trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void OlderStaleTemporaryCannotRollBackValidPrimary()
        {
            Assert.IsTrue(repository.Save(Create(14)));
            string main = Path.Combine(folder, "fruit-tree-orchard.json");
            string temporary = main + ".tmp";
            File.WriteAllText(temporary, UnityEngine.JsonUtility.ToJson(Create(2)));
            File.SetLastWriteTimeUtc(temporary, DateTime.UtcNow.AddMinutes(-10));
            File.SetLastWriteTimeUtc(main, DateTime.UtcNow.AddMinutes(-5));

            Assert.AreEqual(14, repository.Load().trees[0].pickedFruitIds.Count);
        }

        [Test]
        public void NewerButInvalidTemporaryCannotReplaceValidPrimary()
        {
            Assert.IsTrue(repository.Save(Create(9)));
            string main = Path.Combine(folder, "fruit-tree-orchard.json");
            string temporary = main + ".tmp";
            File.WriteAllText(temporary, "{invalid json");
            File.SetLastWriteTimeUtc(main, DateTime.UtcNow.AddMinutes(-10));
            File.SetLastWriteTimeUtc(temporary, DateTime.UtcNow.AddMinutes(-5));

            Assert.AreEqual(9, repository.Load().trees[0].pickedFruitIds.Count);
        }

    }
}
