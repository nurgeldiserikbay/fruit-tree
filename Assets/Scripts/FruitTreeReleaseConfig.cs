namespace FruitTree
{
    /// <summary>
    /// Product-wide release switches. Ads are AdMob interstitials between tree
    /// changes, requested only as child-directed, G-rated, non-personalised ads,
    /// and never for players who bought the Google Play "remove_ads" item.
    /// </summary>
    public static class FruitTreeReleaseConfig
    {
        public const string LanguageCode = "en";
        public const bool AdvertisingEnabled = true;
        public const bool PurchasesEnabled = true;

        // Fruit Tree's own AdMob app and "Tree change" interstitial unit.
        public const string AdMobAndroidAppId = "ca-app-pub-9702825788968948~3234977815";
        // The phone APK and the testing AAB define FRUITTREE_TEST_ADS and carry
        // only Google's sample interstitial, which never shows real ads, so taps
        // while testing cannot count as invalid clicks. Only the production AAB
        // contains Fruit Tree's real "Tree change" unit.
#if FRUITTREE_TEST_ADS
        public const bool UsesTestAdUnit = true;
        public const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#else
        public const bool UsesTestAdUnit = false;
        public const string InterstitialAdUnitId = "ca-app-pub-9702825788968948/3419965593";
#endif
        public static bool UsesGoogleTestAds => AdMobAndroidAppId.StartsWith("ca-app-pub-3940256099942544");

        /// <summary>One interstitial for every this many changes to another tree.</summary>
        public const int TreeChangesPerAd = 3;

        public static bool CanOfferRemoveAds(bool certifiedAdProviderAvailable)
        {
            return AdvertisingEnabled && PurchasesEnabled &&
                certifiedAdProviderAvailable;
        }
    }
}
