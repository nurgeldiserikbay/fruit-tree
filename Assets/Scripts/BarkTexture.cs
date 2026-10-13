using UnityEngine;

namespace FruitTree
{
    public static class BarkTexture
    {
        /// <summary>Procedural bark avoids external image licenses and stays at mobile texture resolution.</summary>
        public static Texture2D Create()
        {
            const int width = 128, height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, true)
            {
                name = "Generated Bark",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width, v = y / (float)height;
                    // Warped vertical fibers and branching hairline cracks keep the
                    // bark directional without an expensive normal map.
                    float warp = (Mathf.PerlinNoise(u * 3.5f + 17f, v * 1.8f) - .5f) * .12f;
                    float fiber = Mathf.PerlinNoise((u + warp) * 19f, v * 1.8f);
                    float grain = Mathf.PerlinNoise((u + warp) * 63f + 17f, v * 8.3f);
                    float coarse = Mathf.PerlinNoise(u * 5.1f, v * 5.6f);
                    float fissure = Mathf.Pow(1f - Mathf.Abs(2f * fiber - 1f), 18f);
                    float shortCrack = Mathf.Pow(1f - Mathf.Abs(2f * coarse - 1f), 20f);
                    float tone = Mathf.Clamp01(.42f + .31f * fiber + .17f * grain +
                        .12f * coarse - .34f * fissure - .12f * shortCrack);
                    byte r = (byte)Mathf.RoundToInt(170f * tone);
                    byte g = (byte)Mathf.RoundToInt(122f * tone);
                    byte b = (byte)Mathf.RoundToInt(75f * tone);
                    pixels[y * width + x] = new Color32(r, g, b, 255);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
