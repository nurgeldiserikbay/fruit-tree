using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// The list of all ten trees: which are open, which are locked, how much is
    /// picked. Tapping an open tree goes straight to it. Built in code, no assets.
    /// </summary>
    public sealed class TreeCollectionScreen : MonoBehaviour
    {
        public struct Entry
        {
            public string Id, Title;
            public Color Fruit;
            public int Picked, Total;
            public bool Unlocked, Cleared, Active;
        }

        private static readonly Color Veil = new Color(.05f, .12f, .09f, .93f);
        private static readonly Color Ivory = new Color(.97f, .965f, .89f, 1f);
        private static readonly Color Current = new Color(.97f, .86f, .61f, 1f);
        private static readonly Color Locked = new Color(.22f, .29f, .25f, 1f);
        private static readonly Color Ink = new Color(.18f, .26f, .17f, 1f);
        private static readonly Color Pale = new Color(.92f, .94f, .88f, .8f);

        private sealed class Row
        {
            public Button Button;
            public RoundedPanel Face, Dot;
            public Text Name, Status;
            public string Id;
        }

        private readonly List<Row> rows = new List<Row>();
        private Action<string> open;
        private Font font;

        public static TreeCollectionScreen Create(Transform parent, Font font, int treeCount,
            Action<string> open, Action close)
        {
            var root = new GameObject("Tree Collection Screen", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanel));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, Vector2.zero, Vector2.one);
            var veil = root.GetComponent<RoundedPanel>();
            veil.CornerRadius = 0f;
            veil.color = Veil;
            veil.raycastTarget = true; // The list blocks taps on the tree behind it.

            var screen = root.AddComponent<TreeCollectionScreen>();
            screen.open = open;
            screen.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen.Label("Title", rect, "MY TREES", 76, Color.white, TextAnchor.MiddleCenter,
                new Vector2(.06f, .905f), new Vector2(.94f, .97f));
            screen.Label("Subtitle", rect, "Pick every fruit on a tree to open the next one",
                30, Pale, TextAnchor.MiddleCenter, new Vector2(.06f, .862f), new Vector2(.94f, .905f));

            const float top = .845f, bottom = .155f;
            float step = (top - bottom) / treeCount;
            for (int i = 0; i < treeCount; i++)
            {
                float rowTop = top - i * step;
                screen.rows.Add(screen.MakeRow(rect, new Vector2(.06f, rowTop - step + .006f),
                    new Vector2(.94f, rowTop - .006f)));
            }

            var back = MakeButton("Back", rect, new Vector2(.25f, .05f), new Vector2(.75f, .12f), Ivory);
            screen.Label("Back Label", back.transform, "BACK", 44, Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            back.onClick.AddListener(() => close?.Invoke());
            root.SetActive(false);
            return screen;
        }

        /// <summary>Refreshes every row and shows the screen on top of everything.</summary>
        public void Show(IReadOnlyList<Entry> entries)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                bool present = i < entries.Count;
                row.Button.gameObject.SetActive(present);
                if (!present) continue;
                Entry entry = entries[i];
                row.Id = entry.Id;
                row.Button.interactable = entry.Unlocked;
                row.Face.color = !entry.Unlocked ? Locked : entry.Active ? Current : Ivory;
                row.Dot.color = entry.Unlocked ? entry.Fruit : new Color(.3f, .34f, .3f, .7f);
                row.Name.text = (i + 1) + ".  " + entry.Title;
                row.Name.color = entry.Unlocked ? Ink : new Color(.86f, .9f, .85f, .75f);
                row.Status.color = row.Name.color;
                row.Status.text = StatusText(entry);
            }
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public static string StatusText(Entry entry)
        {
            if (!entry.Unlocked) return "LOCKED";
            if (entry.Cleared) return "DONE - TAP TO REGROW";
            if (entry.Active) return entry.Picked + " / " + entry.Total + "  HERE";
            return entry.Picked + " / " + entry.Total;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public bool IsOpen => gameObject.activeSelf;

        private Row MakeRow(RectTransform parent, Vector2 min, Vector2 max)
        {
            var row = new Row();
            row.Button = MakeButton("Tree Row", parent, min, max, Ivory);
            row.Face = row.Button.GetComponent<RoundedPanel>();
            var dot = new GameObject("Fruit Colour", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanel));
            var dotRect = dot.GetComponent<RectTransform>();
            dotRect.SetParent(row.Button.transform, false);
            Stretch(dotRect, new Vector2(.035f, .24f), new Vector2(.095f, .76f));
            row.Dot = dot.GetComponent<RoundedPanel>();
            row.Dot.CornerRadius = 100f;
            row.Dot.raycastTarget = false;
            row.Name = Label("Tree Name", row.Button.transform, "", 34, Ink, TextAnchor.MiddleLeft,
                new Vector2(.13f, .1f), new Vector2(.66f, .9f));
            row.Status = Label("Tree Status", row.Button.transform, "", 28, Ink, TextAnchor.MiddleRight,
                new Vector2(.5f, .1f), new Vector2(.96f, .9f));
            row.Button.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(row.Id)) open?.Invoke(row.Id);
            });
            return row;
        }

        private static Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanel), typeof(Button));
            Stretch(go.GetComponent<RectTransform>(), min, max);
            go.transform.SetParent(parent, false);
            var face = go.GetComponent<RoundedPanel>();
            face.CornerRadius = 34f;
            face.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = face;
            // The face colour itself carries the state, so tints only darken it a little.
            var tint = button.colors;
            tint.normalColor = Color.white;
            tint.highlightedColor = new Color(.96f, .97f, .94f);
            tint.pressedColor = new Color(.82f, .87f, .80f);
            tint.disabledColor = Color.white;
            button.colors = tint;
            return button;
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
    }
}
