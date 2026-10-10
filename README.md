# Fruit Tree 🌳

A relaxing, Android-first 3D fruit-picking game: one semi-realistic tree, freely orbit around it, zoom into the branches, tap ripe apples, listen to nature. No timers, accounts, or complex UI.

**Current status:** Unity 6 C# prototype in development. Not yet built or tested with the Unity Editor or on a phone. See [setup and limitations](docs/SETUP.md).

## Implemented in code
- Seeded procedural tree geometry with tapering branches and batched leaves.
- Rounded apple meshes, stem geometry, harvest animation.
- 360-degree touch orbit and pinch zoom, mouse support for development.
- Safe-area HUD, restart, fruit progress.
- Simple synthesized ambience and harvest feedback, sound toggle.
- Unity Editor scene generator and Android portrait config.

## Start
Install Unity 6 6000.0.32f1, open this directory in Unity Hub, let it compile and generate the first scene, then press Play. [Full instructions](docs/SETUP.md).

## Next priorities
Unity compilation and real-device verification, better bark and foliage art, performance profiling and real licensed environmental recordings. Generated audio currently serves only as a functional placeholder.
