# Fruit Tree — lightweight living environments

Status: **code integrated into the generated Unity scene, not yet compiled or visually approved in Unity 6000.6.5f1**.

## Eight backgrounds implemented in source

| Theme | Visual elements | Automatic level example |
|---|---|---|
| Meadow | Soft yellow/pink wildflower patches, distant treeline and clear sky | Apple tree |
| Park | Walking loop, subtle benches, blossoming plants and distant trees | Pear tree |
| City Park | Walking loop, low-poly faraway urban buildings and a green park buffer | Manually selectable |
| Mountains | Layered mountains, pale peaks, fir silhouettes and cool atmosphere | Cherry tree |
| Japanese Garden | Pond, footbridge, stone lanterns and soft blossom silhouettes | Orange tree |
| Chinese Garden | Pavilion silhouettes, moon gate and garden path | Apricot tree |
| Lakeside | Distant shore, gentle water strip and reeds | Plum tree |
| Evening Garden | Warm lanterns, dusky horizon and garden paths | Lemon tree |

The backgrounds stay **outside a radius of 5.1 world units** from the central tree; they deliberately have **no physics colliders**. A player can rotate around the tree 360 degrees without meeting an artificial wall or blocking a picking raycast.

## User controls

Home → Settings → **SCENERY: AUTO**. Each press cycles **AUTO → MEADOW → PARK → CITY PARK → MOUNTAINS → JAPANESE GARDEN → CHINESE GARDEN → LAKESIDE → EVENING GARDEN → AUTO**.

Automatic mode selects the scene based on the current tree index, while explicit selections persist in `PlayerPrefs` (`FruitTree.EnvironmentTheme`) and apply across all unlocked trees. All scenes are available from the start, do not require paying or watching ads, and do not alter 100%-harvest requirements.

Themes update three things:
- Ground/grass material tints (the same pre-generated ground geometry, no reload).
- Sky shader palette, sun tint and light intensity, fog color and range.
- A small single renderer containing merged, statically colored decor meshes, created **only on the first visit to a theme**, then cached for quick repeated switching.

## Performance and art rules

- **Exactly one scenery renderer** for theme decor, plus the existing terrain and grass renderers. Individual flowers, buildings, benches, and background trees are *not* GameObjects.
- No scenery colliders, Rigidbody, particle emitters, Update hooks, separate cameras or large textures.
- Up to **eight compact cached environment meshes** exist after visiting all themes, while **only one scenery renderer** is active. The meshes are released on scene destruction; repeat switching allocates no new meshes.
- Source unit tests require each theme decor to remain under **6,000 vertices** and **2,800 triangles** and its vertices to stay outside central 5.1-unit harvesting zone. These budgets are intentionally conservative and test-only until profile data exists.
- Distant decor and sky use simple shader/material colors and fog; mobile can keep soft shadows focused on the main tree.
- Trees and fruit are still a bigger cost than these decorations; 120–220 fruit performance must be measured on devices. A good decoration budget does **not** prove the game runs at 30/60 FPS.

## UX and future environments

Environment is decoration and relaxation, not an explorable open world. The harvest tree remains the focus. Future themes may include an autumn garden, rainy garden and spring blossoms, but each must respect the same budgets and user control.

An eventual polish pass should improve stylized landscape models (mountain silhouettes, park architecture, leaf clusters, realistic proportions), subtle audio atmospheres and light transitions based on actual Unity screenshots. Initial meshes are lightweight prototypes, **not** final production art.

## Acceptance when end-of-development testing begins

1. Pull `main`; stop Play and run **Fruit Tree → Refresh Visuals (Back Up Scene)** once to regenerate the serialized Settings UI and attach the environment.
2. Press Play. Initial Apple environment = Meadow. Open Settings and cycle all nine modes, including AUTO.
3. Confirm neither park furniture nor mountain/city decor blocks taps, orbit, zoom or 100% harvest.
4. Unlock Pear (Park) and Cherry (Mountains) and return to previously unlocked trees. Persisted manual selection must override automatic selections.
5. Run Unity Test Runner → EditMode → `EnvironmentThemeTests`.
6. Profile on low- and mid-range Android phones, including repeated theme changes, RAM, frame time, battery/temperature.

