using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// An unobtrusive fade after a REAL final fruit pick, never after save restore.
    /// No win screen, confetti, particles or extra hierarchy rebuilds.
    /// </summary>
    public sealed class CalmCompletionFeedback : MonoBehaviour
    {
        public OrchardGameController orchard;
        public CanvasGroup messageGroup;
        public Text message;

        private float elapsed;
        private bool playing;

        private void Awake()
        {
            if (messageGroup != null) messageGroup.alpha = 0f;
        }

        private void Start()
        {
            if (orchard != null)
            {
                orchard.TreeCleared += Celebrate;
                orchard.TreeDisplayed += Dismiss;
            }
        }

        private void OnDestroy()
        {
            if (orchard != null)
            {
                orchard.TreeCleared -= Celebrate;
                orchard.TreeDisplayed -= Dismiss;
            }
        }

        private void Celebrate()
        {
            if (message != null) message.text = "EVERY FRUIT COLLECTED";
            elapsed = 0f;
            playing = true;
            if (messageGroup != null) messageGroup.alpha = 0f;
        }

        private void Dismiss()
        {
            playing = false;
            elapsed = 0f;
            if (messageGroup != null) messageGroup.alpha = 0f;
        }

        private void Update()
        {
            if (!playing || messageGroup == null) return;
            elapsed += Time.unscaledDeltaTime;
            messageGroup.alpha = EvaluateAlpha(elapsed);
            if (elapsed >= 4.5f) playing = false;
        }

        public static float EvaluateAlpha(float seconds)
        {
            if (seconds <= 0f || seconds >= 4.5f) return 0f;
            if (seconds < .85f) return Mathf.SmoothStep(0f, .92f, seconds / .85f);
            if (seconds < 3.35f) return .92f;
            return .92f * (1f - Mathf.SmoothStep(0f, 1f, (seconds - 3.35f) / 1.15f));
        }
    }
}
