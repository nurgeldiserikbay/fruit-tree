using System;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Explicitly fails closed: no ad request before Google Play ownership has
    /// been fetched, and never any ad request without a verified child-safe SDK.
    /// Currently NO ad SDK is included, so no ads are shown.
    /// </summary>
    public sealed class AdsAccessPolicy : MonoBehaviour
    {
        private IFamilySafeAdProvider provider;
        public bool EntitlementChecked { get; private set; }
        public bool OwnsRemoveAds { get; private set; }
        public bool HasAdProvider => provider != null && provider.FamiliesConfigurationApproved;
        public bool CanRequestAds => FruitTreeReleaseConfig.AdvertisingEnabled &&
            EntitlementChecked && !OwnsRemoveAds && HasAdProvider;
        public event Action StateChanged;

        public void AttachProvider(IFamilySafeAdProvider certifiedProvider)
        {
            provider = certifiedProvider;
            SyncProvider();
        }

        public void MarkEntitlementUnknown()
        {
            EntitlementChecked = false;
            SyncProvider();
        }

        public void SetStoreEntitlement(bool ownsRemoveAds)
        {
            EntitlementChecked = true;
            OwnsRemoveAds = ownsRemoveAds;
            SyncProvider();
        }

        private void SyncProvider()
        {
            if (provider != null) provider.SetAdvertisingEnabled(CanRequestAds);
            StateChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (provider != null) provider.SetAdvertisingEnabled(false);
        }
    }

    /// <summary>
    /// An explicit opt-in integration seam for a future Google Play Families
    /// self-certified provider. Do not implement this with an uncertified SDK.
    /// </summary>
    public interface IFamilySafeAdProvider
    {
        bool FamiliesConfigurationApproved { get; }
        void SetAdvertisingEnabled(bool enabled);
    }
}
