# Test the playable three-tree progression in Unity

This is a **new gameplay implementation**, not just GDD or isolated code. Rebuild the scene once to connect the new OrchardGameController and collection buttons.

## Upgrade instructions

1. Stop Unity Play mode. Save any custom scene work.
2. From the repository, run `git pull origin main`.
3. Wait for Unity 6000.6.5f1 to finish compiling. Check Console for red errors.
4. Select **Fruit Tree > Refresh Visuals (Back Up Scene)**. The tool saves a timestamped copy of the old scene in `Builds/SceneBackups` before rebuilding `Assets/Scenes/Main.unity`.
5. Open `Assets/Scenes/Main.unity`, Game tab, **9:16**, press Play.
6. You should now see **APPLE TREE** and a **0 / 120** counter instead of 0 / 30.

## Acceptance test

- Pick 1 apple. Counter becomes 1/120.
- Stop Play, start again (without resetting the save). Counter stays 1/120 and that fruit stays picked.
- Tap **Pause > TREES**. The Tree Collection should show apple progress, and **NEXT TREE** should be disabled while apples remain.
- Pick **all 120** apples. Only then should the **NEXT TREE** shortcut appear and the Pear tree unlock.
- Select Pear: it should show **0 / 140**, with differently colored pear-shaped fruit and narrower branches.
- After fully clearing Pear, Cherry unlocks with **0 / 220**.
- Return to a completed Apple tree via the PREVIOUS button. Select **REBLOOM**: its fruit refills but Pear and Cherry access stays unlocked.
- Close Unity and reopen the project. Progress should persist using JSON under `Application.persistentDataPath`.

### Reset test progress (only when explicitly requested)

The save is `fruit-tree-orchard.json` under `Application.persistentDataPath`. Back up that file before deleting it; deletion restarts the collection from Apple. Do **not** delete the file to refresh the 3D scene.

## Known limitations of this first three-tree vertical slice

- Tree trunks/branches are still procedurally generated. Pear and cherry differ in crown dimensions, fruit mesh, color and scale, but they are **not yet high-quality botanically distinct art**.
- All 120–220 fruits must be visible and tappable: actual reachability, mobile FPS and last-fruit assistance are **not yet validated**.
- Existing Unity tests and new integration tests are committed but need to run in the actual Unity Editor.
- The current implementation saves each picked fruit; phone I/O/frame hitch profiling is still necessary.
- Ad SDK remains absent and Remove Ads purchase remains disabled until compliant integration.
