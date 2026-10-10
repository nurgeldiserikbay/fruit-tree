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
- [x] Bark texture generator and slight leaf wind shader
- [x] Touch orbit and pinch zoom
- [x] Apple interaction, animation, counter, restart
- [x] Simple procedural sound, sound toggle
- [x] Auto-generated scene and safe area
- [x] Android build menu command
- [x] Edit-mode geometry tests (not yet executed)

## NOT VERIFIED OR COMPLETE
- [ ] Unity Editor compile and scene-opening acceptance test
- [ ] Real Android device build and play test
- [ ] Real recorded nature audio and tuned sounds
- [ ] Detailed bark/branch art, foliage look and fruit placement critique in running game
- [ ] Frame-time, battery, heating profiling on low-end phones
- [ ] Android signing, Google Play store release assets, permissions and age-rating review

No APK is included. Do not describe the game as production-ready without Unity builds and hardware testing.

## Testing handoff
See [SETUP.md](SETUP.md). The user's intended first manual check is near the end of development, but testing is indispensable before release.
