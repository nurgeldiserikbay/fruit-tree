# Fruit Tree — lightweight living environments

Status: **code integrated into the generated Unity scene, not yet compiled or visually approved in Unity 6000.6.5f1**.

## Four backgrounds shipped in source

| Theme | Visual elements | Automatic level example |
|---|---|---|
| Meadow | Soft yellow/pink wildflower patches, distant treeline and clear sky | Apple tree |
| Park | Walking loop, subtle benches, blossoming plants and distant trees | Pear tree |
| City Park | Walking loop, low-poly faraway urban buildings and a green park buffer | Manually selectable |
| Mountains | Layered mountains, pale peaks, fir silhouettes and cool atmosphere | Cherry tree |

The backgrounds stay **outside a radius of 5.1 world units** from the central tree; they deliberately have **no physics colliders**. A player can rotate around the tree 360 degrees without meeting an artificial wall or blocking a picking raycast.

## User controls

Home → Settings → **SCENERY: AUTO**. Each press cycles **AUTO → MEADOW → PARK → CITY PARK → MOUNTAINS → AUTO**.

Automatic mode selects the scene based on the current tree index, while explicit selections persist in `PlayerPrefs` (`FruitTree.EnvironmentTheme`) and apply across all unlocked trees. All scenes are available from the start, do not require paying or watching ads, and do not alter 100%-harvest requirements.

Themes update three things:
- Ground/grass material tints (the same pre-generated ground geometry, no reload).
- Sky shader palette, sun tint and light intensity, fog color and range.
- A small single renderer containing merged, statically colored decor meshes, regenerated **only on a theme switch**.

## Performance and art rules

- **Exactly one scenery renderer** for theme decor, plus the existing terrain and grass renderers. Individual flowers, buildings, benches, and background trees are *not* GameObjects.
- No scenery colliders, Rigidbody, particle emitters, Update hooks, separate cameras or large textures.
- Only one environment mesh exists after switching; the previous mesh is disposed.
- Source unit tests require each theme decor to remain under **6,000 vertices** and **2,800 triangles** and its vertices to stay outside central 5.1-unit harvesting zone. These budgets are intentionally conservative and test-only until profile data exists.
- Distant decor and sky use simple shader/material colors and fog; mobile can keep soft shadows focused on the main tree.
- Trees and fruit are still a bigger cost than these decorations; 120–220 fruit performance must be measured on devices. A good decoration budget does **not** prove the game runs at 30/60 FPS.

## UX and future environments

Environment is decoration and relaxation, not an explorable open world. The harvest tree remains the focus. Future themes may include lakeside, autumn garden, spring blossoms and evening park, but each must respect the same budgets and user control.

An eventual polish pass should improve stylized landscape models (mountain silhouettes, park architecture, leaf clusters, realistic proportions), subtle audio atmospheres and light transitions based on actual Unity screenshots. Initial meshes are lightweight prototypes, **not** final production art.

## Acceptance when end-of-development testing begins

1. Pull `main`; stop Play and run **Fruit Tree → Refresh Visuals (Back Up Scene)** once to regenerate the serialized Settings UI and attach the environment.
2. Press Play. Initial Apple environment = Meadow. Open Settings and cycle all five modes, including AUTO.
3. Confirm neither park furniture nor mountain/city decor blocks taps, orbit, zoom or 100% harvest.
4. Unlock Pear (Park) and Cherry (Mountains) and return to previously unlocked trees. Persisted manual selection must override automatic selections.
5. Run Unity Test Runner → EditMode → `EnvironmentThemeTests`.
6. Profile on low- and mid-range Android phones, including repeated theme changes, RAM, frame time, battery/temperature.
