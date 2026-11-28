using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FruitTree.Garden
{
    /// <summary>
    /// The garden's overlay, built in code: a card on top with the bed and how
    /// the tree is doing, buttons at the bottom. The middle stays free, so the
    /// player can turn the tree and pick its fruit.
    /// </summary>
    public sealed class GardenScreen : MonoBehaviour
    {
        private static readonly Color Card = new Color(.10f, .24f, .17f, .82f);
        private static readonly Color Ivory = new Color(.97f, .965f, .89f, 1f);
        private static readonly Color Gold = new Color(.97f, .86f, .61f, 1f);
        private static readonly Color Water = new Color(.70f, .86f, .93f, 1f);
        private static readonly Color Ink = new Color(.18f, .26f, .17f, 1f);
        private static readonly Color Pale = new Color(.92f, .94f, .88f, .9f);
        private static readonly Color Veil = new Color(.05f, .12f, .09f, .94f);

        private Font font;
        private Text bedLabel, nameLabel, statusLabel, seedsLabel, actionLabel;
        private Button actionButton, previousButton, nextButton;
        private RoundedPanel actionFace;
        private GameObject seedPicker;
        private RectTransform seedList;
        private readonly List<GameObject> seedButtons = new List<GameObject>();
        private Action<string> plant;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsPickingSeed => seedPicker != null && seedPicker.activeSelf;

        public static GardenScreen Create(Transform parent, Font font, Action previous, Action next,
            Action action, Action back, Action<string> plant)
        {
            var root = new GameObject("Garden Screen", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, Vector2.zero, Vector2.one);
            var screen = root.AddComponent<GardenScreen>();
            screen.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen.plant = plant;

            // Top card: where we are and how the tree is doing.
            var top = Panel("Garden Card", rect, new Vector2(.05f, .785f), new Vector2(.95f, .965f), Card);
            screen.Label("Garden Title", top, "MY GARDEN", 64, Color.white, new Vector2(.05f, .66f), new Vector2(.95f, .96f));
            screen.bedLabel = screen.Label("Garden Bed", top, "", 32, Pale, new Vector2(.05f, .52f), new Vector2(.95f, .68f));
            screen.nameLabel = screen.Label("Garden Tree Name", top, "", 40, Gold, new Vector2(.05f, .30f), new Vector2(.95f, .54f));
            screen.statusLabel = screen.Label("Garden Status", top, "", 36, Color.white, new Vector2(.05f, .04f), new Vector2(.95f, .31f));
            screen.statusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Bottom: previous bed, the one useful action, next bed; then back.
            screen.previousButton = screen.MakeButton("Garden Previous", rect, "<", new Vector2(.05f, .135f), new Vector2(.21f, .215f), Ivory, previous);
            screen.actionButton = screen.MakeButton("Garden Action", rect, "", new Vector2(.24f, .135f), new Vector2(.76f, .215f), Water, action);
            screen.actionFace = screen.actionButton.GetComponent<RoundedPanel>();
            screen.actionLabel = screen.actionButton.GetComponentInChildren<Text>();
            screen.nextButton = screen.MakeButton("Garden Next", rect, ">", new Vector2(.79f, .135f), new Vector2(.95f, .215f), Ivory, next);
            screen.MakeButton("Garden Back", rect, "BACK", new Vector2(.05f, .035f), new Vector2(.47f, .11f), Ivory, back);
            var seedsCard = Panel("Garden Seeds Card", rect, new Vector2(.53f, .035f), new Vector2(.95f, .11f), Card);
            screen.seedsLabel = screen.Label("Garden Seeds", seedsCard, "", 32, Color.white, Vector2.zero, Vector2.one);

            screen.BuildSeedPicker(rect, back);
            UiPolish.Apply(root);
            root.SetActive(false);
            return screen;
        }

        public void Show()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            HideSeedPicker();
            gameObject.SetActive(false);
        }

        public void SetInfo(string bed, string name, string status, string action, bool actionEnabled,
            bool actionIsWater, int seeds, bool canBrowse)
        {
            bedLabel.text = bed;
            nameLabel.text = name;
            statusLabel.text = status;
            actionLabel.text = action;
            actionButton.interactable = actionEnabled;
            actionFace.color = !actionEnabled ? new Color(.80f, .82f, .76f, .9f) : actionIsWater ? Water : Gold;
            seedsLabel.text = seeds == 1 ? "1 SEED" : seeds + " SEEDS";
            previousButton.interactable = canBrowse;
            nextButton.interactable = canBrowse;
        }

        // ---------- Seed picker ----------

        private void BuildSeedPicker(RectTransform parent, Action back)
        {
            seedPicker = Panel("Garden Seed Picker", parent, Vector2.zero, Vector2.one, Veil).gameObject;
            seedPicker.GetComponent<RoundedPanel>().CornerRadius = 0f;
            seedPicker.GetComponent<RoundedPanel>().raycastTarget = true;
            var pickerRect = (RectTransform)seedPicker.transform;
            Label("Seed Title", pickerRect, "PLANT A SEED", 64, Color.white, new Vector2(.06f, .86f), new Vector2(.94f, .94f));
            Label("Seed Subtitle", pickerRect, "It grows by itself. Watering is a kind extra, never a must.",
                28, Pale, new Vector2(.06f, .80f), new Vector2(.94f, .86f)).horizontalOverflow = HorizontalWrapMode.Wrap;
            var list = new GameObject("Seed List", typeof(RectTransform));
            seedList = list.GetComponent<RectTransform>();
            seedList.SetParent(pickerRect, false);
            Stretch(seedList, new Vector2(.12f, .16f), new Vector2(.88f, .78f));
            MakeButton("Seed Cancel", pickerRect, "CANCEL", new Vector2(.25f, .05f), new Vector2(.75f, .12f), Ivory, HideSeedPicker);
            seedPicker.SetActive(false);
        }

        public void ShowSeedPicker(IReadOnlyList<KeyValuePair<string, int>> seeds)
        {
            foreach (GameObject old in seedButtons) Destroy(old);
            seedButtons.Clear();
            const int perColumn = 6;
            for (int i = 0; i < seeds.Count; i++)
            {
                string species = seeds[i].Key;
                int column = seeds.Count > perColumn ? i / perColumn : 0;
                int row = seeds.Count > perColumn ? i % perColumn : i;
                float width = seeds.Count > perColumn ? .48f : 1f;
                float left = column * .52f;
                float top = 1f - row * (1f / perColumn);
                var button = MakeButton("Seed " + species, seedList, GardenController.TreeTitle(species).Replace(" TREE", "") +
                    "   x" + seeds[i].Value, new Vector2(left, top - 1f / perColumn + .02f), new Vector2(left + width, top),
                    Ivory, () => plant?.Invoke(species));
                seedButtons.Add(button.gameObject);
            }
            UiPolish.Apply(seedPicker);
            seedPicker.transform.SetAsLastSibling();
            seedPicker.SetActive(true);
        }

        public void HideSeedPicker()
        {
            if (seedPicker != null) seedPicker.SetActive(false);
        }

        // ---------- Building blocks ----------

        private static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedPanel));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, min, max);
            var face = go.GetComponent<RoundedPanel>();
            face.CornerRadius = 48f;
            face.color = color;
            face.raycastTarget = true; // Taps on the card never reach the tree behind it.
            return rect;
        }

        private Button MakeButton(string name, Transform parent, string label, Vector2 min, Vector2 max,
            Color color, Action click)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanel), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var face = go.GetComponent<RoundedPanel>();
            face.CornerRadius = 34f;
            face.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = face;
            var tint = button.colors;
            tint.normalColor = Color.white;
            tint.highlightedColor = new Color(.96f, .97f, .94f);
            tint.pressedColor = new Color(.82f, .87f, .80f);
            tint.disabledColor = new Color(1f, 1f, 1f, .75f);
            button.colors = tint;
            Label(name + " Label", go.transform, label, 40, Ink, Vector2.zero, Vector2.one);
            if (click != null) button.onClick.AddListener(() => click());
            return button;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, size * 2 / 3);
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
    }
}
