using UnityEngine;

namespace FruitTree
{
    /// <summary>
    /// Three prebuilt, non-interactive UI glints signal a successful fruit tap.
    /// Nothing is instantiated during harvesting; Update runs only while active.
    /// </summary>
    public sealed class FruitPickPulse : MonoBehaviour
    {
        public TreeGenerator tree;
        public Camera worldCamera;
        public RectTransform overlay;
        public CanvasGroup[] glints;

        private readonly Vector2[] starts = new Vector2[3];
        private float elapsed;

        public bool IsAnimating { get; private set; }

        private void Start()
        {
            if (tree != null) tree.FruitPicked += Show;
            Hide();
        }

        private void OnDestroy()
        {
            if (tree != null) tree.FruitPicked -= Show;
        }

        private void Show(Apple picked)
        {
            if (picked == null || worldCamera == null || overlay == null ||
                glints == null || glints.Length != starts.Length) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(picked.transform.position);
            if (screen.z <= 0f) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlay, screen, null, out var center)) return;

            for (int i = 0; i < starts.Length; i++)
            {
                if (glints[i] == null) continue;
                starts[i] = center + new Vector2((i - 1) * 17f,
                    i == 1 ? 9f : -4f);
                glints[i].transform.localScale = Vector3.one;
                glints[i].GetComponent<RectTransform>().anchoredPosition = starts[i];
                glints[i].alpha = 0.65f;
            }
            elapsed = 0f;
            IsAnimating = true;
            enabled = true;
        }

        private void Update()
        {
            if (!IsAnimating) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / .42f);
            for (int i = 0; i < starts.Length; i++)
            {
                if (glints == null || i >= glints.Length || glints[i] == null)
                    continue;
                glints[i].alpha = Opacity(t) * .65f;
                glints[i].GetComponent<RectTransform>().anchoredPosition =
                    starts[i] + new Vector2((i - 1) * 12f * t,
                        (25f + (i % 2) * 12f) * t);
            }
            if (t >= 1f) Hide();
        }

        private void Hide()
        {
            IsAnimating = false;
            if (glints != null)
                foreach (var glint in glints)
                    if (glint != null) glint.alpha = 0f;
            enabled = false;
        }

        public static float Opacity(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return (1f - t) * (1f - t);
        }
    }
}
