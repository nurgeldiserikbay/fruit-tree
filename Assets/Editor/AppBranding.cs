#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Puts the app icon on Android (legacy, round and adaptive layers) and the
    /// game logo on the launch splash screen. Runs before every Android build.
    /// </summary>
    public static class AppBranding
    {
        public const string LegacyIcon = "Assets/Art/Icon/FruitTreeIconLegacy.png";
        public const string IconBackground = "Assets/Art/Icon/FruitTreeIconBackground.png";
        public const string IconForeground = "Assets/Art/Icon/FruitTreeIconForeground.png";
        public const string Logo = "Assets/Resources/Branding/FruitTreeLogo.png";
        private static readonly Color SplashBackground = new Color(.06f, .14f, .10f);

        [MenuItem("Fruit Tree/Apply App Icon And Logo")]
        public static void Apply()
        {
            var legacy = Load<Texture2D>(LegacyIcon);
            var background = Load<Texture2D>(IconBackground);
            var foreground = Load<Texture2D>(IconForeground);
            var logo = Load<Sprite>(Logo);

            // Unity 6 builds Android icons from adaptive layers only.
            NamedBuildTarget android = NamedBuildTarget.Android;
            PlatformIcon[] adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
            // Adaptive icons take two layers: background first, then foreground.
            foreach (PlatformIcon icon in adaptive) icon.SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, adaptive);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { legacy }, IconKind.Any);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.backgroundColor = SplashBackground;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
            AssetDatabase.SaveAssets();
            Debug.Log("Fruit Tree: app icon and splash logo applied.");
        }

        private static T Load<T>(string path) where T : Object
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new FileNotFoundException("Branding asset is missing", path);
            return asset;
        }
    }
}
#endif
