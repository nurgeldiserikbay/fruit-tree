using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>Keep optional generated textures mobile-friendly and preserve alpha
    /// readability for foliage-aware touch picking.</summary>
    public sealed class TreeTextureImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/TreeTextures/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, System.StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            bool leaf = assetPath.StartsWith(Root + "Leaves/",
                System.StringComparison.OrdinalIgnoreCase);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            // Single leaf cutouts must not repeat their opposite edge into the
            // transparent silhouette. Bark and fruit skins still tile.
            importer.wrapMode = leaf ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.alphaSource = leaf
                ? TextureImporterAlphaSource.FromInput
                : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = leaf;
            // Picking checks the alpha texel to avoid collecting fruits hidden
            // behind a visible leaf. Other textures can discard CPU copies.
            importer.isReadable = leaf;
            importer.textureCompression = TextureImporterCompression.Compressed;

            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 512;
            android.format = TextureImporterFormat.Automatic;
            android.compressionQuality = 50;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
