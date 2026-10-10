using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    public sealed class HarvestHUD : MonoBehaviour
    {
        public TreeGenerator tree;
        public Text countText;
        private void Start()
        {
            if (tree == null || countText == null) return;
            tree.HarvestChanged += Refresh;
            Refresh(0, tree.Apples.Count);
        }
        private void OnDestroy()
        {
            if (tree != null) tree.HarvestChanged -= Refresh;
        }
        private void Refresh(int picked, int total)
        {
            countText.text = "Apples  " + picked + " / " + total;
        }
    }
}
