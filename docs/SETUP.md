# Setup and current limitations

1. Install Unity 6 in Unity Hub with Android Build Support, SDK, NDK and OpenJDK.
2. Add this repository as a project. It uses the built-in pipeline for the first prototype (no custom rendering packages).
3. Open Unity. Use **Fruit Tree > Generate Playable Scene**.
4. Open `Assets/Scenes/Main.unity`, press Play.
5. Mouse drag / touch drag rotates the camera. Tap visible red apples to harvest. Pinch zooms; Editor mouse wheel also zooms.
6. For Android: File > Build Profiles > Android > Switch Platform, then Build And Run with USB debugging enabled.

## Status

This is an *unverified Unity prototype*, not a finished visual-quality 3D scene. It uses primitive procedural geometry; no imported art or licensed audio. The editor scene must be generated locally; it is not yet committed as a .unity scene. Final artwork, ambient sound, automated tests, touch QA, and device FPS profiling are next. Unity editor compatibility needs confirmation in a real Unity installation.

## Design

One tree in the middle of a simple clearing. No park navigation, timers, stores or monetization. Longer-term: hand-authored or Blender-generated tapered trunk/branches, leaf cards with shaders, textured fruit with stems, subtle wind and satisfying natural sounds.
