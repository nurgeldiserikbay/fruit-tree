# Fruit Tree development state

## Main-branch scene and music release safeguards (2026-10-13)

- [x] PRs #1–#3 were merged into `main` before this pass. All follow-up changes for this pass were committed directly to `main`.
- [x] Bumped generated-scene layout stamp to revision **6** so old scenes cannot silently omit the new Music controls. Editor validation now verifies the Music button Text reference.
- [x] Fixed optional music playback after SOUND OFF → scenery change → SOUND ON; the current licensed track resumes instead of remaining stopped.
- [x] Fixed the initial theme used for music when a global licensed ambience override is configured.
- [x] Settings shows **MUSIC: NOT AVAILABLE** with a disabled action when the current biome has no licensed track. Nothing is advertised as playable if no clip is supplied.
- [x] Synthetic audio lifecycle handles EditMode cleanup without calling play-mode-only object destruction.
- [x] Wrote PlayMode regression for muted scenery changes and EditMode test for missing/available music. Expanded the PlayMode scenery/audio cache check to all eight landscapes and reset test-specific PlayerPrefs.
- [x] Updated README, Setup, eight-theme acceptance and environment documentation.
- [ ] Unity 6000.6.5f1 compilation and test suites still need to be executed on an installed Unity editor. Audio clips, real Android thermal/FPS tests, public Privacy Policy and signed AAB remain outstanding.

## Portrait-reachability audit fidelity (2026-10-13, source only)

- [x] The editor's all-species raycast diagnostic now builds viewpoints from the same `OrbitPickingCamera.OrbitPosition` function used by gameplay.
- [x] The audit samples legal pitch/focus/zoom combinations around 360 degrees and only accepts direct fruit-collider hits when the fruit center lies within a 9:20, 47-degree portrait viewport.
- [x] Failure labels now include stable `fruit_####` IDs for targeted placement fixes.
- [x] Five EditMode regression tests authored for yaw geometry, vertical pitch, screen-edge rejection, narrow vs. wide phones, and degenerate camera states.
- [ ] Unity 6000.6.5f1 Test Runner and **Fruit Tree > Audit All Ten Tree Species** execution are still required. A diagnostic pass cannot guarantee that all fruit are intuitive to find and tap on real devices.

## Complete interrupted-save recovery (source update, 2026-10-12)

- [x] On startup, recover a structurally valid, more recently written `.tmp` snapshot even if the previous primary is still valid; this avoids losing the latest successful temporary write after a shutdown between write and replace.
- [x] Keep a valid primary when the temporary copy is older, invalid, or has the same modification time (safe fallback on coarse-timestamp file systems).
- [x] Added three deterministic EditMode regression tests for newer-valid, older-valid and newer-invalid temporary snapshots.
- [ ] Execute Unity EditMode tests and Android force-close/restart scenarios. This source fix is not a verified device release.

## Additional safety and accessibility fixes (2026-10-12)

- [x] Offline save repository now uses the same structural validation for both loading and writing. Malformed snapshots are rejected **before** touching a previous valid on-disk save.
- [x] `OrchardSaveRecoveryTests` expanded with a case verifying an invalid in-memory snapshot cannot overwrite a correct one (test authored, not executed).
- [x] `Reduced Motion` now applies consistently to fruit rebloom, the fruit-pick pulse, the optional hint marker, and sky/fog/landscape color transitions. Turning it on during active rebloom finishes that animation safely and makes fruit tappable immediately.
- [x] PlayMode tests authored for instant reduced-motion rebloom and toggling during a blossom animation; not yet run.
- [ ] Final Unity 6000.6.5f1 compilation, automated EditMode/PlayMode suite, ten-tree reachability and real Android device checks remain outstanding.


## Offline save-integrity recovery (2026-10-12)

- [x] Source: reject syntactically valid but incomplete local save files (empty tree list, null picked-ID collection, negative counters, duplicate saved tree IDs, or missing active-tree record).
- [x] Source: preserve existing `.bak` / `.tmp` recovery sequence; do not reset stored fruit IDs or the ten-tree progression rules.
- [x] Authored `OrchardSaveRecoveryTests` for backup recovery, valid interrupted writes and duplicate tree entry rejection.
- [ ] Execute Unity EditMode suite, corrupted-save Android tests, full fruit reachability audit and visual QA before publication.


## English-only and release-engineering hardening (2026-10-12)

- [x] First release explicitly **English-only** (`FruitTreeReleaseConfig.LanguageCode = "en"`), with scene validation rejecting Cyrillic UI labels.
- [x] First release explicitly **ad-free with no purchases**; `REMOVE ADS` panel entry hidden and Play Billing initialization disabled.
- [x] Editor-generated scene revision stamp added. Validation now explains how to refresh the outdated local scene with a backup, instead of pretending GitHub C# commits update the local scene automatically.
- [x] Guarded **Build Signed Android AAB** Unity editor command added (policy, ARM64, IL2CPP, API, up-to-date scene, all-ten-tree reachability audit, local keystore checks). **No AAB has been built.**
- [x] English-only and ad-free launch tests authored (not executed in Unity).
- [x] Full ten-species reachability audit added as a separate Editor menu command and mandatory signed-AAB gate; source-only, not yet run.
- [ ] User's real public Privacy Policy URL and release signing credentials; Unity compile, PlayMode/EditMode testing, authentic screenshots, real-device profiling and signed internal-test AAB remain pending.


