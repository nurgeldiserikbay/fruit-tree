# Fruit Tree - Unity 6 and Android

## First open
1. Install Unity Hub and **Unity 6 6000.6.5f1** with Android Build Support (SDK, NDK, OpenJDK).
2. Clone `https://github.com/nurgeldiserikbay/fruit-tree.git`.
3. In Unity Hub choose Add project from disk and select the root directory.
4. Wait for package import and C# script compilation. The first import may take several minutes. If the Console shows errors, copy the first red error plus the full stack trace before retrying. On first import the editor script automatically creates `Assets/Scenes/Main.unity`.
5. If the scene was not generated: choose **Fruit Tree > Generate Playable Scene**. Next choose **Fruit Tree > Validate Project** to check saved scene references, then press Play.
6. Mouse: drag to orbit, wheel to zoom, click an apple to pick. Android: drag, pinch to zoom, tap.
7. To create an Android APK: File > Build Profiles > Android > Switch Platform, then **Fruit Tree > Build Android APK**. The APK is written to `Builds/FruitTree.apk` in the repository root. Build And Run requires device debugging enabled.
8. To install the APK on device: `adb install -r FruitTree.apk` (or use Build And Run).

## Project constraints
- One tree only, with 30 apples (serialized count can be changed in inspector).
- Gameplay HUD: count and pause. In pause menu: resume, restart, sound on/off. Android Back button toggles pause.
- One batched mesh for leaves and another for branches. Unique pickable apple objects sharing one mesh.
- Procedural grassy ground and gradient sky. Most apples placed toward the crown exterior.
- Two-finger vertical drag pans inspection focus toward upper/lower branches. In Editor, Shift+right-drag pans focus.
- No timer, account, backend, park exploration or external assets. A disabled-by-default backend-free Remove Ads IAP integration is present; no ads SDK is installed.
- Portrait orientation, device safe-area-aware HUD, offline operation.
- Generated ambient audio is a technical placeholder and will need replacement with a licensed natural recording.
- Generated visuals are procedural prototypes. They may need substantial art iteration to match promotional reference quality.
- The Unity editor creates the first scene automatically on first import; source files and stable metadata are committed, but the generated scene is not versioned yet.
- Initial Unity scene is created by the editor and is not tracked until saved from the editor.

## Unity 6000.6.5f1 migration note
- The project now targets the already-installed 6000.6.5f1 editor. Source code and package references were authored before the upgrade, so compilation/package compatibility has **not yet been confirmed**.
- If Unity Hub asks to open a project built with an older version, proceed with the upgrade. Commit any Unity-generated settings changes only after the Editor opens cleanly.
- Do not downgrade the Editor to 6000.0.32f1, which Unity Hub marked with a security warning.
- In Console, use **Clear**, then capture the first red error and subsequent details if compilation fails. Do not repeatedly regenerate the scene while red compile errors remain.

## IMPORTANT
Unity Editor, Android devices and Unity's own C# assemblies are not available to ChatGPT in this session. The project code was authored and reviewed but **has not been compiled in Unity, played on a phone, or verified as release-ready**. Test the prototype in Unity before publishing.

## Platform checks
- First launch does the tree appear with green leaf crown and red pickable apples?
- Can you orbit through 360 degrees and zoom in/out without clipping?
- Does tapping apples increment the counter once each?
- Do Restart and SOUND work without crashes?
- Do the HUD and labels avoid camera notches on popular Android sizes?
- Is frame time stable at 30 FPS on low-end Android and 60 FPS on mid-range Android?
- Is the package identifier correct for your publisher before release?

## Android performance diagnostics (later QA)
- Use a **Development Build** so the `DevelopmentPerformanceMonitor` logs five-second FPS and frame-time samples without adding UI.
- With Android debugging enabled, use `adb logcat -s Unity:D Unity:W` or Unity's Android Logcat window to inspect `[FruitTree] fps=...` messages.
- Compare Android portrait displays with different aspect ratios and RAM/GPU levels; check visual clipping and touch correctness as well as FPS.
- Record heat, battery and stability manually for a minimum 10-minute session; the simple FPS logger is not a GPU profiler.
- The component's sampling code is excluded from release builds with Unity conditional compilation.

