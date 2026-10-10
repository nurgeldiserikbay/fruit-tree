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
            if (!File.Exists(scene))
                FruitTreeSceneBuilder.Generate();
            if (!File.Exists(scene))
                throw new FileNotFoundException("Fruit Tree scene is missing", scene);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException(
                    "Unity Android Build Support is missing. Install Android SDK, NDK and OpenJDK via Unity Hub.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException(
                    "Select Android in Unity Build Profiles first, or run batchmode with -buildTarget Android.");

            // Do not quietly produce an APK from a broken or incompletely imported scene.
            ValidateProject.Run();

            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = "Builds/FruitTree.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new Exception("Unity Android build failed: " +
                    (report == null ? "no build report" : report.summary.result.ToString()));
            if (!File.Exists(options.locationPathName))
                throw new FileNotFoundException("Build succeeded without an APK", options.locationPathName);
            Debug.Log("Fruit Tree Android APK: " + Path.GetFullPath(options.locationPathName));
        }
    }
}
#endif
