using System;
using System.Collections;
using System.Collections.Generic;
using FruitTree.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// Connects saved multi-tree progress to the existing tree, camera, sound and HUD.
    /// Ten ordered fruit species; each successor requires a previous 100% harvest.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class OrchardGameController : MonoBehaviour
    {
        public TreeGenerator tree;
        public OrbitPickingCamera orbitCamera;
        public EnvironmentController environment;
        public HarvestHUD hud;
        public GameObject treeMenuPanel;
        public Text treeLabel;
        public Text collectionStatus;
        public Button previousButton;
        public Button nextButton;
        public Button rebloomButton;
        public Button completionNextButton;
        public Button remainingFruitHintButton;
        public RemainingFruitGuide remainingFruitGuide;
        public PauseController pauseController;
        public event Action TreeCleared;
        public event Action TreeDisplayed;

        private OrchardProgressionService progress;
        private OrchardSaveRepository saves;
        private readonly string[] titles =
        {
            "APPLE TREE", "PEAR TREE", "CHERRY TREE", "PEACH TREE", "ORANGE TREE",
            "APRICOT TREE", "PLUM TREE", "LEMON TREE", "MANDARIN TREE", "POMEGRANATE TREE"
        };
        private bool initialized;
        private OrchardAudio music;
        public bool HasPreviousSave { get; private set; }
        private int hintCursor = -1;

        // Individual picks are batched: rewriting the JSON save on every tap
        // (up to 220 per tree) causes avoidable I/O hitches on low-end phones.
        // Tree clears, tree switches, rebloom, pause and quit still save at once.
        public const float PickSaveDelaySeconds = 1.5f;
        private bool saveDirty;
        private float saveDueTime;

        private static readonly TreeLevelDefinition[] Catalog =
        {
            new TreeLevelDefinition("tree_001", "apple", 150),
            new TreeLevelDefinition("tree_002", "pear", 175),
            new TreeLevelDefinition("tree_003", "cherry", 275),
            new TreeLevelDefinition("tree_004", "peach", 165),
            new TreeLevelDefinition("tree_005", "orange", 200),
            new TreeLevelDefinition("tree_006", "apricot", 225),
            new TreeLevelDefinition("tree_007", "plum", 200),
            new TreeLevelDefinition("tree_008", "lemon", 175),
            new TreeLevelDefinition("tree_009", "mandarin", 250),
            new TreeLevelDefinition("tree_010", "pomegranate", 140)
        };

        // Single source of truth for the Editor's all-species reachability audit.
        public static IReadOnlyList<TreeLevelDefinition> LevelDefinitions => Catalog;

        private void Start()
        {
            if (tree == null || hud == null)
            {
                Debug.LogError("Fruit Tree: OrchardGameController needs tree and HUD references.");
                return;
            }

            saves = new OrchardSaveRepository(Application.persistentDataPath);
            OrchardSaveData loadedSave = saves.Load();
            HasPreviousSave = HasMeaningfulProgress(loadedSave);
            progress = new OrchardProgressionService(Catalog, loadedSave);
            hud.OrchardMode = true;
            tree.FruitPicked += OnFruitPicked;
            initialized = true;
            DisplayActiveTree();
        }

        // An empty autosave written when the user leaves the first launch
        // is not a started game and must not turn PLAY into CONTINUE.
        public static bool HasMeaningfulProgress(OrchardSaveData save)
        {
            if (save == null || save.trees == null) return false;
            if (!string.IsNullOrEmpty(save.activeTreeId) &&
                save.activeTreeId != "tree_001") return true;
            foreach (var tree in save.trees)
            {
                if (tree == null) continue;
                if (tree.completedOnce || tree.timesCleared > 0 ||
                    tree.roundNumber > 1 ||
                    (tree.pickedFruitIds != null && tree.pickedFruitIds.Count > 0))
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (tree != null) tree.FruitPicked -= OnFruitPicked;
            SaveProgress();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) SaveProgress();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveProgress();
        }

        private void OnApplicationQuit() { SaveProgress(); }

        private void Update()
        {
            // Unscaled time: a pending pick must still be written while paused.
            if (saveDirty && Time.unscaledTime >= saveDueTime) SaveProgress();
        }

        private void OnFruitPicked(Apple fruit)
        {
            if (!initialized || fruit == null) return;
            if (!progress.TryPickActive(fruit.StableFruitId))
            {
                Debug.LogWarning("Fruit Tree: ignored duplicate or unknown fruit ID.");
                return;
            }
            bool cleared = progress.GetTree(progress.ActiveTreeId).IsCleared;
            if (cleared) SaveProgress(); // The unlock must never wait for a timer.
            else ScheduleSave();
            RefreshCollectionUI();
            if (cleared) TreeCleared?.Invoke();
        }

        private void ScheduleSave()
        {
            // Trailing debounce from the first unsaved pick, so continuous
            // picking still persists at least every PickSaveDelaySeconds.
            if (saveDirty) return;
            saveDirty = true;
            saveDueTime = Time.unscaledTime + PickSaveDelaySeconds;
        }

        private void DisplayActiveTree()
        {
            if (remainingFruitGuide != null) remainingFruitGuide.Hide();
            TreeRoundState active = progress.GetTree(progress.ActiveTreeId);
            int levelIndex = LevelIndex();
            tree.Configure(active.Definition.SpeciesId, active.Definition.FruitCount,
                122 + levelIndex * 197);
            tree.RestorePickedFruits(active.PickedFruitIds);
            if (environment != null) environment.UseForTree(levelIndex);
            // Each tree has its own melody.
            if (music == null) music = FindFirstObjectByType<OrchardAudio>();
            if (music != null) music.SelectTreeMusic(levelIndex);
            hintCursor = -1;
            // Wait until deferred old objects have been destroyed before computing bounds.
            StartCoroutine(RefitCameraNextFrame());
            RefreshCollectionUI();
            TreeDisplayed?.Invoke();
        }

        private IEnumerator RefitCameraNextFrame()
        {
            yield return null;
            if (orbitCamera != null) orbitCamera.FitTreeInPortrait();
        }

        private int LevelIndex()
        {
            for (int i = 0; i < Catalog.Length; i++)
                if (Catalog[i].Id == progress.ActiveTreeId) return i;
            return 0;
        }

        /// <summary>
        /// Saved active-tree progress for the lightweight home screen.
        /// Calling it never changes or writes the player's harvest state.
        /// </summary>
        public string HomeSummary()
        {
            if (!initialized || progress == null)
                return "YOUR ORCHARD AWAITS";
            int index = LevelIndex();
            TreeRoundState current = progress.GetTree(progress.ActiveTreeId);
            return "TREE " + (index + 1) + " / " + Catalog.Length + "     " +
                current.PickedCount + " / " + current.Definition.FruitCount + " FRUITS";
        }

        private TreeCollectionScreen collectionScreen;
        public bool IsTreeMenuOpen => collectionScreen != null && collectionScreen.IsOpen;

        /// <summary>Shows the list of all trees. Falls back to the old pager without a canvas.</summary>
        public void OpenTreeMenu()
        {
            if (!initialized) return;
            RefreshCollectionUI();
            // The root canvas is always active, so the list opens from home and from pause.
            Canvas canvas = treeMenuPanel != null ? treeMenuPanel.GetComponentInParent<Canvas>(true) :
                hud.messageText != null ? hud.messageText.canvas : null;
            Transform parent = canvas != null ? canvas.rootCanvas.transform : null;
            if (parent == null)
            {
                if (treeMenuPanel != null) treeMenuPanel.SetActive(true);
                return;
            }
            if (treeMenuPanel != null) treeMenuPanel.SetActive(false);
            if (collectionScreen == null)
                collectionScreen = TreeCollectionScreen.Create(parent,
                    hud.messageText != null ? hud.messageText.font : null, Catalog.Length,
                    OpenTree, CloseTreeMenu);
            collectionScreen.Show(CollectionEntries());
        }

        public void CloseTreeMenu()
        {
            if (treeMenuPanel != null) treeMenuPanel.SetActive(false);
            if (collectionScreen != null) collectionScreen.Hide();
        }

        public List<TreeCollectionScreen.Entry> CollectionEntries()
        {
            var entries = new List<TreeCollectionScreen.Entry>(Catalog.Length);
            for (int i = 0; i < Catalog.Length; i++)
            {
                TreeRoundState state = progress.GetTree(Catalog[i].Id);
                entries.Add(new TreeCollectionScreen.Entry
                {
                    Id = Catalog[i].Id,
                    Title = titles[i],
                    Fruit = FruitSpeciesProfile.Get(Catalog[i].SpeciesId).DisplayColor,
                    Picked = state.PickedCount,
                    Total = Catalog[i].FruitCount,
                    Unlocked = progress.IsUnlocked(Catalog[i].Id),
                    Cleared = state.IsCleared,
                    Active = Catalog[i].Id == progress.ActiveTreeId
                });
            }
            return entries;
        }

        /// <summary>
        /// Goes to an open tree. A fully picked tree regrows its fruit straight away,
        /// so returning to it always has something to pick.
        /// </summary>
        public void OpenTree(string id)
        {
            if (!initialized || !progress.IsUnlocked(id)) return;
            bool changedTree = id != progress.ActiveTreeId;
            if (changedTree)
            {
                if (!progress.SelectTree(id)) return;
                DisplayActiveTree();
            }
            if (progress.GetTree(id).IsCleared && progress.TryRebloomActive())
            {
                if (remainingFruitGuide != null) remainingFruitGuide.Hide();
                tree.ResetHarvestAnimated();
                hintCursor = -1;
                RefreshCollectionUI();
            }
            SaveProgress();
            CloseTreeMenu();
            if (pauseController != null) pauseController.Resume();
            if (changedTree) ShowTreeChangeAdIfDue();
        }

        /// <summary>Every few tree changes, a full-screen ad while the new tree appears.</summary>
        private void ShowTreeChangeAdIfDue()
        {
            if (!AdPacing.RegisterTreeChange()) return;
            var policy = FindFirstObjectByType<AdsAccessPolicy>();
            var ads = FindFirstObjectByType<AdMobInterstitialProvider>();
            // Not ready, offline or ad-free: the count stays, so the next change tries again.
            if (policy == null || ads == null || !policy.CanRequestAds) return;
            if (ads.TryShow(null)) AdPacing.MarkShown();
        }

        public void PreviousTree()
        {
            if (!initialized) return;
            int index = LevelIndex();
            if (index <= 0) return;
            SwitchTo(Catalog[index - 1].Id);
        }

        public void NextTree()
        {
            if (!initialized) return;
            int index = LevelIndex();
            if (index >= Catalog.Length - 1) return;
            SwitchTo(Catalog[index + 1].Id);
        }

        private void SwitchTo(string id)
        {
            OpenTree(id);
        }

        public bool CanRebloomActive()
        {
            return initialized && progress != null &&
                progress.GetTree(progress.ActiveTreeId)?.IsCleared == true;
        }

        public void Rebloom()
        {
            if (!initialized || !progress.TryRebloomActive()) return;
            // Persist round number and empty picked set BEFORE any new fruits appear.
            if (remainingFruitGuide != null) remainingFruitGuide.Hide();
            SaveProgress();
            tree.ResetHarvestAnimated();
            hintCursor = -1;
            RefreshCollectionUI();
            CloseTreeMenu();
            if (pauseController != null) pauseController.Resume();
        }

        /// <summary>
        /// Voluntary and free. No ad, currency, timer or automatic collection.
        /// Shows only when <=10 fruits remain; repeated presses cycle through them.
        /// </summary>
        public void FindRemainingFruit()
        {
            if (!initialized || orbitCamera == null || !CanOfferHint()) return;
            IReadOnlyList<Apple> fruits = tree.Apples;
            if (fruits == null || fruits.Count == 0) return;
            for (int step = 0; step < fruits.Count; step++)
            {
                hintCursor = (hintCursor + 1) % fruits.Count;
                Apple fruit = fruits[hintCursor];
                if (fruit == null || fruit.IsHarvested || !fruit.gameObject.activeInHierarchy)
                    continue;
                orbitCamera.LookTowardFruit(fruit.transform.position);
                if (remainingFruitGuide != null) remainingFruitGuide.PointAt(fruit);
                if (hud.messageText != null)
                    hud.messageText.text = "Follow the gentle camera turn to find a fruit";
                return;
            }
        }

        public static bool ShouldOfferHint(int remaining)
        {
            return remaining > 0 && remaining <= 10;
        }

        public bool CanOfferHint()
        {
            if (!initialized || progress == null) return false;
            TreeRoundState state = progress.GetTree(progress.ActiveTreeId);
            if (state == null) return false;
            int remaining = state.Definition.FruitCount - state.PickedCount;
            return remaining > 0 && remaining <= 10;
        }

        private void RefreshCollectionUI()
        {
            if (!initialized) return;
            int index = LevelIndex();
            TreeRoundState active = progress.GetTree(progress.ActiveTreeId);
            if (treeLabel != null) treeLabel.text = titles[index];
            if (collectionStatus != null)
            {
                string status = active.IsCleared ? "TREE CLEARED - 100%" :
                    active.PickedCount + " / " + active.Definition.FruitCount + " collected";
                string next = index + 1 >= Catalog.Length ? "Collection complete!" :
                    progress.IsUnlocked(Catalog[index + 1].Id)
                    ? "Next: " + titles[index + 1] + " unlocked"
                    : "Next: " + titles[index + 1] + " locked";
                collectionStatus.text = titles[index] + "  (" + (index + 1) + "/" +
                    Catalog.Length + ")\n" + status + "\n" + next;
            }
            if (previousButton != null) previousButton.interactable = index > 0;
            if (nextButton != null)
                nextButton.interactable = index + 1 < Catalog.Length &&
                    progress.IsUnlocked(Catalog[index + 1].Id);
            if (rebloomButton != null) rebloomButton.interactable = active.IsCleared;
            if (remainingFruitHintButton != null)
                remainingFruitHintButton.gameObject.SetActive(CanOfferHint());
            if (completionNextButton != null)
                completionNextButton.gameObject.SetActive(active.IsCleared &&
                    index + 1 < Catalog.Length &&
                    progress.IsUnlocked(Catalog[index + 1].Id));
            if (hud.messageText != null)
            {
                if (active.IsCleared)
                    hud.messageText.text = index + 1 < Catalog.Length
                        ? "Tree cleared. Your next tree is ready."
                        : "All trees complete. Enjoy your orchard.";
                else if (active.PickedCount == 0)
                    hud.messageText.text = "Drag to rotate  -  Tap fruit";
                else if (active.Definition.FruitCount - active.PickedCount <= 10)
                    hud.messageText.text = "Only a few fruits remain. Look closely.";
                else
                    hud.messageText.text = "Look between the branches";
            }
        }

        private void SaveProgress()
        {
            if (!initialized || progress == null || saves == null) return;
            if (saves.Save(progress.ToSave()))
            {
                saveDirty = false;
                return;
            }
            Debug.LogWarning("Fruit Tree: orchard progress was not saved.");
            // Retry later instead of hammering storage every frame.
            saveDirty = true;
            saveDueTime = Time.unscaledTime + PickSaveDelaySeconds * 4f;
        }
    }
}
