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

        // Google's public sample IDs: they only ever show test ads. Replace both
        // with the app's own AdMob IDs before publishing to Google Play.
        public const string AdMobAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
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
