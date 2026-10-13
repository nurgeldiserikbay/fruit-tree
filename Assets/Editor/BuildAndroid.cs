#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FruitTree.Editor
{
    public static class BuildAndroid
    {
        [MenuItem("Fruit Tree/Build Android APK")]
        public static void Run()
        {
            const string scene = "Assets/Scenes/Main.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                FruitTreeSceneBuilder.Generate();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                throw new FileNotFoundException("Fruit Tree scene is missing", scene);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException(
                    "Unity Android Build Support is missing. Install Android SDK, NDK and OpenJDK via Unity Hub.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException(
                    "Select Android in Unity Build Profiles first, or run batchmode with -buildTarget Android.");

            // Do not quietly produce an APK from a broken or incompletely imported scene.
            ValidateProject.Run();

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = Path.Combine(projectRoot, "Builds");
            Directory.CreateDirectory(outputDirectory);
            // The quick APK build must not silently switch the developer's
            // bundle preference for a later signed Play Store release.
            bool previousBundleSetting = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = Path.Combine(outputDirectory, "FruitTree.apk"),
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded)
                    throw new Exception("Unity Android build failed: " +
                        (report == null ? "no build report" : report.summary.result.ToString()));
                if (!File.Exists(options.locationPathName))
                    throw new FileNotFoundException(
                        "Build succeeded without an APK", options.locationPathName);
                Debug.Log("Fruit Tree Android APK: " +
                    Path.GetFullPath(options.locationPathName));
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousBundleSetting;
            }
        }
    }
}
#endif
