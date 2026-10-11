# Fruit Tree — implementation roadmap

## Goal and sequencing

Move from an already working 30-apple Unity prototype to a multi-species calming collection game without breaking orbit controls, pick animations, Pause, sound or the backend-free IAP boundary.

### Milestone A — data and persistence foundations

- [ ] Create stable TreeDefinition / TreeCatalog with three initial species.
- [ ] Implement pure TreeRoundState and OrchardProgressionService with exactly-once full-clear logic.
- [ ] Write save schema v1 and robust local persistence (atomic temp / backup / restore).
- [ ] Tests: new save, 119/120 lock, 120/120 unlock, duplicate pick, round replay and app restart.
- [ ] Upgrade path from legacy 30-apple prototype without inventing historic progress.

**Acceptance:** unit tests pass and existing Unity game continues running, even if new catalog is not yet presented.

### Milestone B — playable three-tree vertical slice

- [ ] Give each fruit a stable identity, tracked in the new progression domain.
- [ ] Wire OrchardGameController to the old camera, HUD and fruit-picking scripts.
- [ ] Per-tree spawned fruit count: Apple ~120, Pear ~140, Cherry ~220 (balance targets).
- [ ] Build lightweight TreeSelection and Completion views; only unlocked tree is selectable.
- [ ] Implement saved clear state and replay/rebloom action.
- [ ] Test pick-complete-restart without bypassing levels; QA final reachable fruit.

**Acceptance:** 3 unique trees with 100%-clear sequential unlocking and infinite voluntary replay. No regression in Pause, audio or touch controls.

### Milestone C — serenity and visual distinction

- [ ] Replace or substantially upgrade current procedural foliage, bark and fruit materials.
- [ ] Each species must have distinguishable branches, leaves, fruit shape and coloring.
- [ ] Natural atmospheric audio and subtle varied harvesting SFX; independent mute/level.
- [ ] Subtle completion/rebloom transition, no noisy confetti/pressure.
- [ ] Gentle voluntary last-10-fruit discovery assist.
- [ ] Mobile 9:16 and 9:20 visual QA screenshots.

**Acceptance:** the first 3 trees are recognizable as different species even when fruit has been fully picked. No forced timers, streaks, ads or energy.

### Milestone D — species content and scaling

- [ ] Expand to first-release catalog of 10 trees, with authored data validation.
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
