# Five-tree collection release notes — source code

As of 2026-10-12, the repository implements five tree entries in `OrchardGameController`: Apple 120 → Pear 140 → Cherry 220 → Peach 130 → Orange 160.

The new species are backed by procedurally generated fruit meshes (`ProceduralMesh.Peach` and `ProceduralMesh.Orange`), a shared per-species 64×64 skin texture (`AppleSkinTexture.CreateForSpecies`), 3 shared color-variant materials, and distinct branch/canopy/leaf palettes in `TreeGenerator`.

Old save compatibility: `tree_001`–`tree_003`, their fruit counts and fruit IDs are unchanged. The `OrchardProgressionService` reconstructs state for added catalog entries from a prior three-tree save without granting new unlocks. This is covered by a new EditMode test.

**Remaining first-release content:** at least five additional fruit tree types; high-quality art, lighting and sound design; actual Unity compilation, Android profile and fruit-reachability acceptance. Google Play readiness remains a separate release gate.

See [PLAYTEST_FIVE_TREES.md](PLAYTEST_FIVE_TREES.md).
