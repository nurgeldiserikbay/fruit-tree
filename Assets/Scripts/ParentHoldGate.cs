using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>Parent area intentionally stays out of normal children's play.</summary>
    public sealed class ParentHoldGate : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public GameObject parentPanel;
        public Text label;
        [Range(2f, 5f)] public float holdSeconds = 3f;
        private bool held;
        private float startedAt;

        public void OnPointerDown(PointerEventData eventData)
        {
            startedAt = Time.unscaledTime;
            held = true;
        }

        public void OnPointerUp(PointerEventData eventData) { ResetHold(); }
        public void OnPointerExit(PointerEventData eventData) { ResetHold(); }

        private void Update()
        {
            if (!held) return;
            float elapsed = Time.unscaledTime - startedAt;
            if (label != null)
                label.text = "HOLD " + Mathf.CeilToInt(Mathf.Max(0f, holdSeconds - elapsed)) + "s";
            if (elapsed < holdSeconds) return;
            ResetHold();
            if (parentPanel != null) parentPanel.SetActive(true);
        }

        private void OnDisable() { ResetHold(); }

        private void ResetHold()
        {
            held = false;
            if (label != null) label.text = "FOR PARENTS - HOLD";
        }
    }
}
