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
                    float blush = Mathf.Sin((u * 13f + Mathf.Sin(v * 7f) * .23f) * Mathf.PI * 2f) * .03f;
                    float freckles = Mathf.PerlinNoise(u * 36f + 41f, v * 31f + 27f);
                    float tone = Mathf.Clamp01(.82f + .18f * noise + blush - (freckles > .83f ? .045f : 0f));
                    float green = Mathf.Lerp(.16f, .095f, Mathf.SmoothStep(0f, 1f, v));
                    pixels[y * width + x] = new Color(
                        Mathf.Clamp01(.79f * tone),
                        Mathf.Clamp01(green * tone),
                        Mathf.Clamp01(.052f * tone));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
