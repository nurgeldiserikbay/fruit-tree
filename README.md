# Fruit Tree 🌳

A relaxing, Android-first 3D fruit-picking game. **Agreed future loop:** fully harvest one tree (100%), permanently unlock the next fruit-tree species, and freely rebloom previously cleared trees. No timers, accounts, or complex UI. The generated Unity scene now supports an initial playable Apple (120) → Pear (140) → Cherry (220) progression with local saves, 100% unlock, and rebloom; this integration **still needs Unity acceptance testing**.

**Current status:** Unity 6 C# prototype in development. Not yet built or tested with the Unity Editor or on a phone. See [setup and limitations](docs/SETUP.md).

## Implemented in code
- Asymmetric procedural tree with a slightly curved trunk, tapered branching twigs and batched leaves.
- Rounded apple meshes with procedural skin texture and stems connected to twigs, harvest animation and wood occlusion.
- 360-degree touch orbit and pinch zoom, mouse support for development.
- Safe-area HUD, allocation-free fruit-round restart, fruit progress.
- Parent-area Remove Ads purchase/restore wiring with Unity IAP 5; purchase disabled until a compliant ad provider is connected.
- Simple synthesized ambience and harvest feedback, sound toggle (placeholder audio).
- Procedural meadow and soft gradient sky, custom rounded HUD, Unity Editor scene generator and Android portrait config.

## Start
Install Unity 6 6000.6.5f1, open this directory in Unity Hub, let it compile and generate the first scene, then press Play. [Full instructions](docs/SETUP.md).

## Game design and new progression architecture

- [Approved GDD (Қазақша)](docs/GAME_DESIGN.md)
- [Architecture and save model](docs/ARCHITECTURE.md)
- [Implementation milestones](docs/IMPLEMENTATION_ROADMAP.md)

The new multi-tree collection system is **planned, not yet playable**. Do not confuse the 30-apple prototype with the future 10-species release.

## Next priorities
Unity compilation and real-device verification, better bark and foliage art, performance profiling and real licensed environmental recordings. Generated audio currently serves only as a functional placeholder.

For later automated local verification, see [PowerShell verification script](tools/verify-unity.ps1). It runs EditMode/PlayMode tests and can optionally build an APK on a machine with Unity 6 and Android modules installed.

**Ads are not enabled:** this source includes a purchase entitlement pathway, not an advertising service. See [monetization policy and setup](docs/MONETIZATION.md).

To update an existing Unity scene and see the new tree collection: [Three-tree playtest instructions](docs/PLAYTEST_THREE_TREES.md). Stop Play and choose **Fruit Tree > Refresh Visuals (Back Up Scene)** after pulling main.
