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

        public const string ReachBubbleText = "Too far - zoom in to pick";
        private RectTransform reachBubble;
        private CanvasGroup reachBubbleGroup;
        private Coroutine reachBubbleFade;

        /// <summary>Briefly explains why a distant fruit did not come off: a bubble at the finger.</summary>
        public void ShowReachHint(Vector2 screenPoint)
        {
            if (messageText == null || !isActiveAndEnabled) return;
            if (reachHint != null) StopCoroutine(reachHint);
            reachHint = StartCoroutine(ReachHintRoutine());
            if (reachBubble == null) BuildReachBubble();
            if (reachBubble == null) return;
            var canvasRect = (RectTransform)reachBubble.parent;
            Canvas canvas = messageText.canvas.rootCanvas;
            Camera eye = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eye,
                    out Vector2 local)) return;
            // Above the finger, kept inside the screen.
            Rect bounds = canvasRect.rect;
            Vector2 half = reachBubble.sizeDelta * .5f;
            local.y += 150f;
            local.x = Mathf.Clamp(local.x, bounds.xMin + half.x + 20f, bounds.xMax - half.x - 20f);
            local.y = Mathf.Clamp(local.y, bounds.yMin + half.y + 20f, bounds.yMax - half.y - 20f);
            reachBubble.anchoredPosition = local;
            reachBubble.SetAsLastSibling();
            if (reachBubbleFade != null) StopCoroutine(reachBubbleFade);
            reachBubbleFade = StartCoroutine(FadeReachBubble(local));
        }

        private void BuildReachBubble()
        {
            Canvas canvas = messageText.canvas != null ? messageText.canvas.rootCanvas : null;
            if (canvas == null) return;
            var bubble = new GameObject("Reach Hint Bubble", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanel), typeof(CanvasGroup));
            reachBubble = bubble.GetComponent<RectTransform>();
            reachBubble.SetParent(canvas.transform, false);
            reachBubble.anchorMin = reachBubble.anchorMax = new Vector2(.5f, .5f);
            reachBubble.sizeDelta = new Vector2(620f, 96f);
            var face = bubble.GetComponent<RoundedPanel>();
            face.CornerRadius = 48f;
            face.color = new Color(.10f, .22f, .15f, .9f);
            face.raycastTarget = false;
            reachBubbleGroup = bubble.GetComponent<CanvasGroup>();
            reachBubbleGroup.blocksRaycasts = false;
            reachBubbleGroup.interactable = false;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.SetParent(reachBubble, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 8f);
            labelRect.offsetMax = new Vector2(-24f, -8f);
            var text = label.GetComponent<Text>();
            text.font = messageText.font;
            text.fontSize = 40;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = 40;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.98f, .96f, .86f);
            text.text = ReachBubbleText;
            text.raycastTarget = false;
            bubble.SetActive(false);
        }

        private System.Collections.IEnumerator FadeReachBubble(Vector2 start)
        {
            reachBubble.gameObject.SetActive(true);
            const float duration = 1.6f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float f = t / duration;
                // Appears at once where the finger is, then fades away.
                reachBubbleGroup.alpha = f > .6f ? (1f - f) / .4f : 1f;
                reachBubble.anchoredPosition = start + Vector2.up * (f * 40f);
                yield return null;
            }
            reachBubble.gameObject.SetActive(false);
            reachBubbleFade = null;
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
