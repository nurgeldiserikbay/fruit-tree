using UnityEngine;

namespace FruitTree
{
    /// <summary>Small seamless apple-skin color texture with subtle natural mottling.</summary>
    public static class AppleSkinTexture
    {
        public static Texture2D Create()
        {
            const int width = 128;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, true)
            {
                name = "Generated Apple Skin",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    // Blend two shifted samples, so the horizontal seam joins cleanly.
                    float left = Mathf.PerlinNoise(u * 6f + 17f, v * 7f + 21f);
                    float right = Mathf.PerlinNoise((u - 1f) * 6f + 17f, v * 7f + 21f);
                    float noise = Mathf.Lerp(left, right, u);
                    // Very soft vertical warm-gold streaks and a few pale
                    // freckles; never a repeated arcade-red flat surface.
                    float stripe = Mathf.Sin((u * 9f +
                        Mathf.Sin(v * 5f) * .045f) * Mathf.PI * 2f);
                    float freckles = Mathf.PerlinNoise(u * 43f + 41f, v * 39f + 27f);
                    float tone = Mathf.Clamp01(.82f + .14f * noise + stripe * .04f);
                    float gold = Mathf.SmoothStep(.73f, .99f, v) * .14f +
                        Mathf.Max(0f, stripe) * .075f;
                    float fleck = freckles > .79f ? .062f : 0f;
                    pixels[y * width + x] = new Color(
                        Mathf.Clamp01((.78f + gold * .50f + fleck) * tone),
                        Mathf.Clamp01((.12f + gold + fleck * .66f) * tone),
                        Mathf.Clamp01((.075f + gold * .25f) * tone));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
