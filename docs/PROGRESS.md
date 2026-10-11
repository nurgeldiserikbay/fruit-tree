# Fruit Tree development state

## Agreed scope
- Android only, portrait.
- One fruit tree in the center; no orchard walking.
- Full 3D: 360-degree orbit around tree, zoom into branches.
- Tap to collect red apples. Relaxing and atmospheric, no timer.
- Semi-realistic appearance, low UI density, no network dependence.

## Committed code
- [x] Project editor-version metadata updated to Unity 6000.6.5f1 (not yet compiled with this version)
- [x] Unity project and Android configuration
- [x] Deterministic batched tree generator (wood, leaf canopy, mesh apples)
- [x] Bark texture generator, asymmetric trunk / twigs, leaf-tip wind shader
- [x] Eliminate duplicate reverse leaf faces; material-controlled front/back culling
- [x] Touch orbit, pinch zoom, two-finger focus-height pan, aspect-ratio-based automatic framing
- [x] Apple interaction, animation, counter, allocation-free round reset (meshes and materials retained)
- [x] Softer procedural wind/birds and fruit pluck, optional replacement audio clips, persisted sound toggle
- [x] Auto-generated scene with meadow / sky and safe area
- [x] Minimal pause overlay, Android back button, restart and sound controls
- [x] Automatically pause when the Android app moves to the background
- [x] Continuous curved trunk/major-branch geometry and geometry unit test (not yet executed)
- [x] Safe Unity Resources folder creation through AssetDatabase
- [x] Editor scene validation command to check camera, menu, audio, shaders and Android build settings (not yet executed)
- [x] 360-degree fruit reachability sampling audit (reports warnings; not yet executed)
- [x] First-import shader import ordering hardened; asset-database scene checks
- [x] Android build fails early on unsupported platform and invalid scene; output anchored at project root
- [x] Backendless Remove Ads non-consumable IAP v5 integration (source committed; not executed in Unity)
- [x] Adult-access hold gate and pause-menu purchase/restore UI (disabled without ads SDK)
- [x] Fail-closed Families ad provider interface, store entitlement and guard unit tests (not executed)
- [x] Android build menu command
- [x] Development-only FPS and frame-time logging for eventual device QA (not yet measured)
- [x] Edit-mode geometry tests (not yet executed)
- [x] Play-mode tests for idempotent picking, stable round reset and interrupted animation (not yet executed)
- [x] Apple-skin texture, trunk occlusion and shadow draw-call reductions
- [x] Stem-to-twig attachment for every fruit (Unity test authored; not yet executed)
- [x] Explicit touch-position UI hit testing
- [x] Local Windows Unity test runner / optional APK script (not yet executed)
- [x] Static block-delimiter check across 15 C# scripts (passed; not a compiler run)
- [x] Prevent non-finite apple vertices at the top pole; regression test authored (not executed)

## NOT VERIFIED OR COMPLETE
- [ ] Unity 6000.6.5f1 package import, compile and scene-opening acceptance test
- [ ] Real Android device build and play test
- [ ] Real recorded nature audio and tuned sounds
- [ ] Review real-time tree shape, leaf shading, apple appearance and fruit placement using Unity screenshots; improve after review
- [ ] Frame-time, battery, heating profiling on low-end phones
- [ ] Google Play Console non-consumable product `remove_ads` and license test accounts
- [ ] Connect a Google Play Families self-certified ad SDK with correct child-directed settings
- [ ] Test purchases, pending/deferred orders, refunds and restore on Play internal-testing build
- [ ] Android signing, Google Play store release assets, privacy/data-safety declarations and age-rating review

No APK is included. Do not describe the game as production-ready without Unity builds and hardware testing.

## Testing handoff
See [SETUP.md](SETUP.md). The user's intended first manual check is near the end of development, but testing is indispensable before release.
