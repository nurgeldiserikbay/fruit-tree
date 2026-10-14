# Imported tree textures

The main tree supports optional artist-authored texture assets without changing fruit picking, mesh generation or existing saves.

- Bark: `Assets/Resources/TreeTextures/Bark.png`
- Fruit: `Assets/Resources/TreeTextures/Fruit/apple.png`, `pear.png`, `cherry.png`, `peach.png`, `orange.png`, `apricot.png`, `plum.png`, `lemon.png`, `mandarin.png`, `pomegranate.png`

Unity imports these as Texture2D. With an asset present the material uses it; with an asset absent the existing deterministic procedural skin is generated. Imported textures are shared across fruit instances and never destroyed by the runtime tree. Suggested starting point: 256–512px per fruit, mobile texture compression, mipmaps, horizontal Repeat wrap, and profile on physical Android hardware. Textures should be independently tileable: do not rename the visual-concept images to these runtime paths.

**Status:** the optional texture-loading code is committed, but photographic bark/fruit artwork has not yet been imported or visually validated. The panorama concepts remain **references**, not ready-to-use 360-degree skybox maps.

## Transparent leaf picking

Optional leaf cutout art is loaded from `Assets/Resources/TreeTextures/Leaves/<species>.png` with `Default.png` fallback. The shader uses alpha cutoff 0.45. To let taps pass through transparent texels, **enable Read/Write** in the leaf texture Unity import settings; otherwise the conservative picking fallback treats the leaf collider as opaque and protects hidden fruit. Keep the cutout silhouette aligned to the leaf mesh UVs, use clamp/repeat appropriate for the atlas, and check mipmapping alpha edges. This pixel-aware picking path uses `Physics.RaycastAll` on taps only, not every frame. Unity device validation and an occlusion play test are still required.
