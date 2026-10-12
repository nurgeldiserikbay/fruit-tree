using UnityEngine;

namespace FruitTree
{
    /// <summary>Optional, passive marker for the last few unpicked fruits.</summary>
    public sealed class RemainingFruitGuide : MonoBehaviour
    {
        public Camera worldCamera;
        public RectTransform overlay;
        public RectTransform marker;
        public CanvasGroup markerGroup;

        private Apple chosenFruit;
        private float elapsed;
        public bool IsVisible => chosenFruit != null && enabled;

        private void Awake() { Hide(); }

        public void PointAt(Apple fruit)
        {
            if (fruit == null || fruit.IsHarvested || !fruit.gameObject.activeInHierarchy ||
                worldCamera == null || overlay == null || marker == null ||
                markerGroup == null) return;
            chosenFruit = fruit;
            elapsed = 0f;
            enabled = true;
            markerGroup.alpha = 0f;
        }

        public void Hide()
        {
            chosenFruit = null;
            elapsed = 0f;
            if (markerGroup != null) markerGroup.alpha = 0f;
            enabled = false;
        }

        private void Update()
        {
            if (chosenFruit == null || chosenFruit.IsHarvested ||
                !chosenFruit.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= 4.5f) { Hide(); return; }
            Vector3 position = worldCamera.WorldToScreenPoint(chosenFruit.transform.position);
            if (position.z <= 0f)
            {
                markerGroup.alpha = 0f;
                return;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlay, position, null, out var local))
            {
                marker.anchoredPosition = local + new Vector2(0f, 34f);
                marker.localScale = Vector3.one * (1f + .1f * Mathf.Sin(elapsed * 6f));
                markerGroup.alpha = .74f * Visibility(elapsed);
            }
        }

        public static float Visibility(float seconds)
        {
            if (seconds < 0f || seconds >= 4.5f) return 0f;
            return Mathf.Clamp01(seconds / .25f) *
                Mathf.Clamp01((4.5f - seconds) / .9f);
        }
    }
}
