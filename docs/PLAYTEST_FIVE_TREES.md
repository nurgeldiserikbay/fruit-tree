# Five-tree source milestone — final Unity acceptance plan

**Not yet executed in Unity 6000.6.5f1 or on Android.** User prefers to perform final verification after full development, so this is a future QA contract, not a request to test now.

## Locked progression catalog

| ID | Species | Fruits | Initially unlocked |
|---|---|---:|---|
| tree_001 | Apple | 120 | Yes |
| tree_002 | Pear | 140 | No |
| tree_003 | Cherry | 220 | No |
| tree_004 | Peach | 130 | No |
| tree_005 | Orange | 160 | No |

Unlock rule: collect **all** fruit IDs from the immediately preceding tree once. The unlock persists after rebloom and restart. No timer, ads, purchase or currency gate.

The first three species have the same IDs, counts and generation seeds as before; adding new rows must never wipe or backfill existing picked IDs.

## Deferred acceptance steps

1. Pull `main`; stop Play. Back up local work and run **Fruit Tree → Refresh Visuals (Back Up Scene)**. Confirm no Unity Console errors.
2. Launch at 9:16 and 9:20 aspect ratios. Check home, help, settings, privacy summary, sound, scenic themes, paused collection and Zen Mode.
3. In a **fresh test save** verify `0/120` Apple, `0/140` Pear, `0/220` Cherry, `0/130` Peach and `0/160` Orange in their correct order.
4. At N−1 fruit on **every species**, Next must stay locked. Only N/N can unlock the next. Fruit-pick clicks must not double-count.
5. Check all fruit are physically tappable under full 360-degree orbit/zoom; last-ten-fruit help never auto-picks.
6. Inspect Peach seam and soft blush, Orange round form and fine pores, different foliage/bark/crown. Compare model clarity at phone size.
7. Clear and rebloom each tree. The stored cleared history and subsequent unlocks remain; interrupted blossom animations don't re-enable invalid colliders.
8. Restart Editor/app after partially picking Peach or Orange; the exact fruit IDs and active tree return. Load a prior **three-tree** JSON file; new trees stay locked until their predecessors clear.
9. Run the entire EditMode and PlayMode suites, then test 5 trees on low- and mid-range Android hardware; record FPS, allocation spikes, heat and memory.
10. Confirm signed AAB, API target requirements, original 512×512 icon, 1024×500 feature graphic, authentic screenshots, consent/SDK audit, Play billing, localized Store copy and final public Privacy Policy before Play submission.

**Current release readiness:** not ready for Google Play publication. Five-tree gameplay exists only as unverified Unity source at this point.
