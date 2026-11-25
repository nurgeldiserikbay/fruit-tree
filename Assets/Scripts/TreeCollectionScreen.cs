using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>
    /// The scrolling list of all ten trees: a picture, a short description and
    /// how much is picked. Tapping an open tree goes straight to it. Built in code.
    /// </summary>
    public sealed class TreeCollectionScreen : MonoBehaviour
    {
        public struct Entry
        {
            public string Id, Title, SpeciesId, Description;
            public Color Fruit;
            public int Picked, Total;
            public bool Unlocked, Cleared, Active;
        }

        public const string ThumbnailFolder = "TreeThumbs/";
        private const float CardHeight = 330f, CardGap = 22f, Inset = 16f;
        private static readonly Color Veil = new Color(.05f, .12f, .09f, .96f);
        private static readonly Color Ivory = new Color(.97f, .965f, .89f, 1f);
        private static readonly Color Current = new Color(.97f, .86f, .61f, 1f);
        private static readonly Color Locked = new Color(.22f, .29f, .25f, 1f);
        private static readonly Color Ink = new Color(.18f, .26f, .17f, 1f);
        private static readonly Color Soft = new Color(.30f, .38f, .28f, 1f);
        private static readonly Color Pale = new Color(.92f, .94f, .88f, .8f);
        private static readonly Color Shadow = new Color(.12f, .17f, .14f, 1f);

        private sealed class Card
        {
            public Button Button;
            public RoundedPanel Face;
            public RawImage Picture;
            public Text Name, About, Status;
            public string Id;
        }

        private readonly List<Card> cards = new List<Card>();
        private readonly Dictionary<string, Texture2D> pictures = new Dictionary<string, Texture2D>();
        private Action<string> open;
        private Font font;
        private ScrollRect scroll;
        private RectTransform content;

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
                new Vector2(.06f, .915f), new Vector2(.94f, .975f));
            screen.Label("Subtitle", rect, "Pick every fruit on a tree to open the next one",
                30, Pale, TextAnchor.MiddleCenter, new Vector2(.06f, .875f), new Vector2(.94f, .915f));
            screen.BuildList(rect, treeCount);

            var back = MakeButton("Back", rect, new Vector2(.25f, .035f), new Vector2(.75f, .105f), Ivory);
            screen.Label("Back Label", back.transform, "BACK", 44, Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            back.onClick.AddListener(() => close?.Invoke());
            UiPolish.Apply(root);
            root.SetActive(false);
            return screen;
        }

        private void BuildList(RectTransform parent, int treeCount)
        {
            // Viewport: clips the cards and catches drags anywhere in the list.
            // Its own canvas (with its own raycaster for taps): scrolling redraws only the list.
            var viewport = new GameObject("Tree List", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster), typeof(CanvasRenderer), typeof(RoundedPanel),
                typeof(RectMask2D), typeof(ScrollRect));
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.SetParent(parent, false);
            Stretch(viewRect, new Vector2(.04f, .125f), new Vector2(.96f, .865f));
            var catcher = viewport.GetComponent<RoundedPanel>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            var list = new GameObject("Cards", typeof(RectTransform));
            content = list.GetComponent<RectTransform>();
            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            content.sizeDelta = new Vector2(0f, treeCount * (CardHeight + CardGap) - CardGap);
            content.anchoredPosition = Vector2.zero;

            scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            for (int i = 0; i < treeCount; i++) cards.Add(MakeCard(i));
        }

        /// <summary>Refreshes every card and shows the list, scrolled to the current tree.</summary>
        public void Show(IReadOnlyList<Entry> entries)
        {
            int activeIndex = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                bool present = i < entries.Count;
                card.Button.gameObject.SetActive(present);
                if (!present) continue;
                Entry entry = entries[i];
                if (entry.Active) activeIndex = i;
                card.Id = entry.Id;
                card.Button.interactable = entry.Unlocked;
                card.Face.color = !entry.Unlocked ? Locked : entry.Active ? Current : Ivory;
                card.Picture.texture = Picture(entry.SpeciesId);
                card.Picture.enabled = card.Picture.texture != null;
                // A locked tree shows only its dark silhouette.
                card.Picture.color = entry.Unlocked ? Color.white : Shadow;
                card.Name.text = (i + 1) + ".  " + entry.Title;
                card.About.text = entry.Description;
                card.Status.text = StatusText(entry);
                Color text = entry.Unlocked ? Ink : new Color(.86f, .9f, .85f, .8f);
                card.Name.color = text;
                card.About.color = entry.Unlocked ? Soft : new Color(.75f, .8f, .75f, .65f);
                card.Status.color = text;
            }
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            ScrollTo(activeIndex);
        }

        private void ScrollTo(int index)
        {
            Canvas.ForceUpdateCanvases();
            float viewHeight = scroll.viewport.rect.height;
            float top = index * (CardHeight + CardGap) - (viewHeight - CardHeight) * .5f;
            float limit = Mathf.Max(0f, content.sizeDelta.y - viewHeight);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(top, 0f, limit));
        }

        public static string StatusText(Entry entry)
        {
            if (!entry.Unlocked) return "LOCKED";
            if (entry.Cleared) return "All picked - tap to regrow";
            string picked = entry.Picked + " / " + entry.Total + " picked";
            return entry.Active ? picked + "  -  YOU ARE HERE" : picked;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public bool IsOpen => gameObject.activeSelf;

        private Texture2D Picture(string species)
        {
            if (string.IsNullOrEmpty(species)) return null;
            if (!pictures.TryGetValue(species, out Texture2D picture))
            {
                picture = Resources.Load<Texture2D>(ThumbnailFolder + species);
                pictures[species] = picture;
            }
            return picture;
        }

        private Card MakeCard(int index)
        {
            var card = new Card();
            card.Button = MakeButton("Tree Card", content, Vector2.zero, Vector2.zero, Ivory);
            var rect = (RectTransform)card.Button.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(0f, CardHeight);
            rect.anchoredPosition = new Vector2(0f, -index * (CardHeight + CardGap));
            card.Face = card.Button.GetComponent<RoundedPanel>();
            card.Face.CornerRadius = 44f;

            var picture = new GameObject("Tree Picture", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RawImage));
            var pictureRect = picture.GetComponent<RectTransform>();
            pictureRect.SetParent(rect, false);
            pictureRect.anchorMin = new Vector2(0f, 0f);
            pictureRect.anchorMax = new Vector2(0f, 1f);
            pictureRect.pivot = new Vector2(0f, .5f);
            float side = CardHeight - Inset * 2f;
            pictureRect.offsetMin = new Vector2(Inset, Inset);
            pictureRect.offsetMax = new Vector2(Inset + side, -Inset);
            card.Picture = picture.GetComponent<RawImage>();
            card.Picture.raycastTarget = false;

            float textLeft = Inset * 2f + side;
            card.Name = TextBlock("Tree Name", rect, 44, TextAnchor.LowerLeft, .66f, .93f, textLeft);
            card.About = TextBlock("Tree Description", rect, 29, TextAnchor.UpperLeft, .27f, .64f, textLeft);
            card.Status = TextBlock("Tree Status", rect, 30, TextAnchor.MiddleLeft, .06f, .27f, textLeft);
            card.Button.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(card.Id)) open?.Invoke(card.Id);
            });
            return card;
        }

        private Text TextBlock(string name, RectTransform card, int size, TextAnchor alignment,
            float bottom, float top, float left)
        {
            Text text = Label(name, card, "", size, Ink, alignment,
                new Vector2(0f, bottom), new Vector2(1f, top));
            var rect = text.rectTransform;
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-28f, 0f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, Color color)
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
