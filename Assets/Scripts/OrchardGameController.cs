using System.Collections;
using System.Collections.Generic;
using FruitTree.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// Connects saved multi-tree progress to the existing tree, camera, sound and HUD.
    /// Apple -> pear -> cherry; each successive tree requires a complete 100% harvest.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class OrchardGameController : MonoBehaviour
    {
        public TreeGenerator tree;
        public OrbitPickingCamera orbitCamera;
        public HarvestHUD hud;
        public GameObject treeMenuPanel;
        public Text treeLabel;
        public Text collectionStatus;
        public Button previousButton;
        public Button nextButton;
        public Button rebloomButton;
        public Button completionNextButton;

        private OrchardProgressionService progress;
        private OrchardSaveRepository saves;
        private readonly string[] titles = { "APPLE TREE", "PEAR TREE", "CHERRY TREE" };
        private bool initialized;

        private static readonly TreeLevelDefinition[] Catalog =
        {
            new TreeLevelDefinition("tree_001", "apple", 120),
            new TreeLevelDefinition("tree_002", "pear", 140),
            new TreeLevelDefinition("tree_003", "cherry", 220)
        };

        private void Start()
        {
            if (tree == null || hud == null)
            {
                Debug.LogError("Fruit Tree: OrchardGameController needs tree and HUD references.");
                return;
            }

            saves = new OrchardSaveRepository(Application.persistentDataPath);
            progress = new OrchardProgressionService(Catalog, saves.Load());
            hud.OrchardMode = true;
            tree.FruitPicked += OnFruitPicked;
            initialized = true;
            DisplayActiveTree();
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

        private void OnApplicationQuit() { SaveProgress(); }

        private void OnFruitPicked(Apple fruit)
        {
            if (!initialized || fruit == null) return;
            if (!progress.TryPickActive(fruit.StableFruitId))
            {
                Debug.LogWarning("Fruit Tree: ignored duplicate or unknown fruit ID.");
                return;
            }
            SaveProgress();
            RefreshCollectionUI();
        }

        private void DisplayActiveTree()
        {
            TreeRoundState active = progress.GetTree(progress.ActiveTreeId);
            int levelIndex = LevelIndex();
            tree.Configure(active.Definition.SpeciesId, active.Definition.FruitCount,
                122 + levelIndex * 197);
            tree.RestorePickedFruits(active.PickedFruitIds);
            // Wait until deferred old objects have been destroyed before computing bounds.
            StartCoroutine(RefitCameraNextFrame());
            RefreshCollectionUI();
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

        public void OpenTreeMenu()
        {
            if (!initialized || treeMenuPanel == null) return;
            RefreshCollectionUI();
            treeMenuPanel.SetActive(true);
        }

        public void CloseTreeMenu()
        {
            if (treeMenuPanel != null) treeMenuPanel.SetActive(false);
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
            if (!progress.SelectTree(id)) return;
            SaveProgress();
            DisplayActiveTree();
        }

        public void Rebloom()
        {
            if (!initialized || !progress.TryRebloomActive()) return;
            tree.ResetHarvest();
            SaveProgress();
            RefreshCollectionUI();
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
            if (initialized && progress != null && saves != null &&
                !saves.Save(progress.ToSave()))
                Debug.LogWarning("Fruit Tree: orchard progress was not saved.");
        }
    }
}
