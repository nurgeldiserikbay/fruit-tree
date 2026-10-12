# Fruit Tree — final ten-tree acceptance checklist

**Status 2026-10-12: SOURCE INTEGRATED, NOT UNITY-TESTED, NOT PLAY-STORE-READY.** The user elected to defer a full manual acceptance pass until the game is otherwise complete.

## Source-level catalog — stable order

| Level | Saved ID | Species | Fruits |
|---|---|---|---:|
| 1 | tree_001 | Apple | 120 |
| 2 | tree_002 | Pear | 140 |
| 3 | tree_003 | Cherry | 220 |
| 4 | tree_004 | Peach | 130 |
| 5 | tree_005 | Orange | 160 |
| 6 | tree_006 | Apricot | 180 |
| 7 | tree_007 | Plum | 160 |
| 8 | tree_008 | Lemon | 140 |
| 9 | tree_009 | Mandarin | 200 |
| 10 | tree_010 | Pomegranate | 110 |

All **fruit IDs** remain local `fruit_0000` through `fruit_(N−1)`. The saved JSON schema version remains 1. Earlier species retain their IDs, counts, and generation seed formula, so old progress should not be lost.

## Automated QA to run at final acceptance

- Unity 6000.6.5f1: open repository, let scripts compile and run EditMode and PlayMode test suites, including `TenSpeciesVisualTests`, `MultiTreeIntegrationTests` and `OrchardProgressionTests`. Run **Fruit Tree → Validate Project** to check the generated scene revision; use **Refresh Visuals (Back Up Scene)** if outdated.
- Render all ten procedural fruit meshes; inspect normals, texture seams, leaf coverage, position, stem attachment and whether the fruit is discernible at 9:16 and 9:20.
- New game: only tree_001 unlocked. At N−1 fruits, tree_(N+1) remains locked for each of the first nine species. Picking the final fruit opens *exactly* the next species.
- Run **Fruit Tree → Audit All Ten Tree Species**; it must find no unreachable fruit across ten source-level catalogs before the AAB builder can run. On each tree, verify no collision or decorative object makes fruit unpickable from full 360° orbit and zoom. The small screen-scaled tap margin must not select distant fruit. The free last-ten-fruit help now shows a short-lived, noninteractive gold marker and must not pick fruit automatically.
- Verify app pause, close, process restart, corrupted primary-save fallback, restored exact per-fruit picked set and rebloom of prior clears. Test invalid in-memory saves cannot overwrite valid data. Execute `OrchardSaveRecoveryTests` with all other EditMode tests. Test upgrading saved states from both three- and five-tree versions.
- Inspect merged blossom regrowth on all ten trees. No growth-stage fruit is pickable; switching levels cancels stale animation.
- Visit all four environment themes, including automatic per-level cycling and Settings manual override; verify the new one-second sun/sky/terrain/fog color fade completes even while paused. Measure frame time, RAM, battery and heat on Android devices. Confirm the Low/Balanced/High startup graphics budget selects the intended tier, and verify 30/60 FPS are caps rather than guaranteed actual performance.
- Confirm main-menu navigation, Zen Mode, reduced-motion and audio settings, Android Back and accessibility before publishing. **English is the only supported game language.** Check no KZ/RU selector or labels appear. Verify rounded home/overlay panels, saved-tree home summary, event-driven progress strip and pooled visual tap glints remain readable and touch-through at 9:16 and 9:20.
- Use the guarded **Build Signed Android AAB** command and the publisher's private keystore; confirm ARM64, IL2CPP, target SDK and actual Play Console disclosures. Verify that the ad-free release has no accessible purchases or ads. Add the final **public Privacy Policy URL** and audited Data safety details at the final release gate.

## Performance boundaries

- Ten species does **not** mean ten active trees at once. The game keeps one main tree scene with a single wood mesh, leaf mesh and 110–220 individually clickable fruit objects.
- Background decorations stay combined into one small static mesh per active scenery theme.
- Fruit textures are 128×128 for apple and 64×64 for the other nine species, with a limited number of shared material variants.
- **No unmeasured claim of 30 or 60 FPS is made.** Test at least one low-end and one mid-range Android phone.

## Remaining work

Final art direction/polish, production-quality sound, UI localization, real fruit reachability, Unity build/test runner and signed Google Play release are not complete. Do not describe the project as released or fully production-ready.

## Accessibility acceptance

Verify the **Reduced Motion** toggle while a tree is actively reblooming: the animation must stop, every fruit become full size and selectable, and the saved round remains correct. New rebloom actions must have no blossom/growth animation. A reduced-motion tap must not show moving glints, the last-fruit marker must not pulse, and changing scenery in Settings must update colors without a slow fade. All remain English-only.
