using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    public sealed class HarvestHUD : MonoBehaviour
    {
        public TreeGenerator tree;
        public Text countText;
        public Text messageText;
        public bool OrchardMode { get; set; }
        public const string ReachHint = "Come closer to pick";
        private OrbitPickingCamera orbit;
        private Coroutine reachHint;
        private void Start()
        {
            orbit = FindFirstObjectByType<OrbitPickingCamera>();
            if (orbit != null) orbit.FruitOutOfReach += ShowReachHint;
            if (tree == null) return;
            tree.HarvestChanged += Refresh;
            Refresh(tree.HarvestedCount, tree.TotalApples);
        }
        private void OnDestroy()
        {
            if (orbit != null) orbit.FruitOutOfReach -= ShowReachHint;
            if (tree != null) tree.HarvestChanged -= Refresh;
        }

        /// <summary>Briefly explains why a distant fruit did not come off.</summary>
        public void ShowReachHint()
        {
            if (messageText == null || !isActiveAndEnabled) return;
            if (reachHint != null) StopCoroutine(reachHint);
            reachHint = StartCoroutine(ReachHintRoutine());
        }

        private System.Collections.IEnumerator ReachHintRoutine()
        {
            string previous = messageText.text == ReachHint ? "Look between the branches" : messageText.text;
            messageText.text = ReachHint;
            yield return new WaitForSecondsRealtime(2f);
            if (messageText != null && messageText.text == ReachHint) messageText.text = previous;
            reachHint = null;
        }
        public void Restart()
        {
            // Multi-tree progress is authoritative: a visual reset here would
            // desynchronize restored per-fruit IDs and the permanent unlocks.
            // The orchard controller alone may rebloom a fully cleared tree.
            if (OrchardMode)
            {
                Debug.LogWarning("Fruit Tree: use Rebloom for completed orchard trees.");
                return;
            }
            Time.timeScale = 1;
            if (tree == null) return;
            tree.ResetHarvest();
            if (messageText != null) messageText.text = "Drag to rotate  -  Tap fruit";
        }
        private void Refresh(int picked, int total)
        {
            if (countText != null) countText.text = picked + " / " + total;
            if (picked == total && total > 0)
            {
                int best = Mathf.Max(PlayerPrefs.GetInt("FruitTree.Best", 0), picked);
                PlayerPrefs.SetInt("FruitTree.Best", best);
                PlayerPrefs.Save();
                if (messageText != null) messageText.text = OrchardMode
                    ? "Tree cleared! Next tree is ready."
                    : "All apples collected! Pause > Restart";
            }
            else if (picked > 0 && messageText != null && messageText.text.StartsWith("Drag"))
                messageText.text = "Look between the branches";
        }
    }
}
