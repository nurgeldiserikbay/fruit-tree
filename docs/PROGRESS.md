# Fruit Tree development state

## Agreed scope
- Android only, portrait.
- One fruit tree in the center; no orchard walking.
- Full 3D: 360-degree orbit around tree, zoom into branches.
- Tap to collect red apples. Relaxing and atmospheric, no timer.
- Semi-realistic appearance, low UI density, no network dependence.

## Committed code
- [x] Unity project and Android configuration
- [x] Deterministic batched tree generator (wood, leaf canopy, mesh apples)
- [x] Bark texture generator, asymmetric trunk / twigs, leaf-tip wind shader
- [x] Eliminate duplicate reverse leaf faces; material-controlled front/back culling
- [x] Touch orbit, pinch zoom, two-finger focus-height pan, aspect-ratio-based automatic framing
- [x] Apple interaction, animation, counter, allocation-free round reset (meshes and materials retained)
- [x] Simple procedural sound, sound toggle
- [x] Auto-generated scene with meadow / sky and safe area
- [x] Android build menu command
- [x] Edit-mode geometry tests (not yet executed)
- [x] Play-mode tests for idempotent picking, stable round reset and interrupted animation (not yet executed)
- [x] Apple-skin texture, trunk occlusion and shadow draw-call reductions
- [x] Stem-to-twig attachment for every fruit (Unity test authored; not yet executed)
- [x] Explicit touch-position UI hit testing
- [x] Local Windows Unity test runner / optional APK script (not yet executed)
- [x] Static block-delimiter check across 15 C# scripts (passed; not a compiler run)
- [x] Prevent non-finite apple vertices at the top pole; regression test authored (not executed)

## NOT VERIFIED OR COMPLETE
- [ ] Unity Editor compile and scene-opening acceptance test
- [ ] Real Android device build and play test
- [ ] Real recorded nature audio and tuned sounds
- [ ] Review real-time tree shape, leaf shading, apple appearance and fruit placement using Unity screenshots; improve after review
- [ ] Frame-time, battery, heating profiling on low-end phones
- [ ] Android signing, Google Play store release assets, permissions and age-rating review

No APK is included. Do not describe the game as production-ready without Unity builds and hardware testing.

## Testing handoff
See [SETUP.md](SETUP.md). The user's intended first manual check is near the end of development, but testing is indispensable before release.
