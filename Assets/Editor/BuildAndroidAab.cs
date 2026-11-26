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
        /// <summary>For Play closed or internal testing: only Google test ads.</summary>
        [MenuItem("Fruit Tree/Build Signed AAB - Testing (test ads)")]
        public static void RunTesting() => Build(testAds: true);

        /// <summary>For the production release: Fruit Tree's real AdMob ads.</summary>
        [MenuItem("Fruit Tree/Build Signed AAB - Production (real ads)")]
        public static void RunProduction() => Build(testAds: false);

        private static void Build(bool testAds)
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
            // The AdMob app ID is always Fruit Tree's own; only the ad unit
            // switches. A production bundle must never carry the test define.
            if (FruitTreeReleaseConfig.UsesGoogleTestAds)
                throw new InvalidOperationException(
                    "Put Fruit Tree's own AdMob app ID in FruitTreeReleaseConfig.");
            if (!testAds && PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android)
                    .Contains("FRUITTREE_TEST_ADS"))
                throw new InvalidOperationException(
                    "Remove FRUITTREE_TEST_ADS from Player Settings before a production bundle.");
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
            string path = Path.Combine(projectRoot, "Builds",
                testAds ? "FruitTree-testing.aab" : "FruitTree-release.aab");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            ValidateProject.Run();
            // A game requiring 100% fruit collection must not ship with an
            // unreachable fruit on any of the ten species.
            FruitReachabilityAudit.AuditAllSpecies();

            bool previousBundleSetting = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = true;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                    extraScriptingDefines = testAds
                        ? new[] { "FRUITTREE_TEST_ADS" } : Array.Empty<string>()
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded ||
                    !File.Exists(path))
                    throw new InvalidOperationException(
                        "Signed Android App Bundle build did not finish successfully.");
                Debug.Log("Fruit Tree signed Android App Bundle generated at: " + path +
                    (testAds ? ". TEST ADS ONLY: upload to internal or closed testing."
                             : ". REAL ADS: upload to production only after testing."));
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousBundleSetting;
            }
        }
    }
}
#endif
