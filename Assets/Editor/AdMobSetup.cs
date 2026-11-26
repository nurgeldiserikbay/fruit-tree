#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Writes the AdMob app ID from FruitTreeReleaseConfig into the plugin's settings
    /// before each Android build; without it the plugin stops the build.
    /// </summary>
    public static class AdMobSetup
    {
        [MenuItem("Fruit Tree/Apply AdMob Settings")]
        public static void Apply()
        {
            // The plugin keeps its settings class internal, so reach it by name.
            Type settingsType = Type.GetType(
                "GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            MethodInfo load = settingsType?.GetMethod("LoadInstance",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (load == null) throw new InvalidOperationException("Google Mobile Ads plugin is missing.");
            var settings = (ScriptableObject)load.Invoke(null, null);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("adMobAndroidAppId").stringValue = FruitTreeReleaseConfig.AdMobAndroidAppId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (FruitTreeReleaseConfig.UsesGoogleTestAds)
                Debug.LogWarning("Fruit Tree: AdMob uses Google's test IDs; replace them before release.");
        }
    }
}
#endif
