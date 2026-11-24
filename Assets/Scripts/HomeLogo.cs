using UnityEngine;
using UnityEngine.UI;

namespace FruitTree
{
    /// <summary>Shows the painted game logo on the home card instead of the plain title text.</summary>
    public static class HomeLogo
    {
        public const string ResourcePath = "Branding/FruitTreeLogo";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var logo = Resources.Load<Texture2D>(ResourcePath);
            if (logo == null) return;
            Text title = null, eyebrow = null;
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (text.name == "Game Logo") title = text;
                else if (text.name == "Home Eyebrow") eyebrow = text;
            }
            if (title != null) Apply(title, eyebrow, logo);
        }

        /// <summary>Places the logo over the title area; the text stays as a hidden fallback.</summary>
        public static RawImage Apply(Text title, Text eyebrow, Texture2D logo)
        {
            // The fitter stretches its object over the whole parent, so the logo
            // gets its own area: above the tagline, over the old title and eyebrow.
            var area = new GameObject("Game Logo Area", typeof(RectTransform));
            var areaRect = area.GetComponent<RectTransform>();
            areaRect.SetParent(title.transform.parent, false);
            areaRect.SetSiblingIndex(title.transform.GetSiblingIndex());
            areaRect.anchorMin = new Vector2(.04f, .36f);
            areaRect.anchorMax = new Vector2(.96f, .99f);
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;
            var image = new GameObject("Game Logo Image", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            image.GetComponent<RectTransform>().SetParent(areaRect, false);
            var raw = image.GetComponent<RawImage>();
            raw.texture = logo;
            raw.raycastTarget = false;
            var fitter = image.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = logo.width / (float)Mathf.Max(1, logo.height);
            title.gameObject.SetActive(false);
            if (eyebrow != null) eyebrow.gameObject.SetActive(false);
            return raw;
        }
    }
}
