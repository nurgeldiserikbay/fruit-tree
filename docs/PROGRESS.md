# Fruit Tree development state

## Front-end and Google Play (2026-10-12)

- [x] Source: main menu with Continue / My Trees / Settings / How to Play.
- [x] Quiet-audio, mute and reduced-leaf-motion preferences stored locally (source committed; Editor/device QA pending).
- [x] Source: Pause → Main Menu and Settings, screen-aware Android Back behaviour.
- [x] Source: in-app privacy summary, safe published-Google-Docs URL configuration and disable-on-missing-link logic.
- [x] Draft Google Docs Privacy Policy, Play listing text and release-readiness checklist.
- [x] EditMode tests for navigation and Privacy URL validation (not yet executed).
- [ ] User Unity 6000.6.5f1 compilation and portrait visual tests of new screens.
- [ ] Publish real privacy policy, replace contact placeholders, complete SDK data audit, add verified public URL.
- [ ] Android release AAB, API 36, store assets, 10 distinct trees, localization and real-device tests.


## Approved future direction (2026-10-11)

- [x] GDD: clear every fruit on one tree before unlocking the next species; replay previously cleared trees via rebloom.
- [x] Target architecture and staged migration document for multi-tree progression.
- [x] Non-breaking core: tree definitions, 100%-clear/unlock rules, rebloom, in-memory save DTO and progression EditMode tests (written, not run in Unity).
- [ ] Disk persistence, stable prefab fruit IDs and connection to Unity gameplay — not implemented.
- [x] Three-tree playable controller, local per-fruit save repository, unlock on 100%, rebloom and tree selection integrated into generated scene (code committed; NOT YET Unity-tested).
- [x] Voluntary last-ten-fruit camera guide and collection-switch pause closure (source committed; pending Unity verification).
- [ ] Verify Unity compilation, actual 120/140/220 harvest, unlock/restore and device performance.
- [x] First vertical slice species Apple, Pear, Cherry in procedural prototype form (not yet polished or play-tested).
- [ ] Expand to ten individually authored fruit-tree species after the vertical slice passes.

See [GAME_DESIGN.md](GAME_DESIGN.md), [ARCHITECTURE.md](ARCHITECTURE.md), [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md).


## Agreed scope
- Android only, portrait.
- One fruit tree in the center; no orchard walking.
- Full 3D: 360-degree orbit around tree, zoom into branches.
- Tap to collect red apples. Relaxing and atmospheric, no timer.
- Semi-realistic appearance, low UI density, no network dependence.

## Committed code
- [x] Project editor-version metadata updated to Unity 6000.6.5f1; user confirms Editor Game view renders; Console warnings awaiting recheck
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
- [x] Screenshot-based foliage/meadow/fruit and rounded HUD visual iteration (reviewed in Unity screenshots)
- [x] Increased gameplay instructions and pause-menu button text sizes for portrait readability (awaiting Unity verification; camera controls left unchanged)
- [x] Scene refresh command with local backup before rebuilding UI
- [x] Safe Unity Resources folder creation through AssetDatabase
- [x] Editor scene validation command to check camera, menu, audio, shaders and Android build settings (not yet executed)
- [x] 360-degree fruit reachability sampling audit (reports warnings; not yet executed)
- [x] First-import shader import ordering hardened; asset-database scene checks
- [x] Android build fails early on unsupported platform and invalid scene; output anchored at project root
- [x] Backendless Remove Ads non-consumable IAP v5 integration (source committed; not executed in Unity)
- [x] Adult-access hold gate and pause-menu purchase/restore UI (disabled without ads SDK)
- [x] Fail-closed Families ad provider interface, store entitlement and guard unit tests (not executed)
- [x] Android build menu command
- [x] Development-only FPS and frame-time logging via Debug.isDebugBuild (not yet measured)
- [x] Edit-mode geometry tests (not yet executed)
- [x] Play-mode tests for idempotent picking, stable round reset and interrupted animation (not yet executed)
- [x] Apple-skin texture, trunk occlusion and shadow draw-call reductions
- [x] Stem-to-twig attachment for every fruit (Unity test authored; not yet executed)
- [x] Explicit touch-position UI hit testing
- [x] Local Windows Unity test runner / optional APK script (not yet executed)
- [x] Static block-delimiter check across 15 C# scripts (passed; not a compiler run)
- [x] Prevent non-finite apple vertices at the top pole; regression test authored (not executed)

- [x] Source changes for Unity 6000.6.5f1 warnings CS1998, UAC0009 and CS0618 (awaiting user Editor verification)

## NOT VERIFIED OR COMPLETE
- [ ] Warning-free Unity 6000.6.5f1 compilation, scene validation and EditMode/PlayMode test run
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
