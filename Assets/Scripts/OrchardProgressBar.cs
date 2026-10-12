using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// One lightweight UI strip, driven by harvest events rather than polling.
    /// Works for all species and restores the actual saved picked fraction.
    /// </summary>
    public sealed class OrchardProgressBar : MonoBehaviour
    {
        public TreeGenerator tree;
        public RectTransform fill;
        private bool subscribed;

        private void Start()
        {
            if (tree == null) return;
            tree.HarvestChanged += OnHarvestChanged;
            subscribed = true;
            OnHarvestChanged(tree.HarvestedCount, tree.TotalApples);
        }

        private void OnDestroy()
        {
            if (subscribed && tree != null)
                tree.HarvestChanged -= OnHarvestChanged;
        }

        public void OnHarvestChanged(int picked, int total)
        {
            if (fill == null) return;
            float proportion = Fraction(picked, total);
            Vector2 max = fill.anchorMax;
            max.x = proportion;
            fill.anchorMax = max;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        public static float Fraction(int picked, int total)
        {
            return total <= 0 ? 0f : Mathf.Clamp01(picked / (float)total);
        }
    }
}
