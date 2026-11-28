using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FruitTree.Garden
{
    /// <summary>
    /// The player's own garden. Every fully picked orchard tree gives a seed; a
    /// planted seed grows by itself over a day and a half, watering only speeds
    /// it up a little, nothing ever wilts. Uses the same tree, camera and scene
    /// as the orchard: one tree at a time, shown at the size it has grown to.
    /// </summary>
    public sealed class GardenController : MonoBehaviour
    {
        private const string SeenScaleKey = "FruitTree.Garden.Seen";
        private const EnvironmentTheme GardenTheme = EnvironmentTheme.Park;
        private static GardenController instance;

        private OrchardGameController orchard;
        private FruitTreeScreenFlow flow;
        private TreeGenerator tree;
        private OrbitPickingCamera orbitCamera;
        private EnvironmentController environment;
        private OrchardAudio orchardAudio;
        private GardenSaveRepository saves;
        private GardenState state;
        private GardenScreen screen;
        private AudioSource sound;
        private AudioClip pourClip;
        private int plot;
        private bool inGarden;
        private GardenStage shownStage;
        private float nextRefresh;
        private Coroutine growing;

        public static bool IsInGarden => instance != null && instance.inGarden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInGameScene()
        {
            var orchard = FindFirstObjectByType<OrchardGameController>();
            var flow = FindFirstObjectByType<FruitTreeScreenFlow>();
            if (orchard == null || flow == null || orchard.tree == null) return;
            new GameObject("Garden").AddComponent<GardenController>().Bind(orchard, flow);
        }

        private void Bind(OrchardGameController owner, FruitTreeScreenFlow screens)
        {
            instance = this;
            orchard = owner;
            flow = screens;
            tree = owner.tree;
            orbitCamera = owner.orbitCamera;
            environment = owner.environment;
        }

        private IEnumerator Start()
        {
            // The orchard loads its save in its own Start; wait for it.
            while (orchard != null && !orchard.IsReady) yield return null;
            if (orchard == null) yield break;
            orchardAudio = FindFirstObjectByType<OrchardAudio>();
            saves = new GardenSaveRepository(Application.persistentDataPath);
            bool first = !saves.Exists;
            state = new GardenState(saves.Load());
            if (first)
            {
                // A welcome seed, and one for every tree already picked clean.
                state.AddSeed("apple");
                foreach (string species in orchard.ClearedSpecies()) state.AddSeed(species);
                Save();
            }
            orchard.TreeCleared += OnOrchardTreeCleared;
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            sound.spatialBlend = 0f;
            AddHomeButton();
        }

        private void OnDestroy()
        {
            if (orchard != null) orchard.TreeCleared -= OnOrchardTreeCleared;
            if (tree != null) tree.FruitPicked -= OnGardenFruitPicked;
            if (pourClip != null) Destroy(pourClip);
            if (instance == this) instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnOrchardTreeCleared()
        {
            if (state == null) return;
            string species = orchard.ActiveSpeciesId;
            state.AddSeed(species);
            Save();
            if (orchard.hud != null && orchard.hud.messageText != null)
                orchard.hud.messageText.text = "Tree cleared! A " + species + " seed is waiting in your garden.";
        }

        // ---------- Entering and leaving ----------

        public void Open()
        {
            if (state == null || inGarden) return;
            inGarden = true;
            state.RefreshAll(DateTime.UtcNow);
            Save();
            orchard.CloseTreeMenu();
            orchard.HarvestSuspended = true;
            if (flow.homePanel != null) flow.homePanel.SetActive(false);
            if (flow.gameplayControls != null) flow.gameplayControls.SetActive(false);
            if (flow.pause != null) flow.pause.Resume();
            if (orbitCamera != null) orbitCamera.enabled = true;
            if (environment != null) environment.ApplyThemeDirect(GardenTheme);
            tree.FruitPicked += OnGardenFruitPicked;
            EnsureScreen();
            screen.Show();
            plot = FirstPlantedPlot();
            ShowPlot();
        }

        public void Close()
        {
            if (!inGarden) return;
            inGarden = false;
            if (growing != null) { StopCoroutine(growing); growing = null; }
            tree.FruitPicked -= OnGardenFruitPicked;
            screen.Hide();
            Save();
            tree.transform.localScale = Vector3.one;
            orchard.HarvestSuspended = false;
            orchard.RedisplayActiveTree(); // Also brings back the orchard tree's scenery.
            flow.ReturnHome();
        }

        /// <summary>Android Back: close the seed list, then leave the garden.</summary>
        public static bool HandleBack()
        {
            if (!IsInGarden) return false;
            if (instance.screen.IsPickingSeed) instance.screen.HideSeedPicker();
            else instance.Close();
            return true;
        }

        private void Update()
        {
            if (!inGarden || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 20f;
            DateTime now = DateTime.UtcNow;
            bool changed = state.RefreshAll(now);
            // While the player watches, the tree may reach its next stage.
            if (changed || state.Stage(plot, now) != shownStage) ShowPlot();
            else RefreshInfo();
        }

        // ---------- Showing a bed ----------

        private int FirstPlantedPlot()
        {
            for (int i = 0; i < GardenState.PlotCount; i++)
                if (!string.IsNullOrEmpty(state.Plot(i).speciesId)) return i;
            return 0;
        }

        private void Browse(int step)
        {
            plot = (plot + step + GardenState.PlotCount) % GardenState.PlotCount;
            ShowPlot();
        }

        private void ShowPlot()
        {
            if (growing != null) { StopCoroutine(growing); growing = null; }
            DateTime now = DateTime.UtcNow;
            GardenPlotData bed = state.Plot(plot);
            shownStage = state.Stage(plot, now);
            if (shownStage == GardenStage.Empty)
            {
                // An empty bed: just the grass and the sky, waiting for a seed.
                tree.transform.localScale = Vector3.one * .001f;
            }
            else
            {
                tree.Configure(bed.speciesId, GardenState.FruitPerTree, 7001 + plot * 131);
                tree.RestorePickedFruits(shownStage == GardenStage.Fruit ? bed.pickedFruitIds : AllFruitIds());
                float target = state.TreeScale(plot, now);
                float seen = PlayerPrefs.GetFloat(SeenScaleKey + plot, target);
                // Since the last visit the tree has grown: let the player see it grow.
                float from = seen > 0f && seen < target ? seen : target;
                tree.transform.localScale = Vector3.one * from;
                if (from < target) growing = StartCoroutine(GrowTo(target, 2.2f));
                PlayerPrefs.SetFloat(SeenScaleKey + plot, target);
            }
            StartCoroutine(FitNextFrame());
            RefreshInfo();
        }

        private IEnumerator FitNextFrame()
        {
            // Old tree objects are destroyed at the end of the frame.
            yield return null;
            if (orbitCamera != null) orbitCamera.FitTreeInPortrait();
            CrownShadow.RebuildSoon();
        }

        private IEnumerator GrowTo(float target, float seconds)
        {
            float start = tree.transform.localScale.x;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                tree.transform.localScale = Vector3.one * Mathf.Lerp(start, target, k);
                yield return null;
            }
            tree.transform.localScale = Vector3.one * target;
            CrownShadow.RebuildSoon();
            growing = null;
        }

        private static List<string> AllFruitIds()
        {
            var ids = new List<string>(GardenState.FruitPerTree);
            for (int i = 0; i < GardenState.FruitPerTree; i++) ids.Add("fruit_" + i.ToString("D4"));
            return ids;
        }

        private void RefreshInfo()
        {
            DateTime now = DateTime.UtcNow;
            GardenPlotData bed = state.Plot(plot);
            GardenStage stage = state.Stage(plot, now);
            string wait = GardenState.Describe(state.TimeToFruit(plot, now));
            string name = stage == GardenStage.Empty ? "EMPTY BED" : TreeTitle(bed.speciesId);
            string status;
            switch (stage)
            {
                case GardenStage.Empty:
                    status = state.TotalSeeds > 0 ? "Plant a seed here. It grows by itself."
                        : "Pick every fruit on an orchard tree to earn its seed.";
                    break;
                case GardenStage.Sprout: status = "A tiny sprout. Fruit in " + wait + "."; break;
                case GardenStage.Sapling: status = "A young sapling. Fruit in " + wait + "."; break;
                case GardenStage.YoungTree: status = "Growing strong. Fruit in " + wait + "."; break;
                case GardenStage.Blossom: status = "Almost there! Fruit in " + wait + "."; break;
                case GardenStage.Fruit:
                    status = "Ripe fruit! Turn the tree and pick it:  " + bed.pickedFruitIds.Count +
                        " / " + GardenState.FruitPerTree;
                    break;
                default: status = "Resting after the harvest. New fruit in " + wait + "."; break;
            }

            string action;
            bool enabled, water = false;
            if (stage == GardenStage.Empty)
            {
                enabled = state.TotalSeeds > 0;
                action = enabled ? "PLANT A SEED" : "NO SEEDS YET";
            }
            else if (stage == GardenStage.Fruit)
            {
                enabled = false;
                action = "PICK THE FRUIT";
            }
            else
            {
                water = true;
                enabled = state.CanWater(plot, now);
                action = enabled ? "WATER" : "SOIL IS MOIST";
                if (!enabled) status += "\nWatered. It can drink again in " +
                    GardenState.Describe(state.SoilMoistFor(plot, now)) + ".";
            }
            screen.SetInfo("BED " + (plot + 1) + " OF " + GardenState.PlotCount, name, status, action,
                enabled, water, state.TotalSeeds, true);
        }

        public static string TreeTitle(string species)
        {
            return string.IsNullOrEmpty(species) ? "" : species.ToUpperInvariant() + " TREE";
        }

        // ---------- Actions ----------

        private void OnAction()
        {
            DateTime now = DateTime.UtcNow;
            GardenStage stage = state.Stage(plot, now);
            if (stage == GardenStage.Empty)
            {
                var seeds = new List<KeyValuePair<string, int>>();
                foreach (string species in state.SeedSpecies())
                    seeds.Add(new KeyValuePair<string, int>(species, state.SeedCount(species)));
                if (seeds.Count > 0) screen.ShowSeedPicker(seeds);
                return;
            }
            if (!state.TryWater(plot, now)) return;
            Save();
            PlayPour();
            float target = state.TreeScale(plot, now);
            PlayerPrefs.SetFloat(SeenScaleKey + plot, target);
            if (growing != null) StopCoroutine(growing);
            growing = StartCoroutine(GrowTo(target, 1.6f));
            // Watering a nearly grown tree can ripen it right now.
            if (state.Stage(plot, now) != shownStage) StartCoroutine(ShowAfter(1.7f));
            else RefreshInfo();
        }

        private IEnumerator ShowAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (inGarden) ShowPlot();
        }

        private void Plant(string species)
        {
            screen.HideSeedPicker();
            if (!state.TryPlant(plot, species, DateTime.UtcNow)) return;
            PlayerPrefs.SetFloat(SeenScaleKey + plot, 0f);
            Save();
            PlayPour();
            ShowPlot();
        }

        private void OnGardenFruitPicked(Apple fruit)
        {
            if (!inGarden || fruit == null) return;
            bool done = state.PickFruit(plot, fruit.StableFruitId, DateTime.UtcNow);
            Save();
            if (done)
            {
                shownStage = GardenStage.Resting;
                RefreshInfo();
            }
            else RefreshInfo();
        }

        private void Save()
        {
            if (saves != null && state != null) saves.Save(state.Data);
            PlayerPrefs.Save();
        }

        // ---------- Home button ----------

        /// <summary>
        /// Adds MY GARDEN under MY TREES on the home screen and spaces the five
        /// buttons evenly in the room the four used to take.
        /// </summary>
        private void AddHomeButton()
        {
            if (flow.homePanel == null) return;
            Transform home = flow.homePanel.transform;
            Transform collection = home.Find("Home Collection");
            if (collection == null || home.Find("Home Garden") != null) return;
            var garden = Instantiate(collection.gameObject, home);
            garden.name = "Home Garden";
            garden.transform.SetSiblingIndex(collection.GetSiblingIndex() + 1);
            var button = garden.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(Open);
            foreach (Text text in garden.GetComponentsInChildren<Text>(true))
                if (text.text == "MY TREES") text.text = "MY GARDEN";

            string[] order = { "Home Play", "Home Collection", "Home Garden", "Home Settings", "Home How To Play" };
            const float top = .55f, bottom = .135f, gap = .016f;
            float height = (top - bottom - gap * (order.Length - 1)) / order.Length;
            for (int i = 0; i < order.Length; i++)
            {
                var rect = home.Find(order[i]) as RectTransform;
                if (rect == null) continue;
                float high = top - i * (height + gap);
                rect.anchorMin = new Vector2(rect.anchorMin.x, high - height);
                rect.anchorMax = new Vector2(rect.anchorMax.x, high);
            }
        }

        private void EnsureScreen()
        {
            if (screen != null) return;
            Canvas canvas = flow.homePanel != null ? flow.homePanel.GetComponentInParent<Canvas>(true) : null;
            Transform parent = canvas != null ? canvas.rootCanvas.transform : transform;
            Font font = orchard.hud != null && orchard.hud.messageText != null ? orchard.hud.messageText.font : null;
            screen = GardenScreen.Create(parent, font, () => Browse(-1), () => Browse(1), OnAction, Close, Plant);
        }

        // ---------- Watering sound ----------

        private void PlayPour()
        {
            if (sound == null || (orchardAudio != null && orchardAudio.IsMuted)) return;
            if (pourClip == null) pourClip = CreatePour();
            sound.volume = .5f * AudioLevels.Gain(AudioChannel.Effects);
            sound.PlayOneShot(pourClip);
        }

        /// <summary>A soft trickle of water with a few small drops.</summary>
        public static AudioClip CreatePour()
        {
            const int rate = 22050;
            int count = (int)(rate * 1.5f);
            var rng = new System.Random(5150);
            var data = new float[count];
            float low = 0f, band = 0f, drop = 0f, dropPhase = 0f, dropPitch = 900f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                low += .35f * (white - low);
                band += .08f * (low - band);
                float stream = (low - band) * (.75f + .25f * Mathf.Sin(t * 43f) * Mathf.Sin(t * 17f));
                if (rng.NextDouble() < .0009)
                {
                    drop = .5f + .5f * (float)rng.NextDouble();
                    dropPitch = 700f + 900f * (float)rng.NextDouble();
                }
                drop *= .9985f;
                dropPhase += 2f * Mathf.PI * dropPitch * (1f + drop * .6f) / rate;
                float envelope = Mathf.Clamp01(t / .12f) * Mathf.Clamp01((1.5f - t) / .45f);
                data[i] = Mathf.Clamp((stream * .9f + Mathf.Sin(dropPhase) * drop * .18f) * envelope, -.8f, .8f);
            }
            var clip = AudioClip.Create("Watering", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
