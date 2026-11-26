using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// AdMob interstitials for the Google Play Families audience: every request is
    /// tagged child-directed and under the age of consent, with a G content rating,
    /// so ads are non-personalised. Loads only while AdsAccessPolicy allows ads.
    /// </summary>
    public sealed class AdMobInterstitialProvider : MonoBehaviour, IFamilySafeAdProvider
    {
        private const float RetrySeconds = 30f;
        private bool allowed, initializing, initialized, loading;
        private InterstitialAd ad;
        private Action closed;

        public bool FamiliesConfigurationApproved => true;
        public bool IsReady => allowed && ad != null && ad.CanShowAd();

        // Only the real game scene gets ads, never bare test rigs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            if (!FruitTreeReleaseConfig.AdvertisingEnabled) return;
            var policy = FindFirstObjectByType<AdsAccessPolicy>();
            if (policy == null) return;
            var provider = policy.gameObject.AddComponent<AdMobInterstitialProvider>();
            policy.AttachProvider(provider);
            // The scene keeps billing and the parent entry off; switch them on here.
            var purchaser = FindFirstObjectByType<RemoveAdsPurchaser>(FindObjectsInactive.Include);
            if (purchaser != null) purchaser.enabled = FruitTreeReleaseConfig.PurchasesEnabled;
            foreach (var gate in FindObjectsByType<ParentHoldGate>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                gate.gameObject.SetActive(FruitTreeReleaseConfig.CanOfferRemoveAds(true));
        }

        public void SetAdvertisingEnabled(bool enabled)
        {
            allowed = enabled;
            if (enabled) EnsureInitialized();
            else DestroyAd();
        }

        /// <summary>Shows a loaded ad; <paramref name="onClosed"/> runs when it is dismissed.</summary>
        public bool TryShow(Action onClosed)
        {
            if (!IsReady) return false;
            closed = onClosed;
            ad.Show();
            return true;
        }

        private void EnsureInitialized()
        {
            if (initialized) { Load(); return; }
            if (initializing) return;
            initializing = true;
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.True,
                TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.True,
                MaxAdContentRating = MaxAdContentRating.G
            });
            MobileAds.Initialize(status =>
            {
                initializing = false;
                initialized = true;
                if (allowed) Load();
            });
        }

        private void Load()
        {
            if (!allowed || loading || (ad != null && ad.CanShowAd())) return;
            loading = true;
            InterstitialAd.Load(FruitTreeReleaseConfig.InterstitialAdUnitId, new AdRequest(),
                (loaded, error) =>
                {
                    loading = false;
                    if (error != null || loaded == null)
                    {
                        Invoke(nameof(Load), RetrySeconds);
                        return;
                    }
                    if (!allowed)
                    {
                        loaded.Destroy();
                        return;
                    }
                    DestroyAd();
                    ad = loaded;
                    ad.OnAdFullScreenContentClosed += Finished;
                    ad.OnAdFullScreenContentFailed += failure => Finished();
                });
        }

        private void Finished()
        {
            Action callback = closed;
            closed = null;
            DestroyAd();
            callback?.Invoke();
            Load();
        }

        private void DestroyAd()
        {
            CancelInvoke(nameof(Load));
            if (ad == null) return;
            ad.Destroy();
            ad = null;
        }

        private void OnDestroy()
        {
            allowed = false;
            DestroyAd();
        }
    }

    /// <summary>Counts changes to another tree and says when an interstitial is due.</summary>
    public static class AdPacing
    {
        private const string CounterKey = "FruitTree.TreeChangesSinceAd";

        public static bool IsDue(int changesSinceAd, int every)
        {
            return every > 0 && changesSinceAd >= every;
        }

        /// <summary>Counts one tree change; true when it is time for an ad.</summary>
        public static bool RegisterTreeChange()
        {
            int changes = PlayerPrefs.GetInt(CounterKey, 0) + 1;
            PlayerPrefs.SetInt(CounterKey, changes);
            return IsDue(changes, FruitTreeReleaseConfig.TreeChangesPerAd);
        }

        /// <summary>Starts counting again after an ad was actually shown.</summary>
        public static void MarkShown()
        {
            PlayerPrefs.SetInt(CounterKey, 0);
            PlayerPrefs.Save();
        }
    }
}