## Tree silhouettes, touch accessibility and gentle scenery fades (2026-10-12)

- [x] Source: the ten existing `FruitSpeciesProfile` entries now define trunk height, upward branch lift, twig angle and crown-filler extent; pear is more upright and cherry/peach are more spreading.
- [x] Tree generator uses those parameters for its existing combined wood/leaf meshes. **No new renderers, leaves, fruit meshes or save identifiers** are required.
- [x] Source: the camera performs a short-radius SphereCast **only if the precise tap ray misses**, helping small fruit on phone screens. The margin is calculated from screen height/camera FOV and capped at 0.12 world units.
- [x] Source: sky/ground/fog/ambient light and sun colors transition over one second when changing biomes; existing cached decor mesh and audio crossfades remain.
- [x] Source: a tap-through, four-and-a-half-second optional gold pointer follows a remaining fruit only on explicit FIND FRUIT request. Tree switching and rebloom immediately hide it.
- [x] Source: EditMode tests for finger-radius bounds, differing ten-species silhouettes, easing and passive guide fades; PlayMode test for scenery transition while Settings/game is paused. **Not yet run in Unity.**
- [ ] Final Unity/Android checks: readability of the pointer, portrait tapping precision, color fades from the paused Settings menu, visual silhouette uniqueness, 100% fruit reachability and FPS.


## Main-menu and harvest UI comfort pass (2026-10-12)

- [x] Source: the home screen keeps the tree visible behind a lighter veil, with a single rounded hero panel, warmer primary Continue button and quieter typography.
- [x] Source: the home screen displays the active tree number and picked/total fruit from the saved orchard progression, without modifying progress.
- [x] Source: a narrow, non-interactive harvest progress strip responds to `TreeGenerator.HarvestChanged` and initializes correctly from saved fruit count.
- [x] Source: three pooled, non-blocking tap glints fade away over 0.42 seconds after a genuine fruit pick. No new GameObjects or extra shaders are created per pick; updates run only while the effect is visible.
- [x] Source: rounded dialog surfaces now make Pause, Settings, Tree Collection, How To Play and Privacy screens stylistically consistent while showing some of the world behind them.
- [x] Authored EditMode tests for progress fractions, zero/half/full state and monotonic pulse fade; updated scene validator with required references and touch-through conditions.
- [ ] **Unverified in Unity Editor:** actual screens, font scaling, 9:16 and 9:20 layouts, overlays, device touch and FPS; scheduled for the final acceptance phase.


## Device graphics budget (2026-10-12)

- [x] Source: single-start Android device class for Low/Balanced/High graphics quality, frame-rate caps, shadow distance/type and MSAA.
- [x] Fallback to balanced tier on unknown device RAM/VRAM readings; no effect on game logic or saves.
- [x] Added edit-mode tier-decision tests and verified component wiring through scene validator (tests authored but not run).
- [ ] On-device real FPS, CPU/GPU timings, temperature, memory and image-quality acceptance remains mandatory.


## Ambient environment depth and cache (2026-10-12)

- [x] Source: layered distant hills/mountains, park lanterns and subtle city-window details, merged into the existing scenery renderer.
- [x] Source: lazy cache of up to four small biome meshes, reused on every later theme switch.
- [x] Source: four distinct low-volume synthesized atmosphere loops (wind/birds/city hum) with a gentle 1.5-second crossfade; all cached and working offline.
- [x] Source: themed audio wired into the generated scene, source-level PlayMode regression test for sound and mesh reuse, and scene-reference validator updated.
- [ ] Run Unity EditMode and PlayMode tests and assess real Android FPS, battery, skyline visuals and sound comfort.

See [ENVIRONMENT_SYSTEM.md](ENVIRONMENT_SYSTEM.md).


## Ten-tree content milestone — source code (2026-10-12)

- [x] Catalog contains **10** stable tree IDs and fruit counts: Apple 120, Pear 140, Cherry 220, Peach 130, Orange 160, Apricot 180, Plum 160, Lemon 140, Mandarin 200, Pomegranate 110.
- [x] Five new procedural low-poly fruit meshes, distinct leaf/crown/wood profiles and shared 64×64 RGB24 swatches.
- [x] Source refactor moves branch/foliage/fruit dimensions and shades into `FruitSpeciesProfile` so adding species does not require expanding long `TreeGenerator` condition chains.
- [x] Existing `tree_001`–`tree_005` IDs and fruit counts preserved. Strict full-clear progression and schema-v1 saved state continue to apply.
- [x] Source tests authored for ten stable per-fruit ID sets, all ten level unlock gates, five-tree-save expansion, geometry and texture budgets.
- [ ] Tests need executing with Unity Test Runner. All ten trees must pass 100%-reachability and visual review on real phones; source-level content is **not** Google Play release acceptance.

