# Fruit Tree 🌳

A relaxing, Android-first 3D fruit-picking game. **Agreed future loop:** fully harvest one tree (100%), permanently unlock the next fruit-tree species, and freely rebloom previously cleared trees. No timers, accounts, or complex UI. The generated Unity scene now supports an initial playable Apple (120) → Pear (140) → Cherry (220) progression with local saves, 100% unlock, and rebloom; this integration **still needs Unity acceptance testing**.

**Current status:** Unity 6 C# prototype in development. Unity Editor Game view has been tested by the user; latest home/settings/privacy code still requires compilation and playtesting. Android APK/AAB and phone testing remain outstanding. See [setup and limitations](docs/SETUP.md).

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

The first three-tree collection has been integrated into the playable Unity scene generator, but it and the newly added front-end screens still need end-to-end verification. The planned ten-species release is not complete.

## Next priorities
Unity compilation and real-device verification, better bark and foliage art, performance profiling and real licensed environmental recordings. The latest [visual comfort pass](docs/VISUAL_POLISH.md) adds more organic apple geometry/skin, distinct cheaper cherry geometry, 64×64 pear/cherry textures, smaller fruit, softer grass, instancing-ready materials and a quiet tree-clear fade. Generated audio currently serves only as a functional placeholder.

For later automated local verification, see [PowerShell verification script](tools/verify-unity.ps1). It runs EditMode/PlayMode tests and can optionally build an APK on a machine with Unity 6 and Android modules installed.

**Ads are not enabled:** this source includes a purchase entitlement pathway, not an advertising service. See [monetization policy and setup](docs/MONETIZATION.md).

To update an existing Unity scene and see the new tree collection: [Three-tree playtest instructions](docs/PLAYTEST_THREE_TREES.md). Stop Play and choose **Fruit Tree > Refresh Visuals (Back Up Scene)** after pulling main.

## Environment themes

- Four procedural, static, merged-mesh landscapes: Meadow, Park, City Park and Mountains, with themed sky, sun, fog and ground tint.
- Settings → SCENERY cycles themes and saves a manual choice; AUTO follows tree progression.
- Background geometry has no colliders and stays outside the central fruit-picking radius. [Details & tests](docs/ENVIRONMENT_SYSTEM.md).
- Unity visual/device performance verification is still outstanding.

## App front-end / Google Play preparation

The scene generator now creates **FRUIT TREE** home menu, **CONTINUE**, **MY TREES**, **SETTINGS**, **HOW TO PLAY**, **PRIVACY POLICY** and Pause → **MAIN MENU** navigation.

- [First-open and refresh instructions](docs/SETUP.md)
- [Google Docs-ready privacy-policy draft (requires real contact and published URL)](docs/PRIVACY_POLICY_GOOGLE_DOCS.md)
- [Google Play release checklist](docs/PLAY_STORE_RELEASE.md)
- [Store listing text draft](docs/STORE_LISTING_DRAFT.md)

**The Privacy link is intentionally unavailable online until a real published Google Docs URL is configured.** See `Assets/Scripts/FruitTreePrivacyConfig.cs`. Do not publish an AAB to Google Play without this and the other release prerequisites.

### Latest rebloom and tree-shape refinement

Completed trees now have a lightweight flower-to-fruit regrowth animation, cached in one merged blossom mesh and one coroutine. Apple, pear and cherry have more distinct foliage/crown forms, and the generated scene avoids wasteful double tree generation. See [visual polish notes](docs/VISUAL_POLISH.md). **Unity and Android playtesting remain pending.**
