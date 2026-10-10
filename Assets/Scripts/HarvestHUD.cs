using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    public sealed class HarvestHUD : MonoBehaviour
    {
        public TreeGenerator tree;
        public Text countText;
        public Text messageText;
        private void Start()
        {
            if (tree == null) return;
            tree.HarvestChanged += Refresh;
            Refresh(tree.HarvestedCount, tree.TotalApples);
        }
        private void OnDestroy()
        {
            if (tree != null) tree.HarvestChanged -= Refresh;
        }
        public void Restart()
        {
            Time.timeScale = 1;
            if (tree == null) return;
            tree.ResetHarvest();
            if (messageText != null) messageText.text = "Drag to rotate  -  Tap apples";
        }
        private void Refresh(int picked, int total)
        {
            if (countText != null) countText.text = picked + " / " + total;
            if (picked == total && total > 0)
            {
                int best = Mathf.Max(PlayerPrefs.GetInt("FruitTree.Best", 0), picked);
                PlayerPrefs.SetInt("FruitTree.Best", best);
                PlayerPrefs.Save();
                if (messageText != null) messageText.text = "All apples collected!  Tap Restart.";
            }
            else if (picked > 0 && messageText != null && messageText.text.StartsWith("Drag"))
                messageText.text = "Look between the branches";
        }
    }
}
