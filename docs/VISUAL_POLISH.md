# Fruit Tree — Visual Comfort Polish (2026-10-12)

**Status:** source committed to main; Unity 6000.6.5f1 Editor / Android screenshots and performance measurements remain outstanding.

## Fruit improvements

- **Apple silhouette:** keeps the existing low-budget 16×20 procedural topology but adds rounded shoulders, five subtle calyx lobes and a recessed stem dimple. Model is now less like a plain sphere.
- **Fruit size:** Apple random scale **0.54–0.64** (formerly 0.79–0.90); Pear 0.70–0.80 (formerly 0.88–1.0); Cherry 0.43–0.53 (formerly 0.48–0.61).
- **Placement:** fruit hangs a little closer to twigs, with less radial push out of the crown and smaller jitter; still individually tappable, with a slightly forgiving hit sphere.
- **Skin:** same small 128×128 RGB24 mipmapped texture, with mild russet/gold streaks and sparse freckles; no larger texture requirement.
- **Variation:** exactly three shared material tints per species instead of a separate material for every fruit. Materials use Standard shader GPU instancing when supported.
- **Progress IDs:** `fruit_0000`... identifiers unchanged; existing local progress, tree-clearing, unlock and rebloom rules are untouched.

## Ground comfort

- Replaced tall sharp triangular grass cards with short, subtly bent **five-vertex blades** inside the existing single merged grass mesh.
- Gentler meadow palette, soft patchy color variation, a slightly lighter central grass clearing.
- Keeps the same 64×64 terrain tessellation, no extra foliage GameObjects or physics.

## Completing an entire tree

- The last **real player fruit pick** emits a dedicated `TreeCleared` gameplay event.
- A non-blocking UI label softly fades in and out over **4.5 seconds**.
- No confetti, aggressive sound, flash, forced next-level action or separate full-screen victory scene.
- Merely **restoring a completed tree from storage does not replay the celebration**.
- UI notice does not consume touches; the player can keep moving the camera or use the existing Next Tree button.

## Verification written but not executed

- `VisualPolishTests` checks apple dimensions, finite mesh coordinates, grass height/geometry budget and monotone gentle completion fades.
- A separate numerical reproduction of the apple mesh formula measured 545/560 outward-facing relevant triangles (**97.3%**) vs the existing >95% test requirement. **This is not a Unity Test Runner result.**
- `ValidateProject` now checks the new completion UI references.
- Source and `.meta` file structure checked; **Unity, APK/AAB and phone performance tests are outstanding**.

## Important remaining visual work

- High-quality branching/trunk bark, varied leaf canopy shapes and believable fruit attachment need visual review.
- The themed meadows, parks, city skyline and mountain silhouettes are still low-poly procedural prototypes.
- Licensed nature recordings, transition ambience, seasonal palettes and actual Android frame-time measurements are not yet complete.
- Last-10-fruit reachability must be audited with the smaller fruits on real phone screens.

When the user starts the final acceptance phase, pull main and **Fruit Tree → Refresh Visuals (Back Up Scene)**. Then verify Apple/Pear/Cherry screenshots, 100% collection, Save/Load, Zen Mode, themed environments, test runner and device FPS.
