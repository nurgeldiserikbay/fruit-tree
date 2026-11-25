using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// A softer, more finished interface without touching the scene file: a rounded
    /// font, soft shadows under buttons, a gentle press, and the GRAPHICS setting.
    /// </summary>
    public static class UiPolish
    {
        public const string FontPath = "Fonts/VarelaRound-Regular";
        private static readonly Color ButtonShadow = new Color(0f, 0f, 0f, .22f);
        private static Font font;

        public static Font Font => font != null ? font : font = Resources.Load<Font>(FontPath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var flow = Object.FindFirstObjectByType<FruitTreeScreenFlow>(FindObjectsInactive.Include);
            if (flow == null) return;
            AddGraphicsButton();
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (canvas.isRootCanvas) Apply(canvas.gameObject);
        }

        /// <summary>Gives every text the rounded font and every button a shadow and a press.</summary>
        public static void Apply(GameObject root)
        {
            if (root == null) return;
            if (Font != null)
                foreach (Text text in root.GetComponentsInChildren<Text>(true)) text.font = Font;
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.targetGraphic is RoundedPanel face && face.ShadowColor.a <= 0f)
                    face.ShadowColor = ButtonShadow;
                if (button.GetComponent<PressFeedback>() == null) button.gameObject.AddComponent<PressFeedback>();
            }
        }

        // Settings order, top to bottom, once GRAPHICS is added.
        private static readonly string[] SettingsOrder =
        {
            "Settings Sound", "Settings Quiet Audio", "Settings Music", "Settings Motion",
            "Settings Scenery", "Settings Graphics", "Settings Privacy", "Settings Controls", "Settings Back"
        };

        private static void AddGraphicsButton()
        {
            Button motion = null;
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (button.name == "Settings Motion") motion = button;
            if (motion == null) return;
            Transform settings = motion.transform.parent;
            var graphics = Object.Instantiate(motion.gameObject, settings).GetComponent<Button>();
            graphics.name = "Settings Graphics";
            // The copy carries the motion toggle; switch that off and use its own action.
            for (int i = 0; i < graphics.onClick.GetPersistentEventCount(); i++)
                graphics.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            Text label = graphics.GetComponentInChildren<Text>(true);
            void Refresh() { if (label != null) label.text = GraphicsQuality.High ? "GRAPHICS: HIGH" : "GRAPHICS: LOW"; }
            Refresh();
            graphics.onClick.AddListener(GraphicsQuality.Toggle);
            GraphicsQuality.Changed += Refresh;
            Relayout(settings);
        }

        /// <summary>Spreads the settings buttons evenly so the new row fits on the card.</summary>
        private static void Relayout(Transform settings)
        {
            const float top = .728f, bottom = .085f, gap = .011f;
            float slot = (top - bottom) / SettingsOrder.Length;
            for (int i = 0; i < SettingsOrder.Length; i++)
            {
                var rect = settings.Find(SettingsOrder[i]) as RectTransform;
                if (rect == null) continue;
                float y = top - i * slot;
                bool back = i == SettingsOrder.Length - 1;
                rect.anchorMin = new Vector2(back ? .27f : .20f, y - slot + gap);
                rect.anchorMax = new Vector2(back ? .73f : .80f, y);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }
    }

    /// <summary>A small, quick shrink while a button is held, like a real press.</summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = .96f, Speed = 18f;
        private Selectable selectable;
        private bool pressed;

        private void Awake() { selectable = GetComponent<Selectable>(); }

        public void OnPointerDown(PointerEventData data)
        {
            pressed = selectable == null || selectable.IsInteractable();
        }

        public void OnPointerUp(PointerEventData data) { pressed = false; }
        public void OnPointerExit(PointerEventData data) { pressed = false; }

        private void OnDisable()
        {
            pressed = false;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float target = pressed ? PressedScale : 1f;
            float scale = transform.localScale.x;
            if (Mathf.Approximately(scale, target)) return;
            scale = Mathf.MoveTowards(scale, target, Speed * (1f - PressedScale) * Time.unscaledDeltaTime * 4f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
