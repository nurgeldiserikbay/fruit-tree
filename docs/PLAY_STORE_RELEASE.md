# Fruit Tree — Google Play release readiness

**Status (2026-10-12): Not ready for production. English-only, ad-free first release.** This is a checklist and evidence plan; do not mark a feature complete merely because C# code is committed.

## Gameplay / UX acceptance

- [x] Source: ten-species code catalog and procedurally modeled fruit collection (not yet compiled or visually accepted in Unity).
- [x] Source: local saved progress, 100%-clear tree unlock and rebloom.
- [x] Source: main menu, CONTINUE, MY TREES, SETTINGS, HOW TO PLAY, Pause → Main Menu, privacy summary and link configuration, with translucent hero and live saved-tree progress text (committed; awaiting Unity visual QA).
- [x] Source: voluntary last-ten-fruit camera guide, Zen Mode (HUD-free), soft audio/motion preferences and collection switch close/resume.
- [x] Source: regression tests for Zen Mode and last-fruit hint thresholds (not run in Unity).
- [ ] Build and play-test every button, Zen Mode, hint and Android Back behaviour in Unity 6000.6.5f1.
- [x] Source: ten species with unique profile parameters and compact fruit meshes.
- [ ] Confirm all ten species are visually recognizable and every fruit can be picked on an Android phone; final art and playability not accepted.
- [x] Source: four switchable low-cost environments — Meadow, Park, City Park, Mountains — now with ridge depth, distant scenery and cached geometry, plus offline per-environment synthesized audio and a gentle crossfade (not yet Unity-tested).
- [x] Source: first natural-apple mesh/skin pass, distinct lighter round cherry mesh, 64×64 pear/cherry skin maps, smaller fruit proportions, curved short grass and soft clear-tree feedback (not yet Unity-tested).
- [x] Source: merged blossom animation, species-specific leaf forms/wood tints and startup duplicate-tree optimization (Unity PlayMode QA pending).
- [ ] Finish final-quality fruit/branch/foliage meshes, terrain and lighting; visually approve four environments in Unity.
- [x] Source: bounded finger-sized tap fallback and voluntary last-fruit guide with short-lived non-blocking marker (not yet tested in Unity).
- [x] Source: **Fruit Tree → Audit All Ten Tree Species** checks sampled 360-degree camera raycasts for every fruit and fails a signed AAB build if any are unreachable (not run in Unity).
- [ ] Every fruit on every tree reachable and 100% clear achievable without frustration; run the Editor audit and verify the optional last-fruit assistance on real phones.
- [x] Source: live fruit progress strip and pooled, touch-through 0.42-second harvest glints; new edit-mode tests authored.
- [ ] Responsive 9:16, 9:20 and cut-out / safe-area UI testing.
- [ ] Final natural atmosphere recordings (or approved synthesized audio), licensed/owned SFX and final sound mixing; comfort volume/motion settings are in source but not QA-approved.
- [x] **English-only UI and Play listing.** No Kazakh/Russian language selector or localization promised.
- [x] Source: reject invalid/duplicate saved tree records and authored offline backup/interrupted-write recovery tests (not yet run).
- [ ] Graceful offline operation, app switch/pause, interrupted save and progress restore confirmed on real Android devices.
- [x] Source: provisional mobile Low/Balanced/High quality tier selection; unknown hardware safely defaults to Balanced.
- [ ] Profiling on real low-/mid-range Android phones (FPS, heat, RAM, battery).

## Play Console and privacy

- [ ] Decide the exact developer/publisher display name and functioning support email.
- [ ] Publish the final Privacy Policy as a **public read-only webpage** (Google Docs → Publish to web is an option), then put the exact URL in `FruitTreePrivacyConfig.PublishedPolicyUrl` and Play Console.
- [ ] Confirm that the in-game Settings → Privacy link opens the published policy; add Play Console Privacy Policy URL.
- [ ] Audit all SDKs, Android permissions, collected/shared data and complete the **Data safety** declaration truthfully.
- [ ] Review **Google Play Families**/target audience content, ad SDK certification, purchase gates and suitability.
- [ ] Do not enable ads until a compliant partner is integrated and audited. Never charge to remove nonexistent ads.
- [x] No ads and no paid IAP offer in the first release. Legacy Remove Ads code is dormant; don't activate it until a real compliant ad provider exists and is tested.
- [ ] Apply a content rating and audience declarations.
- [ ] Confirm whether account deletion provisions apply; current design has no in-app user account.

## Android release engineering

- [ ] Verify **target API >= 36** for new Android mobile app submissions from 2026-08-31 (or an approved extension, where applicable).
- [ ] Check actual minSdk and permissions in merged Android manifest.
- [x] Source: guarded Unity menu command **Fruit Tree → Build Signed Android AAB**. It blocks missing public policy, keystore, ARM64, IL2CPP, API 36+, outdated scene versions and failed all-ten-species reachability audit.
- [ ] Actually generate and install the signed **release AAB** with the publisher's private upload key; local Unity Build Support and real policy URL are required.
- [ ] Use private keystore/upload key; do NOT commit secrets. Enable Play App Signing.
- [ ] Install an internal-track build from Play Store; test install, resume, offline save, UI and Android 16. Billing is disabled for the ad-free launch.
- [ ] Verify orientation, app label, package ID, adaptive launcher icon, no debug/development switches and no unintended permissions.
- [ ] Increment versionName/versionCode per release.
- [ ] Configure Play Console testing track, testers, countries and (if required) testing participation criteria.

## Google Play store assets / marketing

- [ ] Final 512×512 store icon, Android adaptive icon (foreground/background/monochrome as needed).
- [ ] 1024×500 feature graphic, no misleading screenshots / text.
- [ ] Authentic portrait phone screenshots from the **actual** working game (menus + each fruit species + completion).
- [x] English-only Play Store description draft; copy must be confirmed against the tested build.
- [ ] Authentic English gameplay screenshots and release-approved descriptions.
- [ ] Support email, developer contact, privacy URL and accurate disclosures.
- [ ] Screenshots reflect the in-app visuals (do not substitute concept illustrations for gameplay).

## Launch blocker list

1. All UI screens exist in source, but **have not been compiled, tested, or visually accepted in Unity**. Existing generated local scenes must be refreshed (with backup) to show new UI.
2. Policy URL, contact email and final Google Docs publication are missing.
3. SDK data audit, AAB signature and Play Console setup not done.
4. Ten species exist as unverified procedural source models; final visual assets, fruit accessibility and Android performance require substantial work.
5. Every-fruit reachability and app navigation need on-device tests. No purchase flow should be accessible in this ad-free release.

References:
- Privacy policy / Data safety: https://support.google.com/googleplay/android-developer/answer/10144311
- Google Docs Publish to web: https://support.google.com/docs/answer/183965
- Target SDK: https://support.google.com/googleplay/android-developer/answer/11926878
- Families: https://support.google.com/googleplay/android-developer/answer/9893335
