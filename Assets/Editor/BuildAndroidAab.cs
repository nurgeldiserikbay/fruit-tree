#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Manual Play Console bundle build. Requires publisher-controlled signing
    /// configuration in the local Unity Editor, never in the repository.
    /// </summary>
    public static class BuildAndroidAab
    {
        [MenuItem("Fruit Tree/Build Signed Android AAB")]
        public static void Run()
        {
            const string scene = "Assets/Scenes/Main.unity";
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException(
                    "Switch Unity Build Profiles to Android before building an AAB.");
            if (!FruitTreePrivacyConfig.IsPublishedUrlValid(
                    FruitTreePrivacyConfig.PublishedPolicyUrl))
                throw new InvalidOperationException(
                    "Publish the real public Google Docs Privacy Policy, then configure " +
                    "FruitTreePrivacyConfig.PublishedPolicyUrl before release.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                throw new FileNotFoundException(
                    "Generate the Unity scene before creating a Play Store bundle.", scene);

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) !=
                ScriptingImplementation.IL2CPP)
                throw new InvalidOperationException(
                    "Select IL2CPP scripting backend in Android Player Settings.");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                throw new InvalidOperationException(
                    "Enable ARM64 in Android Player Settings.");
            // A release must name a supported Android API level explicitly.
            // Reconfirm Play Console API requirements immediately before upload.
            if ((int)PlayerSettings.Android.targetSdkVersion < 36)
                throw new InvalidOperationException(
                    "Set Android target API level to 36 or later in Player Settings.");

            if (!PlayerSettings.Android.useCustomKeystore ||
                string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) ||
                string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName) ||
                string.IsNullOrWhiteSpace(PlayerSettings.Android.keystorePass) ||
                string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasPass))
                throw new InvalidOperationException(
                    "Configure your OWN upload keystore, alias and passwords locally " +
                    "in Unity Player Settings. Do not commit keystores or secrets.");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.Combine(projectRoot, "Builds", "FruitTree-release.aab");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            ValidateProject.Run();

            bool previousBundleSetting = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = true;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded ||
                    !File.Exists(path))
                    throw new InvalidOperationException(
                        "Signed Android App Bundle build did not finish successfully.");
                Debug.Log("Fruit Tree signed Android App Bundle generated at: " + path +
                    ". Upload to the Play Console INTERNAL test track first.");
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousBundleSetting;
            }
        }
    }
}
#endif
