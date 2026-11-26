using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// Settings window with three volume sliders. Takes over the old
    /// "AUDIO: SOFT / NORMAL" button, which now opens it as "SOUND LEVELS".
    /// </summary>
    public sealed class SoundLevelsPanel : MonoBehaviour
    {
        private static readonly Color Veil = new Color(.05f, .12f, .09f, .9f);
        private static readonly Color Card = new Color(.12f, .255f, .18f, .97f);
        private static readonly Color Ivory = new Color(.97f, .965f, .89f, 1f);
        private static readonly Color Track = new Color(.07f, .15f, .11f, 1f);
        private static readonly Color Fill = new Color(.97f, .86f, .61f, 1f);
        private static readonly Color Ink = new Color(.18f, .26f, .17f, 1f);
        private static SoundLevelsPanel instance;
        private Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void TakeOverSettingsButton()
        {
            var flow = FindFirstObjectByType<FruitTreeScreenFlow>(FindObjectsInactive.Include);
            if (flow == null) return;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name != "Settings Quiet Audio") continue;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "SOUND LEVELS";
                if (flow.audioLevelLabel == label) flow.audioLevelLabel = null;
                Transform parent = button.transform.parent.parent;
                Font font = label != null ? label.font : null;
                button.onClick.AddListener(() => Open(parent, font));
                return;
            }
        }

        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        public static void Open(Transform parent, Font font)
        {
            if (instance == null) instance = Build(parent, font);
            instance.transform.SetAsLastSibling();
            instance.gameObject.SetActive(true);
        }

        /// <summary>Closes the window if it is showing; true when it did.</summary>
        public static bool CloseIfOpen()
        {
            if (!IsOpen) return false;
            instance.gameObject.SetActive(false);
            return true;
        }

        // Dragging changes the level every frame; write to disk once, when done.
        private void OnDisable()
        {
            PlayerPrefs.Save();
        }

        private static SoundLevelsPanel Build(Transform parent, Font font)
        {
            var root = Panel("Sound Levels Screen", parent, Vector2.zero, Vector2.one, Veil, 0f);
            root.GetComponent<RoundedPanel>().raycastTarget = true;
            var panel = root.gameObject.AddComponent<SoundLevelsPanel>();
            panel.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Panel("Card", root, new Vector2(.08f, .2f), new Vector2(.92f, .8f), Card, 66f);
            panel.Label("Title", root, "SOUND LEVELS", 70, Color.white, TextAnchor.MiddleCenter,
                new Vector2(.12f, .69f), new Vector2(.88f, .77f));
            panel.Row(root, "MUSIC", AudioChannel.Music, .57f);
            panel.Row(root, "NATURE  -  rain, birds, brook", AudioChannel.Nature, .45f);
            panel.Row(root, "EFFECTS  -  picking, leaves", AudioChannel.Effects, .33f);

            var back = Panel("Back", root, new Vector2(.27f, .23f), new Vector2(.73f, .3f), Ivory, 44f);
            var backFace = back.GetComponent<RoundedPanel>();
            // Panel() makes decorative, untappable shapes; a button face must catch taps.
            backFace.raycastTarget = true;
            var backButton = back.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backFace;
            panel.Label("Back Label", back, "BACK", 44, Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            backButton.onClick.AddListener(() => CloseIfOpen());
            UiPolish.Apply(root.gameObject);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Row(RectTransform root, string title, AudioChannel channel, float y)
        {
            Label(title + " Label", root, title, 34, Color.white, TextAnchor.MiddleLeft,
                new Vector2(.14f, y + .045f), new Vector2(.7f, y + .09f));
            Text percent = Label(title + " Percent", root, "", 34, Fill, TextAnchor.MiddleRight,
                new Vector2(.7f, y + .045f), new Vector2(.86f, y + .09f));
            Slider slider = MakeSlider(root, new Vector2(.14f, y), new Vector2(.86f, y + .04f));
            slider.value = AudioLevels.Get(channel);
            percent.text = Mathf.RoundToInt(slider.value * 100f) + "%";
            slider.onValueChanged.AddListener(value =>
            {
                AudioLevels.Set(channel, value);
                percent.text = Mathf.RoundToInt(value * 100f) + "%";
            });
        }

        private static Slider MakeSlider(RectTransform parent, Vector2 min, Vector2 max)
        {
            var body = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            var rect = body.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, min, max);
            var track = Panel("Track", rect, new Vector2(0f, .3f), new Vector2(1f, .7f), Track, 20f);
            var fillArea = Panel("Fill Area", rect, new Vector2(0f, .3f), new Vector2(1f, .7f), Color.clear, 0f);
            var fill = Panel("Fill", fillArea, Vector2.zero, Vector2.one, Fill, 20f);
            var handleArea = Panel("Handle Area", rect, Vector2.zero, Vector2.one, Color.clear, 0f);
            handleArea.offsetMin = new Vector2(24f, 0f);
            handleArea.offsetMax = new Vector2(-24f, 0f);
            var handle = Panel("Handle", handleArea, Vector2.zero, new Vector2(0f, 1f), Ivory, 40f);
            handle.sizeDelta = new Vector2(48f, 0f);
            // The handle can be grabbed directly, not only the track.
            handle.GetComponent<RoundedPanel>().raycastTarget = true;
            var slider = body.GetComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<RoundedPanel>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            // The whole track is a target, so a tap anywhere moves the handle there.
            track.GetComponent<RoundedPanel>().raycastTarget = true;
            return slider;
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max,
            Color color, float radius)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedPanel));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, min, max);
            var face = go.GetComponent<RoundedPanel>();
            face.color = color;
            face.CornerRadius = radius;
            face.raycastTarget = false;
            return rect;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color,
            TextAnchor alignment, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = size / 2;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
