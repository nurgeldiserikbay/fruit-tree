#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// Enters Play Mode and saves portrait screenshots of the Meadow theme to
    /// Builds/. Run in batch mode without -nographics and without -quit.
    /// </summary>
    [InitializeOnLoad]
    public static class PreviewCapture
    {
        private const string ActiveKey = "FruitTree.PreviewCapture.Active";
        private static int frames;

        static PreviewCapture()
        {
            if (SessionState.GetBool(ActiveKey, false))
                EditorApplication.update += Tick;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Opens the playable scene in the normal Editor window.</summary>
        public static void OpenMainScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        }

        private static void Tick()
        {
            if (!Application.isPlaying) return;
            frames++;
            if (frames == 5)
            {
                var environment = Object.FindFirstObjectByType<EnvironmentController>();
                if (environment != null) environment.ApplyThemeDirect(EnvironmentTheme.Meadow);
                // Saved progress may be on another species; the preview judges
                // the apple slice. Regenerating does not write to the save file.
                var tree = Object.FindFirstObjectByType<TreeGenerator>();
                var apple = OrchardGameController.LevelDefinitions[0];
                if (tree != null) tree.Configure(apple.SpeciesId, apple.FruitCount, 122);
            }
            if (frames < 240) return;

            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            int exitCode = 0;
            try
            {
                Camera camera = Camera.main;
                SaveWithInterface(camera, "preview-home.png");
                var orchard = Object.FindFirstObjectByType<OrchardGameController>();
                var flow = Object.FindFirstObjectByType<FruitTreeScreenFlow>();
                if (orchard != null)
                {
                    orchard.OpenTreeMenu();
                    SaveWithInterface(camera, "preview-trees.png");
                    orchard.CloseTreeMenu();
                }
                if (flow != null && flow.settingsPanel != null)
                {
                    flow.OpenSettingsFromHome();
                    SoundLevelsPanel.Open(flow.settingsPanel.transform.parent, null);
                    SaveWithInterface(camera, "preview-sound.png");
                    SoundLevelsPanel.CloseIfOpen();
                    flow.CloseSettings();
                }
                var hud = Object.FindFirstObjectByType<HarvestHUD>();
                if (hud != null && flow != null)
                {
                    flow.Play();
                    hud.ShowReachHint(new Vector2(Screen.width * .5f, Screen.height * .5f));
                    SaveWithInterface(camera, "preview-reach.png");
                    flow.ReturnHome();
                }
                Save(camera, "preview-meadow.png");
                Vector3 startPosition = camera.transform.position;
                Quaternion startRotation = camera.transform.rotation;
                // Low side view: shows how the panorama meets the ground.
                camera.transform.position = new Vector3(3.4f, 1.5f, -7f);
                camera.transform.LookAt(new Vector3(-1.2f, 3.1f, 0f));
                Save(camera, "preview-meadow-forest.png");
                var weather = Object.FindFirstObjectByType<WeatherController>();
                if (weather != null)
                {
                    WeatherKind opening = weather.Current;
                    foreach (WeatherKind kind in System.Enum.GetValues(typeof(WeatherKind)))
                    {
                        weather.ForceWeather(kind);
                        Save(camera, "preview-weather-" + kind.ToString().ToLowerInvariant() + ".png");
                    }
                    weather.ForceWeather(opening);
                }
                var speciesTree = Object.FindFirstObjectByType<TreeGenerator>();
                if (speciesTree != null)
                {
                    camera.transform.position = new Vector3(2.4f, 2.1f, -8.6f);
                    camera.transform.LookAt(new Vector3(0f, 3.2f, 0f));
                    foreach (var level in OrchardGameController.LevelDefinitions)
                    {
                        speciesTree.Configure(level.SpeciesId, level.FruitCount, 122);
                        Save(camera, "preview-species-" + level.SpeciesId + ".png");
                    }
                    var firstLevel = OrchardGameController.LevelDefinitions[0];
                    speciesTree.Configure(firstLevel.SpeciesId, firstLevel.FruitCount, 122);
                }
                var scenery = Object.FindFirstObjectByType<EnvironmentController>();
                if (scenery != null)
                {
                    if (weather != null) weather.ForceWeather(WeatherKind.Clear);
                    camera.transform.position = new Vector3(3.4f, 1.9f, -7f);
                    camera.transform.LookAt(new Vector3(-1.2f, 2.6f, 0f));
                    for (int index = 0; index < EnvironmentThemes.Count; index++)
                    {
                        var theme = (EnvironmentTheme)index;
                        scenery.ApplyThemeDirect(theme);
                        Save(camera, "preview-theme-" + theme + ".png");
                    }
                    var wildlife = Object.FindFirstObjectByType<Wildlife>();
                    foreach (var theme in new[] { EnvironmentTheme.Meadow, EnvironmentTheme.JapaneseGarden })
                    {
                        scenery.ApplyThemeDirect(theme);
                        if (wildlife != null) wildlife.Showcase(camera);
                        Save(camera, "preview-nature-" + theme + ".png");
                    }
                    scenery.ApplyThemeDirect(EnvironmentTheme.Meadow);
                }
                camera.transform.SetPositionAndRotation(startPosition, startRotation);
                Vector3 focus = new Vector3(0f, 3f, 0f);
                camera.transform.position = Vector3.Lerp(camera.transform.position, focus, .48f);
                camera.transform.LookAt(focus);
                Save(camera, "preview-meadow-close.png");
                // Fruit-only view separates fruit mesh defects from leaf occlusion.
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if (renderer.name == "Merged Leaves") renderer.enabled = false;
                Save(camera, "preview-meadow-fruit.png");
            }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        /// <summary>Overlay canvases skip Camera.Render, so route them through the camera once.</summary>
        private static void SaveWithInterface(Camera camera, string fileName)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas || modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + .05f;
            }
            Canvas.ForceUpdateCanvases();
            try { Save(camera, fileName); }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                    if (canvases[i].isRootCanvas) canvases[i].renderMode = modes[i];
            }
        }

        private static void Save(Camera camera, string fileName)
        {
            const int width = 720, height = 1600;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Builds");
                File.WriteAllBytes(Path.Combine("Builds", fileName), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
#endif
