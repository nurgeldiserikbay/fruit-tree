#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FruitTree.Editor
{
    /// <summary>
    /// Inspect the generated game scene and all required references.
    /// Useful for first import and batchmode smoke tests without a human player.
    /// </summary>
    public static class ValidateProject
    {
        [MenuItem("Fruit Tree/Validate Project")]
        public static void Run()
        {
            const string path = "Assets/Scenes/Main.unity";
            if (!File.Exists(path)) FruitTreeSceneBuilder.Generate();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var errors = new List<string>();

            var root = GameObject.Find("Apple Tree");
            var tree = root != null ? root.GetComponent<TreeGenerator>() : null;
            if (tree == null) errors.Add("Apple Tree or TreeGenerator is missing");

            var camera = Camera.main;
            if (camera == null || camera.GetComponent<OrbitPickingCamera>() == null)
                errors.Add("Main camera or orbit input component is missing");

            var hudObject = GameObject.Find("HUD");
            var hud = hudObject != null ? hudObject.GetComponent<HarvestHUD>() : null;
            if (hud == null || hud.tree != tree || hud.countText == null)
                errors.Add("Harvest HUD has missing references");

            var pause = hudObject != null ? hudObject.GetComponent<PauseController>() : null;
            if (pause == null || pause.panel == null || pause.hud != hud)
                errors.Add("Pause menu has missing references");

            var soundObject = GameObject.Find("Orchard Sounds");
            var sound = soundObject != null ? soundObject.GetComponent<OrchardAudio>() : null;
            if (sound == null || sound.tree != tree || sound.soundStatusText == null)
                errors.Add("Orchard audio has missing references");

            if (hudObject != null)
            {
                var buttons = hudObject.GetComponentsInChildren<Button>(true);
                if (buttons.Length != 4)
                    errors.Add("Expected Pause, Continue, Restart and Sound buttons");
                foreach (var button in buttons)
                {
                    if (button.onClick.GetPersistentEventCount() < 1 ||
                        button.onClick.GetPersistentTarget(0) == null)
                        errors.Add("Button without a persistent callback: " + button.name);
                }
            }

            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/VertexLitReference.mat") == null)
                errors.Add("Foliage shader reference material is missing");
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SoftSkyReference.mat") == null)
                errors.Add("Sky shader reference material is missing");

            bool foundBuildScene = false;
            foreach (var entry in EditorBuildSettings.scenes)
                if (entry.enabled && entry.path == path) foundBuildScene = true;
            if (!foundBuildScene) errors.Add("Playable scene is not in build settings");

            if (errors.Count > 0)
                throw new InvalidOperationException("Fruit Tree validation failed:\n" +
                    string.Join("\n", errors));

            FruitReachabilityAudit.Run();

            Debug.Log("Fruit Tree scene validated. Camera, tree, menu, buttons, " +
                "audio, shaders and build settings have valid serialized references.");
        }
    }
}
#endif