## Fruit visibility audit
- Run **Fruit Tree > Audit Fruit Reachability** (or **Fruit Tree > Validate Project**). It creates a temporary procedural tree far from the gameplay area and samples line-of-sight raycasts from 48 orbit angles and four heights.
- The audit warns about apples that could not be selected from any sampled angle. Such warnings require visual review and repositioning of the affected fruit before release.
- This is a *sampling diagnostic*, not a guarantee that every fruit is easy to spot, nor a substitute for an actual play session.

## Monetization
Backend-free remove-ads entitlement logic is implemented, but remains untested and the purchase control is disabled until a compliant ad provider is connected. See [MONETIZATION.md](MONETIZATION.md).

## Refreshing the visual scene after the first Unity screenshot
1. Save your work and stop Play mode. Pull the latest `main` branch (Git) and wait for Unity to recompile.
2. From the Unity menu, select **Fruit Tree > Refresh Visuals (Back Up Scene)**. This saves modified open scenes (with confirmation), stores a timestamped backup under `Builds/SceneBackups/`, then recreates `Assets/Scenes/Main.unity` with the updated rounded UI. It **replaces** the existing generated scene, so do not use it if you have custom scene edits you want to retain without reviewing the backup.
3. Open the regenerated `Assets/Scenes/Main.unity`, press Play, set the **Game** window's aspect to **9:16 portrait** (or a phone preset) rather than Free Aspect landscape, then send a new screenshot.
4. The updated tree/ground geometry is generated at runtime. The menu refresh is chiefly required to replace serialized legacy UI rectangles.

## Main menu and Google Play privacy test (new)

1. Stop Play, `git pull origin main`, wait until Unity finishes compiling.
2. Select **Fruit Tree → Refresh Visuals (Back Up Scene)**. This is required once because the existing `Main.unity` has serialized old UI elements. Review scene backup in `Builds/SceneBackups`.
3. In the Game tab, select **9:16 Portrait** and press Play. The first screen should be **FRUIT TREE**, not the apple counter.
4. Check **CONTINUE** (resumes the saved tree), **MY TREES** (opens collection), **SETTINGS** (Sound + Privacy Policy), **HOW TO PLAY**, and Pause → **MAIN MENU**. Android Back should close the topmost panel before pausing the game.
5. Settings → Privacy Policy opens an in-app privacy summary. The online button stays disabled **until** the owner publishes the real policy and updates `FruitTreePrivacyConfig.PublishedPolicyUrl`. Do not enter fake Docs links.
6. After contact and data-safety review, use the steps in [PRIVACY_POLICY_GOOGLE_DOCS.md](PRIVACY_POLICY_GOOGLE_DOCS.md). Run **Fruit Tree → Validate Project** and all EditMode/PlayMode tests.

The screen code is committed but was **not yet executed in Unity or on an Android phone** at authoring time.

## Final-game acceptance (deferred until the user wants to test the complete game)

After all graphics/content work and Play Store prerequisites are implemented:
- Open the refreshed Unity scene and verify the home scene shows an actual tree behind the menu.
- Harvest down to 10 fruits: **FIND FRUIT** appears; it rotates the camera towards a remaining fruit without picking it, and disappears when none remain.
- From Pause choose **ZEN MODE**: all standard HUD elements disappear and **EXIT ZEN** remains accessible; normal fruit collection still works and continues saving.
- From Settings toggle **AUDIO: SOFT/NORMAL**, **SOUND: ON/OFF**, **MOTION: GENTLE/MINIMAL**; close and reopen the app, confirm persisted preferences.
- Tree Collection → NEXT/PREVIOUS now closes Pause as the new tree loads, and previously picked fruit stays saved.
- Check Android Back navigation, Unity EditMode/PlayMode tests, performance on phones, IAP and published privacy policy before release.

This is a verification plan. The newest features have **not** been compiled or run in Unity in the authoring environment.

## Four environment themes
After a scene refresh, Settings → **SCENERY** cycles automatic / Meadow / Park / City Park / Mountains. No scenery has physics colliders. Detailed implementation and future test checklist: [ENVIRONMENT_SYSTEM.md](ENVIRONMENT_SYSTEM.md). The new scenery code has not been tested in Unity or on Android yet.
