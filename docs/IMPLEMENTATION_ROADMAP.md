# Fruit Tree — implementation roadmap

## Goal and sequencing

Move from an already working 30-apple Unity prototype to a multi-species calming collection game without breaking orbit controls, pick animations, Pause, sound or the backend-free IAP boundary.

### Milestone A — data and persistence foundations

- [x] Ten species catalog and centralized visual profiles implemented in source (ScriptableObject editing remains optional later).
- [x] Implement pure TreeRoundState and OrchardProgressionService with exactly-once full-clear logic (Unity tests authored, not yet run).
- [x] Write save schema v1 and local persistence with temp + backup recovery (source committed, not verified on Android).
- [x] Authored EditMode tests: new save, 119/120 lock, 120/120 unlock, duplicate pick, replay and save in-memory roundtrip (not yet executed in Unity).
- [ ] Upgrade path from legacy 30-apple prototype without inventing historic progress.

**Acceptance:** unit tests **must pass in Unity** and existing Unity game must keep running. Source and tests are committed; Unity run is still outstanding.

### Milestone B — ten-tree source milestone

- [x] Give each fruit a stable identity, tracked in the new progression domain (code committed).
- [x] Wire OrchardGameController to the old camera, HUD and fruit-picking scripts (Unity test pending).
- [x] All ten planned fruit-count targets encoded in Unity source; counts and saved-ID compatibility covered by tests.
- [x] Build Tree Collection and completion-next UI; only unlocked next tree selectable (pending actual Unity verification).
- [x] Implement saved clear state and replay/rebloom action (code committed).
- [ ] Test pick-complete-restart without bypassing levels; QA final reachable fruit.

**Acceptance:** ten unique trees with 100%-clear sequential unlocking and infinite voluntary replay. No regression in Pause, audio or touch controls.

### Milestone C — serenity and visual distinction

- [ ] Finish premium procedural foliage, bark and fruit materials. First apple mesh/skin proportions and ground comfort pass committed (Unity visual review outstanding).
- [ ] Each species must have distinguishable branches, leaves, fruit shape and coloring.
- [ ] Natural atmospheric audio and subtle varied harvesting SFX; independent mute/level.
- [x] Source: subtle actual-full-clear UI fade, no noisy confetti/pressure. Rebloom animation still pending.
- [x] Source: minimal Zen Mode, persistent soft audio and reduced wind motion settings; acceptance test pending.
- [x] Source: gentle voluntary last-ten-fruit camera guide (Unity/on-device QA still pending).
- [ ] Mobile 9:16 and 9:20 visual QA screenshots.

**Acceptance:** the first 3 trees are recognizable as different species even when fruit has been fully picked. No forced timers, streaks, ads or energy.

### Milestone D — species content and scaling

- [x] Source: all ten release-candidate species and visual profiles are present.
- [ ] Visually review species uniqueness, balance, 100% fruit reachability and performance on Android.
- [ ] On-device profile 100–300 fruit, optimize mesh/material instancing and collider selection.
- [ ] Save migrations for slot versions and new content.
- [ ] KZ/RU text keys and accessible UI sizes; help not locked behind ads.
- [ ] Automated round-trip save and stress test suites.

### Milestone E — release

- [ ] Android portrait build and real-device stability (FPS, heat, startup, battery).
- [ ] Google Play Console purchase testing; no ad SDK enabled until Families compliance.
- [ ] Privacy, data-safety, signing, age rating and Play Store assets.
- [ ] Functional end-to-end tests on 3 device classes.

## Out of scope

No backend, chat, social feed, competitive leaderboards, farm economy, timers, daily login obligations, paid tree unlocks, account requirement, or interstitial ad during harvesting.

## Current status

The older one-tree prototype still exists. This roadmap is a forward plan, **not** a completed-feature checklist. Mark a checkbox only after source is merged and Unity tests validate it.
