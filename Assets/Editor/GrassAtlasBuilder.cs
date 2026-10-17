using System.IO;
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    public static class GrassAtlasBuilder
    {
        private const string Folder = "Assets/Resources/Vegetation/Grass/";
        private const string Atlas = Folder + "GrassAtlas.png";

        [MenuItem("FruitTree/Vegetation/Rebuild Grass Atlas")]
        public static void Build()
        {
            var images = new Texture2D[10];
            for (int i = 0; i < images.Length; i++)
            {
                string path = Folder + "grass_" + (i + 1).ToString("00") + ".png";
                if (!File.Exists(path)) return; // Wait for the full PNG pack.
                images[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!images[i].LoadImage(File.ReadAllBytes(path)))
                    throw new IOException("Cannot decode " + path);
            }
            // Fixed 4x3 grid, 256px cells; transparent cells are intentional.
            var atlas = new Texture2D(1024, 768, TextureFormat.RGBA32, false);
            var clear = new Color[1024 * 768];
            atlas.SetPixels(clear);
            for (int i = 0; i < images.Length; i++)
            {
                int col = i % 4, row = i / 4;
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                        atlas.SetPixel(col * 256 + x, (2 - row) * 256 + y,
                            images[i].GetPixelBilinear((x + .5f) / 256f,
                                (y + .5f) / 256f));
                Object.DestroyImmediate(images[i]);
            }
            atlas.Apply();
            File.WriteAllBytes(Atlas, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceUpdate);
            Debug.Log("FruitTree: generated grass atlas from ten PNG sprites.");
        }
    }
}