See [TEN_TREE_ACCEPTANCE.md](TEN_TREE_ACCEPTANCE.md).


## Five-tree collection expansion (2026-10-12)

- [x] Source: Peach 130 (`tree_004`) and Orange 160 (`tree_005`) following existing Apple 120, Pear 140, Cherry 220.
- [x] Source: independently shaped peach (subtle seam) and citrus (round slight ribbing) mesh, species-specific bark/canopy/leaf color and 64×64 skin swatches.
- [x] Existing IDs, counts, earlier clear history, local save schema v1 and strict 100%-before-next unlock preserved.
- [x] EditMode tests authored for all five stable fruit-ID sets, mesh budgets, sequential unlock, replay and older three-tree-save expansion.
- [ ] Unity compile, actual fruit visibility/100%-reachability, PlayMode checks and real Android performance remain unverified.

See [FIVE_TREE_RELEASE_NOTES.md](FIVE_TREE_RELEASE_NOTES.md).

## Gentle rebloom and distinct tree shape (2026-10-12)

- [x] Source: cleared trees can rebloom through a **single shared coroutine**, with pale flower petals and staggered growth of all existing fruit instances.
- [x] Source: each blossom group uses **one merged static 3D mesh**, not 120–220 particle systems, with mesh/material reused on subsequent rounds.
- [x] Source: growing fruits keep colliders disabled; direct Pick calls also reject them until fully grown.
- [x] Source: save before visual regrowth, keep stable fruit IDs, hide old blossoms/cancel animation during a tree change, and close the collection menu on rebloom.
- [x] Source: Orchard scene no longer makes a redundant 30-fruit default tree before loading the saved 120/140/220-fruit species.
- [x] Source: pear/apple/cherry have distinct leaf profiles, canopy placement and bark tones; smaller-fruit stems now correctly connect in world space.
- [x] PlayMode/EditMode tests authored for rebloom timing, interruption, collision gates, world-space stems and leaf mesh budgets. **Tests have not been executed in Unity.**
- [ ] Visually approve blossom and bark/foliage treatment and measure initial-scene/Android frame-time improvements.


## Fruit / landscape / completion polish (2026-10-12)

- [x] Source: natural apple shoulder/lobe silhouette, softer 128×128 apple-skin freckles, real round cherry geometry, shared 64×64 pear/cherry skins and three color variants per species.
- [x] Source: smaller Apple/Pear/Cherry fruit with closer twig attachment, preserved stable picked-fruit IDs.
- [x] Source: softer short curved grass and subtle terrain tinting within existing mesh budgets.
- [x] Source: gentle nonblocking complete-harvest fade only after last real pick; no restore celebration.
- [x] Source: optional instancing for shared Standard fruit/stem materials, guarded by device support.
- [x] Added regression tests for fruit mesh, grass size and completion fade (authored, not run in Unity).
- [ ] Final Unity screenshots, visually approved apple/pear/cherry meshes, performance profiling and on-device test.

Details: [VISUAL_POLISH.md](VISUAL_POLISH.md).


## Lightweight living environments (2026-10-12)

- [x] Source: Meadow, Park, City Park, Mountains 3D themed scenery in one merged decor mesh per environment.
- [x] Source: dynamic sky/sun/fog and ground tint presets for each environment; manual Settings cycling with persistent user preference and AUTO per-tree default.
- [x] EditMode tests for geometry budgets, center clearance and deterministic theme names/defaults (authored, NOT run in Unity).
- [ ] Unity compile, actual screenshots, scene switching, device frame-time profiling and art polish.
- [ ] Additional biomes (lake, autumn, evening) after the four launch themes are approved.

See [ENVIRONMENT_SYSTEM.md](ENVIRONMENT_SYSTEM.md).


## Front-end and Google Play (2026-10-12)

- [x] Source: main menu with Continue / My Trees / Settings / How to Play.
- [x] Quiet-audio, mute and reduced-leaf-motion preferences stored locally (source committed; Editor/device QA pending).
- [x] Optional HUD-free Zen Mode with persistent in-view Exit Zen control; no score or timers added (code committed, Unity acceptance pending).
- [x] Source: Pause → Main Menu and Settings, screen-aware Android Back behaviour.
- [x] Game camera framed immediately on home screen before player input is disabled; pending Unity visual QA.
- [x] Source: in-app privacy summary, safe published-Google-Docs URL configuration and disable-on-missing-link logic.
- [x] Draft Google Docs Privacy Policy, Play listing text and release-readiness checklist.
- [x] EditMode tests for navigation and Privacy URL validation (not yet executed).
- [ ] User Unity 6000.6.5f1 compilation and portrait visual tests of new screens, Zen Mode, hint, settings and menu switching — deferred to final acceptance as requested.
- [ ] Publish real privacy policy, replace contact placeholders, complete SDK data audit, add verified public URL.
- [ ] Actual signed Android release AAB, real device tests, store images and end-to-end ten-tree acceptance. Language is English only.


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
