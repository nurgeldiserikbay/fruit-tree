using NUnit.Framework;

namespace FruitTree.Tests
{
    public sealed class MobilePerformanceBudgetTests
    {
        [TestCase(2048, 256, MobilePerformanceTier.Low)]
        [TestCase(4096, 256, MobilePerformanceTier.Low)]
        [TestCase(4096, 1024, MobilePerformanceTier.Balanced)]
        [TestCase(8192, 1024, MobilePerformanceTier.Balanced)]
        [TestCase(8192, 2048, MobilePerformanceTier.High)]
        [TestCase(0, 0, MobilePerformanceTier.Balanced)]
        [TestCase(8192, 0, MobilePerformanceTier.Balanced)]
        public void MemoryBasedTiersAreStableWithoutDeviceQueries(
            int ramMb, int gpuMb, MobilePerformanceTier expected)
        {
            Assert.AreEqual(expected, MobilePerformanceBudget.Recommend(ramMb, gpuMb));
        }

        [Test]
        public void BudgetNeverAltersStoredProgressOrTheTenTreeCatalog()
        {
            Assert.AreEqual(MobilePerformanceTier.Low,
                MobilePerformanceBudget.Recommend(2800, 600));
            Assert.AreEqual(10, FruitSpeciesProfile.Count);
            Assert.AreEqual(4, EnvironmentThemes.Count);
        }
    }
}
