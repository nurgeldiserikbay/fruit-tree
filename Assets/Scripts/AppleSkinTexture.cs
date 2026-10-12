using UnityEngine;

namespace FruitTree
{
    /// <summary>Small seamless apple-skin color texture with subtle natural mottling.</summary>
    public static class AppleSkinTexture
    {
        public static Texture2D CreateForSpecies(string species)
        {
            if (species == "apple") return Create();

            // Compact 64 px shared swatch for pears/cherries: no per-fruit
            // texture allocation, no individual materials per harvested fruit.
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true)
            {
                name = species == "pear" ? "Pear Skin" :
                    species == "peach" ? "Peach Skin" :
                    species == "orange" ? "Citrus Skin" :
                    species == "apricot" ? "Apricot Skin" :
                    species == "plum" ? "Plum Bloom" :
                    species == "lemon" ? "Lemon Skin" :
                    species == "mandarin" ? "Mandarin Skin" :
                    species == "pomegranate" ? "Pomegranate Skin" : "Cherry Skin",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float a = Mathf.PerlinNoise(u * 5f + 21f, v * 5f + 11f);
                    float b = Mathf.PerlinNoise((u - 1f) * 5f + 21f, v * 5f + 11f);
                    float mottling = Mathf.Lerp(a, b, u);
                    float freckles = Mathf.PerlinNoise(u * 29f + 44f, v * 27f + 18f);
                    if (species == "pear")
                    {
                        float tone = .89f + .15f * mottling;
                        float speck = freckles > .8f ? .07f : 0f;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone + speck), Mathf.Clamp01(tone * .96f +
                            speck * .65f), Mathf.Clamp01(tone * .66f));
                    }
                    else if (species == "peach")
                    {
                        // A pale velvety peach blush and a soft orange-gold side.
                        float blush = Mathf.SmoothStep(.33f, .78f, mottling);
                        float tone = .89f + .11f * mottling;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(.88f * tone -
                            .14f * blush), Mathf.Clamp01(.78f * tone -
                            .18f * blush));
                    }
                    else if (species == "orange")
                    {
                        // Very small pores: no large normal map or shiny citrus shader.
                        float pores = freckles > .74f ? .075f : 0f;
                        float tone = .9f + .12f * mottling - pores;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(.96f * tone),
                            Mathf.Clamp01(.89f * tone));
                    }
                    else if (species == "apricot")
                    {
                        float warmth = Mathf.SmoothStep(.28f, .79f, mottling);
                        float tone = .88f + .12f * mottling;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(tone * (.96f - warmth * .10f)),
                            Mathf.Clamp01(tone * (.75f - warmth * .14f)));
                    }
                    else if (species == "plum")
                    {
                        // Delicate powdery bloom over the deep-purple material tint.
                        float dust = freckles > .66f ? .068f : 0f;
                        float tone = .91f + mottling * .09f + dust;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone * .96f), Mathf.Clamp01(tone),
                            Mathf.Clamp01(tone * 1.07f));
                    }
                    else if (species == "lemon")
                    {
                        float pores = freckles > .70f ? .055f : 0f;
                        float tone = .90f + .12f * mottling - pores;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(tone * .99f),
                            Mathf.Clamp01(tone * .84f));
                    }
                    else if (species == "mandarin")
                    {
                        float pores = freckles > .72f ? .063f : 0f;
                        float tone = .87f + .15f * mottling - pores;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(tone * .95f),
                            Mathf.Clamp01(tone * .86f));
                    }
                    else if (species == "pomegranate")
                    {
                        float blush = Mathf.SmoothStep(.3f, .78f, mottling);
                        float dots = freckles > .79f ? .035f : 0f;
                        float tone = .89f + .1f * blush + dots;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(tone * .88f),
                            Mathf.Clamp01(tone * .82f));
                    }
                    else
                    {
                        float tone = .88f + .14f * mottling;
                        pixels[y * size + x] = new Color(
                            Mathf.Clamp01(tone), Mathf.Clamp01(tone * .92f),
                            Mathf.Clamp01(tone * .88f));
                    }
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

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
