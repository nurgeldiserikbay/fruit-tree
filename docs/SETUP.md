# Fruit Tree - Unity 6 and Android

## First open
1. Install Unity Hub and **Unity 6 6000.0.32f1** with Android Build Support (SDK, NDK, OpenJDK).
2. Clone `https://github.com/nurgeldiserikbay/fruit-tree.git`.
3. In Unity Hub choose Add project from disk and select the root directory.
4. Wait for package import and C# script compilation. On first import the editor script automatically creates `Assets/Scenes/Main.unity`.
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
- No timer, account, backend, ads, in-app purchases, park exploration or external assets.
- Portrait orientation, device safe-area-aware HUD, offline operation.
- Generated ambient audio is a technical placeholder and will need replacement with a licensed natural recording.
- Generated visuals are procedural prototypes. They may need substantial art iteration to match promotional reference quality.
- The Unity editor creates the first scene automatically on first import; source files and stable metadata are committed, but the generated scene is not versioned yet.
- Initial Unity scene is created by the editor and is not tracked until saved from the editor.

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
