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
                    float longRidges = Mathf.PerlinNoise(u * 13f, v * 2.5f);
                    float grain = Mathf.PerlinNoise(u * 43f + 17f, v * 5.3f);
                    float crack = Mathf.Pow(1f - Mathf.Abs(2f * longRidges - 1f), 12f);
                    float tone = Mathf.Clamp01(.5f + .34f * longRidges + .17f * grain - .38f * crack);
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
