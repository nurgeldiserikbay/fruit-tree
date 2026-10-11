# Fruit Tree: backend-free Remove Ads (Android)

## Current status and privacy-safe default

The code supports a Google Play **non-consumable** product called `remove_ads` using Unity IAP 5.0.4 for Unity 6.0.32f1.

**Important: NO advertising SDK is installed in this repository. No ads are shown and the Buy button remains disabled.** Charging for "removing ads" before actual ads exist would be misleading. The future ad provider has to be integrated separately and verified for Google Play Families compliance.

The non-consumable entitlement is retrieved from Google Play on launch with `FetchPurchases`, and after a paid pending order the app unlocks the entitlement and calls `ConfirmPurchase` to acknowledge it. The option is in the Pause menu under **FOR PARENTS - HOLD** (hold about 3 seconds). This is a friction gate, **not** age verification or a guarantee against purchases by children.

## What requires Play Console before a real purchase

1. Create/activate Google Play Console app with application ID `com.nurgeldiserikbay.fruittree` (confirm your actual publisher naming first).
2. Add a one-time in-app product with product ID exactly `remove_ads` (lowercase, no spaces). Set its price and activate it. There is **no hard-coded price** in the app; `Product.metadata.localizedPriceString` comes from the store.
3. Upload a signed AAB to an internal testing track and configure tester accounts and country availability; test only with Play-installed builds and license testers.
4. Install Android Build Support in Unity Hub and complete Play Console setup. Do not publish until the app passes purchase tests.
5. Test first purchase, cancel, deferred family approval, no connection, acknowledgement, purchase restore after reinstall, and revocation/refund on subsequent launch.

Google Play manages payment details. No custom backend or cloud database is involved. No Google Play credentials, key files or paid test tokens belong in GitHub.

## Advertising policy: default disabled

The `AdsAccessPolicy` interface is an explicit integration seam for a future `IFamilySafeAdProvider`.

An ad provider must never load or show ads unless ALL are true:

- A real, applicable Google Play Families self-certified ads SDK and permitted SDK version is used, with non-personalized child-directed settings.
- Store ownership lookup has succeeded in this session.
- The player does not own the non-consumable entitlement.
- The child audience, format and data practices have passed policy review.

`AdsAccessPolicy.CanRequestAds` applies this gate. On ownership changes, the provider must cancel pending ad loads, remove banners and prevent interstitials; its implementation must be audited. Since there is no ad provider yet, all these ad operations are absent and the build stays ad-free.

If Google Play is unreachable, the ownership state is `Unknown` and **ads are suppressed** until purchase history can be fetched. No editable PlayerPrefs flag is used as proof of ownership. Players can continue picking fruit offline, but purchases and restoring need Google Play connectivity.

## Important limitations

- Client-only entitlement checks are weaker than independent server-side purchase validation. A modified APK can potentially bypass checks. Backend-free does not mean tamperproof.
- The local state is set only from Unity IAP store order events, not from a button press, and is refreshed from Play each launch.
- Non-consumable purchases may be revoked/refunded; on a successful future store lookup the ownership state must be reevaluated.
- Check the current [Google Play Families policy](https://support.google.com/googleplay/android-developer/answer/9893335) and [Families self-certified ad SDK program](https://support.google.com/googleplay/android-developer/answer/9900633) **before** adding any ad SDK.
- The purchasing API reference follows [Unity IAP initialization](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing) and [purchase acknowledgement](https://docs.unity.com/en-us/iap/purchases).

## Test status

No Unity Editor or Play Console runtime tests were run in this coding environment. Written EditMode tests verify ad gating with a fake provider. They do not exercise Google Play Billing. Manual Play internal-testing verification is mandatory before release.
