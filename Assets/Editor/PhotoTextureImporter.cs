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
