using UnityEngine;

namespace FruitTree
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastSize;
        private RectTransform rect;
        private void Awake() { rect = GetComponent<RectTransform>(); }
        private void LateUpdate()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == lastSafeArea && size == lastSize) return;
            lastSafeArea = safe; lastSize = size;
            rect.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            rect.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
