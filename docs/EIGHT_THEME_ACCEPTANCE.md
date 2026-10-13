# Eight environments — acceptance notes

Source added: four new static low-poly theme silhouettes. Unity compilation, geometry budget tests, and Android profiling have NOT been run.

- Existing save integers 0-3 continue to refer to Meadow, Park, City Park and Mountains. New values are 4 Japanese Garden, 5 Chinese Garden, 6 Lakeside and 7 Evening Garden. No orchard progression/save format change.
- AUTO uses the existing four themes for first four trees. Trees 5-8 automatically showcase new scenes; Settings cycle includes all eight.
- Every scenery is one cached combined mesh, no prop colliders, no per-frame scenery scripts. Automated EditMode checks require fewer than 6,000 vertices, fewer than 2,800 triangles and clear space beyond 5.1 units.
- Generated nature audio adds subtle garden-water texture, birds and evening insects. These are synthesized placeholders, not actual field recordings.
- Optional `OrchardAudio.themeMusic[0..7]` accepts externally sourced, commercially licensed audio clips. No copyrighted recordings or Suno files are included. Missing clips stay silent without affecting ambience.
- Settings MUSIC toggle is independent of the AUDIO SOFT/NORMAL toggle, but global SOUND OFF mutes everything. PlayerPrefs stores music preference.
- When the user finishes scene testing: inspect garden silhouettes from 360° orbit, measure FPS and peak RAM on an Android phone, listen for sudden transitions, verify every one of the ten trees remains harvestable, and confirm Settings music toggle after relaunch.
- Regenerate an old local scene with **Fruit Tree > Refresh Visuals (Back Up Scene)** before checking the new Music UI and corrected home footer.
