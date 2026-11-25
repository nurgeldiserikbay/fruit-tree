using NUnit.Framework;

namespace FruitTree.Tests
{
    public sealed class AdPacingTests
    {
        [Test]
        public void AdComesOnEveryThirdTreeChange()
        {
            Assert.IsFalse(AdPacing.IsDue(1, 3));
            Assert.IsFalse(AdPacing.IsDue(2, 3));
            Assert.IsTrue(AdPacing.IsDue(3, 3));
            Assert.IsTrue(AdPacing.IsDue(4, 3), "A missed ad is tried again on the next change.");
            Assert.IsFalse(AdPacing.IsDue(5, 0));
        }

        [Test]
        public void RemoveAdsIsOfferedOnlyWithARealAdProvider()
        {
            Assert.IsFalse(FruitTreeReleaseConfig.CanOfferRemoveAds(false));
            Assert.AreEqual(FruitTreeReleaseConfig.AdvertisingEnabled && FruitTreeReleaseConfig.PurchasesEnabled,
                FruitTreeReleaseConfig.CanOfferRemoveAds(true));
        }
    }
}
