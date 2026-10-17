using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Makes the user-supplied grass pack available in Android builds.
    /// Assets/Art is an Editor-only load path; player builds can only use
    /// Resources/Vegetation/Grass. Preserve originals and copy once.
    /// </summary>
    [InitializeOnLoad]
    public static class GrassResourcesInstaller
    {
        private const string Source = "Assets/Art/Vegetation/Grass/";
        private const string Destination = "Assets/Resources/Vegetation/Grass/";

        static GrassResourcesInstaller()
        {
            EditorApplication.delayCall += EnsureRuntimeAssets;
        }

        [MenuItem("FruitTree/Vegetation/Sync Grass for Android")]
        public static void EnsureRuntimeAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling) return;
            bool changed = false;
            for (int i = 1; i <= 10; i++)
            {
                string name = "grass_" + i.ToString("00") + ".png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(Source + name) == null)
                    continue;
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(Destination + name) != null)
                    continue;
                EnsureFolder("Assets/Resources", "Vegetation");
                EnsureFolder("Assets/Resources/Vegetation", "Grass");
                if (AssetDatabase.CopyAsset(Source + name, Destination + name))
                    changed = true;
                else
                    Debug.LogWarning("FruitTree: failed to copy " + name +
                        " to Resources for Android build.");
            }
            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("FruitTree: grass artwork copied to Resources for Android.");
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
