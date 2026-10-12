# Fruit Tree — Visual Comfort Polish (2026-10-12)

**Status:** source committed to main; Unity 6000.6.5f1 Editor / Android screenshots and performance measurements remain outstanding.

## Fruit improvements

- **Apple silhouette:** keeps the existing low-budget 16×20 procedural topology but adds rounded shoulders, five subtle calyx lobes and a recessed stem dimple. Model is now less like a plain sphere.
- **Fruit size:** Apple random scale **0.54–0.64** (formerly 0.79–0.90); Pear 0.70–0.80 (formerly 0.88–1.0); Cherry 0.43–0.53 (formerly 0.48–0.61).
- **Placement:** fruit hangs a little closer to twigs, with less radial push out of the crown and smaller jitter; still individually tappable, with a slightly forgiving hit sphere.
- **Skin:** the existing 128×128 RGB24 apple texture gains soft blush/russet streaks; pear/cherry get **separate 64×64 RGB24 mipmapped swatches** shared across every instance. No per-fruit texture allocation.
- **Cherry mesh:** no longer a shrunken five-lobed apple. Uses its own round 12×16 latitude/longitude mesh (221 vertices vs the apple's 357), keeping consistent click/harvest rules.
- **Variation:** exactly three shared material tints per species instead of a separate material for every fruit. Materials use Standard shader GPU instancing when `SystemInfo.supportsInstancing` is available. On-device draw-call reduction has not been measured.
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

- `VisualPolishTests` checks apple dimensions, finite mesh coordinates, reduced cherry mesh, 64×64 pear/cherry skins, grass height/geometry budget and gentle completion fades.
- A separate numerical reproduction of the apple mesh formula measured 545/560 outward-facing relevant triangles (**97.3%**) vs the existing >95% test requirement. **This is not a Unity Test Runner result.**
- `ValidateProject` now checks the new completion UI references.
- Source and `.meta` file structure checked; **Unity, APK/AAB and phone performance tests are outstanding**.

## Important remaining visual work

- High-quality branching/trunk bark, varied leaf canopy shapes and believable fruit attachment need visual review.
- The themed meadows, parks, city skyline and mountain silhouettes are still low-poly procedural prototypes.
- Licensed nature recordings, transition ambience, seasonal palettes and actual Android frame-time measurements are not yet complete.
- Last-10-fruit reachability must be audited with the smaller fruits on real phone screens.

When the user starts the final acceptance phase, pull main and **Fruit Tree → Refresh Visuals (Back Up Scene)**. Then verify Apple/Pear/Cherry screenshots, 100% collection, Save/Load, Zen Mode, themed environments, test runner and device FPS.

## Tree growth and distinctive profiles (2026-10-12)

- **Rebloom:** a fully cleared tree is saved with an empty picked-fruit set before any animation starts. The old fruit objects are reset once and a **single coroutine** gradually scales them in over ~1.95 seconds.
- **Flower phase:** the first 0.88 seconds show a merged mesh of simple pastel petals near each fruit. Five low-poly petals per fruit share just one renderer. No per-petal GameObjects or particle emitters. The flower mesh is cached for later rounds.
- **Colliders:** fruit colliders remain off until growth finishes. A direct `Pick()` call also rejects growing/disabled fruit, preventing accidental double-counting.
- **Interruption:** switching species cancels the old growth coroutine, immediately hides blossom visuals and frees its generated mesh on regeneration.
- **Species forms:** pear leaves fuller and rounder, cherry leaves slimmer; trunks/limbs have different warm bark tints and species-dependent crown heights. All leaves still use eight vertices and six triangles each.
- **Twig attachment:** a stem attached to a scaled fruit now compensates for the fruit's scale in its cylinder dimensions, reaching both fruit top and twig anchor rather than visibly floating.
- **Launch performance:** `TreeGenerator` has a serialized `generateAutomatically` option; the scene builder disables a redundant default 30-fruit generation before `OrchardGameController` creates the correct saved species. Standalone trees keep their default auto-generation.

These features are committed to source and have **unexecuted Unity tests**. The flower timing, leaf/wood colors, screenshots and phone frame time still require actual Editor/device verification. A visible flower sequence is not a guarantee of final art quality.

## Peach and orange expansion (2026-10-12)

- New fruit profiles: Peach uses an apple-derived mesh reshaped with a shallow suture; Orange uses a compact cherry-derived round citrus body with subtle ribbing.
- Dedicated shared 64×64 RGB24 mipmapped peach/orange skin swatches with warm blush and fine citrus pores.
- Peach and Orange have different canopy spreads, major-branch counts, foliage proportions/colors and bark shades. Their meshes remain merged for mobile cost control.
- Peach: **130** fruits; Orange: **160**. Previous IDs, exact tree counts and JSON schema stay stable, so existing players' earlier progress should survive the catalog extension.
- Mesh, fruit-count, stable-ID, strict 100% unlock and save-extension tests have been authored. **No Unity Editor/Android run has taken place.**

## Ten distinct species profiles (2026-10-12)

- `FruitSpeciesProfile` defines all ten trees' major branch counts, crown width and height, bark/wood/leaf colors, leaf proportions, fruit scales and hit radii in one immutable lookup.
- The new fruit shapes include a small apricot, oval plum, tapered lemon, flattened mandarin and red pomegranate with five integrated crown sepals opposite its stem. They derive from the existing compact procedural meshes, without new individual mesh allocations per fruit.
- Each new species has a shared 64×64 RGB24 skin texture (golden blush, plum bloom, citrus pores or pomegranate blush) and three shared material tints per tree.
- Existing first-five IDs, fruit counts and level seed formula were not changed.
- Editor tests check model bounds and vertex budgets and progression strictness, but **the models have not been reviewed on mobile**. Final art may require further mesh and leaf work.
