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

            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = "Builds/FruitTree.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Unity Android build failed: " + report.summary.result);
            Debug.Log("Fruit Tree Android APK: " + Path.GetFullPath(options.locationPathName));
        }
    }
}
#endif
