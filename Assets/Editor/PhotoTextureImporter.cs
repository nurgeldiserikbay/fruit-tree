#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Import rules for photographic game textures, so they stay correct
    /// when a file is replaced or a fresh checkout reimports the project.
    /// </summary>
    public sealed class PhotoTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith("Assets/Resources/TreeTextures/Leaves/"))
            {
                // Fruit picking reads leaf alpha at runtime, so it must be readable.
                importer.isReadable = true;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = .45f;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 512;
            }
            else if (assetPath.StartsWith("Assets/Resources/Branding/"))
            {
                // A sprite so the splash screen can use it; mipmaps keep it smooth when small.
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                // The original import had alpha switched off, which drew a black box.
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
            }
            else if (assetPath.StartsWith("Assets/Art/Icon/"))
            {
                // Unity scales these to every launcher size; keep the source lossless.
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
            }
            else if (assetPath == "Assets/Resources/TreeTextures/Bark.jpg" ||
                assetPath.StartsWith("Assets/Resources/Ground/"))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.maxTextureSize = 1024;
            }
        }
    }
}
#endif
