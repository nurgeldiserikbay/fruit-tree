namespace FruitTree
{
    /// <summary>
    /// Product-wide defaults for the first English-only, ad-free release.
    /// Do not turn ads or billing on without real ads, a policy/privacy audit,
    /// store product configuration and on-device purchase tests.
    /// </summary>
    public static class FruitTreeReleaseConfig
    {
        public const string LanguageCode = "en";
        public const bool AdvertisingEnabled = false;
        public const bool PurchasesEnabled = false;

        public static bool CanOfferRemoveAds(bool certifiedAdProviderAvailable)
        {
            return AdvertisingEnabled && PurchasesEnabled &&
                certifiedAdProviderAvailable;
        }
    }
}