## Low-cost atmosphere polish (2026-10-12, code only)

- A continuous ring of softly colored distant ridges is added behind Meadow/Park/City Park and as two mountain layers. This adds horizon depth from a freely rotating 360-degree camera without any terrain streaming or extra scene cameras.
- Park has six tiny static lantern silhouettes along the distant path. City Park buildings now include a few flat, subtle daytime window panels. The mountain skyline has both near and far ridgelines.
- All new triangles are still part of **the same one static scenery renderer per active theme**, no physics objects, no additional materials or per-frame procedural geometry.
- A generated, quiet 11,025 Hz mono ambient loop is created **lazily** for each of the four themes. Meadow has more bird chirps; Park features soft wind and a few birds; City Park adds barely audible distant hum; Mountains emphasize slow wind.
- Switching environments crossfades between two built-in AudioSources over 1.5 seconds. Once visited, a theme's 8-second clip is cached and reused. The sound preference and quiet mode continue to apply; licensed user-supplied ambient recordings override generated placeholders and are played unchanged.
- The new PlayMode source test verifies scenery mesh reuse, four-theme audio caching and rapid switching. **It has not yet been run with the Unity Test Runner**.

### Outstanding art/audio work

The new objects and sound are **temporary procedural art**, not fully professional scenery models or real-world recordings. Check sound comfort and frequency on speakers/headphones, silhouette quality in portrait, Unity compilation, rendering correctness and real Android CPU/GPU/RAM/heat before release.

## Device-dependent Android graphics budget (source added 2026-10-12)

`MobilePerformanceBudget` attaches to the same scene as the environment and runs once at startup on Android. Using `SystemInfo.systemMemorySize` and `SystemInfo.graphicsMemorySize`, it chooses:

| Estimated hardware tier | Target frame-rate cap | Shadow budget | MSAA |
|---|---:|---|---:|
| Low (RAM < 3 GB or video RAM < 512 MB) | 30 | Off | Off |
| Balanced (RAM < 6 GB or VRAM < 1.5 GB) | 60 | 15m hard shadows | 2x |
| High | 60 | 23m soft shadows | 2x |
| RAM/VRAM unknown | 60 | Balanced defaults | 2x |

The runtime logs which tier it selected. This changes **graphics quality only**, not the fruit counts, achievements, progression, privacy settings or offline saves. Frame-rate caps are not FPS measurements or guarantees. All thresholds are provisional, need actual GPU timing, thermals and battery measurements before release. Unity Editor remains on its existing settings.

## Soft landscape palette transition

The environment keeps four cached static scenery meshes, but the background **palette** now eases between two theme settings over **one unscaled second**. Sky horizon and zenith, terrain/grass tint, fog, ambient and direct sunlight are interpolated together. The transition continues if Settings is open while the game time scale is zero; rapid changes stop the previous blend and start from the currently displayed colors. This adds only a brief coroutine, not terrain or GameObject rebuilds.

Source tests cover easing limits and a deferred PlayMode scenario with `Time.timeScale = 0`. Real Unity Editor/device rendering is still unverified.


## Optional panoramic sky backgrounds (source support, Unity verification pending)

`EnvironmentController.panoramas` can hold one 2:1 equirectangular `Texture2D` per existing environment theme, in enum order (Meadow, Park, CityPark, Mountains, JapaneseGarden, ChineseGarden, Lakeside, EveningGarden). Import with horizontal wrap **Repeat** and vertical clamp as appropriate; use a mobile-appropriate compressed texture and profile memory before shipping. Assign textures to the component in the Unity inspector. Empty or invalid-aspect entries use the existing procedural gradient/haze sky (`_PanoramaBlend = 0`). These are distant backgrounds, not physically navigable scenery and do not affect picking colliders. Panorama artwork is **not yet included** in this repository. Test seam, zenith/nadir, biome switching, Android shader compilation and GPU memory before release.
